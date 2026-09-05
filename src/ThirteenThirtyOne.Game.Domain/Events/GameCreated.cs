namespace ThirteenThirtyOne.Game.Domain.Events;

public sealed record GameCreated(GameId Game, EngineCompatibility Compatibility) : GameEvent;
