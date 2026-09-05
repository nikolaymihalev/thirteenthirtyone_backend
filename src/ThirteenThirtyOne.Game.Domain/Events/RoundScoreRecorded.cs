namespace ThirteenThirtyOne.Game.Domain.Events;

public sealed record RoundScoreRecorded(int Round, PlayerId Player, int RoundScore, long TotalScore, bool IsTieBreak) : GameEvent;
