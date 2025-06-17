using System.Text.Json;
using GenAI.Bridge.Abstractions;
using GenAI.Bridge.Contracts;
using StackExchange.Redis;

namespace GenAI.Bridge.Context;

/// <summary>
/// Redis-backed implementation of <see cref="IContextStore"/> for storing conversation history
/// with support for:
/// <list type="bullet">
///   <item>Multi-turn chat/session storage</item>
///   <item>Sliding window (ListTrim) for bounded history</item>
///   <item>Configurable TTL (per-session expiry)</item>
///   <item>Async and cancellation support</item>
/// </list>
/// 
/// <para>
/// <example>
/// <code>
/// // Create a RedisContextStore with default options
/// var contextStore = new RedisContextStore(new RedisContextStore.Options
/// {
///     ConnectionString = "localhost:6379",
///     KeyPrefix = "myapp:context:"
/// });
/// 
/// // Save a conversation turn with 1 hour TTL
/// await contextStore.SaveTurnAsync(
///     sessionId: "user123",
///     turn: new PromptTurn("user", "Hello AI!"),
///     ttl: TimeSpan.FromHours(1)
/// );
/// 
/// // Load the most recent 10 turns
/// var turns = await contextStore.LoadTurnsAsync("user123", 10);
/// </code>
/// </example>
/// </para>
/// 
/// <remarks>
/// Redis key format is <c>{KeyPrefix}{sessionId}</c> and stores conversation turns as a Redis list.
/// The most recent turns are always at the start of the list (index 0).
/// TTL is applied to the entire session key.
/// </remarks>
/// </summary>
public class RedisContextStore : IContextStore, IDisposable
{
    private readonly ConnectionMultiplexer _redis;
    private readonly string _keyPrefix;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly Options _options;
    private bool _disposed;
    
    /// <summary>
    /// Configuration options for <see cref="RedisContextStore"/>
    /// </summary>
    public class Options
    {
        /// <summary>
        /// Prefix for Redis keys to avoid collisions with other applications
        /// </summary>
        /// <remarks>
        /// All Redis keys for this context store will be prefixed with this value.
        /// Example: "myapp:context:" will result in keys like "myapp:context:session123"
        /// </remarks>
        public string KeyPrefix { get; init; } = "genai:context:";
        
        /// <summary>
        /// Default TTL for sessions if not specified explicitly
        /// </summary>
        /// <remarks>
        /// Each time a new turn is added to a session, the TTL is reset to this value.
        /// Consider your usage patterns when setting this. For interactive chat applications,
        /// a few hours might be appropriate. For longer-term storage, consider days or weeks.
        /// </remarks>
        public TimeSpan DefaultTtl { get; init; } = TimeSpan.FromHours(1);
        
        /// <summary>
        /// Default maximum number of turns to keep per session
        /// </summary>
        /// <remarks>
        /// This limits the amount of conversation history stored in Redis.
        /// Older turns beyond this limit will be trimmed automatically.
        /// </remarks>
        public int DefaultMaxTurns { get; init; } = 10;
    }
    
