namespace GenAI.Bridge.Contracts;

/// <summary>
/// Represents a single turn in a multi-turn prompt.
/// </summary>
public sealed record PromptTurn(
    string Role, // e.g., "user", "assistant", "function"
    string Content);