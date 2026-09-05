namespace ThirteenThirtyOne.Game.Domain;

public sealed record DrawContext : ResolutionContext
{
    public DrawContext(ContextId id, ContextId? parentId, DrawKind kind, PlayerId recipient, int remainingNumbers)
        : base(id, parentId)
    {
        if (!Enum.IsDefined(kind) || string.IsNullOrWhiteSpace(recipient.Value)
            || remainingNumbers < 0 || remainingNumbers > (kind == DrawKind.NormalDraw ? 1 : 2)
            || (kind == DrawKind.NormalDraw ? parentId is not null : parentId is null))
        {
            throw new ArgumentException("Invalid numerical obligation.");
        }

        Kind = kind;
        Recipient = recipient;
        RemainingNumbers = remainingNumbers;
    }

    public DrawKind Kind { get; }
    public PlayerId Recipient { get; }
    public int RemainingNumbers { get; }
}
