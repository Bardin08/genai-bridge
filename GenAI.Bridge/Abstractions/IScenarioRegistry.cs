using GenAI.Bridge.Contracts;

namespace GenAI.Bridge.Abstractions;

/// <summary>
/// Registry for reusable, named AI scenarios (multi-stage orchestration).
/// <para>CONTRACT: Returns a valid ScenarioPrompt or throws KeyNotFoundException if missing.</para>
/// </summary>
public interface IScenarioRegistry
{
    /// <summary>
    /// Gets a scenario by name.
    /// </summary>
    Task<ScenarioPrompt> GetScenario(string scenarioName);

    /// <summary>
    /// Lists all available scenario names.
    /// </summary>
    IEnumerable<string> ListScenarioNames();
}