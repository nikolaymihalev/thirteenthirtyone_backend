using ThirteenThirtyOne.Game.Domain;
using ThirteenThirtyOne.Game.Domain.Events;

namespace ThirteenThirtyOne.Game.Engine;

public static partial class GameEngine
{
    private static void CompleteRound(Transition transition)
    {
        if (transition.Players.Any(player => player.IsActive) || transition.Stack.Count != 0 || transition.PendingDecision is not null)
        {
            throw new InvalidOperationException("A round cannot complete with active players or unresolved contexts.");
        }

        var participants = transition.Players.Where(player => player.IsParticipating).ToArray();
        foreach (var player in participants)
        {
            transition.DiscardPile.AddRange(player.NumberHistory);
        }

        transition.Events.Add(new RoundEnded(transition.RoundNumber, transition.RoundKind));
        foreach (var player in participants)
        {
            var roundScore = player.RoundScore!.Value;
            var total = transition.RoundKind == RoundKind.Normal ? checked(player.TotalScore + roundScore) : player.TotalScore;
            var tieBreakResult = transition.RoundKind == RoundKind.TieBreak ? roundScore : player.TieBreakRoundResult;
            transition.SetPlayer(new PlayerState(player.Player, player.Status, player.Reason, [], player.CurrentScore,
                roundScore, total, tieBreakResult));
            transition.Events.Add(new RoundScoreRecorded(transition.RoundNumber, player.Player, roundScore, total,
                transition.RoundKind == RoundKind.TieBreak));
        }

        var scored = transition.Players.Where(player => player.IsParticipating).ToArray();
        PlayerId[] nextParticipants;
        if (transition.RoundKind == RoundKind.Normal && scored.Max(player => player.TotalScore) < 150)
        {
            nextParticipants = transition.Seats.Players.ToArray();
        }
        else
        {
            var maximum = transition.RoundKind == RoundKind.Normal
                ? scored.Max(player => player.TotalScore) : scored.Max(player => (long)player.TieBreakRoundResult!.Value);
            var leaders = scored.Where(player => (transition.RoundKind == RoundKind.Normal
                ? player.TotalScore : player.TieBreakRoundResult) == maximum).Select(player => player.Player).ToArray();
            if (leaders.Length == 1)
            {
                transition.Winner = leaders[0];
                transition.CompletedTurnOwner = null;
                transition.TurnOwner = null;
                transition.Boundary = BoundaryKind.GameTerminal;
                transition.Events.Add(new GameEnded(transition.GameId, transition.RoundNumber, leaders[0]));
                return;
            }

            transition.RoundKind = RoundKind.TieBreak;
            nextParticipants = leaders;
        }

        transition.RoundNumber = checked(transition.RoundNumber + 1);
        var candidate = transition.Seats.Next(transition.RoundStarter);
        while (!nextParticipants.Contains(transition.Seats[candidate]))
        {
            candidate = transition.Seats.Next(candidate);
        }

        transition.RoundStarter = candidate;
        foreach (var player in transition.Players.ToArray())
        {
            transition.SetPlayer(new PlayerState(player.Player,
                nextParticipants.Contains(player.Player) ? PlayerRoundStatus.Active : PlayerRoundStatus.NotParticipating,
                TerminationReason.None, [], 0, null, player.TotalScore, player.TieBreakRoundResult));
        }

        BeginRound(transition);
    }
}
