using GenAI.Bridge.Contracts;
using GenAI.Bridge.Contracts.Prompts;
using GenAI.Bridge.Contracts.Results;

namespace GenAI.Bridge.Abstractions;

/// <summary>
/// Produces vector embeddings for arbitrary resources.
/// <para>CONTRACT: Returned embedding must be compatible with the consumer's vector store.</para>
/// </summary>
public interface IEmbeddingAdapter
{
    /// <summary>
    /// Computes an embedding/vector representation for the given input.
    /// Throws ArgumentException for unsupported input types.
    /// </summary>
    Task<EmbeddingResult> GetEmbeddingAsync(
        EmbeddingPrompt prompt,
        CancellationToken ct = default);
}