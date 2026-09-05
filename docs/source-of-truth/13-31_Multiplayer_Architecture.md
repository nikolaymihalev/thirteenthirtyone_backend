13/31 — Authoritative Multiplayer Technical Architecture v1.1 

# **13/31** 

## **Authoritative Multiplayer Technical Architecture v1.1** 

Mobile Full Online Multiplayer — iOS / Android 

### **DRAFT FOR FINAL ARCHITECTURE APPROVAL** 

|**Field**|**Value**|
|---|---|
|Primary owner|Senior Software Architect / Multiplayer Backend Architect / Technical<br>Lead|
|Required reviewers|Software / Game Engineer; Systems Game Designer; Product<br>Manager / Multiplayer Product Designer|
|Architecture version|v1.1|
|Date|5 September 2026|
|MVP scope|Public solo online matchmaking;2–4players;iOS / Android|
|Authority posture|Server authoritative;clients are untrusted|



##### **Architecture recommendation** 

Use a small service-oriented backend with two deployable application roles: (1) stateless API/Realtime Gateway and (2) Authoritative Game Backend containing Identity, Matchmaking, Match Runtime, Deadline Processing, Result Finalization and Outbox modules. Use a single-writer match actor per match, PostgreSQL as the only durable source of truth, Redis-compatible cache only for transient routing/pub-sub/rate limiting, WebSocket for realtime, and a deterministic pure C# game engine. Deploy as managed containers on AWS ECS Fargate with RDS PostgreSQL Multi-AZ. 

13/31 | DRAFT FOR FINAL ARCHITECTURE APPROVAL | Page 1 

13/31 — Authoritative Multiplayer Technical Architecture v1.1 

### **Revision Summary v1.0 → v1.1** 

Revision Request 03 is a targeted correctness revision. The approved architecture direction remains unchanged; v1.1 closes implementation-level gaps in matchmaking, AFK qualification durability, stale connection fencing, pre-start auto-requeue persistence, idempotency-key payload binding, deterministic uniform RNG sampling, race/failure coverage and document shipping QA. 

|**Change ID**|**Section**|**Change**|**Reason**|**Status**|
|---|---|---|---|---|
|ARCH-CH-001|17, 36–38, 43–44|Replaced the incomplete<br>matchmaking shortcut with a<br>descending 4→3→2 legal-candidate<br>evaluator using per-entry eligibility<br>and oldest-frst stablepriority.|Required to implement MP-DR-001<br>exactly, including legal smaller-<br>group fallback.|REQUIRED / CLOSED|
|ARCH-CH-002|12, 15–16, 22–23, 25, 36–37|Made connected-AFK qualifcation<br>durable per pending decision and<br>monotonic true→false on any<br>qualifyingunavailability.|Recovery/failover must know<br>whether a timeout counts toward<br>connected-AFK escalation.|REQUIRED / CLOSED|
|ARCH-CH-003|12, 16, 19–20, 22, 27, 36–40|Formalized server-issued<br>connection_epoch fencing; stale<br>transport generations cannot<br>demote newer authoritative<br>presence.|Prevents delayed old-socket<br>disconnect/background events<br>from overriding a newer reconnect.|REQUIRED / CLOSED|
|ARCH-CH-004|17–18, 25, 29, 36, 40, 43–44|Added immutable queue<br>provenance and atomic exactly-<br>once pre-start auto-requeue for<br>unafected players after peer-<br>caused cancellation.|Preserves queue age/priority<br>without resurrecting CONSUMED<br>queue rows.|REQUIRED / CLOSED|
|ARCH-CH-005|8–9, 28–30, 36–37, 40|Bound every ProcessedCommand<br>command_id to a canonical<br>command_fngerprint and reject<br>keyreuse with a diferentpayload.|Distinguishes legitimate retry from<br>idempotency-key misuse.|REQUIRED / CLOSED|
|ARCH-CH-006|7, 11, 31, 36–38, 43–44|Added versioned bias-free<br>NextUniformIntExclusive via<br>rejection sampling and versioned<br>shufle compatibility.|Locked random operations require<br>uniform sampling and replay<br>stability.|REQUIRED / CLOSED|
|ARCH-CH-007|37–40|Extended invariants, ADR wording,<br>risk register and failure-mode/race<br>audit for all v1.1guarantees.|Synchronizes architecture controls<br>with implementation/recovery<br>behavior.|REQUIRED / CLOSED|
|ARCH-CH-008|36, 44|Expanded automated testing and<br>implementation handof with<br>explicit correction work items.|Prevents corrected semantics<br>from remaining implicit<br>engineeringdetails.|REQUIRED / CLOSED|
|ARCH-CH-009|Document-wide, 31, 41|Corrected pagination: non-splitting<br>callout rows, diagram+caption<br>cohesion, repeating table headers<br>and full render QA.|Required shipping correction for<br>orphan/near-empty pages and<br>awkward callout splitting.|REQUIRED / CLOSED|



13/31 | DRAFT FOR FINAL ARCHITECTURE APPROVAL | Page 2 

13/31 — Authoritative Multiplayer Technical Architecture v1.1 

### **Document Map** 

1. Executive Architecture Summary 

2. Sources and Locked Constraints 

3. Goals / Non-Goals 

4. High-Level Architecture 

5. Component Responsibilities 

6. Authoritative Match Execution Model 

7. Game Engine Architecture 

8. Command Model 

9. Idempotency 

10. State Versioning 

11. RNG / Randomness 

12. Match State Model 

13. Persistence 

14. Commit / Atomicity Model 

15. Timer Architecture 

16. Race Ordering 

17. Matchmaking Architecture 

18. Candidate / Admission Architecture 

19. Identity / Session 

20. Reconnect 

21. State Synchronization 

22. Presence 

23. Forfeit / Safe Boundary 

24. Result Finalization 

25. Failure Recovery 26. Match Ownership / Failover 27. Networking 

28. Security / Trust Boundaries 

29. Data Model / Retention 

30. Observability 

31. Replay / Audit 

32. Scaling 

33. Deployment Topology 

34. Technology Recommendations 

35. Managed Platform vs Custom Backend Analysis 

36. Testing Architecture 

37. Architecture Invariants 

38. ADR Register 

39. Risk Register 

40. Failure Mode Register 

41. Sequence Diagrams 

42. State Ownership Matrix 

43. Source-to-Architecture Traceability 

44. Implementation Handoff 

45. Remaining Architecture Decisions Architecture Consistency Audit 

13/31 | DRAFT FOR FINAL ARCHITECTURE APPROVAL | Page 3 

13/31 — Authoritative Multiplayer Technical Architecture v1.1 

### **1. Executive Architecture Summary** 

The MVP architecture is designed around one non-negotiable property: every authoritative mutation has exactly one serialized server-side owner and becomes externally authoritative only after durable commit. Mobile clients submit intents; they never determine deck order, card draws, scores, timers, target legality, match ownership, forfeit state, or results. 

The recommended system is deliberately smaller than a microservice estate. It separates connection-heavy edge work from authoritative game execution, but keeps all game/product business modules in one authoritative backend codebase and database transaction boundary. This preserves development speed while still allowing WebSocket connection scaling and match-runtime scaling to evolve independently. 

|**Concern**|**Recommended mechanism**|
|---|---|
|Architecture style|Small service-oriented architecture: stateless Edge Gateway + modular<br>Authoritative Game Backend;not microservices.|
|Match execution|Single logical match actor / serialized mailboxper match_id.|
|Single-writer safety|Database-backed lease with monotonically increasing fencing epoch;<br>everywrite fenced.|
|Game rules|Pure deterministicgame-engine library, infrastructure-independent.|
|Durability|PostgreSQL transactional snapshot after every accepted authoritative<br>transition, plus append-only transition/audit log and processed-command<br>dedupe.|
|Realtime|Authenticated TLS WebSocket; HTTPS for bootstrap/session APIs.|
|Timers|Persisted absolute deadlines + in-process scheduling + durable due-<br>deadline scanner;expiries enter same actor mailbox as commands.|
|RNG|Server-only cryptographically strong deterministic PRNG stream;<br>seed/algorithm version/RNG state are durable and never sent to clients.|
|Matchmaking|Single logical coordinator per queue partition; Postgres-authoritative<br>queue/candidate reservations.|
|Reconnect|Same PLAYER_ID authenticates, active match is discovered, full projected<br>snapshot is returned, then incremental committed events resume.|
|State sync|Hybrid: full snapshot on join/reconnect/gap; incremental versioned event<br>batches duringhealthyrealtime connection.|
|Infrastructure|AWS ECS Fargate + RDS PostgreSQL Multi-AZ + managed Redis-compatible<br>cache + OpenTelemetry/CloudWatch.|



##### **Core commit rule** 

No authoritative success, card result, timer outcome, forfeit state or terminal result is sent to a client before the database transaction containing the matching state mutation has committed. Broadcast is post-commit and recoverable through snapshot resync. 

### **2. Sources and Locked Constraints** 

For Task 03, the three project sources below are normative. The task instruction explicitly requires the Digital Game Rules Specification and Multiplayer Product & Match Rules Specification to be treated as APPROVED / LOCKED production contracts even if their internal document-control cover still shows a draft-review label. 

|**Priority**|**Source**|**Architecture treatment**|
|---|---|---|
|1|13/31 — Rules<br>|Upstream tabletop mechanical source of truth.<br>Used for original deck/card/scoringsemantics.|
|2|13/31 — Digital Game Rules Specifcation v1.1|APPROVED / LOCKED gameplay contract.<br>Architecture must implement its deterministic<br>state machine without reinterpretation.|
|3|13/31 — Multiplayer Product & Match Rules<br>Specifcation v1.1|APPROVED / LOCKED multiplayer product<br>contract. Architecture must implement<br>matchmaking, timers, presence, reconnect,<br>forfeit,fnalityand MVP scope exactly.|



#### **2.1 Locked architecture inputs** 

|**Area**|**Locked input**|
|---|---|
|Roster|Exactly 2–4 players; immutable MATCH_ROSTER after MATCH_CREATED;<br>no backfll.|
|Seats / turn order|Uniform random seat assignment at MATCH_CREATED; immutable<br>SEAT_RING; independent random Round 1 starter; locked later-round<br>rotation.|
|Decision timers|20s normal action; 10s target; gameplay expiry outcomes remain exactly<br>locked.|
|Acceptance / admission / start|10s explicit ACCEPT/DECLINE; 30s shared admission; no manual READY;<br>3s automatic start countdown.|
|Reconnect|60s continuous unavailability grace; no global pause; gameplay timers<br>continue.|
|Queue liveness|10s foreground disconnect grace; 30s background grace;<br>ownership/deadlineprecedence must not extend an earlier deadline.|



13/31 | DRAFT FOR FINAL ARCHITECTURE APPROVAL | Page 4 

13/31 — Authoritative Multiplayer Technical Architecture v1.1 

|**Area**|**Locked input**|
|---|---|
|AFK|Three consecutive fully-connected decision timeouts =><br>FORFEIT_PENDING; warningat two; manual accepted decision resets.|
|Leave|Confrmed Leave => immediate irreversible FORFEIT_PENDING; no<br>reconnectgrace.|
|Forfeit|Finalize onlyat safegameplayboundary; no reduced-roster continuation.|
|Eligible survivors|ELIGIBLE_SURVIVOR is a liveness gate; zero eligible at boundary =><br>FORFEIT_RESOLUTION_WAIT.|
|Forfeit results|Exactly one non-forfeited survivor => winner; 2+ => no winner; all<br>irreversiblyexpire with zero eligible => ABORTED_NO_CONTEST.|
|Identity|Stable persistent opaque PLAYER_ID; persistent guest identity allowed; one<br>activequeue and one activegameplaymatch maximumperplayer.|
|MVP access|Public solo matchmaking only. Private rooms, parties, friends/invites,<br>spectating, rematch, surrender, Ranked/MMR are not MVP requirements.|



### **3. Goals / Non-Goals** 

#### **3.1 Architecture goals** 

|**Goal**|**Defnition**|
|---|---|
|Correctness|Locked gameplay and product rules are represented as executable<br>invariants and validated transitions, not conventions.|
|Determinism|Identical initial random outcomes, deck/RNG state, accepted intents and<br>authoritative timer expiries produce identical authoritative transition<br>sequences.|
|Server authority|Clients express intent only; server state, timers, RNG and fnality are<br>authoritative.|
|Concurrency safety|All match-scoped mutations share one serialized ordering domain; all<br>queue-partition mutations share one coordinator orderingdomain.|
|Idempotency|A retried logical intent is applied at most once and can return its previously<br>committed outcome.|
|Reconnectability|Same PLAYER_ID can reauthenticate, discover its active match and receive<br>an exact currentprojected snapshot.|
|Recoverability|Process failure loses at most uncommitted work; committed match state,<br>dedupe records, deadlines and RNG state survive.|
|Finality|Terminal results are single-insert immutable records and late inputs cannot<br>change them.|
|Observability|Each authoritative transition is attributable to a command/timer/system<br>event and state version.|
|MVP simplicity|Use two application deployables and managed data services; avoid broker-<br>heavy/event-sourced/microservice complexity unless needed by measured<br>scale.|



#### **3.2 Non-goals** 

- UX/UI layout, animations, art, audio or presentation design. 

- Monetization. 

- Ranked/MMR formulas or skill matchmaking. 

- Private rooms, invite codes, friends, premade parties or party matchmaking. 

- Spectating, rematch or surrender. 

- Mandatory social/registered login, account linking, cross-device or reinstall recovery guarantees. 

- Full anti-cheat platform or cryptographic proof protocol beyond server authority, secure identity and command validation. 

- Multi-region active-active match execution in MVP. 

### **4. High-Level Architecture** 

The application layer is split by operational behavior, not by every business capability. Connection churn and socket fan-out are kept in a stateless edge service; all authoritative product/game mutation logic remains in one modular backend so that transactions and invariants stay local. 

13/31 | DRAFT FOR FINAL ARCHITECTURE APPROVAL | Page 5 



