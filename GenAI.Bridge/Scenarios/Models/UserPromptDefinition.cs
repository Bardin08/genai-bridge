using GenAI.Bridge.Contracts;
using GenAI.Bridge.Contracts.Configuration;

namespace GenAI.Bridge.Scenarios.Models;

/// <summary>
/// Represents a user prompt definition for a scenario stage.
/// </summary>
public sealed record UserPromptDefinition
{
    /// <summary>
    /// The user prompt template for this stage.
    /// </summary>
    public string Template { get; init; } = string.Empty;

    /// <summary>
    /// Optional parameters for the user prompt.
    /// </summary>
    public Dictionary<string, object> Parameters { get; init; } = [];

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
    /// Represents response format configuration from the LLM model. Use to configure structured output.
    /// </summary>
    public ResponseFormatConfig ResponseFormatConfig { get; set; } = new();
}   