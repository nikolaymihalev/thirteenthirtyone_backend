using System.Collections.Immutable;

namespace ThirteenThirtyOne.Game.Domain;

public static class CardCatalog
{
    public static ImmutableArray<Card> Cards { get; } = Create();

    private static ImmutableArray<Card> Create()
    {
        var cards = ImmutableArray.CreateBuilder<Card>(112);
        for (var number = 1; number <= 12; number++)
        {
            for (var copy = 0; copy < 8; copy++)
            {
                cards.Add(new Card(new CardId(cards.Count), CardKind.Number, number));
            }
        }

        foreach (var (kind, count) in new[]
        {
            (CardKind.Zero, 2), (CardKind.PlusFive, 4), (CardKind.MinusFive, 4),
            (CardKind.DrawTwo, 3), (CardKind.Stop, 3),
        })
        {
            for (var copy = 0; copy < count; copy++)
            {
                cards.Add(new Card(new CardId(cards.Count), kind));
            }
        }

        return cards.MoveToImmutable();
    }
}
