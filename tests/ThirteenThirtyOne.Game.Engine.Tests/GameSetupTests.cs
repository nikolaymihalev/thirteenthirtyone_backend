using ThirteenThirtyOne.Game.Domain;
using ThirteenThirtyOne.Game.Domain.Events;
using Xunit;

namespace ThirteenThirtyOne.Game.Engine.Tests;

public sealed class GameSetupTests
{
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void CreationProducesDeterministicSeatsOpeningDealAndFirstDecision(int count)
    {
        var roster = Enumerable.Range(0, count).Select(index => new PlayerId(((char)('A' + index)).ToString())).ToArray();
        var first = GameEngine.CreateGame(new GameId("setup"), roster, new RandomState(new byte[32]), EngineCompatibility.V1);
        var second = GameEngine.CreateGame(new GameId("setup"), roster, new RandomState(new byte[32]), EngineCompatibility.V1);

        Assert.Equal(first.StateValidationHash, second.StateValidationHash);
        Assert.Equal(first.Events.ToArray(), second.Events.ToArray());
        Assert.Equal(roster.OrderBy(player => player.Value, StringComparer.Ordinal),
            first.State.Seats.Players.OrderBy(player => player.Value, StringComparer.Ordinal));
        Assert.All(first.State.Players, player =>
        {
            Assert.Single(player.NumberHistory);
            Assert.Equal(player.NumberHistory[0].Number, player.CurrentScore);
            Assert.Equal(0, player.TotalScore);
            Assert.True(player.IsActive);
        });
        Assert.Equal(first.State.Seats[first.State.RoundStarter], first.PendingDecision!.Owner);
        Assert.Equal(1, first.PendingDecision.Id.Value);
        Assert.Equal(BoundaryKind.WaitPlayerAction, first.Boundary);
        Assert.False(first.IsSafeGameplayBoundary);
        Assert.Empty(first.State.DiscardPile);
        Assert.Empty(first.State.OpeningSetAside);
        StateValidator.Validate(first.State);
    }

    [Fact]
    public void StarterConsumesAnIndependentRandomSelectionAndNeedNotBeSeatZero()
    {
        var results = Enumerable.Range(0, 12).Select(value =>
        {
            var seed = new byte[32];
            seed[0] = (byte)value;
            return GameEngine.CreateGame(new GameId("starters"),
                [new PlayerId("A"), new PlayerId("B"), new PlayerId("C"), new PlayerId("D")],
                new RandomState(seed), EngineCompatibility.V1);
        }).ToArray();
        Assert.Contains(results, result => result.State.RoundStarter.Value != 0);
        foreach (var result in results)
        {
            var seats = Assert.Single(result.Events.OfType<RandomOperationCompleted>(), item => item.Operation == RandomOperation.SeatAssignment);
            var starter = Assert.Single(result.Events.OfType<RandomOperationCompleted>(), item => item.Operation == RandomOperation.InitialStarter);
            var deck = Assert.Single(result.Events.OfType<RandomOperationCompleted>(), item => item.Operation == RandomOperation.InitialDeckShuffle);
            Assert.Equal(seats.AfterWord, starter.BeforeWord);
            Assert.Equal(starter.BeforeWord + 1, starter.AfterWord);
            Assert.Equal(starter.AfterWord, deck.BeforeWord);
        }
    }

    [Fact]
    public void InvalidCreationRejectsRosterAndUnsupportedCompatibility()
    {
        Assert.Throws<ArgumentException>(() => GameEngine.CreateGame(new GameId("bad"), [new PlayerId("A")],
            new RandomState(new byte[32]), EngineCompatibility.V1));
        Assert.Throws<ArgumentException>(() => GameEngine.CreateGame(new GameId("bad"), [new PlayerId("A"), new PlayerId("A")],
            new RandomState(new byte[32]), EngineCompatibility.V1));
        Assert.Throws<NotSupportedException>(() => GameEngine.CreateGame(new GameId("bad"), [new PlayerId("A"), new PlayerId("B")],
            new RandomState(new byte[32]), EngineCompatibility.V1 with { RngAlgorithmVersion = 2 }));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FilteredOpeningCompletesOnePlayerBeforeNextAndReintegratesEffectsOnce(bool tieBreak)
    {
        PlayerId[] roster = [new("A"), new("B"), new("C"), new("D")];
        // Start at D; D receives effects then number 1; A (or B in the tie-break) gets number 2.
        int[] prefix = tieBreak ? [96, 98, 0, 102, 8] : [96, 98, 0, 102, 8, 16, 24];
        var pile = prefix.Select(id => CardCatalog.Cards[id])
            .Concat(CardCatalog.Cards.Where(card => !prefix.Contains(card.Id.Value))).ToArray();
        var players = roster.Select((player, seat) => new PlayerState(player,
            tieBreak && seat is 0 or 2 ? PlayerRoundStatus.NotParticipating : PlayerRoundStatus.Active,
            TerminationReason.None, [], 0, null, tieBreak ? 150 : 0));
        var before = new GameplayState(new GameId("opening"), EngineCompatibility.V1, new SeatRing(roster),
            1, tieBreak ? RoundKind.TieBreak : RoundKind.Normal, new SeatIndex(3), null, players,
            pile, [], [], [], null, BoundaryKind.WaitPlayerAction, 0, 0, new RandomState(new byte[32]));
        var transition = new Transition(before);

        GameEngine.BeginRound(transition);
        var result = transition.Finish();

        Assert.Equal(tieBreak ? new[] { roster[3], roster[1] } : [roster[3], roster[0], roster[1], roster[2]],
            result.Events.OfType<OpeningCardDealt>().Select(item => item.Player).ToArray());
        var filtered = result.Events.Where(item => item is OpeningEffectSetAside or OpeningCardDealt).ToArray();
        Assert.Equal(roster[3], Assert.IsType<OpeningEffectSetAside>(filtered[0]).Player);
        Assert.Equal(roster[3], Assert.IsType<OpeningEffectSetAside>(filtered[1]).Player);
        Assert.Equal(roster[3], Assert.IsType<OpeningCardDealt>(filtered[2]).Player);
        Assert.Single(result.Events.OfType<RandomOperationCompleted>(), item => item.Operation == RandomOperation.OpeningReintegration);
        Assert.Empty(result.Events.OfType<EffectStarted>());
        Assert.Empty(result.State.OpeningSetAside);
        Assert.Empty(result.State.DiscardPile);
        Assert.All(prefix.Where(id => id >= 96), id => Assert.Contains(result.State.DrawPile, card => card.Id.Value == id));
        Assert.Equal(0UL, before.Random.WordPosition);
    }
}
