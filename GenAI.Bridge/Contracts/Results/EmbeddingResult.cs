namespace GenAI.Bridge.Contracts.Results;

/// <summary>
/// Represents the result of embedding a resource for search/retrieval.
/// </summary>
/// <param name="Metadata">
/// This field is not used by the library itself and may be used for any relevant data (cost, source, user IDs, etc).
/// </param>
public sealed record EmbeddingResult(
    IReadOnlyList<float> Vector,
    int TokenCount,
    string? Model = null,
    IReadOnlyDictionary<string, object>? Metadata = null);