using ThirteenThirtyOne.Game.Domain;

namespace ThirteenThirtyOne.Game.Engine;

public static class StateValidator
{
    public static void Validate(GameplayState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Compatibility != EngineCompatibility.V1)
        {
            throw new NotSupportedException("Unsupported gameplay or deterministic algorithm compatibility version.");
        }

        Require(state.Players.Length == state.Seats.Count
            && state.Players.Select(player => player.Player).SequenceEqual(state.Seats.Players), "Players must be in immutable seat order.");
        Require(state.Players.Count(player => player.IsParticipating) >= 2, "A round requires at least two participants.");
        Require(state.Players[state.RoundStarter.Value].IsParticipating, "Round starter must participate.");
        Require(state.RoundKind != RoundKind.Normal || state.Players.All(player => player.IsParticipating), "Normal rounds include the full roster.");
        Require(state.OpeningSetAside.IsEmpty, "Opening deal must complete before yielding.");
        foreach (var player in state.Players.Where(player => player.IsParticipating))
        {
            Require(state.Boundary == BoundaryKind.GameTerminal || !player.NumberHistory.IsEmpty,
                "Participants must retain their opening/history cards until round completion.");
            if (player.NumberHistory.Length >= 2)
            {
                var lastPairIs13 = player.NumberHistory[^2].Number + player.NumberHistory[^1].Number == 13;
                Require(!lastPairIs13 || player.Status == PlayerRoundStatus.Bust13,
                    "A last numerical pair of 13 must already have terminated the recipient.");
                Require(player.Status != PlayerRoundStatus.Bust13 || lastPairIs13, "BUST_13 requires the final danger pair.");
            }
        }

        var cards = state.DrawPile.Concat(state.DiscardPile).Concat(state.OpeningSetAside)
            .Concat(state.Players.SelectMany(player => player.NumberHistory))
            .Concat(state.ResolutionStack.OfType<EffectContext>().Select(context => context.SourceCard)).ToArray();
        Require(cards.Length == 112 && cards.Select(card => card.Id).Distinct().Count() == 112, "Every physical card must have exactly one owner.");
        Require(cards.All(card => card == CardCatalog.Cards[card.Id.Value]), "Card identity must match canonical composition.");

        Require(state.ResolutionStack.Select(context => context.Id).Distinct().Count() == state.ResolutionStack.Length,
            "Context IDs cannot repeat.");
        for (var index = 0; index < state.ResolutionStack.Length; index++)
        {
            var context = state.ResolutionStack[index];
            Require(context.Id.Value <= state.ContextSequence, "Context sequence must cover every frame.");
            if (context is DrawContext draw)
            {
                Require(draw.RemainingNumbers > 0, "Completed draws cannot survive a yield.");
                Require(state.Seats.Players.Contains(draw.Recipient) && state.Player(draw.Recipient).IsParticipating,
                    "Draw recipients must belong to the current round.");
                if (index == 0)
                {
                    Require(draw.Kind == DrawKind.NormalDraw && draw.ParentId is null && draw.Recipient == state.TurnOwner,
                        "Root draw must belong to the turn owner.");
                }
                else
                {
                    Require(state.ResolutionStack[index - 1] is EffectContext parent
                        && parent.SourceCard.Kind == CardKind.DrawTwo && draw.Kind == DrawKind.DrawTwo
                        && parent.EffectTarget == draw.Recipient && draw.ParentId == parent.Id,
                        "Forced draw must belong to its selected DRAW 2 effect.");
                }
            }
            else if (context is EffectContext effect)
            {
                Require(index > 0 && state.ResolutionStack[index - 1] is DrawContext parent
                    && effect.ParentId == parent.Id && effect.EffectDrawer == parent.Recipient,
                    "Effect drawer must be the physical draw recipient.");
                Require(effect.DecisionOwner == effect.EffectDrawer, "Effect decision owner must be its drawer.");
                if (index < state.ResolutionStack.Length - 1)
                {
                    Require(effect.SourceCard.Kind == CardKind.DrawTwo && effect.EffectTarget.HasValue
                        && state.Seats.Players.Contains(effect.EffectTarget.Value)
                        && state.Player(effect.EffectTarget.Value).IsParticipating, "Only a selected DRAW 2 can own a child.");
                }
                else
                {
                    Require(effect.EffectTarget is null, "Resolved effect must not survive at stack top.");
                }
            }
            else
            {
                throw new InvalidOperationException("Unknown resolution context.");
            }
        }

        var decision = state.PendingDecision;
        if (decision is not null)
        {
            Require(decision.Id.Value == state.DecisionSequence && state.Seats.Players.Contains(decision.Owner)
                && state.Player(decision.Owner).IsActive, "Decision must be current and owned by an active participant.");
        }

        switch (state.Boundary)
        {
            case BoundaryKind.WaitPlayerAction:
                Require(state.ResolutionStack.IsEmpty && decision is { Kind: DecisionKind.PlayerAction }
                    && state.TurnOwner == decision.Owner && state.CompletedTurnOwner is null && state.Winner is null,
                    "Action yield requires an active turn owner and no contexts.");
                break;
            case BoundaryKind.WaitTarget:
                Require(state.TurnOwner.HasValue && state.Seats.Players.Contains(state.TurnOwner.Value)
                    && state.CompletedTurnOwner is null && state.Winner is null
                    && state.ResolutionStack.LastOrDefault() is EffectContext top
                    && decision is { Kind: DecisionKind.EffectTarget } && decision.Owner == top.DecisionOwner,
                    "Target yield requires the top effect's decision.");
                Require(decision!.AllowedTargets.SequenceEqual(state.Players.Where(player => player.IsActive).Select(player => player.Player)),
                    "Target set must be exactly the active participants in seat order.");
                break;
            case BoundaryKind.SafePostResolution:
                Require(state.ResolutionStack.IsEmpty && decision is null && state.TurnOwner is null
                    && state.CompletedTurnOwner.HasValue && state.Seats.Players.Contains(state.CompletedTurnOwner.Value)
                    && state.Winner is null, "Safe yield must precede turn/round progression.");
                break;
            case BoundaryKind.GameTerminal:
                Require(state.ResolutionStack.IsEmpty && decision is null && state.TurnOwner is null
                    && state.CompletedTurnOwner is null && state.Winner.HasValue
                    && state.Seats.Players.Contains(state.Winner.Value) && state.Players.All(player => !player.IsActive)
                    && state.Players.All(player => player.NumberHistory.IsEmpty)
                    && state.Players.Max(player => player.TotalScore) >= 150, "Terminal state requires a completed qualifying round.");
                var candidates = state.Players.Where(player => player.IsParticipating).ToArray();
                var maximum = state.RoundKind == RoundKind.Normal
                    ? candidates.Max(player => player.TotalScore) : candidates.Max(player => (long)player.TieBreakRoundResult!.Value);
                var leaders = candidates.Where(player => (state.RoundKind == RoundKind.Normal
                    ? player.TotalScore : player.TieBreakRoundResult) == maximum).ToArray();
                Require(leaders.Length == 1 && leaders[0].Player == state.Winner, "Winner must be the unique round-evaluated leader.");
                break;
            default:
                throw new InvalidOperationException("Unknown engine boundary.");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
