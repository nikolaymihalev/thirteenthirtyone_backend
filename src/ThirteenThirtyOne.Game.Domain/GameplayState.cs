using System.Collections.Immutable;

namespace ThirteenThirtyOne.Game.Domain;

// Immutable authoritative snapshot. Global consistency is checked by Engine.StateValidator.
public sealed class GameplayState
{
    public GameplayState(GameId gameId, EngineCompatibility compatibility, SeatRing seats,
        int roundNumber, RoundKind roundKind, SeatIndex roundStarter, PlayerId? turnOwner,
        IEnumerable<PlayerState> players, IEnumerable<Card> drawPile, IEnumerable<Card> discardPile,
        IEnumerable<Card> openingSetAside, IEnumerable<ResolutionContext> resolutionStack,
        PendingDecision? pendingDecision, BoundaryKind boundary, long decisionSequence,
        long contextSequence, RandomState random, PlayerId? winner = null, PlayerId? completedTurnOwner = null)
    {
        ArgumentNullException.ThrowIfNull(compatibility);
        ArgumentNullException.ThrowIfNull(seats);
        ArgumentNullException.ThrowIfNull(random);
        if (string.IsNullOrWhiteSpace(gameId.Value) || roundNumber < 1 || decisionSequence < 0 || contextSequence < 0
            || !Enum.IsDefined(roundKind) || !Enum.IsDefined(boundary) || roundStarter.Value >= seats.Count)
        {
            throw new ArgumentException("Invalid gameplay snapshot metadata.");
        }

        GameId = gameId;
        Compatibility = compatibility;
        Seats = seats;
        RoundNumber = roundNumber;
        RoundKind = roundKind;
        RoundStarter = roundStarter;
        TurnOwner = turnOwner;
        Players = players.ToImmutableArray();
        DrawPile = drawPile.ToImmutableArray();
        DiscardPile = discardPile.ToImmutableArray();
        OpeningSetAside = openingSetAside.ToImmutableArray();
        ResolutionStack = resolutionStack.ToImmutableArray();
        PendingDecision = pendingDecision;
        Boundary = boundary;
        DecisionSequence = decisionSequence;
        ContextSequence = contextSequence;
        Random = random;
        Winner = winner;
        CompletedTurnOwner = completedTurnOwner;
    }

    public GameId GameId { get; }
    public EngineCompatibility Compatibility { get; }
    public SeatRing Seats { get; }
    public int RoundNumber { get; }
    public RoundKind RoundKind { get; }
    public SeatIndex RoundStarter { get; }
    public PlayerId? TurnOwner { get; }
    public ImmutableArray<PlayerState> Players { get; }
    public ImmutableArray<Card> DrawPile { get; }
    public ImmutableArray<Card> DiscardPile { get; }
    public ImmutableArray<Card> OpeningSetAside { get; }
    public ImmutableArray<ResolutionContext> ResolutionStack { get; }
    public PendingDecision? PendingDecision { get; }
    public BoundaryKind Boundary { get; }
    public long DecisionSequence { get; }
    public long ContextSequence { get; }
    public RandomState Random { get; }
    public PlayerId? Winner { get; }
    public PlayerId? CompletedTurnOwner { get; }
    public bool IsSafeGameplayBoundary => Boundary == BoundaryKind.SafePostResolution;
    public PlayerState Player(PlayerId player) => Players[Seats.SeatOf(player).Value];
}