<!-- Start of picture text -->
13/31 MVP — Recommended Authoritative Architecture<br>Unity iOS / Android Client<br>Rendering + input only<br>NO authoritative game truth<br>intents / snapshots / committed events<br>Edge Gateway Service<br>HTTPS/ WSS * Auth « Rate Limits<br>Connection & backpressure management<br>match intents / reconnect queue/candidate intents<br>1 single-writerMatch Runtimeactor/ match Authoritativeidentity:;:Session‘.  Game:  Back 1 fencedMatchmaking owner___|/ queueCoordinatorpartition<br>|yj |<br>Le + ever q deterministic_inp | expiry intents ——terminalinput____| | on hints<br>atomic Pure Deterministic Durable Deadline Result Finalizer / event fan-out optional; derived index<br>TX Scanner / Scheduler [ ae WEREACHONAIOUSOX | ts owner hints<br>a SS —ee<br>identity syouauaicainisters Makctecnepenck Redis-compatible cache — TRANSIENT ONLY Opentelemetry / CloudWatch<br>Dealings * Dedupe + Audit + Result « Ontaes Owner/connection routing * Pub/Sub » Rate limits Logs * Metrics * Traces<br><!-- End of picture text -->

13/31 — Authoritative Multiplayer Technical Architecture v1.1 

|**Component**|**Responsibility**|**Owns authoritative state?**|**Persistence**|**Failure impact**|
|---|---|---|---|---|
|Game Engine|Pure deterministic<br>transformation of gameplay<br>state and inputs; emits<br>yield/boundarymetadata.|No independent persistence<br>owner|State is embedded in<br>MatchSnapshot.|Bug afects correctness;<br>isolated tests/replay detect.|
|Deadline Scanner|Find due durable deadlines and<br>emit internal expiryintents.|No|Deadline rows are authoritative;<br>scanner cursor transient.|Restart only delays delivery; due<br>deadlines remain discoverable.|
|Result Finalizer|Pure validation/construction of<br>locked result object.|No independent mutation<br>owner|Match Runtime commits<br>MatchResult.|Finalization waits/retries; no<br>partial result.|
|Transactional Outbox|Publish already-committed<br>authoritative events.|No|Outbox rows in PostgreSQL.|Delivery can retry; clients can<br>always resync snapshot.|
|Telemetry|Structured<br>logs/metrics/traces/audit<br>export.|No|Managed observability store /<br>optional object storage.|Diagnostics degrade; gameplay<br>must not.|



##### **State mutation rule** 

Business components may compute proposals, but only the current domain owner may commit them. Game Engine and Result Finalizer are <u>pure modules; they never write the database directly.</u> 

### **6. Authoritative Match Execution Model** 

Each active match is represented by one logical Match Actor. The actor is not merely an in-memory object; it is a single-writer authority guarded by a durable ownership lease and fencing epoch. All player commands, presence transitions, timer expiries, Leave events, forfeit triggers, engine continuations and terminal claims for that match enter the same serialized mailbox. 

#### **6.1 Ordering mechanism** 

**1.** Gateway authenticates the request and routes it to the current match owner using a transient owner hint. If the hint is missing/stale, any runtime node may resolve ownership from PostgreSQL and claim only if the lease is eligible. 

**2.** The owner assigns a local monotonically increasing mailbox sequence to accepted ingress. Before assigning a new external item, it inserts any authoritative deadlines already due according to server time, so late-after-deadline input cannot jump ahead of expiry. 

**3.** The actor processes exactly one mailbox item at a time. No engine transition or match-level product mutation overlaps another mutation for the same match. 

**4.** The resulting proposed state is committed in PostgreSQL using both expected match_state_version and current lease_epoch fencing predicates. 

**5.** Only after commit does the actor expose success/events. The next mailbox item then observes the committed version. 

#### **6.2 Authoritative yield points** 

One mailbox input may execute multiple automatic game-engine substates, but it stops and commits at a deterministic yield point: a new external player decision is required; a safe post-resolution boundary is reached; gameplay reaches a terminal state; or product orchestration must evaluate forfeit/finality. This prevents partial card/effect mutations while still allowing recoverable pending decisions. 

|**Yield**|**Examples**|**Durable state atyield**|
|---|---|---|
|WAIT_PLAYER_ACTION|TURN_START → WAITING_FOR_PLAYER_ACTION<br>|TURN_OWNER, decision_id, 20s deadline, full<br>engine state.<br>|
|WAIT_TARGET|Efect drawn and EFFECT_CONTEXT created|Source efect already removed from DRAW_PILE,<br>full context stack,<br>DECISION_OWNER/EFFECT_DRAWER, target<br>decision_id, 10s deadline.|
|SAFE_POST_RESOLUTION|Current decision/started contexts complete<br>before any next turn/round progression|No pending decision; no started context; host<br>may fnalize pending forfeit or allow engine<br>continuation.|
|GAME_TERMINAL|Locked GAME_ENDED reached before any earlier<br>product terminal claim|Frozen gameplay outcome awaiting product<br>result commit.|



### **7. Game Engine Architecture** 

The game engine is a deterministic, infrastructure-independent C# library. It contains the canonical digital gameplay state machine and accepts only explicit deterministic inputs. It does not open sockets, query a database, read wall-clock time, call cloud APIs or create arbitrary randomness. 

#### **7.1 Mandatory gameplay state representation** 

|**Concept**|**Required representation / rule**|
|---|---|
|TURN_OWNER|Independent player reference; never inferred from DRAW_RECIPIENT or<br>EFFECT_DRAWER.<br>|
|DRAW_RECIPIENT|Stored on topDRAW_CONTEXT; maydifer from TURN_OWNER.<br>|
|EFFECT_DRAWER|Stored in EFFECT_CONTEXT asplayer whophysicallydrew source efect.|



13/31 | DRAFT FOR FINAL ARCHITECTURE APPROVAL | Page 7 

13/31 — Authoritative Multiplayer Technical Architecture v1.1 

|**Concept**|**Required representation / rule**|
|---|---|
|DECISION_OWNER|Stored separately; current rules set it to EFFECT_DRAWER.|
|EFFECT_TARGET|Exactlyone selected ACTIVEplayer after target decision resolves.|
|DRAW_CONTEXT|Stack frame with type NORMAL_DRAW or DRAW_2, recipient, source<br>metadata and remaining_numbers.|
|EFFECT_CONTEXT|Stack frame with source card, drawer, decision owner, selected target and<br>child context relation.|
|Resolution stack|Explicit depth-frst/LIFO stack; a numerical card decrements only the direct<br>DRAW_CONTEXT.|
|Round/player felds|round_state, termination_reason, NUMBER_HISTORY, CURRENT_SCORE,<br>ROUND_SCORE,TOTAL_SCORE.|
|Deck zones|Ordered DRAW_PILE, DISCARD_PILE, OPENING_SET_ASIDE and per-player<br>numerical histories usingunique card-instance IDs.|
|Seat/round order|Immutable SEAT_RING, ROUND_STARTER, participation set and current<br>turn selection state.|
|RNG API|Versioned deterministic RNG contract exposes bias-free<br>NextUniformIntExclusive(maxExclusive) and deterministic Fisher–Yates;<br>engine never uses naive modulo-bounded sampling.|



#### **7.2 Engine API shape** 

```
EngineTransitionResult Apply(GameplayState state, EngineInput input)
```

```
Inputs include:
```

```
  PlayerDecision(DRAW | STOP | SELECT_TARGET, decision_id, player_id, payload)
  GameplayTimerExpired(ACTION | TARGET, decision_id)
  ContinueAutomaticResolution()
```

```
Result contains:
  new_gameplay_state
  gameplay_domain_events[]
  pending_decision?
  boundary_kind = WAIT_PLAYER_ACTION | WAIT_TARGET | SAFE_POST_RESOLUTION | GAME_TERMINAL
  is_safe_gameplay_boundary
```

```
  state_validation_hash
```

The Multiplayer Orchestrator owns product-level presence/forfeit behavior and decides whether to call ContinueAutomaticResolution after SAFE_POST_RESOLUTION. This is the handoff that allows product forfeit to stop before a new turn/round progression without modifying the locked game mechanics. 

### **8. Command Model** 

#### **8.1 Common command envelope** 

|**Field**|**Meaning / authority**|
|---|---|
|command_id|Client-generated globally unique logical idempotency key for this<br>authenticated PLAYER_ID. Reused only for retries of the same canonical<br>logical intent.|
|command_type|PLAY, CANCEL_QUEUE, ACCEPT_MATCH, DECLINE_MATCH,<br>JOIN_MATCH, DRAW, STOP, SELECT_TARGET, LEAVE_MATCH,<br>RECONNECT.|
|authenticated_player_id|Derived from server-validated session token. A client-supplied PLAYER_ID<br>is never trusted over this value.|
|scope_id|queue_entry_id,candidate_id or match_id as applicable.|
|expected_state_version|Client-observed authoritative version. Used as a precondition hint;<br>semantic decision IDs remain the strongerguard.|
|decision_id|Required for DRAW/STOP/SELECT_TARGET; identifes the exact pending<br>authoritative decision.|
|payload|Target player, lifecycle intent, etc. Never includes trusted<br>score/card/deck/seat values.|
|client_sent_at<br>|Diagnostic only. Never used to resolve authorityor deadline races.|
|command_fngerprint|Server-computed SHA-256 (or equivalent collision-resistant hash) of<br>canonical normalized command identity: command_type +<br>authenticated_player_id + scope_id + decision_id when applicable +<br>normalizedpayload. Transport-onlytimestamps are excluded.|
|server_ingress_at / ingress_sequence|Assigned server-side and used for operational ordering/audit.|



#### **8.2 Command-specific validation** 

|**Command**|**Authoritative validation**<br>|
|---|---|
|PLAY|Authenticated PLAYER_ID has no active match/candidate/queue confict;<br>duplicate PLAY is no-opby player_activity/uniquequeue invariant.|



13/31 | DRAFT FOR FINAL ARCHITECTURE APPROVAL | Page 8 

13/31 — Authoritative Multiplayer Technical Architecture v1.1 

|**Command**|**Authoritative validation**|
|---|---|
|CANCEL_QUEUE|Queue entry still QUEUED, or candidate commit has not won. If candidate<br>already committed, reinterpret per locked product behavior as DECLINE in<br>Match Found fow.|
|ACCEPT_MATCH / DECLINE_MATCH|Candidate membership, candidate state, 10s deadline, member status,<br>command dedupe.|
|JOIN_MATCH|PLAYER_ID is in immutable MATCH_ROSTER; match pre-start state allows<br>admission/reconnect.|
|DRAW / STOP|PLAYER_ID equals current TURN_OWNER and current decision owner for action<br>decision; decision_id matches; deadline not already committed expired; engine<br>says action legal.|
|SELECT_TARGET|PLAYER_ID equals DECISION_OWNER; decision_id matches; target is ACTIVE<br>under locked engine state.|
|LEAVE_MATCH|Authenticated roster player, match in product state where confrmed Leave<br>applies;once accepted creates irreversiblependingstate.|
|RECONNECT|Same authenticated PLAYER_ID owns roster slot and terminal state has not<br>replaced active participation; returns terminal result only if already<br>terminal/pendingas locked.|



#### **8.3 Command result** 

Accepted commands return command_id, outcome (APPLIED / ALREADY_APPLIED / NO_OP), committed state version and optional snapshot/event hint. Rejections are durable only when needed for dedupe/audit and include a stable code such as UNAUTHORIZED, NOT_MEMBER, STALE_STATE, DECISION_MISMATCH, ILLEGAL_ACTION, ILLEGAL_TARGET, DEADLINE_EXPIRED, ALREADY_TERMINAL, IDEMPOTENCY_KEY_REUSE or RETRYABLE_AUTHORITY_UNAVAILABLE. 

### **9. Idempotency** 

Idempotency is enforced before game-engine execution and is payload-bound. The canonical uniqueness namespace is (authenticated_player_id, command_id), so one client idempotency key cannot be reused for a different scope, decision or command payload by the same player. 

#### **9.1 Canonical command fingerprint** 

```
command_fingerprint = SHA256(CanonicalEncode({
  command_type,
  authenticated_player_id,
  scope_id,
  decision_id?,
  normalized_payload
}))
```

```
Excluded: client_sent_at, transport connection_id, retry count, gateway timestamps.
```

CanonicalEncode has a protocol-versioned deterministic field ordering/normalization. Semantically equivalent retries must produce the same bytes and fingerprint; omitted/default values are normalized consistently. ProcessedCommand stores command_id, command_fingerprint, committed outcome, resulting entity/state version and correlation IDs. 

#### **9.2 Claim / retry algorithm** 

**1.** Look up ProcessedCommand by (authenticated_player_id, command_id) before domain execution. 

**2.** If a row exists and its fingerprint equals the incoming fingerprint, return the stored outcome as ALREADY_APPLIED / original NO_OP without re-running the engine. 

**3.** If a row exists and the fingerprint differs, reject IDEMPOTENCY_KEY_REUSE, emit security/diagnostic telemetry and never return the old result as if it matched the new payload. 

**4.** If no row exists, execute normal authoritative validation and transition. The ProcessedCommand row is inserted in the same PostgreSQL transaction as the mutation. 

**5.** Concurrent requests racing to claim the same key are serialized by the unique constraint. Exactly one fingerprint can commit; the loser re-reads the winner and follows the same-fingerprint or different-fingerprint branch. 

|**Scenario**|**Behavior**|
|---|---|
|Duplicate PLAY, same key/same fngerprint|Return original queue outcome. A second diferently-keyed PLAY while already<br>queued remains the locked deterministic no-op. Casually reusing the original<br>keyfor diferent PLAY felds is rejected as keyreuse.|
|Duplicate ACCEPT|<br>Same key/same fngerprint returns prior ACCEPT outcome; cannot increment<br>acceptance twice or create two matches.|
|DRAW retry after lost response|ProcessedCommand row returns stored applied version/outcome. Do not invoke<br>engine or draw another card.|
|Duplicate SELECT_TARGET|Same key/same fngerprint returns prior outcome. Same key with a diferent<br>target/decision is IDEMPOTENCY_KEY_REUSE.|
|Delayed packet|Validated against current decision/candidate state after dedupe; old decision_id<br>or terminal lifecycle is rejected.|



13/31 | DRAFT FOR FINAL ARCHITECTURE APPROVAL | Page 9 

||13/31 — Authoritative Multiplayer Technical Architecture v1.1|
|---|---|
|**Scenario**|**Behavior**|
|Resent Leave|Same key returns original FORFEIT_PENDING outcome. New key after already<br>pending yields deterministic no-op/ALREADY_FORFEIT_PENDING; never a<br>second trigger.|
|Same command_id, diferent command|Reject IDEMPOTENCY_KEY_REUSE even if the second request uses another<br>scope or decision.|
|Reconnect retry|Reconnect is logically idempotent; a new transport generation may be attached,<br>butgameplaymutation is not repeated.|



Processed-command outcome is committed in the same transaction as the authoritative state mutation. This makes crash-afterDRAW / retry decidable after recovery and makes idempotency-key misuse observable rather than ambiguous. 

### **10. State Versioning** 

Every committed match-level authoritative mutation increments match_state_version by exactly one. Rejected commands, pure reads, heartbeats that do not change presence state and duplicate-command lookups do not increment it. QueueEntry and CandidateSet have separate versions because they are not yet in the match ordering domain. 

#### **10.1 Stale-version algorithm** 

**1.** Deduplicate command_id first. A known command returns its original outcome regardless of current version. 

**2.** If expected_state_version is greater than current version, reject as invalid/future. 

**3.** If equal, perform normal legality validation. 

**4.** If lower, do not reject mechanically. Revalidate the semantic precondition token: for gameplay decisions, decision_id must still be the current pending decision and the same PLAYER_ID must still own it; for Leave the player must still be a valid in-progress roster member; for reconnect the active/terminal state is re-read. 

**5.** If the semantic precondition still holds, the command may be accepted against the current state. Otherwise reject STALE_STATE / DECISION_MISMATCH and provide current version/resync instruction. 

##### **Why decision_id is required** 

Presence changes and other product mutations may increment match_state_version while the same gameplay decision remains pending. A strict expected_version equality check would reject otherwise legal input. decision_id preserves exact decision ownership without weakening state validation. 

### **11. RNG / Randomness** 

All randomness is generated and consumed server-side. The recommended PRNG remains a deterministic ChaCha20-based random stream seeded with 256 bits from the operating-system cryptographic RNG at match creation. v1.1 formalizes the bounded-sampling contract required for uniform random seats/starters/shuffles. 

#### **11.1 Versioned uniform bounded primitive** 

```
uint32 NextUniformIntExclusive(uint32 maxExclusive) {
  require 1 <= maxExclusive <= 2^32;
  if (maxExclusive == 1) return 0;
  uint64 range = 2^32;
  uint64 limit = floor(range / maxExclusive) * maxExclusive;
  do { x = NextUInt32FromChaCha20(); } while (x >= limit);
  return x % maxExclusive;
}
```

The modulo operation occurs only after rejection has restricted the accepted PRNG range to an exact multiple of maxExclusive; therefore each result in [0,maxExclusive) has equal probability. Naive random_uint % maxExclusive over the full 2^32 range is forbidden when the bound does not divide the range evenly. 

|**Random use**|**Requiredprimitive / architecture**|
|---|---|
|Seat assignment|Deterministic Fisher–Yates over the accepted roster. Each j index is<br>NextUniformIntExclusive(i+1); persist seats and post-operation RNG state in the<br>conversion transaction.|
|Round 1 starter|NextUniformIntExclusive(participating_count); result/state committed before<br>publication.|
|Initial deck shufle|Create 112 unique card instances; deterministic Fisher–Yates using the same<br>boundedprimitive.|
|On-demand discard reshufle|Only when the locked rule requires it; deterministic Fisher–Yates, resulting pile<br>andpost-operation RNG state commit atomically.|
|Opening set-aside reintegration|The locked reintegration shufle uses the same versioned bounded primitive and<br>shufle implementation exactlyonceper required operation.|



13/31 | DRAFT FOR FINAL ARCHITECTURE APPROVAL | Page 10 

13/31 — Authoritative Multiplayer Technical Architecture v1.1 

#### **11.2 Replay compatibility and secrecy** 

- Persist rng_algorithm_version, bounded_sampling_algorithm_version and shuffle_algorithm_version as immutable match compatibility metadata. 

- Persist encrypted/restricted rng_seed reference and/or full PRNG stream state/counter server-side. 

- Persist the resulting ordered DRAW_PILE in every authoritative snapshot; recovery never re-runs a previously committed shuffle or consumes the same random words again. 

- Transition audit records RNG operation type, algorithm versions, before/after counter and state hash; random bytes and future deck order stay secret. 

- Changing the bounded sampler or Fisher–Yates implementation creates a new algorithm version for newly created matches; already-created matches retain their original versions. 

- Golden-vector tests pin exact sequences for bounds 2, 3, 4 and non-power-of-two values, plus full shuffle outputs and persisted-counter recovery. 

### **12. Match State Model** 

The match snapshot is the complete recoverable authority for an active match. The conceptual schema below is not SQL DDL; it is the aggregate boundary owned by the Match Actor. 

```
MatchState {
  match_id, rules_engine_version, product_contract_version
  lifecycle_state, match_state_version, lease_epoch
  roster[{player_id, seat_index}], seat_ring[]
  queue_provenance[player_id] {
    origin_queue_entry_id, origin_queue_started_at,
    origin_priority_creation_seq
  }
  players[player_id] {
    presence_state, active_connection_epoch
    unavailability_started_at, grace_deadline_id?
    consecutive_connected_timeouts
    forfeit_state, forfeit_reason, forfeit_trigger_version?
  }
  gameplay {
    session_state, round_index, round_kind, participants[]
    round_starter, turn_owner
    per_player { round_state, termination_reason, number_history[],
                 current_score, round_score, total_score, tiebreak_round_result? }
    draw_pile[], discard_pile[], opening_set_aside[]
    resolution_stack[DRAW_CONTEXT | EFFECT_CONTEXT]
    draw_recipient?, effect_drawer?, decision_owner?, effect_target?
    pending_decision {
      decision_id, kind, owner_player_id, deadline_id,
      counts_toward_connected_afk,
      afk_qualification_invalidated_at_version?,
      afk_qualification_reason?
    }?
  }
  deadlines[{deadline_id, kind, due_at_utc, generation, status}]
  admission/start metadata
  rng {
    rng_algorithm_version,
    bounded_sampling_algorithm_version,
    shuffle_algorithm_version,
    seed_reference/state, counter
  }
  terminal_claim?
  result_reference?
}
```

#### **12.1 Classification** 

|**State**|**Classifcation**|**Client visibility**|
|---|---|---|
|Roster, seats, lifecycle, scores, round states|Durable authoritative|Projected as appropriate.|
|Full deck order / RNG state|Durable authoritative secret|Never sent.|
|Resolution stack / pending decision|Durable authoritative|Client receives only its public/decision<br>projection.|
|Presence state / active connection epoch / grace<br>deadlines / AFK counters / forfeit state|Durable authoritative product state|Relevant state/deadline may be shown.|



13/31 | DRAFT FOR FINAL ARCHITECTURE APPROVAL | Page 11 

||13/31|— Authoritative Multiplayer Technical Architecture v1.1|
|---|---|---|
|**State**<br>|**Classifcation**|**Client visibility**<br>|
|Pending-decision connected-AFK qualifcation|Durable authoritative decision metadata; true<br>may irreversibly become false within the<br>decision|Not required as raw feld; warning/AFK outcome<br>may be projected.|
|Queue provenance on pre-start roster|Durable immutable match metadata until<br>cancellation/requeue is resolved|Not normally exposed.|
|WebSocket connection object / gateway routing<br>hint|Transient/reconstructable|N/A.|
|Animations, local countdown interpolation,<br>optimistic button state|Client-view-only|Not authoritative.|



### **13. Persistence** 

#### **13.1 Options** 

|**Model**|**Advantages**|**Disadvantages**|
|---|---|---|
|Snapshot only|Very simple recovery; tiny match state makes<br>frequent snapshots afordable.|Weak audit/replay explanation; dedupe/result<br>causalityharder to diagnose.|
|Full event sourcing|Excellent replay/history; state derived from events.|More implementation complexity, schema evolution<br>burden, event correctness becomes recovery-<br>critical.|
|Transactional snapshot + append-only transition<br>audit|Fast exact recovery plus audit-grade history; no<br>need to rebuild from logon everyfailover.|Writes both snapshot and audit row; slightly more<br>storage.|



##### **Recommendation** 

Use transactional full match snapshot after every accepted authoritative mutation, plus append-only MatchTransition, ProcessedCommand, Deadline and Outbox rows in the same PostgreSQL transaction. This is not full event sourcing; snapshots are the recovery source of truth and the log is audit/replay evidence. 

#### **13.2 Crash immediately after DRAW** 

**1.** Actor computes the deterministic new state in memory without mutating the durable snapshot. 

**2.** One PostgreSQL transaction atomically writes snapshot version N+1, processed command_id, transition audit row, deadline changes and outbox event. 

**3.** If transaction fails, no part is authoritative; client receives no success and retry may execute against version N. 

**4.** If transaction commits and process crashes before response, recovery loads N+1 and the processed command row. The retry returns ALREADY_APPLIED; no second draw occurs. 

**5.** If broadcast was not delivered, client reconnects and receives snapshot N+1; outbox may also replay the committed event. 

### **14. Commit / Atomicity Model** 

A single mailbox input is an atomic authoritative transition. The transition may traverse multiple deterministic engine substates but must stop at a durable yield point. All state that makes that yield internally consistent is committed together. 

|**Input**|**Atomic transaction includes**|
|---|---|
|DRAW that yields a number<br>|Card removed from DRAW_PILE; number appended; score/checks/round-state<br>updates; context quota updates; turn/boundary state; version; dedupe; audit;<br>outbox.<br>|
|DRAW that yields an efect target decision|Efect card removed from pile; EFFECT_CONTEXT with source/drawer/decision<br>owner persisted; resolution stack persisted; target decision and 10s deadline<br>created; version/dedupe/audit/outbox.<br>|
|SELECT_TARGET|Target selection, efect application, any nested automatic draws until next yield,<br>all score/context changes, new deadline if another decision appears.|
|Timer expiry|Deadline status consumed plus resulting gameplay/product transition and any<br>next deadlines.|
|Leave / grace expiry|FORFEIT_PENDING trigger and reason, version, transition audit; gameplay<br>round_state untouched.|
|Terminal result|Match lifecycle terminal transition + unique immutable MatchResult + outbox in<br>one transaction.|



The actor publishes only after database commit. There is no supported code path that broadcasts a new authoritative state and then attempts persistence. An outbox row is written in the same transaction so committed events remain publishable after process failure. 

### **15. Timer Architecture** 

All product/gameplay timers are represented as durable deadlines. Client countdowns are derived presentation and can be wrong without affecting authority. 

|**Timer**<br>**Locked duration**<br>**Owner domain**|
|---|
|Match Found acceptance<br>10s<br>MatchmakingCoordinator / CandidateSet|
|13/31 | DRAFT FOR FINAL ARCHITECTURE APPROVAL | Page12|



||13/31 — Authoritative Multiplayer Technical Architecture v1.1<br> <br>|
|---|---|
|**Timer**|**Locked duration**<br>**Owner domain**|
|Admission|30s shared<br>Match Actor|
|Start countdown|3s<br>Match Actor|
|Normal action|20s<br>Match Actor + Game Engine timeout input|
|Target selection|10s<br>Match Actor + Game Engine timeout input|
|Queue disconnectgrace|10s<br>MatchmakingCoordinator|
|Queue backgroundgrace|30s<br>MatchmakingCoordinator|
|In-match unavailability grace|60s continuous<br>Match Actor|



#### **15.1 Durable deadline record** 

```
Deadline {
  deadline_id, entity_type, entity_id, kind
  due_at_utc, generation
  status = ACTIVE | CONSUMED | CANCELLED
  created_state_version
  player_id? / decision_id?
}
```

#### **15.2 Clock model** 

- Persist due_at_utc using database/server UTC time. Client time is never authoritative. 

- Use process monotonic clocks only to schedule local wakeups between commits; after restart recompute remaining delay from persisted due_at_utc and database/current server time. 

- Lease expiry and authoritative deadline comparisons use database time to avoid node wall-clock disagreement. 

- NTP/clock monitoring remains operationally required, but correctness does not depend on a client clock. 

#### **15.3 Exactly-once logical expiry** 

**1.** Creating/cancelling a deadline is part of the same transaction that creates/removes the waiting state. 

**2.** The owner maintains local timers for low latency. A separate scanner polls ACTIVE due deadlines from PostgreSQL and is the recovery safety net. 

**3.** Local callback or scanner emits TimerExpired(deadline_id, generation) into the authoritative owner mailbox. 

**4.** Actor validates that the same deadline generation is still ACTIVE. If already cancelled/consumed, expiry is a no-op. 

**5.** Consuming the deadline and applying its outcome commit atomically. Duplicate expiry delivery therefore cannot apply twice. 

#### **15.4 Restart** 

After ownership recovery, the actor loads all ACTIVE deadlines. Future deadlines are rescheduled locally. Deadlines already due are enqueued before accepting later external intents. This preserves the locked post-expiry stale-input rule without relying on a timer process having survived. 

#### **15.5 Connected-AFK qualification on decision deadlines** 

**1.** When a normal-action or target decision is created, pending_decision.counts_toward_connected_afk is set true only if DECISION_OWNER is authoritatively IN_MATCH_CONNECTED at decision start. 

**2.** If that owner enters TEMPORARILY_UNAVAILABLE or IN_MATCH_DISCONNECTED before the decision resolves, the same match transaction sets counts_toward_connected_afk=false and stores the invalidation version/reason. 

**3.** For the same decision the flag is monotonic: false never becomes true on reconnect. 

**4.** At timeout resolution, the locked gameplay timeout outcome is applied first. The product AFK counter increments only if the committed pending-decision flag was still true. 

**5.** Because the flag is in the durable MatchSnapshot, crash/failover never reconstructs AFK qualification from transport logs. 

### **16. Race Ordering** 

The product invariant “first authoritative committed transition wins” is implemented by domain serialization, durable deadline guards, connection-generation fencing and transactional uniqueness. Client timestamps never decide a race. 

|**Race**|**Serialization / commit mechanism**|**Deterministic outcome**<br>|
|---|---|---|
|Action vs 20s timeout|Both enter same Match Actor. Due-deadline barrier prevents<br>input received after an already-due deadline from bypassing<br>expiry; whichever transition commits frst closes decision_id.|Action frst => action. Timeout frst => STOPPED+TIMEOUT;<br>late action rejected.<br>|
|Target vs 10s timeout|Same actor + same decision_id + deadline generation.|Target frst => selected target. Expiry frst => self-target; late<br>target rejected.<br>|
|Disconnect/background vs decision timeout|Same Match Actor. A valid current-epoch unavailability<br>transition that commits before timeout permanently fips<br>pending_decision.counts_toward_connected_afk=false.|Unavailability frst => timeout does not increment connected-<br>AFK. Timeout frst => decision ended while qualifcation was<br>still true and may count; later disconnect afects only<br>subsequent state.|
|New connection vs stale old disconnect|Connection attach carries a server-issued epoch;<br>actor/coordinator stores active_connection_epoch.<br>Demotion events must match it.|CONNECTED(18) committed then DISCONNECTED(17) =><br>stale NO_OP; no grace/AFK/liveness change.<br>DISCONNECTED(18)maydemote.<br>|
|Cancel queue vs candidate commit|Same queue-partition coordinator; candidate reservation<br>transaction locksqueue entry.|Cancel frst removes eligibility; candidate commit frst<br>causes cancel to be handled as DECLINE.|



13/31 | DRAFT FOR FINAL ARCHITECTURE APPROVAL | Page 13 

13/31 — Authoritative Multiplayer Technical Architecture v1.1 

|**Race**|**Serialization / commit mechanism**|**Deterministic outcome**|
|---|---|---|
|Admission cancellation vs JOIN/reconnect|Match Actor serializes JOIN/presence and admission-<br>deadline expiry. Cancellation transaction determines<br>afected/failing players from the committed state.|JOIN/connection frst may satisfy READY. Admission expiry<br>frst commits MATCH_CANCELLED and any qualifying auto-<br>requeue; late JOIN cannot resurrect match.|
|Auto-requeue retry after lost response|Cancellation + PlayerActivity changes + new QueueEntry<br>rows + provenance copy commit in one transaction with<br>unique requeue source key.|Retry reads terminal cancellation and existing requeue row;<br>cannot create a second active QueueEntry.|
|Reconnect vs 60s expiry|Presence restoration and grace expiry are match-actor inputs<br>consuming same deadline generation and current<br>connection epoch.|Reconnect commit frst clears grace; expiry commit frst<br>creates irreversible FORFEIT_PENDING.|
|GAME_ENDED vs FORFEIT_PENDING|Single actor. Engine progression yields at safe post-<br>resolution boundary before starting next progression.|Normal GAME_ENDED frst => normal result. Pending forfeit<br>frst => current started resolution may fnish, then forfeit path<br>owns boundary.|
|Leave vs disconnect|Same actor. Confrmed Leave creates pending forfeit and<br>invalidates future participation regardless of connection<br>state.|Whichever commits frst is recorded; confrmed Leave before<br>grace expiry immediately creates irreversible pending forfeit<br>as locked.|
|Two requests share command_id with diferent payloads|ProcessedCommand unique (player_id, command_id) key is<br>claimed transactionally; fngerprint comparison is<br>mandatory.|First committed fngerprint owns key. Same fngerprint retries<br>get old outcome; diferent fngerprint rejects<br>IDEMPOTENCY_KEY_REUSE.|
|RNG consumed transition vs actor crash|Random calls occur only while computing one transition;<br>post-call RNG counter and resulting state commit atomically<br>with the transition.|Commit => recovery loads post-call RNG state and never<br>consumes again. No commit => retry starts from pre-call<br>state and deterministicallyrecomputes once.|
|Reconnect during fnalization|Final result commit and reconnect event share actor<br>ordering; Result unique insert/lifecycle terminalguard.|Terminal commit frst => result view only; reconnect cannot<br>mutate result.|



### **17. Matchmaking Architecture** 

Public solo matchmaking has one authoritative coordinator per queue partition. For MVP the partition key is ruleset/mode plus deployment region configuration; there is no party or skill/MMR dimension. Only one fenced coordinator mutates a partition at a time. 

#### **17.1 QUEUE_ENTRY storage and priority provenance** 

|**Field**|**Meaning**<br>|
|---|---|
|queue_entry_id|Physical immutable row identifer. A new row is created after qualifying post-<br>conversion auto-requeue;CONSUMED historyis never resurrected.|
|player_id|Unique amongactivequeue/candidate states.|
|queue_started_at|Authoritative queue-age origin used by allowed_group_sizes; preserved across<br>permitted unafected auto-requeue.|
|priority_creation_seq|Stable creation-order tie-break of the original logical queue priority. Preserved<br>acrosspermitted unafected auto-requeue.|
|row_creation_seq|Physical row creation sequence for audit only; never replaces preserved<br>priority_creation_seqafter auto-requeue.|
|origin_queue_entry_id|Self/original entry for manual PLAY; preserved original provenance when a new<br>row is auto-requeued.|
|state|QUEUED|RESERVED|INVALIDATED|CONSUMED.|
|active_connection_epoch|Latest server-issued connection generation recognized by queue presence; stale<br>demotion events are ignored.|
|presence_owner / efective deadline|FOREGROUND or BACKGROUND plus durable efective liveness deadline<br>reference.|
|version|Monotonicqueue-entryversion.|



#### **17.2 General deterministic candidate evaluator** 

Candidate selection implements MP-DR-001 directly. The coordinator never assumes that failure to form a larger group means it must wait; it evaluates candidate sizes in descending order and may form a smaller currently legal group. 

```
eligible_entries = live durable QUEUED entries not reserved
order key = (queue_started_at ASC, priority_creation_seq ASC)
```

```
for N in [4, 3, 2]:
    eligible_for_N = [e for e in eligible_entries
                      if N in allowed_group_sizes(authoritative_queue_age(e))]
    sort eligible_for_N by order key
    if len(eligible_for_N) >= N:
        candidate = first N entries
        reserve candidate transactionally
        return candidate
return NONE
```

For this MVP ruleset, filtering by N and taking the first N entries is mathematically equivalent to enumerating legal N-member combinations because legality is per-member and has no pairwise constraint: an N-set is legal iff every member independently allows N. Descending N enforces largest legal group; ordering the eligible set enforces oldest eligible entries first and priority_creation_seq resolves exact-age ties deterministically. 

|**Required case**|**Evaluation**<br>|**Outcome**|
|---|---|---|
|A: 27s, 25s, 5s|N=4 insuficient. N=3: only 27s/25s allow 3 =><br>insuficient. N=2: 27s/25s allow 2 and are oldest eligible.|FORM 2P from 27s + 25s; do not wait for 3p.|
|B: 27s, 15s, 12s|N=3: all three allow 3.|FORM 3P.|



13/31 | DRAFT FOR FINAL ARCHITECTURE APPROVAL | Page 14 

||13/31 —|Authoritative Multiplayer Technical Architecture v1.1|
|---|---|---|
|**Required case**|**Evaluation**|**Outcome**|
|C: 30s, 30s, 8s, 2s|N=4: all entries allow 4.<br>|FORM 4P.|
|D: 5+ mixed ages|Evaluate N=4 frst; choose frst four among entries that|Largest legal candidate; oldest eligible; exact tie by|
||allow 4 (all current entries do). If a future contract<br>changes eligibility, samegeneral flter still applies.|priority_creation_seq.|



#### **17.3 Reservation transaction** 

Candidate formation locks the chosen queue rows, revalidates current authoritative ages/liveness, current connection epochs and PlayerActivity invariants, creates CANDIDATE_SET, marks rows RESERVED, creates the 10s deadline and writes outbox notifications. Unique active-state constraints prevent a PLAYER_ID from entering two candidates; fencing blocks stale coordinator commits. 

#### **17.4 Queue liveness** 

Queue background/disconnect state and effective deadline are durable. Ownership transitions follow the locked min-deadline rules. Transport demotion signals carry connection_epoch and are applied only when epoch == active_connection_epoch; stale older events cannot start a grace or invalidate an entry. Background/network restoration does not reset queue_started_at or priority_creation_seq. Expiry invalidates the active entry and later reconnect cannot resurrect it. 

### **18. Candidate / Admission Architecture** 

#### **18.1 CANDIDATE_SET lifecycle** 

|**State**|**Technical behavior**|
|---|---|
|RESERVED|2–4 QueueEntries frozen; candidate_id/version/deadline durable; explicit<br>ACCEPT/DECLINE commands allowed.|
|DISSOLVED|Any decline/timeout or validation failure before match conversion. Failing<br>member returns lobby; unafected RESERVED rows may return to QUEUED with<br>the same row and unchanged queue priority because they were not yet<br>CONSUMED.|
|CONVERTING|Ephemeral transaction stage only;never externallyvisible.|
|CONVERTED|Exactly one durable Match created; original QueueEntries are permanently<br>CONSUMED;PlayerActivitymoves to match admission.|



#### **18.2 Atomic candidate → MATCH_ROSTER conversion** 

**1.** Lock candidate row and all member/PlayerActivity rows. 

**2.** Validate candidate still RESERVED, deadline not consumed, all members ACCEPTED and every player still owns the same reserved queue entry. 

**3.** Generate unique match_id and server RNG seed; use the versioned unbiased RNG API to assign immutable seats according to the locked gameplay contract. 

**4.** Insert Match + MatchSnapshot lifecycle MATCH_CREATED, immutable roster/seat order, match_state_version=1 and shared admission deadline = transaction-authoritative creation time + 30s. 

**5.** For every roster member copy immutable queue provenance into the match: origin_queue_entry_id, origin_queue_started_at and origin_priority_creation_seq. 

**6.** Mark candidate CONVERTED, original queue rows CONSUMED and PlayerActivity as MATCH_ADMISSION. 

**7.** Insert outbox notifications and commit once. If any write fails, the entire conversion rolls back; no partial roster exists. 

#### **18.3 Admission and start** 

The Match Actor owns all post-creation admission. JOIN_MATCH commits JOINED and current fenced connection presence. READY is derived, never commanded. When all roster members are JOINED+IN_MATCH_CONNECTED, actor atomically transitions MATCH_READY→MATCH_STARTING and creates the 3s start deadline. Disconnect during that countdown does not cancel; the 60s in-match grace begins from the authoritative unavailability event and the countdown still expires into MATCH_IN_PROGRESS. 

#### **18.4 Partial process failure** 

Match creation is durable before any actor process is required. If conversion commits but no runtime process is active, the match and 30s admission deadline still exist. A runtime node can claim it later. Clients are never told MATCH_CREATED before commit; process activation is reconstructable infrastructure, not match creation authority. 

#### **18.5 Peer-caused pre-start cancellation auto-requeue** 

After candidate conversion, original QueueEntries remain CONSUMED forever. If a roster member causes a qualifying pre-start MATCH_CANCELLED / NO_CONTEST and the locked product contract permits unaffected players to auto-requeue, cancellation finalization and requeue restoration use one PostgreSQL transaction. 

**1.** Lock Match, relevant PlayerActivity rows and any active QueueEntry rows for the roster. 

13/31 | DRAFT FOR FINAL ARCHITECTURE APPROVAL | Page 15 

13/31 — Authoritative Multiplayer Technical Architecture v1.1 

**2.** Classify the failing player and unaffected players from the authoritative cancellation cause/state. 

**3.** For each unaffected player entitled to auto-requeue, create a new physical QueueEntry with a new queue_entry_id and row_creation_seq, but copy origin_queue_started_at into queue_started_at and origin_priority_creation_seq into priority_creation_seq; set origin_queue_entry_id to the preserved original provenance. 

**4.** Transition that player’s PlayerActivity directly from cancelled-match admission to QUEUED and bind it to the new QueueEntry. The failing player transitions to LOBBY and receives no auto-requeue. 

**5.** Enforce one active queue row per PLAYER_ID and a unique AutoRequeueSource(cancelled_match_id, player_id, reason) key so retries are exactly once. 

**6.** Commit MATCH_CANCELLED terminal record, PlayerActivity transitions, new QueueEntries and outbox notifications atomically. If the transaction fails, none of them is authoritative and no terminal success is published. 

##### **Priority preservation rule** 

Matchmaking ordering after auto-requeue uses the preserved logical priority tuple (queue_started_at, priority_creation_seq), not the new row_creation_seq. Therefore an unaffected player keeps the original queue age and exact stable tie-break position without pretending the historical CONSUMED row became active again. 

### **19. Identity / Session** 

PLAYER_ID is server-issued, opaque and persistent. Persistent guest identity is the default MVP bootstrap; registered/social accounts are unnecessary. 

#### **19.1 Guest bootstrap** 

**1.** On first launch the app calls an HTTPS guest-bootstrap endpoint over TLS. 

**2.** Server creates an opaque PLAYER_ID and a high-entropy refresh credential. The raw refresh credential is returned once; only a salted/hash form is stored server-side. 

**3.** Client stores the refresh credential in iOS Keychain / Android Keystore-backed secure storage. PLAYER_ID alone is not sufficient to authenticate. 

**4.** Refresh exchanges mint short-lived signed access tokens containing PLAYER_ID, session_id and token/session generation. Token duration/rotation are security tunables and do not change gameplay rules. 

**5.** App relaunch uses the stored refresh credential to recover the same PLAYER_ID. Reinstall/cross-device recovery is explicitly out of MVP. 

#### **19.2 Server-issued connection generation** 

connection_epoch is a monotonic server-issued generation for a PLAYER_ID transport attachment; the client cannot choose it. On successful authenticated WSS attachment the session/identity layer atomically allocates the next epoch. A transport connection is not authoritative presence merely because the socket opened: the owning coordinator/Match Actor must commit ConnectionAttached(epoch) and store active_connection_epoch. 

```
ConnectionAttached(player, epoch=18) -> owner commits active_connection_epoch=18
TransportDisconnected(player, epoch=17) -> STALE / NO_OP
```

```
TransportDisconnected(player, epoch=18) -> may demote presence and start/continue grace
```

#### **19.3 Session ownership** 

- Gateway derives authenticated PLAYER_ID from token and ignores conflicting client-supplied identity fields. 

- Every transport-originated presence/lifecycle event carries server-issued connection_epoch. Demotion events are legal only for the current authoritative epoch. 

- PlayerActivity is enforced by a durable row keyed by PLAYER_ID, preventing simultaneous active queue/candidate and gameplay match ownership. 

- Fabricating another PLAYER_ID, match_id, seat index or connection_epoch does not grant authority; membership/generation are resolved from server state. 

- Token/session revocation can invalidate future connections but does not rewrite already committed match transitions. 

### **20. Reconnect** 

#### **20.1 Exact reconnect sequence** 

A newly opened socket does not immediately overwrite authoritative presence. Reconnect first obtains a server-issued connection_epoch, then the active domain owner commits that generation before it can demote/restore current presence. 

**1.** Client launches and restores guest refresh credential from secure storage. 

**2.** HTTPS token refresh authenticates the same PLAYER_ID. 

**3.** Gateway queries/receives player_activity and detects active candidate/admission/match/terminal record. 

**4.** Client opens authenticated WSS. Identity/session allocates a new monotonic server-issued connection_epoch; RECONNECT/resume carries that epoch plus last_seen_match_state_version for diagnostics. 

13/31 | DRAFT FOR FINAL ARCHITECTURE APPROVAL | Page 16 

13/31 — Authoritative Multiplayer Technical Architecture v1.1 

**5.** Gateway routes to current Match Actor; actor validates roster membership/product state and commits ConnectionAttached(epoch) only if the generation is newer/current. This becomes active_connection_epoch. 

**6.** Actor commits presence restoration only if the player has not already become irreversible FORFEIT_PENDING/terminal and the grace expiry has not won the ordering race. 

**7.** Actor produces a full player-projected authoritative snapshot at current match_state_version. No speculative commands are accepted until this resync completes. 

**8.** Gateway sends snapshot, then resumes committed event delivery. Client discards/reconciles any local assumptions and enables only server-advertised legal actions. 

#### **20.2 Required cases** 

|**Case**|**Architecture outcome**|
|---|---|
|Wi‑Fi → mobile|Transport connection may change; same token/PLAYER_ID reconnects. If<br>usablepresence was interrupted,same 60sgrace recordgoverns.|
|App crash/kill|Socket loss makes presence unavailable; process-local client state is<br>irrelevant. Relaunch authenticates and resyncs durable match.|
|Delayed reconnect before grace expiry|Presence restoration competes with grace expiry in actor. If restoration<br>commits frst, grace is cancelled and full snapshot returned.|
|Reconnect after gameplay timeout|Snapshot includes already-committed STOPPED+TIMEOUT or self-target<br>outcome; no rollback.|
|Reconnect after FORFEIT_PENDING|Identity may authenticate but gameplay participation is not restored; client<br>receivespending/terminalprojection only.<br>|
|Reconnect after MATCH_COMPLETED|Read-onlyfnal resultprojection; no match mutation.|



### **21. State Synchronization** 

|**Strategy**|**Assessment**|
|---|---|
|Full snapshot everyupdate|Simple but noisyand weak for animation causality.<br>|
|Incremental events only|Eficient but reconnect/gap recovery becomes event-stream dependent<br>and harder to make robust.|
|Hybrid snapshot + events|Small authoritative snapshots give simple recovery; versioned event<br>batchesgive eficient realtime and animation cues.|



##### **Recommendation** 

Hybrid synchronization. Send a full player-projected snapshot on JOIN_MATCH, RECONNECT, explicit RESYNC_REQUIRED, state-version gap or integrity mismatch. During a healthy socket, send versioned committed event batches/deltas. The server can always fall back to snapshot because it is the durable recovery authority. 

#### **21.1 Client reconciliation** 

- Every server snapshot/event batch includes match_id and authoritative state_version. 

- Client applies only monotonically newer versions; duplicate/older events are ignored. 

- A gap in versions or state-hash/projection mismatch triggers snapshot resync rather than local guessing. 

- Snapshots are projections: future draw-pile order, RNG state and server-only audit fields are omitted. 

- Outbound backpressure can drop superseded incremental messages and send RESYNC_REQUIRED; it must never drop or alter server authority. 

#### **21.2 Client prediction policy** 

##### **Recommendation** 

Do not perform speculative authoritative gameplay prediction in MVP. The client may immediately acknowledge button presses visually and stage reversible presentation animations, but drawn cards, score changes, target outcomes, turn progression, timer expiry, forfeit state and winner/result are displayed as authoritative only after a committed server event/snapshot. 

- The client may compute presentation-only countdown interpolation from a server deadline, but expiry authority remains server-side. 

- The client may highlight legal-looking buttons/targets from the latest projection for UX, but the server revalidates every submitted intent. 

- If presentation animation starts before the authoritative response, it must be cancellable/reconcilable and must not mutate the client model used as game truth. 

- No local deck/RNG simulation is used to predict future cards. 

### **22. Presence** 

Presence is a product-layer state machine stored beside, not inside, gameplay round_state. The owning Match Actor commits durable transitions; Gateway supplies authenticated transport/lifecycle signals with a server-issued connection_epoch. 

13/31 | DRAFT FOR FINAL ARCHITECTURE APPROVAL | Page 17 

13/31 — Authoritative Multiplayer Technical Architecture v1.1 

|**Presence state**|**Technical representation**|**Gameplay efect**|
|---|---|---|
|IN_MATCH_CONNECTED|Usable authenticated active_connection_epoch; no<br>active unavailability grace.|Allows legal input when gameplay role permits.|
|TEMPORARILY_UNAVAILABLE|Current-epoch app background/suspend signal;<br>unavailability_started_at and 60s deadline durable.|No direct gameplay-state mutation; gameplay<br>timers continue.|
|IN_MATCH_DISCONNECTED|Current-epoch transport unusable/lost; same 60s<br>grace model.|No direct gameplay-state mutation; still targetable if<br>round_state ACTIVE.|
|RECONNECTING|Authenticated newer connection is resyncing; no<br>speculativegameplayinput accepted.|No rollback or action until snapshot ack/resync<br>completes.|
|FORFEIT_PENDING|Irreversible product terminal trigger committed.|Gameplay round_state remains whatever engine<br>committed;no new decisions frompending player.|



#### **22.1 Connection-epoch fencing** 

**1.** ConnectionAttached(epoch) may advance active_connection_epoch only for a server-issued valid generation and is serialized by the current domain owner. 

**2.** A transport-originated demotion such as DISCONNECTED/BACKGROUND may mutate authoritative presence only when event.connection_epoch == active_connection_epoch. 

**3.** If the event epoch is older, return STALE_PRESENCE_EVENT / NO_OP. It must not start grace, invalidate queue liveness, flip connected-AFK qualification, cancel the newer connection or create FORFEIT_PENDING. 

**4.** Queue Coordinator applies the same guard to active QueueEntry presence. Candidate ACCEPT/DECLINE remains command/deadline-based; admission connection state is fenced by the Match Actor. 

**5.** If a newer connection is transport-open but its attach has not yet committed, the previous epoch remains authoritative until ordered otherwise. This preserves first-committed-transition semantics. 

#### **22.2 Durable connected-AFK decision qualification** 

When the engine/host creates a pending gameplay decision, counts_toward_connected_afk starts true only when DECISION_OWNER is IN_MATCH_CONNECTED. Any valid current-epoch transition to TEMPORARILY_UNAVAILABLE or IN_MATCH_DISCONNECTED before decision completion is committed in the same MatchSnapshot mutation together with counts_toward_connected_afk=false. Reconnect never re-enables the flag for that decision. A stale old-epoch disconnect is a noop and therefore cannot invalidate a decision that remained authoritatively connected. 

#### **22.3 Transport health detection** 

The concrete WebSocket heartbeat interval and silent-link failure threshold are transport engineering parameters, not new reconnect-grace durations. Recommended initial values remain 5s ping / approximately 10s silent-link detection (ENGINEERING TARGET — TUNABLE). The 60s product grace begins when the server commits the current-epoch presence transition to unavailable. 

### **23. Forfeit / Safe Boundary** 

#### **23.1 Forfeit triggers** 

|**Trigger**|**Actor input**|**Commit**|
|---|---|---|
|60s continuous unavailability|GRACE_EXPIRED(player, deadline_id)|Mark FORFEIT_PENDING + reason<br>RECONNECT_GRACE; consumegrace deadline.<br>|
|Three consecutive fully-connected decision<br>timeouts<br>|Post-timeout product evaluation using durable<br>pending_decision.counts_toward_connected_afk|Gameplay timeout result commits frst. Increment<br>counter only if fag remained true; after third<br>qualifying timeout same actor commits<br>FORFEIT_PENDING + CONNECTED_AFK.|
|Confrmed Leave|LEAVE_MATCH command|Immediate irreversible FORFEIT_PENDING + LEAVE;<br>nograce.|



#### **23.2 Exact safe gameplay boundary definition** 

##### **is_safe_gameplay_boundary == true** 

The Game Engine has completed the current authoritative decision outcome and all already-started draw/effect contexts required by atomicity; resolution_stack is empty; pending_decision is null; the engine is at the host yield immediately before any next TURN_START / ROUND_END progression. The host therefore can terminate without interrupting a decision, started effect or draw obligation and without scoring an unfinished round retroactively. 

WAITING_FOR_PLAYER_ACTION and WAITING_FOR_TARGET_SELECTION are not safe because a locked decision remains unresolved. A non-empty DRAW_CONTEXT/EFFECT_CONTEXT stack is not safe. The engine exposes boundary_kind and is_safe_gameplay_boundary explicitly; the product layer never guesses from UI state or timers. 

#### **23.3 Forfeit handoff algorithm** 

**1.** Commit FORFEIT_PENDING immediately when its trigger wins ordering. Do not mutate gameplay round_state. 

13/31 | DRAFT FOR FINAL ARCHITECTURE APPROVAL | Page 18 

13/31 — Authoritative Multiplayer Technical Architecture v1.1 

**2.** If the engine is inside a pending decision/started context, allow only the locked decision timeout/atomic effect completion required to reach SAFE_POST_RESOLUTION. Reject new gameplay decisions from the pending-forfeit player. 

**3.** At SAFE_POST_RESOLUTION, do not call engine continuation that would start a new turn or round while any FORFEIT_PENDING exists. 

**4.** Derive ELIGIBLE_SURVIVOR from roster: not forfeited/pending, currently IN_MATCH_CONNECTED and otherwise able to continue product participation. 

**5.** If eligible count >=1, construct FORFEIT_COMPLETION immediately. survivor_set is all non-forfeited players, not merely currently eligible players. 

**6.** If eligible count ==0, commit lifecycle FORFEIT_RESOLUTION_WAIT. Gameplay is frozen; only reconnect/presence/grace/integrity events remain accepted. 

**7.** During WAIT, any qualifying reconnect causes re-evaluation and immediate finalization; each player’s independent grace expiry may add another pending forfeit. If all remaining non-forfeited players irreversibly expire with no eligible survivor, commit ABORTED_NO_CONTEST. 

### **24. Result Finalization** 

Result Finalizer is a pure function that constructs the locked result schema from the final match snapshot and terminal claim. Match Actor remains the only component allowed to commit it. 

|**Field**|**Validation rule**|
|---|---|
|match_id|Exactlycurrent match;unique resultprimarykey.|
|roster / seat_order|Exact immutable values from MATCH_CREATED.|
|termination_type|NORMAL_COMPLETION, FORFEIT_COMPLETION, CANCELLED_NO_CONTEST or<br>ABORTED_NO_CONTEST only.|
|winner_set|Exactly one for NORMAL_COMPLETION and one-survivor<br>FORFEIT_COMPLETION; emptyotherwise.|
|survivor_set|FORFEIT_COMPLETION only: all non-forfeited players; empty for normal/no-<br>contest.|
|forfeited_players|All irreversiblepending/fnal forfeits included in FORFEIT_COMPLETION.|
|fnal_total_scores|Locked TOTAL_SCORE through last completed normal round; unfnished round<br>not retroactivelyscored.|
|result_validity|NORMAL_COMPETITIVE, FORFEIT_COMPETITIVE, FORFEIT_ADMINISTRATIVE or<br>NO_CONTEST consistent with termination/winner/survivor sets.|
|fnalization_reason|Specifc authoritative terminal cause.|
|forfeit_reasons_by_player|Required when forfeited_players non-empty.|



#### **24.1 Immutable commit** 

**1.** Actor obtains terminal path ownership in its serialized state. 

**2.** Result Finalizer validates all cross-field invariants and produces a canonical result hash. 

**3.** One PostgreSQL transaction inserts MatchResult with unique(match_id), sets match lifecycle terminal, stores result_reference/hash, closes active deadlines/activity and writes transition/outbox. 

**4.** Database application role has no ordinary UPDATE path for MatchResult; any later finalization attempt reads the existing immutable record. A mismatch with an already-existing hash is an integrity alert, never an update. 

**5.** After commit, reconnect or late commands return terminal projection only. 

### **25. Failure Recovery** 

|**Failure**|**Authoritative behavior**|**Recovery**|
|---|---|---|
|Match worker/process crash|No uncommitted in-memory transition is authoritative.<br>Last committed snapshot/version remains truth.|New worker claims higher fencing epoch, reloads<br>snapshot/dedupe/RNG/deadlines, schedules due work,<br>clients resync.|
|Matchmaking process crash|Queue/candidate rows and deadlines remain durable;<br>reservations are not lost.|Standby coordinator acquires partition lease/fence,<br>reloads state, processes due acceptance/liveness<br>deadlines.|
|Database temporarily unavailable|No authoritative mutation can commit; server must not<br>report success. In-memory proposals are<br>discarded/retried.|HA database failover/recovery; actors reload/revalidate.<br>Persisted deadlines that became due are processed<br>before later input. If integrity becomes uncertain,<br>existing ABORTED_NO_CONTEST path is used rather<br>than fabricatingstate.|
|Gateway restart|No authoritative state lost. Connections drop; transport<br>presence maybecome unavailable.|Clients reconnect; worker ownership and snapshots<br>remain. Reconnect storm handled byscaledgateways.<br>|
|Timer scanner restart|No deadline is lost because due_at/status are in<br>PostgreSQL.|New scanner fnds ACTIVE due deadlines; duplicate<br>expiryis idempotent.|
|Client gets no response after valid command|Outcome may be uncommitted or committed.|Retry same command_id; dedupe returns prior outcome<br>if committed, otherwise actor evaluates once.|
|Persistence success + broadcast failure|Commit remains authoritative.|Outbox retry and/or client snapshot resync deliver<br>current state.|
|Pre-start cancellation/requeue commit + response loss|Cancellation and exactly-one replacement QueueEntry<br>are already authoritative.|Retry/read returns terminal cancellation plus existing<br>auto-requeue state; unique source key prevents a<br>second entry.|
|Actor crash after AFK qualifcation became false|Durable pending_decision metadata already records<br>false.|New owner loads false; later timeout cannot count as<br>fullyconnected.|



13/31 | DRAFT FOR FINAL ARCHITECTURE APPROVAL | Page 19 

||13/31 —|Authoritative Multiplayer Technical Architecture v1.1|
|---|---|---|
|**Failure**|**Authoritative behavior**|**Recovery**|
|Actor crash after RNG-consuming transition commit|Snapshot contains resulting state and post-call RNG|New owner never reconsumes committed random calls;|
||counter/versions.|retryuses ProcessedCommand.|
|Broadcast success + persistence failure|Architecturally prohibited: publication path is invoked<br>onlyafter commit acknowledgment.|Any code path violating this is a correctness defect and<br>test failure.|



### **26. Match Ownership / Failover** 

Each Match Runtime node may host many match actors, but one match_id has one mutation owner epoch at a time. Ownership is durable and fenced in PostgreSQL; Redis routing hints do not grant authority. 

```
MatchLease {
  match_id PRIMARY KEY
  owner_instance_id
  lease_epoch BIGINT
  lease_expires_at (DB time)
}
```

```
Every match mutation transaction includes:
  WHERE match_id = ?
    AND lease_epoch = actor_epoch
    AND match_state_version = expected_version
```

```
If zero rows are updated, the actor is stale and must stop.
```

#### **26.1 Claim / renew / failover** 

**1.** Node claims an unowned/expired lease using database time and increments lease_epoch atomically. 

**2.** Actor periodically renews lease. Renewal failure causes it to stop accepting new authoritative work and revalidate ownership before any further commit. 

**3.** After expiry, another node claims a higher epoch. Old node may still be alive due partition, but every stale write is rejected by fencing predicate. 

**4.** New node loads last committed snapshot and active deadlines. Ownership routing hint in Redis is refreshed after DB claim, never before. 

**5.** Clients need not know the owner node; Gateway reroutes or returns a retryable resync while ownership is moving. 

### **27. Networking** 

#### **27.1 Transport decision** 

|**Option**|**Assessment**|
|---|---|
|WebSocket|Bidirectional low-latency channel, natural for commands/events/presence;<br>explicit reconnect and ordering semantics remain under application<br>control.|
|HTTP commands + polling/push|Simpler request handling but poor synchronous event/presence<br>experience; mobilepush is not realtime-authoritative.|
|Managed proprietary realtime SDK|Can reduce socket operations but introduces vendor semantics/lock-in<br>around ordering/reconnect that still must obeycustom rules.|



##### **Recommendation** 

Use TLS WebSocket (WSS) for active queue/candidate/match realtime commands and committed events. Use HTTPS for guest bootstrap, token refresh and non-realtime recovery/bootstrap APIs. Internal Gateway→Game Backend calls use authenticated gRPC/HTTP2. 

#### **27.2 Connection model** 

- WSS connection authenticates with short-lived access token; token maps to PLAYER_ID/session generation. 

- Identity/session allocates a monotonic server-issued connection_epoch for each authenticated attachment; Gateway maintains connection_id/heartbeat and includes epoch on transport events. Authoritative owner stores active_connection_epoch and fences stale demotions. 

- Commands carry command_id and semantic scope/decision token; transport ordering alone is never trusted as the authority ordering mechanism. 

- Server events carry state_version and server_message_seq. Client detects duplicates/gaps. 

- Backpressure uses bounded per-connection buffers. If the client falls behind, gateway emits RESYNC_REQUIRED rather than allowing unbounded memory growth. 

- Reconnect always performs snapshot resync before enabling gameplay input. 

13/31 | DRAFT FOR FINAL ARCHITECTURE APPROVAL | Page 20 

13/31 — Authoritative Multiplayer Technical Architecture v1.1 

#### **27.3 API / Message Boundary** 

|**Category**|**Representative messages**|**Contract rule**|
|---|---|---|
|Client → Server commands|PLAY, CANCEL_QUEUE, ACCEPT_MATCH,<br>DECLINE_MATCH, JOIN_MATCH, DRAW, STOP,<br>SELECT_TARGET, LEAVE_MATCH, RECONNECT|Intent only; authenticated identity and current server<br>state determine legality.|
|Server → Client authoritative events|QUEUE_STATE_CHANGED, MATCH_FOUND,<br>MATCH_CREATED, PLAYER_PRESENCE_CHANGED,<br>DECISION_REQUIRED, CARD_DRAWN,<br>SCORE_CHANGED, TURN_CHANGED,<br>FORFEIT_PENDING, MATCH_COMPLETED|Emitted only for committed versions; include<br>entity/match version and correlation IDs.|
|Server → Client snapshots|QUEUE_SNAPSHOT, CANDIDATE_SNAPSHOT,<br>MATCH_SNAPSHOT, TERMINAL_RESULT_SNAPSHOT|Full player-projected recovery state; excludes RNG<br>seed/future deck order/server-onlyaudit felds.|
|Error / rejection responses|UNAUTHORIZED, STALE_STATE, DECISION_MISMATCH,<br>ILLEGAL_ACTION, ILLEGAL_TARGET,<br>DEADLINE_EXPIRED, ALREADY_TERMINAL,<br>IDEMPOTENCY_KEY_REUSE, STALE_PRESENCE_EVENT,<br>RETRYABLE_AUTHORITY_UNAVAILABLE|Never mutate state; include command_id and<br>resync/version hint where useful.|
|Presence updates|APP_BACKGROUND / APP_FOREGROUND hints,<br>transport disconnect/reconnect, server presence<br>projection|Client lifecycle signals are authenticated hints;<br>authoritative presence transition/deadline is committed<br>bycoordinator/Match Actor.|



#### **27.4 Mobile Client Architectural Responsibilities** 

**<mark>Client owns Client does NOT own</mark>** Rendering, animations, audio, local input collection, navigation, reversible Deck order, card draw result, score, round/game state, legal-action authority, presentation state, secure token storage, connectivity indicators, display-only target legality, authoritative timers, seat assignment, matchmaking result, countdown interpolation, authoritative snapshot/event presentation. forfeit, winner/result, reconnect finality. 

#### **27.5 Offline behavior** 

- 13/31 MVP is not an offline-authoritative game. There is no local continuation of an active match while disconnected. 

- A queued or gameplay intent that cannot reach and be committed by the authoritative server is not treated as applied. The client retries the same command_id only when connectivity returns and the intent is still relevant. 

- Reconnect/resync is mandatory before new gameplay input. The client cannot “catch up” by locally applying assumed draws, timeout results or turns. 

- If the authoritative server reports a timeout, forfeit or terminal result that occurred while offline, the client presents that committed state without rollback. 

### **28. Security / Trust Boundaries** 

|**Threat / boundary**|**Enforcement**|
|---|---|
|Untrusted client<br>|Server computes all legal actions, card results, deck state, timers, seats, score,<br>forfeit and results.<br>|
|PLAYER_ID spoofng|PLAYER_ID comes from validated access token; raw client feld ignored for<br>authority.|
|Fabricated match_id|Authorization resolves roster membership from durable Match; non-members<br>rejected.<br>|
|Fabricated seat/score/card/deck input|Protocol never accepts these as authoritative mutation felds; server reads<br>canonical state.|
|Illegal target|Game Engine validates EFFECT_TARGET ACTIVE and exact DECISION_OWNER.|
|Replay / idempotency-key misuse|Unique (authenticated_player_id, command_id) + canonical<br>command_fngerprint. Same key/same fngerprint returns original outcome;<br>diferent fngerprint rejects IDEMPOTENCY_KEY_REUSE.|
|Stale transport event|Server-issued connection_epoch must equal authoritative<br>active_connection_epoch before a demotion can mutate presence/liveness/AFK<br>qualifcation.<br>|
|Malformed input|Versioned schema validation, bounded feld sizes/enums, reject<br>unknown/invalidpayloads before actor execution.|
|Command food|Per-connection and per-PLAYER_ID rate limits at gateway; actor mailbox<br>bounds; repeated invalid input metrics.|
|Stolen guest token|Refresh credential stored securely client-side, hashed server-side,<br>rotated/revocable; access tokens short-lived.|
|Redis compromise/loss|Redis contains no authoritative match/deck/result truth; Postgres rehydration<br>restores service.|



MVP does not require a full anti-cheat product. The principal anti-cheat boundary is server authority plus identity, authorization, legality validation, hidden deck/RNG state and replay protection. 

### **29. Data Model / Retention** 

#### **29.1 Logical persisted entities** 

|**Entity**|**Owner / lifetime**|**Purpose**|
|---|---|---|
|PlayerIdentity / GuestCredential|Identity; long-lived|Stable opaque PLAYER_ID and credential hash/rotation<br>metadata.|



13/31 | DRAFT FOR FINAL ARCHITECTURE APPROVAL | Page 21 

13/31 — Authoritative Multiplayer Technical Architecture v1.1 

|**Entity**|**Owner / lifetime**|**Purpose**|
|---|---|---|
|QueueEntry|Matchmaking; queue lifetime + short diagnostic<br>retention|Physical row + logical priority provenance:<br>queue_started_at, priority_creation_seq,<br>origin_queue_entry_id, liveness,<br>active_connection_epoch, reservation state.|
|CandidateSet + members|Matchmaking; acceptance + diagnostic retention|Reservation, member decisions, 10s deadline,<br>conversion outcome.|
|Match|Match Runtime; match + post-match index|Immutable metadata, lifecycle, versions, rules/RNG<br>versions and per-roster queue provenance needed for<br>qualifying pre-start auto-requeue.|
|MatchSnapshot|Match Runtime; active recovery; optional short post-<br>match retention|Complete authoritative active match aggregate.|
|MatchDeadline|Coordinator/Match Runtime; active until consumed +<br>short audit retention|Recoverable timers/grace/decision deadlines.|
|ProcessedCommand|Domain owner; bounded retention at least through<br>active match/candidate + retrywindow|command_id, canonical command_fngerprint, logical<br>outcome/resultingversion and causalityIDs.|
|MatchTransition|Match Runtime; diagnostic/audit retention|Append-only causal history, state hashes, RNG<br>counters, command/timer causes.|
|MatchResult|Result; durablepost-match|Immutable fnal competitive/no-contest result.|
|Outbox|Domain owner; untilpublished + short safetyretention|Reliablepost-commit eventpublication.|
|Telemetry|Observability;policy-dependent|Operational logs/metrics/traces; not source of truth.|



#### **29.2 Retention classification** 

|**Class**|**Must retain**|**Recommendedpolicy**|
|---|---|---|
|Required during active match|Snapshot, RNG/deck state, active deadlines,<br>processed commands, current transition audit,<br>player activity.|Never purge while match can resume.|
|Required after completion|MatchResult and immutable roster/seat/result<br>metadata.|Long-lived according to product/legal/privacy policy.|
|Diagnostic / temporary|Completed snapshots, transition audit, candidate<br>history, processed commands, outbox, verbose<br>logs.|ENGINEERING DEFAULT — TUNABLE: retain audit-<br>grade match transitions ~30 days and processed-<br>command history at least several days after terminal<br>result; fnalize exact durations with<br>privacy/operationspolicy.|



### **30. Observability** 

Observability must explain behavior without becoming a source of truth. All authoritative logs are structured and correlationfriendly. 

|**Signal**|**Required dimensions / examples**|
|---|---|
|Structured transition log|match_id, state_version_before/after, transition_id, input_type,<br>command_id/deadline_id, decision_id, owner lease_epoch, result code,<br>state_hash.|
|Presence/liveness|connection_epoch/current epoch, accepted/stale attach-demotion events,<br>disconnect/background/reconnect transitions, unavailability start, grace<br>deadline/expiry,queue liveness owner changes.|
|Gameplaytimers|action timeout,target timeout,decision owner,deadline,locked outcome.|
|AFK|decision_id, counts_toward_connected_afk start value, invalidation<br>version/reason,counter before/after,warningat 2, pending-forfeit trigger at 3.|
|Idempotency|command_id, fngerprint match/mismatch result, IDEMPOTENCY_KEY_REUSE<br>count;do not logsensitive fullpayload when not needed.|
|Auto-requeue|cancelled_match_id, failing player diagnostic ref, requeue source key, origin<br>prioritytuple and replacementqueue_entry_id for unafectedplayers.|
|Forfeit|trigger reason/player, pending version, safe-boundary detection, eligible survivor<br>count,WAIT entry/exit,fnal survivor/winner cardinality.|
|Cancellation/abort/result|admission failure, integrity abort, terminal type, fnalization reason,<br>result_validity,immutable result hash.|
|Gameplay diagnostics|NUMERICAL_DECK_DEADLOCK mandatory telemetry with round/card-zone<br>counts;efect stack depth and invalid-action counters.|
|Infrastructure metrics|active sockets, active matches/actors, queue depth/age, DB latency/errors,<br>lease takeovers,outbox lag,deadline scan lag,reconnect rate.|



#### **30.1 Privacy** 

PLAYER_ID is opaque but still treated as pseudonymous identifier. General logs should use a keyed/hash-derived diagnostic player reference where practical, with raw PLAYER_ID restricted to authorized support/audit contexts. Never log guest refresh credentials, access tokens, RNG seed or future deck order. 

### **31. Replay / Audit** 

##### **MVP recommendation** 

Require audit-grade transition history and deterministic engineering replay, not event-sourcing-based production recovery. Production recovery uses snapshots; audit/replay uses MatchTransition + initial RNG material + engine version to reproduce and explain the match. 

13/31 | DRAFT FOR FINAL ARCHITECTURE APPROVAL | Page 22 

13/31 — Authoritative Multiplayer Technical Architecture v1.1 

#### **31.1 Match X reconstruction** 

**1.** Load immutable match metadata: rules_engine_version, product_contract_version, roster/seat order, queue provenance and RNG seed reference plus rng_algorithm_version / bounded_sampling_algorithm_version / shuffle_algorithm_version. 

**2.** Read ordered MatchTransition rows by resulting state_version. 

**3.** For each row inspect authoritative input: accepted player command + command_fingerprint, timer expiry, presence/connection_epoch/forfeit/system event and command/deadline IDs. 

**4.** Replay deterministic engine inputs and RNG stream in a test/support tool. Compare each canonical gameplay state hash and RNG counter to recorded values. 

**5.** Inspect product-layer transitions for fenced presence generations, durable per-decision AFK qualification, pending forfeits, eligible-survivor gating and terminal claim. 

**6.** Compare computed final result hash with immutable MatchResult. 

Audit rows record only information needed to prove causality. Sensitive RNG seed material is access-controlled and not part of general telemetry. The audit log is append-only for the application role. 

### **32. Scaling** 

No business traffic forecast is assumed. Scaling is driven by independently measurable resources: concurrent sockets, active matches, queue coordination and PostgreSQL commit rate. 

|**Layer**|**Horizontal scaling model**|**Likely bottleneck / mitigation**<br>|
|---|---|---|
|Edge Gateway|Stateless instances behind load balancer;<br>connections distributed across instances.|Memory/fle descriptors and outbound bandwidth;<br>add instances. No state afinity required after<br>connection establishment.|
|Match Runtime|Many actors per worker; assign matches by<br>ownership lease/routing. Add workers to spread<br>active matches.|DB transaction latency and actor CPU; card game<br>transitions are lightweight.<br>|
|Matchmaking|Single writer per queue partition; standby<br>processes. Partition only when measured<br>contention demands it.|One hot global queue if trafic becomes very large;<br>future partition by region/ruleset while preserving<br>rules inside each confguredpool.|
|PostgreSQL|Primary handles writes; Multi-AZ for HA; read<br>replicas for support/analytics only.|Accepted transition write rate, audit/outbox volume,<br>connection count; use pooling, partition old audit<br>tables, tune indexes.|
|Redis-compatible cache|Cluster/replica as needed;fullyrebuildable.|Pub/sub fan-out and routinglookupvolume.|
|Telemetry|Async OTel export/batching.|High-cardinality logs; sampling for traces but never<br>sample required authoritative audit records.|



#### **32.1 Sizing assumptions** 

ASSUMPTION FOR SIZING ONLY: a 2–4 player card match has a small state footprint (hundreds of card/state fields, not megabytes) and human-paced command frequency. Capacity must be established by load tests that model reconnect bursts, effect chains and worst-case outbox/audit writes; no product concurrency target is inferred here. 

### **33. Deployment Topology** 

#### **33.1 MVP topology** 

```
Internet
  |
  v
AWS Application Load Balancer
  |
  +--> ECS Fargate: Edge Gateway Service (N replicas)
          | internal authenticated gRPC/HTTP2
          v
       ECS Fargate: Authoritative Game Backend (N replicas)
          |-- Identity / Session
          |-- Matchmaking Coordinator leader/standby
          |-- Match Runtime Actors
          |-- Deadline Scanner
          |-- Result Finalizer / Outbox
          |
          +--> RDS PostgreSQL Multi-AZ  [AUTHORITY]
          +--> ElastiCache/Valkey        [TRANSIENT]
          +--> OpenTelemetry Collector -> CloudWatch / traces
```

- Run across at least two availability zones; do not bind match correctness to one container instance. 

- Use managed database backups/PITR, TLS, encrypted storage and Secrets Manager/KMS for credentials. 

- No Kubernetes is required for MVP. ECS/Fargate provides container scheduling and rolling deployment with lower operational burden. 

13/31 | DRAFT FOR FINAL ARCHITECTURE APPROVAL | Page 23 

13/31 — Authoritative Multiplayer Technical Architecture v1.1 

- Rules-engine/product-contract versions are part of match metadata so rolling deployments can keep an existing match on compatible code; incompatible engine changes require side-by-side version support, not mid-match migration. 

#### **33.2 Future scale-out path** 

Scale Gateway and Game Backend replica counts independently. If queue coordination or blast radius later warrants it, Matchmaking and Match Runtime can become separate deployables without changing their module contracts. If multi-region is later required, matchmaking pool/region selection and match-home-region policy need an explicit product/operations decision; active-active mutation of one match remains forbidden. 

### **34. Technology Recommendations** 

#### **34.1 Mobile/client technology boundary** 

|**Option A**|**Option B**|**Recommendation**|**Reason**|**Trade-ofs**|
|---|---|---|---|---|
|Unity LTS / C#|Flutter or React Native UI app|Unity LTS + C#|Game-oriented<br>animation/tooling; same<br>language as backend/domain|Larger runtime/app footprint<br>and game-engine workfow<br>compared with UI frameworks.|
|**34.2 Backend runtm**|**e**||contracts; mature iOS/Android<br>game delivery.||
|**Option A**|**Option B**|**Recommendation**|**Reason**|**Trade-ofs**|
|.NET LTS / ASP.NET Core<br>**34.3 Realtme transp**|Go<br>**ort**|Current .NET LTS, C#|Strong typing for state<br>machines; excellent<br>async/networking; common<br>language with Unity; mature<br>PostgreSQL/OTel ecosystem.|Higher memory footprint than<br>Go; shared language does not<br>imply shared authority.|
|**Option A**|**Option B**|**Recommendation**|**Reason**|**Trade-ofs**|
|Raw/versioned WebSocket<br>|HTTP commands + polling /<br>|WSS + versioned Protobuf<br>|Full control over command IDs,<br>|Must implement<br>|
|protocol|proprietary realtime SDK|envelopes|versions, reconnect and<br>|connection/backpressure/reco<br>|
|**34.4 Primary persist**|**ence**||ordering; eficient on mobile.|nnectprotocol deliberately.|
|**Option A**|**Option B**|**Recommendation**|**Reason**|**Trade-ofs**|
|PostgreSQL|DynamoDB / key-value store|Managed PostgreSQL|Strong transactions/constraints<br>for queue reservations, dedupe,<br>result uniqueness, leases and|Write scaling is<br>vertical/partitioned before fully<br>horizontal; requires|
||||atomic snapshot/audit/outbox|schema/index discipline.|
|**34.5 Transient coord**|**inaton**||<br>commits.||
|**Option A**|**Option B**|**Recommendation**|**Reason**|**Trade-ofs**|
|No cache/pub-sub|Redis-compatible managed|Managed Redis-compatible|Useful for owner routing,|Additional service; all|
||cache|Valkey/Redis, transient only|connection registry, pub/sub|correctness must survive its|
||||and rate limits without risking|loss.|
|**34.6 Hostng / obser**|**vability**||<br>durable truth.||
|**Option A**|**Option B**|**Recommendation**|**Reason**|**Trade-ofs**|
|AWS ECS Fargate + RDS|Kubernetes/EKS|ECS Fargate + RDS PostgreSQL<br>Multi-AZ|Managed container lifecycle<br>and HA data without Kubernetes<br>|Less low-level scheduling<br>control; AWS coupling.|
||||operatingburden.||
|OpenTelemetry + CloudWatch|Vendor-specifc SDK only|OpenTelemetry SDK/Collector<br>exporting to CloudWatch<br>metrics/logs/traces|Portable instrumentation and<br>standardized correlation.|Collector confguration and<br>cost/cardinality tuning required.|



##### **Recommended MVP stack direction** 

Unity LTS/C# client; .NET LTS/ASP.NET Core backend; custom WSS Protobuf protocol; PostgreSQL authority; Redis-compatible transient coordination; AWS ECS Fargate + RDS Multi-AZ; OpenTelemetry + CloudWatch. The Game Engine is a pure C# library with versioned deterministic RNG. 

13/31 | DRAFT FOR FINAL ARCHITECTURE APPROVAL | Page 24 

13/31 — Authoritative Multiplayer Technical Architecture v1.1 

### **35. Managed Platform vs Custom Backend Analysis** 

|**Criterion**|**Custom authoritative backend**|**Managed multiplayer/backend**<br>**platform**<br>|**Hybrid managed infrastructure**|
|---|---|---|---|
|Deterministic card-game logic|Full control over exact<br>state/yield/atomicity semantics.|May require ftting logic into platform<br>room/functions model; hidden<br>retry/ownership semantics can<br>complicateguarantees.|Full application control while outsourcing<br>databases/containers/monitoring.|
|Custom matchmaking|Exact per-entry<br>age/intersection/reservation rules easy<br>to encode.|Platform matcher may not expose exact<br>queue-age precedence/reservation<br>semantics.|Custom matcher on managed<br>DB/compute.|
|Timers / reconnect / forfeit|Exact durable deadline and same-actor<br>ordering can be implemented explicitly.|Platform timers/reconnect lifecycle may<br>difer from locked contract and require<br>workarounds.|Custom semantics, managed reliability<br>primitives.|
|Operational burden|Highest if all infrastructure self-<br>managed.|Lowest initially.|Moderate/low with managed compute,<br>DB, cache and telemetry.|
|Vendor lock-in|Application portable at container/SQL<br>level.|High around proprietary session/match<br>APIs.|Moderate cloud coupling but domain<br>core remainsportable.|
|MVP speed|Good with only two services and<br>managed infrastructure.|Potentially fast until custom semantics<br>require bypasses.|Best balance for this ruleset.|
|Future scalability|Direct control over<br>sharding/ownership/data model.|Depends on platform constraints/pricing.|Scale application roles while managed<br>stores scale independently.|



##### **Decision** 

Build a custom authoritative 13/31 backend on managed cloud infrastructure. Do not outsource the authoritative match state machine or matchmaking semantics to a proprietary multiplayer platform for MVP. Managed services should supply compute, PostgreSQL, cache, load balancing, secrets and observability—not game truth. 

### **36. Testing Architecture** 

|**Test layer**|**Architecture requirement / explicit v1.1 coverage**<br>|
|---|---|
|Pure Game Engine unit tests|No DB/network/time dependencies; every locked state transition, card efect, LIFO<br>DRAW 2, Rule 13priority, tie-break, deadlock and versioned RNG API.|
|Deterministic replay / RNG tests|Golden vectors for identical seed+algorithm versions; bounds 2/3/4 and non-power-of-<br>two; deterministic Fisher–Yates; persisted RNG counter recovery; no committed<br>random consumption repeats after crash.<br>|
|State-machine transition tests|Illegal states/role confation rejected; explicit gameplay role invariants; safe-boundary<br>yields.|
|Matchmaking tests|Exact descending 4→3→2 evaluator. Required: 27/25/5s => 2p fallback; 27/15/12s =><br>3p; 30/30/8/2s => 4p; 5+ mixed => largest legal then oldest eligible and<br>priority_creation_seqtie-break.|
|Auto-requeue tests|After converted-match admission cancellation caused by peer: unafected player<br>receives exactly one replacement QueueEntry with original<br>queue_started_at/priority_creation_seq; original row stays CONSUMED; failing player<br>lobby; retryafter response loss creates no duplicate.|
|Timer boundary tests|Fake clock; action/target exactly-before/after deadline, restart with overdue deadline,<br>duplicate expirydeliveryand deadline-generation fencing.|
|Connected-AFK tests|Connected entire 20s => counts. Disconnect at second 19 => does not count.<br>Background then reconnect before timeout => remains false. Reconnect then manual<br>accepted decision resets counter. Crash/failover after false preserves false.<br>Duplicate/stalepresence cannot re-enablequalifcation.|
|Connection-epoch tests|CONNECTED epoch18 then DISCONNECTED epoch17 => NO_OP; epoch18<br>disconnect valid. Apply equivalent guard to queue liveness and admission/match<br>presence.|
|Idempotency tests|Same key/same canonical fngerprint returns previous outcome. Same key/diferent<br>fngerprint rejects IDEMPOTENCY_KEY_REUSE. Concurrent diferent fngerprints: one<br>claims key, other rejects.|
|Concurrency/race tests|Deterministic scheduler: command/timeout; disconnect/timeout; new<br>connection/stale disconnect; cancellation/JOIN; auto-requeue retry;<br>Leave/disconnect; reconnect/expiry; GAME_ENDED/forfeit.|
|Disconnect/reconnect tests|Gateway loss, app background, Wi-Fi→mobile generation change, snapshot resync,<br>post-timeout reconnect,post-forfeit reconnect, terminal reconnect.|
|Recovery tests|Kill runtime before/after commit, after AFK fag false, after RNG-consuming commit,<br>during lease renewal; verify fenced takeover and exact version/RNG/deadline/AFK<br>recovery.|
|Persistence failure tests|Transaction rollback at each write point; DB unavailable; candidate reservation races;<br>auto-requeue transaction failure; ProcessedCommand unique-key conficts; outbox<br>retry.|
|Multi-client integration|2/3/4 clients over WSS execute end-to-end matches with reconnects, mixed-age<br>queue fallback and terminal result verifcation.|
|Property/invariant tests|Card conservation, version monotonicity, one owner, candidate largest-legal property,<br>one key→one fngerprint, epoch monotonic fencing, AFK false-never-true, terminal<br>immutability, no multi-survivor winner,presence/gameplayseparation.|



#### **36.1 Testability design** 

Runtime code uses injectable IClock, deterministic RNG contract, ownership store, persistence repository and outbound publisher interfaces. The pure engine accepts deterministic state/input and owns no wall-clock/network side effects. Match actor and matchmaking tests run with virtual time and deterministic mailboxes; integration tests use real PostgreSQL 

13/31 | DRAFT FOR FINAL ARCHITECTURE APPROVAL | Page 25 

13/31 — Authoritative Multiplayer Technical Architecture v1.1 

constraints/transactions so fingerprint claims, queue replacement uniqueness and fencing behavior are exercised rather than mocked away. 

### **37. Architecture Invariants** 

|**ID**|**Architecture invariant**|**Enforcement**|
|---|---|---|
|ARCH-INV-001|One authoritative mutation owner per match.|DB lease + lease_epoch fencing; actor serialized<br>mailbox.|
|ARCH-INV-002|Client never determines random card/seat/starter<br>result.|Server-only RNG seed/state; engine generates<br>outcomes.|
|ARCH-INV-003|Everylogical command is applied at most once.|ProcessedCommand unique key+ same-TX outcome.|
|ARCH-INV-004|match_state_version increases monotonically for<br>committed match mutations.|Optimistic version predicate + single increment in<br>transaction.|
|ARCH-INV-005|Terminal result is immutable.|Unique MatchResult insert; lifecycle terminal guard; no<br>application updatepath.|
|ARCH-INV-006|Persistence precedes externally observable<br>authoritative success.|Commit-before-ack/broadcast + transactional outbox.|
|ARCH-INV-007|Timer expiry and client actions share same match<br>serialization domain.|TimerExpired is an internal actor mailbox input.|
|ARCH-INV-008|Presence state does not directly mutate gameplay<br>round_state.|Separate product state model; only engine mutates<br>round_state.|
|ARCH-INV-009|Client timestamps never resolve races.|Server deadline/ingress/commit order only.|
|ARCH-INV-010|A PLAYER_ID has at most one active queue/candidate<br>and one activegameplaymatch.|PlayerActivity row + unique constraints/row locks.|
|ARCH-INV-011|A queue entry cannot belong to two live candidate sets.|RESERVED state + locked transaction + candidate-<br>member uniqueness.|
|ARCH-INV-012|MATCH_ROSTER and seat order never change after<br>MATCH_CREATED.|Immutable stored roster; no update API; result<br>references same data.|
|ARCH-INV-013|Game Engine has no infrastructure dependency.|Pure library; no DB/socket/cloud/time APIs.|
|ARCH-INV-014|TURN_OWNER, DRAW_RECIPIENT, EFFECT_DRAWER,<br>DECISION_OWNER and EFFECT_TARGET remain distinct<br>felds.|Typed engine state + invariant tests.|
|ARCH-INV-015|Nested DRAW 2/efects resolve LIFO with independent<br>quotas.|Explicit context stack; engine tests.|
|ARCH-INV-016|Started efects cannot be rolled back due to<br>presence/forfeit.|Product layer waits for engine safe yield.|
|ARCH-INV-017|All authoritative deadlines surviveprocess restart.|Durable Deadline rows; recoveryscanner.|
|ARCH-INV-018|RNG cannot diverge after recovery.|Persist draw pile + PRNG state/counter and engine/RNG<br>versions in same snapshot.|
|ARCH-INV-019|FORFEIT_PENDING is irreversible once committed.|State transition validator; no reconnect-clearing path.|
|ARCH-INV-020|Forfeit fnalization happens only at explicit safe<br>boundary.|Engine boundary_kind/is_safe fag; host gate before next<br>progression.|
|ARCH-INV-021|Zero eligible survivors cannot create a winner.|Result fnalizer rejects; lifecycle enters<br>FORFEIT_RESOLUTION_WAIT.|
|ARCH-INV-022|2+ non-forfeited survivors cannot create a gameplay<br>winner.|Result fnalizer cardinality invariant => winner_set<br>empty.|
|ARCH-INV-023|Redis/cache loss cannot lose authoritative state.|No authoritative entities stored onlyin Redis.|
|ARCH-INV-024|Reconnect never rolls back committed<br>gameplay/timeouts/fnality.|Snapshot current version; no reverse transition paths.|
|ARCH-INV-025|No broadcast can expose an uncommitted authoritative<br>version.|Publisher accepts only committed outbox/version<br>references.|
|ARCH-INV-026|Audit can map every committed mutation to a cause.|Transition row links version to<br>command/deadline/system event IDs.|
|ARCH-INV-027|Candidate selection chooses the largest currently legal<br>group under every member’s per-entry eligibility before<br>anysmallergroup.|Descending N=4→3→2 evaluator; flter by<br>allowed_group_sizes; oldest eligible priority tuple.|
|ARCH-INV-028|Connected-AFK qualifcation is durable per decision and<br>cannot become eligible again after qualifying<br>unavailabilityoccurred in that window.|pending_decision.counts_toward_connected_afk<br>persisted; only true→false transition; timeout reads<br>committed fag.|
|ARCH-INV-029|A stale connection epoch cannot demote or overwrite a<br>newer authoritativepresencegeneration.|<br>Server-issued active_connection_epoch; demotion<br>CAS/actorguard requires equality; stale event NO_OP.|
|ARCH-INV-030|Qualifying unafected pre-start auto-requeue preserves<br>locked queue age/priority without resurrecting historical<br>CONSUMED state.|Immutable queue provenance in Match + atomic<br>replacement QueueEntry/PlayerActivity transaction +<br>unique requeue source.|
|ARCH-INV-031|One command_id maps to exactly one canonical logical<br>command fngerprint for an authenticatedplayer.|Unique (player_id, command_id) ProcessedCommand +<br>fngerprint compare; mismatch rejection.|
|ARCH-INV-032|All bounded authoritative random selections use<br>versioned bias-free uniform sampling.|Rejection-sampled NextUniformIntExclusive + versioned<br>Fisher–Yates;golden vectors and replaymetadata.|



### **38. ADR Register** 

|**ADR**|**Decision**|**Options**|**Recommendation**|**Rationale**|**Trade-ofs**|**Status**|
|---|---|---|---|---|---|---|
|ADR-001|Architecture style|Monolith / small SOA /<br>microservices|Small SOA: Edge Gateway<br>+ modular Game Backend|Separates socket scaling<br>from authoritative<br>runtime while retaining<br>simple business<br>transactions.|One internal service hop.|ACCEPTED|
|ADR-002|Authoritative execution|Locks/transactions /<br>actor / distributed events|Single-writer Match Actor|Matches one ordering<br>domain and deterministic<br>game semantics.|Requires ownership<br>routing/failover.|ACCEPTED|
|ADR-003|Persistence model|Snapshot / event sourcing<br>/ hybrid|Snapshot + append-only<br>audit log|Exact recovery is simple;<br>audit/replay retained<br>without event-sourcing<br>complexity.|More writes than<br>snapshot-only.|ACCEPTED|



13/31 | DRAFT FOR FINAL ARCHITECTURE APPROVAL | Page 26 

13/31 — Authoritative Multiplayer Technical Architecture v1.1 

|**ADR**|**Decision**|**Options**|**Recommendation**|**Rationale**|**Trade-ofs**|**Status**|
|---|---|---|---|---|---|---|
|ADR-004|Primary database|PostgreSQL / NoSQL KV|PostgreSQL|Strong transactions,<br>constraints, row locks<br>and unique result/dedupe<br>guarantees.|Primary write bottleneck<br>requires capacity<br>planning.|ACCEPTED|
|ADR-005|Match ownership|Sticky routing / Redis lock<br>/ DB fenced lease|PostgreSQL lease +<br>monotonic fencing epoch|Split-brain stale writes<br>are rejected by<br>authoritative store.|Lease renewal/claim<br>logic required.|ACCEPTED|
|ADR-006|Realtime|WebSocket / HTTP polling<br>/ proprietary SDK|Authenticated WSS|Low latency and explicit<br>control over<br>reconnect/order.|Connection operations<br>required.|ACCEPTED|
|ADR-007|State sync|Snapshots / events /<br>hybrid|Hybrid snapshot + events|Simple reconnect<br>correctness plus eficient<br>healthy updates.|Two message shapes to<br>support.|ACCEPTED|
|ADR-008|RNG|System random per<br>draw / seeded PRNG /<br>prebuilt deck only|Server-only deterministic<br>ChaCha20 stream +<br>versioned rejection-<br>sampled bounded<br>uniform API +<br>deterministic Fisher–<br>Yates; persist deck/RNG<br>state|Uniform locked random<br>operations, deterministic<br>replay and crash safety<br>without modulo bias.|Seed protection plus<br>RNG/bounded-sampler/s<br>hufle version<br>compatibility required.|ACCEPTED|
|ADR-009|Timer processing|Process timers only /<br>external scheduler only /<br>hybrid|Durable deadlines + local<br>timer + DB scanner|Low latency and restart<br>recovery; exactly-once<br>logical expiry.|Scanner/duplicate expiry<br>handling required.|ACCEPTED|
|ADR-010|Matchmaking<br>coordination|Concurrent DB workers /<br>single coordinator /<br>managed matcher|One fenced coordinator<br>per queue partition using<br>exact descending-size<br>legal candidate evaluator|Implements MP-DR-001<br>exactly while keeping<br>deterministic<br>reservations/order and<br>simple failover.|Potential hot partition at<br>very high scale; evaluator<br>must use preserved<br>priority provenance.|ACCEPTED|
|ADR-011|Transient coordination|None / Redis authoritative<br>/ Redis transient|Redis-compatible cache<br>transient only|Simplifes routing/pub-<br>sub without correctness<br>dependency.|Extra managed service.|ACCEPTED|
|ADR-012|Identity|Mandatory account /<br>persistent guest / device<br>ID trust|Server-issued persistent<br>guest PLAYER_ID +<br>refresh credential|Meets locked identity<br>without social login.|Identity can be lost on<br>reinstall by MVP scope.|ACCEPTED|
|ADR-013|Client prediction|Gameplay prediction / no<br>prediction|No speculative<br>authoritative gameplay<br>prediction|Card game latency<br>tolerates server round-<br>trip; eliminates rollback<br>complexity.|UI must wait for server for<br>truth.|ACCEPTED|
|ADR-014|Deployment|Kubernetes / ECS<br>Fargate / serverless<br>functions|AWS ECS Fargate + RDS<br>Multi-AZ|Long-lived<br>sockets/workers with low<br>ops burden.|AWS coupling; less low-<br>level control.|ACCEPTED|
|ADR-015|Managed multiplayer<br>platform|Custom / managed<br>platform / hybrid|Custom authoritative<br>backend on managed<br>infrastructure|<br>Exact custom<br>rules/timers/matchmakin<br>g need direct control;<br>managed primitives<br>reduce ops.|More application code<br>than turnkey platform.|ACCEPTED|



### **39. Risk Register** 

|**Risk ID**|**Risk**|**Probability**|**Impact**|**Mitigation**|**Residual risk**|
|---|---|---|---|---|---|
|R-001|Client/server state desync|Medium|High|Versioned events +<br>snapshot resync +<br>canonical projection<br>hashes; no client authority.|Short visual<br>correction/resync.|
|R-002|Duplicate commands from<br>mobile retries|High|High|command_id dedupe<br>committed with state;<br>semantic decision IDs.|Dedupe storage/retention<br>cost.|
|R-003|Split-brain match ownership|Low|Critical|DB-time lease + fencing<br>epoch on every write; stale<br>owner stops on zero-row<br>mutation.|Short failover interruption.|
|R-004|Timer drift / delayed scanner|Medium|High|Persist absolute deadlines;<br>local monotonic timers + DB<br>scanner; overdue deadlines<br>on recoveryfrst.|Expiry delivery may be<br>delayed, but logical<br>deadline remains exact.|
|R-005|Crash during commit|Medium|High|Single DB transaction;<br>commit status discovered<br>via snapshot/dedupe; no<br>pre-commit broadcast.|Uncommitted command<br>must retry.|
|R-006|Reconnect storm after<br>gateway/zone failure|Medium|High|Stateless gateway<br>horizontal scale, connection<br>backof/jitter, snapshot rate<br>limits, DBpooling.|Temporary latency spike.|
|R-007|Matchmaking contention|Low/Medium|Medium|Single coordinator per<br>partition; transactional<br>reservations; partition only<br>when measured.|One partition can become<br>throughput ceiling.|
|R-008|Database outage|Low|Critical|Multi-AZ managed Postgres,<br>connection pool failover, no<br>success without commit,<br>outbox/recovery.|During outage authoritative<br>progress unavailable.|
|R-009|Event ordering/fan-out gaps|Medium|Medium|match_state_version +<br>server_message_seq +<br>resync on gap; Redis not<br>authoritative.|Extra snapshots.|
|R-010|Stale client protocol/state|Medium|Medium|Versioned protocol, min<br>supported version,<br>decision_id, explicit<br>STALE/RESYNC.|May require forced client<br>update for incompatible<br>protocol.|
|R-011|Guest identity loss|Medium|Medium|Keychain/Keystore refresh<br>credential; clear messaging;<br>reinstall recovery explicitly<br>out of MVP.|Reinstall/device loss can<br>create new identity.|



13/31 | DRAFT FOR FINAL ARCHITECTURE APPROVAL | Page 27 

||||13/31 — Authoritative Multiplayer T|echnical Architecture v1.1|
|---|---|---|---|---|
|**Risk ID**|**Risk**|**Probability**|**Impact**<br>**Mitigation**|**Residual risk**|
|R-012|Telemetry gaps|Medium|Medium<br>Authoritative<br>MatchTransition stored in<br>Postgres separate from<br>sampled traces; monitor<br>exporter health.|Operational traces may still<br>be incomplete.|
|R-013|Rules-engine version drift<br>during deploy|Low|Critical<br>Persist engine version per<br>match; side-by-side<br>compatible code; never<br>migrate active match<br>semantics silently.|Deployment complexity for<br>breaking engine changes.|
|R-014|Redis loss/routing<br>inconsistency|Medium|Low/Medium<br>Treat as cache only; DB<br>ownership resolution<br>fallback; rebuild routing.|Temporary command<br>routing latency.|
|R-015|Stale old-connection<br>demotion after reconnect|Medium|High<br>Server-issued<br>connection_epoch and<br>equality guard at<br>authoritative owner; stale<br>events telemetry/no-op.|A newer attach that has not<br>committed yet does not<br>protect until it wins<br>ordering, by design.|
|R-016|Connected-AFK<br>misclassifcation after<br>crash/race|Low/Medium|High<br>Persist monotonic per-<br>decision qualifcation and<br>invalidation cause/version;<br>serialize presence vs<br>timeout.|Transport detection latency<br>still defnes when<br>authoritative unavailability<br>is observed.|
|R-017|Auto-requeue priority<br>loss/duplication|Low|High<br>Persist queue provenance in<br>converted match; one-TX<br>cancellation+replacement<br>row+PlayerActivity; unique<br>requeue source.|If DB unavailable,<br>cancellation/requeue<br>completion is delayed rather<br>than partially applied.|
|R-018|Idempotency key reused for<br>diferent payload|Medium|Medium/High<br>Canonical fngerprint bound<br>to unique<br>player+command_id;<br>mismatch rejection +<br>telemetry.|Malicious clients can still<br>generate many fresh keys;<br>rate limiting remains<br>needed.|
|R-019|Modulo-biased or version-<br>drifting RNG|Low|High<br>Versioned rejection<br>sampler, deterministic<br>Fisher–Yates, golden<br>vectors and persisted<br>algorithm versions/state.|Algorithm upgrades require<br>explicit new version<br>support.|



### **40. Failure Mode Register** 

|**Failure**|**Detection**|**Authoritative behavior**|**Recovery**|**Player impact**|
|---|---|---|---|---|
|Runtime actor crashes|Lease heartbeat/connection<br>loss/process health.|No uncommitted transition<br>becomes authoritative.|New fenced owner loads latest<br>snapshot/deadlines.|Brief reconnect/resync; committed<br>state retained.|
|DB transaction timeout/rollback|Database error/zero commit ack.|Command not acknowledged as<br>applied.|Retry with same command_id after<br>revalidation.|Possible temporary action<br>unavailability.|
|Committed state, response lost|Client timeout; processed<br>command exists.|State remains committed.|Retry returns original outcome;<br>snapshot resync.|No duplicate efect.|
|Outbox publish fails|Outbox retry count/lag metric.|No rollback; DB state stays<br>authoritative.|Retry publish; reconnect snapshot.|Realtime update delayed.|
|Redis unavailable|Cache health/routing misses.|No match/queue truth lost.|Resolve owner/state from DB;<br>degrade fan-out until cache<br>returns.|Higher latency / reconnects.|
|Gateway dies|Connection health/instance<br>termination.|Match actor continues; presence<br>transitions according to<br>connection loss.|Client reconnects another<br>gateway.|May enter unavailability grace.|
|Deadline scanner dies|Worker health + scan lag.|Deadlines remain ACTIVE in DB.|Replacement scans due rows;<br>actor validatesgeneration.|Expiry delivery delayed, logical due<br>timepreserved.|
|Old owner resumes after partition|Fencing predicate failure / lease<br>epoch mismatch.|All stale writes rejected.|Actor stops and drops ownership;<br>clients route to current owner.|Transient errors only.|
|Candidate coordinator crashes<br>mid-reservation|Transaction state/lease expiry.|Either full reservation committed<br>or none.|New coordinator reloads durable<br>candidate/queue states.|Match Found delivery may be<br>delayed.|
|All players unavailable in forfeit<br>wait|Grace expiries and eligible count.|No winner may be manufactured.|Mark expiries; when all remaining<br>pending, ABORTED_NO_CONTEST.|Match ends no contest.|
|Integrity/state hash mismatch|Replay/hash invariant or<br>impossible state validation.|Do not continue speculative<br>gameplay.|Freeze/diagnose; use locked<br>MATCH_ABORTED /<br>INTEGRITY_ABORT if authoritative<br>result cannot be trusted.|Players receive no-contest abort.|
|Stale disconnect from superseded<br>socket|event.connection_epoch <<br>active_connection_epoch.|NO_OP; no grace, queue<br>invalidation, AFK change or forfeit<br>trigger.|Drop stale event; retain newer<br>connection.|None.|
|AFK timeout races with disconnect|Both inputs arrive near deadline.|Actor commit order determines<br>qualifcation: disconnect frst fips<br>durable fag false; timeout frst<br>resolves usingstill-true fag.|Audit version ordering explains<br>outcome; no transport-log<br>reconstruction.|Deterministic warning/forfeit<br>behavior.|
|Admission cancellation + auto-<br>requeue TX fails|DB rollback/error.|Neither terminal cancellation<br>success nor replacement queues<br>are published/authoritative.|Retry same cancellation<br>fnalization; unique source makes<br>eventual replacement exactly<br>once.|Temporary pre-start resolution<br>delay.|
|Auto-requeue committed,<br>response lost|Client timeout but<br>terminal/requeue rows exist.|Committed state remains<br>authoritative.|Retry/read returns existing<br>replacement queue; no second<br>row.|Player may briefy wait for resync.|
|Concurrent same command_id,<br>diferent payload|ProcessedCommand unique-key<br>confict / fngerprint mismatch.|Only frst committed fngerprint<br>owns key; other rejects<br>IDEMPOTENCY_KEY_REUSE.|Return stable rejection and<br>telemetry; never run second<br>payload.|No duplicate/misapplied action.|
|Actor crashes after consuming<br>RNG in committed transition|New owner sees later<br>state_version and post-call RNG<br>counter.|Do not consume random values<br>again.|Load snapshot and<br>ProcessedCommand; replay/audit<br>usespinned algorithm versions.|No visible divergence.|



13/31 | DRAFT FOR FINAL ARCHITECTURE APPROVAL | Page 28 

13/31 — Authoritative Multiplayer Technical Architecture v1.1 

#### **40.1 Performance and SLO assumptions** 

The following are ENGINEERING TARGET — TUNABLE, not Product Owner commitments: 

|**Metric**|**Initial engineering target**|
|---|---|
|Accepted command server processing|p95 < 150 ms from Gateway receipt to durable commit under normal load,<br>excludingmobile network latency.|
|Committed event fan-out|p95 < 250 ms from commit to connected-client deliveryunder normal load.|
|Reconnect state recovery|p95 < 2 s from authenticated WSS reconnect to full snapshot delivery under<br>normal load.|
|Matchmaking evaluation cadence|Event-driven on queue change with a safety wake-up <= 500 ms around age<br>thresholds.|
|Deadline scanner lag|p99 < 250 ms under normal load;correctness uses due_at,not scan time.|
|Service availability|Engineering objective 99.9%+ monthly for production application path; exact SLA<br>requires operations/product approval.|



#### **40.2 Cost / operational complexity** 

|**Area**|**MVP footprint / cost driver**|
|---|---|
|Application compute|Two ECS services. Cost scales mainly with concurrent WebSockets and active<br>match worker replicas.<br>|
|Database|RDS PostgreSQL Multi-AZ is the primary fxed reliability cost and main per-<br>transition storage/IO driver.|
|Transient cache|Small managed Redis-compatible cluster for routing/pub-sub/rate limiting; can<br>start modest because not authoritative.|
|Telemetry|Log volume and high-cardinality traces can become material cost; keep audit<br>records in structured DB rows and tune trace sampling.|
|Operational burden|No Kubernetes, Kafka or full event-sourcing stack in MVP; on-call surface is<br>Gateway, Game Backend, Postgres, cache and observability.|



### **41. Sequence Diagrams** 

#### **A. PLAY → Matchmaking → Match Found → ACCEPT → MATCH_CREATED** 

Sequence A candidate evaluation uses the Section 17 descending legal evaluator (4 → 3 → 2), then oldest eligible priority; it may fall back to a smaller legal group when a larger group is illegal. 

13/31 | DRAFT FOR FINAL ARCHITECTURE APPROVAL | Page 29 



<!-- Start of picture text -->
5 Matchmaking stitial Match Runtimex<br>PLAY(command_id) |<br>i ee<br>Authenticate PLAYER_ID; route PLAY<br>C3 so<br>3 TX: create/keep>one QUEUE_ENTRY; preserve creation order |H<br>4 — Evaluate legal group sizes; reserve candidate H<br>TX: reserve members + CANDIDATE_SET + 10s deadline [COMMIT]<br>5<br>MATCH_FOUND(candidate_id, deadline)<br>6 —————————— Ee<br>Authoritative Match Found<br>th I<br>ACCEPT_MATCH(candidate_id, command_id) |<br>8 an a H<br>Route ACCEPT |<br>9 —<br>TX final accept: candidate +» MATCH; immutable roster; seat RNG; admission deadline [COMMIT] H<br>10 ee<br>Activate/claim durable match<br>ha $$<br>MATCH_CREATED(match_id, state_version) H<br>12 =<br>Join instructions / snapshot endpoint i<br>13 < H<br><!-- End of picture text -->



<!-- Start of picture text -->
——<br>ih oeJOIN_MATCH(match_id) H<br>Authenticated join intent<br>2 TT _—_—><br>TX: JOINED + IN_-MATCH_CONNECTED; derive READY [COMMIT v+1]<br>3 —_——<br>4 — When all READY: create 3s start deadline<br>TX: MATCH_READY > MATCH_STARTING; persist deadline [COMMIT v+1]<br>5 a<br>Broadcast READY/start countdown<br>6 ——ee<br>7 soe 3s deadline expires in same actor ordering domain<br>Initialize GAME_SETUP / authoritative RNG as required<br>8 jp "=<br>Deterministic initial gameplay state<br>9)<br>TX: MATCH_IN. PROGRESS + engine snapshot + timers [COMMIT v+1]<br>10 ——“=<br>Broadcast authoritative match start snapshot/events !<br>aby CO oo i<br>Render; client countdown was display-only !<br>12 It<br><!-- End of picture text -->



<!-- Start of picture text -->
Postaresat<br>DRAW(command_id, decision_id, expected_version)<br>1 ad<br>Authenticated intent; server ingress<br>2 sa<br>3 ee Dedup + deadline barrier + legality validation<br>Apply DRAW to cloned authoritative state<br>4 es<br>5 — Draw from server deck; resolve number/effect until next yield point<br>New state + domain events + boundary metadata<br>6 —<br>Single TX: snapshot v>v+1 + processed command+ transition + deadlines + outbox [COMMIT]<br>7 a<br>Commitisuccess<br>8 I<br>Publish committed event envelope only after commit<br>9 rhve aoaovovr'rvr——<br>Command result: APPLIED, new version<br>10 I<br>Authoritative state/event; no speculative card truth<br>pial a<br><!-- End of picture text -->



<!-- Start of picture text -->
ponery<br>During DRAW: effect card drawn<br>1 a<br>Yield WAITING_FOR_TARGET_SELECTION with stored EFFECT_CONTEXT + decision_id<br>2 I<br>TX: persist effect card removal, context stack, target deadline [COMMIT v+1]<br>3 ><br>TARGET_REQUIRED(decision_id, legal target projection, deadline)<br>4 <t<br>Prompt target selection<br>5 th<br>SELECT_TARGET(command_id, decision_id, target_player_id)<br>6 a<br>Authenticated intent<br>7 —_—_—_—_—_—_—:<br>Validate DECISION_OWNER/EFFECT_TARGET; apply effect; push nested DRAW_2 if needed<br>8 a<br>Resolve LIFO until next decision or safe yield<br>9 I<br>TX: snapshot + transition + new deadlines [COMMIT v+1]<br>10 $$ SSSSSSSSsS—SFsKm<br>Broadcast authoritative result<br>11 I<br>12 — If target timer won first: internal expiry command auto-self-targets instead<br><!-- End of picture text -->



<!-- Start of picture text -->
——e<br>Transport loss / background event<br>i a |<br>TX: presence unavailable + 60s grace deadline [COMMIT v+1] H<br>2 _<br>Existing 20s action deadline continues; no pause i<br>3 a<br>ACTION_TIMEOUT(deadline_id) H<br>4 H a '<br>TX: locked STOPPED+TIMEOUT via engine; AFK count only if fully connected window [COMMIT v+1]<br>5 a><br>Reconnect before 60s grace expiry H<br>6 Oo<br>RECONNECT same authenticated PLAYER_ID i<br>7 so H<br>TX: presence restored; clear grace if no FORFEIT_PENDING [COMMIT v+1] |<br>8 OOrvhv—m>s<br>Full projected snapshot at current version H<br>9) oc... I H<br>Resync; timeout remains committed; future legal play resumes H<br>10 a \<br><!-- End of picture text -->



<!-- Start of picture text -->
—<br>Player A becomes unavailable |<br>L sa i<br>TX: persist grace deadline T+60 [COMMIT]<br>2 aasaainn—OD<br>GRACE_EXPIRED(A, deadline_id)<br>3 —_—<—<X—n«="_—_"_"——<br>TX: A = irreversible FORFEIT_PENDING; preserve gameplay round_state [COMMIT v+1]<br>4 fs_p<br>If decision/effect pending: allow only locked completion to safe yield<br>5 ——<br>SAFE_BOUNDARY_REACHED; no new turn started<br>69<br>7= Compute ELIGIBLE_SURVIVOR<br>If eligible>=1: finalize forfeit; if 0: persist FORFEIT_RESOLUTION_WAIT [COMMIT]<br>8 issS3838388S98S909090 0 GG iiGiB<br>Publish committed pending/wait/result state H<br>EL} 030.0000|<br><!-- End of picture text -->



<!-- Start of picture text -->
Client B Result Finalizer PostgreSQL<br>State already FORFEIT_RESOLUTION:WAIT; B/C grace deadlines continue<br>1 _—Se<br>Reconnect before B grace expiry<br>= a<br>Authenticated same PLAYER_ID B<br>z} ss<br>TX: B presence IN_MATCH_CONNECTED [COMMIT v+1]<br>4a<br>5—— Recompute ELIGIBLE_SURVIVOR: B qualifies<br>Build FORFEIT_COMPLETION from non-forfeited ‘roster<br>6 Oo<br>survivor_set may include still-unavailable non-forfeited C; winner only if exactly 1 survivor<br>t I<br>TX: immutable MatchResult + MATCH_COMPLETED [COMMIT v+1]<br>8 a<br>Publish terminal snapshot/result<br>‘) I<br>Result view only after terminal commit<br>10<br><!-- End of picture text -->



