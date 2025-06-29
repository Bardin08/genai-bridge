namespace GenAI.Bridge.Contracts;

/// <summary>
/// Represents a scenario-driven prompt for multi-stage or guided workflows.
/// </summary>
public sealed record ScenarioPrompt(
    string Name,
    IReadOnlyList<ScenarioStage> Stages,
    IReadOnlyDictionary<string, string>? Metadata = null)
{
    /// <summary>
    /// Gets the version of the scenario prompt.
    /// </summary>
    public string? Version => Metadata?.TryGetValue("version", out var version) == true ? version : null;
    
    /// <summary>
    /// Gets the description of the scenario prompt.
    /// </summary>
    public string? Description => Metadata?.TryGetValue("description", out var description) == true ? description : null;
    
    /// <summary>
    /// Gets the list of valid models for this scenario.
    /// </summary>
    public IReadOnlyList<string>? ValidModels =>
        Metadata?.TryGetValue("valid_models", out var modelsStr) == true
            ? modelsStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : null;
            
    /// <summary>
    /// Gets the category of the scenario.
    /// </summary>
    public string? Category => Metadata?.TryGetValue("category", out var category) == true ? category : null;
    
    /// <summary>
    /// Gets the author of the scenario.
    /// </summary>
    public string? Author => Metadata?.TryGetValue("author", out var author) == true ? author : null;

    /// <summary>
    /// Finds a stage by name.
    /// </summary>
    /// <param name="stageName">The name of the stage to find.</param>
    /// <returns>The stage if found, null otherwise.</returns>
    public ScenarioStage? FindStage(string stageName)
    {
        return Stages.FirstOrDefault(s => string.Equals(s.Name, stageName, StringComparison.OrdinalIgnoreCase));
    }
}
