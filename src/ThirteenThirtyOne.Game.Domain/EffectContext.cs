namespace ThirteenThirtyOne.Game.Domain;

public sealed record EffectContext : ResolutionContext
{
    public EffectContext(ContextId id, ContextId parentId, Card sourceCard, PlayerId effectDrawer,
        PlayerId decisionOwner, PlayerId? effectTarget = null) : base(id, parentId)
    {
        ArgumentNullException.ThrowIfNull(sourceCard);
        if (sourceCard.IsNumber || string.IsNullOrWhiteSpace(effectDrawer.Value) || decisionOwner != effectDrawer
            || (effectTarget.HasValue && string.IsNullOrWhiteSpace(effectTarget.Value.Value)))
        {
            throw new ArgumentException("An effect requires an effect card and its actual drawer as decision owner.");
        }

        SourceCard = sourceCard;
        EffectDrawer = effectDrawer;
        DecisionOwner = decisionOwner;
        EffectTarget = effectTarget;
    }

    public Card SourceCard { get; }
    public PlayerId EffectDrawer { get; }
    public PlayerId DecisionOwner { get; }
    public PlayerId? EffectTarget { get; }
}
