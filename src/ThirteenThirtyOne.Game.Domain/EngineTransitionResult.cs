using System.Collections.Immutable;

namespace ThirteenThirtyOne.Game.Domain;

public sealed record EngineTransitionResult(
    GameplayState State,
    ImmutableArray<Events.GameEvent> Events,
    EngineRejection Rejection,
    string StateValidationHash)
{
    public bool IsAccepted => Rejection == EngineRejection.None;
    public PendingDecision? PendingDecision => State.PendingDecision;
    public BoundaryKind Boundary => State.Boundary;
    public bool IsSafeGameplayBoundary => State.IsSafeGameplayBoundary;
}
