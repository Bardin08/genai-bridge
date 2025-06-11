using GenAI.Bridge.Contracts;

namespace GenAI.Bridge.Abstractions;

/// <summary>
/// Facade for GenAI workflows: completions, scenarios, embeddings.
/// All operations are asynchronous and cancellation-aware.
/// </summary>
public interface IGenAiBridge
{
    /// <summary>
    /// Requests a single-shot or multi-turn completion from an LLM.
    /// <para>CONTRACT: Must return a valid <see cref="CompletionResult"/> for any well-formed prompt.
    /// Throws ArgumentException for invalid prompts.</para>
    /// </summary>
    Task<CompletionResult> CompleteAsync(
        CompletionPrompt prompt,
        CancellationToken ct = default);

    /// <summary>
    /// Streams completion results as chunks, supporting large or incremental output.
    /// <para>CONTRACT: Chunks must arrive in order (by Index), and form the full response when concatenated.</para>
    /// </summary>
    IAsyncEnumerable<ContentChunk> StreamCompletionAsync(
        CompletionPrompt prompt,
        CancellationToken ct = default);

    /// <summary>
    /// Runs a scenario-driven multi-stage prompt (e.g., guided plans, document generation).
    /// <para>CONTRACT: Implementations may invoke multiple models/providers as defined in the scenario.</para>
    /// </summary>
    Task<CompletionResult> RunScenarioAsync(
        ScenarioPrompt prompt,
        CancellationToken ct = default);

    /// <summary>
    /// Requests embedding/vectorization of a resource (text, file, etc).
    /// <para>CONTRACT: EmbeddingResult.Vector must always be non-null, even if empty.</para>
    /// </summary>
    Task<EmbeddingResult> EmbedAsync(
        EmbeddingPrompt prompt,
        CancellationToken ct = default);
}