namespace ThirteenThirtyOne.Game.Domain;

public readonly record struct PlayerId
{
    public PlayerId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public string Value { get; }
}
