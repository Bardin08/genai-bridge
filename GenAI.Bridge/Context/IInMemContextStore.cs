namespace GenAI.Bridge.Context;

public interface IInMemContextStore
{
    /* ───────────────────────────────────────── Generic */
    Task SaveItemAsync<T>(string sessionId, string key, T value, TimeSpan? ttl = null, CancellationToken ct = default);
    Task<T?> LoadItemAsync<T>(string sessionId, string key, CancellationToken ct = default);
}