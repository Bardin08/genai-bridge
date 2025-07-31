namespace GenAI.Bridge.Providers.OpenAI;

/// <summary>
/// Configuration for the OpenAI LLM adapter.
/// </summary>
public sealed record OpenAiLlmAdapterConfig
{
    /// <summary>
    /// The base URL for the OpenAI API.
    /// </summary>
    public string BaseUrl { get; init; } = "https://api.openai.com/v1/";

    /// <summary>
    /// The OpenAI API key.
    /// </summary>
    public required string ApiKey { get; init; }

    /// <summary>
    /// Optional organization ID for OpenAI API.
    /// </summary>
    public string? OrganizationId { get; init; }

    /// <summary>
    /// The set of models supported by this adapter configuration.
    /// </summary>
    public HashSet<string> SupportedModels { get; init; } = [];

    /// <summary>
    /// Optional model cost metadata for cost estimation and policy enforcement.
    /// Key is the model name, value is the cost per 1K tokens.
    /// </summary>
    public Dictionary<string, decimal>? ModelCostPerThousandTokens { get; init; }

    /// <summary>
    /// Optional timeout for API requests in seconds.
    /// </summary>
    public int TimeoutSeconds { get; init; } = 120;

    /// <summary>
    /// Represents the ID of the project at the OpenAI side. Used for cost management.
    /// </summary>
    public string? ProjectId { get; init; }

    /// <summary>
    /// Optional flag to allow parallel tool calls. Default is false.
    /// </summary>
    public bool AllowParallelToolCalls { get; init; } = false;

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the configuration is invalid.</exception>
    public void Validate()
    {
        if (string.IsNullOrEmpty(ApiKey))
        {
            throw new ArgumentException("API key must be provided", nameof(ApiKey));
        }

        if (string.IsNullOrEmpty(BaseUrl))
        {
            throw new ArgumentException("Base URL must be provided", nameof(BaseUrl));
        }

        if (SupportedModels.Count == 0)
        {
            throw new ArgumentException("At least one supported model must be specified", nameof(SupportedModels));
        }

        if (TimeoutSeconds <= 0)
        {
            throw new ArgumentException("Timeout must be greater than 0", nameof(TimeoutSeconds));
        }
    }
}
