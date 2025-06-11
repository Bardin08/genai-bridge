namespace GenAI.Bridge.Contracts;

/// <summary>
/// Represents a scenario-driven prompt, for multi-stage or guided workflows.
/// </summary>
public sealed record ScenarioPrompt(
    string ScenarioName,
    CompletionPrompt InitialPrompt,
    IReadOnlyDictionary<string, object>? Context = null);