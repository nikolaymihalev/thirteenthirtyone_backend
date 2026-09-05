using ThirteenThirtyOne.Game.Domain;
using ThirteenThirtyOne.Game.Domain.Events;
using ThirteenThirtyOne.Game.Engine.Randomness;

namespace ThirteenThirtyOne.Game.Engine;

public static partial class GameEngine
{
    public static EngineTransitionResult CreateGame(GameId gameId, IEnumerable<PlayerId> roster,
        RandomState randomState, EngineCompatibility compatibility)
    {
        ArgumentNullException.ThrowIfNull(randomState);
        if (compatibility != EngineCompatibility.V1)
        {
            throw new NotSupportedException("Unsupported gameplay or deterministic algorithm compatibility version.");
        }

        var acceptedRoster = new SeatRing(roster);
        var random = new ChaCha20Random(randomState);
        var shuffledRoster = acceptedRoster.Players.ToList();
        var beforeSeats = random.Snapshot().WordPosition;
        random.Shuffle(shuffledRoster);
        var afterSeats = random.Snapshot().WordPosition;
        var seats = new SeatRing(shuffledRoster);
        var starter = new SeatIndex((int)random.NextUniformIntExclusive((ulong)seats.Count));
        var afterStarter = random.Snapshot().WordPosition;
        var players = seats.Players.Select(player => new PlayerState(player, PlayerRoundStatus.Active,
            TerminationReason.None, [], 0, null, 0));
        var initial = new GameplayState(gameId, compatibility, seats, 1, RoundKind.Normal, starter, null, players,
            CardCatalog.Cards, [], [], [], null, BoundaryKind.WaitPlayerAction, 0, 0, random.Snapshot());
        var transition = new Transition(initial);
        transition.Events.Add(new GameCreated(gameId, compatibility));
        for (var index = 0; index < seats.Count; index++)
        {
            transition.Events.Add(new SeatAssigned(seats.Players[index], new SeatIndex(index)));
        }

        transition.Events.Add(new RandomOperationCompleted(RandomOperation.SeatAssignment, beforeSeats, afterSeats, compatibility));
        transition.Events.Add(new RandomOperationCompleted(RandomOperation.InitialStarter, afterSeats, afterStarter, compatibility));
        transition.Shuffle(transition.DrawPile, RandomOperation.InitialDeckShuffle);
        BeginRound(transition);
        return transition.Finish();
    }

    internal static void BeginRound(Transition transition)
    {
        transition.Events.Add(new RoundStarted(transition.RoundNumber, transition.RoundKind, transition.Seats[transition.RoundStarter]));
        for (var offset = 0; offset < transition.Seats.Count; offset++)
        {
            var seat = (transition.RoundStarter.Value + offset) % transition.Seats.Count;
            var player = transition.Players[seat];
            if (!player.IsParticipating)
            {
                continue;
            }

            Card card;
            do
            {
                card = DrawPhysicalCard(transition);
                if (!card.IsNumber)
                {
                    transition.OpeningSetAside.Add(card);
                    transition.Events.Add(new OpeningEffectSetAside(player.Player, card.Id));
                }
            }
            while (!card.IsNumber);

            transition.SetPlayer(new PlayerState(player.Player, PlayerRoundStatus.Active, TerminationReason.None,
                [card], card.Number, null, player.TotalScore, player.TieBreakRoundResult));
            transition.Events.Add(new OpeningCardDealt(player.Player, card.Id, card.Number));
        }

        if (transition.OpeningSetAside.Count > 0)
        {
            transition.DrawPile.AddRange(transition.OpeningSetAside);
            transition.OpeningSetAside.Clear();
            transition.Shuffle(transition.DrawPile, RandomOperation.OpeningReintegration);
        }

        transition.CompletedTurnOwner = null;
        transition.TurnOwner = transition.Seats[transition.RoundStarter];
        transition.RequestDecision(DecisionKind.PlayerAction, transition.TurnOwner.Value);
    }

    private static Card DrawPhysicalCard(Transition transition)
    {
        if (transition.DrawPile.Count == 0)
        {
            if (transition.DiscardPile.Count == 0)
            {
                throw new InvalidOperationException("No physical cards available for a required draw.");
            }

            transition.DrawPile.AddRange(transition.DiscardPile);
            transition.DiscardPile.Clear();
            transition.Shuffle(transition.DrawPile, RandomOperation.DiscardRefill);
        }

        var card = transition.DrawPile[0];
        transition.DrawPile.RemoveAt(0);
        return card;
    }
}
