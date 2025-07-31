namespace GenAI.Bridge.Scenarios.Middlewares;

public interface IStageMiddleware
{
    Task InvokeAsync(StageExecutionContext ctx, Func<Task> next, CancellationToken ct);
}