using GenAI.Bridge.Contracts;
using GenAI.Bridge.Scenarios.Models;

namespace GenAI.Bridge.Abstractions;

/// <summary>
/// Interface for remote scenario storage providers.
/// </summary>
public interface IRemoteScenarioStore
{
    /// <summary>
    /// Gets a scenario by name.
    /// </summary>
    /// <param name="scenarioName">Name of the scenario to retrieve.</param>
    /// <returns>The scenario if found, null otherwise.</returns>
    Task<ScenarioPrompt?> GetScenarioAsync(string scenarioName);

    /// <summary>
    /// Gets all scenarios matching the specified name.
    /// </summary>
    /// <returns>All existing scenarios or empty collection.</returns>
    Task<List<ScenarioPrompt?>> GetAllScenariosAsync();

    /// <summary>
    /// Lists all available scenario names.
    /// </summary>
    /// <returns>Collection of scenario names.</returns>
    Task<IEnumerable<string>> ListScenarioNamesAsync();

    /// <summary>
    /// Stores a scenario.
    /// </summary>
    /// <param name="scenario">The scenario to store.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> StoreScenarioAsync(ScenarioDefinition scenario);
    
    /// <summary>
    /// Deletes a scenario.
    /// </summary>
    /// <param name="scenarioName">Name of the scenario to delete.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> DeleteScenarioAsync(string scenarioName);
}
