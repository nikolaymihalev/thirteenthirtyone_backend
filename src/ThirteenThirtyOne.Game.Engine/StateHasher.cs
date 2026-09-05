using System.Buffers;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using ThirteenThirtyOne.Game.Domain;

namespace ThirteenThirtyOne.Game.Engine;

public static class StateHasher
{
    public static string Compute(GameplayState state) => Convert.ToHexString(SHA256.HashData(Encode(state)));

    internal static byte[] Encode(GameplayState state)
    {
        var bytes = new ArrayBufferWriter<byte>();
        void Number(long value)
        {
            BinaryPrimitives.WriteInt64LittleEndian(bytes.GetSpan(8), value);
            bytes.Advance(8);
        }

        void Text(string value)
        {
            var encoded = new UTF8Encoding(false, true).GetBytes(value);
            Number(encoded.Length);
            bytes.Write(encoded);
        }

        void Player(PlayerId? player)
        {
            Number(player.HasValue ? 1 : 0);
            if (player.HasValue)
            {
                Text(player.Value.Value);
            }
        }

        void Optional(long? value)
        {
            Number(value.HasValue ? 1 : 0);
            if (value.HasValue)
            {
                Number(value.Value);
            }
        }

        void Cards(IEnumerable<Card> cards)
        {
            var array = cards.ToArray();
            Number(array.Length);
            foreach (var card in array)
            {
                Number(card.Id.Value);
                Number((int)card.Kind);
                Number(card.Number);
            }
        }

        Text("13/31:gameplay-state");
        Text(state.GameId.Value);
        Text(state.Compatibility.RulesVersion);
        Number(state.Compatibility.EngineVersion);
        Number(state.Compatibility.RngAlgorithmVersion);
        Number(state.Compatibility.BoundedSamplingAlgorithmVersion);
        Number(state.Compatibility.ShuffleAlgorithmVersion);
        Number(state.Compatibility.StateHashVersion);
        Number(state.Seats.Count);
        foreach (var player in state.Seats.Players)
        {
            Text(player.Value);
        }

        Number(state.RoundNumber);
        Number((int)state.RoundKind);
        Number(state.RoundStarter.Value);
        Player(state.TurnOwner);
        Player(state.CompletedTurnOwner);
        Number((int)state.Boundary);
        Player(state.Winner);
        Number(state.DecisionSequence);
        Number(state.ContextSequence);
        Number(state.Players.Length);
        foreach (var player in state.Players)
        {
            Text(player.Player.Value);
            Number((int)player.Status);
            Number((int)player.Reason);
            Number(player.CurrentScore);
            Optional(player.RoundScore);
            Number(player.TotalScore);
            Optional(player.TieBreakRoundResult);
            Cards(player.NumberHistory);
        }

        Cards(state.DrawPile);
        Cards(state.DiscardPile);
        Cards(state.OpeningSetAside);
        Number(state.ResolutionStack.Length);
        foreach (var context in state.ResolutionStack)
        {
            Number(context.Id.Value);
            Optional(context.ParentId?.Value);
            switch (context)
            {
                case DrawContext draw:
                    Number(0);
                    Number((int)draw.Kind);
                    Text(draw.Recipient.Value);
                    Number(draw.RemainingNumbers);
                    break;
                case EffectContext effect:
                    Number(1);
                    Cards([effect.SourceCard]);
                    Text(effect.EffectDrawer.Value);
                    Text(effect.DecisionOwner.Value);
                    Player(effect.EffectTarget);
                    break;
                default:
                    throw new InvalidOperationException("Unknown context cannot be hashed.");
            }
        }

        Number(state.PendingDecision is null ? 0 : 1);
        if (state.PendingDecision is { } decision)
        {
            Number(decision.Id.Value);
            Number((int)decision.Kind);
            Text(decision.Owner.Value);
            Number(decision.AllowedTargets.Length);
            foreach (var player in decision.AllowedTargets)
            {
                Text(player.Value);
            }
        }

        Number(state.Random.Seed.Length);
        bytes.Write(state.Random.Seed.AsSpan());
        Number(checked((long)state.Random.WordPosition));
        return bytes.WrittenSpan.ToArray();
    }
}
