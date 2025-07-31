namespace GenAI.Bridge.Contracts.Configuration;

/// <summary>
/// Configuration for response format in a scenario stage.
/// </summary>
public sealed record ResponseFormatConfig
{
    /// <summary>
    /// Gets or sets the type of response format.
    /// </summary>
    public ResponseFormatType Type { get; init; } = ResponseFormatType.Text;
    
    /// <summary>
    /// Gets or sets the schema for JSON responses as a string, if applicable.
    /// </summary>
    public string? Schema { get; init; }
    
    /// <summary>
    /// Gets or sets the C# type name to generate schema from.
    /// </summary>
    public string? ResponseTypeName { get; init; }
}