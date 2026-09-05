namespace ThirteenThirtyOne.Game.Domain;

public enum EngineRejection
{
    None = 0,
    GameAlreadyTerminal = 1,
    NoDecisionPending = 2,
    DecisionMismatch = 3,
    WrongDecisionOwner = 4,
    IllegalAction = 5,
    IllegalTarget = 6,
    ContinuationNotAllowed = 7,
}
