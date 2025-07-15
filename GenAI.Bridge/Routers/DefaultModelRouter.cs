using GenAI.Bridge.Abstractions;
using GenAI.Bridge.Contracts.Prompts;
using GenAI.Bridge.Contracts.Scenarios;

namespace GenAI.Bridge.Routers;

/// <summary>
/// Default implementation of the model router that uses a simple hierarchy for model resolution:
/// 1. Stage-specific model
/// 2. Scenario's validModels (first one)
/// 3. Throws exception if no model found
/// </summary>
public class DefaultModelRouter(string? defaultModel = null) : IModelRouter
{
    private readonly string? _defaultModel = defaultModel;

    /// <inheritdoc />
    public string ResolveModelFor(ScenarioPrompt scenario, IDictionary<string, object> context)
    {
        var hasPrevStageKey = context.TryGetValue("currentStage", out var currentStageObject);
        if (!hasPrevStageKey || currentStageObject is not ScenarioStage currentStage)
        {
            throw new ArgumentException("Context must contain 'currentStage' key with an integer value representing the stage index.");
        }

        if (!string.IsNullOrEmpty(currentStage.Model))
            return currentStage.Model;

        if (scenario.ValidModels?.Count > 0)
            return scenario.ValidModels[0];

        if (!string.IsNullOrEmpty(_defaultModel))
            return _defaultModel;

        if (scenario.Metadata != null &&
            scenario.Metadata.TryGetValue("model", out var modelObj) &&
            !string.IsNullOrEmpty(modelObj))
            return modelObj;

        throw new InvalidOperationException($"No model defined for {currentStage.Name} of scenario {scenario.Name}");
    }

    /// <inheritdoc />
    public string ResolveModelFor(CompletionPrompt prompt, IDictionary<string, object> context)
    {
        if (prompt.Metadata != null && 
            prompt.Metadata.TryGetValue("model", out var modelObj) && 
            modelObj is string modelName && 
            !string.IsNullOrEmpty(modelName))
            return modelName;

        if (context.TryGetValue("model", out var contextModelObj) && 
            contextModelObj is string contextModel && 
            !string.IsNullOrEmpty(contextModel))
            return contextModel;

        if (!string.IsNullOrEmpty(_defaultModel))
            return _defaultModel;

        throw new InvalidOperationException("No model defined for completion prompt");
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetAvailableModels(ScenarioPrompt scenario)
        => scenario.ValidModels ?? [];

    /// <inheritdoc />
    public IReadOnlyList<string> GetAvailableModels(CompletionPrompt prompt)
    {
        if (prompt.Metadata != null && 
            prompt.Metadata.TryGetValue("availableModels", out var modelsObj) && 
            modelsObj is IReadOnlyList<string> models)
            return models;

        return !string.IsNullOrEmpty(_defaultModel) 
            ? new[] { _defaultModel } 
            : Array.Empty<string>();
    }
}
