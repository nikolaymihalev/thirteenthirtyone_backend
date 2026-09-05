namespace ThirteenThirtyOne.Game.Domain;

public enum TerminationReason
{
    None = 0,
    PlayerChoice = 1,
    Timeout = 2,
    StopEffect = 3,
    NumericalDeckDeadlock = 4,
}
