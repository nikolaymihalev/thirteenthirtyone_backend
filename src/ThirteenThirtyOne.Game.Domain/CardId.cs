namespace ThirteenThirtyOne.Game.Domain;

public readonly record struct CardId
{
    public CardId(int value)
    {
        if (value is < 0 or >= 112)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        Value = value;
    }

    public int Value { get; }
}
