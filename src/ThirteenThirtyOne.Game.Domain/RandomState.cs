using System.Collections.Immutable;

namespace ThirteenThirtyOne.Game.Domain;

// Secret authoritative material: never project this state to a client or ordinary log.
public sealed class RandomState
{
    public const ulong WordCapacity = 1UL << 36;

    public RandomState(IEnumerable<byte> seed, ulong wordPosition = 0)
    {
        ArgumentNullException.ThrowIfNull(seed);
        Seed = seed.ToImmutableArray();
        if (Seed.Length != 32 || wordPosition > WordCapacity)
        {
            throw new ArgumentException("ChaCha20 V1 requires a 32-byte seed and a valid stream position.");
        }

        WordPosition = wordPosition;
    }

    public ImmutableArray<byte> Seed { get; }
    public ulong WordPosition { get; }
    public ulong BlockCounter => WordPosition / 16;
    public int WordInBlock => (int)(WordPosition % 16);
}
