namespace ThirteenThirtyOne.Game.Domain.Events;

public sealed record DrawContextCompleted(ContextId Context, PlayerId Recipient, int UnfulfilledNumbers) : GameEvent;
