namespace GenAI.Bridge.Abstractions;

/// <summary>
/// Storage/retrieval of vectors (RAG, semantic search, etc).
/// </summary>
public interface IVectorStore
{
    /// Store or update a vector associated with a consumer-defined external ID.
    Task UpsertAsync(string externalId, IReadOnlyList<float> vector, CancellationToken ct = default);

    /// Search for the most similar vectors by a query embedding.
    Task<IReadOnlyList<(string ExternalId, float Score)>> SearchAsync(
        IReadOnlyList<float> queryVector, int topK, CancellationToken ct = default);
}