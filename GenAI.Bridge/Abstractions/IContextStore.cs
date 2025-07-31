using GenAI.Bridge.Contracts.Prompts;

namespace GenAI.Bridge.Abstractions;

/// <summary>
/// Manages temporary or persistent conversational context (chat history, short-term memory).
/// <para>CONTRACT: All operations are thread-safe and support TTL-based expiration.</para>
/// </summary>
public interface IContextStore
{
    /// <summary>
    /// Saves a single conversation turn for a session.
    /// If the session doesn't exist, it is created.
    /// </summary>
    Task SaveTurnAsync(
        string sessionId,
        PromptTurn turn,
        TimeSpan? ttl = null,
        CancellationToken ct = default);

    /// <summary>
    /// Loads the most recent turns for a session, up to maxTurns.
    /// If session does not exist or expired, returns empty list.
    /// </summary>
    Task<IReadOnlyList<PromptTurn>> LoadTurnsAsync(
        string sessionId,
        int? maxTurns = null,
        CancellationToken ct = default);
}