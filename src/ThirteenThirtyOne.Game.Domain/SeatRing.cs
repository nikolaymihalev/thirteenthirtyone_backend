using System.Collections.Immutable;

namespace ThirteenThirtyOne.Game.Domain;

public sealed class SeatRing
{
    public SeatRing(IEnumerable<PlayerId> clockwisePlayers)
    {
        ArgumentNullException.ThrowIfNull(clockwisePlayers);
        Players = clockwisePlayers.ToImmutableArray();
        if (Players.Length is < 2 or > 4 || Players.Distinct().Count() != Players.Length
            || Players.Any(player => string.IsNullOrWhiteSpace(player.Value)))
        {
            throw new ArgumentException("A seat ring requires 2–4 distinct valid players.", nameof(clockwisePlayers));
        }
    }

    public ImmutableArray<PlayerId> Players { get; }
    public int Count => Players.Length;

    public PlayerId this[SeatIndex seat] => Players[seat.Value];

    public SeatIndex SeatOf(PlayerId player)
    {
        var index = Players.IndexOf(player);
        return index >= 0 ? new SeatIndex(index) : throw new ArgumentException("Player is not seated.", nameof(player));
    }

    public SeatIndex Next(SeatIndex seat)
    {
        if (seat.Value >= Count)
        {
            throw new ArgumentOutOfRangeException(nameof(seat));
        }

        return new SeatIndex((seat.Value + 1) % Count);
    }
}
