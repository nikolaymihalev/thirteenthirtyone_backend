using System.Collections.Immutable;

namespace ThirteenThirtyOne.Application.DevelopmentGameplay;

public sealed record DevelopmentPlayerView(string PlayerId, int Seat, string RoundStatus, string TerminationReason,
    int CurrentScore, int? RoundScore, long TotalScore, int? TieBreakRoundResult, ImmutableArray<int> NumberHistory);
