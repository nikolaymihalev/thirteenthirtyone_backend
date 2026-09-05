namespace ThirteenThirtyOne.Application.DevelopmentGameplay;

public interface IDevelopmentGameplayService
{
    Task<DevelopmentGameOperationResult> CreateGameAsync(CreateDevelopmentGameCommand command, CancellationToken cancellationToken);
    Task<DevelopmentGameOperationResult> GetGameAsync(string gameId, CancellationToken cancellationToken);
    Task<DevelopmentGameOperationResult> SubmitDecisionAsync(SubmitDevelopmentDecisionCommand command, CancellationToken cancellationToken);
    Task<DevelopmentGameOperationResult> ExpireDecisionAsync(ExpireDevelopmentDecisionCommand command, CancellationToken cancellationToken);
    Task<DevelopmentGameOperationResult> ContinueAsync(string gameId, CancellationToken cancellationToken);
    Task<DevelopmentGameOperationResult> DeleteAsync(string gameId, CancellationToken cancellationToken);
}
