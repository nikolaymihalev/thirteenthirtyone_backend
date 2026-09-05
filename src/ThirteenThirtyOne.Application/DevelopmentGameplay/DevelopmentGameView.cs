using System.Collections.Immutable;

namespace ThirteenThirtyOne.Application.DevelopmentGameplay;

public sealed record DevelopmentGameView(string GameId, string StateHash, string RulesVersion, int EngineVersion,
    int RoundNumber, string RoundKind, string RoundStarterPlayerId, string Boundary, bool IsSafeGameplayBoundary,
    string? TurnOwnerPlayerId, string? CompletedTurnOwnerPlayerId, string? WinnerPlayerId,
    ImmutableArray<DevelopmentPlayerView> Players, DevelopmentDecisionView? PendingDecision,
    int DrawPileCount, int DiscardPileCount, int OpeningSetAsideCount,
    ImmutableArray<DevelopmentContextView> ResolutionStack, ulong RandomWordPosition);
