using ThirteenThirtyOne.Game.Domain;

namespace ThirteenThirtyOne.Application.DevelopmentGameplay;

// Authoritative persistence contract: never used as a transport contract.
public sealed class StoredGameSession(GameplayState state, string stateHash)
{
    public GameplayState State { get; } = state;
    public string StateHash { get; } = stateHash;
}
