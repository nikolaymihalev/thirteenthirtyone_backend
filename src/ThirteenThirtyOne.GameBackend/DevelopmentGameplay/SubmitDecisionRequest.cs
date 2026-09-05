using ThirteenThirtyOne.Application.DevelopmentGameplay;

namespace ThirteenThirtyOne.GameBackend.DevelopmentGameplay;

public sealed record SubmitDecisionRequest(long DecisionId, string? PlayerId, DevelopmentPlayerAction? Action, string? TargetPlayerId);
