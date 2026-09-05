using ThirteenThirtyOne.Application.DevelopmentGameplay;
using ThirteenThirtyOne.Game.Domain;
using ThirteenThirtyOne.Game.Engine;
using Xunit;

namespace ThirteenThirtyOne.Application.Tests;

public sealed class DevelopmentGameplayTests
{
    private static readonly CreateDevelopmentGameCommand Create = new("game", ["a", "b"], new string('0', 64));

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task ExplicitSeedCreatesIdenticalGamesInIndependentStores(int playerCount)
    {
        var command = Create with { PlayerIds = Enumerable.Range(0, playerCount).Select(index => $"player-{index}").ToArray() };
        var first = await new DevelopmentGameplayService(new TestStore()).CreateGameAsync(command, default);
        var second = await new DevelopmentGameplayService(new TestStore()).CreateGameAsync(command, default);
        Assert.True(first.Accepted && second.Accepted);
        Assert.Equal(playerCount, first.Game!.Players.Length);
        Assert.Equal(first.Game.StateHash, second.Game!.StateHash);
        Assert.Equal(first.EventTypes.ToArray(), second.EventTypes.ToArray());
    }

    [Fact]
    public async Task CreateGetDuplicateAndDelete()
    {
        var store = new TestStore();
        var service = new DevelopmentGameplayService(store);
        var result = await service.CreateGameAsync(Create, default);
        Assert.True(result.Accepted);
        Assert.Equal("WaitPlayerAction", result.Game!.Boundary);
        Assert.Equal(2, result.Game.Players.Length);
        Assert.Equal(result.Game.StateHash, (await service.GetGameAsync("game", default)).Game!.StateHash);
        Assert.Equal("DuplicateGameId", (await service.CreateGameAsync(Create, default)).Rejection);
        Assert.True((await service.DeleteAsync("game", default)).Accepted);
        Assert.Equal("GameNotFound", (await service.GetGameAsync("game", default)).Rejection);
        Assert.Equal("GameNotFound", (await service.DeleteAsync("game", default)).Rejection);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("00")]
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz")]
    public async Task InvalidSeedsNeverReachStore(string? seed)
    {
        var store = new TestStore();
        var result = await new DevelopmentGameplayService(store).CreateGameAsync(Create with { SeedHex = seed }, default);
        Assert.Equal(DevelopmentResultKind.InvalidRequest, result.Kind);
        Assert.Null(store.Session);
    }

    [Fact]
    public async Task InvalidIdentifiersRostersAndInputSyntaxAreRejected()
    {
        var service = new DevelopmentGameplayService(new TestStore());
        foreach (var command in new[] { Create with { GameId = " " }, Create with { PlayerIds = null },
            Create with { PlayerIds = ["a"] }, Create with { PlayerIds = ["a", "a"] },
            Create with { PlayerIds = ["a", " "] }, Create with { PlayerIds = ["a", "b", "c", "d", "e"] } })
        {
            Assert.Equal(DevelopmentResultKind.InvalidRequest, (await service.CreateGameAsync(command, default)).Kind);
        }
        foreach (var command in new[] { new SubmitDevelopmentDecisionCommand("game", 0, "a", DevelopmentPlayerAction.Draw),
            new("game", 1, "", DevelopmentPlayerAction.Draw), new("game", 1, "a", (DevelopmentPlayerAction)99),
            new("game", 1, "a", DevelopmentPlayerAction.SelectTarget), new("game", 1, "a", DevelopmentPlayerAction.Stop, "b") })
        {
            Assert.Equal(DevelopmentResultKind.InvalidRequest, (await service.SubmitDecisionAsync(command, default)).Kind);
        }
        Assert.Equal(DevelopmentResultKind.InvalidRequest,
            (await service.ExpireDecisionAsync(new("game", 1, (DevelopmentDecisionKind)99), default)).Kind);
    }

