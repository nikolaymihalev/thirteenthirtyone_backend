namespace ThirteenThirtyOne.Game.Domain;

public sealed record PlayerDecision(DecisionId DecisionId, PlayerId Player, PlayerAction Action,
    PlayerId? Target = null) : EngineInput;
