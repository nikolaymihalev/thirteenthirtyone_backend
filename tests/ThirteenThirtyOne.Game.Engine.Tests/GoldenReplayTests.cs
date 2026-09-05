using System.Security.Cryptography;
using System.Text;
using ThirteenThirtyOne.Game.Domain;
using Xunit;

namespace ThirteenThirtyOne.Game.Engine.Tests;

public sealed class GoldenReplayTests
{
    [Fact]
    public void EffectTurnAndNextRoundHavePinnedCanonicalHashes()
    {
        var fixture = new Scenario().Player(0, 1, [1]).Player(1, 2, [2]);
        fixture.Draw(fixture.Effect(CardKind.PlusFive), fixture.Number(3));
        var states = new List<GameplayState> { fixture.Build() };
        states.Add(Scenario.Act(states[^1], PlayerAction.Draw).State);
        states.Add(Scenario.Target(states[^1], 1).State);
        states.Add(GameEngine.Apply(states[^1], new ContinueAutomaticResolution()).State);
        states.Add(Scenario.Act(states[^1], PlayerAction.Stop).State);
        states.Add(GameEngine.Apply(states[^1], new ContinueAutomaticResolution()).State);
        states.Add(Scenario.Act(states[^1], PlayerAction.Stop).State);
        states.Add(GameEngine.Apply(states[^1], new ContinueAutomaticResolution()).State);
        Assert.Equal(2, states[^1].RoundNumber);
        Assert.Equal(new long[] { 4, 7 }, states[^1].Players.Select(player => player.TotalScore).ToArray());
        // Reviewed V1 trace: initial, DRAW, target B, continue, B STOP, continue, A STOP, next round.
        string[] expected =
        [
            "BE0310DBA332679DEFC0AA91327067FF67B21037ACE42CE3A25128B461BC06CF",
            "C3846718AE8F504B3F0542C2680A4ECA18B28378DB2342CBB844BCE0A38E93ED",
            "EBD64CAE7E7346546D33D4AE616386CFA715AD291B2176B2BCEF09AFFE9656E9",
            "34BE8F1C17E5E3CD822E42BD02282A4ABB6ED8BD591B079976503DB2716F2F3A",
            "4A174F7EE2AA1D4D442876690FDD3AEEC59DF2D977E22AD198437356873C13D6",
            "564B0DACB1FFB5B3CF66CCC9DEFE75D614234C9BF503B1A063BDC672A6104BF7",
            "6714BC1A2516B722EBE45669ECF6672071D7D3961E6893CC8C98DD5EA9970AD5",
            "525973999D90FED90C8ABE7C4F9819F05D3FDB7354A8A2DDA67C3E73F417301E",
        ];
        Assert.Equal(expected, states.Select(StateHasher.Compute).ToArray());
    }

    [Fact]
    public void FullSeededGameHasPinnedOutcomeAndTransitionHashSequence()
    {
        var state = GameEngine.CreateGame(new GameId("golden-game"), [new PlayerId("A"), new PlayerId("B"), new PlayerId("C"), new PlayerId("D")],
            new RandomState(new byte[32]), EngineCompatibility.V1).State;
        var hashes = new List<string> { StateHasher.Compute(state) };
        while (state.Boundary != BoundaryKind.GameTerminal && hashes.Count < 10000)
        {
            state = GameEngine.Apply(state, DeterministicReplayTests.NextInput(state)).State;
            hashes.Add(StateHasher.Compute(state));
        }

        Assert.Equal(BoundaryKind.GameTerminal, state.Boundary);
        var traceHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', hashes))));
        Assert.Equal(178, hashes.Count);
        Assert.Equal(8, state.RoundNumber);
        Assert.Equal(new PlayerId("B"), state.Winner);
        Assert.Equal(247UL, state.Random.WordPosition);
        Assert.Equal(new[] { "B", "D", "A", "C" }, state.Seats.Players.Select(player => player.Value).ToArray());
        Assert.Equal(new long[] { 151, 113, 96, 104 }, state.Players.Select(player => player.TotalScore).ToArray());
        Assert.Equal("BDFC13A7FA6190998DFE9DB3AB65E50CA04C584A180D9BE52FC4B4DE9C05F89A", hashes[^1]);
        Assert.Equal("01A25C769538B3F580A5277E9DDC4BE7BE4A7B3DE0CFC8F5510AB5E2ECF6E3FA", traceHash);
    }
}
