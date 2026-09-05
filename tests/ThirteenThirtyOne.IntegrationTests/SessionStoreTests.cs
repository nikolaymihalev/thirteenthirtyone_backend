using ThirteenThirtyOne.Application.DevelopmentGameplay;
using ThirteenThirtyOne.Game.Domain;
using ThirteenThirtyOne.Game.Engine;
using ThirteenThirtyOne.Infrastructure;
using Xunit;

namespace ThirteenThirtyOne.IntegrationTests;

public sealed class SessionStoreTests
{
    private static StoredGameSession Session(string id = "game")
    {
        var result = GameEngine.CreateGame(new GameId(id), [new PlayerId("a"), new PlayerId("b")],
            new RandomState(new byte[32]), EngineCompatibility.V1);
        return new(result.State, result.StateValidationHash);
    }

    [Fact]
    public async Task LifecycleUsesOrdinalIdsAndImmutableSnapshots()
    {
        var store = new InMemoryGameSessionStore();
        var session = Session();
        Assert.Null(await store.GetAsync("game", default));
        Assert.True(await store.TryCreateAsync(session, default));
        Assert.False(await store.TryCreateAsync(session, default));
        Assert.True(await store.TryCreateAsync(Session("GAME"), default));
        Assert.Same(session, await store.GetAsync("game", default));
        Assert.False(await store.TryReplaceAsync("game", "stale", session, default));
        Assert.True(await store.DeleteAsync("game", default));
        Assert.False(await store.DeleteAsync("game", default));
        Assert.NotNull(await store.GetAsync("GAME", default));
    }

    [Fact]
    public async Task ConcurrentCreationHasExactlyOneWinner()
    {
        var store = new InMemoryGameSessionStore();
        var session = Session();
        var results = await Task.WhenAll(Enumerable.Range(0, 32)
            .Select(_ => Task.Run(async () => await store.TryCreateAsync(session, default))));
        Assert.Single(results, result => result);
    }

    [Fact]
    public async Task ConcurrentReplacementsWithSameExpectedHashHaveOneWinner()
    {
        var store = new InMemoryGameSessionStore();
        var session = Session();
        await store.TryCreateAsync(session, default);
        var results = await Task.WhenAll(Enumerable.Range(0, 32).Select(index => Task.Run(async () =>
            await store.TryReplaceAsync("game", session.StateHash, new(session.State, $"replacement-{index}"), default))));
        Assert.Single(results, result => result);
        Assert.NotEqual(session.StateHash, (await store.GetAsync("game", default))!.StateHash);
        Assert.Equal(StateHasher.Compute(session.State), session.StateHash);
    }

    [Fact]
    public async Task CancelledOperationsDoNotChangeStore()
    {
        var store = new InMemoryGameSessionStore();
        var session = Session();
        var token = new CancellationToken(true);
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await store.TryCreateAsync(session, token));
        await store.TryCreateAsync(session, default);
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await store.GetAsync("game", token));
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await store.TryReplaceAsync("game", session.StateHash, session, token));
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await store.DeleteAsync("game", token));
        Assert.Same(session, await store.GetAsync("game", default));
    }
}


