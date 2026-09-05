namespace ThirteenThirtyOne.Game.Domain.Events;

public sealed record PlayerRoundStateChanged(PlayerId Player, PlayerRoundStatus Status, TerminationReason Reason, int RoundScore) : GameEvent;
