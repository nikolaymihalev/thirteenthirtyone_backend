namespace ThirteenThirtyOne.Application.DevelopmentGameplay;

public sealed record CreateDevelopmentGameCommand(string? GameId, IReadOnlyList<string>? PlayerIds, string? SeedHex);
