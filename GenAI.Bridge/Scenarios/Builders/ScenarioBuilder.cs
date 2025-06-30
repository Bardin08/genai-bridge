using System.Text.Json;
using GenAI.Bridge.Contracts.Configuration;
using GenAI.Bridge.Contracts.Prompts;
using GenAI.Bridge.Contracts.Scenarios;
using GenAI.Bridge.Scenarios.Models;
using GenAI.Bridge.Utils;

namespace GenAI.Bridge.Scenarios.Builders;

internal static class ScenarioBuilder
{
    
    public static ScenarioPrompt ConvertToScenarioPrompt(ScenarioDefinition definition)
    {
        var stages = definition.Stages.Select(ScenarioBuilder.ConstructStage).ToList();

        return new ScenarioPrompt(
            Name: definition.Name,
            Stages: stages,
            Metadata: new Dictionary<string, string>(definition.Metadata)
            {
                ["version"] = definition.Version,
                ["description"] = definition.Description,
                ["valid_models"] = string.Join(",", definition.ValidModels)
            }
        );
    }

    #region User Prompt Construction

    private static PromptTurn ConstructUserPrompt(ScenarioStageDefinition stageDef, UserPromptDefinition promptDef)
    {
        var parameters = ConstructUserPromptParameters(stageDef, promptDef);

        var promptId = Guid.NewGuid().ToString("N")[..8];
        var userPromptName = $"user_prompt_{stageDef.Name}_{promptId}";

        return PromptTurn.User(promptDef.Template, userPromptName, parameters);
    }

    private static Dictionary<string, object> ConstructUserPromptParameters(ScenarioStageDefinition stageDef,
        UserPromptDefinition promptDef)
    {
        var parameters = new Dictionary<string, object>(promptDef.Parameters);

        if (!promptDef.Temperature.HasValue)
            parameters["temperature"] = stageDef.Temperature ?? 1.0f;

        if (!promptDef.TopP.HasValue)
            parameters["top_p"] = stageDef.TopP ?? 1.0f;

        if (!promptDef.MaxTokens.HasValue)
            parameters["max_tokens"] = stageDef.MaxTokens ?? 1000;

        var responseFormat = GetResponseFormat(promptDef.ResponseFormatConfig);
        parameters["response_format"] = responseFormat;
        return parameters;
    }

    private static ResponseFormat GetResponseFormat(ResponseFormatConfig responseFormatConfig)
    {
        return responseFormatConfig.Type switch
        {
            ResponseFormatType.Text => ResponseFormat.Text(),
            ResponseFormatType.JsonObject => ResponseFormat.Json(),
            ResponseFormatType.JsonSchema => GetResponseJsonSchema(responseFormatConfig),
            _ => ResponseFormat.Text()
        };
    }

    private static ResponseFormat GetResponseJsonSchema(ResponseFormatConfig responseFormatConfig)
    {
        ResponseFormat? responseFormat;

        if (!string.IsNullOrWhiteSpace(responseFormatConfig.ResponseTypeName))
        {
            var schema = Utils.TypeResolver.GenerateSchemaFromTypeName(responseFormatConfig.ResponseTypeName);
            responseFormat = schema != null
                ? ResponseFormat.JsonWithSchema(schema)
                : ResponseFormat.Json();

            // _logger?.LogInformation("Generated schema from type: {Type}", responseFormatConfig.ResponseTypeName);
        }
        else if (!string.IsNullOrWhiteSpace(responseFormatConfig.Schema))
        {
            responseFormat = ResponseFormat.JsonWithSchema(responseFormatConfig.Schema);
        }
        else
        {
            throw new ArgumentException("No response schema specified. Reference a C# type or provide a JSON schema.");
        }

        return responseFormat;
    }

    #endregion

    #region Scenario Stage Construction

