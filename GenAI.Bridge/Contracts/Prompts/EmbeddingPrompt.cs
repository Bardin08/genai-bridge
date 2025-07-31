namespace GenAI.Bridge.Contracts.Prompts;

/// <summary>
/// Represents a request to embed a resource (text, file, etc.).
/// </summary>
/// <param name="Metadata">
/// This field is not used by the library itself and may be used for any relevant data (cost, source, user IDs, etc).
/// </param>
public sealed record EmbeddingPrompt(
    string InputType, // e.g., "text", "file"
    string Content,   // or a file path/id
    IReadOnlyDictionary<string, object>? Metadata = null);