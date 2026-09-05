using ThirteenThirtyOne.Game.Domain;

namespace ThirteenThirtyOne.Game.Engine.Tests;

// Test-only snapshot construction. Production has no deck override or cheat command.
internal sealed class Scenario
{
    private readonly List<Card> available = CardCatalog.Cards.ToList();
    private readonly List<Card> prefix = [];
    private readonly PlayerState[] players;

    internal Scenario(int count = 2)
    {
        Seats = new SeatRing(Enumerable.Range(0, count).Select(index => new PlayerId(((char)('A' + index)).ToString())));
        players = Seats.Players.Select(player => new PlayerState(player, PlayerRoundStatus.Active,
            TerminationReason.None, [], 0, null, 0)).ToArray();
    }

    internal SeatRing Seats { get; }
    internal int Starter { get; set; }
    internal int Owner { get; set; }
    internal RoundKind Kind { get; set; }
    internal int RoundNumber { get; set; } = 1;
    internal bool RemainingToDiscard { get; set; }

    internal Card Number(int number) => Take(card => card.IsNumber && card.Number == number);
    internal Card Effect(CardKind kind) => Take(card => card.Kind == kind);
    internal Scenario Draw(params Card[] cards)
    {
        prefix.AddRange(cards);
        return this;
    }

    internal Scenario Player(int seat, int score, int[]? history = null, PlayerRoundStatus status = PlayerRoundStatus.Active,
        long total = 0, TerminationReason reason = TerminationReason.None)
    {
        var cards = (history ?? []).Select(Number).ToArray();
        var award = status is PlayerRoundStatus.Active or PlayerRoundStatus.NotParticipating ? (int?)null
            : status == PlayerRoundStatus.Perfect31 ? 50
            : status is PlayerRoundStatus.Bust13 or PlayerRoundStatus.BustOver31 or PlayerRoundStatus.Zeroed ? 0 : score;
        if (status == PlayerRoundStatus.Stopped && reason == TerminationReason.None)
        {
            reason = TerminationReason.PlayerChoice;
        }

        if (status == PlayerRoundStatus.ForcedStop && reason == TerminationReason.None)
        {
            reason = TerminationReason.StopEffect;
        }

        players[seat] = new PlayerState(Seats.Players[seat], status, reason, cards, score, award, total);
        return this;
    }

    internal Scenario AllRemainingNumbersInHistory(int seat, int score)
    {
        var numbers = available.Where(card => card.IsNumber).ToArray();
        available.RemoveAll(card => card.IsNumber);
        var old = players[seat];
        players[seat] = new PlayerState(old.Player, old.Status, old.Reason,
            old.NumberHistory.Concat(numbers), score, old.RoundScore, old.TotalScore);
        return this;
    }

    internal GameplayState Build(bool safe = false)
    {
        var state = new GameplayState(new GameId("scenario"), EngineCompatibility.V1, Seats, RoundNumber, Kind,
            new SeatIndex(Starter), safe ? null : Seats.Players[Owner], players,
            RemainingToDiscard ? prefix : prefix.Concat(available),
            RemainingToDiscard ? available : [], [], [],
            safe ? null : new PendingDecision(new DecisionId(1), DecisionKind.PlayerAction, Seats.Players[Owner], []),
            safe ? BoundaryKind.SafePostResolution : BoundaryKind.WaitPlayerAction,
            1, 0, new RandomState(new byte[32]), completedTurnOwner: safe ? Seats.Players[Owner] : null);
        StateValidator.Validate(state);
        return state;
    }

    internal static EngineTransitionResult Act(GameplayState state, PlayerAction action, PlayerId? target = null) =>
        GameEngine.Apply(state, new PlayerDecision(state.PendingDecision!.Id, state.PendingDecision.Owner, action, target));

    internal static EngineTransitionResult Target(GameplayState state, int seat) =>
        Act(state, PlayerAction.SelectTarget, state.Seats.Players[seat]);

    private Card Take(Func<Card, bool> predicate)
    {
        var card = available.First(predicate);
        available.Remove(card);
        return card;
    }
}