<!-- Start of picture text -->
Recut Finalizer —<br>GAME_ENDED with locked unique gameplay winner<br>L sa<br>2 —— Check no earlier FORFEIT_PENDING terminal claim owns path<br>Construct NORMAL_COMPLETION result<br>3 _—_————'*s<br>Validate winner_set=1, survivor_set=empty, final scores, validity<br>4 A<br>Single TX::immutable MatchResult + MATCH COMPLETED + transition + outbox: [COMMIT]<br>5 ——<br>Commit success; unique(match_id) prevents duplicate finalization<br>6 9<br>Publish terminal event after commit<br>7 SUE<br>Authoritative final result<br>8 —<br>Late/reconnect inputs rejected as terminal; no rollback<br>9 ee eee eee<br><!-- End of picture text -->



<!-- Start of picture text -->
Postgres Same engine<br>DRAW(command_id=X)<br>ut oo<br>Route X<br>2 ——_e'=s<br>Validate and apply once<br>3 oO—WWNN—'B_<br>TX: snapshot v+1 + processed_command(X) + stored outcome [COMMIT]<br>4 a<br>Response lost due network<br>5: I<br>Retry DRAW(command_id=X) after reconnect<br>6 a<br>Route X<br>7 ——————<br>Lookup dedupe key X — existing committed outcome<br>8 snal<br>Return original result/version; no engine invocation<br>9<br>ALREADY_APPLIED + current/resync hint<br>10 I<br>No second card draw<br>11 It<br><!-- End of picture text -->



