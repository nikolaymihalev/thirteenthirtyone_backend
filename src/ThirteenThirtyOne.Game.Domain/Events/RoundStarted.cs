namespace ThirteenThirtyOne.Game.Domain.Events;

public sealed record RoundStarted(int Round, RoundKind Kind, PlayerId Starter) : GameEvent;
