namespace ThirteenThirtyOne.Game.Domain.Events;

public sealed record EffectStarted(ContextId Context, CardId Card, PlayerId TurnOwner, PlayerId EffectDrawer, PlayerId DecisionOwner) : GameEvent;
