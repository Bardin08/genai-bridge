using GenAI.Bridge.Abstractions;

namespace GenAI.Bridge.Scenarios.Middlewares;

public sealed class LlmRequestMiddleware(ILlmAdapter llmAdapter) : IStageMiddleware
{
    public async Task InvokeAsync(StageExecutionContext ctx, Func<Task> next, CancellationToken ct)
    {
        var completionPrompts = ctx.Stage.ToCompletionPrompts(ctx.SessionId, ctx.Metadata);

        foreach (var completionPrompt in completionPrompts)
        {
            var completionResult = await llmAdapter
                .CompleteAsync(ctx.Stage.Model!, completionPrompt, ct: ct);

            ctx.Results.Add(completionResult);
        }

        await next();
    }
}