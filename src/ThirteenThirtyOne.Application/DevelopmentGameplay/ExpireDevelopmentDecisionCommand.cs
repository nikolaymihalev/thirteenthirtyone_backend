namespace ThirteenThirtyOne.Application.DevelopmentGameplay;

public sealed record ExpireDevelopmentDecisionCommand(string? GameId, long DecisionId, DevelopmentDecisionKind DecisionKind);