    /// <summary>
    /// Initializes a new instance of <see cref="RedisContextStore"/>
    /// </summary>
    /// <param name="options">Configuration options</param>
    /// <exception cref="ArgumentNullException">When <paramref name="options"/> is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">When default TTL or max turns is invalid</exception>
    public RedisContextStore(ConnectionMultiplexer redis, Options options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrEmpty(options.KeyPrefix);
        
        if (options.DefaultTtl <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options.DefaultTtl), "Default TTL must be positive");
        }
        
        if (options.DefaultMaxTurns <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.DefaultMaxTurns), "Default maximum turns must be positive");
        }
        
        _options = options;
        _redis = redis;
        _keyPrefix = options.KeyPrefix;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
    }

    /// <inheritdoc />
    /// <summary>
    /// Saves a single conversation turn for a session.
    /// </summary>
    /// <param name="sessionId">Unique identifier for the conversation session</param>
    /// <param name="turn">The conversation turn to save</param>
    /// <param name="ttl">Time-to-live for the session (applies to all turns in the session)</param>
    /// <param name="ct">Optional cancellation token</param>
    /// <exception cref="ArgumentException">When <paramref name="sessionId"/> is null or empty</exception>
    /// <exception cref="ArgumentNullException">When <paramref name="turn"/> is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="ttl"/> is not positive</exception>
    /// <exception cref="OperationCanceledException">When operation is canceled via <paramref name="ct"/></exception>
    /// <exception cref="InvalidOperationException">When Redis transaction fails</exception>
    public async Task SaveTurnAsync(string sessionId, PromptTurn turn, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(sessionId);
        ArgumentNullException.ThrowIfNull(turn);
        
        ttl ??= _options.DefaultTtl;
        if (ttl <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(ttl), "TTL must be positive");
        }
        
        // Check for cancellation
        ct.ThrowIfCancellationRequested();

        var sessionKey = GetSessionKey(sessionId);
        var turnJson = SerializePromptTurn(turn);

        var db = _redis.GetDatabase();
        var transaction = db.CreateTransaction();
        
        _ = transaction.ListLeftPushAsync(sessionKey, turnJson);
        _ = transaction.KeyExpireAsync(sessionKey, ttl);

        var success = await transaction.ExecuteAsync();
        if (!success)
        {
            throw new InvalidOperationException($"Failed to save turn for session {sessionId}");
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Loads the most recent turns for a session, up to the specified maximum.
    /// Also trims the Redis list to maintain the sliding window of conversation history.
    /// </summary>
    /// <param name="sessionId">Unique identifier for the conversation session</param>
    /// <param name="maxTurns">Maximum number of turns to retrieve and keep</param>
    /// <param name="ct">Optional cancellation token</param>
    /// <returns>List of the most recent turns, newest first</returns>
    /// <exception cref="ArgumentException">When <paramref name="sessionId"/> is null or empty</exception>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="maxTurns"/> is not positive</exception>
    /// <exception cref="OperationCanceledException">When operation is canceled via <paramref name="ct"/></exception>
    public async Task<IReadOnlyList<PromptTurn>> LoadTurnsAsync(string sessionId, int? maxTurns = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(sessionId);

        maxTurns ??= _options.DefaultMaxTurns;
        if (maxTurns <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxTurns), "Maximum turns must be positive");
        }

        ct.ThrowIfCancellationRequested();

        var sessionKey = GetSessionKey(sessionId);
        
        var db = _redis.GetDatabase();

        var listLength = await db.ListLengthAsync(sessionKey);
        if (listLength == 0)
        {
            return [];
        }
        
        var jsonTurns = await db.ListRangeAsync(sessionKey, 0, maxTurns.Value - 1);
        if (listLength > maxTurns)
        {
            await db.ListTrimAsync(sessionKey, 0, maxTurns.Value - 1);
        }

        var turns = new List<PromptTurn>(jsonTurns.Length);
        
        foreach (var jsonTurn in jsonTurns)
        {
            if (jsonTurn.IsNullOrEmpty)
            {
                continue;
            }

            ct.ThrowIfCancellationRequested();
            
            var turn = DeserializePromptTurn(jsonTurn!);
            if (turn != null)
            {
                turns.Add(turn);
            }
        }
        
        return turns;
    }
    
    private string SerializePromptTurn(PromptTurn turn)
    {
        return JsonSerializer.Serialize(turn, _jsonOptions);
    }

    private PromptTurn? DeserializePromptTurn(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<PromptTurn>(json, _jsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private string GetSessionKey(string sessionId) => $"{_keyPrefix}{sessionId}";
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }
        
        if (disposing)
        {
            _redis?.Dispose();
        }
        
        _disposed = true;
    }
}
