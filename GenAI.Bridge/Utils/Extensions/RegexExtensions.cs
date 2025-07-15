using System.Text;
using System.Text.RegularExpressions;

namespace GenAI.Bridge.Utils.Extensions;

internal static partial class RegexExtensions
{
    /// <summary>
    /// Substitutes parameters in the input string. Supports two formats:
    /// - {{var_name}} - regular parameters (isParam = false)
    /// - {var_name} - alternative format (isParam = true)
    /// </summary>
    internal static async Task<string> SubstituteParametersAsync(
        this string input, 
        Func<string, bool, Task<string>> replacer, 
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(replacer);


        var result = new StringBuilder();
        var lastIndex = 0;

        foreach (Match match in VariablesKeysRegex().Matches(input))
        {
            ct.ThrowIfCancellationRequested();
            
            // Append text before the match
            result.Append(input, lastIndex, match.Index - lastIndex);
            
            // Determine which format was matched and extract the variable name
            string varName;
            bool isParam;
            
            if (match.Groups[1].Success)
            {
                // {{var_name}} format
                varName = match.Groups[1].Value;
                isParam = false;
            }
            else
            {
                // }var_name{ format
                varName = match.Groups[2].Value;
                isParam = true;
            }
            
            var replacement = await replacer(varName, isParam);
            result.Append(replacement);
            
            lastIndex = match.Index + match.Length;
        }

        result.Append(input, lastIndex, input.Length - lastIndex);
        return result.ToString();
    }

    // Combined regex pattern to match both formats:
    // {{var_name}} - captured in group 1
    // {param_name} - captured in group 2
    [GeneratedRegex(@"\{\{([^}]+)\}\}|\{([^{]+)\}", RegexOptions.Compiled)]
    private static partial Regex VariablesKeysRegex();
}