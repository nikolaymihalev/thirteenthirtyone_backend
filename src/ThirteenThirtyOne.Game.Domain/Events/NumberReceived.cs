namespace ThirteenThirtyOne.Game.Domain.Events;

public sealed record NumberReceived(PlayerId Player, CardId Card, ContextId Context, int RemainingNumbers) : GameEvent;
