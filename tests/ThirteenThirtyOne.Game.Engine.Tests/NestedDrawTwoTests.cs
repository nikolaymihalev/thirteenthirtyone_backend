using ThirteenThirtyOne.Game.Domain;
using ThirteenThirtyOne.Game.Domain.Events;
using Xunit;

namespace ThirteenThirtyOne.Game.Engine.Tests;

public sealed class NestedDrawTwoTests
{
    [Fact]
    public void SelfDrawTwoRequiresTwoForcedNumbersThenItsOwnAdditionalNormalNumber()
    {
        var fixture = new Scenario().Player(0, 1, [1]).Player(1, 1, [1]);
        var drawTwo = fixture.Effect(CardKind.DrawTwo);
        fixture.Draw(drawTwo, fixture.Number(1), fixture.Number(2), fixture.Effect(CardKind.MinusFive), fixture.Number(3));
        var start = Scenario.Act(fixture.Build(), PlayerAction.Draw);
        var normalId = start.State.ResolutionStack[0].Id;

        var afterForcedNumbers = Scenario.Target(start.State, 0);

        Assert.Equal(BoundaryKind.WaitTarget, afterForcedNumbers.Boundary);
        Assert.Contains(drawTwo, afterForcedNumbers.State.DiscardPile);
        var parent = Assert.Single(afterForcedNumbers.State.ResolutionStack.OfType<DrawContext>());
        Assert.Equal(normalId, parent.Id);
        Assert.Equal(1, parent.RemainingNumbers);
        Assert.Equal(new[] { 1, 1, 2 }, afterForcedNumbers.State.Players[0].NumberHistory.Select(card => card.Number).ToArray());
        var forced = afterForcedNumbers.Events.OfType<NumberReceived>().ToArray();
        Assert.Equal(2, forced.Length);
        Assert.All(forced, item => Assert.NotEqual(normalId, item.Context));
        Assert.Equal(new[] { 1, 0 }, forced.Select(item => item.RemainingNumbers).ToArray());

        var result = Scenario.Target(afterForcedNumbers.State, 1);

        Assert.True(result.IsSafeGameplayBoundary);
        var normal = Assert.Single(result.Events.OfType<NumberReceived>());
        Assert.Equal(normalId, normal.Context);
        Assert.Equal(0, normal.RemainingNumbers);
        Assert.Equal(new[] { 1, 1, 2, 3 }, result.State.Players[0].NumberHistory.Select(card => card.Number).ToArray());
    }

    [Fact]
    public void ThreeNestedDrawTwosResolveLifoWithIndependentQuotasAndRoles()
    {
        var fixture = new Scenario(4).Player(0, 1, [1]).Player(1, 1, [1]).Player(2, 1, [1]).Player(3, 1, [1]);
        var outer = fixture.Effect(CardKind.DrawTwo);
        var middle = fixture.Effect(CardKind.DrawTwo);
        var inner = fixture.Effect(CardKind.DrawTwo);
        fixture.Draw(outer, middle, inner, fixture.Number(1), fixture.Number(1), fixture.Number(2), fixture.Number(2),
            fixture.Number(3), fixture.Number(3), fixture.Number(4));
        var first = Scenario.Act(fixture.Build(), PlayerAction.Draw);
        var second = Scenario.Target(first.State, 1);
        var third = Scenario.Target(second.State, 2);

        Assert.Equal(first.State.Seats.Players[0], third.State.TurnOwner);
        Assert.Equal(first.State.Seats.Players[2], third.PendingDecision!.Owner);
        Assert.Equal(6, third.State.ResolutionStack.Length);
        Assert.Empty(third.State.DiscardPile);
        Assert.All(third.State.ResolutionStack.OfType<DrawContext>(), context =>
            Assert.Equal(context.Kind == DrawKind.NormalDraw ? 1 : 2, context.RemainingNumbers));
        var roles = third.State.ResolutionStack.OfType<EffectContext>().ToArray();
        Assert.Equal(new[] { first.State.Seats.Players[0], first.State.Seats.Players[1], first.State.Seats.Players[2] },
            roles.Select(context => context.EffectDrawer).ToArray());
        Assert.All(roles, context => Assert.Equal(context.EffectDrawer, context.DecisionOwner));

        var result = Scenario.Target(third.State, 3);

        var recipients = result.Events.OfType<NumberReceived>().Select(item => result.State.Seats.SeatOf(item.Player).Value).ToArray();
        Assert.Equal(new[] { 3, 3, 2, 2, 1, 1, 0 }, recipients);
        Assert.Equal(new[] { inner.Id, middle.Id, outer.Id }, result.Events.OfType<EffectResolved>().Select(item => item.Card).ToArray());
        Assert.Equal(new[] { 1, 0, 1, 0, 1, 0, 0 }, result.Events.OfType<NumberReceived>().Select(item => item.RemainingNumbers).ToArray());
        Assert.All(result.Events.OfType<CardDrawn>(), item => Assert.Equal(first.State.TurnOwner, item.TurnOwner));
        Assert.True(result.IsSafeGameplayBoundary);
        StateValidator.Validate(result.State);
    }

