using ThirteenThirtyOne.Game.Domain;
using Xunit;

namespace ThirteenThirtyOne.Game.Domain.Tests;

public sealed class DomainInvariantTests
{
    [Fact]
    public void IdentifiersHaveValueEqualityAndOrdinalPlayerIdentity()
    {
        Assert.Equal(new PlayerId("A"), new PlayerId("A"));
        Assert.NotEqual(new PlayerId("A"), new PlayerId("a"));
        Assert.Equal(new GameId("game"), new GameId("game"));
        Assert.Equal(new CardId(12), new CardId(12));
        Assert.NotEqual(new DecisionId(1), new DecisionId(2));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void InvalidSeatsAreRejected(int seat) => Assert.Throws<ArgumentOutOfRangeException>(() => new SeatIndex(seat));

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    public void InvalidRosterSizesAreRejected(int count) => Assert.Throws<ArgumentException>(() =>
        new SeatRing(Enumerable.Range(0, count).Select(index => new PlayerId(index.ToString(System.Globalization.CultureInfo.InvariantCulture)))));

    [Fact]
    public void RosterRejectsDuplicatesAndDefaultIdsAndDefensivelyCopiesInput()
    {
        var a = new PlayerId("A");
        var b = new PlayerId("B");
        Assert.Throws<ArgumentException>(() => new SeatRing([a, a]));
        Assert.Throws<ArgumentException>(() => new SeatRing([a, default]));
        PlayerId[] roster = [a, b];
        var ring = new SeatRing(roster);
        roster[0] = b;
        Assert.Equal(a, ring[new SeatIndex(0)]);
        Assert.Equal(new SeatIndex(0), ring.Next(new SeatIndex(1)));
        Assert.Throws<ArgumentException>(() => ring.SeatOf(new PlayerId("unknown")));
    }

    [Fact]
    public void CardsRejectInvalidValuesAndPreservePhysicalIdentity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CardId(112));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CardId(-1));
        Assert.Throws<ArgumentException>(() => new Card(new CardId(0), CardKind.Number, 0));
        Assert.Throws<ArgumentException>(() => new Card(new CardId(0), CardKind.Number, 13));
        Assert.Throws<ArgumentException>(() => new Card(new CardId(0), CardKind.Zero, 1));
        Assert.NotEqual(new Card(new CardId(0), CardKind.Number, 1), new Card(new CardId(1), CardKind.Number, 1));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(31)]
    [InlineData(32)]
    public void ActiveScoreMustBeNonnegativeAndBelow31(int score) => Assert.Throws<ArgumentException>(() =>
        new PlayerState(new PlayerId("A"), PlayerRoundStatus.Active, TerminationReason.None, [], score, null, 0));

    [Fact]
    public void RoundStateProtectsScoringAndHistoryInvariants()
    {
        var a = new PlayerId("A");
        Assert.Throws<ArgumentException>(() => new PlayerState(a, PlayerRoundStatus.Perfect31, TerminationReason.None, [], 31, 31, 0));
        Assert.Throws<ArgumentException>(() => new PlayerState(a, PlayerRoundStatus.Stopped, TerminationReason.None, [], 5, 5, 0));
        Assert.Throws<ArgumentException>(() => new PlayerState(a, PlayerRoundStatus.Zeroed, TerminationReason.None, [], 5, 0, 0));
        Assert.Throws<ArgumentException>(() => new PlayerState(a, PlayerRoundStatus.Active, TerminationReason.None,
            [new Card(new CardId(96), CardKind.Zero)], 0, null, 0));
        var number = new Card(new CardId(0), CardKind.Number, 1);
        Assert.Throws<ArgumentException>(() => new PlayerState(a, PlayerRoundStatus.Active, TerminationReason.None,
            [number, number], 2, null, 0));
        Card[] history = [number];
        var player = new PlayerState(a, PlayerRoundStatus.Active, TerminationReason.None, history, 1, null, 0);
        history[0] = new Card(new CardId(8), CardKind.Number, 2);
        Assert.Equal(number, player.NumberHistory[0]);
        Assert.True(player.IsActive);
    }

    [Fact]
    public void ContextsValidateQuotaParentageAndDrawerOwnership()
    {
        var a = new PlayerId("A");
        Assert.Throws<ArgumentException>(() => new DrawContext(new ContextId(1), null, DrawKind.NormalDraw, a, 2));
        Assert.Throws<ArgumentException>(() => new DrawContext(new ContextId(2), new ContextId(1), DrawKind.DrawTwo, a, -1));
        Assert.Throws<ArgumentException>(() => new DrawContext(new ContextId(2), null, DrawKind.DrawTwo, a, 2));
        Assert.Throws<ArgumentException>(() => new EffectContext(new ContextId(2), new ContextId(1),
            new Card(new CardId(96), CardKind.Zero), a, new PlayerId("B")));
        Assert.Throws<ArgumentException>(() => new EffectContext(new ContextId(2), new ContextId(1),
            new Card(new CardId(0), CardKind.Number, 1), a, a));
        Assert.Throws<ArgumentException>(() => new DrawContext(new ContextId(1), new ContextId(2), DrawKind.DrawTwo, a, 2));
    }

    [Fact]
    public void DecisionsRequirePositiveSequenceAndLegalSemantics()
    {
        var a = new PlayerId("A");
        Assert.Throws<ArgumentOutOfRangeException>(() => new DecisionId(0));
        Assert.Throws<ArgumentException>(() => new PendingDecision(default, DecisionKind.PlayerAction, a, []));
        Assert.Throws<ArgumentException>(() => new PendingDecision(new DecisionId(1), DecisionKind.PlayerAction, a, [a]));
        Assert.Throws<ArgumentException>(() => new PendingDecision(new DecisionId(1), DecisionKind.EffectTarget, a, []));
        var decision = new PendingDecision(new DecisionId(1), DecisionKind.PlayerAction, a, []);
        Assert.Equal([PlayerAction.Draw, PlayerAction.Stop], decision.AllowedActions.ToArray());
    }

    [Fact]
    public void RandomStateRequires256BitsAndCopiesSeed()
    {
        Assert.Throws<ArgumentException>(() => new RandomState(new byte[31]));
        Assert.Throws<ArgumentException>(() => new RandomState(new byte[32], RandomState.WordCapacity + 1));
        var seed = new byte[32];
        var state = new RandomState(seed, 17);
        seed[0] = 1;
        Assert.Equal(0, state.Seed[0]);
        Assert.Equal(1UL, state.BlockCounter);
        Assert.Equal(1, state.WordInBlock);
    }
}
