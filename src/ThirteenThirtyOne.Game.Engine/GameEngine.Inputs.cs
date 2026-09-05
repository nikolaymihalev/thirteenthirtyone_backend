using ThirteenThirtyOne.Game.Domain;
using ThirteenThirtyOne.Game.Domain.Events;

namespace ThirteenThirtyOne.Game.Engine;

public static partial class GameEngine
{
    public static EngineTransitionResult Apply(GameplayState state, EngineInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        StateValidator.Validate(state);
        var rejection = CheckInput(state, input);
        if (rejection != EngineRejection.None)
        {
            return new EngineTransitionResult(state, [], rejection, StateHasher.Compute(state));
        }

        var transition = new Transition(state);
        if (input is ContinueAutomaticResolution)
        {
            Progress(transition);
            return transition.Finish();
        }

        var decision = transition.PendingDecision!;
        transition.PendingDecision = null;
        if (decision.Kind == DecisionKind.PlayerAction)
        {
            if (input is PlayerDecision { Action: PlayerAction.Draw })
            {
                transition.Stack.Add(new DrawContext(transition.NextContextId(), null, DrawKind.NormalDraw, decision.Owner, 1));
            }
            else
            {
                var player = transition.Player(decision.Owner);
                TerminatePlayer(transition, player, PlayerRoundStatus.Stopped, player.CurrentScore,
                    input is GameplayTimerExpired ? TerminationReason.Timeout : TerminationReason.PlayerChoice);
            }
        }
        else
        {
            var effect = (EffectContext)transition.Stack[^1];
            var target = input is PlayerDecision playerDecision ? playerDecision.Target!.Value : decision.Owner;
            var selected = new EffectContext(effect.Id, effect.ParentId!.Value, effect.SourceCard,
                effect.EffectDrawer, effect.DecisionOwner, target);
            transition.Stack[^1] = selected;
            transition.Events.Add(new TargetSelected(effect.Id, decision.Owner, target, input is GameplayTimerExpired));
            ApplySelectedEffect(transition, selected);
        }

        ResolveStack(transition);
        return transition.Finish();
    }

    private static EngineRejection CheckInput(GameplayState state, EngineInput input)
    {
        if (state.Boundary == BoundaryKind.GameTerminal)
        {
            return EngineRejection.GameAlreadyTerminal;
        }

        if (input is ContinueAutomaticResolution)
        {
            return state.IsSafeGameplayBoundary ? EngineRejection.None : EngineRejection.ContinuationNotAllowed;
        }

        if (state.PendingDecision is not { } decision)
        {
            return EngineRejection.NoDecisionPending;
        }

        switch (input)
        {
            case PlayerDecision player:
                if (player.DecisionId != decision.Id)
                {
                    return EngineRejection.DecisionMismatch;
                }

                if (player.Player != decision.Owner)
                {
                    return EngineRejection.WrongDecisionOwner;
                }

                if (!decision.AllowedActions.Contains(player.Action)
                    || (decision.Kind == DecisionKind.PlayerAction && player.Target.HasValue))
                {
                    return EngineRejection.IllegalAction;
                }

                if (decision.Kind == DecisionKind.EffectTarget
                    && (!player.Target.HasValue || !decision.AllowedTargets.Contains(player.Target.Value)))
                {
                    return EngineRejection.IllegalTarget;
                }

                return EngineRejection.None;
            case GameplayTimerExpired timer:
                return timer.DecisionId != decision.Id ? EngineRejection.DecisionMismatch
                    : timer.Kind != decision.Kind ? EngineRejection.IllegalAction : EngineRejection.None;
            default:
                return EngineRejection.IllegalAction;
        }
    }

    private static void Progress(Transition transition)
    {
        if (transition.Players.Any(player => player.IsActive))
        {
            var completedSeat = transition.Seats.SeatOf(transition.CompletedTurnOwner!.Value).Value;
            for (var offset = 1; offset <= transition.Seats.Count; offset++)
            {
                var player = transition.Players[(completedSeat + offset) % transition.Seats.Count];
                if (player.IsActive)
                {
                    transition.CompletedTurnOwner = null;
                    transition.TurnOwner = player.Player;
                    transition.RequestDecision(DecisionKind.PlayerAction, player.Player);
                    return;
                }
            }
        }

        CompleteRound(transition);
    }
}
