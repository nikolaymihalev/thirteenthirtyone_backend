using ThirteenThirtyOne.Game.Domain;
using ThirteenThirtyOne.Game.Domain.Events;
using Xunit;

namespace ThirteenThirtyOne.Game.Engine.Tests;

public sealed class NumberAndTurnTests
{
    [Theory]
    [InlineData(1, 12)]
    [InlineData(2, 11)]
    [InlineData(3, 10)]
    [InlineData(4, 9)]
    [InlineData(5, 8)]
    [InlineData(6, 7)]
    [InlineData(7, 6)]
    [InlineData(8, 5)]
    [InlineData(9, 4)]
    [InlineData(10, 3)]
    [InlineData(11, 2)]
    [InlineData(12, 1)]
    public void EveryOrderedDangerPairBustsBeforeFurtherScoreChecks(int previous, int next)
    {
        var fixture = new Scenario().Player(0, previous, [previous]).Player(1, 2, [2]);
        fixture.Draw(fixture.Number(next));

        var result = Scenario.Act(fixture.Build(), PlayerAction.Draw);

        var player = result.State.Players[0];
        Assert.Equal(PlayerRoundStatus.Bust13, player.Status);
        Assert.Equal(0, player.RoundScore);
        Assert.Equal(new[] { previous, next }, player.NumberHistory.Select(card => card.Number).ToArray());
        Assert.Equal(ScoreCheck.Rule13, Assert.Single(result.Events.OfType<ScoreCheckPerformed>()).Check);
        Assert.Equal(BoundaryKind.SafePostResolution, result.Boundary);
    }

    [Theory]
    [InlineData(26, 5, 8, PlayerRoundStatus.Bust13, 0)]
    [InlineData(29, 5, 8, PlayerRoundStatus.Bust13, 0)]
    [InlineData(26, 5, 4, PlayerRoundStatus.Perfect31, 50)]
    [InlineData(29, 5, 4, PlayerRoundStatus.BustOver31, 0)]
    [InlineData(8, 8, 8, PlayerRoundStatus.Active, null)]
    public void NumberResolutionHonorsPriorityAndAllowsDuplicates(int score, int next, int previous,
        PlayerRoundStatus expected, int? award)
    {
        var fixture = new Scenario().Player(0, score, [previous]).Player(1, 1, [1]);
        fixture.Draw(fixture.Number(next));
        var before = fixture.Build();
        var hash = StateHasher.Compute(before);

        var result = Scenario.Act(before, PlayerAction.Draw);

        Assert.Equal(expected, result.State.Players[0].Status);
        Assert.Equal(award, result.State.Players[0].RoundScore);
        Assert.Equal(score + next, result.State.Players[0].CurrentScore);
        Assert.Equal(hash, StateHasher.Compute(before));
        Assert.Equal(before.Random.WordPosition, result.State.Random.WordPosition);
        Assert.Empty(result.State.ResolutionStack);
        Assert.Null(result.State.TurnOwner);
        Assert.Equal(before.TurnOwner, result.State.CompletedTurnOwner);
    }

    [Fact]
    public void OnlyTheLastTwoNumbersMatterAndChecksRunInOrder()
    {
        var fixture = new Scenario().Player(0, 19, [5, 6, 8]).Player(1, 1, [1]);
        fixture.Draw(fixture.Number(2));

        var result = Scenario.Act(fixture.Build(), PlayerAction.Draw);

        Assert.True(result.State.Players[0].IsActive);
        Assert.Equal(new[] { ScoreCheck.Rule13, ScoreCheck.Over31, ScoreCheck.Perfect31 },
            result.Events.OfType<ScoreCheckPerformed>().Select(item => item.Check).ToArray());
        Assert.Equal(0, Assert.Single(result.Events.OfType<NumberReceived>()).RemainingNumbers);
        Assert.Single(result.Events.OfType<DrawContextCompleted>());
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(23, false)]
    [InlineData(0, true)]
    [InlineData(23, true)]
    public void StopAndActionTimeoutPreserveScoreAndYieldBeforeNextTurn(int score, bool timeout)
    {
        var before = new Scenario().Player(0, score, [1]).Player(1, 2, [2]).Build();
        var result = timeout
            ? GameEngine.Apply(before, new GameplayTimerExpired(before.PendingDecision!.Id, DecisionKind.PlayerAction))
            : Scenario.Act(before, PlayerAction.Stop);

        Assert.Equal(PlayerRoundStatus.Stopped, result.State.Players[0].Status);
        Assert.Equal(score, result.State.Players[0].RoundScore);
        Assert.Equal(timeout ? TerminationReason.Timeout : TerminationReason.PlayerChoice, result.State.Players[0].Reason);
        Assert.True(result.IsSafeGameplayBoundary);
        Assert.Null(result.PendingDecision);
        Assert.Equal(0, result.State.Players[0].TotalScore);
        Assert.Equal(before.DecisionSequence, result.State.DecisionSequence);
        Assert.Empty(result.Events.OfType<RoundEnded>());

        var continued = GameEngine.Apply(result.State, new ContinueAutomaticResolution());
        Assert.Equal(before.Seats.Players[1], continued.PendingDecision!.Owner);
        Assert.Equal(before.DecisionSequence + 1, continued.State.DecisionSequence);
    }

    [Fact]
    public void NextTurnSkipsInactiveSeatsAndSoleActivePlayerKeepsPlaying()
    {
        var fixture = new Scenario(4).Player(0, 1, [1]).Player(1, 2, [2], PlayerRoundStatus.Stopped)
            .Player(2, 0, [3], PlayerRoundStatus.Zeroed).Player(3, 4, [4], PlayerRoundStatus.Stopped);
        fixture.Draw(fixture.Number(1));
        var result = Scenario.Act(fixture.Build(), PlayerAction.Draw);

        var continued = GameEngine.Apply(result.State, new ContinueAutomaticResolution());

        Assert.Equal(result.State.Seats.Players[0], continued.PendingDecision!.Owner);
        Assert.Equal(1, continued.State.RoundNumber);
        Assert.Empty(continued.Events.OfType<RoundEnded>());
    }
}
