namespace GenAI.Bridge.Scenarios.Middlewares;

internal sealed class MiddlewarePipeline(IEnumerable<IStageMiddleware> mw)
{
    private readonly List<IStageMiddleware> _middlewares = mw.ToList();

    public Task ExecuteAsync(StageExecutionContext ctx, CancellationToken ct)
    {
        return Step(0);

        Task Step(int i) => i == _middlewares.Count
            ? Task.CompletedTask
            : _middlewares[i].InvokeAsync(ctx, () => Step(i + 1), ct);
    }
}