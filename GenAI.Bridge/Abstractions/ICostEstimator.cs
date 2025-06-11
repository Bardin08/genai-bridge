using GenAI.Bridge.Contracts;

namespace GenAI.Bridge.Abstractions;

/// <summary>
/// Estimates tokens and cost for AI operations.
/// </summary>
public interface ICostEstimator
{
    int EstimatePromptTokens(CompletionPrompt prompt);
    decimal EstimateCost(CompletionPrompt prompt, string model);
}