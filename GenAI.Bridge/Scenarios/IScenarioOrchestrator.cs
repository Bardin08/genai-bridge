using GenAI.Bridge.Contracts.Results;

namespace GenAI.Bridge.Scenarios;

public interface IScenarioOrchestrator
{
    Task<IReadOnlyList<List<CompletionResult>>> ExecuteScenarioAsync(string sessionId, string scenarioName,
        CancellationToken ct = default);

    Task<List<CompletionResult>> ExecuteStageAsync(string sessionId, string scenarioName, int stageId,
        CancellationToken ct = default);
}