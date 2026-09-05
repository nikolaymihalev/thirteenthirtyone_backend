# 13/31 — Authoritative Project Documentation

These documents are normative inputs for implementation.

Authority order:

1. `source-of-truth/13-31_Rules.md`
   - Upstream source of truth for original tabletop mechanics.

2. `source-of-truth/13-31_Digital_Game_Rules_Specification.md`
   - APPROVED / LOCKED — Production Gameplay Rules Contract.
   - Authoritative for all digital gameplay behavior.

3. `source-of-truth/13-31_Multiplayer_Product_and_Match_Rules_Specification.md`
   - APPROVED / LOCKED — Multiplayer Product Contract.
   - Authoritative for matchmaking, timers, reconnect, AFK, forfeit, etc.

4. `source-of-truth/13-31_Multiplayer_Architecture.md`
   - APPROVED / LOCKED — Production MVP Architecture Contract.
   - Authoritative for technical implementation constraints.

## Implementation Rule

Do not invent or reinterpret gameplay/product rules.

If implementation reveals a conflict or ambiguity:

- do not silently choose a behavior;
- stop the affected implementation;
- report the exact source section;
- mark it as `DECISION REQUIRED`.

For gameplay implementation, the Digital Game Rules Specification v1.1 is the primary contract.
