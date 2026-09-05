using System.Buffers.Binary;
using System.Numerics;

namespace ThirteenThirtyOne.Game.Engine.Randomness;

// RFC 8439 section 2.3. This is a PRNG building block, not an encryption API.
internal static class ChaCha20Block
{
    internal static byte[] Generate(ReadOnlySpan<byte> key, uint counter, ReadOnlySpan<byte> nonce)
    {
        if (key.Length != 32 || nonce.Length != 12)
        {
            throw new ArgumentException("ChaCha20 requires 32 key bytes and 12 nonce bytes.");
        }

        Span<uint> initial = stackalloc uint[16];
        initial[0] = 0x61707865;
        initial[1] = 0x3320646e;
        initial[2] = 0x79622d32;
        initial[3] = 0x6b206574;
        for (var index = 0; index < 8; index++)
        {
            initial[index + 4] = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(index * 4, 4));
        }

        initial[12] = counter;
        for (var index = 0; index < 3; index++)
        {
            initial[index + 13] = BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(index * 4, 4));
        }

        Span<uint> working = stackalloc uint[16];
        initial.CopyTo(working);
        for (var round = 0; round < 10; round++)
        {
            QuarterRound(working, 0, 4, 8, 12);
            QuarterRound(working, 1, 5, 9, 13);
            QuarterRound(working, 2, 6, 10, 14);
            QuarterRound(working, 3, 7, 11, 15);
            QuarterRound(working, 0, 5, 10, 15);
            QuarterRound(working, 1, 6, 11, 12);
            QuarterRound(working, 2, 7, 8, 13);
            QuarterRound(working, 3, 4, 9, 14);
        }

        var output = new byte[64];
        for (var index = 0; index < 16; index++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(output.AsSpan(index * 4, 4), unchecked(working[index] + initial[index]));
        }

        return output;
    }

    private static void QuarterRound(Span<uint> state, int a, int b, int c, int d)
    {
        state[a] = unchecked(state[a] + state[b]);
        state[d] = BitOperations.RotateLeft(state[d] ^ state[a], 16);
        state[c] = unchecked(state[c] + state[d]);
        state[b] = BitOperations.RotateLeft(state[b] ^ state[c], 12);
        state[a] = unchecked(state[a] + state[b]);
        state[d] = BitOperations.RotateLeft(state[d] ^ state[a], 8);
        state[c] = unchecked(state[c] + state[d]);
        state[b] = BitOperations.RotateLeft(state[b] ^ state[c], 7);
    }
}
