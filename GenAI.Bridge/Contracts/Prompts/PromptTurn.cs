using GenAI.Bridge.Contracts.Configuration;
using GenAI.Bridge.Utils.Extensions;

namespace GenAI.Bridge.Contracts.Prompts;

/// <summary>
/// Represents a single turn in a multi-turn prompt, aligned with OpenAI's message format.
/// </summary>
public sealed record PromptTurn(
    string Role,
    string Content,
    string? Name = null,
    IReadOnlyDictionary<string, object>? Parameters = null)
{
    /// <summary>
    /// Temperature setting for this turn (0.0 to 1.0)
    /// </summary>
    public float? Temperature
        => Parameters?.TryGetValue("temperature", out var topP) == true ? topP.GetParamAs<float>() : null;

    /// <summary>
    /// TopP setting for this turn (0.0 to 1.0)
    /// </summary>
    public float? TopP
        => Parameters?.TryGetValue("top_p", out var topP) == true ? topP.GetParamAs<float>() : null;

    /// <summary>
    /// Maximum tokens to generate for this turn
    /// </summary>
    public int? MaxTokens
        => Parameters?.TryGetValue("max_tokens", out var maxTokens) == true ? maxTokens.GetParamAs<int>() : null;

    public bool IsUserTurn => Role.Equals(Constants.Roles.User, StringComparison.OrdinalIgnoreCase);
    
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
        new(Role: Constants.Roles.User, Content: content, Name: name, Parameters: parameters);

    /// <summary>
    /// Creates a system prompt turn
    /// </summary>
    public static PromptTurn System(string content, IReadOnlyDictionary<string, object>? parameters = null) =>
        new(Role: Constants.Roles.System, Content: content, Parameters: parameters);

    /// <summary>
    /// Creates an assistant prompt turn
    /// </summary>
    public static PromptTurn Assistant(string content, IReadOnlyDictionary<string, object>? parameters = null) =>
        new(Role: Constants.Roles.Assistant, Content: content, Parameters: parameters);

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

        return new PromptTurn(Role: Constants.Roles.User, Content: content, Parameters: allParams);
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

        return new PromptTurn(Role: Constants.Roles.User, Content: content, Parameters: allParams);
    }
}