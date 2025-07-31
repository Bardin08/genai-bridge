using GenAI.Bridge.Scenarios.Models;

namespace GenAI.Bridge.Contracts.Configuration;

/// <summary>
/// Tool definition for scenario stages.
/// </summary>
public sealed record ToolDefinition
{
    /// <summary>
    /// Gets or sets the type of the tool.
    /// </summary>
    public string Type { get; init; } = "function";
    
    /// <summary>
    /// Gets or sets the function definition for this tool.
    /// </summary>
    public FunctionDefinitionConfig Function { get; init; } = new();
}