using System.Globalization;
using ThirteenThirtyOne.Game.Domain;
using ThirteenThirtyOne.Game.Domain.Events;
using Xunit;

namespace ThirteenThirtyOne.Game.Engine.Tests;

public sealed class DeterministicReplayTests
{
    public static TheoryData<int, int> Games
    {
        get
        {
            var data = new TheoryData<int, int>();
            for (var players = 2; players <= 4; players++)
            {
                for (var seed = 0; seed < 8; seed++)
                {
                    data.Add(players, seed);
                }
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Games))]
    public void CompleteSeededGamesReplayEveryHashEventBoundaryAndWinner(int count, int seedValue)
    {
        var seed = new byte[32];
        seed[0] = (byte)seedValue;
        var roster = Enumerable.Range(0, count).Select(index => new PlayerId(((char)('A' + index)).ToString())).ToArray();
        var initial = GameEngine.CreateGame(new GameId("replay"), roster, new RandomState(seed), EngineCompatibility.V1);
        var state = initial.State;
        var inputs = new List<EngineInput>();
        var results = new List<EngineTransitionResult>();
        while (state.Boundary != BoundaryKind.GameTerminal && inputs.Count < 10000)
        {
            var input = NextInput(state);
            var beforeHash = StateHasher.Compute(state);
            var result = GameEngine.Apply(state, input);
            Assert.True(result.IsAccepted);
            Assert.Equal(beforeHash, StateHasher.Compute(state));
            StateValidator.Validate(result.State);
            Assert.True(result.State.DecisionSequence >= state.DecisionSequence);
            Assert.True(result.State.ContextSequence >= state.ContextSequence);
            Assert.True(result.State.Random.WordPosition >= state.Random.WordPosition);
            Assert.Equal(initial.State.Seats.Players.ToArray(), result.State.Seats.Players.ToArray());

            if (state.RoundKind == RoundKind.TieBreak)
            {
                Assert.Equal(state.Players.Select(player => player.TotalScore).ToArray(),
                    result.State.Players.Select(player => player.TotalScore).ToArray());
            }

            if (state.RoundNumber == result.State.RoundNumber)
            {
                for (var index = 0; index < count; index++)
                {
                    Assert.False(!state.Players[index].IsActive && result.State.Players[index].IsActive);
                }
            }

            inputs.Add(input);
            results.Add(result);
            state = result.State;
        }

        Assert.Equal(BoundaryKind.GameTerminal, state.Boundary);
        Assert.NotNull(state.Winner);
        var replay = GameEngine.CreateGame(new GameId("replay"), roster, new RandomState(seed), EngineCompatibility.V1);
        AssertEquivalent(initial, replay);
        for (var index = 0; index < inputs.Count; index++)
        {
            // Reconstruct every snapshot, including pending nested effects and partial RNG blocks.
            var reconstructed = SnapshotCopy.Reconstruct(replay.State);
            Assert.NotSame(replay.State, reconstructed);
            Assert.Equal(replay.StateValidationHash, StateHasher.Compute(reconstructed));
            replay = GameEngine.Apply(reconstructed, inputs[index]);
            AssertEquivalent(results[index], replay);
        }

        Assert.Equal(state.Winner, replay.State.Winner);
        Assert.Equal(state.Players.Select(player => player.TotalScore), replay.State.Players.Select(player => player.TotalScore));
    }

    [Fact]
    public void StateHashIsCultureIndependentAndIncludesRandomnessAndCompatibility()
    {
        var state = GameEngine.CreateGame(new GameId("hash"), [new PlayerId("А"), new PlayerId("B")],
            new RandomState(new byte[32]), EngineCompatibility.V1).State;
        var hash = StateHasher.Compute(state);
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            foreach (var culture in new[] { "bg-BG", "ar-SA", "tr-TR" })
            {
                CultureInfo.CurrentCulture = new CultureInfo(culture);
                Assert.Equal(hash, StateHasher.Compute(SnapshotCopy.Reconstruct(state)));
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }

        var altered = new Transition(state);
        altered.Random.NextUInt32();
        Assert.NotEqual(hash, StateHasher.Compute(altered.Freeze()));
        var incompatible = new GameplayState(state.GameId, state.Compatibility with { ShuffleAlgorithmVersion = 2 }, state.Seats,
            state.RoundNumber, state.RoundKind, state.RoundStarter, state.TurnOwner, state.Players, state.DrawPile,
            state.DiscardPile, state.OpeningSetAside, state.ResolutionStack, state.PendingDecision, state.Boundary,
            state.DecisionSequence, state.ContextSequence, state.Random, state.Winner, state.CompletedTurnOwner);
        Assert.NotEqual(hash, StateHasher.Compute(incompatible));
        Assert.Throws<NotSupportedException>(() => GameEngine.Apply(incompatible, new ContinueAutomaticResolution()));
    }

    internal static EngineInput NextInput(GameplayState state)
    {
        if (state.Boundary == BoundaryKind.SafePostResolution)
        {
            return new ContinueAutomaticResolution();
        }

        var decision = state.PendingDecision!;
        if (decision.Id.Value % 11 == 0)
        {
            return new GameplayTimerExpired(decision.Id, decision.Kind);
        }

        if (decision.Kind == DecisionKind.PlayerAction)
        {
            var action = state.Player(decision.Owner).CurrentScore >= 20 ? PlayerAction.Stop : PlayerAction.Draw;
            return new PlayerDecision(decision.Id, decision.Owner, action);
        }

        var target = decision.AllowedTargets[(int)(decision.Id.Value % decision.AllowedTargets.Length)];
        return new PlayerDecision(decision.Id, decision.Owner, PlayerAction.SelectTarget, target);
    }

    private static void AssertEquivalent(EngineTransitionResult expected, EngineTransitionResult actual)
    {
        Assert.Equal(expected.StateValidationHash, actual.StateValidationHash);
        Assert.Equal(expected.Boundary, actual.Boundary);
        Assert.Equal(expected.IsSafeGameplayBoundary, actual.IsSafeGameplayBoundary);
        Assert.Equal(expected.Events.ToArray(), actual.Events.ToArray());
        Assert.Equal(expected.State.Random.WordPosition, actual.State.Random.WordPosition);
        Assert.Equal(expected.PendingDecision?.Id, actual.PendingDecision?.Id);
        Assert.Equal(expected.PendingDecision?.Owner, actual.PendingDecision?.Owner);
    }
}
