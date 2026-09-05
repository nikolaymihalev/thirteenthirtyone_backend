namespace ThirteenThirtyOne.Application.DevelopmentGameplay;

// Authoritative persistence contract: never used as a transport contract.
public interface IGameSessionStore
{
    ValueTask<StoredGameSession?> GetAsync(string gameId, CancellationToken cancellationToken);
    ValueTask<bool> TryCreateAsync(StoredGameSession session, CancellationToken cancellationToken);
    ValueTask<bool> TryReplaceAsync(string gameId, string expectedStateHash, StoredGameSession replacement, CancellationToken cancellationToken);
    ValueTask<bool> DeleteAsync(string gameId, CancellationToken cancellationToken);
}
