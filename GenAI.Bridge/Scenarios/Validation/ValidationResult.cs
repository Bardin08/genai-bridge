namespace GenAI.Bridge.Scenarios.Validation;

/// <summary>
/// Validation result for scenario definitions.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the scenario is valid.
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// Gets the validation errors.
    /// </summary>
    public List<ValidationError> Errors { get; } = [];
}