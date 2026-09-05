using System.Collections.Concurrent;
using ThirteenThirtyOne.Application.DevelopmentGameplay;

namespace ThirteenThirtyOne.Infrastructure;

public sealed class InMemoryGameSessionStore : IGameSessionStore
{
    private readonly ConcurrentDictionary<string, StoredGameSession> sessions = new(StringComparer.Ordinal);

    public ValueTask<StoredGameSession?> GetAsync(string gameId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(sessions.GetValueOrDefault(gameId));
    }

    public ValueTask<bool> TryCreateAsync(StoredGameSession session, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(sessions.TryAdd(session.State.GameId.Value, session));
    }

    public ValueTask<bool> TryReplaceAsync(string gameId, string expectedStateHash,
        StoredGameSession replacement, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.Equals(gameId, replacement.State.GameId.Value, StringComparison.Ordinal))
        {
            throw new ArgumentException("Replacement must belong to the same game.", nameof(replacement));
        }

        // Reference identity of the immutable value closes the read/check/update race.
        return ValueTask.FromResult(sessions.TryGetValue(gameId, out var current)
            && string.Equals(current.StateHash, expectedStateHash, StringComparison.Ordinal)
            && sessions.TryUpdate(gameId, replacement, current));
    }

    public ValueTask<bool> DeleteAsync(string gameId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(sessions.TryRemove(gameId, out _));
    }
}
