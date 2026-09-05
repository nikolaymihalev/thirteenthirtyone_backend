using ThirteenThirtyOne.Game.Domain;
using Xunit;

namespace ThirteenThirtyOne.Game.Engine.Tests;

public sealed class DecisionValidationTests
{
    [Fact]
    public void WrongOwnerStaleDecisionWrongKindAndExtraneousTargetRejectWithoutAnyMutation()
    {
        var state = new Scenario().Player(0, 1, [1]).Player(1, 2, [2]).Build();
        var decision = state.PendingDecision!;
        EngineInput[] illegal =
        [
            new PlayerDecision(decision.Id, state.Seats.Players[1], PlayerAction.Draw),
            new PlayerDecision(decision.Id, state.Seats.Players[1], PlayerAction.Stop),
            new PlayerDecision(new DecisionId(99), decision.Owner, PlayerAction.Draw),
            new PlayerDecision(default, decision.Owner, PlayerAction.Stop),
            new PlayerDecision(decision.Id, decision.Owner, PlayerAction.SelectTarget, decision.Owner),
            new PlayerDecision(decision.Id, decision.Owner, (PlayerAction)99),
            new PlayerDecision(decision.Id, decision.Owner, PlayerAction.Draw, decision.Owner),
            new GameplayTimerExpired(decision.Id, DecisionKind.EffectTarget),
            new GameplayTimerExpired(new DecisionId(99), DecisionKind.PlayerAction),
            new ContinueAutomaticResolution(),
        ];
        foreach (var input in illegal)
        {
            AssertRejectedUnchanged(state, input);
        }
    }

    [Theory]
    [InlineData(PlayerRoundStatus.Stopped, 5)]
    [InlineData(PlayerRoundStatus.ForcedStop, 5)]
    [InlineData(PlayerRoundStatus.Bust13, 13)]
    [InlineData(PlayerRoundStatus.BustOver31, 32)]
    [InlineData(PlayerRoundStatus.Perfect31, 31)]
    [InlineData(PlayerRoundStatus.Zeroed, 0)]
    [InlineData(PlayerRoundStatus.NotParticipating, 0)]
    public void EveryInactiveStateIsAnIllegalTarget(PlayerRoundStatus status, int score)
    {
        var fixture = new Scenario(3) { Kind = status == PlayerRoundStatus.NotParticipating ? RoundKind.TieBreak : RoundKind.Normal };
        fixture.Player(0, 1, [1]).Player(1, score,
                status == PlayerRoundStatus.NotParticipating ? [] : status == PlayerRoundStatus.Bust13 ? [1, 12] : [2], status)
            .Player(2, 3, [3]);
        fixture.Draw(fixture.Effect(CardKind.PlusFive), fixture.Number(1));
        var waiting = Scenario.Act(fixture.Build(), PlayerAction.Draw).State;
        var decision = waiting.PendingDecision!;

        Assert.Equal(EngineRejection.IllegalTarget, AssertRejectedUnchanged(waiting,
            new PlayerDecision(decision.Id, decision.Owner, PlayerAction.SelectTarget, waiting.Seats.Players[1])).Rejection);
        AssertRejectedUnchanged(waiting, new PlayerDecision(decision.Id, decision.Owner, PlayerAction.SelectTarget, new PlayerId("unknown")));
        AssertRejectedUnchanged(waiting, new PlayerDecision(decision.Id, decision.Owner, PlayerAction.SelectTarget));
        AssertRejectedUnchanged(waiting, new PlayerDecision(decision.Id, waiting.Seats.Players[2], PlayerAction.SelectTarget, decision.Owner));
        AssertRejectedUnchanged(waiting, new PlayerDecision(decision.Id, decision.Owner, PlayerAction.Stop));
    }

    [Fact]
    public void StaleSecondActionAndLateTimeoutCannotResolveANewerDecision()
    {
        var start = new Scenario().Player(0, 1, [1]).Player(1, 2, [2]).Build();
        var original = new PlayerDecision(start.PendingDecision!.Id, start.PendingDecision.Owner, PlayerAction.Stop);
        var stopped = GameEngine.Apply(start, original);
        AssertRejectedUnchanged(stopped.State, original);
        var next = GameEngine.Apply(stopped.State, new ContinueAutomaticResolution());
        Assert.Equal(EngineRejection.DecisionMismatch, AssertRejectedUnchanged(next.State, original).Rejection);
        AssertRejectedUnchanged(next.State, new GameplayTimerExpired(start.PendingDecision.Id, DecisionKind.PlayerAction));
    }

    [Fact]
    public void TerminalSnapshotRejectsEveryInputWithoutChangingWinnerOrRandomness()
    {
        var before = new Scenario().Player(0, 20, [10], PlayerRoundStatus.Stopped, 140)
            .Player(1, 10, [10], PlayerRoundStatus.Stopped, 10).Build(safe: true);
        var terminal = GameEngine.Apply(before, new ContinueAutomaticResolution()).State;
        EngineInput[] inputs =
        [
            new ContinueAutomaticResolution(),
            new PlayerDecision(new DecisionId(1), terminal.Seats.Players[0], PlayerAction.Draw),
            new PlayerDecision(new DecisionId(1), terminal.Seats.Players[0], PlayerAction.Stop),
            new GameplayTimerExpired(new DecisionId(1), DecisionKind.PlayerAction),
            new GameplayTimerExpired(new DecisionId(1), DecisionKind.EffectTarget),
        ];
        foreach (var input in inputs)
        {
            Assert.Equal(EngineRejection.GameAlreadyTerminal, AssertRejectedUnchanged(terminal, input).Rejection);
        }
    }

    private static EngineTransitionResult AssertRejectedUnchanged(GameplayState state, EngineInput input)
    {
        var before = StateHasher.Encode(state);
        var result = GameEngine.Apply(state, input);
        Assert.False(result.IsAccepted);
        Assert.Same(state, result.State);
        Assert.Empty(result.Events);
        Assert.Equal(before, StateHasher.Encode(result.State));
        Assert.Equal(StateHasher.Compute(state), result.StateValidationHash);
        Assert.Equal(state.Random.WordPosition, result.State.Random.WordPosition);
        return result;
    }
}
