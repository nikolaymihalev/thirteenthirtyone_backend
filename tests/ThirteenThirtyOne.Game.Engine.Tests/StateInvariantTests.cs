using ThirteenThirtyOne.Game.Domain;
using Xunit;

namespace ThirteenThirtyOne.Game.Engine.Tests;

public sealed class StateInvariantTests
{
    [Fact]
    public void ConservationRejectsMissingDuplicateAndCounterfeitPhysicalCards()
    {
        var before = Created();
        var missing = new Transition(before);
        missing.DrawPile.RemoveAt(0);
        Assert.Throws<InvalidOperationException>(() => StateValidator.Validate(missing.Freeze()));

        var duplicate = new Transition(before);
        duplicate.DrawPile[0] = duplicate.DrawPile[1];
        Assert.Throws<InvalidOperationException>(() => StateValidator.Validate(duplicate.Freeze()));

        var counterfeit = new Transition(before);
        var card = counterfeit.DrawPile.First(item => item.IsNumber);
        counterfeit.DrawPile[counterfeit.DrawPile.IndexOf(card)] = new Card(card.Id, CardKind.Number, card.Number == 1 ? 2 : 1);
        Assert.Throws<InvalidOperationException>(() => StateValidator.Validate(counterfeit.Freeze()));
        StateValidator.Validate(before);
    }

    [Fact]
    public void ActionBoundaryRejectsWrongOwnerSequenceAndIncompleteOpening()
    {
        var state = Created();
        var wrongOwner = new Transition(state);
        var other = state.Seats.Players.First(player => player != state.PendingDecision!.Owner);
        wrongOwner.PendingDecision = new PendingDecision(state.PendingDecision!.Id, DecisionKind.PlayerAction, other, []);
        Assert.Throws<InvalidOperationException>(() => StateValidator.Validate(wrongOwner.Freeze()));
        var sequence = new Transition(state) { DecisionSequence = 0 };
        Assert.Throws<InvalidOperationException>(() => StateValidator.Validate(sequence.Freeze()));
        var opening = new Transition(state);
        var card = opening.DrawPile.First(item => !item.IsNumber);
        opening.DrawPile.Remove(card);
        opening.OpeningSetAside.Add(card);
        Assert.Throws<InvalidOperationException>(() => StateValidator.Validate(opening.Freeze()));
    }

    [Fact]
    public void TargetBoundaryRejectsWrongDrawerQuotaAndAlreadyDiscardedSource()
    {
        var fixture = new Scenario().Player(0, 1, [1]).Player(1, 2, [2]);
        fixture.Draw(fixture.Effect(CardKind.PlusFive), fixture.Number(1));
        var state = Scenario.Act(fixture.Build(), PlayerAction.Draw).State;
        var top = (EffectContext)state.ResolutionStack[^1];
        var wrong = new Transition(state);
        wrong.Stack[^1] = new EffectContext(top.Id, top.ParentId!.Value, top.SourceCard, state.Seats.Players[1], state.Seats.Players[1]);
        Assert.Throws<InvalidOperationException>(() => StateValidator.Validate(wrong.Freeze()));
        var quota = new Transition(state);
        var draw = (DrawContext)quota.Stack[0];
        quota.Stack[0] = new DrawContext(draw.Id, null, DrawKind.NormalDraw, draw.Recipient, 0);
        Assert.Throws<InvalidOperationException>(() => StateValidator.Validate(quota.Freeze()));
        var discard = new Transition(state);
        discard.DiscardPile.Add(top.SourceCard);
        Assert.Throws<InvalidOperationException>(() => StateValidator.Validate(discard.Freeze()));
        var unsafeBoundary = new Transition(state) { Boundary = BoundaryKind.SafePostResolution };
        Assert.Throws<InvalidOperationException>(() => StateValidator.Validate(unsafeBoundary.Freeze()));
    }

    [Fact]
    public void TerminalBoundaryRejectsWrongWinnerAndAnUnfinishedRound()
    {
        var start = new Scenario().Player(0, 20, [10], PlayerRoundStatus.Stopped, 140)
            .Player(1, 10, [10], PlayerRoundStatus.Stopped, 10).Build(safe: true);
        var terminal = GameEngine.Apply(start, new ContinueAutomaticResolution()).State;
        var wrongWinner = new Transition(terminal) { Winner = start.Seats.Players[1] };
        Assert.Throws<InvalidOperationException>(() => StateValidator.Validate(wrongWinner.Freeze()));
        var unfinished = new Transition(Created()) { Boundary = BoundaryKind.GameTerminal, Winner = start.Seats.Players[0] };
        Assert.Throws<InvalidOperationException>(() => StateValidator.Validate(unfinished.Freeze()));
    }

    [Fact]
    public void HashDetectsOrderedCardZonesRolesScoresAndMonotonicSequences()
    {
        var state = Created();
        var hash = StateHasher.Compute(state);
        var changedOrder = new Transition(state);
        (changedOrder.DrawPile[0], changedOrder.DrawPile[1]) = (changedOrder.DrawPile[1], changedOrder.DrawPile[0]);
        Assert.NotEqual(hash, StateHasher.Compute(changedOrder.Freeze()));
        var changedZone = new Transition(state);
        changedZone.DiscardPile.Add(changedZone.DrawPile[0]);
        changedZone.DrawPile.RemoveAt(0);
        Assert.NotEqual(hash, StateHasher.Compute(changedZone.Freeze()));
        var changedSequence = new Transition(state) { ContextSequence = 99 };
        Assert.NotEqual(hash, StateHasher.Compute(changedSequence.Freeze()));
        var changedRound = new Transition(state) { RoundNumber = 99 };
        Assert.NotEqual(hash, StateHasher.Compute(changedRound.Freeze()));
        var changedScore = new Transition(state);
        var player = changedScore.Players[0];
        changedScore.SetPlayer(new PlayerState(player.Player, player.Status, player.Reason, player.NumberHistory,
            player.CurrentScore, player.RoundScore, 99));
        Assert.NotEqual(hash, StateHasher.Compute(changedScore.Freeze()));
        Assert.Equal(hash, StateHasher.Compute(SnapshotCopy.Reconstruct(state)));
    }

    private static GameplayState Created() => GameEngine.CreateGame(new GameId("invariants"), [new PlayerId("A"), new PlayerId("B")],
        new RandomState(new byte[32]), EngineCompatibility.V1).State;
}
