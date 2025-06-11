using GenAI.Bridge.Contracts;

namespace GenAI.Bridge.Abstractions;

/// <summary>
/// Volatile or persistent context for multi-turn/agent scenarios.
/// </summary>
public interface IContextStore
{
    Task SaveTurnAsync(string sessionId, PromptTurn turn, TimeSpan ttl, CancellationToken ct = default);
    Task<IReadOnlyList<PromptTurn>> LoadTurnsAsync(string sessionId, int maxTurns, CancellationToken ct = default);
}