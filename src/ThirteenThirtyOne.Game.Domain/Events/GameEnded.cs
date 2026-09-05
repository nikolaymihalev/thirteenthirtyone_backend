namespace ThirteenThirtyOne.Game.Domain.Events;

public sealed record GameEnded(GameId Game, int Round, PlayerId Winner) : GameEvent;
