namespace GenAI.Bridge.Contracts;

/// <summary>
/// Represents a logical stage within a scenario with its own sequence of prompt turns.
/// </summary>
public sealed record ScenarioStage(
    string Name,
    IReadOnlyList<PromptTurn> Turns,
    string? Model = null,
    IReadOnlyDictionary<string, object>? Parameters = null)
{
    /// <summary>
    /// Builds multiple CompletionPrompts from this scenario stage, one for each user turn.
    /// </summary>
    /// <param name="context">The context dictionary for parameter interpolation.</param>
    /// <returns>A list of CompletionPrompts ready to be sent to an LLM.</returns>
    public IReadOnlyList<CompletionPrompt> ToCompletionPrompts(IDictionary<string, object> context)
    {
        // Extract system message (only one allowed)
        var systemTurn = Turns.FirstOrDefault(t => t.Role == "system");
        var systemMessage = systemTurn?.Content;

        // Get all user turns
        var userTurns = Turns
            .Where(t => t.Role == "user")
            .ToList();

        if (userTurns.Count == 0)
        {
            throw new InvalidOperationException($"Stage '{Name}' does not contain any user turns");
        }

        // Create multiple completion prompts, one for each user turn
        var prompts = new List<CompletionPrompt>();
        
        foreach (var userTurn in userTurns)
        {
            // Create metadata from stage parameters
            var metadata = Parameters != null 
                ? new Dictionary<string, object>(Parameters) 
                : new Dictionary<string, object>();

            // Create relevant history for this user turn
            // (all previous turns before this user turn)
            var turnsBeforeThis = Turns
                .TakeWhile(t => t != userTurn)
                .Count(t => t.Role != "system");

            metadata["history_depth"] = turnsBeforeThis;
            
            var prompt = new CompletionPrompt(systemMessage, userTurn, metadata);

            prompts.Add(prompt);
        }

        return prompts;
    }
}
