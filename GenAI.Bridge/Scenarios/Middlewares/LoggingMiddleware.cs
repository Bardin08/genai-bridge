using GenAI.Bridge.Utils;
using Microsoft.Extensions.Logging;

namespace GenAI.Bridge.Scenarios.Middlewares;

public sealed class LoggingMiddleware(ILogger<LoggingMiddleware>? log = null) : IStageMiddleware
{
    private readonly ILogger<LoggingMiddleware>? _log = log;

    public async Task InvokeAsync(StageExecutionContext ctx, Func<Task> next, CancellationToken ct)
    {
        _log?.LogInformation("[Session:{S}] Stage {St} started", ctx.SessionId, ctx.Stage.Name);
        var sw = ValueStopwatch.StartNew();

        await next();

        _log?.LogInformation("[Session:{S}] Stage {St} finished in {Ms} ms", ctx.SessionId, ctx.Stage.Name,
            sw.GetElapsedTime().TotalMilliseconds);
    }
}