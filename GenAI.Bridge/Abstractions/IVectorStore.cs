namespace GenAI.Bridge.Abstractions;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Storage and retrieval abstraction for vector embeddings (semantic search, RAG, etc).
/// <para>CONTRACT: All methods are idempotent and must not throw for missing keys (return empty instead).</para>
/// </summary>
public interface IVectorStore
{
    /// <summary>
    /// Inserts or updates a vector for a specific external resource ID.
    /// </summary>
    Task UpsertAsync(
        Guid externalId,
        IReadOnlyList<float> vector,
        Dictionary<string, string> payload,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the most similar vectors (by cosine similarity or equivalent), ordered by score descending.
    /// </summary>
    Task<IReadOnlyList<(Guid ExternalId, float Score, IReadOnlyList<float> Vector)>> SearchAsync(
        IReadOnlyList<float> queryVector,
        uint topK,
        CancellationToken ct = default);
}