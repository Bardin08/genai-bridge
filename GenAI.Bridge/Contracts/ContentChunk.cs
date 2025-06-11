namespace GenAI.Bridge.Contracts;

/// <summary>
/// Represents a streamed chunk of content (for streaming output APIs).
/// </summary>
public sealed record ContentChunk(
    int Index,
    string Content);