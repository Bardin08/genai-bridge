using System.Collections.Concurrent;
using GenAI.Bridge.Abstractions;
using GenAI.Bridge.Contracts;
using GenAI.Bridge.Contracts.Prompts;
using GenAI.Bridge.Scenarios.Storage;
using Microsoft.Extensions.Logging;

namespace GenAI.Bridge.Scenarios;

/// <summary>
/// Implementation of IScenarioRegistry that loads scenarios from YAML/JSON files
/// with support for remote scenario store as a fallback.
/// </summary>
public class ScenarioRegistry : IScenarioRegistry
{
    private readonly ConcurrentDictionary<string, ScenarioPrompt>
        _scenarioCache = new(StringComparer.OrdinalIgnoreCase);

    private readonly List<IRemoteScenarioStore> _scenarioStores = [];
    private readonly ILogger<ScenarioRegistry>? _logger;

    private readonly Task _initializeTask;
    private bool _isInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScenarioRegistry"/> class.
    /// </summary>
    /// <param name="scenarioStores">A list of stores for the scenarios. Stores processed one by one. If couple stores has a scenario with a similar name the latest will be used.</param>
    /// <param name="logger">Optional logger.</param>
    public ScenarioRegistry(
        IRemoteScenarioStore[] scenarioStores,
        ILogger<ScenarioRegistry>? logger = null)
    {
        if (scenarioStores == null || scenarioStores.Length == 0)
        {
            throw new ArgumentException("At least one scenario store must be provided", nameof(scenarioStores));
        }

        _scenarioStores.AddRange(scenarioStores);
        _logger = logger;

        _initializeTask = Task.Run(async () =>
        {
            await LoadAllScenariosAsync();
            _isInitialized = true;
        });
    }

    /// <inheritdoc />
    public async Task<ScenarioPrompt> GetScenario(string scenarioName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioName);
        
        if (!_isInitialized) await _initializeTask;

        if (_scenarioCache.TryGetValue(scenarioName, out var scenario))
            return scenario;

        var fetchTasks = _scenarioStores
            .Select(store => store.GetScenarioAsync(scenarioName));
        var fetchedScenarios = await Task.WhenAll(fetchTasks);

        foreach (var scenarioPrompt in fetchedScenarios)
        {
            if (scenarioPrompt == null)
                continue;

            _scenarioCache[scenarioPrompt.Name] = scenarioPrompt;
        }
        
        if (_scenarioCache.TryGetValue(scenarioName, out scenario))
            return scenario;

        throw new KeyNotFoundException($"Scenario '{scenarioName}' not found");
    }

    /// <inheritdoc />
    public IEnumerable<string> ListScenarioNames()
    {
        return _scenarioCache.Keys.OrderBy(k => k);
    }

    private async Task LoadAllScenariosAsync()
    {
        await Task.WhenAll(
            _scenarioStores.Select(LoadAllScenariosFromStore)
        );
    }

    private async Task LoadAllScenariosFromStore(IRemoteScenarioStore store)
    {
        var allScenarios = await store.GetAllScenariosAsync();

        foreach (var scenarioPrompt in allScenarios)
        {
            try
            {
                if (scenarioPrompt == null || string.IsNullOrWhiteSpace(scenarioPrompt.Name))
                    continue;

                _scenarioCache[scenarioPrompt.Name] = scenarioPrompt;
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Failed to load scenario '{ScenarioName}' from store {StoreName}",
                    scenarioPrompt?.Name ?? "Unknown", store.GetType().Name);
            }
        }
    }
}