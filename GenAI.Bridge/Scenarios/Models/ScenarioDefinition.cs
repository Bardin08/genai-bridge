namespace GenAI.Bridge.Scenarios.Models;

/// <summary>
/// Represents a scenario definition that can be loaded from a YAML/JSON file.
/// </summary>
public sealed record ScenarioDefinition
{
    /// <summary>
    /// The unique name of the scenario.
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Description of the scenario's purpose and usage.
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// The models this scenario is compatible with.
    /// </summary>
    public List<string> ValidModels { get; init; } = [];
    
    /// <summary>
    /// The ordered stages of the scenario.
    /// </summary>
    public List<ScenarioStageDefinition> Stages { get; init; } = [];
    
    /// <summary>
    /// Optional metadata for the scenario.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();
    
    /// <summary>
    /// Version of the scenario definition.
    /// </summary>
    public string Version { get; init; } = "1.0";
}