<!-- Start of picture text -->
biliaidiliie<br>Own match with lease_epoch=7; commit snapshot version N<br>1 ee<br>2 — Process crashes before/after broadcast<br>Connection/event stream may break; client retries/reconnects<br>3 —<br>Claim expired lease using DB time; increment epoch 7-8 [COMMIT]<br>4 rs<br>Load latest snapshot N, processed commands, active deadlines, RNG state<br>5 —__—_—_—_—_—_—_———_—><br>6 a Recreate actor; schedule future deadlines; enqueue already-due expiries first<br>Any stale late write with epoch=7 is rejected by fencing condition<br>a sa<br>Reconnect same PLAYER_ID<br>8 2.<br>Route to current owner epoch=8<br>) I<br>Full authoritative snapshot version >=N; continue without rollback<br>10 FF TF SSOSOSOaI_OwNP><br><!-- End of picture text -->

|**Requirement**|13/31<br>**Source**|— Authoritative Multiplayer Technical Architecture v1.1<br>**Architecture mechanism**|
|---|---|---|
|Uniform random seats / immutable ring|Digital §2.1|Server RNG at candidate→match conversion; immutable<br>roster/seat columns and snapshot.|
|Round 1 starter / deck shufle / reshufle|Digital §§2–3|Versioned ChaCha20 stream + rejection-sampled<br>NextUniformIntExclusive + deterministic Fisher–Yates;<br>persisted RNG/deck state.<br>|
|Distinctgameplayroles|Digital §1.1|Typed engine state felds; no inferred aliasing.|
|Nested DRAW 2 LIFO|Digital §§5.2, 15|Explicit DRAW_CONTEXT/EFFECT_CONTEXT stack in<br>pure engine.|
|Efect atomicity|Digital §15.5|Engine completes started context before safe boundary;<br>product forfeit host waits.|
|20s action timeout|Product §9 + Digital DR-005A|Durable action deadline -> internal timer input in match<br>actor -> engine STOPPED+TIMEOUT.|
|10s target timeout|Product §9 + Digital DR-005B|Durable target deadline -> same actor -> self-target<br>input.|
|60s reconnect grace / no pause|Product §§9–12|Durable player grace deadline; presence separate;<br>gameplaytimers unchanged.|
|Queue 10s/30s liveness precedence|Product §5.1/§9|QueueEntry efective deadline state owned by<br>coordinator; min-deadline transitions.|
|Match Found 10s explicit accept|Product §6|CandidateSet acceptance states + durable deadline +<br>no auto-accept.|
|30s admission / 3s start|Product §§7–9|Match Actor admission deadline, derived READY,<br>persisted start deadline.|
|Largest legal dynamic matchmaking candidate|Product §5 / MP-DR-001|General descending-size evaluator 4→3→2; flter each<br>size by every entry’s allowed_group_sizes, then oldest<br>eligible by preserved stablepriority.|
|Stable PLAYER_ID / guest allowed|Product §23|Server-issued guest identity + secure refresh credential<br>+ token auth.|
|One activequeue/match|Product §3/§23|PlayerActivityunique invariant and transaction locks.|
|Connected AFK only when connected entire decision|Product §13|Durable<br>pending_decision.counts_toward_connected_afk; any<br>current-epoch unavailability fips true→false<br>permanentlyfor that decision; timeout reads fag.|
|Reconnect/presence correctness|Product §§10–12, §24|Server-issued connection_epoch fencing; stale older<br>transport demotions are authoritative NO_OPs.|
|Unafected pre-start priority preservation|Product §§6–8, §21|Immutable queue provenance copied into Match;<br>atomic cancellation + new QueueEntry + PlayerActivity<br>transactionpreserves original age/tiepriority.|
|At-most-once logical intent|Architecture requirement / product race fnality|ProcessedCommand unique player+command_id plus<br>canonical fngerprint; same payload returns old<br>outcome, diferentpayload rejects.|
|Confrmed Leave irreversiblepending<br>|Product §14|LEAVE_MATCH actor transition; no reversepath.|
|Forfeit safe-boundary fnalization|Product §16.1; Digital atomicity|Engine explicit SAFE_POST_RESOLUTION yield before<br>nextprogression.|
|ELIGIBLE_SURVIVOR / WAIT|Product §16.1|Derived eligible set; MATCH lifecycle<br>FORFEIT_RESOLUTION_WAIT; only presence/grace<br>inputs accepted.|
|1 survivor winner / 2+ no winner|Product §16.2/§19|Result Finalizer cardinalityinvariants.|
|Terminal result immutable|Product §19/§24|Unique single transaction MatchResult insert + terminal<br>lifecycle + no update.|
|Numerical deck deadlock telemetry|Digital §3.3|Engine emits mandatory domain/telemetry event;<br>transition audit logs card-zone counts.|
|MVP public solo only|Product §1/§30/§31|No party/private/friend/spectator/rematch/surrender<br>modules or architecture requirements.|



