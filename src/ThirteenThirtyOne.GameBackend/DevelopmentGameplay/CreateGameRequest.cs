using ThirteenThirtyOne.Application.DevelopmentGameplay;

namespace ThirteenThirtyOne.GameBackend.DevelopmentGameplay;

public sealed record CreateGameRequest(string? GameId, string[]? Players, string? SeedHex);
