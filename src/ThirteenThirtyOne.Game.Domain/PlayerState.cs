using System.Collections.Immutable;

namespace ThirteenThirtyOne.Game.Domain;

public sealed class PlayerState
{
    public PlayerState(PlayerId player, PlayerRoundStatus status, TerminationReason reason,
        IEnumerable<Card> numberHistory, int currentScore, int? roundScore, long totalScore,
        int? tieBreakRoundResult = null)
    {
        ArgumentNullException.ThrowIfNull(numberHistory);
        NumberHistory = numberHistory.ToImmutableArray();
        if (string.IsNullOrWhiteSpace(player.Value) || !Enum.IsDefined(status) || !Enum.IsDefined(reason)
            || currentScore is < 0 or > 42 || totalScore < 0 || NumberHistory.Any(card => !card.IsNumber)
            || NumberHistory.Select(card => card.Id).Distinct().Count() != NumberHistory.Length
            || (tieBreakRoundResult is not null && !IsAward(tieBreakRoundResult.Value)))
        {
            throw new ArgumentException("Invalid player state.");
        }

        var validScore = status switch
        {
            PlayerRoundStatus.Active => currentScore < 31 && roundScore is null && reason == TerminationReason.None,
            PlayerRoundStatus.NotParticipating => currentScore == 0 && roundScore is null && NumberHistory.IsEmpty
                && reason == TerminationReason.None,
            PlayerRoundStatus.Stopped => currentScore < 31 && roundScore == currentScore
                && reason is TerminationReason.PlayerChoice or TerminationReason.Timeout,
            PlayerRoundStatus.ForcedStop => currentScore < 31 && roundScore == currentScore
                && reason is TerminationReason.StopEffect or TerminationReason.NumericalDeckDeadlock,
            PlayerRoundStatus.Perfect31 => currentScore == 31 && roundScore == 50 && reason == TerminationReason.None,
            PlayerRoundStatus.Bust13 => roundScore == 0 && reason == TerminationReason.None,
            PlayerRoundStatus.BustOver31 => currentScore >= 32 && roundScore == 0 && reason == TerminationReason.None,
            PlayerRoundStatus.Zeroed => currentScore == 0 && roundScore == 0 && reason == TerminationReason.None,
            _ => false,
        };
        if (!validScore)
        {
            throw new ArgumentException("Score or termination reason conflicts with the round state.");
        }

        Player = player;
        Status = status;
        Reason = reason;
        CurrentScore = currentScore;
        RoundScore = roundScore;
        TotalScore = totalScore;
        TieBreakRoundResult = tieBreakRoundResult;
    }

    public PlayerId Player { get; }
    public PlayerRoundStatus Status { get; }
    public TerminationReason Reason { get; }
    public ImmutableArray<Card> NumberHistory { get; }
    public int CurrentScore { get; }
    public int? RoundScore { get; }
    public long TotalScore { get; }
    public int? TieBreakRoundResult { get; }
    public bool IsActive => Status == PlayerRoundStatus.Active;
    public bool IsParticipating => Status != PlayerRoundStatus.NotParticipating;

    private static bool IsAward(int score) => score is >= 0 and <= 30 or 50;
}