### **44. Implementation Handoff** 

Recommended implementation order minimizes the chance that networking/product flows are built on an unstable authority core. 

|**Order**|**Implementation unit**|**Dependencies**|**Handof outcome**|
|---|---|---|---|
|1|Domain contracts + deterministic Game<br>Engine|None|Implement gameplay state/context<br>stack, engine yields, versioned<br>ChaCha20 RNG abstraction, bias-free<br>NextUniformIntExclusive, deterministic<br>Fisher–Yates andgolden vectors.|
|2|PostgreSQL schema + repository<br>transaction primitives|1 domain model|MatchSnapshot (including pending-<br>decision AFK fag and queue<br>provenance), MatchTransition,<br>ProcessedCommand+fngerprint,<br>Deadline, MatchResult, lease/fencing,<br>outbox, auto-requeue source<br>uniqueness.|
|3|Match Actor runtime + commit pipeline|1–2|Serialized mailbox, versioning, fngerprint<br>dedupe, engine adapter, commit-before-<br>publish, recovery/failover.|
|4|Timer/deadline subsystem|2–3|Durable deadline<br>create/cancel/consume, local<br>scheduling, scanner, fake-clock tests;<br>timeout reads durable AFKqualifcation.|
|5|Identity/session + PlayerActivity +<br>connection epochs|2|Guest bootstrap, refresh/access tokens,<br>activity uniqueness, monotonic server-<br>issued connection_epoch allocation.|



