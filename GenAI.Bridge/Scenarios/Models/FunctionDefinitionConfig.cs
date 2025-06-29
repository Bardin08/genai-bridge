namespace GenAI.Bridge.Scenarios.Models;

/// <summary>
/// Function definition for scenario stages.
/// </summary>
public sealed record FunctionDefinitionConfig
{
    /// <summary>
    /// Gets or sets the name of the function.
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the description of the function.
    /// </summary>
    public string? Description { get; init; }
    
    /// <summary>
    /// Gets or sets the parameters schema for the function as a string.
    /// </summary>
    public string Parameters { get; init; } = "{}";
    
    /// <summary>
    /// Gets or sets the C# type name for generating the parameters schema.
    /// </summary>
    public string? ParametersType { get; init; }
}