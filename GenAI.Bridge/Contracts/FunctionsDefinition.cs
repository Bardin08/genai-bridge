using GenAI.Bridge.Scenarios.Models;

namespace GenAI.Bridge.Contracts;

/// <summary>
/// Functions configuration for a scenario stage.
/// </summary>
public sealed record FunctionsDefinition
{
    /// <summary>
    /// Gets or sets the list of function definitions.
    /// </summary>
    public List<FunctionDefinitionConfig> Functions { get; init; } = [];
    
    /// <summary>
    /// Gets or sets the specific function to call, or "auto" to let the model choose.
    /// </summary>
    public string? FunctionCall { get; init; }
}