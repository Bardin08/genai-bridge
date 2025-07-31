using GenAI.Bridge.Contracts.Results;
using GenAI.Bridge.Contracts.Scenarios;
using GenAI.Bridge.Scenarios.Middlewares;

namespace GenAI.Bridge.Scenarios;

public sealed class ScenarioOrchestrator(
    IScenarioRegistry scenarioRegistry,
    IEnumerable<IStageMiddleware> middlewaresPipeline)
    : IScenarioOrchestrator
{
    private readonly IScenarioRegistry _scenarioRegistry = scenarioRegistry;
    private readonly MiddlewarePipeline _pipe = new(middlewaresPipeline);

    public async Task<IReadOnlyList<List<CompletionResult>>> ExecuteScenarioAsync(
        string sessionId, string scenarioName, CancellationToken ct = default)
    {
        var scenario = await _scenarioRegistry.GetScenario(scenarioName);
        var metadata = new Dictionary<string, object>();
        var list = new List<List<CompletionResult>>();

        foreach (var stage in scenario.Stages)
        {
            var stageCompletionResults = await RunStageAsync(sessionId, stage, metadata, ct);
            list.Add(stageCompletionResults);
        }

        return list;
    }

    public async Task<List<CompletionResult>> ExecuteStageAsync(
        string sessionId, string scenarioName, int stageId, CancellationToken ct = default)
    {
        var scenario = await _scenarioRegistry.GetScenario(scenarioName);
        var st = scenario.FindStage(stageId) ?? throw new ArgumentException($"Stage '{stageId}' not found");
        return await RunStageAsync(sessionId, st, new Dictionary<string, object>(), ct);
    }

    private async Task<List<CompletionResult>> RunStageAsync(
        string sessionId, ScenarioStage stage, Dictionary<string, object> metadata, CancellationToken ct)
    {
        var ctx = new StageExecutionContext
        {
            SessionId = sessionId,
            Stage = stage,
            Metadata = metadata
        };

        await _pipe.ExecuteAsync(ctx, ct);

        return ctx.Results;
    }
}