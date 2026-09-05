using ThirteenThirtyOne.Game.Domain;
using ThirteenThirtyOne.Game.Domain.Events;
using ThirteenThirtyOne.Game.Engine.Randomness;

namespace ThirteenThirtyOne.Game.Engine;

// Private working copy for one synchronous proposal. Never escapes the engine.
internal sealed class Transition
{
    internal Transition(GameplayState state)
    {
        GameId = state.GameId;
        Compatibility = state.Compatibility;
        Seats = state.Seats;
        RoundNumber = state.RoundNumber;
        RoundKind = state.RoundKind;
        RoundStarter = state.RoundStarter;
        TurnOwner = state.TurnOwner;
        Players = state.Players.ToList();
        DrawPile = state.DrawPile.ToList();
        DiscardPile = state.DiscardPile.ToList();
        OpeningSetAside = state.OpeningSetAside.ToList();
        Stack = state.ResolutionStack.ToList();
        PendingDecision = state.PendingDecision;
        Boundary = state.Boundary;
        DecisionSequence = state.DecisionSequence;
        ContextSequence = state.ContextSequence;
        Random = new ChaCha20Random(state.Random);
        Winner = state.Winner;
        CompletedTurnOwner = state.CompletedTurnOwner;
    }

    internal GameId GameId { get; }
    internal EngineCompatibility Compatibility { get; }
    internal SeatRing Seats { get; }
    internal int RoundNumber { get; set; }
    internal RoundKind RoundKind { get; set; }
    internal SeatIndex RoundStarter { get; set; }
    internal PlayerId? TurnOwner { get; set; }
    internal List<PlayerState> Players { get; }
    internal List<Card> DrawPile { get; }
    internal List<Card> DiscardPile { get; }
    internal List<Card> OpeningSetAside { get; }
    internal List<ResolutionContext> Stack { get; }
    internal PendingDecision? PendingDecision { get; set; }
    internal BoundaryKind Boundary { get; set; }
    internal long DecisionSequence { get; set; }
    internal long ContextSequence { get; set; }
    internal ChaCha20Random Random { get; }
    internal PlayerId? Winner { get; set; }
    internal PlayerId? CompletedTurnOwner { get; set; }
    internal List<GameEvent> Events { get; } = [];

    internal PlayerState Player(PlayerId player) => Players[Seats.SeatOf(player).Value];

    internal void SetPlayer(PlayerState player) => Players[Seats.SeatOf(player.Player).Value] = player;

    internal ContextId NextContextId() => new(checked(++ContextSequence));

    internal GameplayState Freeze() => new(GameId, Compatibility, Seats, RoundNumber, RoundKind, RoundStarter,
        TurnOwner, Players, DrawPile, DiscardPile, OpeningSetAside, Stack, PendingDecision, Boundary,
        DecisionSequence, ContextSequence, Random.Snapshot(), Winner, CompletedTurnOwner);

    internal EngineTransitionResult Finish()
    {
        var state = Freeze();
        StateValidator.Validate(state);
        return new EngineTransitionResult(state, [.. Events], EngineRejection.None, StateHasher.Compute(state));
    }

    internal void RequestDecision(DecisionKind kind, PlayerId owner)
    {
        PendingDecision = new PendingDecision(new DecisionId(checked(++DecisionSequence)), kind, owner,
            kind == DecisionKind.EffectTarget ? Players.Where(player => player.IsActive).Select(player => player.Player) : []);
        Boundary = kind == DecisionKind.PlayerAction ? BoundaryKind.WaitPlayerAction : BoundaryKind.WaitTarget;
        Events.Add(new DecisionRequested(PendingDecision.Id, kind, owner));
    }

    internal void Shuffle(List<Card> cards, RandomOperation operation)
    {
        var before = Random.Snapshot().WordPosition;
        Random.Shuffle(cards);
        Events.Add(new RandomOperationCompleted(operation, before, Random.Snapshot().WordPosition, Compatibility));
    }
}
