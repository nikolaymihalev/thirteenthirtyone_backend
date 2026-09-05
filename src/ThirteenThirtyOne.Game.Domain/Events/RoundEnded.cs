namespace ThirteenThirtyOne.Game.Domain.Events;

public sealed record RoundEnded(int Round, RoundKind Kind) : GameEvent;
