using GenAI.Bridge.Contracts;

namespace GenAI.Bridge.Abstractions;

/// <summary>
/// Estimates tokens and cost for AI completions or scenarios.
/// <para>CONTRACT: Cost must be accurate to within 1% for known models, and may be approximate for unknown ones.</para>
/// </summary>
public interface ICostEstimator
{
    /// <summary>
    /// Estimates the number of tokens the prompt will consume.
    /// </summary>
    int EstimatePromptTokens(CompletionPrompt prompt);

    /// <summary>
    /// Estimates cost (in USD) for the prompt/model.
    /// </summary>
    decimal EstimateCost(CompletionPrompt prompt, string model);
}