13/31 | DRAFT FOR FINAL ARCHITECTURE APPROVAL | Page 40 

13/31 — Authoritative Multiplayer Technical Architecture v1.1 

|**Order**|**Implementation unit**|**Dependencies**|**Handof outcome**|
|---|---|---|---|
|6|Edge Gateway / protocol|3,5|WSS auth, command<br>envelope/fngerprint canonicalization<br>contract, epoch-tagged transport signals,<br>routing, backpressure,<br>snapshots/events, rate limits.|
|7|Exact Matchmaking Coordinator|2,4,5|QUEUE_ENTRY provenance felds,<br>liveness epoch fencing, descending<br>4→3→2 legal evaluator, oldest eligible<br>priority, reservations and pre-conversion<br>requeue.|
|8|Candidate conversion / admission / auto-<br>requeue|3–7|Atomic candidate→match with queue<br>provenance; 30s admission; derived<br>READY; 3s start; one-TX peer-<br>cancellation replacement QueueEntries<br>withpreservedpriority.|
|9|Presence / reconnect|3–6,8|Current-epoch attach/demote guards,<br>60s grace, reconnect sequence,<br>snapshot resync; stale old-socket events<br>no-op.|
|10|Durable connected-AFK + Leave / forfeit<br>orchestration|3–4,9|Per-decision<br>counts_toward_connected_afk<br>true→false metadata, counter/reset<br>semantics, pending triggers, safe-<br>boundary interception, eligible-survivor<br>wait.|
|11|Result fnalizer / terminal immutability|3,10|Locked result schema/invariants, unique<br>terminal commit.|
|12|Observability / replay tooling|1–11|Structured transition audit including<br>fngerprints, connection epochs, AFK<br>invalidation and RNG algorithm<br>versions/state hashes.|
|13|Correction-focused integration/recovery<br>suite + deployment|All|Mixed-age matchmaking fallback, epoch<br>races, AFK crash recovery, auto-requeue<br>retry, idempotency mismatch race, RNG<br>crash recovery, plus<br>multi-client/load/chaos tests.|



