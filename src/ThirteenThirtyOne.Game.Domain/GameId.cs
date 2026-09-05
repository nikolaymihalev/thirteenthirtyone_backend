namespace ThirteenThirtyOne.Game.Domain;

public readonly record struct GameId
{
    public GameId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public string Value { get; }
}
