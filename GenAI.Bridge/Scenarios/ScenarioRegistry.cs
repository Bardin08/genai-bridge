using System.Collections.Concurrent;
using System.Text.Json;
using GenAI.Bridge.Abstractions;
using GenAI.Bridge.Contracts;
using GenAI.Bridge.Scenarios.Validation;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GenAI.Bridge.Scenarios;

/// <summary>
/// Implementation of IScenarioRegistry that loads scenarios from YAML/JSON files
/// with support for remote scenario store as a fallback.
/// </summary>
public class ScenarioRegistry : IScenarioRegistry
{
    private readonly ConcurrentDictionary<string, ScenarioPrompt>
        _scenarioCache = new(StringComparer.OrdinalIgnoreCase);

    private readonly string _scenariosDirectory;
    private readonly IDeserializer _yamlDeserializer;
    private readonly IScenarioValidator _validator;
    private readonly IRemoteScenarioStore? _remoteStore;
    private readonly ILogger<ScenarioRegistry>? _logger;
    private readonly bool _preferRemoteStore;
    private readonly Task _initializeTask;

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private bool _isInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScenarioRegistry"/> class.
    /// </summary>
    /// <param name="scenariosDirectory">Directory where scenario files are stored.</param>
    /// <param name="validator">Validator for scenario definitions.</param>
    /// <param name="remoteStore">Optional remote scenario store.</param>
    /// <param name="preferRemoteStore">Whether to prefer the remote store over file system.</param>
    /// <param name="logger">Optional logger.</param>
    public ScenarioRegistry(
        string scenariosDirectory,
        IScenarioValidator validator,
        IRemoteScenarioStore? remoteStore = null,
        bool preferRemoteStore = true,
        ILogger<ScenarioRegistry>? logger = null)
    {
        _scenariosDirectory = scenariosDirectory ?? throw new ArgumentNullException(nameof(scenariosDirectory));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _remoteStore = remoteStore;
        _preferRemoteStore = preferRemoteStore && remoteStore != null;
        _logger = logger;

        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        _initializeTask = Task.Run(async () =>
        {
            await LoadAllScenariosAsync();
            _isInitialized = true;
        });
    }

    /// <inheritdoc />
    public async Task<ScenarioPrompt> GetScenario(string scenarioName)
    {
        if (string.IsNullOrWhiteSpace(scenarioName))
        {
            throw new ArgumentException("Scenario name cannot be empty", nameof(scenarioName));
        }

        if (!_isInitialized)
        {
            await _initializeTask;
        }

        // Try to get from cache first
        if (_scenarioCache.TryGetValue(scenarioName, out var scenario))
        {
            return scenario;
        }

        if (_preferRemoteStore && _remoteStore != null)
        {
            var remoteResult = await GetScenarioFromRemoteStorage(scenarioName);
            if (remoteResult is { IsSuccess: true, Scenario: not null })
            {
                _scenarioCache[scenarioName] = remoteResult.Scenario;
                return remoteResult.Scenario;
            }
        }

        var fileResult = await GetScenarioFromFile(scenarioName);
        if (fileResult is { IsSuccess: true, Scenario: not null })
        {
            _scenarioCache[scenarioName] = fileResult.Scenario;
            return fileResult.Scenario;
        }

        // If remote store wasn't tried yet, try it as fallback
        if (_remoteStore != null)
        {
            var remoteResult = await GetScenarioFromRemoteStorage(scenarioName);
            if (remoteResult is { IsSuccess: true, Scenario: not null })
            {
                _scenarioCache[scenarioName] = remoteResult.Scenario;
                return remoteResult.Scenario;
            }
        }

        throw new KeyNotFoundException($"Scenario '{scenarioName}' not found");
    }

    private async Task<(bool IsSuccess, ScenarioPrompt? Scenario)> GetScenarioFromRemoteStorage(string scenarioName)
    {
        var scenario = await LoadScenarioFromRemoteStoreAsync(scenarioName);
        return (scenario is not null, scenario);
    }

    private async Task<(bool IsSuccess, ScenarioPrompt? Scenario)> GetScenarioFromFile(string scenarioName)
    {
        var scenarioPath = FindScenarioFile(scenarioName);
        if (scenarioPath == null)
            return (false, null);

        var scenario = await LoadScenarioFromFileAsync(scenarioPath);
        return (scenario is not null, scenario);
    }

    private string? FindScenarioFile(string scenarioName)
    {
        if (!Directory.Exists(_scenariosDirectory))
        {
            return null;
        }

        var possibleExtensions = new[] { ".yaml", ".yml", ".json" };
        foreach (var ext in possibleExtensions)
        {
            var possiblePath = Path.Combine(_scenariosDirectory, $"{scenarioName}{ext}");
            if (File.Exists(possiblePath))
            {
                return possiblePath;
            }

            var foundFiles = Directory.GetFiles(
                _scenariosDirectory,
                $"{scenarioName}{ext}",
                SearchOption.AllDirectories);

            if (foundFiles.Length > 0)
            {
                return foundFiles[0];
            }
        }

        return null;
    }