#### **44.1 Definition of Done answers** 

|**Question**|**Unambiguous answer**|
|---|---|
|Authority|PostgreSQL-backed domain state owned by Matchmaking Coordinator before<br>match creation and a single fenced Match Actor after MATCH_CREATED.|
|Mutation|Only current domain owner commits state; Game Engine/Result Finalizer<br>compute but do notpersist directly.|
|Concurrency|Per-match actor mailbox; per-queue-partition coordinator; timers/internal<br>events use same orderingdomain.<br>|
|Retry|Unique player+command_id is bound to canonical fngerprint; same fngerprint<br>returns stored outcome, mismatch rejects; row commits with transition.|
|Persistence|Full authoritative snapshot + version + RNG/deadlines/dedupe commit<br>atomically.|
|Recovery|New fenced owner loads snapshot and active deadlines; uncommitted work is<br>retried.|
|Timers|Durable due_at records; local scheduling + DB scanner; expiry idempotent and<br>serialized.|
|Reconnect|Authenticate same PLAYER_ID, allocate newer server connection_epoch, actor<br>commits current generation, resync exact snapshot, then allow legal input; stale<br>old events cannot demote.|
|Randomness|Server-only deterministic versioned PRNG with bias-free bounded<br>sampler/Fisher–Yates; outcomes, algorithm versions and PRNG state<br>durable/auditable.|
|Matchmaking|Single coordinator + exact descending legal evaluator + row reservations +<br>PlayerActivityuniqueness +preservedqueuepriority provenance.|
|Finality|Unique immutable MatchResult inserted in the terminal transaction; late events<br>read terminal state only.|
|Observability|MatchTransition and structured telemetry correlate command/deadline IDs to<br>state versions and result.|
|Testing|Pure engine, fake clock, deterministic mailbox, real Postgres integration and<br>recovery/chaos tests.|



