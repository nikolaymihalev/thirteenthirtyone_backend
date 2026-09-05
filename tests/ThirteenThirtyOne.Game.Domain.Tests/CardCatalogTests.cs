using Xunit;

namespace ThirteenThirtyOne.Game.Domain.Tests;

public sealed class CardCatalogTests
{
    [Fact]
    public void CanonicalCatalogContains112StableUniquePhysicalCards()
    {
        var cards = CardCatalog.Cards;
        Assert.Equal(112, cards.Length);
        Assert.Equal(Enumerable.Range(0, 112), cards.Select(card => card.Id.Value));
        for (var number = 1; number <= 12; number++)
        {
            var group = cards.Where(card => card.IsNumber && card.Number == number).ToArray();
            Assert.Equal(8, group.Length);
            Assert.Equal((number - 1) * 8, group[0].Id.Value);
        }

        Assert.Equal(2, cards.Count(card => card.Kind == CardKind.Zero));
        Assert.Equal(4, cards.Count(card => card.Kind == CardKind.PlusFive));
        Assert.Equal(4, cards.Count(card => card.Kind == CardKind.MinusFive));
        Assert.Equal(3, cards.Count(card => card.Kind == CardKind.DrawTwo));
        Assert.Equal(3, cards.Count(card => card.Kind == CardKind.Stop));
        Assert.Equal(CardKind.Zero, cards[96].Kind);
        Assert.Equal(CardKind.PlusFive, cards[98].Kind);
        Assert.Equal(CardKind.MinusFive, cards[102].Kind);
        Assert.Equal(CardKind.DrawTwo, cards[106].Kind);
        Assert.Equal(CardKind.Stop, cards[109].Kind);
    }
}
