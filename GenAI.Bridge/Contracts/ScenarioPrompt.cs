namespace GenAI.Bridge.Contracts;

/// <summary>
/// Represents a scenario-driven prompt for multi-stage or guided workflows.
/// </summary>
public sealed record ScenarioPrompt(
    string Name,
    IReadOnlyList<PromptTurn> Turns,
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
    public IEnumerable<string>? ValidModels =>
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
    /// Creates a new scenario prompt with a single system and user turn.
    /// </summary>
    /// <param name="name">The name of the scenario.</param>
    /// <param name="systemPrompt">The system prompt content.</param>
    /// <param name="userPrompt">The user prompt content.</param>
    /// <param name="parameters">Optional parameters for the scenario.</param>
    /// <param name="metadata">Optional metadata for the scenario.</param>
    /// <returns>A new ScenarioPrompt instance.</returns>
    public static ScenarioPrompt Create(
        string name,
        string systemPrompt,
        string userPrompt,
        IReadOnlyDictionary<string, object>? parameters = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        var turns = new List<PromptTurn>
        {
            PromptTurn.System(systemPrompt),
            PromptTurn.User(userPrompt, parameters)
        };
        
        return new ScenarioPrompt(name, turns, metadata);
    }
}
