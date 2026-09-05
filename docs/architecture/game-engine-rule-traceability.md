# Game engine rule traceability

Locked sources are the four files in `docs/source-of-truth`, especially
`13-31_Digital_Game_Rules_Specification.md`. Gameplay rules are implemented in Domain and
Engine; multiplayer product and deployment rules remain outside this milestone.

| Rule area | Implementation | Executable coverage |
| --- | --- | --- |
| Canonical deck, physical identity | CardCatalog, StateValidator | CardCatalogTests, StateInvariantTests |
| Seats, independent starter, filtered opening | GameEngine.CreateGame, BeginRound | GameSetupTests |
| Last-two-number Rule 13, over 31, perfect 31 | GameEngine.Resolution | NumberAndTurnTests |
| Zero, +5, -5, Stop; active target including self | ApplySelectedEffect | EffectTests |
| Forced numerical quotas, depth-first DRAW 2, independent roles | DrawContext, EffectContext, ResolveStack | NestedDrawTwoTests |
| Physical exhaustion/refill, numerical deadlock | DrawPhysicalCard, ResolveDeadlock | DeckLifecycleTests |
| Explicit safe boundary and continuation | GameEngine.Inputs | NumberAndTurnTests, DecisionValidationTests |
| Scores, threshold, restricted/repeated tie-break, starter rotation | GameEngine.Rounds | RoundAndTieBreakTests |
| Decision IDs, ownership, timeouts, rejection immutability | CheckInput, Apply | DecisionValidationTests, EffectTests |
| ChaCha20, rejection sampling, Fisher–Yates, cursor restore | Randomness | RandomnessTests |
| Canonical hash and snapshot replay | StateHasher, StateValidator | GoldenReplayTests, DeterministicReplayTests |

Replay tests run 24 complete seeded games across two, three and four seats. Every
transition is replayed from a reconstructed snapshot, comparing hashes, ordered events
and boundaries. The four-seat golden complete-game trace reaches round 8 in 177 inputs,
with winner B and random word position 247. Golden vectors are compatibility contracts.
