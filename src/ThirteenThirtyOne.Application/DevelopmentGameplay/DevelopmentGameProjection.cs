using System.Collections.Immutable;
using ThirteenThirtyOne.Game.Domain;

namespace ThirteenThirtyOne.Application.DevelopmentGameplay;

internal static class DevelopmentGameProjection
{
    public static DevelopmentGameView Map(StoredGameSession session)
    {
        var state = session.State;
        var decision = state.PendingDecision;
        return new(state.GameId.Value, session.StateHash, state.Compatibility.RulesVersion, state.Compatibility.EngineVersion,
            state.RoundNumber, state.RoundKind.ToString(), state.Seats[state.RoundStarter].Value, state.Boundary.ToString(),
            state.IsSafeGameplayBoundary, state.TurnOwner?.Value, state.CompletedTurnOwner?.Value, state.Winner?.Value,
            state.Players.Select((player, seat) => new DevelopmentPlayerView(player.Player.Value, seat,
                player.Status.ToString(), player.Reason.ToString(), player.CurrentScore, player.RoundScore,
                player.TotalScore, player.TieBreakRoundResult, player.NumberHistory.Select(card => card.Number).ToImmutableArray())).ToImmutableArray(),
            decision is null ? null : new(decision.Id.Value, decision.Kind.ToString(), decision.Owner.Value,
                decision.AllowedActions.Select(action => action.ToString()).ToImmutableArray(),
                decision.AllowedTargets.Select(player => player.Value).ToImmutableArray()),
            state.DrawPile.Length, state.DiscardPile.Length, state.OpeningSetAside.Length,
            state.ResolutionStack.Select(MapContext).ToImmutableArray(), state.Random.WordPosition);
    }

    private static DevelopmentContextView MapContext(ResolutionContext context) => context switch
    {
        DrawContext draw => new(draw.Id.Value, draw.ParentId?.Value, "Draw", draw.Kind.ToString(), draw.Recipient.Value,
            draw.RemainingNumbers, null, null, null, null),
        EffectContext effect => new(effect.Id.Value, effect.ParentId?.Value, "Effect", null, null, null,
            effect.SourceCard.Kind.ToString(), effect.EffectDrawer.Value, effect.DecisionOwner.Value, effect.EffectTarget?.Value),
        _ => throw new InvalidOperationException("Unsupported resolution context."),
    };
}

