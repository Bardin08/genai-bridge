using GenAI.Bridge.Contracts;

namespace GenAI.Bridge.Abstractions;

/// <summary>
/// Resolves model/provider by policy for a prompt or scenario.
/// <para>CONTRACT: Returns provider/model identifiers suitable for downstream orchestration.</para>
/// </summary>
public interface IModelRouter
{
    string ResolveModelFor(CompletionPrompt prompt);

    string ResolveModelFor(ScenarioPrompt scenarioPrompt);
}