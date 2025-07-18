namespace GenAI.Bridge.Contracts;

/// <summary>
/// Represents a stage within a scenario definition.
/// </summary>
public sealed record ScenarioStage
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
    public string UserPromptTemplate { get; init; } = string.Empty;
    
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
    /// Response format configuration.
    /// </summary>
    public ResponseFormatConfig? ResponseFormat { get; init; }
    
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