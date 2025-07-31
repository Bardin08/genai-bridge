using System.Text;
using System.Text.RegularExpressions;
using GenAI.Bridge.Context;
using GenAI.Bridge.Contracts.Prompts;
using GenAI.Bridge.Contracts.Scenarios;
using GenAI.Bridge.Utils;

namespace GenAI.Bridge.Scenarios.Middlewares;

public sealed partial class ContextPopulationMiddleware(IInMemContextStore contextStore) : IStageMiddleware
{
    // Combined regex pattern to match both formats:
    // {{var_name}} - captured in group 1
    // {param_name} - captured in group 2
    [GeneratedRegex(@"\{\{([^}]+)\}\}|\{([^{]+)\}", RegexOptions.Compiled)]
    private static partial Regex VariablesKeysRegex();

    public async Task InvokeAsync(StageExecutionContext ctx, Func<Task> next, CancellationToken ct)
    {
        var turns = new List<PromptTurn>();
        foreach (var turn in ctx.Stage.Turns)
        {
            if (!turn.IsUserTurn)
            {
                turns.Add(turn);
                continue;
            }

            var filledContent = await SubstituteParametersAsync(turn.Content, PlaceholderValueResolver(ctx, ct), ct);
            turns.Add(turn with { Content = filledContent });
        }

        ctx.Stage = ctx.Stage with { Turns = turns };

        await next();
    }

    /// <summary>
    /// Substitutes parameters in the input string. Supports two formats:
    /// - {{var_name}} - regular parameters (isParam = false)
    /// - {var_name} - alternative format (isParam = true)
    /// </summary>
    private static async Task<string> SubstituteParametersAsync(
        string input, 
        Func<string, bool, Task<string>> replacer, 
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(replacer);


        var result = new StringBuilder();
        var lastIndex = 0;

        foreach (Match match in VariablesKeysRegex().Matches(input))
        {
            ct.ThrowIfCancellationRequested();
            
            // Append text before the match
            result.Append(input, lastIndex, match.Index - lastIndex);
            
            // Determine which format was matched and extract the variable name
            string varName;
            bool isParam;
            
            if (match.Groups[1].Success)
            {
                // {{var_name}} format
                varName = match.Groups[1].Value;
                isParam = false;
            }
            else
            {
                // }var_name{ format
                varName = match.Groups[2].Value;
                isParam = true;
            }
            
            var replacement = await replacer(varName, isParam);
            result.Append(replacement);
            
            lastIndex = match.Index + match.Length;
        }

        result.Append(input, lastIndex, input.Length - lastIndex);
        return result.ToString();
    }

    private Func<string, bool, Task<string>> PlaceholderValueResolver(StageExecutionContext ctx, CancellationToken ct)
        => (variableKey, isParam) => GetVariableValueAsync(ctx.SessionId, ctx.Stage, variableKey, isParam, ct);

    private async Task<string> GetVariableValueAsync(
        string sid, ScenarioStage stage, string key, bool isParameter, CancellationToken ct)
    {
        var isOutputSubstitution = key.Contains(":output", StringComparison.OrdinalIgnoreCase);
        if (isOutputSubstitution)
        {
            return await GetOutputValueAsync(sid, key, ct);
        }
        
        if (isParameter)
            return await HandleParameterSubstitution(sid, stage, key, ct);

        var variableValue = await contextStore.LoadItemAsync<object>(sid, key, ct);
        return variableValue?.ToString() ?? string.Empty;
    }

    private async Task<string> GetOutputValueAsync(string sid, string key, CancellationToken ct)
    {
        var outputKeyParts = SplitOutputKey(key); 
        var output = (await contextStore.LoadItemAsync<object>(sid, outputKeyParts.recordKey, ct))?.ToString() ?? string.Empty;

        try
        {
            var jsonNavigator = new JsonNavigator(output);
            var value = jsonNavigator.GetValue(outputKeyParts.routingKey);
            return value ?? "{}";
        }
        catch
        {
            return output;
        }
    }

    /// <summary>
    /// Splits a key into output request key and output routing key.
    /// The split occurs at the first ":" after ":output".
    /// </summary>
    /// <param name="key">The input key to split</param>
    /// <returns>A tuple containing (outputReqKey, outputRoutingKey)</returns>
    private static (string recordKey, string routingKey) SplitOutputKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        
        const string outputMarker = ":output";
        
        var outputIndex = key.IndexOf(outputMarker, StringComparison.Ordinal);
        if (outputIndex == -1)
        {
            return (key, string.Empty);
        }
        
        var afterOutputIndex = outputIndex + outputMarker.Length;
        if (afterOutputIndex >= key.Length)
        {
            return (key, string.Empty);
        }
        
        var nextColonIndex = key.IndexOf(':', afterOutputIndex);
        if (nextColonIndex == -1)
        {
            return (key, string.Empty);
        }
        
        var outputReqKey = key[..nextColonIndex];
        var outputRoutingKey = key[(nextColonIndex + 1)..];

        return (outputReqKey, outputRoutingKey);
    }

    private async Task<string> HandleParameterSubstitution(string sid, ScenarioStage stage, string key, CancellationToken ct)
    {
        if (stage.Parameters is not { } parameters)
            return string.Empty;

        var paramValue = parameters.GetValueOrDefault(key, string.Empty).ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(paramValue))
            return string.Empty;

        if (!paramValue.StartsWith("{{") || !paramValue.EndsWith("}}"))
            return paramValue;

        var placeholderKey = paramValue[2..^2].Trim();
        return await GetVariableValueAsync(sid, stage, placeholderKey, false, ct);
    }
}
