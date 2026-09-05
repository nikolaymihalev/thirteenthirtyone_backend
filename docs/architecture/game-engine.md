# Deterministic gameplay engine

`GameEngine.CreateGame(GameId, IEnumerable<PlayerId>, RandomState, EngineCompatibility)`
creates the immutable authoritative snapshot. `GameEngine.Apply(GameplayState, EngineInput)`
returns a snapshot, ordered typed events, typed rejection and SHA-256 validation hash.
Rejected inputs retain the original snapshot and emit no events. Neither API performs I/O.

The caller supplies a 32-byte seed and explicit decision or timer-expiry inputs. Stable
yields are `WaitPlayerAction`, `WaitTarget`, `SafePostResolution` and `GameTerminal`.
Only explicit `ContinueAutomaticResolution` advances from the safe boundary into the
next turn or round. A completed turn owner is retained at that boundary.

Draw contexts count directly received number cards. Effect contexts suspend their parent;
DRAW 2 pushes a child draw context and resolves depth first. Turn owner, effect drawer,
decision owner and effect target are distinct roles. Inactive ancestors cancel on unwind;
already started children finish. Effect cards remain owned by their contexts until resolved.

## Reproducibility contract

Compatibility V1 locks rules 1.1 and engine, RNG, bounded sampling, shuffle and hash versions 1.
ChaCha20 uses the [RFC 8439 block function](https://www.rfc-editor.org/rfc/rfc8439.html),
a zero 96-bit nonce, a 32-bit block counter and little-endian output words. The snapshot
stores the seed and absolute word position; counter exhaustion fails before wraparound.
Bounded sampling rejects words above the largest complete multiple of the bound within
2^32. Bound one consumes no randomness. Fisher–Yates runs from the last index downward.

Creation shuffles seats, independently samples the starter, then shuffles the canonical
deck. Card IDs 0–95 represent numbers 1–12, eight copies each, followed by two Zero,
four PlusFive, four MinusFive, three DrawTwo and three Stop cards. Opening effects are
set aside and reintegrated with one shuffle only when the set-aside zone is nonempty.
Discard refills occur only when a physical draw finds an empty draw pile.

The hash encoding is explicit binary, independent of JSON, culture and runtime object
hash codes: signed 64-bit little-endian numbers, length-prefixed UTF-8 text/collections,
and presence flags for nullable fields. `StateHasher.Encode` defines the field order:
domain tag, game ID, compatibility, seats, round/owner/boundary metadata, sequences,
players, card zones, bottom-to-top contexts, pending decision, seed and word position.
Every authoritative field and physical card identity is included.

`StateValidator` checks canonical 112-card conservation, seat/participant consistency,
context ownership, pending decisions and stable boundary invariants. Tests include RFC
vectors, bounded/shuffle vectors, restored snapshots, golden hashes and complete games.

No persistence, timers, networking, accounts, actor or product match lifecycle belongs
in this engine. The application is responsible for storing snapshots and submitting inputs.
