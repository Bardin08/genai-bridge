namespace GenAI.Bridge.Scenarios.Validation;

/// <summary>
/// Represents a validation error.
/// </summary>
public sealed record ValidationError
{
    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the property name that caused the error.
    /// </summary>
    public string PropertyName { get; init; } = string.Empty;
}