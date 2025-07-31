using System.Text.Json;

namespace GenAI.Bridge.Tooling;

/// <inheritdoc />
public sealed class FunctionRegistry : IFunctionRegistry
{
    private readonly Dictionary<string, Func<JsonElement, string>> _map =
        new(StringComparer.OrdinalIgnoreCase);

    public void Register(string name, Func<JsonElement, string> impl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _map[name] = impl ?? throw new ArgumentNullException(nameof(impl));
    }

    public bool TryGet(string name, out Func<JsonElement, string> impl)
        => _map.TryGetValue(name, out impl!);

    public IReadOnlyCollection<string> RegisteredNames => _map.Keys.ToArray();
}