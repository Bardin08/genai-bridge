using GenAI.Bridge.Contracts;
using GenAI.Bridge.Contracts.Prompts;
using GenAI.Bridge.Contracts.Results;

namespace GenAI.Bridge.Abstractions;

/// <summary>
/// Interface for interacting with LLM providers.
/// </summary>
public interface ILlmAdapter
{
    /// <summary>
    /// Gets the set of model names supported by this adapter.
    /// </summary>
    IReadOnlySet<string> SupportedModels { get; }

    /// <summary>
    /// Sends a completion request to the LLM provider.
    /// </summary>
    /// <param name="model">The model name to use for completion.</param>
    /// <param name="prompt">The completion prompt to send.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A completion result containing the LLM's response.</returns>
    /// <exception cref="NotSupportedException">Thrown when the specified model is not supported.</exception>
    Task<CompletionResult> CompleteAsync(
        string model, 
        CompletionPrompt prompt, 
        CancellationToken ct = default);
}
