namespace ThirteenThirtyOne.Game.Domain;

public readonly record struct DecisionId
{
    public DecisionId(long value)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        Value = value;
    }

    public long Value { get; }
}
