using ThirteenThirtyOne.Application.DevelopmentGameplay;

namespace ThirteenThirtyOne.GameBackend.DevelopmentGameplay;

public sealed record ExpireDecisionRequest(long DecisionId, DevelopmentDecisionKind? DecisionKind);

