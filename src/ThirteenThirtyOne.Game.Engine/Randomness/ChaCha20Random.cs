using System.Buffers.Binary;
using ThirteenThirtyOne.Game.Domain;

namespace ThirteenThirtyOne.Game.Engine.Randomness;

internal sealed class ChaCha20Random
{
    private readonly byte[] seed;
    private ulong position;

    internal ChaCha20Random(RandomState state)
    {
        seed = state.Seed.ToArray();
        position = state.WordPosition;
    }

    internal RandomState Snapshot() => new(seed, position);

    internal uint NextUInt32()
    {
        if (position >= RandomState.WordCapacity)
        {
            throw new InvalidOperationException("ChaCha20 V1 stream exhausted; counter reuse is forbidden.");
        }

        // Regeneration of the current block is pure. No hidden buffer state is needed for recovery.
        var block = ChaCha20Block.Generate(seed, checked((uint)(position / 16)), new byte[12]);
        var result = BinaryPrimitives.ReadUInt32LittleEndian(block.AsSpan((int)(position % 16) * 4, 4));
        position++;
        return result;
    }

    internal uint NextUniformIntExclusive(ulong maxExclusive)
    {
        const ulong range = 1UL << 32;
        if (maxExclusive is < 1 or > range)
        {
            throw new ArgumentOutOfRangeException(nameof(maxExclusive));
        }

        if (maxExclusive == 1)
        {
            return 0;
        }

        var limit = range / maxExclusive * maxExclusive;
        uint value;
        do
        {
            value = NextUInt32();
        }
        while (value >= limit);

        return (uint)(value % maxExclusive);
    }

    internal void Shuffle<T>(IList<T> items)
    {
        for (var index = items.Count - 1; index > 0; index--)
        {
            var other = (int)NextUniformIntExclusive((ulong)index + 1);
            (items[index], items[other]) = (items[other], items[index]);
        }
    }
}