    /// <inheritdoc />
    public IEnumerable<string> ListScenarioNames()
    {
        return _scenarioCache.Keys.OrderBy(k => k);
    }

    private async Task LoadAllScenariosAsync()
    {
        Task[] tasks =
        [
            LoadAllScenariosFromFilesAsync(),
            LoadAllScenariosFromRemoteStorageAsync()
        ];

        await Task.WhenAll(tasks);
    }

    private async Task LoadAllScenariosFromFilesAsync()
    {
        if (Directory.Exists(_scenariosDirectory))
        {
            var yamlFiles = Directory.GetFiles(_scenariosDirectory, "*.yaml", SearchOption.AllDirectories);
            var ymlFiles = Directory.GetFiles(_scenariosDirectory, "*.yml", SearchOption.AllDirectories);
            var jsonFiles = Directory.GetFiles(_scenariosDirectory, "*.json", SearchOption.AllDirectories);

            var scenarioFiles = yamlFiles.Concat(ymlFiles).Concat(jsonFiles).ToList();

            foreach (var file in scenarioFiles)
            {
                try
                {
                    await LoadScenarioFromFileAsync(file);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to load scenario from file: {File}", file);
                }
            }
        }
        else
        {
            _logger?.LogWarning("Scenarios directory does not exist: {Directory}", _scenariosDirectory);
        }
    }

    private async Task<ScenarioPrompt?> LoadScenarioFromFileAsync(string filePath)
    {
        var fileContent = await File.ReadAllTextAsync(filePath);
        var fileExtension = Path.GetExtension(filePath).ToLowerInvariant();

        var scenarioDefinition = fileExtension == ".json"
            ? JsonSerializer.Deserialize<ScenarioDefinition>(fileContent, JsonSerializerOptions)!
            : _yamlDeserializer.Deserialize<ScenarioDefinition>(fileContent);

        var validationResult = _validator.Validate(scenarioDefinition);
        if (!validationResult.IsValid)
        {
            var errorMessage = string.Join("; ", validationResult.Errors.Select(e => e.Message));
            _logger?.LogWarning("Scenario validation failed for {File}: {Errors}", filePath, errorMessage);
            return null;
        }

        // Convert to ScenarioPrompt and add to cache
        var scenarioPrompt = ConvertToScenarioPrompt(scenarioDefinition);

        _logger?.LogInformation("Loaded scenario: {Name} from {File}", scenarioDefinition.Name, filePath);
        return scenarioPrompt;
    }

    private async Task LoadAllScenariosFromRemoteStorageAsync()
    {
        if (_remoteStore != null)
        {
            try
            {
                var remoteScenarioNames = await _remoteStore.ListScenarioNamesAsync();

                foreach (var name in remoteScenarioNames)
                {
                    if (_scenarioCache.ContainsKey(name))
                        continue;

                    await LoadScenarioFromRemoteStoreAsync(name);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load scenarios from remote store");
            }
        }
    }

    private async Task<ScenarioPrompt?> LoadScenarioFromRemoteStoreAsync(string scenarioName)
    {
        if (_remoteStore == null)
        {
            return null;
        }

        try
        {
            var definition = await _remoteStore.GetScenarioAsync(scenarioName);
            if (definition == null)
            {
                return null;
            }

            var validationResult = _validator.Validate(definition);
            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join("; ", validationResult.Errors.Select(e => e.Message));
                _logger?.LogWarning("Remote scenario validation failed for {Name}: {Errors}", scenarioName,
                    errorMessage);
                return null;
            }

            var scenarioPrompt = ConvertToScenarioPrompt(definition);

            _logger?.LogInformation("Loaded scenario from remote store: {Name}", definition.Name);
            return scenarioPrompt;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load scenario from remote store: {Name}", scenarioName);
            return null;
        }
    }

    private ScenarioPrompt ConvertToScenarioPrompt(ScenarioDefinition definition)
    {
        var turns = new List<PromptTurn>();

        foreach (var stage in definition.Stages)
        {
            if (!string.IsNullOrWhiteSpace(stage.SystemPrompt))
            {
                turns.Add(PromptTurn.System(stage.SystemPrompt));
            }

            ConstructStage(stage, turns);
        }

        return new ScenarioPrompt(
            Name: definition.Name,
            Turns: turns,
            Metadata: new Dictionary<string, string>(definition.Metadata)
            {
                ["version"] = definition.Version,
                ["description"] = definition.Description,
                ["valid_models"] = string.Join(",", definition.ValidModels)
            }
        );
    }

    private void ConstructStage(ScenarioStage stage, List<PromptTurn> turns)
    {
        var parameters = new Dictionary<string, object>(stage.Parameters);

        if (stage.Temperature.HasValue) parameters["temperature"] = stage.Temperature.Value;
        if (stage.TopP.HasValue) parameters["top_p"] = stage.TopP.Value;
        if (stage.MaxTokens.HasValue) parameters["max_tokens"] = stage.MaxTokens.Value;

        FillStageResponseFormat(stage, parameters);
        AddStageFunctions(stage, parameters);
        AddStageTools(stage, parameters);

        turns.Add(PromptTurn.User(stage.UserPromptTemplate, parameters));
    }

