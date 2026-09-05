namespace ThirteenThirtyOne.Game.Domain.Events;

public sealed record CardDrawn(PlayerId TurnOwner, PlayerId Recipient, ContextId Context, CardId Card, CardKind Kind) : GameEvent;
