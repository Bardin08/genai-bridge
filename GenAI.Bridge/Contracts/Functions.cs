using System.Text.Json.Serialization;

namespace GenAI.Bridge.Contracts;

/// <summary>
/// Represents a function definition that can be called by the model.
/// </summary>
public sealed record FunctionDefinition
{
    /// <summary>
    /// Gets or sets the name of the function.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the description of the function.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }
    
    /// <summary>
    /// Gets or sets the parameters schema for the function.
    /// </summary>
    [JsonPropertyName("parameters")]
    public object Parameters { get; init; } = new();
}

/// <summary>
/// Function calling configuration for a completion request.
/// </summary>
public sealed record FunctionCall
{
    /// <summary>
    /// Gets or sets the name of the function to call.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }
    
    /// <summary>
    /// Creates a configuration that allows the model to automatically choose a function.
    /// </summary>
    /// <returns>A function call configuration for auto function selection.</returns>
    public static FunctionCall Auto() => new() { Name = "auto" };
    
    /// <summary>
    /// Creates a configuration that forces the model to call a specific function.
    /// </summary>
    /// <param name="functionName">The name of the function to call.</param>
    /// <returns>A function call configuration for the specified function.</returns>
    public static FunctionCall Specific(string functionName) => new() { Name = functionName };
}

/// <summary>
/// Configuration for function calling in a completion request.
/// </summary>
public sealed record FunctionsConfig
{
    /// <summary>
    /// Gets or sets the available functions that can be called.
    /// </summary>
    [JsonPropertyName("functions")]
    public IReadOnlyList<FunctionDefinition> Functions { get; init; } = [];
    
    /// <summary>
    /// Gets or sets how the model should use functions.
    /// </summary>
    [JsonPropertyName("function_call")]
    public FunctionCall? FunctionCall { get; init; }
}