    [Fact]
    public async Task StopPreservesSafeBoundaryAndContinueIsExplicit()
    {
        var store = new TestStore();
        var service = new DevelopmentGameplayService(store);
        var start = await service.CreateGameAsync(Create, default);
        var decision = start.Game!.PendingDecision!;
        var stopped = await service.SubmitDecisionAsync(new("game", decision.DecisionId, decision.OwnerPlayerId, DevelopmentPlayerAction.Stop), default);
        Assert.Equal("SafePostResolution", stopped.Game!.Boundary);
        Assert.Null(stopped.Game.PendingDecision);
        Assert.Equal(1, store.Replacements);
        Assert.Equal("WaitPlayerAction", (await service.ContinueAsync("game", default)).Game!.Boundary);
        Assert.Equal(2, store.Replacements);
    }

    [Fact]
    public async Task RejectionsPreserveHashAndDoNotWrite()
    {
        var store = new TestStore();
        var service = new DevelopmentGameplayService(store);
        var start = await service.CreateGameAsync(Create, default);
        var decision = start.Game!.PendingDecision!;
        var stale = await service.SubmitDecisionAsync(new("game", decision.DecisionId + 1, decision.OwnerPlayerId, DevelopmentPlayerAction.Draw), default);
        Assert.Equal("DecisionMismatch", stale.Rejection);
        var wrong = await service.SubmitDecisionAsync(new("game", decision.DecisionId, "outsider", DevelopmentPlayerAction.Draw), default);
        Assert.Equal("WrongDecisionOwner", wrong.Rejection);
        var continuation = await service.ContinueAsync("game", default);
        Assert.Equal("ContinuationNotAllowed", continuation.Rejection);
        Assert.All(new[] { stale, wrong, continuation }, result =>
        {
            Assert.Equal(start.Game.StateHash, result.Game!.StateHash);
            Assert.Empty(result.EventTypes);
        });
        Assert.Equal(0, store.Replacements);
    }

    [Fact]
    public async Task ConflictReturnsCurrentStateAndNeverRetriesInput()
    {
        var store = new TestStore { RejectReplacement = true };
        var service = new DevelopmentGameplayService(store);
        var start = await service.CreateGameAsync(Create, default);
        var decision = start.Game!.PendingDecision!;
        var result = await service.SubmitDecisionAsync(new("game", decision.DecisionId, decision.OwnerPlayerId, DevelopmentPlayerAction.Stop), default);
        Assert.Equal("ConcurrencyConflict", result.Rejection);
        Assert.NotEqual(start.Game.StateHash, result.Game!.StateHash);
        Assert.Equal(store.Session!.StateHash, result.Game.StateHash);
        Assert.Equal("SafePostResolution", result.Game.Boundary);
        Assert.Equal(1, store.Replacements);
        Assert.Empty(result.EventTypes);
    }

