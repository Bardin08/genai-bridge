using GenAI.Bridge.Contracts;
using GenAI.Bridge.Contracts.Configuration;

namespace GenAI.Bridge.Scenarios.Models;

/// <summary>
/// Represents a stage in a scenario definition.
/// </summary>
public sealed record ScenarioStageDefinition
{
    /// <summary>
    /// The name of this stage.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// The system prompt for this stage.
    /// </summary>
    public string SystemPrompt { get; init; } = string.Empty;

    /// <summary>
    /// The user prompt template for this stage.
    /// </summary>
    public List<UserPromptDefinition> UserPrompts { get; init; } = [];

    /// <summary>
    /// Optional temperature setting for this stage.
    /// </summary>
    public float? Temperature { get; init; }

    /// <summary>
    /// Optional top-p setting for this stage.
    /// </summary>
    public float? TopP { get; init; }

    /// <summary>
    /// Maximum tokens to generate for this stage.
    /// </summary>
    public int? MaxTokens { get; init; }
    
    /// <summary>
    /// Model to use for this stage. Default is null, which means the model will be resolved by model router.
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    /// Functions configuration for this stage.
    /// </summary>
    public FunctionsDefinition? Functions { get; init; }

    /// <summary>
    /// Tools configuration for this stage.
    /// </summary>
    public List<ToolDefinition>? Tools { get; init; }

    /// <summary>
    /// Additional parameters for this stage.
    /// </summary>
    public Dictionary<string, object> Parameters { get; init; } = [];
}