    [Fact]
    public void AtomicityA_StoppingTurnOwnerDoesNotInterruptStartedDrawTwoOnAnotherRecipient()
    {
        var fixture = new Scenario().Player(0, 5, [5]).Player(1, 1, [1]);
        var drawTwo = fixture.Effect(CardKind.DrawTwo);
        var stop = fixture.Effect(CardKind.Stop);
        var undrawn = fixture.Number(4);
        fixture.Draw(drawTwo, stop, fixture.Number(1), fixture.Number(2), undrawn);
        var first = Scenario.Act(fixture.Build(), PlayerAction.Draw);
        var second = Scenario.Target(first.State, 1);

        Assert.Equal(second.State.Seats.Players[0], second.State.TurnOwner);
        Assert.Equal(second.State.Seats.Players[1], second.PendingDecision!.Owner);
        Assert.Equal(second.PendingDecision.Owner, ((EffectContext)second.State.ResolutionStack[^1]).EffectDrawer);
        Assert.DoesNotContain(drawTwo, second.State.DiscardPile);

        var result = Scenario.Target(second.State, 0);

        Assert.Equal(PlayerRoundStatus.ForcedStop, result.State.Players[0].Status);
        Assert.Equal(5, result.State.Players[0].RoundScore);
        Assert.Single(result.State.Players[0].NumberHistory);
        Assert.Equal(new[] { 1, 1, 2 }, result.State.Players[1].NumberHistory.Select(card => card.Number).ToArray());
        Assert.Equal(undrawn, result.State.DrawPile[0]);
        Assert.Equal(new[] { stop.Id, drawTwo.Id }, result.Events.OfType<EffectResolved>().Select(item => item.Card).ToArray());
        Assert.True(result.State.Players[1].IsActive);
        Assert.True(result.IsSafeGameplayBoundary);
    }

    [Fact]
    public void AtomicityB_ZeroingParentDrawerDoesNotInterruptNestedChildAndOnlyLegalParentsResume()
    {
        var fixture = new Scenario(3).Player(0, 1, [1]).Player(1, 1, [1]).Player(2, 1, [1]);
        fixture.Draw(fixture.Effect(CardKind.DrawTwo), fixture.Effect(CardKind.DrawTwo), fixture.Effect(CardKind.Zero),
            fixture.Number(1), fixture.Number(2), fixture.Number(3));
        var aChooses = Scenario.Act(fixture.Build(), PlayerAction.Draw);
        var bChooses = Scenario.Target(aChooses.State, 1);
        var cChooses = Scenario.Target(bChooses.State, 2);
        var contexts = cChooses.State.ResolutionStack.OfType<EffectContext>().ToArray();
        Assert.Equal(cChooses.State.Seats.Players[1], contexts[1].EffectDrawer);
        Assert.Equal(cChooses.State.Seats.Players[1], contexts[1].DecisionOwner);
        Assert.Equal(cChooses.State.Seats.Players[2], contexts[1].EffectTarget);
        Assert.Equal(cChooses.State.Seats.Players[2], cChooses.PendingDecision!.Owner);
        Assert.Equal(cChooses.State.Seats.Players[0], cChooses.State.TurnOwner);

        var result = Scenario.Target(cChooses.State, 1);

        Assert.Equal(PlayerRoundStatus.Zeroed, result.State.Players[1].Status);
        Assert.Equal(0, result.State.Players[1].CurrentScore);
        Assert.Single(result.State.Players[1].NumberHistory);
        Assert.Equal(new[] { 2, 2, 0 }, result.Events.OfType<NumberReceived>().Select(item => result.State.Seats.SeatOf(item.Player).Value).ToArray());
        Assert.Equal(new[] { 1, 1, 2 }, result.State.Players[2].NumberHistory.Select(card => card.Number).ToArray());
        Assert.Equal(new[] { 1, 3 }, result.State.Players[0].NumberHistory.Select(card => card.Number).ToArray());
        Assert.True(result.IsSafeGameplayBoundary);
    }

