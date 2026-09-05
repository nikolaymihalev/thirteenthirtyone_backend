using System.Collections.Immutable;
using ThirteenThirtyOne.Application.DevelopmentGameplay;

namespace ThirteenThirtyOne.GameBackend.DevelopmentGameplay;

public sealed record DevelopmentGameResponse(bool Accepted, string? Rejection, ImmutableArray<string> EventTypes, DevelopmentGameView? Game)
{
    public static DevelopmentGameResponse From(DevelopmentGameOperationResult result) =>
        new(result.Accepted, result.Rejection, result.EventTypes, result.Game);
}
