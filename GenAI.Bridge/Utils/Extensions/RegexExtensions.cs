using System.Text;
using System.Text.RegularExpressions;

namespace GenAI.Bridge.Utils.Extensions;

internal static class RegexExtensions
{
    /// <summary>
    /// Polyfill for Regex.ReplaceAsync that is available only in .NET 8+. Executes the asynchronous
    /// replacement delegate sequentially on every match.
    /// </summary>
    internal static async Task<string> ReplaceAsync(
        this Regex regex, string input, Func<Match, Task<string>> replacer, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(regex);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(replacer);

        var result = new StringBuilder();
        var lastIndex = 0;
        foreach (Match match in regex.Matches(input))
        {
            ct.ThrowIfCancellationRequested();
            result.Append(input, lastIndex, match.Index - lastIndex);
            result.Append(await replacer(match));
            lastIndex = match.Index + match.Length;
        }

        result.Append(input, lastIndex, input.Length - lastIndex);
        return result.ToString();
    }
}