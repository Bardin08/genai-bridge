using System.Text.Json;
using GenAI.Bridge.Contracts.Prompts;
using GenAI.Bridge.Scenarios.Builders;
using GenAI.Bridge.Scenarios.Models;
using GenAI.Bridge.Scenarios.Validation;
using GenAI.Bridge.Utils;
using Microsoft.Extensions.Logging;
using YamlDotNet.Core;

namespace GenAI.Bridge.Scenarios.Storage;

public class FileScenarioStore(
    string scenariosDirectory,
    IScenarioValidator validator,
    ILogger<FileScenarioStore> logger)
    : IRemoteScenarioStore
{
    private readonly string _scenariosDirectory =
        scenariosDirectory ?? throw new ArgumentNullException(nameof(scenariosDirectory));

    private readonly IScenarioValidator _validator = validator ?? throw new ArgumentNullException(nameof(validator));

    private readonly ILogger<FileScenarioStore> _logger = logger;

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <inheritdoc />
    public async Task<ScenarioPrompt?> GetScenarioAsync(string scenarioName)
    {
        var scenarioPath = FindScenarioFile(scenarioName);
        if (scenarioPath == null)
            return null;

        return await LoadScenarioFromFileAsync(scenarioPath);
    }

    /// <inheritdoc />
    public async Task<List<ScenarioPrompt?>> GetAllScenariosAsync()
    {
        if (!Directory.Exists(_scenariosDirectory))
        {
            _logger.LogWarning("Scenarios directory does not exist: {Directory}", _scenariosDirectory);
            return [];
        }

        var yamlFiles = Directory.GetFiles(_scenariosDirectory, "*.yaml", SearchOption.AllDirectories);
        var ymlFiles = Directory.GetFiles(_scenariosDirectory, "*.yml", SearchOption.AllDirectories);
        var jsonFiles = Directory.GetFiles(_scenariosDirectory, "*.json", SearchOption.AllDirectories);

        var scenarioFiles = yamlFiles.Concat(ymlFiles).Concat(jsonFiles).ToList();

        var loadScenarioTasks = scenarioFiles
            .Select(LoadScenarioFromFileAsync);

        return (await Task.WhenAll(loadScenarioTasks))
            .Where(s => s != null)
            .ToList();
    }

    public Task<IEnumerable<string>> ListScenarioNamesAsync()
    {
        if (!Directory.Exists(_scenariosDirectory))
            return Task.FromResult(Enumerable.Empty<string>());

        var scenarioFiles = Directory.GetFiles(_scenariosDirectory, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".json", StringComparison.OrdinalIgnoreCase));

        var scenarioNames = scenarioFiles.Select(Path.GetFileNameWithoutExtension).Distinct();
        return Task.FromResult(scenarioNames)!;
    }

    public Task<bool> StoreScenarioAsync(ScenarioDefinition scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        if (string.IsNullOrWhiteSpace(scenario.Name))
            throw new ArgumentException("Scenario name cannot be null or empty.", nameof(scenario));

        if (!Directory.Exists(_scenariosDirectory))
            Directory.CreateDirectory(_scenariosDirectory);

        var fileName = Path.Combine(_scenariosDirectory, $"{scenario.Name}.yaml");
        try
        {
            var yamlContent = scenario.SerializeToYaml();
            File.WriteAllText(fileName, yamlContent);
            _logger.LogInformation("Stored scenario: {Name} to {File}", scenario.Name, fileName);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store scenario: {Name}", scenario.Name);
            return Task.FromResult(false);
        }
    }

    public Task<bool> DeleteScenarioAsync(string scenarioName)
    {
        var filePath = FindScenarioFile(scenarioName);
        if (filePath == null)
        {
            _logger.LogWarning("Scenario file not found: {Name}", scenarioName);
            return Task.FromResult(true); // Consider it successful if the file doesn't exist
        }

        try
        {
            File.Delete(filePath);
            _logger.LogInformation("Deleted scenario: {Name} from {File}", scenarioName, filePath);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete scenario: {Name}", scenarioName);
            return Task.FromResult(false);
        }
    }

    private string? FindScenarioFile(string scenarioName)
    {
        if (!Directory.Exists(_scenariosDirectory))
            return null;

        var possibleExtensions = new[] { ".yaml", ".yml", ".json" };
        foreach (var ext in possibleExtensions)
        {
            var possiblePath = Path.Combine(_scenariosDirectory, $"{scenarioName}{ext}");
            if (File.Exists(possiblePath))
                return possiblePath;

            var foundFiles = Directory.GetFiles(
                _scenariosDirectory,
                $"{scenarioName}{ext}",
                SearchOption.AllDirectories);

            if (foundFiles.Length > 0)
                return foundFiles[0];
        }

        return null;
    }

    private async Task<ScenarioPrompt?> LoadScenarioFromFileAsync(string filePath)
    {
        try
        {
            var scenarioPrompt = await LoadScenarioFromFileInternalAsync(filePath);
            return scenarioPrompt;
        }
        catch (YamlException e)
        {
            throw new ArgumentException($"Failed to parse YAML scenario file: {filePath}", e);
        }
        catch (JsonException e)
        {
            throw new ArgumentException($"Failed to parse JSON scenario file: {filePath}", e);
        }
        catch (Exception)
        {
            throw new ArgumentException($"Unexpected error loading scenario from file: {filePath}", filePath);
        }
    }

    private async Task<ScenarioPrompt?> LoadScenarioFromFileInternalAsync(string filePath)
    {
        var fileContent = await File.ReadAllTextAsync(filePath);
        var fileExtension = Path.GetExtension(filePath).ToLowerInvariant();

        var scenarioDefinition = fileExtension == ".json"
            ? JsonSerializer.Deserialize<ScenarioDefinition>(fileContent, JsonSerializerOptions)!
            : fileContent.DeserializeFromYaml<ScenarioDefinition>();    

        var validationResult = _validator.Validate(scenarioDefinition);
        if (!validationResult.IsValid)
        {
            var errorMessage = string.Join("; ", validationResult.Errors.Select(e => e.Message));
            _logger.LogWarning("Scenario validation failed for {File}: {Errors}", filePath, errorMessage);
            return null;
        }

        var scenarioPrompt = ScenarioBuilder.ConvertToScenarioPrompt(scenarioDefinition);

        _logger.LogInformation("Loaded scenario: {Name} from {File}", scenarioDefinition.Name, filePath);
        return scenarioPrompt;
    }
}