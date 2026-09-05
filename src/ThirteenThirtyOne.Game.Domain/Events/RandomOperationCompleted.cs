namespace ThirteenThirtyOne.Game.Domain.Events;

public sealed record RandomOperationCompleted(RandomOperation Operation, ulong BeforeWord, ulong AfterWord, EngineCompatibility Compatibility) : GameEvent;
