using GenAI.Bridge.Contracts.Results;
using GenAI.Bridge.Contracts.Scenarios;

namespace GenAI.Bridge.Scenarios;

public sealed class StageExecutionContext
{
    public required string SessionId { get; init; }
    public required ScenarioStage Stage { get; set; }
    public required IDictionary<string, object> Metadata { get; init; }
    public List<CompletionResult> Results { get; } = [];
}