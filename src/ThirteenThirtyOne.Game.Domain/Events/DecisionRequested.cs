namespace ThirteenThirtyOne.Game.Domain.Events;

public sealed record DecisionRequested(DecisionId Decision, DecisionKind Kind, PlayerId Owner) : GameEvent;
