using GenAI.Bridge.Contracts.Configuration;

namespace GenAI.Bridge.Scenarios.Models;

/// <summary>
/// Represents a stage in a scenario definition.
/// </summary>
public sealed record ScenarioStageDefinition
{
    /// <summary>
    /// The ID of the stage. Used to refer to stage at the definition
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The name of this stage.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// The description of this stage.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The system prompt for this stage.
    /// </summary>
    public string SystemPrompt { get; init; } = string.Empty;

    /// <summary>
    /// The user prompt template for this stage.
    /// </summary>
    public List<UserPromptDefinition> UserPrompts { get; init; } = [];

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