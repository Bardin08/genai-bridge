using GenAI.Bridge.Contracts;

namespace GenAI.Bridge.Abstractions;

/// <summary>
/// Produces vector embeddings for arbitrary resources.
/// </summary>
public interface IEmbeddingAdapter
{
    Task<EmbeddingResult> GetEmbeddingAsync(EmbeddingPrompt prompt, CancellationToken ct = default);
}