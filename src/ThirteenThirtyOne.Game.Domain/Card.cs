namespace ThirteenThirtyOne.Game.Domain;

public sealed record Card
{
    public Card(CardId id, CardKind kind, int number = 0)
    {
        if (!Enum.IsDefined(kind) || (kind == CardKind.Number ? number is < 1 or > 12 : number != 0))
        {
            throw new ArgumentException("A card must be a number 1–12 or one of the five effects.");
        }

        Id = id;
        Kind = kind;
        Number = number;
    }

    public CardId Id { get; }
    public CardKind Kind { get; }
    public int Number { get; }
    public bool IsNumber => Kind == CardKind.Number;
}