    private void FillStageResponseFormat(ScenarioStage stage, Dictionary<string, object> parameters)
    {
        if (stage.ResponseFormat == null)
            return;

        var responseFormatType = stage.ResponseFormat.Type.ToLowerInvariant() switch
        {
            "json_object" => ResponseFormatType.JsonObject,
            "json" => ResponseFormatType.JsonObject,
            _ => ResponseFormatType.Text
        };

        if (responseFormatType is ResponseFormatType.Text)
        {
            parameters["response_format"] = ResponseFormat.Text();
            return;
        }

        GetResponseJsonSchema(stage, parameters);
    }

    private void GetResponseJsonSchema(ScenarioStage stage, Dictionary<string, object> parameters)
    {
        ResponseFormat? responseFormat;

        // Check for schema_type first (C# type reference)
        if (!string.IsNullOrWhiteSpace(stage.ResponseFormat!.SchemaType))
        {
            var schema = Utils.TypeResolver.GenerateSchemaFromTypeName(stage.ResponseFormat.SchemaType);
            responseFormat = schema != null
                ? ResponseFormat.JsonWithSchema(schema)
                : ResponseFormat.Json();

            _logger?.LogInformation("Generated schema from type: {Type}", stage.ResponseFormat.SchemaType);
        }
        // Then check for explicit schema
        else if (!string.IsNullOrWhiteSpace(stage.ResponseFormat.Schema))
        {
            responseFormat = ResponseFormat.JsonWithSchema(stage.ResponseFormat.Schema);
        }
        else
        {
            throw new ArgumentException("No response schema specified. Reference a C# type or provide a JSON schema.");
        }

        parameters["response_format"] = responseFormat;
    }

    private void AddStageFunctions(ScenarioStage stage, Dictionary<string, object> parameters)
    {
        if (stage.Functions is not { Functions.Count: > 0 })
            return;

        var functions = new List<FunctionDefinition>();
        foreach (var func in stage.Functions.Functions)
        {
            BuildFunction(func, functions);
        }

        var functionCall = stage.Functions.FunctionCall != null
            ? new FunctionCall { Name = stage.Functions.FunctionCall == "auto" ? null : stage.Functions.FunctionCall }
            : null;

        parameters["functions"] = new FunctionsConfig
        {
            Functions = functions,
            FunctionCall = functionCall
        };
    }

    private void BuildFunction(FunctionDefinitionConfig func, List<FunctionDefinition> functions)
    {
        var parametersSchema = func.Parameters;

        // If parameters_type is specified, generate schema from the C# type
        if (!string.IsNullOrWhiteSpace(func.ParametersType))
        {
            var generatedSchema = Utils.TypeResolver.GenerateSchemaFromTypeName(func.ParametersType);
            if (generatedSchema != null)
            {
                parametersSchema = generatedSchema;
                _logger?.LogInformation("Generated function parameters schema from type: {Type}",
                    func.ParametersType);
            }
            else
            {
                _logger?.LogWarning("Failed to generate function parameters schema from type: {Type}",
                    func.ParametersType);
            }
        }

        // Parse the parameters JSON string to object
        object parametersObject;
        try
        {
            parametersObject = JsonDocument.Parse(parametersSchema).RootElement;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to parse function parameters JSON for function: {Name}",
                func.Name);
            parametersObject = new { };
        }

        functions.Add(new FunctionDefinition
        {
            Name = func.Name,
            Description = func.Description,
            Parameters = parametersObject
        });
    }

    private void AddStageTools(ScenarioStage stage, Dictionary<string, object> parameters)
    {
        if (stage.Tools is not { Count: > 0 })
            return;

        var tools = new List<Tool>();
        foreach (var tool in stage.Tools)
        {
            BuildTool(tool, tools);
        }

        parameters["tools"] = tools;
    }

    private void BuildTool(ToolDefinition tool, List<Tool> tools)
    {
        var parametersSchema = tool.Function.Parameters;

        if (!string.IsNullOrWhiteSpace(tool.Function.ParametersType))
        {
            var generatedSchema = Utils.TypeResolver.GenerateSchemaFromTypeName(tool.Function.ParametersType);
            if (generatedSchema != null)
            {
                parametersSchema = generatedSchema;
                _logger?.LogInformation("Generated tool parameters schema from type: {Type}",
                    tool.Function.ParametersType);
            }
            else
            {
                _logger?.LogWarning("Failed to generate tool parameters schema from type: {Type}",
                    tool.Function.ParametersType);
            }
        }

        // Parse the parameters JSON string to object
        object parametersObject;
        try
        {
            parametersObject = JsonDocument.Parse(parametersSchema).RootElement;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to parse tool parameters JSON for function: {Name}",
                tool.Function.Name);
            parametersObject = new { };
        }

        tools.Add(new Tool
        {
            Type = tool.Type,
            Function = new FunctionDefinition
            {
                Name = tool.Function.Name,
                Description = tool.Function.Description,
                Parameters = parametersObject
            }
        });
    }
}