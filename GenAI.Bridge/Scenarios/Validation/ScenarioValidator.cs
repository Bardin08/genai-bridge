using GenAI.Bridge.Contracts;

namespace GenAI.Bridge.Scenarios.Validation;

/// <summary>
/// Default implementation of IScenarioValidator.
/// </summary>
public class ScenarioValidator : IScenarioValidator
{
    /// <inheritdoc />
    public ValidationResult Validate(ScenarioDefinition scenario)
    {
        var result = new ValidationResult();

        EnsureScenarioName(scenario, result);
        EnsureValidModels(scenario, result);

        ValidateScenarioStages(scenario, result);

        return result;
    }

    private static void EnsureValidModels(ScenarioDefinition scenario, ValidationResult result)
    {
        if (scenario.ValidModels.Count == 0)
        {
            result.Errors.Add(new ValidationError
            {
                PropertyName = nameof(scenario.ValidModels),
                Message = "At least one valid model must be specified"
            });
        }
    }

    private static void EnsureScenarioName(ScenarioDefinition scenario, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(scenario.Name))
        {
            result.Errors.Add(new ValidationError
            {
                PropertyName = nameof(scenario.Name),
                Message = "Scenario name is required"
            });
        }
    }

    private static void ValidateScenarioStages(ScenarioDefinition scenario, ValidationResult result)
    {
        if (scenario.Stages.Count == 0)
        {
            result.Errors.Add(new ValidationError
            {
                PropertyName = nameof(scenario.Stages),
                Message = "Scenario must have at least one stage"
            });
        }
        else
        {
            for (var i = 0; i < scenario.Stages.Count; i++)
            {
                ValidateScenarioStage(scenario, result, i);
            }
        }
    }

    private static void ValidateScenarioStage(ScenarioDefinition scenario, ValidationResult result, int stageIndex)
    {
        var stage = scenario.Stages[stageIndex];
        var stageNumber = stageIndex + 1;

        EnsureUserPromptTemplate(stage, result, stageIndex, stageNumber);
        EnsureValidTemperature(stage, result, stageIndex, stageNumber);
        EnsureValidTopP(stage, result, stageIndex, stageNumber);
        EnsureMaxTokens(stage, result, stageIndex, stageNumber);
    }

    private static void EnsureUserPromptTemplate(ScenarioStage stage, ValidationResult result, int stageIndex,
        int stageNumber)
    {
        if (string.IsNullOrWhiteSpace(stage.UserPromptTemplate))
        {
            result.Errors.Add(new ValidationError
            {
                PropertyName = $"{nameof(ScenarioDefinition.Stages)}[{stageIndex}].{nameof(stage.UserPromptTemplate)}",
                Message = $"Stage {stageNumber}: User prompt template is required"
            });
        }
    }

    private static void EnsureValidTemperature(ScenarioStage stage, ValidationResult result, int i, int stageIndex)
    {
        if (stage.Temperature is < 0 or > 1)
        {
            result.Errors.Add(new ValidationError
            {
                PropertyName = $"{nameof(ScenarioDefinition.Stages)}[{i}].{nameof(stage.Temperature)}",
                Message = $"Stage {stageIndex}: Temperature must be between 0 and 1"
            });
        }
    }

    private static void EnsureValidTopP(ScenarioStage stage, ValidationResult result, int stageIndex, int stageNumber)
    {
        if (stage.TopP is < 0 or > 1)
        {
            result.Errors.Add(new ValidationError
            {
                PropertyName = $"{nameof(ScenarioDefinition.Stages)}[{stageIndex}].{nameof(stage.TopP)}",
                Message = $"Stage {stageNumber}: TopP must be between 0 and 1"
            });
        }
    }

    private static void EnsureMaxTokens(ScenarioStage stage, ValidationResult result, int stageIndex, int stageNumber)
    {
        if (stage.MaxTokens is <= 0)
        {
            result.Errors.Add(new ValidationError
            {
                PropertyName = $"{nameof(ScenarioDefinition.Stages)}[{stageIndex}].{nameof(stage.MaxTokens)}",
                Message = $"Stage {stageNumber}: MaxTokens must be greater than 0"
            });
        }
    }
}