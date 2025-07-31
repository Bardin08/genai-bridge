using GenAI.Bridge.Scenarios.Models;

namespace GenAI.Bridge.Scenarios.Validation;

/// <summary>
/// Validator for scenario definitions.
/// </summary>
public interface IScenarioValidator
{
    /// <summary>
    /// Validates a scenario definition.
    /// </summary>
    /// <param name="scenario">The scenario to validate.</param>
    /// <returns>The validation result.</returns>
    ValidationResult Validate(ScenarioDefinition scenario);
}