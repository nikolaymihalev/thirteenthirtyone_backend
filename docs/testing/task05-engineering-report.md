# Task 05 engineering report

## Implementation

Added the Development gameplay harness within the existing Clean Architecture graph.
The stateless scoped `DevelopmentGameplayService` provides Create, Get, SubmitDecision,
ExpireDecision, Continue and Delete through `IDevelopmentGameplayService`. Commands use
primitive IDs and Application-owned enums. Immutable game, player, decision and context
views are mapped explicitly from Domain in Application. HTTP request DTOs map to commands;
the HTTP response envelope intentionally embeds the safe Application view.

`IGameSessionStore` is the narrow persistence port. Its internal-facing
`StoredGameSession` contains an immutable authoritative snapshot and validation hash.
The host never consumes that representation or references Domain/Engine types.

## In-memory adapter

`InMemoryGameSessionStore` is registered as a process-wide singleton only in Development.
An instance-owned ordinal `ConcurrentDictionary` provides atomic creation and deletion.
Replacement checks the expected prior hash and atomically compares the immutable stored
object's reference identity using TryUpdate. No global gameplay lock is used. Losing
writers receive ConcurrencyConflict; the application does not retry gameplay. Rejected
engine inputs perform no replacement. Games are lost on process restart.

## Endpoints

| Method | Path | Purpose | Success |
| --- | --- | --- | --- |
| POST | /dev/games | Explicit seed and roster creation | 201 + Location |
| GET | /dev/games/{gameId} | Inspect current projection | 200 |
| POST | /dev/games/{gameId}/decisions | Draw, Stop, SelectTarget | 200 |
| POST | /dev/games/{gameId}/timeouts | Explicit action/target expiry | 200 |
| POST | /dev/games/{gameId}/continue | Advance from safe boundary | 200 |
| DELETE | /dev/games/{gameId} | Reset/cleanup | 204 |

The entire group is mapped inside `app.Environment.IsDevelopment()`. Swagger and store
registration share that startup boundary. There are no handler-level environment checks.

## HTTP errors

- 400: InvalidRequest, malformed JSON, missing required body, invalid enum syntax.
- 404: GameNotFound.
- 409: DuplicateGameId, ConcurrencyConflict, DecisionMismatch, GameAlreadyTerminal.
- 422: WrongDecisionOwner, IllegalAction, IllegalTarget, NoDecisionPending, ContinuationNotAllowed.
- Unexpected programming/invariant failures surface as server errors without being disguised.

Engine rejections retain the current loaded projection and emit no event names. Concurrency
conflicts return the latest available projection, or null if concurrently deleted.

## Projection and determinism

Exposes game/hash/version, round/starter/boundary/owners/winner, seat-ordered players,
scores/status/reason/numerical history, current decision/actions/targets, card-zone counts,
safe resolution context metadata and RNG word position. Ordered event type names mirror
the engine result. Seeds, raw RNG words, draw order, cards and authoritative snapshots
are never serialized. The safe post-resolution boundary is returned unchanged; continuation
and target selection are always explicit caller operations.

## Swagger and packages

Development UI: `http://localhost:5080/swagger/index.html`.
OpenAPI: `http://localhost:5080/swagger/v1/swagger.json`.

Added direct package: **Swashbuckle.AspNetCore 10.2.3**, centrally pinned, for stable
OpenAPI generation and its interactive embedded UI. Its resolved transitive additions:

| Package | Version |
| --- | --- |
| Swashbuckle.AspNetCore.Swagger | 10.2.3 |
| Swashbuckle.AspNetCore.SwaggerGen | 10.2.3 |
| Swashbuckle.AspNetCore.SwaggerUI | 10.2.3 |
| Microsoft.OpenApi | 2.7.5 |
| Microsoft.Extensions.ApiDescription.Server | 10.0.0 |

All six operations have names, descriptions, status metadata and schemas under
Development / Gameplay. Runtime and generator options agree on string enums.

## Tests and validation

209 tests: Domain 16; Engine 131; Application 14; Integration 30 (4 store, 23 gameplay
HTTP and 3 existing host tests); Architecture 18. Application tests cover 2/3/4-player
deterministic creation, syntax, lifecycle, engine equivalence, rejection immutability,
explicit continuation, cancellation and a competing H2 commit. Store tests exercise
concurrent creation/replacement. HTTP tests run a complete game twice in isolated hosts,
comparing every response and checking secrets, and force two competing HTTP decisions
to load the same state before replacement. Exactly one succeeds and the other returns 409.

Validation commands: restore; Release build without restore; tests without build;
format verification without restore. Actual result: restore passed; Release build passed
with zero warnings and zero errors; all 209 tests passed; formatting verification passed.

## Architecture and engine freeze

Existing architecture tests were retained and strengthened with two core purity checks.
GameBackend has no compiled dependency on Domain or Engine. Infrastructure has no Engine
dependency. Engine references only Domain; Protocol remains independent. Evaluated project
graphs remain identical in Debug and Release. No inner-layer packages were introduced.

Task 04 was completed and validated before this integration milestone. Task 05 changes to
Domain/Engine production files and engine tests/golden vectors: **NONE**. SHA-256 comparison
of all 80 frozen core/engine-test files confirms no edits or removals since the Task 04 freeze.

## Actual live verification

Started the Release backend in Development on port 5080. Opened Swagger in the browser
and verified all six rendered operations. HTTP checks: Swagger UI 200, OpenAPI 200,
Create 201 with Location, GET with matching hash, STOP 200 with SafePostResolution and
safe flag true, explicit Continue returning WaitPlayerAction and next decision ID 2,
Delete 204. Started Production on port 5081: GET/POST gameplay and Swagger UI/spec returned
404; health returned 200. Test hosts were stopped after verification.

## Issues and next task

No engine defects or unresolved implementation issues found. In-memory volatility and
framework-owned 400 binding response bodies are documented harness behavior.

Next recommended task: **PostgreSQL Persistence + Authoritative Transaction Primitives**.
No database, actor, production transport, authentication or product lifecycle was added.
