using GenAI.Bridge.Contracts;

namespace GenAI.Bridge.Abstractions;

/// <summary>
/// Registry of reusable AI scenarios (multi-stage workflows).
/// </summary>
public interface IScenarioRegistry
{
    ScenarioPrompt GetScenario(string scenarioName);
    IEnumerable<string> ListScenarioNames();
}