using GenAI.Bridge.Context;
using GenAI.Bridge.Contracts;
using GenAI.Bridge.Contracts.Prompts;
using StackExchange.Redis;

namespace GenAI.Bridge.Integration.Tests;

public class RedisContextStoreTests
{
    private static readonly string RedisUrl =
        Environment.GetEnvironmentVariable("REDIS_URL") ?? "localhost:6379";

    private static readonly string TestSessionId =
        $"test_session_{Guid.NewGuid():N}";

    private readonly RedisContextStore _contextStore;

    public RedisContextStoreTests()
    {
        // Create a new RedisContextStore for each test
        var options = new RedisContextStore.Options
        {
            KeyPrefix = "genai:test:",
            DefaultTtl = TimeSpan.FromMinutes(5),
            DefaultMaxTurns = 5
        };

        var redis = ConnectionMultiplexer.Connect(RedisUrl);

        _contextStore = new RedisContextStore(redis, options);
    }

    [Fact]
    public async Task SaveAndLoadTurns_Works()
    {
        // Arrange
        var turn1 = new PromptTurn("user", "Hello, how are you?");
        var turn2 = new PromptTurn("assistant", "I'm fine, thank you!");
        var turn3 = new PromptTurn("user", "What's the weather like today?");

        // Act - Save three turns
        await _contextStore.SaveTurnAsync(TestSessionId, turn1, TimeSpan.FromMinutes(5));
        await _contextStore.SaveTurnAsync(TestSessionId, turn2, TimeSpan.FromMinutes(5));
        await _contextStore.SaveTurnAsync(TestSessionId, turn3, TimeSpan.FromMinutes(5));

        // Act - Load turns
        var loadedTurns = await _contextStore.LoadTurnsAsync(TestSessionId, 10);

        // Assert
        Assert.Equal(3, loadedTurns.Count);
        Assert.Equal(turn3.Role, loadedTurns[0].Role);
        Assert.Equal(turn3.Content, loadedTurns[0].Content);
        Assert.Equal(turn2.Role, loadedTurns[1].Role);
        Assert.Equal(turn2.Content, loadedTurns[1].Content);
        Assert.Equal(turn1.Role, loadedTurns[2].Role);
        Assert.Equal(turn1.Content, loadedTurns[2].Content);
    }

    [Fact]
    public async Task SlidingWindow_TrimsOldestTurns()
    {
        // Arrange
        const int maxTurns = 3;

        var turn1 = new PromptTurn("user", "First message");
        var turn2 = new PromptTurn("assistant", "First response");
        var turn3 = new PromptTurn("user", "Second message");
        var turn4 = new PromptTurn("assistant", "Second response");
        var turn5 = new PromptTurn("user", "Third message");

        // Act - Save five turns but limit to 3
        await _contextStore.SaveTurnAsync(TestSessionId, turn1, TimeSpan.FromMinutes(5));
        await _contextStore.SaveTurnAsync(TestSessionId, turn2, TimeSpan.FromMinutes(5));
        await _contextStore.SaveTurnAsync(TestSessionId, turn3, TimeSpan.FromMinutes(5));
        await _contextStore.SaveTurnAsync(TestSessionId, turn4, TimeSpan.FromMinutes(5));
        await _contextStore.SaveTurnAsync(TestSessionId, turn5, TimeSpan.FromMinutes(5));

        // Load with sliding window of 3
        var loadedTurns = await _contextStore.LoadTurnsAsync(TestSessionId, maxTurns);

        // Assert - Only the 3 most recent turns should be returned
        Assert.Equal(maxTurns, loadedTurns.Count);
        Assert.Equal(turn5.Content, loadedTurns[0].Content);
        Assert.Equal(turn4.Content, loadedTurns[1].Content);
        Assert.Equal(turn3.Content, loadedTurns[2].Content);

        // The first two turns should have been trimmed
        Assert.DoesNotContain(loadedTurns, t => t.Content == turn1.Content);
        Assert.DoesNotContain(loadedTurns, t => t.Content == turn2.Content);
    }

    [Fact]
    public async Task Ttl_ExpiresTurnsAfterTimeout()
    {
        // Arrange
        var turn = new PromptTurn("user", "Short-lived message");
        var shortTtl = TimeSpan.FromSeconds(1);

        // Act
        await _contextStore.SaveTurnAsync(TestSessionId, turn, shortTtl);

        // Assert - Initially the turn should be available
        var turnsBeforeExpiry = await _contextStore.LoadTurnsAsync(TestSessionId, 10);
        Assert.Single(turnsBeforeExpiry);
        Assert.Equal(turn.Content, turnsBeforeExpiry[0].Content);

        // Wait for TTL to expire
        await Task.Delay(TimeSpan.FromSeconds(2)); // Wait longer than the TTL

        // After TTL expires, no turns should be available
        var turnsAfterExpiry = await _contextStore.LoadTurnsAsync(TestSessionId, 10);
        Assert.Empty(turnsAfterExpiry);
    }

    [Fact]
    public async Task CancellationToken_ImmediatelyCancels()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync(); // Cancel immediately

        var turn = new PromptTurn("user", "This shouldn't be saved");

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await _contextStore.SaveTurnAsync(TestSessionId, turn, TimeSpan.FromMinutes(5), cts.Token);
        });

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await _contextStore.LoadTurnsAsync(TestSessionId, 10, cts.Token);
        });
    }
}