using GenAI.Bridge.Contracts.Prompts;
using GenAI.Bridge.Utils.Extensions;

namespace GenAI.Bridge.Contracts.Scenarios;

/// <summary>
/// Represents a logical stage within a scenario with its own sequence of prompt turns.
/// </summary>
public sealed record ScenarioStage(
    int Id,
    string Name,
    IReadOnlyList<PromptTurn> Turns,
    string? Model = null,
    IReadOnlyDictionary<string, object>? Parameters = null)
{
    /// <summary>
    /// Get all user turns in this stage.
    /// </summary>
    /// <returns>A list of user turns in this stage.</returns>
    public List<PromptTurn> GetUserTurns()
        => Turns.Where(t => t.Role is Constants.Roles.User).ToList();

    /// <summary>
    /// Get the system prompt for this stage.
    /// </summary>
    /// <returns>A system prompt for this stage.</returns>
    public PromptTurn? GetSystemPrompt()
        => Turns.SingleOrDefault(t => t.Role is Constants.Roles.System);

    /// <summary>
    /// Builds multiple CompletionPrompts from this scenario stage, one for each user turn.
    /// </summary>
    /// <param name="sessionId"></param>
    /// <param name="execMetadata">The context dictionary for parameter interpolation.</param>
    /// <returns>A list of CompletionPrompts ready to be sent to an LLM.</returns>
    public IReadOnlyList<CompletionPrompt> ToCompletionPrompts(string sessionId,
        IDictionary<string, object> execMetadata)
    {
        var systemMessage = GetSystemPrompt()?.Content;

        var userTurns = GetUserTurns();
        if (userTurns.Count == 0)
            throw new InvalidOperationException($"Stage '{Name}' does not contain any user turns");

        var prompts = new List<CompletionPrompt>();
        
        foreach (var userTurn in userTurns)
        {
            // Create metadata from stage parameters
            var metadata = Parameters != null 
                ? new Dictionary<string, object>(Parameters) 
                : new Dictionary<string, object>();

            metadata.Merge(execMetadata);

            // Create relevant history for this user turn (all previous turns before this user turn)
            var turnsBeforeThis = Turns
                .TakeWhile(t => t != userTurn)
                .Count(t => t.Role != Constants.Roles.System);

            metadata["history_depth"] = turnsBeforeThis;
            
            var prompt = new CompletionPrompt(sessionId, systemMessage, userTurn, metadata);
            prompts.Add(prompt);
        }

        return prompts;
    }
}
