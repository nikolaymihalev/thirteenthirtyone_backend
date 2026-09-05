using ThirteenThirtyOne.Game.Domain;
using ThirteenThirtyOne.Game.Domain.Events;

namespace ThirteenThirtyOne.Game.Engine;

public static partial class GameEngine
{
    private static void ResolveStack(Transition transition)
    {
        while (transition.Stack.Count > 0)
        {
            switch (transition.Stack[^1])
            {
                case DrawContext draw:
                    if (draw.RemainingNumbers == 0 || !transition.Player(draw.Recipient).IsActive)
                    {
                        transition.Stack.RemoveAt(transition.Stack.Count - 1);
                        transition.Events.Add(new DrawContextCompleted(draw.Id, draw.Recipient, draw.RemainingNumbers));
                        continue;
                    }

                    if (!transition.DrawPile.Any(card => card.IsNumber) && !transition.DiscardPile.Any(card => card.IsNumber))
                    {
                        ResolveDeadlock(transition);
                        break;
                    }

                    var card = DrawPhysicalCard(transition);
                    transition.Events.Add(new CardDrawn(transition.TurnOwner!.Value, draw.Recipient, draw.Id, card.Id, card.Kind));
                    if (card.IsNumber)
                    {
                        ReceiveNumber(transition, draw, card);
                    }
                    else
                    {
                        var effect = new EffectContext(transition.NextContextId(), draw.Id, card, draw.Recipient, draw.Recipient);
                        transition.Stack.Add(effect);
                        transition.Events.Add(new EffectStarted(effect.Id, card.Id, transition.TurnOwner.Value,
                            effect.EffectDrawer, effect.DecisionOwner));
                        transition.RequestDecision(DecisionKind.EffectTarget, effect.DecisionOwner);
                        return;
                    }

                    break;
                case EffectContext effect:
                    if (!effect.EffectTarget.HasValue)
                    {
                        throw new InvalidOperationException("An unselected effect cannot resolve automatically.");
                    }

                    transition.Stack.RemoveAt(transition.Stack.Count - 1);
                    transition.DiscardPile.Add(effect.SourceCard);
                    transition.Events.Add(new EffectResolved(effect.Id, effect.SourceCard.Id, false));
                    break;
                default:
                    throw new InvalidOperationException("Unknown context.");
            }
        }

        transition.CompletedTurnOwner = transition.TurnOwner;
        transition.TurnOwner = null;
        transition.Boundary = BoundaryKind.SafePostResolution;
    }

    private static void ReceiveNumber(Transition transition, DrawContext draw, Card card)
    {
        var player = transition.Player(draw.Recipient);
        var history = player.NumberHistory.Add(card);
        var score = checked(player.CurrentScore + card.Number);
        transition.Stack[^1] = new DrawContext(draw.Id, draw.ParentId, draw.Kind, draw.Recipient, draw.RemainingNumbers - 1);
        transition.Events.Add(new NumberReceived(player.Player, card.Id, draw.Id, draw.RemainingNumbers - 1));
        transition.Events.Add(new ScoreChanged(player.Player, player.CurrentScore, score));

        var rule13 = history.Length >= 2 && history[^2].Number + history[^1].Number == 13;
        transition.Events.Add(new ScoreCheckPerformed(player.Player, ScoreCheck.Rule13, rule13));
        PlayerRoundStatus status;
        if (rule13)
        {
            status = PlayerRoundStatus.Bust13;
        }
        else
        {
            status = CheckScore(transition, player.Player, score);
        }

        var result = new PlayerState(player.Player, status, TerminationReason.None, history, score,
            status == PlayerRoundStatus.Active ? null : status == PlayerRoundStatus.Perfect31 ? 50 : 0,
            player.TotalScore, player.TieBreakRoundResult);
        transition.SetPlayer(result);
        if (!result.IsActive)
        {
            transition.Events.Add(new PlayerRoundStateChanged(result.Player, result.Status, result.Reason, result.RoundScore!.Value));
        }
    }

