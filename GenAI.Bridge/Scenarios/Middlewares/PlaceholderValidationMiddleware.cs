using System.Text.RegularExpressions;

namespace GenAI.Bridge.Scenarios.Middlewares;

public sealed partial class PlaceholderValidationMiddleware : IStageMiddleware
{
    public Task InvokeAsync(StageExecutionContext ctx, Func<Task> next, CancellationToken ct)
    {
        var unresolvedPlaceholders = ctx.Stage.GetUserTurns()
            .SelectMany(turn => PlaceHolderRegex().Matches(turn.Content).Select(m => m.Value))
            .Distinct()
            .ToList();

        if (unresolvedPlaceholders.Count != 0)
            throw new InvalidOperationException(
                $"Unresolved placeholder in stage '{ctx.Stage.Name}'. " +
                $"Unresolved placeholders: {string.Join(", ", unresolvedPlaceholders)}");

        return next();
    }

    [GeneratedRegex(@"\{\{[a-zA-Z_][a-zA-Z0-9_]*\}\}|\{[a-zA-Z_][a-zA-Z0-9_]*\}", RegexOptions.Compiled)]
    private static partial Regex PlaceHolderRegex();
}