namespace ThirteenThirtyOne.Game.Domain;

public readonly record struct SeatIndex
{
    public SeatIndex(int value)
    {
        if (value is < 0 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        Value = value;
    }

    public int Value { get; }
}
