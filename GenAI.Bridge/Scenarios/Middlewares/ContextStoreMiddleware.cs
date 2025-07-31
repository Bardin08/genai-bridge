using System.Text.Json;
using GenAI.Bridge.Context;
using GenAI.Bridge.Contracts.Results;
using GenAI.Bridge.Providers.InMemory;
using GenAI.Bridge.Providers.OpenAI;

namespace GenAI.Bridge.Scenarios.Middlewares;

public sealed class ContextStoreMiddlewareV2(InMemoryContextStore store) : IStageMiddleware
{
    public async Task InvokeAsync(StageExecutionContext ctx, Func<Task> next, CancellationToken ct)
    {
        await next();
        await SaveStageExecResultToContext(ctx);
    }

    private async Task SaveStageExecResultToContext(StageExecutionContext ctx)
    {
        var tasks = new List<Task>();

        for (var turnIndex = 0; turnIndex < ctx.Results.Count; turnIndex++)
        {
            var turnResult = ctx.Results[turnIndex];
            var stageKey = GetStageKey(ctx.Stage.Id, turnIndex);

            tasks.AddRange(GetSaveResultDataTasks(turnResult, stageKey));
        }

        await Task.WhenAll(tasks);
    }

    private List<Task> GetSaveResultDataTasks(CompletionResult result, string stageKey)
    {
        var tasks = new List<Task>();

        tasks.AddRange(GetSavePromptDataTasks(result, stageKey));
        tasks.AddRange(GetSaveTurnParametersTasks(result, stageKey));
        tasks.AddRange(GetSaveExecutionResultTasks(result, stageKey));
        tasks.AddRange(GetSaveResultMetadataTasks(result, stageKey));

        return tasks;
    }

    private List<Task> GetSavePromptDataTasks(CompletionResult result, string stageKey)
    {
        var tasks = new List<Task>();

        if (string.IsNullOrEmpty(result.SystemPrompt))
        {
            var systemPromptKey = ContextKeysBuilder.InputKey(stageKey, "system_prompt");
            tasks.Add(store.SaveItemAsync(result.SessionId, systemPromptKey, result.SystemPrompt,
                ct: CancellationToken.None));
        }

        var userPromptKey = ContextKeysBuilder.InputKey(stageKey, "user_prompt");
        tasks.Add(store.SaveItemAsync(result.SessionId, userPromptKey, result.UserPrompt.Content,
            ct: CancellationToken.None));

        return tasks;
    }

    private List<Task> GetSaveTurnParametersTasks(CompletionResult result, string stageKey)
    {
        var tasks = new List<Task>();

        if (result.Metadata is null)
            return tasks;

        var model = result.Metadata["model"];
        var modelKey = ContextKeysBuilder.MetadataKey(stageKey, "model");
        tasks.Add(store.SaveItemAsync(result.SessionId, modelKey, model, ct: CancellationToken.None));

        if (model is "unknown")
        {
            var errorLogKey = ContextKeysBuilder.OutputLogKey(stageKey, "error");
            tasks.Add(store.SaveItemAsync(result.SessionId, errorLogKey, model, ct: CancellationToken.None));
        }

        tasks.AddRange(result.Metadata
            .Select(param => new { param, paramKey = ContextKeysBuilder.InputParamsKey(stageKey, param.Key) })
            .Select(t =>
                store.SaveItemAsync(result.SessionId, t.paramKey, t.param.Value, ct: CancellationToken.None)));

        return tasks;
    }

    private List<Task> GetSaveExecutionResultTasks(CompletionResult result, string stageKey)
    {
        var tasks = new List<Task>();

        // Save execution result content
        var resultContent = result.Content;
        var resultOutputKey = ContextKeysBuilder.OutputKey(stageKey);
        tasks.Add(store.SaveItemAsync(result.SessionId, resultOutputKey, resultContent, ct: CancellationToken.None));

        // Save execution ID
        var execId = result.Metadata?[MetadataKeys.Id].ToString() ?? stageKey;
        var execResultKey = ContextKeysBuilder.OutputParamKey(stageKey, "execution_id");
        tasks.Add(store.SaveItemAsync(result.SessionId, execResultKey, execId, ct: CancellationToken.None));

        return tasks;
    }

    private List<Task> GetSaveResultMetadataTasks(CompletionResult result, string stageKey)
    {
        if (result.Metadata is null || result.Metadata.Count == 0)
            return [];

        var tasks = new List<Task>();

        tasks.AddRange(GetSaveModelMetadataTasks(result, stageKey));
        tasks.AddRange(GetSaveToolCallsTasks(result, stageKey));
        tasks.AddRange(GetSaveTokenMetadataTasks(result, stageKey));

        return tasks;
    }

    private List<Task> GetSaveModelMetadataTasks(CompletionResult result, string stageKey)
    {
        if (result.Metadata is null || result.Metadata.Count == 0)
            return [];

        var tasks = new List<Task>();

        var outputModel = result.Metadata[MetadataKeys.Model].ToString() ?? "unknown";
        var modelMetadataKey = ContextKeysBuilder.MetadataKey(stageKey, "output_model");
        tasks.Add(store.SaveItemAsync(result.SessionId, modelMetadataKey, outputModel, ct: CancellationToken.None));

        if (result.Metadata[MetadataKeys.FinishReason] is not string finishReasonStr)
            return tasks;

        var finishReasonKey = ContextKeysBuilder.MetadataKey(stageKey, "finish_reason");
        tasks.Add(store.SaveItemAsync(result.SessionId, finishReasonKey, finishReasonStr,
            ct: CancellationToken.None));

        return tasks;
    }

    private List<Task> GetSaveToolCallsTasks(CompletionResult result, string stageKey)
    {
        if (result.Metadata is null ||
            result.Metadata.Count == 0 ||
            result.Metadata[MetadataKeys.ToolCalls] is not IEnumerable<ToolCallAudit> toolCalls)
            return [];

        var tasks = new List<Task>();
        var saveToolCallsTasks = toolCalls
            .Select(x => new
            {
                toolCall = x,
                toolCallKey = ContextKeysBuilder.ToolCallKey(stageKey, x.FunctionName, x.Id),
                funcJsonStr = JsonSerializer.Serialize(x, Constants.Json.DefaultSettings)
            })
            .Select(x =>
                store.SaveItemAsync(result.SessionId, x.toolCallKey, x.funcJsonStr, ct: CancellationToken.None));

        tasks.AddRange(saveToolCallsTasks);
        return tasks;
    }

    private List<Task> GetSaveTokenMetadataTasks(CompletionResult result, string stageKey)
    {
        if (result.Metadata is null || result.Metadata.Count == 0)
            return [];

        var tasks = new List<Task>();

        var tokenMetadata = new[]
        {
            (MetadataKeys.InputTokens, "input_tokens"),
            (MetadataKeys.OutputTokens, "output_tokens"),
            (MetadataKeys.TotalTokens, "total_tokens")
        };

        foreach (var (metadataKey, keyName) in tokenMetadata)
        {
            if (result.Metadata[metadataKey] is not int tokenCount)
                continue;

            var tokenKey = ContextKeysBuilder.MetadataKey(stageKey, keyName);
            tasks.Add(store.SaveItemAsync(result.SessionId, tokenKey, tokenCount, ct: CancellationToken.None));
        }

        return tasks;
    }

    private static string GetStageKey(int stageId, int turnIndex) => $"{stageId}-{turnIndex + 1}";
}
