using System.Collections.Concurrent;
using System.Text.Json;
using GenAI.Bridge.Context;

namespace GenAI.Bridge.Providers.InMemory;

public sealed class InMemoryContextStore : IInMemContextStore
{
    private sealed record Bucket(ConcurrentDictionary<string, string> Items);

    private readonly ConcurrentDictionary<string, Bucket> _buckets = new();
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.General);

    private Bucket Get(string id) => _buckets.GetOrAdd(id,
        _ => new Bucket(new ConcurrentDictionary<string, string>()));

    public Task SaveItemAsync<T>(string s, string k, T v, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        Get(s).Items[k] = JsonSerializer.Serialize(v, _json);
        return Task.CompletedTask;
    }

    public Task<T?> LoadItemAsync<T>(string s, string k, CancellationToken ct = default)
        => Task.FromResult(Get(s).Items.TryGetValue(k, out var j) ? JsonSerializer.Deserialize<T>(j, _json) : default);
}