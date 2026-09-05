namespace ThirteenThirtyOne.Game.Domain.Events;

public sealed record TargetSelected(ContextId Context, PlayerId DecisionOwner, PlayerId Target, bool ByTimeout) : GameEvent;
