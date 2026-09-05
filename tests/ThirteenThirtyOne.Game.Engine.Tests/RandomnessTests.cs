using ThirteenThirtyOne.Game.Domain;
using ThirteenThirtyOne.Game.Engine.Randomness;
using Xunit;

namespace ThirteenThirtyOne.Game.Engine.Tests;

public sealed class RandomnessTests
{
    [Fact]
    public void BlockMatchesRfc8439Section232()
    {
        var key = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();
        var nonce = Convert.FromHexString("000000090000004a00000000");
        var expected = Convert.FromHexString(
            "10f1e7e4d13b5915500fdd1fa32071c4c7d1f4c733c068030422aa9ac3d46c4e" +
            "d2826446079faa0914c2d705d98b02a2b5129cd1de164eb9cbd083e8a2503c4e");

        Assert.Equal(expected, ChaCha20Block.Generate(key, 1, nonce));
    }

    [Fact]
    public void ZeroSeedMatchesRfc8439AppendixA()
    {
        var expected = Convert.FromHexString(
            "76b8e0ada0f13d90405d6ae55386bd28bdd219b8a08ded1aa836efcc8b770dc7" +
            "da41597c5157488d7724e03fb8d84a376a43b8f41518a11cc387b669b2ee6586");

        Assert.Equal(expected, ChaCha20Block.Generate(new byte[32], 0, new byte[12]));
    }

    public static TheoryData<ulong, uint[], ulong> BoundedGoldenVectors => new()
    {
        { 2UL, [0U, 0U, 0U, 1U, 1U, 0U, 0U, 1U, 0U, 1U, 1U, 0U], 12UL },
        { 3UL, [0U, 0U, 0U, 2U, 2U, 0U, 2U, 2U, 1U, 0U, 1U, 1U], 12UL },
        { 4UL, [2U, 0U, 0U, 3U, 1U, 0U, 0U, 3U, 2U, 1U, 3U, 0U], 12UL },
        { 7UL, [5U, 4U, 2U, 1U, 3U, 6U, 2U, 0U, 3U, 0U, 4U, 5U], 12UL },
        { 112UL, [54U, 32U, 16U, 99U, 45U, 48U, 72U, 91U, 10U, 49U, 39U, 40U], 12UL },
        { 2147483649UL, [683509331U, 451775904U, 2086224346U, 1071654007U, 927652024U, 480319509U, 1773569987U, 2050511189U, 2090318488U, 218639731U, 1768285000U, 1045677586U], 23UL },
        { 4294967296UL, [2917185654U, 2419978656U, 3848953152U, 683509331U, 3088700093U, 451775904U, 3438229160U, 3339548555U, 2086224346U, 2370328401U, 1071654007U, 927652024U], 12UL },
    };

    [Theory]
    [MemberData(nameof(BoundedGoldenVectors))]
    public void BoundedSamplerMatchesExactGoldenSequenceAndConsumption(ulong bound, uint[] expected, ulong words)
    {
        var random = new ChaCha20Random(new RandomState(new byte[32]));
        var actual = expected.Select(_ => random.NextUniformIntExclusive(bound)).ToArray();

        Assert.Equal(expected, actual);
        Assert.Equal(words, random.Snapshot().WordPosition);
    }

    [Fact]
    public void BoundOneConsumesNothingAndInvalidBoundsReject()
    {
        var random = new ChaCha20Random(new RandomState(new byte[32]));
        Assert.Equal(0U, random.NextUniformIntExclusive(1));
        Assert.Equal(0UL, random.Snapshot().WordPosition);
        Assert.Throws<ArgumentOutOfRangeException>(() => random.NextUniformIntExclusive(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => random.NextUniformIntExclusive((1UL << 32) + 1));
        Assert.Equal(0UL, random.Snapshot().WordPosition);
    }

    [Fact]
    public void FisherYatesPinsTheEntire112CardPermutation()
    {
        int[] expected =
        [
            36, 93, 62, 44, 32, 24, 97, 28, 40, 41, 13, 66, 39, 77, 71, 42,
            4, 99, 43, 31, 69, 107, 45, 82, 17, 78, 55, 109, 6, 67, 70, 0,
            100, 47, 53, 102, 111, 56, 33, 74, 18, 75, 2, 48, 84, 61, 65, 72,
            1, 26, 105, 104, 89, 23, 34, 37, 103, 49, 80, 5, 81, 92, 20, 19,
            30, 83, 96, 11, 90, 46, 8, 87, 108, 38, 60, 14, 52, 58, 3, 94,
            110, 64, 12, 98, 7, 73, 63, 25, 59, 106, 27, 15, 79, 68, 29, 95,
            9, 91, 21, 86, 51, 85, 10, 50, 35, 16, 76, 101, 88, 22, 57, 54,
        ];
        var cards = CardCatalog.Cards.ToList();
        var random = new ChaCha20Random(new RandomState(new byte[32]));

        random.Shuffle(cards);

        Assert.Equal(expected, cards.Select(card => card.Id.Value).ToArray());
        Assert.Equal(111UL, random.Snapshot().WordPosition);
        Assert.Equal(112, cards.Select(card => card.Id).Distinct().Count());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(111)]
    public void SnapshotContinuesExactlyAcrossBlockBoundaries(int consumed)
    {
        var original = new ChaCha20Random(new RandomState(new byte[32]));
        for (var index = 0; index < consumed; index++)
        {
            original.NextUInt32();
        }

        var snapshot = original.Snapshot();
        var recovered = new ChaCha20Random(new RandomState(snapshot.Seed.ToArray(), snapshot.WordPosition));
        for (var index = 0; index < 100; index++)
        {
            Assert.Equal(original.NextUInt32(), recovered.NextUInt32());
        }

        Assert.Equal(original.Snapshot().WordPosition, recovered.Snapshot().WordPosition);
        Assert.Equal((ulong)consumed, snapshot.WordPosition);
    }

    [Fact]
    public void DifferentSeedsProduceDifferentStreamsAndCounterCannotWrap()
    {
        var zero = new ChaCha20Random(new RandomState(new byte[32]));
        var changed = new byte[32];
        changed[0] = 1;
        var other = new ChaCha20Random(new RandomState(changed));
        Assert.NotEqual(zero.NextUInt32(), other.NextUInt32());

        var last = new ChaCha20Random(new RandomState(new byte[32], RandomState.WordCapacity - 1));
        last.NextUInt32();
        Assert.Equal(RandomState.WordCapacity, last.Snapshot().WordPosition);
        Assert.Throws<InvalidOperationException>(() => last.NextUInt32());
        Assert.Equal(RandomState.WordCapacity, last.Snapshot().WordPosition);
    }
}
