namespace ThirteenThirtyOne.Game.Domain.Events;

public sealed record ScoreChanged(PlayerId Player, int Before, int After) : GameEvent;
