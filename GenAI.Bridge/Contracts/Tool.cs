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
    public required FunctionDefinition Function { get; init; } = new();
}
