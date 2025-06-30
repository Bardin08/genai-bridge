using GenAI.Bridge.Contracts.Configuration;

namespace GenAI.Bridge.Contracts.Prompts;

/// <summary>
/// Represents a single turn in a multi-turn prompt, aligned with OpenAI's message format.
/// </summary>
public sealed record PromptTurn(
    string Role, // e.g., "user", "assistant", "system", "function"
    string Content,
    string? Name = null, // For function calls and tool usage
    IReadOnlyDictionary<string, object>? Parameters = null)
{
    /// <summary>
    /// Temperature setting for this turn (0.0 to 1.0)
    /// </summary>
    public float? Temperature => Parameters?.TryGetValue("temperature", out var temp) == true ? (float?)temp : null;

    /// <summary>
    /// TopP setting for this turn (0.0 to 1.0)
    /// </summary>
    public float? TopP => Parameters?.TryGetValue("top_p", out var topP) == true ? (float?)topP : null;

    /// <summary>
    /// Maximum tokens to generate for this turn
    /// </summary>
    public int? MaxTokens => Parameters?.TryGetValue("max_tokens", out var maxTokens) == true ? (int?)maxTokens : null;

    /// <summary>
    /// Function calling configuration for this turn
    /// </summary>
    public FunctionsConfig? Functions => Parameters?.TryGetValue("functions", out var functions) == true
        ? (FunctionsConfig)functions
        : null;

    /// <summary>
    /// Tool usage configuration for this turn
    /// </summary>
    public IReadOnlyList<Tool>? Tools =>
        Parameters?.TryGetValue("tools", out var tools) == true ? (IReadOnlyList<Tool>)tools : null;

    /// <summary>
    /// Response format for structured output (e.g., JSON)
    /// </summary>
    public ResponseFormat? ResponseFormat => Parameters?.TryGetValue("response_format", out var format) == true
        ? (ResponseFormat)format
        : null;

    /// <summary>
    /// Creates a user prompt turn
    /// </summary>
    public static PromptTurn User(string content, string name,
        IReadOnlyDictionary<string, object>? parameters = null) =>
        new(Role: "user", Content: content, Name: name, Parameters: parameters);

    /// <summary>
    /// Creates a system prompt turn
    /// </summary>
    public static PromptTurn System(string content, IReadOnlyDictionary<string, object>? parameters = null) =>
        new(Role: "system", Content: content, Parameters: parameters);

    /// <summary>
    /// Creates an assistant prompt turn
    /// </summary>
    public static PromptTurn Assistant(string content, IReadOnlyDictionary<string, object>? parameters = null) =>
        new(Role: "assistant", Content: content, Parameters: parameters);

    /// <summary>
    /// Creates a function prompt turn
    /// </summary>
    public static PromptTurn Function(string name, string content,
        IReadOnlyDictionary<string, object>? parameters = null) =>
        new(Role: "function", Content: content, Name: name, Parameters: parameters);

    /// <summary>
    /// Creates a user prompt turn with JSON response format
    /// </summary>
    public static PromptTurn UserWithJsonResponse(string content,
        IReadOnlyDictionary<string, object>? parameters = null)
    {
        var allParams = parameters != null
            ? new Dictionary<string, object>(parameters)
            : new Dictionary<string, object>();

        allParams["response_format"] = ResponseFormat.Json();

        return new PromptTurn(Role: "user", Content: content, Parameters: allParams);
    }

    /// <summary>
    /// Creates a user prompt turn with JSON schema response format
    /// </summary>
    public static PromptTurn UserWithJsonSchema(string content, string schema,
        IReadOnlyDictionary<string, object>? parameters = null)
    {
        var allParams = parameters != null
            ? new Dictionary<string, object>(parameters)
            : new Dictionary<string, object>();

        allParams["response_format"] = ResponseFormat.JsonWithSchema(schema);

        return new PromptTurn(Role: "user", Content: content, Parameters: allParams);
    }
}