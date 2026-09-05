namespace ThirteenThirtyOne.Application.DevelopmentGameplay;

public sealed record SubmitDevelopmentDecisionCommand(string? GameId, long DecisionId, string? PlayerId,
    DevelopmentPlayerAction Action, string? TargetPlayerId = null);
