using System.Collections.Immutable;

namespace ThirteenThirtyOne.Application.DevelopmentGameplay;

public sealed record DevelopmentDecisionView(long DecisionId, string DecisionKind, string OwnerPlayerId,
    ImmutableArray<string> AllowedActions, ImmutableArray<string> AllowedTargets);
