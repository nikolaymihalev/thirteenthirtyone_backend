namespace ThirteenThirtyOne.Game.Domain.Events;

public sealed record OpeningCardDealt(PlayerId Player, CardId Card, int Number) : GameEvent;
