using GenAI.Bridge.Contracts;

namespace GenAI.Bridge.Abstractions;

/// <summary>
/// Main integration facade for generative AI workflows.
/// </summary>
public interface IGenAiBridge
{
    Task<CompletionResult> CompleteAsync(CompletionPrompt prompt, CancellationToken ct = default);
    IAsyncEnumerable<ContentChunk> StreamCompletionAsync(CompletionPrompt prompt, CancellationToken ct = default);

    Task<CompletionResult> RunScenarioAsync(ScenarioPrompt prompt, CancellationToken ct = default);

    Task<EmbeddingResult> EmbedAsync(EmbeddingPrompt prompt, CancellationToken ct = default);
}