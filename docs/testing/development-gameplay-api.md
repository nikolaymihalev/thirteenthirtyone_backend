# Development gameplay API

This is an in-memory integration harness for the deterministic engine. Routes, store
registration, Swagger UI and OpenAPI are registered only in Development. Production,
Staging and custom environments return normal 404 responses. All games disappear when
the backend process stops. There is no authentication, durable storage or real timer.

## Start and inspect

From the repository root in PowerShell:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project src/ThirteenThirtyOne.GameBackend --no-launch-profile -- --urls http://localhost:5080
```

Open [Swagger UI](http://localhost:5080/swagger/index.html). The document is at
[OpenAPI JSON](http://localhost:5080/swagger/v1/swagger.json). Expand an operation and
select **Try it out**, edit its JSON and select **Execute**. Actions and decision kinds
are strings. IDs must be supplied explicitly; no seed is generated behind the scenes.

The host uses centrally pinned `Swashbuckle.AspNetCore` 10.2.3 for the generator and
embedded UI. See its [official documentation](https://github.com/domaindrivendev/Swashbuckle.AspNetCore).

## Create

`POST /dev/games` returns 201 and a Location header:

```json
{
  "gameId": "manual-test-001",
  "players": ["player-a", "player-b"],
  "seedHex": "0000000000000000000000000000000000000000000000000000000000000000"
}
```

Supply two to four distinct, nonempty player IDs and exactly 64 hex digits. Game IDs
are case-sensitive. Use simple URL-friendly IDs for convenient manual testing.
Creating an existing ID returns 409. `GET /dev/games/manual-test-001` retrieves it.

Responses contain `accepted`, `rejection`, ordered `eventTypes` for this operation and
`game`. GET emits no transition events. The game contains the validation hash, versions,
seats, starter, round, boundary, scores, statuses, numerical histories, pending decision,
card-zone counts, resolution stack diagnostics and consumed RNG word position. It never
contains the seed, raw RNG words, draw order or serialized authoritative state.

## Follow the current decision

Read `game.pendingDecision` after every operation. Copy its `decisionId` and
`ownerPlayerId`; inspect `allowedActions` and `allowedTargets`. Never guess the next ID.

Submit `POST /dev/games/manual-test-001/decisions`:

```json
{ "decisionId": 1, "playerId": "COPY_CURRENT_OWNER", "action": "Draw" }
```

Replace the example ID and owner with the actual current values. Use `"action":"Stop"`
to stop. Draw and Stop must omit `targetPlayerId`. Draw can return `WaitTarget` or
`SafePostResolution`. At `WaitTarget`, submit the current target decision:

```json
{
  "decisionId": 2,
  "playerId": "COPY_CURRENT_DECISION_OWNER",
  "action": "SelectTarget",
  "targetPlayerId": "COPY_AN_ALLOWED_TARGET"
}
```

Again, 2 is illustrative, not a promised sequence. Effects can generate further target
decisions, including nested DRAW 2. Repeat using each new pending decision. The stack
shows draw kind, recipient and remaining numerical quota, or effect kind, drawer,
decision owner and selected target. Parent IDs identify nesting. Turn owner and effect
decision owner can differ.

## Simulate timeouts

`POST /dev/games/manual-test-001/timeouts` accepts the current ID and kind:

```json
{ "decisionId": 3, "decisionKind": "PlayerAction" }
```

PlayerAction expiry stops the player with reason `Timeout`. For a target decision use
`"decisionKind":"EffectTarget"`; the engine selects the effect owner as target. These
are explicit test inputs, not scheduled timers or durable deadlines.

## Continue and finish

At `SafePostResolution`, pending decision is null and `isSafeGameplayBoundary` is true.
Call `POST /dev/games/manual-test-001/continue` with no body. This advances into the next
turn, next round, tie-break or terminal state. No endpoint automatically continues.

Repeat decisions and explicit continuation until `boundary` is `GameTerminal`.
`winnerPlayerId` then identifies the winner. Further gameplay input returns 409.

`DELETE /dev/games/manual-test-001` returns 204, or 404 when absent. Recreate with the
same game ID, roster, seed and ordered inputs to reproduce the same hashes and events.
The hash includes game ID, so use the same ID when comparing traces across isolated stores.

## Errors and concurrency

| Status | Rejection |
| --- | --- |
| 400 | InvalidRequest; malformed JSON, enum syntax or required body |
| 404 | GameNotFound |
| 409 | DuplicateGameId, ConcurrencyConflict, DecisionMismatch, GameAlreadyTerminal |
| 422 | WrongDecisionOwner, IllegalAction, IllegalTarget, NoDecisionPending, ContinuationNotAllowed |

Application/engine errors use the normal response envelope. Framework JSON-binding
failures return 400 before the application runs and may use the framework error body.
Unexpected invariant exceptions surface as server defects; they are not gameplay rejections.

Rejected engine inputs do not write to the store and return the loaded state and unchanged
hash. Accepted transitions compare-and-replace using the loaded hash. Concurrent updates
cannot blindly overwrite each other. A loser receives 409 without replaying its input
against newer state; the response includes the latest state when still available. If the
game was concurrently deleted, `game` is null. Fetch again before choosing another action.

This optimistic mechanism is a development adapter, not an authoritative Match Actor.
Cancellation tokens propagate to store operations. No seed, deck or full snapshot is logged.

## Verification

`DevelopmentGameplayTests` cover application orchestration and compare its results with
the engine. `SessionStoreTests` cover atomic create/update and cancellation.
`DevelopmentApiTests` cover HTTP syntax, errors, concurrent requests, environment gating,
Swagger string schemas and a complete game repeated in two isolated hosts. Every replay
response is compared, including hash, boundary, decisions, scores and ordered event names;
all responses are checked for secret properties.

For Production verification, stop the process and restart with
`$env:ASPNETCORE_ENVIRONMENT = 'Production'` and the same command. Gameplay and Swagger
paths must return 404; `/health/live` remains 200.

Convenient editable requests are in `http/development-gameplay.http`.
