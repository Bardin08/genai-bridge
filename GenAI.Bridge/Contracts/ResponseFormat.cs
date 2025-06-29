using System.Text.Json.Serialization;
using GenAI.Bridge.Utils;

namespace GenAI.Bridge.Contracts;

/// <summary>
/// Defines the response format types available in OpenAI API.
/// </summary>
public enum ResponseFormatType
{
    /// <summary>
    /// Default response format (text)
    /// </summary>
    Text,

    /// <summary>
    /// JSON object response format
    /// </summary>
    JsonObject,
    
    /// <summary>
    /// Json schema response format
    /// </summary>
    JsonSchema
}

/// <summary>
/// Represents the response format configuration for an API request.
/// </summary>
public sealed record ResponseFormat
{
    /// <summary>
    /// Gets the type of response format.
    /// </summary>
    [JsonPropertyName("type")]
    public ResponseFormatType Type { get; init; }

    /// <summary>
    /// Gets or sets the schema for JSON responses as a string, if applicable..
    /// </summary>
    [JsonPropertyName("schema")]
    public string? Schema { get; init; }

    /// <summary>
    /// Gets or sets the C# type name to generate schema from.
    /// </summary>
    [JsonPropertyName("schema_type")]
    public string? SchemaType { get; init; }

    /// <summary>
    /// Creates a new text response format.
    /// </summary>
    /// <returns>A response format configured for text output.</returns>
    public static ResponseFormat Text() => new() { Type = ResponseFormatType.Text };

    /// <summary>
    /// Creates a new JSON object response format.
    /// </summary>
    /// <returns>A response format configured for JSON output.</returns>
    public static ResponseFormat Json() => new() { Type = ResponseFormatType.JsonObject };

    /// <summary>
    /// Creates a new JSON object response format with a schema.
    /// </summary>
    /// <param name="schema">The JSON schema as a string.</param>
    /// <returns>A response format configured for JSON output with schema validation.</returns>
    public static ResponseFormat JsonWithSchema(string schema) =>
        new() { Type = ResponseFormatType.JsonSchema, Schema = schema };

    /// <summary>
    /// Creates a new JSON object response format with a schema generated from a C# type.
    /// </summary>
    /// <typeparam name="T">The C# type to generate schema from.</typeparam>
    /// <returns>A response format configured for JSON output with schema validation.</returns>
    public static ResponseFormat JsonFromType<T>() =>
        JsonWithSchema(OpenAiJsonSchemaGenerator.GenerateSchema<T>("response_format_schema_" + typeof(T).Name));
}