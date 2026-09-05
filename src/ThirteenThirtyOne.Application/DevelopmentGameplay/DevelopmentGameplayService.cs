using System.Collections.Immutable;
using ThirteenThirtyOne.Game.Domain;
using ThirteenThirtyOne.Game.Engine;

namespace ThirteenThirtyOne.Application.DevelopmentGameplay;

public sealed class DevelopmentGameplayService(IGameSessionStore store) : IDevelopmentGameplayService
{
    public async Task<DevelopmentGameOperationResult> CreateGameAsync(CreateDevelopmentGameCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(command.GameId) || command.PlayerIds is not { Count: >= 2 and <= 4 }
            || command.PlayerIds.Any(string.IsNullOrWhiteSpace)
            || command.PlayerIds.Distinct(StringComparer.Ordinal).Count() != command.PlayerIds.Count
            || command.SeedHex is not { Length: 64 } || command.SeedHex.Any(character => !Uri.IsHexDigit(character)))
        {
            return Error(DevelopmentResultKind.InvalidRequest, "InvalidRequest");
        }

        var transition = GameEngine.CreateGame(new GameId(command.GameId), command.PlayerIds.Select(id => new PlayerId(id)),
            new RandomState(Convert.FromHexString(command.SeedHex)), EngineCompatibility.V1);
        var session = new StoredGameSession(transition.State, transition.StateValidationHash);
        if (!await store.TryCreateAsync(session, cancellationToken))
        {
            return Error(DevelopmentResultKind.Conflict, "DuplicateGameId",
                await store.GetAsync(command.GameId, cancellationToken));
        }

        return Success(session, transition.Events.Select(item => item.GetType().Name).ToImmutableArray());
    }

    public async Task<DevelopmentGameOperationResult> GetGameAsync(string gameId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(gameId))
        {
            return Error(DevelopmentResultKind.InvalidRequest, "InvalidRequest");
        }
        var session = await store.GetAsync(gameId, cancellationToken);
        return session is null ? Error(DevelopmentResultKind.NotFound, "GameNotFound") : Success(session, []);
    }

    public Task<DevelopmentGameOperationResult> SubmitDecisionAsync(SubmitDevelopmentDecisionCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(command.GameId) || string.IsNullOrWhiteSpace(command.PlayerId)
            || command.DecisionId <= 0 || !Enum.IsDefined(command.Action)
            || (command.Action == DevelopmentPlayerAction.SelectTarget
                ? string.IsNullOrWhiteSpace(command.TargetPlayerId) : command.TargetPlayerId is not null))
        {
            return Task.FromResult(Error(DevelopmentResultKind.InvalidRequest, "InvalidRequest"));
        }

        var action = command.Action switch
        {
            DevelopmentPlayerAction.Draw => PlayerAction.Draw,
            DevelopmentPlayerAction.Stop => PlayerAction.Stop,
            DevelopmentPlayerAction.SelectTarget => PlayerAction.SelectTarget,
            _ => throw new InvalidOperationException(),
        };
        return ApplyAsync(command.GameId, new PlayerDecision(new DecisionId(command.DecisionId), new PlayerId(command.PlayerId),
            action, command.TargetPlayerId is null ? null : new PlayerId(command.TargetPlayerId)), cancellationToken);
    }

    public Task<DevelopmentGameOperationResult> ExpireDecisionAsync(ExpireDevelopmentDecisionCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(command.GameId) || command.DecisionId <= 0 || !Enum.IsDefined(command.DecisionKind))
        {
            return Task.FromResult(Error(DevelopmentResultKind.InvalidRequest, "InvalidRequest"));
        }
        return ApplyAsync(command.GameId, new GameplayTimerExpired(new DecisionId(command.DecisionId),
            command.DecisionKind == DevelopmentDecisionKind.PlayerAction ? DecisionKind.PlayerAction : DecisionKind.EffectTarget), cancellationToken);
    }

    public Task<DevelopmentGameOperationResult> ContinueAsync(string gameId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return string.IsNullOrWhiteSpace(gameId)
            ? Task.FromResult(Error(DevelopmentResultKind.InvalidRequest, "InvalidRequest"))
            : ApplyAsync(gameId, new ContinueAutomaticResolution(), cancellationToken);
    }

    public async Task<DevelopmentGameOperationResult> DeleteAsync(string gameId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(gameId))
        {
            return Error(DevelopmentResultKind.InvalidRequest, "InvalidRequest");
        }
        return await store.DeleteAsync(gameId, cancellationToken)
            ? new(DevelopmentResultKind.Success, true, null, [], null)
            : Error(DevelopmentResultKind.NotFound, "GameNotFound");
    }

    private async Task<DevelopmentGameOperationResult> ApplyAsync(string gameId, EngineInput input, CancellationToken cancellationToken)
    {
        var prior = await store.GetAsync(gameId, cancellationToken);
        if (prior is null)
        {
            return Error(DevelopmentResultKind.NotFound, "GameNotFound");
        }
        cancellationToken.ThrowIfCancellationRequested();
        var transition = GameEngine.Apply(prior.State, input);
        if (!transition.IsAccepted)
        {
            var kind = transition.Rejection is EngineRejection.DecisionMismatch or EngineRejection.GameAlreadyTerminal
                ? DevelopmentResultKind.Conflict : DevelopmentResultKind.Rejected;
            return Error(kind, transition.Rejection.ToString(), prior);
        }

        var replacement = new StoredGameSession(transition.State, transition.StateValidationHash);
        if (!await store.TryReplaceAsync(gameId, prior.StateHash, replacement, cancellationToken))
        {
            return Error(DevelopmentResultKind.Conflict, "ConcurrencyConflict", await store.GetAsync(gameId, cancellationToken));
        }

        return Success(replacement, transition.Events.Select(item => item.GetType().Name).ToImmutableArray());
    }

    private static DevelopmentGameOperationResult Success(StoredGameSession session, ImmutableArray<string> events) =>
        new(DevelopmentResultKind.Success, true, null, events, DevelopmentGameProjection.Map(session));

    private static DevelopmentGameOperationResult Error(DevelopmentResultKind kind, string code, StoredGameSession? session = null) =>
        new(kind, false, code, [], session is null ? null : DevelopmentGameProjection.Map(session));
}
