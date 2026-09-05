using System.Collections.Immutable;

namespace ThirteenThirtyOne.Game.Domain;

public sealed class PendingDecision
{
    public PendingDecision(DecisionId id, DecisionKind kind, PlayerId owner, IEnumerable<PlayerId> allowedTargets)
    {
        ArgumentNullException.ThrowIfNull(allowedTargets);
        AllowedTargets = allowedTargets.ToImmutableArray();
        if (id.Value < 1 || !Enum.IsDefined(kind) || string.IsNullOrWhiteSpace(owner.Value)
            || AllowedTargets.Any(player => string.IsNullOrWhiteSpace(player.Value))
            || AllowedTargets.Distinct().Count() != AllowedTargets.Length
            || (kind == DecisionKind.PlayerAction ? !AllowedTargets.IsEmpty : !AllowedTargets.Contains(owner)))
        {
            throw new ArgumentException("Invalid pending decision.");
        }

        Id = id;
        Kind = kind;
        Owner = owner;
    }

    public DecisionId Id { get; }
    public DecisionKind Kind { get; }
    public PlayerId Owner { get; }
    public ImmutableArray<PlayerId> AllowedTargets { get; }
    public ImmutableArray<PlayerAction> AllowedActions => Kind == DecisionKind.PlayerAction
        ? [PlayerAction.Draw, PlayerAction.Stop] : [PlayerAction.SelectTarget];
}