    private static PlayerRoundStatus CheckScore(Transition transition, PlayerId player, int score)
    {
        transition.Events.Add(new ScoreCheckPerformed(player, ScoreCheck.Over31, score > 31));
        if (score > 31)
        {
            return PlayerRoundStatus.BustOver31;
        }

        transition.Events.Add(new ScoreCheckPerformed(player, ScoreCheck.Perfect31, score == 31));
        return score == 31 ? PlayerRoundStatus.Perfect31 : PlayerRoundStatus.Active;
    }

    private static void TerminatePlayer(Transition transition, PlayerState player,
        PlayerRoundStatus status, int roundScore, TerminationReason reason)
    {
        transition.SetPlayer(new PlayerState(player.Player, status, reason, player.NumberHistory,
            status == PlayerRoundStatus.Zeroed ? 0 : player.CurrentScore, roundScore, player.TotalScore, player.TieBreakRoundResult));
        transition.Events.Add(new PlayerRoundStateChanged(player.Player, status, reason, roundScore));
    }

    private static void ApplySelectedEffect(Transition transition, EffectContext effect)
    {
        var target = transition.Player(effect.EffectTarget!.Value);
        switch (effect.SourceCard.Kind)
        {
            case CardKind.DrawTwo:
                transition.Stack.Add(new DrawContext(transition.NextContextId(), effect.Id,
                    DrawKind.DrawTwo, target.Player, 2));
                break;
            case CardKind.PlusFive:
                var score = checked(target.CurrentScore + 5);
                transition.Events.Add(new ScoreChanged(target.Player, target.CurrentScore, score));
                var status = CheckScore(transition, target.Player, score);
                transition.SetPlayer(new PlayerState(target.Player, status, TerminationReason.None, target.NumberHistory,
                    score, status == PlayerRoundStatus.Active ? null : status == PlayerRoundStatus.Perfect31 ? 50 : 0,
                    target.TotalScore, target.TieBreakRoundResult));
                if (status != PlayerRoundStatus.Active)
                {
                    transition.Events.Add(new PlayerRoundStateChanged(target.Player, status, TerminationReason.None,
                        status == PlayerRoundStatus.Perfect31 ? 50 : 0));
                }

                break;
            case CardKind.MinusFive:
                var reducedScore = Math.Max(0, target.CurrentScore - 5);
                transition.SetPlayer(new PlayerState(target.Player, PlayerRoundStatus.Active, TerminationReason.None,
                    target.NumberHistory, reducedScore, null, target.TotalScore, target.TieBreakRoundResult));
                transition.Events.Add(new ScoreChanged(target.Player, target.CurrentScore, reducedScore));
                break;
            case CardKind.Stop:
                TerminatePlayer(transition, target, PlayerRoundStatus.ForcedStop, target.CurrentScore, TerminationReason.StopEffect);
                break;
            case CardKind.Zero:
                transition.Events.Add(new ScoreChanged(target.Player, target.CurrentScore, 0));
                TerminatePlayer(transition, target, PlayerRoundStatus.Zeroed, 0, TerminationReason.None);
                break;
            default:
                throw new NotSupportedException("Unsupported effect.");
        }
    }

    private static void ResolveDeadlock(Transition transition)
    {
        transition.Events.Add(new NumericalDeckDeadlockOccurred(transition.GameId, transition.RoundNumber,
            transition.DrawPile.Count, transition.DiscardPile.Count, transition.OpeningSetAside.Count,
            transition.Players.Sum(player => player.NumberHistory.Length), transition.Stack.OfType<EffectContext>().Count()));
        foreach (var player in transition.Players.Where(player => player.IsActive).ToArray())
        {
            TerminatePlayer(transition, player, PlayerRoundStatus.ForcedStop, player.CurrentScore, TerminationReason.NumericalDeckDeadlock);
        }

        while (transition.Stack.Count > 0)
        {
            var context = transition.Stack[^1];
            transition.Stack.RemoveAt(transition.Stack.Count - 1);
            if (context is EffectContext effect)
            {
                transition.DiscardPile.Add(effect.SourceCard);
                transition.Events.Add(new EffectResolved(effect.Id, effect.SourceCard.Id, true));
            }
            else if (context is DrawContext draw)
            {
                transition.Events.Add(new DrawContextCompleted(draw.Id, draw.Recipient, draw.RemainingNumbers));
            }
        }
    }

}
