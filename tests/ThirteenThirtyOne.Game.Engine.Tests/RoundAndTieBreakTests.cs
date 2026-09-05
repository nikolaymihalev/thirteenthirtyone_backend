using ThirteenThirtyOne.Game.Domain;
using ThirteenThirtyOne.Game.Domain.Events;
using Xunit;

namespace ThirteenThirtyOne.Game.Engine.Tests;

public sealed class RoundAndTieBreakTests
{
    [Fact]
    public void RoundCompletionDiscardsHistoriesScoresOnceAndRotatesOnePhysicalSeat()
    {
        var fixture = new Scenario(3) { Starter = 1 };
        fixture.Player(0, 10, [10], PlayerRoundStatus.Stopped, 20)
            .Player(1, 0, [1], PlayerRoundStatus.Zeroed, 30)
            .Player(2, 31, [12], PlayerRoundStatus.Perfect31, 40);
        fixture.Draw(fixture.Number(1), fixture.Number(2), fixture.Number(3));
        var before = fixture.Build(safe: true);

        var result = GameEngine.Apply(before, new ContinueAutomaticResolution());

        Assert.Equal(new long[] { 30, 30, 90 }, result.State.Players.Select(player => player.TotalScore).ToArray());
        Assert.Equal(2, result.State.RoundNumber);
        Assert.Equal(new SeatIndex(2), result.State.RoundStarter);
        Assert.Same(before.Seats, result.State.Seats);
        Assert.Single(result.Events.OfType<RoundEnded>());
        Assert.Equal(3, result.Events.OfType<RoundScoreRecorded>().Count());
        Assert.All(before.Players.SelectMany(player => player.NumberHistory), card => Assert.Contains(card, result.State.DiscardPile));
        Assert.All(result.State.Players, player => Assert.Single(player.NumberHistory));
        Assert.Empty(result.Events.OfType<RandomOperationCompleted>());

        var duplicateContinuation = GameEngine.Apply(result.State, new ContinueAutomaticResolution());
        Assert.Equal(EngineRejection.ContinuationNotAllowed, duplicateContinuation.Rejection);
        Assert.Equal(result.StateValidationHash, duplicateContinuation.StateValidationHash);
    }

    [Theory]
    [InlineData(140, 10, 100, 20, 0)]
    [InlineData(145, 10, 140, 20, 1)]
    [InlineData(149, 0, 140, 9, -1)]
    public void ThresholdUsesCompletedNormalRoundAndUniqueHighest(long aTotal, int aRound, long bTotal, int bRound, int winner)
    {
        var fixture = new Scenario().Player(0, aRound, [1], PlayerRoundStatus.Stopped, aTotal)
            .Player(1, bRound, [2], PlayerRoundStatus.Stopped, bTotal);
        var before = fixture.Build(safe: true);

        var result = GameEngine.Apply(before, new ContinueAutomaticResolution());

        if (winner < 0)
        {
            Assert.Equal(BoundaryKind.WaitPlayerAction, result.Boundary);
            Assert.Equal(2, result.State.RoundNumber);
        }
        else
        {
            Assert.Equal(BoundaryKind.GameTerminal, result.Boundary);
            Assert.Equal(before.Seats.Players[winner], result.State.Winner);
            Assert.Single(result.Events.OfType<GameEnded>());
            Assert.All(result.State.Players, player => Assert.Empty(player.NumberHistory));
        }

        Assert.Equal(aTotal + aRound, result.State.Players[0].TotalScore);
        Assert.Equal(bTotal + bRound, result.State.Players[1].TotalScore);
    }

    [Fact]
    public void Potential150DuringATurnDoesNotScoreOrEndBeforeTheWholeRoundCompletes()
    {
        var before = new Scenario().Player(0, 10, [10], total: 145).Player(1, 2, [2], total: 5).Build();
        var stopped = Scenario.Act(before, PlayerAction.Stop);
        Assert.Equal(145, stopped.State.Players[0].TotalScore);
        Assert.Null(stopped.State.Winner);
        Assert.Empty(stopped.Events.OfType<RoundEnded>());

        var next = GameEngine.Apply(stopped.State, new ContinueAutomaticResolution());
        Assert.Equal(1, next.State.RoundNumber);
        var secondStopped = Scenario.Act(next.State, PlayerAction.Stop);
        Assert.Equal(145, secondStopped.State.Players[0].TotalScore);
        var terminal = GameEngine.Apply(secondStopped.State, new ContinueAutomaticResolution());
        Assert.Equal(BoundaryKind.GameTerminal, terminal.Boundary);
        Assert.Equal(155, terminal.State.Players[0].TotalScore);
    }

