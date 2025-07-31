// filepath: /Users/vbardin/Documents/GitHub/Personal/genai-bridge/GenAI.Bridge/Abstractions/IModelRouter.cs
using GenAI.Bridge.Contracts;
using GenAI.Bridge.Contracts.Prompts;

namespace GenAI.Bridge.Abstractions;

/// <summary>
/// Service that maps scenarios and stages to specific models/providers.
/// </summary>
public interface IModelRouter
{
    /// <summary>
    /// Resolves the appropriate model name (or provider key) for the given scenario and stage.
    /// </summary>
    /// <param name="scenario">The scenario prompt containing metadata about the scenario.</param>
    /// <param name="context">Additional context that may influence model selection.</param>
    /// <returns>The name of the model to use for the given stage.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no suitable model can be found.</exception>
    string ResolveModelFor(ScenarioPrompt scenario, IDictionary<string, object> context);
    
    /// <summary>
    /// Resolves the appropriate model name (or provider key) for a completion prompt.
    /// </summary>
    /// <param name="prompt">The completion prompt to find a model for.</param>
    /// <param name="context">Additional context that may influence model selection.</param>
    /// <returns>The name of the model to use for the completion.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no suitable model can be found.</exception>
    string ResolveModelFor(CompletionPrompt prompt, IDictionary<string, object> context);
    
    /// <summary>
    /// Returns the available models for a given scenario.
    /// </summary>
    /// <param name="scenario">The scenario prompt containing metadata about the scenario.</param>
    /// <returns>A read-only list of available model names.</returns>
    IReadOnlyList<string> GetAvailableModels(ScenarioPrompt scenario);
    
    /// <summary>
    /// Returns the available models for a completion prompt.
    /// </summary>
    /// <param name="prompt">The completion prompt.</param>
    /// <returns>A read-only list of available model names.</returns>
    IReadOnlyList<string> GetAvailableModels(CompletionPrompt prompt);
}
