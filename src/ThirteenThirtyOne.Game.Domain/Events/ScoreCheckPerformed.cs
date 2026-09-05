namespace ThirteenThirtyOne.Game.Domain.Events;

public sealed record ScoreCheckPerformed(PlayerId Player, ScoreCheck Check, bool Matched) : GameEvent;
