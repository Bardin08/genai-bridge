using GenAI.Bridge.Contracts.Prompts;

namespace GenAI.Bridge.Contracts.Results;

/// <summary>
/// Represents a completion response from the AI.
/// </summary>
public sealed record CompletionResult(
    string SessionId,
    string Content,
    string? SystemPrompt,
    PromptTurn UserPrompt,
    IReadOnlyDictionary<string, object>? Metadata = null);