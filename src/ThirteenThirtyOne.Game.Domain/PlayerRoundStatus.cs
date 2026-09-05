namespace ThirteenThirtyOne.Game.Domain;

public enum PlayerRoundStatus
{
    Active = 0,
    Stopped = 1,
    ForcedStop = 2,
    Bust13 = 3,
    BustOver31 = 4,
    Perfect31 = 5,
    Zeroed = 6,
    NotParticipating = 7,
}
