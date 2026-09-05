namespace ThirteenThirtyOne.Game.Domain.Events;

public sealed record SeatAssigned(PlayerId Player, SeatIndex Seat) : GameEvent;
