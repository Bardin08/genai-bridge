using GenAI.Bridge.Contracts;

namespace GenAI.Bridge.Abstractions;

/// <summary>
/// Resolves model/provider for a given prompt or scenario.
/// </summary>
public interface IModelRouter
{
    string ResolveModelFor(CompletionPrompt prompt);
    string ResolveModelFor(ScenarioPrompt scenarioPrompt);
}