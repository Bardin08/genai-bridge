using System.Text.Json.Serialization;

namespace GenAI.Bridge.Contracts;

/// <summary>
/// Represents a tool that can be used by the model.
/// </summary>
public sealed record Tool
{
    /// <summary>
    /// Gets or sets the type of the tool.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "function";
    
    /// <summary>
    /// Gets or sets the function definition for this tool.
    /// </summary>
    [JsonPropertyName("function")]
    public FunctionDefinition Function { get; init; } = new FunctionDefinition();
    
    /// <summary>
    /// Creates a function-based tool.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="description">Function description.</param>
    /// <param name="parameters">Function parameters schema.</param>
    /// <returns>A new tool instance.</returns>
    public static Tool FunctionTool(string name, string description, object parameters) => 
        new() 
        { 
            Type = "function",
            Function = new FunctionDefinition 
            { 
                Name = name,
                Description = description,
                Parameters = parameters
            }
        };
}