    [Fact]
    public async Task DrawTargetsAndBothTimeoutKindsMatchEngineExactly()
    {
        var store = new TestStore();
        var service = new DevelopmentGameplayService(store);
        await service.CreateGameAsync(Create, default);
        var targetSeen = false;
        var actionTimeoutSeen = false;
        var targetTimeoutSeen = false;
        for (var step = 0; step < 200 && !(targetSeen && actionTimeoutSeen && targetTimeoutSeen); step++)
        {
            var prior = store.Session!;
            var pending = prior.State.PendingDecision;
            EngineInput input;
            Task<DevelopmentGameOperationResult> operation;
            if (pending is null)
            {
                input = new ContinueAutomaticResolution();
                operation = service.ContinueAsync("game", default);
            }
            else if (pending.Kind == DecisionKind.EffectTarget && !targetSeen)
            {
                targetSeen = true;
                input = new PlayerDecision(pending.Id, pending.Owner, PlayerAction.SelectTarget, pending.Owner);
                operation = service.SubmitDecisionAsync(new("game", pending.Id.Value, pending.Owner.Value,
                    DevelopmentPlayerAction.SelectTarget, pending.Owner.Value), default);
            }
            else if (pending.Kind == DecisionKind.EffectTarget || !actionTimeoutSeen)
            {
                targetTimeoutSeen |= pending.Kind == DecisionKind.EffectTarget;
                actionTimeoutSeen |= pending.Kind == DecisionKind.PlayerAction;
                input = new GameplayTimerExpired(pending.Id, pending.Kind);
                operation = service.ExpireDecisionAsync(new("game", pending.Id.Value,
                    pending.Kind == DecisionKind.PlayerAction ? DevelopmentDecisionKind.PlayerAction : DevelopmentDecisionKind.EffectTarget), default);
            }
            else
            {
                input = new PlayerDecision(pending.Id, pending.Owner, PlayerAction.Draw);
                operation = service.SubmitDecisionAsync(new("game", pending.Id.Value, pending.Owner.Value, DevelopmentPlayerAction.Draw), default);
            }
            var expected = GameEngine.Apply(prior.State, input);
            var actual = await operation;
            Assert.True(actual.Accepted);
            Assert.Equal(expected.StateValidationHash, actual.Game!.StateHash);
            Assert.Equal(expected.Events.Select(item => item.GetType().Name), actual.EventTypes.ToArray());
        }
        Assert.True(targetSeen && actionTimeoutSeen && targetTimeoutSeen);
    }

    [Fact]
    public async Task CancellationPropagatesThroughAllOperations()
    {
        var store = new TestStore();
        var service = new DevelopmentGameplayService(store);
        using var cancellation = new CancellationTokenSource();
        await service.CreateGameAsync(Create, cancellation.Token);
        Assert.Equal(cancellation.Token, store.LastToken);
        await cancellation.CancelAsync();
        await Assert.ThrowsAsync<OperationCanceledException>(() => service.CreateGameAsync(Create, cancellation.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => service.GetGameAsync("game", cancellation.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => service.SubmitDecisionAsync(new("game", 1, "a", DevelopmentPlayerAction.Draw), cancellation.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => service.ExpireDecisionAsync(new("game", 1, DevelopmentDecisionKind.PlayerAction), cancellation.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => service.ContinueAsync("game", cancellation.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => service.DeleteAsync("game", cancellation.Token));
        Assert.Equal(0, store.Replacements);
    }

    private sealed class TestStore : IGameSessionStore
    {
        public StoredGameSession? Session { get; private set; }
        public int Replacements { get; private set; }
        public bool RejectReplacement { get; init; }
        public CancellationToken LastToken { get; private set; }
        public ValueTask<StoredGameSession?> GetAsync(string gameId, CancellationToken cancellationToken)
        {
            LastToken = cancellationToken;
            return ValueTask.FromResult(Session);
        }
        public ValueTask<bool> TryCreateAsync(StoredGameSession session, CancellationToken cancellationToken)
        {
            LastToken = cancellationToken;
            if (Session is not null) { return ValueTask.FromResult(false); }
            Session = session;
            return ValueTask.FromResult(true);
        }
        public ValueTask<bool> TryReplaceAsync(string gameId, string expectedStateHash, StoredGameSession replacement, CancellationToken cancellationToken)
        {
            LastToken = cancellationToken;
            Replacements++;
            Assert.Equal(Session!.StateHash, expectedStateHash);
            if (RejectReplacement)
            {
                // A competing request commits H2 after this request loaded H1.
                var pending = Session.State.PendingDecision!;
                var competitor = GameEngine.Apply(Session.State, new PlayerDecision(pending.Id, pending.Owner, PlayerAction.Stop));
                Session = new(competitor.State, competitor.StateValidationHash);
                return ValueTask.FromResult(false);
            }
            Session = replacement;
            return ValueTask.FromResult(true);
        }
        public ValueTask<bool> DeleteAsync(string gameId, CancellationToken cancellationToken)
        {
            LastToken = cancellationToken;
            var existed = Session is not null;
            Session = null;
            return ValueTask.FromResult(existed);
        }
    }
}
