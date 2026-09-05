using ThirteenThirtyOne.Game.Domain;

namespace ThirteenThirtyOne.Game.Engine.Tests;

internal static class SnapshotCopy
{
    internal static GameplayState Reconstruct(GameplayState source)
    {
        var players = source.Players.Select(player => new PlayerState(new PlayerId(player.Player.Value), player.Status, player.Reason,
            player.NumberHistory.Select(CopyCard), player.CurrentScore, player.RoundScore, player.TotalScore, player.TieBreakRoundResult));
        var stack = source.ResolutionStack.Select<ResolutionContext, ResolutionContext>(context => context switch
        {
            DrawContext draw => new DrawContext(draw.Id, draw.ParentId, draw.Kind, draw.Recipient, draw.RemainingNumbers),
            EffectContext effect => new EffectContext(effect.Id, effect.ParentId!.Value, CopyCard(effect.SourceCard),
                effect.EffectDrawer, effect.DecisionOwner, effect.EffectTarget),
            _ => throw new InvalidOperationException(),
        });
        var pending = source.PendingDecision is { } decision
            ? new PendingDecision(decision.Id, decision.Kind, decision.Owner, decision.AllowedTargets.ToArray()) : null;
        return new GameplayState(new GameId(source.GameId.Value), source.Compatibility with { },
            new SeatRing(source.Seats.Players.ToArray()), source.RoundNumber, source.RoundKind, source.RoundStarter,
            source.TurnOwner, players, source.DrawPile.Select(CopyCard), source.DiscardPile.Select(CopyCard),
            source.OpeningSetAside.Select(CopyCard), stack, pending, source.Boundary, source.DecisionSequence,
            source.ContextSequence, new RandomState(source.Random.Seed.ToArray(), source.Random.WordPosition),
            source.Winner, source.CompletedTurnOwner);
    }

    private static Card CopyCard(Card card) => new(card.Id, card.Kind, card.Number);
}
