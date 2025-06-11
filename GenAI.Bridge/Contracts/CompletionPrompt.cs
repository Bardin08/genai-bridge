namespace GenAI.Bridge.Contracts;

/// <summary>
/// Represents a general-purpose prompt for AI completions.
/// </summary>
/// <param name="Metadata">
/// This field is not used by the library itself and may be used for any relevant data (cost, source, user IDs, etc).
/// </param>
public sealed record CompletionPrompt(
    string? SystemMessage,
    IReadOnlyList<PromptTurn> Turns,
    IReadOnlyDictionary<string, object>? Metadata = null);