    [Fact]
    public void LockedBDTieBreakRotatesFromBThroughDThenBOnOriginalRingAndNeverChangesTotals()
    {
        var fixture = new Scenario(4) { Starter = 1, Owner = 3 };
        fixture.Player(0, 0, [1], PlayerRoundStatus.Stopped, 100)
            .Player(1, 10, [10], PlayerRoundStatus.Stopped, 140)
            .Player(2, 0, [2], PlayerRoundStatus.Stopped, 100)
            .Player(3, 20, [12], PlayerRoundStatus.Stopped, 130);
        fixture.Draw(fixture.Number(10), fixture.Number(10), fixture.Number(12), fixture.Number(12), fixture.Number(1), fixture.Number(2));
        var before = fixture.Build(safe: true);

        var first = GameEngine.Apply(before, new ContinueAutomaticResolution());

        Assert.Equal(RoundKind.TieBreak, first.State.RoundKind);
        Assert.Equal(new SeatIndex(3), first.State.RoundStarter);
        Assert.Equal(new[] { before.Seats.Players[3], before.Seats.Players[1] },
            first.Events.OfType<OpeningCardDealt>().Select(item => item.Player).ToArray());
        Assert.Equal(PlayerRoundStatus.NotParticipating, first.State.Players[0].Status);
        Assert.Equal(PlayerRoundStatus.NotParticipating, first.State.Players[2].Status);
        Assert.Same(before.Seats, first.State.Seats);
        var totals = first.State.Players.Select(player => player.TotalScore).ToArray();

        var second = StopEntireRound(first.State);

        Assert.Equal(new SeatIndex(1), second.State.RoundStarter);
        Assert.Equal(RoundKind.TieBreak, second.State.RoundKind);
        Assert.Equal(10, second.State.Players[1].TieBreakRoundResult);
        Assert.Equal(10, second.State.Players[3].TieBreakRoundResult);
        Assert.Equal(totals, second.State.Players.Select(player => player.TotalScore).ToArray());
        var third = StopEntireRound(second.State);
        Assert.Equal(new SeatIndex(3), third.State.RoundStarter);

        var terminal = StopEntireRound(third.State);

        Assert.Equal(BoundaryKind.GameTerminal, terminal.Boundary);
        Assert.Equal(before.Seats.Players[1], terminal.State.Winner);
        Assert.Equal(totals, terminal.State.Players.Select(player => player.TotalScore).ToArray());
        Assert.Equal(2, terminal.State.Players[1].TieBreakRoundResult);
        Assert.Equal(1, terminal.State.Players[3].TieBreakRoundResult);
        Assert.Same(before.Seats, terminal.State.Seats);
    }

    [Fact]
    public void RepeatedTieBreakNarrowsFromThreeParticipantsToTwoAndSkipsExcludedSeats()
    {
        var fixture = new Scenario(4) { Kind = RoundKind.TieBreak, Starter = 0 };
        fixture.Player(0, 12, [12], PlayerRoundStatus.Stopped, 150)
            .Player(1, 0, status: PlayerRoundStatus.NotParticipating, total: 100)
            .Player(2, 3, [3], PlayerRoundStatus.Stopped, 150)
            .Player(3, 12, [12], PlayerRoundStatus.Stopped, 150);
        fixture.Draw(fixture.Number(1), fixture.Number(2), fixture.Effect(CardKind.MinusFive), fixture.Number(1));

        var narrowed = GameEngine.Apply(fixture.Build(safe: true), new ContinueAutomaticResolution());

        Assert.Equal(new SeatIndex(3), narrowed.State.RoundStarter);
        Assert.Equal(PlayerRoundStatus.NotParticipating, narrowed.State.Players[2].Status);
        Assert.Equal(new[] { 0, 3 }, narrowed.State.Players.Where(player => player.IsParticipating)
            .Select(player => narrowed.State.Seats.SeatOf(player.Player).Value).ToArray());
        var target = Scenario.Act(narrowed.State, PlayerAction.Draw);
        Assert.Equal(new[] { narrowed.State.Seats.Players[0], narrowed.State.Seats.Players[3] }, target.PendingDecision!.AllowedTargets.ToArray());
        var completed = Scenario.Target(target.State, 0);
        var next = GameEngine.Apply(completed.State, new ContinueAutomaticResolution());
        Assert.Equal(narrowed.State.Seats.Players[0], next.PendingDecision!.Owner);
        Assert.Equal(new long[] { 150, 100, 150, 150 }, next.State.Players.Select(player => player.TotalScore).ToArray());
    }

    [Fact]
    public void NumericalDeadlockFollowsStandardRoundScoringOnlyAfterSafeContinuation()
    {
        var fixture = new Scenario().Player(0, 4, [4]).Player(1, 0, [1]);
        fixture.AllRemainingNumbersInHistory(1, 0);
        var deadlock = Scenario.Act(fixture.Build(), PlayerAction.Draw);
        Assert.Equal(0, deadlock.State.Players[0].TotalScore);

        var nextRound = GameEngine.Apply(deadlock.State, new ContinueAutomaticResolution());

        Assert.Equal(4, nextRound.State.Players[0].TotalScore);
        Assert.Equal(2, nextRound.State.RoundNumber);
        Assert.Single(nextRound.Events.OfType<RoundEnded>());
        Assert.All(nextRound.State.Players, player => Assert.Single(player.NumberHistory));
        StateValidator.Validate(nextRound.State);
    }

    internal static EngineTransitionResult StopEntireRound(GameplayState state)
    {
        var round = state.RoundNumber;
        EngineTransitionResult result;
        do
        {
            result = Scenario.Act(state, PlayerAction.Stop);
            Assert.True(result.IsSafeGameplayBoundary);
            result = GameEngine.Apply(result.State, new ContinueAutomaticResolution());
            state = result.State;
        }
        while (state.RoundNumber == round && state.Boundary != BoundaryKind.GameTerminal);

        return result;
    }
}
