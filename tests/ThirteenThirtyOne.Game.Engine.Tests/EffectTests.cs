using ThirteenThirtyOne.Game.Domain;
using ThirteenThirtyOne.Game.Domain.Events;
using Xunit;

namespace ThirteenThirtyOne.Game.Engine.Tests;

public sealed class EffectTests
{
    [Theory]
    [InlineData(CardKind.PlusFive, 25, 30, PlayerRoundStatus.Active, null)]
    [InlineData(CardKind.PlusFive, 26, 31, PlayerRoundStatus.Perfect31, 50)]
    [InlineData(CardKind.PlusFive, 27, 32, PlayerRoundStatus.BustOver31, 0)]
    [InlineData(CardKind.MinusFive, 3, 0, PlayerRoundStatus.Active, null)]
    [InlineData(CardKind.MinusFive, 5, 0, PlayerRoundStatus.Active, null)]
    [InlineData(CardKind.MinusFive, 29, 24, PlayerRoundStatus.Active, null)]
    [InlineData(CardKind.Stop, 14, 14, PlayerRoundStatus.ForcedStop, 14)]
    [InlineData(CardKind.Zero, 30, 0, PlayerRoundStatus.Zeroed, 0)]
    public void EffectsApplyToOpponentWithoutChangingNumericalHistory(CardKind kind, int initialScore, int expectedScore,
        PlayerRoundStatus expectedStatus, int? award)
    {
        var fixture = new Scenario().Player(0, 1, [1]).Player(1, initialScore, [8]);
        var effect = fixture.Effect(kind);
        fixture.Draw(effect, fixture.Number(2));
        var start = fixture.Build();

        var waiting = Scenario.Act(start, PlayerAction.Draw);

        Assert.Equal(BoundaryKind.WaitTarget, waiting.Boundary);
        Assert.False(waiting.IsSafeGameplayBoundary);
        Assert.DoesNotContain(waiting.State.DrawPile, card => card.Id == effect.Id);
        Assert.DoesNotContain(waiting.State.DiscardPile, card => card.Id == effect.Id);
        Assert.Equal(effect, Assert.Single(waiting.State.ResolutionStack.OfType<EffectContext>()).SourceCard);
        Assert.Equal(start.Seats.Players.ToArray(), waiting.PendingDecision!.AllowedTargets.ToArray());

        var result = Scenario.Target(waiting.State, 1);

        var target = result.State.Players[1];
        Assert.Equal(expectedScore, target.CurrentScore);
        Assert.Equal(expectedStatus, target.Status);
        Assert.Equal(award, target.RoundScore);
        Assert.Equal(start.Players[1].NumberHistory.ToArray(), target.NumberHistory.ToArray());
        Assert.Equal(3, result.State.Players[0].CurrentScore);
        Assert.Contains(effect, result.State.DiscardPile);
        Assert.DoesNotContain(result.Events.OfType<ScoreCheckPerformed>(), item => item.Player == target.Player && item.Check == ScoreCheck.Rule13);
        Assert.Equal(BoundaryKind.SafePostResolution, result.Boundary);
        Assert.Equal(2, result.State.Players[0].NumberHistory.Length);
        StateValidator.Validate(result.State);
    }

    [Theory]
    [InlineData(CardKind.Stop, PlayerRoundStatus.ForcedStop)]
    [InlineData(CardKind.Zero, PlayerRoundStatus.Zeroed)]
    [InlineData(CardKind.PlusFive, PlayerRoundStatus.Perfect31)]
    public void SelfTerminalEffectCompletesBeforeCancellingNormalDraw(CardKind kind, PlayerRoundStatus expected)
    {
        var fixture = new Scenario().Player(0, 26, [6]).Player(1, 1, [1]);
        var effect = fixture.Effect(kind);
        var undrawn = fixture.Number(2);
        fixture.Draw(effect, undrawn);
        var waiting = Scenario.Act(fixture.Build(), PlayerAction.Draw);

        var result = Scenario.Target(waiting.State, 0);

        Assert.Equal(expected, result.State.Players[0].Status);
        Assert.Single(result.State.Players[0].NumberHistory);
        Assert.Equal(undrawn, result.State.DrawPile[0]);
        Assert.Empty(result.Events.OfType<CardDrawn>());
        Assert.Contains(effect, result.State.DiscardPile);
        Assert.True(result.IsSafeGameplayBoundary);
        var facts = result.Events.ToArray();
        Assert.True(Array.FindIndex(facts, item => item is EffectResolved) < Array.FindIndex(facts, item => item is DrawContextCompleted));
    }

    [Theory]
    [InlineData(CardKind.Zero)]
    [InlineData(CardKind.Stop)]
    [InlineData(CardKind.MinusFive)]
    [InlineData(CardKind.PlusFive)]
    public void TargetTimeoutDeterministicallySelfTargets(CardKind kind)
    {
        var fixture = new Scenario().Player(0, 1, [1]).Player(1, 2, [2]);
        fixture.Draw(fixture.Effect(kind), fixture.Number(1));
        var waiting = Scenario.Act(fixture.Build(), PlayerAction.Draw);

        var result = GameEngine.Apply(waiting.State, new GameplayTimerExpired(waiting.PendingDecision!.Id, DecisionKind.EffectTarget));

        var selected = Assert.Single(result.Events.OfType<TargetSelected>());
        Assert.True(selected.ByTimeout);
        Assert.Equal(waiting.PendingDecision.Owner, selected.Target);
        Assert.Equal(waiting.State.Players[1], result.State.Players[1]);
        Assert.True(result.IsAccepted);
    }

    [Fact]
    public void MultipleEffectsPreserveAdjacencyAndMinusFiveDoesNotPreventRule13()
    {
        var fixture = new Scenario().Player(0, 8, [8]).Player(1, 2, [2]);
        fixture.Draw(fixture.Effect(CardKind.MinusFive), fixture.Effect(CardKind.PlusFive), fixture.Number(5));
        var first = Scenario.Act(fixture.Build(), PlayerAction.Draw);
        var second = Scenario.Target(first.State, 0);

        Assert.Equal(3, second.State.Players[0].CurrentScore);
        Assert.Single(second.State.Players[0].NumberHistory);
        Assert.Equal(1, second.State.ResolutionStack.OfType<DrawContext>().Single().RemainingNumbers);
        var final = Scenario.Target(second.State, 1);

        Assert.Equal(PlayerRoundStatus.Bust13, final.State.Players[0].Status);
        Assert.Equal(new[] { 8, 5 }, final.State.Players[0].NumberHistory.Select(card => card.Number).ToArray());
        Assert.Equal(7, final.State.Players[1].CurrentScore);
    }

    [Fact]
    public void SoleActivePlayerHasOnlySelfAsLegalTarget()
    {
        var fixture = new Scenario().Player(0, 2, [2]).Player(1, 4, [4], PlayerRoundStatus.Stopped);
        fixture.Draw(fixture.Effect(CardKind.MinusFive), fixture.Number(1));
        var waiting = Scenario.Act(fixture.Build(), PlayerAction.Draw);

        Assert.Equal(waiting.PendingDecision!.Owner, Assert.Single(waiting.PendingDecision.AllowedTargets));
        var result = Scenario.Target(waiting.State, 0);
        Assert.True(result.State.Players[0].IsActive);
        Assert.Equal(1, result.State.Players[0].CurrentScore);
    }
}