### **45. Remaining Architecture Decisions** 

No ARCHITECTURE-CONSTRAINT CONFLICT was identified. The locked gameplay and product contracts are technically implementable with the architecture above. No player-visible product rule needs to be changed to build the MVP. 

|**ID**|**Decision**|**Status**|**Recommended default / impact**|
|---|---|---|---|
|ARCH-DR-001|Production launch region / latency<br>geography|NON-BLOCKING / CAN BE DEFERRED|MVP architecture assumes one logical<br>home region deployed Multi-AZ. Final<br>cloud region is an operations/release<br>choice; does not change match rules.|



13/31 | DRAFT FOR FINAL ARCHITECTURE APPROVAL | Page 41 

13/31 — Authoritative Multiplayer Technical Architecture v1.1 

|**ID**|**Decision**|**Status**|**Recommended default / impact**|
|---|---|---|---|
|ARCH-DR-002|Diagnostic/audit retention durations|NON-BLOCKING / CAN BE DEFERRED|Use ~30-day transition audit as<br>engineering default until<br>privacy/legal/operations policy sets<br>exact retention. MatchResult remains<br>durableperproduct datapolicy.|
|ARCH-DR-003|WebSocket heartbeat / silent-link<br>detection thresholds|NON-BLOCKING / CAN BE DEFERRED|Initial engineering target: 5s ping /<br>~10s silent-link detection. This only<br>determines when transport<br>unavailability is detected; the locked<br>60s grace begins at the committed<br>unavailable transition.|
|ARCH-DR-004|Exact service SLO / capacity target|NON-BLOCKING / CAN BE DEFERRED|Use Section 40 engineering targets for<br>implementation/load testing; formal<br>business SLA requires<br>product/operations approval.|



None of these decisions blocks implementation of the authority model, game engine, persistence, matchmaking, reconnect, forfeit or result-finalization semantics. 

### **Architecture Consistency Audit** 

|**Audit item**|**Result**|**Evidence**|
|---|---|---|
|Gameplay contract unchanged|PASS|Locked gameplay states/roles/LIFO/efect<br>atomicity/card/scoring/turn/round/tie-break<br>behavior are unchanged.|
|Multiplayer product contract unchanged|PASS|Revision only strengthens implementation<br>correctness; all product timers, eligibility, presence,<br>AFK, reconnect, Leave, forfeit and result semantics<br>remain locked.|
|Exact matchmaking eligibility|PASS|Candidate evaluator checks N=4→3→2, flters by<br>each entry’s allowed_group_sizes, then oldest<br>eligible stablepriority.|
|Smaller legal fallback|PASS|27s/25s/5s forms legal 2p after 3p is illegal;<br>architecture does not wait incorrectly.|
|Connected-AFK durability|PASS|Per-decision qualifcation is persisted; current-<br>epoch disconnect/background permanently fips<br>true→false for that decision and survives failover.|
|Presence fencing|PASS|Server-issued connection_epoch; stale old-epoch<br>demotion events are no-op and cannot start<br>grace/change AFK/queue state.|
|Pre-start auto-requeue priority|PASS|Converted match stores immutable queue<br>provenance; peer-caused cancellation atomically<br>creates one new QueueEntry with original age/tie<br>priority; consumed historyremains consumed.|
|Idempotency same key/same payload|PASS|Canonical fngerprint match returns original<br>committed outcome without execution.|
|Idempotency same key/diferent payload|PASS|Fingerprint mismatch returns<br>IDEMPOTENCY_KEY_REUSE; unique key allows one<br>canonicalpayload only.|
|Uniform RNG|PASS|All bounded authoritative selections use versioned<br>rejection sampling; Fisher–Yates and RNG versions<br>are replay-pinned; no naive modulo bias.|
|Server authoritative|PASS|Clients send intents only; all authoritative<br>state/RNG/deadlines/results are server-owned.|
|One-match-one-owner invariant|PASS|Single Match Actor plus database lease_epoch<br>fencing.|
|Timer and command races serialized|PASS|All match timers, commands, presence and<br>terminal claims enter the same actor ordering<br>domain; queue races share one coordinator.|
|Commit before publish|PASS|Snapshot/dedupe/deadline/audit/outbox<br>transaction commits before authoritative ack/event<br>publication.|
|Crash recovery exact|PASS|Snapshot includes deck/RNG versions+counter,<br>deadlines, dedupe, pending-decision AFK metadata,<br>presencegeneration and forfeit state.|
|Reconnect causes no rollback|PASS|Reconnect projects current committed state;<br>irreversible timeout/forfeit/terminal state remains.|
|Terminal results immutable|PASS|Unique result insert in terminal commit; no ordinary<br>updatepath.|
|Presence/gameplay separation|PASS|Presence/connection generation/AFK are product<br>state; gameplay round_state changes only through<br>Game Engine.|
|Forfeit safe-boundary semantics|PASS|Explicit engine SAFE_POST_RESOLUTION yield<br>before new progression; product forfeit never<br>interrupts started efect/decision.|
|ELIGIBLE_SURVIVOR / FORFEIT_RESOLUTION_WAIT|PASS|<br>Derived eligibility and frozen WAIT lifecycle with<br>continued independent grace processing are<br>preserved.|



13/31 | DRAFT FOR FINAL ARCHITECTURE APPROVAL | Page 42 

13/31 — Authoritative Multiplayer Technical Architecture v1.1 

|**Audit item**|**Result**|**Evidence**|
|---|---|---|
|MVP scope discipline|PASS|No private rooms, parties, friends/invites,<br>spectating, rematch, surrender, Ranked/MMR or<br>mandatorysocial login added.|
|No blocking architecture ambiguity|PASS|Correction mechanisms are explicit implementation<br>contracts; remaining ARCH-DR items are non-<br>blockingoperational tunables.|
|Final render visually checked page-by-page|PASS|v1.1 shipping render was re-generated after layout<br>corrections and every page was inspected for<br>blank/orphan pages, split captions/callouts, table<br>clipping,overlaps and version/status consistency.|



##### **Final architecture status** 

DRAFT FOR FINAL ARCHITECTURE APPROVAL. Recommended for review by Software/Game Engineer (implementability), Systems Game Designer (gameplay contract consistency), and Product Manager / Multiplayer Product Designer (multiplayer contract consistency). No ARCHITECTURE-CONSTRAINT CONFLICT / DECISION REQUIRED is open. 

13/31 | DRAFT FOR FINAL ARCHITECTURE APPROVAL | Page 43 