    [Theory]
    [InlineData(8, 8, 5, PlayerRoundStatus.Bust13)]
    [InlineData(30, 1, 2, PlayerRoundStatus.BustOver31)]
    [InlineData(29, 1, 2, PlayerRoundStatus.Perfect31)]
    public void FirstForcedNumberTerminalCancelsSecondQuota(int score, int previous, int drawn, PlayerRoundStatus expected)
    {
        var fixture = new Scenario().Player(0, 1, [1]).Player(1, score, [previous]);
        fixture.Draw(fixture.Effect(CardKind.DrawTwo), fixture.Number(drawn), fixture.Number(3));
        var waiting = Scenario.Act(fixture.Build(), PlayerAction.Draw);

        var result = Scenario.Target(waiting.State, 1);

        Assert.Equal(expected, result.State.Players[1].Status);
        Assert.Equal(2, result.State.Players[1].NumberHistory.Length);
        Assert.Single(result.Events.OfType<NumberReceived>(), item => item.Player == result.State.Seats.Players[1]);
        Assert.Equal(3, result.State.Players[0].NumberHistory[^1].Number);
    }

    [Theory]
    [InlineData(CardKind.Stop, PlayerRoundStatus.ForcedStop)]
    [InlineData(CardKind.Zero, PlayerRoundStatus.Zeroed)]
    [InlineData(CardKind.PlusFive, PlayerRoundStatus.Perfect31)]
    public void SelfTerminalEffectDuringForcedDrawCancelsOnlyThatRecipient(CardKind kind, PlayerRoundStatus expected)
    {
        var fixture = new Scenario().Player(0, 1, [1]).Player(1, 26, [6]);
        fixture.Draw(fixture.Effect(CardKind.DrawTwo), fixture.Effect(kind), fixture.Number(3));
        var first = Scenario.Act(fixture.Build(), PlayerAction.Draw);
        var second = Scenario.Target(first.State, 1);

        var result = Scenario.Target(second.State, 1);

        Assert.Equal(expected, result.State.Players[1].Status);
        Assert.Single(result.State.Players[1].NumberHistory);
        Assert.Equal(result.State.Seats.Players[0], Assert.Single(result.Events.OfType<NumberReceived>()).Player);
        Assert.True(result.IsSafeGameplayBoundary);
    }

    [Fact]
    public void DrawTwoTargetTimeoutUsesDrawerAndStillRequiresNormalNumber()
    {
        var fixture = new Scenario().Player(0, 1, [1]).Player(1, 1, [1]);
        fixture.Draw(fixture.Effect(CardKind.DrawTwo), fixture.Number(1), fixture.Number(2), fixture.Number(3));
        var waiting = Scenario.Act(fixture.Build(), PlayerAction.Draw);

        var result = GameEngine.Apply(waiting.State, new GameplayTimerExpired(waiting.PendingDecision!.Id, DecisionKind.EffectTarget));

        Assert.Equal(4, result.State.Players[0].NumberHistory.Length);
        Assert.True(Assert.Single(result.Events.OfType<TargetSelected>()).ByTimeout);
    }
}
