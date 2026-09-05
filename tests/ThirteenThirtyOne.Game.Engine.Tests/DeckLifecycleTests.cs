using ThirteenThirtyOne.Game.Domain;
using ThirteenThirtyOne.Game.Domain.Events;
using ThirteenThirtyOne.Game.Engine.Randomness;
using Xunit;

namespace ThirteenThirtyOne.Game.Engine.Tests;

public sealed class DeckLifecycleTests
{
    [Fact]
    public void DrawTwoUsesLastPhysicalCardBeforeRefillingAndDoesNotReshuffleItsUnresolvedSource()
    {
        var fixture = new Scenario { RemainingToDiscard = true };
        fixture.Player(0, 1, [1]).Player(1, 1, [1]);
        var source = fixture.Effect(CardKind.DrawTwo);
        var last = fixture.Number(1);
        fixture.Draw(source, last);
        var waiting = Scenario.Act(fixture.Build(), PlayerAction.Draw);
        Assert.Empty(waiting.Events.OfType<RandomOperationCompleted>());
        Assert.Single(waiting.State.DrawPile);

        var result = Scenario.Target(waiting.State, 1);
        var repeat = Scenario.Target(waiting.State, 1);

        Assert.Equal(result.StateValidationHash, repeat.StateValidationHash);
        Assert.Equal(result.Events.ToArray(), repeat.Events.ToArray());
        var events = result.Events.ToArray();
        var drawIndex = Array.FindIndex(events, item => item is CardDrawn drawn && drawn.Card == last.Id);
        var refillIndex = Array.FindIndex(events, item => item is RandomOperationCompleted { Operation: RandomOperation.DiscardRefill });
        Assert.True(drawIndex >= 0 && refillIndex > drawIndex);
        Assert.DoesNotContain(result.Events.OfType<CardDrawn>(), item => item.Card == source.Id);
        StateValidator.Validate(result.State);
        StateValidator.Validate(waiting.State);
    }

    [Fact]
    public void ResolvedEffectCanReenterTheDrawPileAndBeDrawnAgainInTheSameRound()
    {
        var fixture = new Scenario().Player(0, 1, [1]).Player(1, 1, [1]);
        var effect = fixture.Effect(CardKind.MinusFive);
        fixture.Draw(effect, fixture.Number(1));
        var waiting = Scenario.Act(fixture.Build(), PlayerAction.Draw);
        var completed = Scenario.Target(waiting.State, 1);
        Assert.Contains(effect, completed.State.DiscardPile);

        // Construct a later same-round refill snapshot with all still-available cards in discard.
        var later = new Transition(GameEngine.Apply(completed.State, new ContinueAutomaticResolution()).State);
        later.DiscardPile.AddRange(later.DrawPile);
        later.DrawPile.Clear();
        var baseState = later.Freeze();
        ulong position = 0;
        for (; position < 10000; position++)
        {
            var probe = new ChaCha20Random(new RandomState(new byte[32], position));
            var shuffled = baseState.DiscardPile.ToList();
            probe.Shuffle(shuffled);
            if (shuffled[0] == effect)
            {
                break;
            }
        }

        Assert.True(position < 10000);
        var refillState = new GameplayState(baseState.GameId, baseState.Compatibility, baseState.Seats,
            baseState.RoundNumber, baseState.RoundKind, baseState.RoundStarter, baseState.TurnOwner,
            baseState.Players, baseState.DrawPile, baseState.DiscardPile, [], [], baseState.PendingDecision,
            baseState.Boundary, baseState.DecisionSequence, baseState.ContextSequence, new RandomState(new byte[32], position));

        var result = Scenario.Act(refillState, PlayerAction.Draw);

        Assert.Equal(baseState.RoundNumber, result.State.RoundNumber);
        Assert.Equal(effect.Id, Assert.Single(result.Events.OfType<CardDrawn>()).Card);
        Assert.Equal(effect, Assert.Single(result.State.ResolutionStack.OfType<EffectContext>()).SourceCard);
        Assert.Single(result.Events.OfType<RandomOperationCompleted>(), item => item.Operation == RandomOperation.DiscardRefill);
        Assert.Empty(result.State.DiscardPile);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(17)]
    public void NumericalDeadlockIgnoresDrawableEffectsAndPreservesActiveScores(int score)
    {
        var fixture = new Scenario().Player(0, score, [1]).Player(1, 0, [1]);
        fixture.AllRemainingNumbersInHistory(1, 0);
        var before = fixture.Build();
        Assert.All(before.DrawPile, card => Assert.False(card.IsNumber));

        var result = Scenario.Act(before, PlayerAction.Draw);

        Assert.All(result.State.Players, player =>
        {
            Assert.Equal(PlayerRoundStatus.ForcedStop, player.Status);
            Assert.Equal(TerminationReason.NumericalDeckDeadlock, player.Reason);
            Assert.Equal(player.CurrentScore, player.RoundScore);
        });
        Assert.Equal(score, result.State.Players[0].RoundScore);
        Assert.Equal(0, result.State.Players[1].RoundScore);
        Assert.Empty(result.Events.OfType<CardDrawn>());
        Assert.Empty(result.Events.OfType<RandomOperationCompleted>());
        var deadlock = Assert.Single(result.Events.OfType<NumericalDeckDeadlockOccurred>());
        Assert.Equal(before.GameId, deadlock.Game);
        Assert.Equal(1, deadlock.Round);
        Assert.Equal(96, deadlock.NumberHistoryCount);
        Assert.Equal(16, deadlock.DrawPileCount);
        Assert.True(result.IsSafeGameplayBoundary);
        Assert.Equal(before.DrawPile.ToArray(), result.State.DrawPile.ToArray());
    }

    [Fact]
    public void DeadlockCancelsNestedContextsWithoutLosingSourceCardsOrDrawingAnotherCard()
    {
        var fixture = new Scenario(3).Player(0, 1, [1]).Player(1, 1, [1]).Player(2, 0, [1]);
        var firstEffect = fixture.Effect(CardKind.DrawTwo);
        var secondEffect = fixture.Effect(CardKind.DrawTwo);
        fixture.Draw(firstEffect, secondEffect);
        var first = Scenario.Act(fixture.Build(), PlayerAction.Draw);
        var second = Scenario.Target(first.State, 1);
        var suspended = new Transition(second.State);
        var allNumbers = suspended.DrawPile.Where(card => card.IsNumber).ToArray();
        suspended.DrawPile.RemoveAll(card => card.IsNumber);
        var player = suspended.Players[2];
        suspended.SetPlayer(new PlayerState(player.Player, player.Status, player.Reason,
            player.NumberHistory.Concat(allNumbers), player.CurrentScore, null, 0));

        var result = Scenario.Target(suspended.Freeze(), 2);

        Assert.Empty(result.State.ResolutionStack);
        Assert.Empty(result.Events.OfType<CardDrawn>());
        Assert.Equal(2, Assert.Single(result.Events.OfType<NumericalDeckDeadlockOccurred>()).UnresolvedEffectCount);
        Assert.Equal(new[] { secondEffect.Id, firstEffect.Id }, result.Events.OfType<EffectResolved>().Select(item => item.Card).ToArray());
        Assert.All(result.Events.OfType<EffectResolved>(), item => Assert.True(item.CancelledByDeadlock));
        Assert.Equal(2, result.State.DiscardPile.Count(card => card.Kind == CardKind.DrawTwo));
        Assert.True(result.IsSafeGameplayBoundary);
        StateValidator.Validate(result.State);
    }
}
