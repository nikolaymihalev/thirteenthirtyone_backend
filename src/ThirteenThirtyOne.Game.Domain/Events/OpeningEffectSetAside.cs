namespace ThirteenThirtyOne.Game.Domain.Events;

public sealed record OpeningEffectSetAside(PlayerId Player, CardId Card) : GameEvent;