    public static ScenarioStage ConstructStage(ScenarioStageDefinition stageDef)
    {
        var turns = new List<PromptTurn>();

        ArgumentException.ThrowIfNullOrEmpty(stageDef.Name);

        if (!string.IsNullOrWhiteSpace(stageDef.SystemPrompt))
        {
            turns.Add(PromptTurn.System(stageDef.SystemPrompt));
        }

        var stageParameters = ConstructStageParameters(stageDef);

        var userPrompts = stageDef.UserPrompts
            .Select(promptDef => ScenarioBuilder.ConstructUserPrompt(stageDef, promptDef))
            .ToList();

        turns.AddRange(userPrompts);

        var stage = new ScenarioStage(
            Name: stageDef.Name,
            Turns: turns,
            Model: stageDef.Model,
            Parameters: stageParameters
        );
        return stage;
    }

    private static Dictionary<string, object> ConstructStageParameters(ScenarioStageDefinition defStage)
    {
        var stageParameters = new Dictionary<string, object>(defStage.Parameters);

        if (defStage.Temperature.HasValue) stageParameters["temperature"] = defStage.Temperature.Value;

        if (defStage.TopP.HasValue) stageParameters["top_p"] = defStage.TopP.Value;

        if (defStage.MaxTokens.HasValue) stageParameters["max_tokens"] = defStage.MaxTokens.Value;

        var stageFunctions = ConstructStageFunctions(defStage);
        if (stageFunctions != null) stageParameters["functions"] = stageFunctions;

        var stageTools = ConstructStageTools(defStage);
        if (stageTools is { Count: > 0 }) stageParameters["tools"] = stageTools;

        return stageParameters;
    }

    private static FunctionsConfig? ConstructStageFunctions(ScenarioStageDefinition stageDef)
    {
        if (stageDef.Functions is not { Functions.Count: > 0 })
            return null;

        var functions = stageDef.Functions.Functions.Select(BuildFunction).ToList();

        var functionCall = stageDef.Functions.FunctionCall switch
        {
            null => FunctionCall.Auto(),
            "auto" => FunctionCall.Auto(),
            _ => FunctionCall.Specific(stageDef.Functions.FunctionCall)
        };

        return new FunctionsConfig
        {
            Functions = functions,
            FunctionCall = functionCall
        };
    }

    private static List<Tool>? ConstructStageTools(ScenarioStageDefinition stageDef)
    {
        return stageDef.Tools is not { Count: > 0 } ? null : stageDef.Tools.Select(BuildTool).ToList();
    }

    private static Tool BuildTool(ToolDefinition tool)
    {
        if (tool.Type is "function")
        {
            var toolFunctionDefinition = BuildFunction(tool.Function);
            return new Tool
            {
                Type = tool.Type,
                Function = toolFunctionDefinition
            };
        }

        // _logger?.LogWarning("Unsupported tool type: {Type}. Only 'function' type is supported.", tool.Type);
        throw new NotSupportedException($"Unsupported tool type: {tool.Type}");
    }

    private static FunctionDefinition BuildFunction(FunctionDefinitionConfig funcDef)
    {
        object parametersObject = new { };
        try
        {
            var parametersSchema = GenerateFunctionParamsSchema(funcDef);
            parametersObject = JsonDocument.Parse(parametersSchema).RootElement;
        }
        catch (Exception)
        {
            // _logger?.LogWarning(ex, "Failed to parse function parameters JSON for function: {Name}",
            //     funcDef.Name);
        }

        return new FunctionDefinition
        {
            Name = funcDef.Name,
            Description = funcDef.Description,
            Parameters = parametersObject
        };
    }

    private static string GenerateFunctionParamsSchema(FunctionDefinitionConfig funcDef)
    {
        var isGeneratedFromType = !string.IsNullOrEmpty(funcDef.ParametersType) &&
                                  TypeResolver.ResolveType(funcDef.ParametersType) is not null;

        if (!isGeneratedFromType)
            return funcDef.Parameters;

        var generatedSchema = TypeResolver.GenerateSchemaFromTypeName(funcDef.ParametersType!);
        if (!string.IsNullOrEmpty(generatedSchema))
        {
            // _logger?.LogInformation("Generated function parameters schema from type: {Type}", funcDef.ParametersType);
            return generatedSchema;
        }

        // _logger?.LogWarning("Failed to generate function parameters schema from type: {Type}",
            // funcDef.ParametersType);

        return funcDef.Parameters;
    }

    #endregion
}