namespace ThirteenThirtyOne.Game.Domain.Events;

public sealed record EffectResolved(ContextId Context, CardId Card, bool CancelledByDeadlock) : GameEvent;
