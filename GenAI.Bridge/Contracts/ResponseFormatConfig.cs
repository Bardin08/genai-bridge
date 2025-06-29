using YamlDotNet.Serialization;

namespace GenAI.Bridge.Contracts;

/// <summary>
/// Configuration for response format in a scenario stage.
/// </summary>
public sealed record ResponseFormatConfig
{
    /// <summary>
    /// Gets or sets the type of response format.
    /// </summary>
    public string Type { get; init; } = "text";
    
    /// <summary>
    /// Gets or sets the schema for JSON responses as a string, if applicable.
    /// </summary>
    [YamlMember(Alias = "schema")]
    public string? Schema { get; init; }
    
    /// <summary>
    /// Gets or sets the C# type name to generate schema from.
    /// </summary>
    [YamlMember(Alias = "schema_type")]
    public string? SchemaType { get; init; }
}