namespace ThirteenThirtyOne.Game.Domain;

public sealed record GameplayTimerExpired(DecisionId DecisionId, DecisionKind Kind) : EngineInput;
