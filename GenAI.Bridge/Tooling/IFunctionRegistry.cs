using System.Text.Json;

namespace GenAI.Bridge.Tooling;

/// <summary>
/// Central contract for registering and resolving local functions that a model
/// may invoke via the “tools / function-calling” mechanism.
/// </summary>
public interface IFunctionRegistry
{
    /// <summary>Adds or replaces a function.</summary>
    /// <param name="name">Case-insensitive function name.</param>
    /// <param name="impl">Delegate that receives the model-supplied JSON arguments and
    ///                    returns JSON that will be sent back to the model.</param>
    void Register(string name, Func<JsonElement, string> impl);

    /// <summary>Tries to get a registered function.</summary>
    /// <returns><c>true</c> if the function exists.</returns>
    bool TryGet(string name, out Func<JsonElement, string> impl);

    /// <summary>List of all currently registered names.</summary>
    IReadOnlyCollection<string> RegisteredNames { get; }
}