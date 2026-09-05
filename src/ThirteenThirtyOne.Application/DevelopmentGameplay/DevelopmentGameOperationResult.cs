using System.Collections.Immutable;

namespace ThirteenThirtyOne.Application.DevelopmentGameplay;

public sealed record DevelopmentGameOperationResult(DevelopmentResultKind Kind, bool Accepted, string? Rejection,
    ImmutableArray<string> EventTypes, DevelopmentGameView? Game);
