namespace GenAI.Bridge.Contracts;

/// <summary>
/// Represents a completion response from the AI.
/// </summary>
public sealed record CompletionResult(
    string Content,
    IReadOnlyDictionary<string, object>? Metadata = null);