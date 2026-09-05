13/31 — Multiplayer Product & Match Rules Specification v1.1 

# **13/31** 

## **Multiplayer Product & Match Rules Specification v1.1** 

Mobile Full Online Multiplayer — iOS / Android 

#### **Document status: DRAFT FOR FINAL APPROVAL** 

**Primary owner: Product Manager / Multiplayer Product Designer** Required reviewers: Systems Game Designer; Software Architect / Tech Lead 

Revision date: 5 September 2026 

13/31 | Multiplayer Product Design | DRAFT FOR FINAL APPROVAL | Page 1 

13/31 — Multiplayer Product & Match Rules Specification v1.1 

### **0. Document Control, Authority, and Normative Language** 

This specification defines the authoritative multiplayer product behavior around the locked 13/31 gameplay system. It governs the online lifecycle from PLAY through final match result. It does not redefine card mechanics, scoring, turn/effect resolution, round states, or tie-break logic locked by the Digital Game Rules Specification v1.1. 

|**Source**|**Authority**||**Use**|
|---|---|---|---|
|13/31 — Official Rules|Upstream tabletop so|urce of truth|Original game mechanics, scoring, round and<br>game end.|
|13/31 — Digital Game Rules Specification v1.1|APPROVED / LOCKE|D gameplay contract|Authoritative digital gameplay states, role<br>separation, timeout outcomes, effect atomicity,<br>round/game end and tie-break.|
|13/31 — Multiplayer Product & Match Rules<br>Specification v1.0|Revision baseline||Existing multiplayer product model reviewed as<br>PASS WITH REQUIRED REVISIONS.|
|Revision Request 02|Product Owner appro<br>scope|ved mandatory revision|Approves all listed product decisions and requires<br>the v1.1 consistency/race corrections below.|
|**Term**||**Meaning**||
|MUST / SHALL||Normative product beh|avior.|
|MUST NOT / SHALL NOT||Forbidden product beh|avior.|
|CONFIRMED||Directly inherited from|a locked source.|
|DERIVED||Deterministic consequ<br>choice.|ence of locked/approved rules without new product|
|APPROVED / CLOSED||Product Owner decisio|n integrated as normative v1.1 behavior.|
|APPROVED — OUT OF MVP||Explicitly excluded fro|m MVP by Product Owner.|
|NON-BLOCKING FOR TASK 03||Future product depend|ency that does not block MVP technical architecture.|



##### **v1.1 approval posture** 

##### Status: **DRAFT FOR FINAL APPROVAL** 

Revision Request 02 is Product Owner approval for MP-DR-001 through MP-DR-018 as listed in Section 25. No product Decision Required item remains open for Task 03 MVP architecture. 

#### **0.1 Revision Summary v1.0 → v1.1** 

|**Change ID**|**Section**|**Change**|**Reason**|**Status**|
|---|---|---|---|---|
|MP-CH-001|16, 19|Replaced v1.0 multi-survivor<br>winner assignment with the<br>winner_set/survivor_set model.|PO rejection of the v1.0 multi-<br>survivor winner assignment.|APPROVED / CLOSED|
|MP-CH-002|2, 16, 19, 22|Added survivor_set as explicit<br>final-result field with one<br>consistent schema convention.|Required result-model<br>normalization.|APPROVED / CLOSED|
|MP-CH-003|2, 4, 16, 24|Added ELIGIBLE_SURVIVOR<br>and<br>FORFEIT_RESOLUTION_WAIT<br>finalization gate.|Prevents winner creation when<br>all other players are unavailable.|APPROVED / CLOSED|
|MP-CH-004|16, 24, 26|Formalized staggered reconnect-<br>grace expiries and multiple<br>pending forfeits.|Race-condition closure.|APPROVED / CLOSED|
|MP-CH-005|5|Formalized per-entry<br>allowed_group_sizes and<br>candidate-set intersection<br>legality.|Deterministic dynamic<br>matchmaking.|APPROVED / CLOSED|
|MP-CH-006|23|Added stable persistent opaque<br>PLAYER_ID identity model.|Reconnect/app relaunch identity<br>requirement.|APPROVED / CLOSED|
|MP-CH-007|1, 5|Locked MVP access to public<br>solo matchmaking;<br>private/friends/parties/spectating<br>excluded.|Explicit MVP scope.|APPROVED / CLOSED|
|MP-CH-008|5, 9, 24|Defined queue<br>background/network grace<br>ownership and deadline<br>precedence.|Queue liveness race closure.|APPROVED / CLOSED|
|MP-CH-009|25|Converted all approved v1.0<br>recommendations to APPROVED<br>/ CLOSED; Surrender/Rematch<br>to APPROVED — OUT OF MVP.|Revision Request 02 Product<br>Owner approval.|APPROVED / CLOSED|
|MP-CH-010|27, 30, 31|Separated MVP-required<br>architecture inputs from future<br>dependencies and added final<br>consistency audit.|Task 03 handoff readiness.|APPROVED / CLOSED|



13/31 | Multiplayer Product Design | DRAFT FOR FINAL APPROVAL | Page 2 

13/31 — Multiplayer Product & Match Rules Specification v1.1 

#### **0.2 Executive Decision Summary** 

|**ID**|**Topic**|**Normative v1.1 rule**|**Status**|
|---|---|---|---|
|MP-DR-001|Dynamic matchmaking eligibility|Per-entry group-size eligibility; largest<br>legal group; oldest entries first.|APPROVED / CLOSED|
|MP-DR-002|Match Found acceptance|Explicit ACCEPT/DECLINE, 10s, no auto-<br>accept.|APPROVED / CLOSED|
|MP-DR-003|Ready/start|No READY action;<br>JOINED+CONNECTED => READY; 3s<br>auto-start.|APPROVED / CLOSED|
|MP-DR-004|Admission|30s shared join/load window; any failure<br>=> cancel/no contest.|APPROVED / CLOSED|
|MP-DR-005|Gameplay timer durations|20s normal action; 10s effect target;<br>locked expiry outcomes unchanged.|APPROVED / CLOSED|
|MP-DR-006 / DR-005C|Reconnect grace|60s continuous unavailability; no global<br>pause; decision timers continue.|APPROVED / CLOSED|
|MP-DR-007|Background/suspend|TEMPORARILY_UNAVAILABLE; same<br>60s in-match grace.|APPROVED / CLOSED|
|MP-DR-008|Connected AFK|Warning at 2; 3 consecutive connected<br>decision timeouts =><br>FORFEIT_PENDING.|APPROVED / CLOSED|
|MP-DR-009|Intentional Leave|Confirmed Leave => irreversible<br>FORFEIT_PENDING; no grace.|APPROVED / CLOSED|
|MP-DR-010|Surrender|No separate Surrender action.|APPROVED — OUT OF MVP|
|MP-DR-011 / DR-005D|Forfeit continuation|Finalized player forfeit ends whole<br>gameplay match; no reduced<br>roster/backfill.|APPROVED / CLOSED|
|MP-DR-012|Forfeit result model|1 survivor => winner; 2+ survivors => no<br>winner, administrative result.|APPROVED / CLOSED|
|MP-DR-013|Eligible survivor gate|Zero eligible survivors =><br>FORFEIT_RESOLUTION_WAIT; all<br>expire => abort/no contest.|APPROVED / CLOSED|
|MP-DR-014|Rematch|No rematch handshake in MVP.|APPROVED — OUT OF MVP|
|MP-DR-015|Post-match re-entry|Real match terminal result =><br>lobby/manual PLAY; limited pre-start auto-<br>requeue only.|APPROVED / CLOSED|
|MP-DR-016|Queue liveness|30s background grace; 10s foreground<br>disconnect; deterministic precedence.|APPROVED / CLOSED|
|MP-DR-017|Player identity|Stable persistent opaque PLAYER_ID;<br>persistent guest identity allowed.|APPROVED / CLOSED|
|MP-DR-018|MVP match access|Public solo matchmaking only;<br>private/friends/parties/spectating<br>excluded.|APPROVED / CLOSED|



### **1. Multiplayer Product Scope** 

MVP is a synchronous public online multiplayer product for individual players on iOS and Android. The product matches exactly 2–4 players into one immutable gameplay roster and runs the locked 13/31 game until normal completion, forfeit completion, or no-contest termination. 

|**Area**|**MVP status**|**Product rule**|
|---|---|---|
|Public solo matchmaking|IN MVP|Each player enters individually; no premade group<br>semantics.|
|2–4 player matches|IN MVP|Dynamic candidate sizes governed by Section 5.|
|Match Found acceptance|IN MVP|Explicit 10s ACCEPT/DECLINE gate.|
|Reconnect to existing match|IN MVP|Stable PLAYER_ID + 60s in-match grace.|
|AFK/Leave/Forfeit|IN MVP|Authoritative match-level liveness policy.|
|Private rooms / invite codes|OUT OF MVP|Not a Task 03 MVP requirement.|
|Play With Friends / friends invites|OUT OF MVP|Future product scope.|
|Premade parties / party matchmaking|OUT OF MVP|Public queue is solo-only.|
|Spectating|OUT OF MVP|No spectator admission/presence model.|
|Surrender|OUT OF MVP|Intentional exit uses Leave/Quit.|
|Rematch|OUT OF MVP|Post-result flow returns to lobby/manual PLAY.|



##### **MP-DR-018 — MVP Match Access Scope** Status: **APPROVED / CLOSED** 

IN MVP: public solo matchmaking. OUT OF MVP: private rooms, invite codes, Play With Friends, premade parties, party matchmaking, and spectating. Task 03 MUST NOT treat excluded features as MVP requirements. 

13/31 | Multiplayer Product Design | DRAFT FOR FINAL APPROVAL | Page 3 

13/31 — Multiplayer Product & Match Rules Specification v1.1 

### **2. Multiplayer Terminology** 

|**Term**|**Definition**|
|---|---|
|PLAYER_ID|Stable persistent opaque product identity representing one player across<br>relaunch, temporary network loss, connection migration, and reconnect.<br>Persistent guest/anonymous identity is allowed.|
|QUEUE_ENTRY|One PLAYER_ID’s active public matchmaking request. Stores its own queue<br>age. A PLAYER_ID may have at most one active QUEUE_ENTRY.|
|queue_age|Authoritative elapsed time since QUEUE_ENTRY creation, preserved through<br>permitted temporary queue unavailability and unaffected-peer automatic<br>requeue.|
|allowed_group_sizes(queue_age)|For one QUEUE_ENTRY: age<10s => {4}; 10s<=age<20s => {3,4}; age>=20s<br>=> {2,3,4}.|
|CANDIDATE_SET|Temporary 2–4 player reservation before MATCH_CREATED. Candidate legality<br>is defined in Section 5.|
|MATCH_ROSTER|Immutable 2–4 player gameplay roster created only after all candidates<br>ACCEPT. Inherits locked gameplay semantics.|
|JOINED / ADMITTED|Player has entered the created match and obtained authoritative match state<br>needed to participate.|
|READY|Derived pre-start condition: JOINED + IN_MATCH_CONNECTED during<br>admission. No manual READY action.|
|TEMPORARILY_UNAVAILABLE|Product presence state used for app background/suspend or other temporary<br>lack of participation while the in-match grace runs.|
|FORFEIT_PENDING|Irreversible match-level terminal trigger accepted for a player but not yet finalized<br>because gameplay must reach a safe boundary and survivor gating must be<br>evaluated.|
|ELIGIBLE_SURVIVOR|At a safe gameplay boundary, a MATCH_ROSTER player who is not forfeited,<br>has no irreversible FORFEIT_PENDING trigger, is IN_MATCH_CONNECTED,<br>and can validly continue product participation. Used only as a<br>liveness/finalization gate.|
|survivor_set|Final result field containing all non-forfeited players at valid<br>FORFEIT_COMPLETION. It is not the same as ELIGIBLE_SURVIVOR.|
|safe gameplay boundary|Authoritative point at which no started gameplay effect/draw context or<br>unresolved gameplay decision must complete before product-level termination<br>can be committed. No new turn is started after a pending forfeit reaches such a<br>boundary.|
|FORFEIT_RESOLUTION_WAIT|Terminal-resolution hold entered at a safe boundary when FORFEIT_PENDING<br>exists but eligible_survivor_count=0. New gameplay progression is frozen;<br>presence/grace resolution continues.|
|MATCH_CANCELLED|Pre-gameplay-start terminal no-contest outcome.|
|MATCH_ABORTED|Post-gameplay-start no-contest terminal outcome caused by integrity failure or<br>zero eligible survivors after all remaining grace expiries.|
|result_validity|Final classification: NORMAL_COMPETITIVE, FORFEIT_COMPETITIVE,<br>FORFEIT_ADMINISTRATIVE, or NO_CONTEST.|



### **3. Multiplayer Product Invariants** 

- Gameplay mechanics and gameplay round states remain exactly as locked in Digital Game Rules Specification v1.1. 

- Presence, queue, and match-level forfeit states are orthogonal to gameplay round states. 

- After MATCH_CREATED, MATCH_ROSTER is immutable; MVP has no backfill or replacement player. 

- Every acceptance/admission/gameplay/reconnect deadline has one authoritative expiry. Client clocks do not determine outcomes. 

- Disconnect/background does not pause the match and does not change gameplay targetability by itself. 

- An authoritative timeout or completed gameplay transition is never rolled back because a player reconnects later. 

- FORFEIT_PENDING is irreversible once its trigger is committed; reconnect cannot remove it. 

- Forfeit finalization never interrupts a locked gameplay decision or started effect context; it occurs only at a safe gameplay boundary. 

- ELIGIBLE_SURVIVOR affects only forfeit finalization liveness. It never changes gameplay ACTIVE state or effect targetability. 

- No final match result may infer a gameplay winner among two or more non-forfeited survivors of a forfeit termination. 

- NO_CONTEST never creates a winner or survivor_set. 

- One PLAYER_ID may not participate in two active queues or two active gameplay matches simultaneously. 

### **4. Canonical Match Lifecycle State Model** 

Presence exceptions such as DISCONNECTED and RECONNECTING are player states, not match lifecycle states. The only added match-level hold in v1.1 is FORFEIT_RESOLUTION_WAIT. 

|**State**|**Entry condition**|**Allowed player actions**|**Product actions**|**Exit condition**|**Next states**|
|---|---|---|---|---|---|
|LOBBY|Player has no queue entry or<br>active match.|PLAY.|Validate identity/eligibility.|Valid PLAY.|MATCHMAKING|



13/31 | Multiplayer Product Design | DRAFT FOR FINAL APPROVAL | Page 4 

13/31 — Multiplayer Product & Match Rules <u>Specification v1.1</u> 

|**State**|**Entry condition**|**Allowed player actions**|**Product actions**|**Exit condition**|**Next states**|
|---|---|---|---|---|---|
|MATCHMAKING|Valid QUEUE_ENTRY exists.|Cancel queue.|Maintain queue age/liveness;<br>form legal candidates.|Cancel, invalidation, or<br>candidate commit.|LOBBY /<br>MATCH_FOUND_PENDING_<br>ACCEPTANCE|
|MATCH_FOUND_PENDING<br>_ACCEPTANCE|Legal CANDIDATE_SET<br>committed.|ACCEPT / DECLINE.|Run shared 10s acceptance<br>deadline.|All accept or any<br>decline/timeout.|MATCH_CREATED /<br>MATCHMAKING / LOBBY|
|MATCH_CREATED|All candidates accepted.|No gameplay action.|Create immutable<br>roster/seats per locked<br>gameplay contract; start 30s<br>admission.|Creation succeeds/fails.|WAITING_FOR_ADMISSION<br>/ MATCH_CANCELLED|
|WAITING_FOR_ADMISSION|Gameplay not started.|Join/reconnect; pre-start<br>Leave.|Track JOINED+connected for<br>all roster members.|All READY or admission<br>failure/deadline.|MATCH_READY /<br>MATCH_CANCELLED|
|MATCH_READY|All roster members READY.|None required.|Commit readiness; start 3s<br>countdown.|Immediate transition.|MATCH_STARTING|
|MATCH_STARTING|3s countdown active.|No READY action.|Countdown continues even if<br>a player becomes<br>unavailable.|3s expires.|MATCH_IN_PROGRESS|
|MATCH_IN_PROGRESS|Locked gameplay session<br>started.|Legal gameplay decisions;<br>Leave.|Run gameplay, decision<br>timers, presence grace, AFK<br>policy, forfeit trigger<br>detection.|GAME_ENDED, safe-<br>boundary forfeit handling, or<br>integrity abort.|MATCH_FINISHING /<br>FORFEIT_RESOLUTION_W<br>AIT / MATCH_ABORTED|
|FORFEIT_RESOLUTION_W<br>AIT|Safe boundary +<br>FORFEIT_PENDING + zero<br>eligible survivors.|Reconnect only for non-<br>forfeited unavailable players;<br>no gameplay decisions.|Freeze new gameplay;<br>continue presence/grace;<br>evaluate eligibility/expiries.|Eligible survivor appears, all<br>remaining players irreversibly<br>forfeit, or integrity abort.|MATCH_FINISHING /<br>MATCH_ABORTED|
|MATCH_FINISHING|Terminal type selected.|None affecting result.|Construct/validate final result<br>object.|Result object committed.|MATCH_COMPLETED|
|MATCH_COMPLETED|Final result committed.|Return to lobby.|Expose immutable result.|Post-result navigation.|LOBBY|
|MATCH_CANCELLED|Pre-start no contest.|Return/requeue according to<br>Section 21.|Commit<br>CANCELLED_NO_CONTES<br>T result.|Terminal handling complete.|LOBBY / MATCHMAKING|
|MATCH_ABORTED|Post-start no contest.|Return to lobby.|Commit<br>ABORTED_NO_CONTEST<br>result.|Terminal handling complete.|LOBBY|



#### **4.1 Player-facing flow from PLAY to final result** 

LOBBY → MATCHMAKING → MATCH_FOUND_PENDING_ACCEPTANCE → MATCH_CREATED → WAITING_FOR_ADMISSION → MATCH_READY → MATCH_STARTING → MATCH_IN_PROGRESS → (MATCH_FINISHING or FORFEIT_RESOLUTION_WAIT or MATCH_ABORTED) → MATCH_COMPLETED/terminal no-contest handling → LOBBY. 

### **5. Matchmaking Specification** 

##### **MP-DR-001 — Dynamic Matchmaking Eligibility** 

##### Status: **APPROVED / CLOSED** 

Each QUEUE_ENTRY has its own allowed_group_sizes(queue_age). A candidate set of size N is legal only when N belongs to every candidate member’s allowed_group_sizes. Among legal candidates, select the largest legal group; within equal eligibility select oldest queue entries first. 

|**Queue age for one entry**|**allowed_group_sizes**|
|---|---|
|age < 10s|{4}|
|10s <= age < 20s|{3,4}|
|age >= 20s|{2,3,4}|



Candidate legality: for candidate members p1..pk where k=N, candidate is legal iff N  intersection(allowed_group_sizes(p1), ..., ∈ allowed_group_sizes(pk)). A long-waiting entry cannot force a younger entry into a match size that the younger entry does not yet allow. 

|**Example**|**Entries**|**Evaluation**|**Outcome**|
|---|---|---|---|
|2-player candidate|A=27s {2,3,4}; B=23s {2,3,4}|Intersection={2,3,4}; N=2 is allowed for<br>both.|LEGAL|
|3-player candidate|A=12s {3,4}; B=17s {3,4}; C=29s {2,3,4}|Intersection={3,4}; N=3 is allowed for all.|LEGAL|
|Mixed ages: 2p attempt|A=27s {2,3,4}; B=4s {4}|Intersection={4}; N=2 not allowed.|ILLEGAL|
|More players than needed|Five entries all age>=20s|A legal 4-player group exists and is larger<br>than any legal 2/3 group; choose four<br>oldest eligible entries. Fifth stays queued.|4P SELECTED|
|Mixed group where only 4 works|A=25s; B=4s; C=8s; D=12s|All four allow 4; smaller candidate<br>containing B/C is not legal.|4P LEGAL|
|Equal-age tie|Multiple equally old entries all eligible for<br>same group size|Use stable authoritative QUEUE_ENTRY<br>creation order as final tie-break; never<br>randomize equal-priority selection.|DETERMINISTIC|



Matchmaking formation order: (1) enumerate legal candidate sizes under each entry’s current eligibility; (2) prefer the largest legal size; (3) choose the oldest eligible entries; (4) break exact age ties by stable queue-entry creation order. Candidate reservation freezes those entries until acceptance resolves. 

13/31 | Multiplayer Product Design | DRAFT FOR FINAL APPROVAL | Page 5 

13/31 — Multiplayer Product & Match Rules <u>Specification v1.1</u> 

|**Queue event**|**Normative behavior**|
|---|---|
|PLAY while eligible|Create one QUEUE_ENTRY; queue_age starts.|
|PLAY twice|Second PLAY is a no-op; no duplicate queue entry.|
|Cancel while still queued|Invalidate QUEUE_ENTRY immediately; return to lobby.|
|Cancel races with candidate commit|Whichever is authoritatively committed first governs; if candidate already<br>committed, the player is in Match Found flow and cancellation is treated as<br>DECLINE.|
|Queue entry liveness expires|Invalidate entry; queue age is lost; player must press PLAY again.|
|Peer-caused candidate/admission failure|Unaffected players may auto-requeue with original queue age/priority as<br>specified in Sections 6–8.|



##### **MP-DR-016 — Queue Liveness and Precedence** 

##### Status: **APPROVED / CLOSED** 

Foreground network disconnect grace = 10s. Background grace = 30s. While backgrounded, background grace owns queue unavailability. Timer ownership transitions MUST NOT extend queue occupancy beyond an already earlier authoritative deadline. 

#### **5.1 Queue liveness precedence** 

|**Situation**|**Authoritative rule**|
|---|---|
|Foreground + network lost|Start QUEUE_DISCONNECT_GRACE, deadline = loss_time+10s. Reconnect<br>before deadline preserves QUEUE_ENTRY and queue_age; expiry invalidates<br>entry.|
|Background while connected|Start QUEUE_BACKGROUND_GRACE, background_deadline =<br>background_time+30s. Foreground return while connected restores queue-active<br>presence.|
|Network lost while already backgrounded|Do not start a separate 10s timer. Existing 30s background deadline remains<br>authoritative.|
|Network restores while still backgrounded|Remain under QUEUE_BACKGROUND_GRACE. Network restoration alone<br>does not restore queue-active presence.|
|Foreground return while still offline|Transition to QUEUE_DISCONNECT_GRACE with effective deadline =<br>min(original_background_deadline, foreground_return_time+10s).|
|Background occurs after foreground disconnect already started|Background becomes the presence owner, but effective expiry =<br>min(existing_disconnect_deadline, background_time+30s). This is DERIVED<br>from the approved no-deadline-extension rule.|
|Any liveness deadline expires|Invalidate QUEUE_ENTRY. A later foreground/reconnect does not resurrect it.|



Approved examples: Background T=0, network loss T=5, foreground offline T=20 => background deadline T=30, new disconnect candidate T=30, expiry T=30. Background T=0, foreground offline T=5 => disconnect candidate T=15, background deadline T=30, expiry T=15. 

### **6. Match Found / Acceptance Flow** 

##### **MP-DR-002 — Match Found Acceptance** Status: **APPROVED / CLOSED** 

Every legal CANDIDATE_SET requires explicit ACCEPT from every candidate within 10 seconds. There is no automatic acceptance. 

|**Case**|**Normative behavior**|
|---|---|
|All ACCEPT before deadline|Create MATCH_CREATED with exactly those players; candidate queue<br>reservations convert into immutable roster membership.|
|Any DECLINE|Dissolve candidate immediately. Decliner returns to lobby. Unaffected<br>candidates auto-requeue with preserved queue age/priority.|
|Any acceptance timeout|Treat non-accepting player as failing candidate; dissolve candidate. Unaffected<br>candidates auto-requeue with preserved queue age/priority.|
|Disconnect/background during acceptance|10s acceptance timer never pauses. Reconnect/foreground may ACCEPT only if<br>deadline has not expired. No auto-accept.|
|Candidate dissolves after one player accepted|Accepted unaffected players are not penalized; auto-requeue with preserved<br>queue age/priority.|
|Late ACCEPT after expiry|Reject as stale; cannot recreate dissolved candidate.|



### **7. Player Admission, Ready State, and Match Start** 

##### **MP-DR-003 — Ready / Start Flow** Status: **APPROVED / CLOSED** 

No manual READY button. READY is derived from JOINED + IN_MATCH_CONNECTED. When all roster members are READY, enter MATCH_READY and run a 3-second automatic start countdown. 

13/31 | Multiplayer Product Design | DRAFT FOR FINAL APPROVAL | Page 6 

13/31 — Multiplayer Product & Match Rules Specification v1.1 

##### **MP-DR-004 — Initial Admission** 

##### Status: **APPROVED / CLOSED** 

A shared 30-second admission window starts at MATCH_CREATED. If any roster member is not JOINED and connected by the deadline, the entire match is MATCH_CANCELLED / NO_CONTEST. 

|**Stage term**|**Exact condition**|**Product consequence**|
|---|---|---|
|matched|PLAYER_ID is in current CANDIDATE_SET.|Not yet in gameplay roster.|
|accepted|Player submitted ACCEPT before candidate<br>deadline.|Still not roster member until all candidates accept.|
|connected|Usable live match presence exists.|Does not by itself imply joined/ready.|
|joined / admitted|Player entered created match and received<br>authoritative match state.|Counts toward admission.|
|ready|JOINED + IN_MATCH_CONNECTED during<br>admission.|Derived; no player action.|
|gameplay started|3s MATCH_STARTING countdown expired.|Locked gameplay session begins.|



Commitment boundary: once MATCH_READY is reached, pre-match admission has succeeded. Disconnect during the 3- second MATCH_STARTING countdown does not cancel the match. The countdown completes; the unavailable player enters MATCH_IN_PROGRESS under the 60-second in-match grace measured from actual unavailability time. 

### **8. Pre-Match Failure Rules** 

|**Situation**|**Product behavior**|
|---|---|
|Player disconnects before JOINED|May reconnect/join before shared 30s deadline. Expiry =><br>MATCH_CANCELLED / NO_CONTEST.|
|Player closes/kills app before JOINED|Same as unavailable/not admitted; deadline continues.|
|Player loads slowly|No special extension. Join before 30s or match cancels.|
|Player never joins|At 30s => MATCH_CANCELLED / NO_CONTEST.|
|Authoritative match session cannot start|MATCH_CANCELLED / NO_CONTEST.|
|Player Leave before MATCH_IN_PROGRESS|Treat as pre-start admission failure, not competitive forfeit; cancel whole match.|
|All players admit|MATCH_READY → 3s MATCH_STARTING.|
|Disconnect during MATCH_STARTING|Do not cancel; start gameplay and apply in-match grace.|



On peer-caused pre-start cancellation, players who successfully accepted/admitted and did not cause the failure may automatically return to matchmaking with preserved prior queue age/priority. The failing player returns to lobby and must press PLAY again. 

### **9. Authoritative Timer Specification** 

##### **MP-DR-005 — Gameplay Decision Timer Durations** 

##### Status: **APPROVED / CLOSED** 

Normal Turn Action Timer = 20s. Effect Target Selection Timer = 10s. Expiry behavior remains locked: action timeout => STOPPED + TIMEOUT; target timeout => automatic self-target when DECISION_OWNER remains ACTIVE. 

|**Timer**|**Start**|**Duration**|**Pause/ownership**|**Expiry**|**Reconnect interaction**|**Client visibility**|
|---|---|---|---|---|---|---|
|Queue background grace|QUEUED player enters<br>background.|30s|Owned by background<br>state; no pause.|Invalidate<br>QUEUE_ENTRY.|Foreground connected<br>restores; foreground offline<br>uses Section 5.1 deadline<br>rule.|Status/deadline may be<br>surfaced on return.|
|Queue disconnect grace|Foreground QUEUED<br>player loses usable<br>network.|10s|No pause; may transfer to<br>background owner without<br>deadline extension.|Invalidate<br>QUEUE_ENTRY.|Reconnect before effective<br>deadline preserves<br>entry/age.|May be surfaced on<br>reconnect.|
|Match Found acceptance|CANDIDATE_SET<br>committed.|10s|Never pauses.|Non-accepting candidate<br>fails; candidate dissolves.|Reconnect may ACCEPT<br>before deadline only.|Visible countdown required<br>when available.|
|Initial join/load|MATCH_CREATED.|30s shared|Never pauses.|Any roster member not<br>READY =><br>MATCH_CANCELLED.|Reconnect/join before<br>deadline succeeds.|Visible admission<br>progress/deadline<br>recommended.|
|Match start countdown|MATCH_READY.|3s|Never pauses.|Enter<br>MATCH_IN_PROGRESS.|Disconnect does not<br>cancel; in-match grace<br>starts.|Visible countdown<br>recommended.|
|Normal turn action|Locked<br>WAITING_FOR_PLAYER_<br>ACTION.|20s|Never pauses for<br>disconnect/background.|Locked STOPPED +<br>TIMEOUT.|Reconnect before deadline<br>may act; after expiry no<br>rollback.|Visible when match view<br>available.|
|Effect target selection|Locked<br>WAITING_FOR_TARGET_<br>SELECTION.|10s|Never pauses for<br>disconnect/background.|Locked automatic self-<br>target.|Reconnect before deadline<br>may target; after expiry no<br>rollback.|Visible when match view<br>available.|
|In-match<br>reconnect/unavailability|Player becomes<br>DISCONNECTED or<br>TEMPORARILY_UNAVAIL<br>ABLE in active match.|60s continuous|No global pause. Clears<br>only on successful return to<br>IN_MATCH_CONNECTED<br>before expiry.|Create irreversible<br>FORFEIT_PENDING.|Reconnect before expiry<br>resyncs; after expiry cannot<br>restore participation.|<br>Remaining grace should be<br>visible when<br>reconnecting/foreground.|



Timer finality: authoritative deadline order governs. Input arriving after expiry is stale even if the client displayed remaining time. Disconnect/background never extends gameplay decision deadlines. 

13/31 | Multiplayer Product Design | DRAFT FOR FINAL APPROVAL | Page 7 

13/31 — Multiplayer Product & Match Rules Specification v1.1 

### **10. Disconnect Specification — DR-005C** 

##### **MP-DR-006 / DR-005C — In-Match Reconnect Grace** 

##### Status: **APPROVED / CLOSED** 

Reconnect/unavailability grace = 60 seconds continuous. The match does not globally pause. Gameplay decision timers continue. Disconnect does not change gameplay round_state or effect targetability. Grace expiry creates irreversible FORFEIT_PENDING, subject to safe-boundary and eligiblesurvivor finalization. 

|**Situation**|**Normative behavior**|
|---|---|
|Network disconnect outside own decision|Set presence IN_MATCH_DISCONNECTED; start/continue 60s grace;<br>gameplay continues.|
|Disconnect during own normal action timer|20s timer continues. Reconnect before action deadline may act; otherwise<br>locked STOPPED + TIMEOUT occurs. Grace continues independently until<br>connection restored or 60s expiry.|
|Disconnect during own target timer|10s timer continues. Reconnect before deadline may choose legal target;<br>otherwise locked self-target occurs.|
|Disconnected player is effect target|Still targetable if locked gameplay round_state is ACTIVE. Presence alone never<br>changes target legality.|
|Grace expiry during unresolved decision/effect|Commit FORFEIT_PENDING. Do not interrupt locked resolution; complete<br>required timeout/effect atomicity to next safe boundary.|
|Grace expiry at stable safe boundary|Evaluate ELIGIBLE_SURVIVOR gate immediately; finalize or enter<br>FORFEIT_RESOLUTION_WAIT.|



#### **10.1 No general disconnect pause** 

A global pause is not used because one unavailable player must not indefinitely stall a synchronous 2–4 player match. Mobile resilience is provided by 60s reconnect grace while locked decision timers deterministically resolve missing decisions. 

### **11. Reconnect Specification** 

|**Scenario**|**Expected behavior**|
|---|---|
|App closes and reopens after 5s|If within 60s and no irreversible FORFEIT_PENDING: identify same<br>PLAYER_ID, enter RECONNECTING, resync authoritative state, return<br>IN_MATCH_CONNECTED.|
|Wi-Fi → mobile transition|Transient loss uses same grace only if usable presence is interrupted. Identity<br>remains same PLAYER_ID.|
|Reconnect during another player’s turn|Resync current state; no gameplay rollback or extra action.|
|Reconnect during own action timer|May act only if authoritative 20s deadline has not expired and player remains<br>legal decision owner.|
|Reconnect after automatic action timeout|Timeout result remains committed; no rollback. Player can continue future<br>participation if no forfeit trigger.|
|Reconnect after target timeout|Automatic self-target remains committed; resync post-resolution state.|
|Reconnect after 60s grace expiry|FORFEIT_PENDING is irreversible; reconnect cannot restore participation. It<br>may only receive terminal/result state.|
|Reconnect during FORFEIT_RESOLUTION_WAIT by non-forfeited player|If before that player’s own grace expiry and identity/state restoration succeeds,<br>player becomes ELIGIBLE_SURVIVOR and triggers forfeit finalization.|
|Reconnect after MATCH_COMPLETED|Read final result only. No state mutation.|
|Reconnect after MATCH_ABORTED/CANCELLED|Read terminal no-contest state; no resurrection.|



Authoritative events are irreversible once committed: acceptance/admission expiry, gameplay timeout outcomes, gameplay state transitions, FORFEIT_PENDING triggers, MATCH_COMPLETED, MATCH_CANCELLED, and MATCH_ABORTED. 

### **12. App Background / Suspend Rules** 

##### **MP-DR-007 — Background / Suspend** 

##### Status: **APPROVED / CLOSED** 

During MATCH_IN_PROGRESS, app background/suspend is TEMPORARILY_UNAVAILABLE and uses the same 60-second continuous in-match grace. Gameplay decision timers continue. 

|**Mobile event**|**Product behavior**|
|---|---|
|Minimize app|TEMPORARILY_UNAVAILABLE; 60s grace starts/continues; gameplay<br>continues.|
|Lock phone|Same as background.|
|OS suspends app|Same product behavior; no pause.|
|App killed/crashes|Presence becomes unavailable; 60s grace applies. Relaunch may reconnect via<br>same PLAYER_ID.|
|Phone call / temporary foreground loss|TEMPORARILY_UNAVAILABLE while match presence unavailable; grace<br>continues.|



13/31 | Multiplayer Product Design | DRAFT FOR FINAL APPROVAL | Page 8 

||13/31 — Multiplayer Product&Match Rules Specification v1.1|
|---|---|
|**Mobile event**|**Product behavior**|
|Return foreground before grace expiry|Reconnect/resync. If presence restored, clear continuous unavailability grace.|
|Remain unavailable for 60s|Irreversible FORFEIT_PENDING.|



### **13. AFK Policy** 

##### **MP-DR-008 — Connected AFK** 

##### Status: **APPROVED / CLOSED** 

Track consecutive decision timeouts that occurred while the player remained connected for the entire decision window. First => counter 1. Second => warning. Third => FORFEIT_PENDING after the locked timeout outcome resolves. Any accepted manual gameplay decision resets counter to 0. 

|**Case**|**Product behavior**|
|---|---|
|1 connected timeout|Locked gameplay timeout outcome occurs; AFK counter=1.|
|2 consecutive connected timeouts|Locked outcome occurs; AFK counter=2; emit warning state/event.|
|3 consecutive connected timeouts|Locked outcome occurs first; then commit irreversible FORFEIT_PENDING.|
|Accepted manual gameplay decision|Reset consecutive connected AFK counter to 0.|
|Timeout window contains disconnect/background|Do not count that timeout toward connected AFK threshold; disconnect policy<br>applies separately.|
|Disconnected player times out repeatedly|Gameplay timeouts still resolve, but do not create connected-AFK count.|
|Analytics|AFK causes/reasons should be observable, but analytics flags do not alter<br>gameplay mechanics.|



### **14. Intentional Leave / Quit** 

##### **MP-DR-009 — Intentional Leave** 

##### Status: **APPROVED / CLOSED** 

Leave during MATCH_IN_PROGRESS requires confirmation. Once the confirmed Leave is authoritatively accepted, FORFEIT_PENDING is immediate and irreversible; no reconnect grace and no rollback. Finalization still waits for a safe gameplay boundary. 

|**Scenario**|**Outcome**|
|---|---|
|Leave requested but not confirmed|No state change.|
|Leave confirmed at safe boundary|Commit FORFEIT_PENDING; immediately evaluate eligible-survivor gate.|
|Leave confirmed during own action decision|Commit FORFEIT_PENDING; reject any later action from leaver; locked action<br>timer expires to STOPPED+TIMEOUT before safe-boundary finalization.|
|Leave confirmed during target selection|Commit FORFEIT_PENDING; reject later target input from leaver; locked target<br>timer self-targets on expiry, effect resolves atomically, then finalize.|
|Leave confirmed while another player decision is pending|Current decision resolves; do not start a new turn after next safe boundary.|
|Connection drops at same time as Leave|If confirmed Leave commits first, Leave forfeit governs. If disconnect commits<br>first but confirmed Leave then commits before grace expiry, Leave immediately<br>creates irreversible FORFEIT_PENDING.|
|Reconnect after confirmed Leave|Cannot restore participation.|



### **15. Surrender** 

##### **MP-DR-010 — Surrender** 

##### Status: **APPROVED — OUT OF MVP** 

MVP has no separate SURRENDER command. Intentional in-match exit uses confirmed Leave/Quit, which is governed by Section 14 and the forfeit policy. 

### **16. Match Forfeit Policy — DR-005D** 

##### **MP-DR-011 / DR-005D — Forfeit Continuation** Status: **APPROVED / CLOSED** 

A finalized player forfeit ends the whole gameplay match. MVP never continues with a reduced roster, never backfills/replaces a player, and never converts match-level forfeit into ZEROED/STOPPED/BUST/PERFECT_31. 

|**Trigger**|**Forfeit?**|**Rule**|
|---|---|---|
|60s continuous disconnect/unavailability|YES|At 60s commit irreversible FORFEIT_PENDING.|
|Confirmed intentional Leave|YES|Immediate irreversible FORFEIT_PENDING; no<br>grace.|
|3 consecutive connected AFK timeouts|YES|After third locked timeout outcome resolves, commit<br>FORFEIT_PENDING.|
|Single/two connected AFK timeouts|NO|Gameplay timeout outcomes only; warning at 2.|



13/31 | Multiplayer Product Design | DRAFT FOR FINAL APPROVAL | Page 9 

|||13/31 — Multiplayer Product&Match Rules Specification v1.1|
|---|---|---|
|**Trigger**|**Forfeit?**|**Rule**|
|Gameplay bust/ZERO/STOP/PERFECT_31|NO|Round-level gameplay states only.|
|Pre-start admission failure|NO competitive forfeit|MATCH_CANCELLED / NO_CONTEST.|
|Future discipline/policy violation|OUT OF CURRENT MVP|May be future trigger only by separate approved<br>change.|



#### **16.1 Safe-boundary and Eligible Survivor Finalization Gate** 

##### **MP-DR-013 — Eligible Survivor / Forfeit Finalization Gate** 

##### Status: **APPROVED / CLOSED** 

At a safe gameplay boundary with FORFEIT_PENDING: if eligible_survivor_count>=1, finalize FORFEIT_COMPLETION. If eligible_survivor_count=0, enter FORFEIT_RESOLUTION_WAIT, freeze new gameplay, and continue only presence/reconnect grace resolution. If all remaining non-forfeited players reach irreversible forfeit/unavailability expiry without any eligible survivor, MATCH_ABORTED / ABORTED_NO_CONTEST. 

1. When any irreversible forfeit trigger commits, mark that player FORFEIT_PENDING. Do not roll back already committed gameplay. 

2. If gameplay is inside a pending decision or started effect/draw context, allow locked timeout/atomicity rules to complete until the next safe gameplay boundary. 

3. At the safe boundary, do not start a new normal turn while any FORFEIT_PENDING exists. 

4. Compute ELIGIBLE_SURVIVOR over the current MATCH_ROSTER. A player is eligible only if not forfeited/pending, currently IN_MATCH_CONNECTED, and able to continue product participation. 

5. If eligible_survivor_count>=1, finalize FORFEIT_COMPLETION immediately using Section 16.2. 

6. If eligible_survivor_count=0, enter FORFEIT_RESOLUTION_WAIT. No gameplay decisions or new turns are accepted/started; presence and individual 60s grace deadlines continue. 

7. If a non-forfeited unavailable player reconnects before their own grace expiry, that player becomes ELIGIBLE_SURVIVOR and forfeit finalization occurs immediately from the existing safe hold. 

8. If all remaining non-forfeited players reach irreversible expiry while eligible_survivor_count remains 0, terminate MATCH_ABORTED with termination_type=ABORTED_NO_CONTEST, winner_set empty, survivor_set empty, result_validity=NO_CONTEST. 

9. If a critical authoritative integrity failure occurs during FORFEIT_RESOLUTION_WAIT, integrity abort takes precedence and produces ABORTED_NO_CONTEST. 

Important: ELIGIBLE_SURVIVOR is a liveness gate only. survivor_set is a final result field. Example: A is FORFEIT_PENDING, B reconnects and is eligible, C is still disconnected but has not reached its grace expiry. Finalization is allowed because B is eligible; survivor_set={B,C}, not only {B}. Because there are two non-forfeited survivors, winner_set is empty and result_validity=FORFEIT_ADMINISTRATIVE. 

#### **16.2 Revised Forfeit Result Model** 

##### **MP-DR-012 — Forfeit Result Model** 

##### Status: **APPROVED / CLOSED** 

Forfeit result depends on the number of non-forfeited survivors at finalization, not original match size. Exactly one survivor => unique winner and FORFEIT_COMPETITIVE. Two or more survivors => winner_set empty and FORFEIT_ADMINISTRATIVE. Never infer shared winners, ordered placement, or a score-based winner. 

|**Condition**|**Final fields / action**|**Validity**|**Interpretation**|
|---|---|---|---|
|Exactly 1 non-forfeited survivor|winner_set={survivor};<br>survivor_set={survivor};<br>forfeited_players=all pending/final forfeits.|FORFEIT_COMPETITIVE|Unique match winner. Applies to original<br>2p, 3p, or 4p.|
|2+ non-forfeited survivors|winner_set=empty; survivor_set=all non-<br>forfeited players; forfeited_players=all<br>pending/final forfeits.|FORFEIT_ADMINISTRATIVE|No gameplay winner, no ordered<br>placement, no score-inferred winner.<br>Survivors are administratively non-losing.|
|0 eligible survivors at safe boundary|Do not finalize yet. Enter<br>FORFEIT_RESOLUTION_WAIT.|Not final|Wait for eligible survivor or all remaining<br>expiries.|
|All remaining players irreversibly expire<br>with no eligible survivor|winner_set=empty; survivor_set=empty.|NO_CONTEST|MATCH_ABORTED /<br>ABORTED_NO_CONTEST.|



#### **16.3 No Reduced-Roster Continuation** 

Once a player forfeit is finalized, the gameplay match terminates. Surviving players do not continue the current round, start a new round, or play a reduced-roster tie-break. This preserves the immutable roster and avoids inventing gameplay transitions not defined by the locked rules. 

### **17. Forfeit vs Gameplay Elimination** 

|**Layer**|**Example states**|**Purpose**|**Authority**|
|---|---|---|---|
|Gameplay round layer|ACTIVE, STOPPED, FORCED_STOP,<br>BUST_13, BUST_OVER_31,<br>PERFECT_31, ZEROED,<br>NOT_PARTICIPATING|Controls turns, targeting, and round<br>scoring.|Digital Game Rules v1.1 — LOCKED|



13/31 | Multiplayer Product Design | DRAFT FOR FINAL APPROVAL | Page 10 

13/31 — Multiplayer Product & Match Rules <u>Specification v1.1</u> 

|**Layer**|**Example states**|**Purpose**|**Authority**|
|---|---|---|---|
|Player presence layer|IN_MATCH_CONNECTED,<br>TEMPORARILY_UNAVAILABLE,<br>IN_MATCH_DISCONNECTED,<br>RECONNECTING|Controls ability to receive<br>prompts/submit decisions and grace<br>timing.|This specification|
|Match-forfeit layer|FORFEIT_PENDING, FORFEITED|Controls irreversible match-level<br>termination cause.|This specification|
|Match lifecycle layer|MATCH_IN_PROGRESS,<br>FORFEIT_RESOLUTION_WAIT,<br>MATCH_FINISHING,<br>MATCH_COMPLETED,<br>MATCH_ABORTED|Controls online session terminal<br>behavior.|This specification|



A player can be gameplay-inactive for a round without forfeiting the match. Conversely, a player may become match-level FORFEIT_PENDING while their locked gameplay round state remains whatever the gameplay engine last committed. Product forfeit MUST NOT mutate that round state to simulate elimination. 

### **18. Match Cancellation / No Contest / Abort** 

|**Condition**|**Phase**|**Terminal behavior**|**Winner?**|**Result impact**|
|---|---|---|---|---|
|Candidate fails before<br>MATCH_CREATED|Pre-match|No match result object required<br>beyond candidate failure;<br>unaffected players may auto-<br>requeue.|No|No competitive result.|
|Match creation fails|Pre-start|MATCH_CANCELLED /<br>CANCELLED_NO_CONTEST.|No|NO_CONTEST.|
|Any roster player misses 30s<br>admission|Pre-start|MATCH_CANCELLED /<br>CANCELLED_NO_CONTEST.|No|NO_CONTEST.|
|Player quits before gameplay<br>start|Pre-start|MATCH_CANCELLED /<br>CANCELLED_NO_CONTEST.|No|NO_CONTEST.|
|Critical authoritative state<br>corruption after start|Post-start|MATCH_ABORTED /<br>ABORTED_NO_CONTEST.|No|NO_CONTEST.|
|FORFEIT_RESOLUTION_WAIT<br>ends with all remaining players<br>irreversibly expired|Post-start|MATCH_ABORTED /<br>ABORTED_NO_CONTEST.|No|NO_CONTEST.|
|Ordinary player disconnect under<br>60s|Post-start|Not cancellation; match<br>continues.|N/A|No result yet.|
|One forfeit with eligible survivor|Post-start|FORFEIT_COMPLETION, not<br>cancellation.|Per Section 16.2|Authoritative forfeit result.|



Ranked/MMR is a future dependency. A future rating system MUST consume result_validity and MUST NOT reinterpret NO_CONTEST as a gameplay win/loss. 

### **19. Match Result Validity and Finalization** 

MATCH_FINISHING is the single product finalization step for winner-bearing results. MATCH_COMPLETED begins only after the authoritative result object below is internally consistent. MATCH_CANCELLED and MATCH_ABORTED also produce terminal result records with NO_CONTEST semantics. 

|**Required result field**|**Normative rule**|
|---|---|
|match_id|Unique match identifier.|
|roster|Complete immutable MATCH_ROSTER.|
|seat_order|Locked seat order assigned at MATCH_CREATED.|
|termination_type|NORMAL_COMPLETION, FORFEIT_COMPLETION,<br>CANCELLED_NO_CONTEST, or ABORTED_NO_CONTEST.|
|winner_set|Exactly one player for NORMAL_COMPLETION and one-survivor<br>FORFEIT_COMPLETION; otherwise empty.|
|survivor_set|FORFEIT_COMPLETION only: all non-forfeited players at finalization. Empty for<br>NORMAL_COMPLETION and all NO_CONTEST results.|
|forfeited_players|All players whose irreversible forfeit trigger is included in a<br>FORFEIT_COMPLETION. Empty for NORMAL_COMPLETION and cancellation;<br>may record pending causes for aborted diagnostics without creating a forfeit<br>result.|
|final_total_scores|Locked TOTAL_SCORE values through the last completed normal round.<br>Unfinished round progress is not retroactively scored.|
|result_validity|NORMAL_COMPETITIVE, FORFEIT_COMPETITIVE,<br>FORFEIT_ADMINISTRATIVE, or NO_CONTEST.|
|finalization_reason|Authoritative terminal cause, e.g. GAMEPLAY_GAME_ENDED,<br>FORFEIT_LEAVE, FORFEIT_RECONNECT_GRACE,<br>FORFEIT_CONNECTED_AFK, ADMISSION_TIMEOUT,<br>NO_ELIGIBLE_SURVIVOR, INTEGRITY_ABORT.|
|forfeit_reasons_by_player|Required when forfeited_players is non-empty; identifies each player’s<br>committed Leave / reconnect-grace / connected-AFK cause.|



13/31 | Multiplayer Product Design | DRAFT FOR FINAL APPROVAL | Page 11 

||||13/3 <br>|1 — Multiplayer Product&Ma<br>|tch Rules Specification v1.1<br>|
|---|---|---|---|---|---|
|**Termination type**|**Trigger**|**winner_set**|**survivor_set**|**result_validity**|**Meaning**|
|NORMAL_COMPLETION|Locked gameplay reaches<br>GAME_ENDED.|Exactly one locked gameplay<br>winner after tie-break if<br>needed.|empty|NORMAL_COMPETITIVE|Winner-bearing competitive<br>result.|
|FORFEIT_COMPLETION —<br>1 survivor|At least one player forfeits;<br>exactly one non-forfeited<br>survivor at valid finalization.|Exactly that survivor.|{that player}|FORFEIT_COMPETITIVE|Winner-bearing match-level<br>forfeit result.|
|FORFEIT_COMPLETION —<br>2+ survivors|At least one player forfeits;<br>two or more non-forfeited<br>survivors at valid finalization.|empty|all non-forfeited players|FORFEIT_ADMINISTRATIVE|Authoritative administrative<br>result; no gameplay<br>winner/placement.|
|CANCELLED_NO_CONTES<br>T|Gameplay not started and<br>pre-start failure occurs.|empty|empty|NO_CONTEST|No competitive result.|
|ABORTED_NO_CONTEST|Post-start integrity abort or no<br>eligible survivor after all<br>remaining grace expiries.|empty|empty|NO_CONTEST|No competitive result.|



Result finality: once MATCH_COMPLETED/CANCELLED/ABORTED terminal record is committed, reconnect, late gameplay input, late acceptance, or client-local state cannot change it. 

### **20. Rematch Flow** 

##### **MP-DR-014 — Rematch** 

##### Status: **APPROVED — OUT OF MVP** 

MVP has no rematch handshake. Every future game requires a new manual PLAY and forms a new match with reset match-level scores/state according to normal match creation. 

### **21. Matchmaking Re-entry Rules** 

##### **MP-DR-015 — Post-Match Re-entry** 

Status: **APPROVED / CLOSED** 

After NORMAL_COMPLETION, FORFEIT_COMPLETION, MATCH_ABORTED, or completed MATCH_CANCELLED handling, players return to lobby and must press PLAY again, except unaffected players in candidate/pre-start peer failure may auto-requeue with preserved prior queue age/priority. 

|**Prior outcome**|**Next product state**|
|---|---|
|NORMAL_COMPLETION|Lobby; manual PLAY.|
|FORFEIT_COMPLETION — forfeiter|Lobby; manual PLAY. Future cooldown/discipline is out of MVP.|
|FORFEIT_COMPLETION — survivor|Lobby; manual PLAY.|
|MATCH_ABORTED|Lobby; manual PLAY.|
|User cancels own queue|Lobby; manual PLAY.|
|Decline/timeout in Match Found — failing player|Lobby; manual PLAY.|
|Match Found dissolves due to another candidate|Unaffected player auto-requeues with preserved queue age/priority.|
|Pre-start admission failure caused by another roster member|Unaffected admitted/ready players may auto-requeue with preserved queue<br>age/priority.|
|Rematch|Not available; lobby/manual PLAY.|



### **22. Player Presence State Model** 

|**Presence state**|**Meaning**|**Allowed actions**|**Next states**|
|---|---|---|---|
|LOBBY_ONLINE|Valid PLAYER_ID, not queued/not active<br>match.|PLAY.|MATCHMAKING / OFFLINE|
|MATCHMAKING|Active QUEUE_ENTRY,<br>foreground+network usable.|Cancel.|QUEUE_BACKGROUND_GRACE /<br>QUEUE_DISCONNECT_GRACE /<br>MATCH_FOUND / LOBBY|
|QUEUE_BACKGROUND_GRACE|Queued but app backgrounded; 30s<br>owner.|Foreground return.|MATCHMAKING /<br>QUEUE_DISCONNECT_GRACE /<br>LOBBY|
|QUEUE_DISCONNECT_GRACE|Queued, foreground, network unusable;<br>effective 10s/precedence deadline.|Reconnect or background transition.|MATCHMAKING /<br>QUEUE_BACKGROUND_GRACE /<br>LOBBY|
|MATCH_FOUND_PENDING_ACCEPTAN<br>CE|Candidate reserved.|ACCEPT / DECLINE.|JOINING_MATCH / MATCHMAKING /<br>LOBBY|
|JOINING_MATCH|MATCH_CREATED; admission<br>incomplete.|Join/reconnect.|IN_MATCH_CONNECTED /<br>MATCH_CANCELLED|
|IN_MATCH_CONNECTED|Admitted/in-match and able to submit<br>legal decisions.|Gameplay decisions / Leave when legal.|TEMPORARILY_UNAVAILABLE /<br>IN_MATCH_DISCONNECTED /<br>FORFEIT_PENDING /<br>MATCH_COMPLETE|
|TEMPORARILY_UNAVAILABLE|Background/suspend/unavailable.|Return foreground/reconnect.|RECONNECTING /<br>FORFEIT_PENDING /<br>MATCH_COMPLETE|



13/31 | Multiplayer Product Design | DRAFT FOR FINAL APPROVAL | Page 12 

|||13/31 — Multiplay|er Product&Match Rules Specification v1.1|
|---|---|---|---|
|**Presence state**|**Meaning**|**Allowed actions**|**Next states**|
|IN_MATCH_DISCONNECTED|Network presence lost.|Reconnect.|RECONNECTING /<br>FORFEIT_PENDING /<br>MATCH_COMPLETE|
|RECONNECTING|Same PLAYER_ID attempting<br>authoritative resync.|No speculative gameplay action until<br>resync complete.|IN_MATCH_CONNECTED /<br>FORFEIT_PENDING /<br>MATCH_COMPLETE|
|FORFEIT_PENDING|Irreversible forfeit trigger committed;<br>match not yet final.|No new gameplay decisions.|FORFEITED / MATCH_COMPLETE|
|FORFEITED|Player included in finalized<br>forfeited_players.|View result only.|MATCH_COMPLETE / LOBBY|
|MATCH_COMPLETE|Terminal match result available.|Return to lobby.|LOBBY_ONLINE|



#### **22.1 Presence vs gameplay examples** 

|**Presence**|**Gameplay round state**|**Meaning**|
|---|---|---|
|IN_MATCH_DISCONNECTED|ACTIVE|Still ACTIVE/targetable under locked rules; presence<br>grace/timers handle missing input.|
|TEMPORARILY_UNAVAILABLE|STOPPED|Unavailable player may already be round-inactive;<br>60s match-level grace still matters for future<br>rounds/match participation.|
|FORFEIT_PENDING|ACTIVE|Do not mutate ACTIVE to a gameplay terminal state;<br>product waits for safe-boundary termination.|
|IN_MATCH_CONNECTED|ZEROED|Connected but eliminated only from current round;<br>remains a match participant.|



### **23. Player Identity Model** 

##### **MP-DR-017 — Player Identity Model** Status: **APPROVED / CLOSED** 

MVP requires a stable persistent opaque PLAYER_ID and authenticated session identity. Persistent guest/anonymous identity is allowed. Mandatory registered accounts are not required. 

|**Identity case**|**Product rule**|
|---|---|
|App relaunch|Same PLAYER_ID must be recognized so an active match is resumed rather<br>than a new queue identity created.|
|Temporary network loss|Reconnect ownership is proven as the same PLAYER_ID.|
|Wi-Fi → mobile transition|Identity remains unchanged across transport change.|
|App crash/kill|Relaunch may recover the same active session identity and reconnect within<br>grace.|
|Persistent guest identity|Allowed for MVP and may satisfy PLAYER_ID persistence without<br>email/password/social login.|
|Registered account login|Not mandatory for MVP.|
|Account linking / guest→registered merge|OUT OF MVP.|
|Cross-device account recovery|OUT OF MVP.|
|Reinstall recovery guarantees|OUT OF MVP.|
|Social login requirement|OUT OF MVP.|



“Authenticated” means the product can reliably establish current-session ownership of the opaque PLAYER_ID. It does not imply mandatory email/password, Apple Sign-In, Google Sign-In, or social account registration. 

### **24. Illegal / Race-Condition Product Actions** 

Race policy: events are evaluated in one authoritative order. The first committed state transition governs; later events are evaluated against the resulting state and cannot roll it back. The following specific races are normative. 

|**Race / illegal action**|**Authoritative outcome**|
|---|---|
|PLAY twice|First valid PLAY creates one QUEUE_ENTRY; later PLAY while queued is no-op.|
|Cancel queue vs MATCH_FOUND|Cancel first => no candidate membership. Candidate commit first => cancellation is a<br>DECLINE under Match Found flow.|
|Late ACCEPT after 10s|Acceptance expiry first => reject ACCEPT; candidate remains dissolved.|
|Gameplay action vs 20s timeout|Action committed before expiry => process action. Timeout committed first => locked<br>STOPPED+TIMEOUT; late action rejected.|
|Target selection vs 10s timeout|Target committed first => use it. Timeout committed first => locked self-target; late<br>target rejected.|
|Reconnect exactly at 60s grace|Presence restoration committed before grace expiry => grace clears. Grace expiry<br>committed first => irreversible FORFEIT_PENDING; reconnect cannot restore<br>participation.|



13/31 | Multiplayer Product Design | DRAFT FOR FINAL APPROVAL | Page 13 

13/31 — Multiplayer Product & Match Rules <u>Specification v1.1</u> 

|**Race / illegal action**|**Authoritative outcome**|
|---|---|
|Forfeit trigger while all other players unavailable|At next safe boundary eligible_survivor_count=0 => FORFEIT_RESOLUTION_WAIT;<br>do not create a winner.|
|Staggered 60s expiries A/B/C|Each player has own deadline. First expiry creates pending trigger; wait if zero eligible.<br>Later expiries update forfeited set. First valid reconnect by non-forfeited player can<br>enable finalization.|
|Survivor reconnect just before own expiry|Reconnect commits first => becomes eligible; finalize forfeit with survivor_set<br>determined at that instant.|
|Survivor reconnect just after own expiry|Expiry commits first => player is FORFEIT_PENDING and cannot be eligible; continue<br>wait/abort as applicable.|
|Safe boundary with zero eligible survivors|Enter FORFEIT_RESOLUTION_WAIT; freeze new gameplay; grace timers continue.|
|Normal GAME_ENDED vs forfeit trigger|Whichever irreversible terminal claim commits first governs. GAME_ENDED first =><br>NORMAL_COMPLETION. FORFEIT_PENDING first => forfeit resolution at safe<br>boundary; no later gameplay result overrides it.|
|Integrity abort during FORFEIT_RESOLUTION_WAIT|Integrity abort takes precedence => MATCH_ABORTED / ABORTED_NO_CONTEST.|
|Background + network loss in queue|Apply Section 5.1 ownership; network loss while already backgrounded does not start<br>10s timer.|
|Foreground return while still offline|Switch to disconnect owner with deadline=min(original background deadline,<br>return+10s).|
|Mixed queue ages candidate|Candidate legal only if its size is allowed by every member; otherwise cannot form.|
|Multiple players forfeit before same safe boundary|All committed FORFEIT_PENDING players enter forfeited_players at finalization;<br>survivor count then drives winner_set/result_validity.|
|4p: 3 players eventually forfeit, 1 remains connected|Eligible survivor exists; survivor_set={remaining player}; winner_set={remaining<br>player}; FORFEIT_COMPETITIVE.|
|4p: 1 forfeits, 3 survive|survivor_set={3 players}; winner_set=empty; FORFEIT_ADMINISTRATIVE.|
|3p: 1 forfeits, 2 survive|survivor_set={2 players}; winner_set=empty; FORFEIT_ADMINISTRATIVE.|
|Match completion while identity reconnect is in progress|Terminal match state commits; reconnect completes into result-view state only; no<br>gameplay resurrection.|
|Leave vs network loss|Confirmed Leave commit creates immediate irreversible pending trigger; network state<br>cannot grant grace after that.|
|All players eventually unavailable/forfeit during wait|If no eligible survivor appears and all remaining players reach irreversible expiry =><br>MATCH_ABORTED / NO_CONTEST.|



### **25. Decision Register** 

|**ID**|**Decision**|**Options**|**Approved rule**|**Product impact**|**Status**|
|---|---|---|---|---|---|
|MP-DR-001|Dynamic matchmaking<br>size/eligibility|Always fixed size; dynamic; per-<br>entry eligibility.|Per-entry allowed_group_sizes;<br>candidate intersection; largest<br>legal group; oldest-first.|Queue time, match richness,<br>deterministic fairness.|APPROVED / CLOSED|
|MP-DR-002|Match Found acceptance|Direct create; explicit accept.|Explicit 10s ACCEPT/DECLINE;<br>no auto-accept.|Reduces early no-shows.|APPROVED / CLOSED|
|MP-DR-003|Ready model|Manual READY; derived ready.|JOINED+CONNECTED =><br>READY; 3s auto-start.|Lower friction, deterministic<br>start.|APPROVED / CLOSED|
|MP-DR-004|Initial admission|20/30/45s.|30s shared deadline; any failure<br>cancels match.|Mobile load resilience vs<br>waiting.|APPROVED / CLOSED|
|MP-DR-005|Gameplay timer durations|Alternative action/target times.|20s action; 10s target.|Pace/stall risk.|APPROVED / CLOSED|
|MP-DR-006 / DR-005C|Disconnect grace|Pause; short/long grace.|60s continuous grace; no pause;<br>gameplay timers continue.|Mobile resilience vs<br>abandonment.|APPROVED / CLOSED|
|MP-DR-007|Background policy|Connected; immediate<br>disconnect; temporary<br>unavailable.|TEMPORARILY_UNAVAILABL<br>E; same 60s grace.|Mobile interruptions.|APPROVED / CLOSED|
|MP-DR-008|AFK escalation|Analytics only; warning; forfeit.|3 consecutive connected<br>timeouts => forfeit; warning at 2.|Protect opponents from<br>connected abandonment.|APPROVED / CLOSED|
|MP-DR-009|Intentional Leave|Treat as disconnect; immediate<br>forfeit.|Confirmed Leave => irreversible<br>FORFEIT_PENDING.|Prevents intentional abuse of<br>grace.|APPROVED / CLOSED|
|MP-DR-010|Surrender|Separate surrender; none.|No Surrender; Leave handles<br>exit.|MVP scope reduction.|APPROVED — OUT OF MVP|
|MP-DR-011 / DR-005D|Forfeit continuation|Continue reduced roster; end<br>whole match.|End whole gameplay match; no<br>backfill/reduced roster.|Consistency with immutable<br>roster/gameplay contract.|APPROVED / CLOSED|
|MP-DR-012|Forfeit result|Shared-winner model; score<br>winner; survivor model.|1 survivor => winner; 2+ => no<br>winner, administrative<br>survivor_set.|Competitive integrity/result<br>semantics.|APPROVED / CLOSED|
|MP-DR-013|Forfeit finalization liveness|Finalize on first expiry; wait for<br>eligible survivor.|ELIGIBLE_SURVIVOR gate +<br>FORFEIT_RESOLUTION_WAIT<br>.|Prevents manufactured winner.|APPROVED / CLOSED|
|MP-DR-014|Rematch|Handshake; partial; none.|No rematch in MVP.|Avoid party/session scope.|APPROVED — OUT OF MVP|
|MP-DR-015|Post-match re-entry|Autoqueue; lobby; reason-<br>specific.|Lobby/manual PLAY after real<br>match; limited unaffected pre-<br>start auto-requeue.|Avoid surprise repeat matches.|APPROVED / CLOSED|
|MP-DR-016|Queue liveness precedence|Immediate invalidate;<br>independent timers; owned<br>timer.|30s background; 10s foreground<br>disconnect; no deadline<br>extension.|Queue freshness/mobile<br>resilience.|APPROVED / CLOSED|
|MP-DR-017|Player identity|Mandatory account; persistent<br>guest allowed.|Stable persistent opaque<br>PLAYER_ID; guest allowed.|Reconnect/session continuity.|APPROVED / CLOSED|
|MP-DR-018|MVP access scope|Public/private/party variants.|Public solo matchmaking only.|Task 03 scope control.|APPROVED / CLOSED|



13/31 | Multiplayer Product Design | DRAFT FOR FINAL APPROVAL | Page 14 

13/31 — Multiplayer Product & Match Rules Specification v1.1 

### **26. Edge Case Register** 

|**ID**|**Situation**|**Expected Product Behavior**|**Match/Presence State**|**Result Impact**|**Status**|
|---|---|---|---|---|---|
|MP-EC-001|PLAY tapped twice|Keep one queue entry; second<br>PLAY no-op.|MATCHMAKING|None|DERIVED|
|MP-EC-002|Queue cancel races Match<br>Found|First authoritative event wins;<br>post-commit cancel=DECLINE.|MATCHMAKING/<br>MATCH_FOUND|Candidate may dissolve|APPROVED|
|MP-EC-003|A 27s + B 4s try 2p|Illegal: B allows only 4.|MATCHMAKING|None|APPROVED|
|MP-EC-004|Three entries 12s/17s/29s|3p legal because all allow 3.|MATCHMAKING|Candidate formed|APPROVED|
|MP-EC-005|Five entries all >=20s|Form 4p from four oldest<br>eligible; fifth remains queued.|MATCHMAKING|None|APPROVED|
|MP-EC-006|Exact age tie|Stable queue-entry creation<br>order breaks tie.|MATCHMAKING|None|DERIVED|
|MP-EC-007|Queue background T0, loss T5,<br>foreground offline T20|Background deadline T30 owns;<br>return+10=T30; expire T30.|QUEUE_*_GRACE|Queue entry invalid at T30 if<br>offline|APPROVED|
|MP-EC-008|Queue background T0,<br>foreground offline T5|Effective disconnect expiry<br>min(T30,T15)=T15.|QUEUE_*_GRACE|Queue invalid at T15|APPROVED|
|MP-EC-009|Foreground disconnect then<br>background|Transfer ownership without<br>extending earlier disconnect<br>deadline.|QUEUE_*_GRACE|Deterministic invalidation|DERIVED|
|MP-EC-010|Peer declines Match Found after<br>others accepted|Dissolve candidate; unaffected<br>accepted players auto-requeue<br>with preserved age.|MATCH_FOUND|No match result|APPROVED|
|MP-EC-011|Acceptance disconnect|10s continues; reconnect may<br>accept before deadline only.|MATCH_FOUND|Candidate failure if timeout|APPROVED|
|MP-EC-012|One player never admits|At 30s cancel/no contest;<br>unaffected admitted players may<br>auto-requeue.|WAITING_FOR_ADMISSION|NO_CONTEST|APPROVED|
|MP-EC-013|Disconnect during 3s start<br>countdown|Do not cancel; start match; 60s<br>grace from disconnect time.|MATCH_STARTING|None yet|APPROVED|
|MP-EC-014|Disconnect during own action|20s continues; timeout<br>STOPPED+TIMEOUT if no<br>timely reconnect/action.|MATCH_IN_PROGRESS|Gameplay event irreversible|LOCKED + APPROVED|
|MP-EC-015|Disconnected ACTIVE player<br>targeted|Target remains legal under<br>locked gameplay rules.|MATCH_IN_PROGRESS|None|LOCKED|
|MP-EC-016|Reconnect after action timeout|No rollback; resync post-timeout<br>state.|MATCH_IN_PROGRESS|None|APPROVED|
|MP-EC-017|Connected AFK timeout #2|Warning event; no forfeit yet.|MATCH_IN_PROGRESS|None|APPROVED|
|MP-EC-018|Connected AFK timeout #3|Locked timeout resolves, then<br>FORFEIT_PENDING.|MATCH_IN_PROGRESS|Pending forfeit|APPROVED|
|MP-EC-019|Leave during target selection|Reject later target from leaver;<br>wait for locked self-target/effect<br>resolution, then finalize.|MATCH_IN_PROGRESS|Forfeit|APPROVED|
|MP-EC-020|A grace expires; B/C both<br>disconnected within own grace|At safe boundary eligible=0 =><br>FORFEIT_RESOLUTION_WAIT<br>; no winner.|FORFEIT_RESOLUTION_WAIT|No result yet|APPROVED|
|MP-EC-021|During wait B reconnects; C still<br>within grace|B eligible => finalize;<br>survivor_set={B,C}; winner_set<br>empty.|MATCH_FINISHING|FORFEIT_ADMINISTRATIVE|APPROVED|
|MP-EC-022|During wait C expires, then B<br>reconnects before B expiry|forfeited={A,C};<br>survivor_set={B};<br>winner_set={B}.|MATCH_FINISHING|FORFEIT_COMPETITIVE|APPROVED|
|MP-EC-023|All remaining players expire<br>during wait|MATCH_ABORTED;<br>winner/survivor empty.|MATCH_ABORTED|NO_CONTEST|APPROVED|
|MP-EC-024|4p, one forfeits, three connected<br>survive|survivor_set=3; winner_set<br>empty.|MATCH_FINISHING|FORFEIT_ADMINISTRATIVE|APPROVED|
|MP-EC-025|3p, one forfeits, two survive|survivor_set=2; winner_set<br>empty.|MATCH_FINISHING|FORFEIT_ADMINISTRATIVE|APPROVED|
|MP-EC-026|4p, three forfeit before safe<br>boundary, one survivor|survivor_set=1; winner_set=that<br>player.|MATCH_FINISHING|FORFEIT_COMPETITIVE|APPROVED|
|MP-EC-027|Normal GAME_ENDED commits<br>before disconnect grace expiry|Normal result finalizes; later<br>disconnect cannot create forfeit.|MATCH_FINISHING|NORMAL_COMPETITIVE|DERIVED|
|MP-EC-028|Forfeit trigger commits before<br>GAME_ENDED terminal commit|Forfeit path owns next safe-<br>boundary termination.|MATCH_IN_PROGRESS|Forfeit per survivor rule|DERIVED|
|MP-EC-029|Integrity failure during survivor<br>wait|Abort/no contest takes<br>precedence.|MATCH_ABORTED|NO_CONTEST|APPROVED|
|MP-EC-030|Reconnect exactly after own<br>grace expiry|Expiry first => pending forfeit;<br>reconnect cannot restore.|FORFEIT_PENDING|Forfeit/abort path|APPROVED|
|MP-EC-031|Reconnect exactly before own<br>grace expiry|Reconnect first =><br>connected/eligible if otherwise<br>valid.|IN_MATCH_CONNECTED|May allow finalization|APPROVED|
|MP-EC-032|Match completes while<br>reconnecting|Reconnect enters result view<br>only; no mutation.|MATCH_COMPLETED|Final|DERIVED|
|MP-EC-033|Gameplay ZEROED|<br>Round elimination only; no<br>match forfeit.|MATCH_IN_PROGRESS|Normal gameplay scoring|LOCKED|
|MP-EC-034|Pre-start player presses Leave|Cancel/no contest, not<br>competitive forfeit.|WAITING_FOR_ADMISSION|NO_CONTEST|APPROVED|
|MP-EC-035|Player relaunches app during<br>active match|<br>Same PLAYER_ID reconnects<br>to existing match; must not<br>create new queue identity.|RECONNECTING|None|APPROVED|
|MP-EC-036|Guest identity used|Allowed if stable persistent<br>PLAYER_ID/session ownership<br>requirements are met.|Any|None|APPROVED|
|MP-EC-037|Partial rematch interest|No rematch feature; all return<br>lobby/manual PLAY.|MATCH_COMPLETED|New future match only|OUT OF MVP|



13/31 | Multiplayer Product Design | DRAFT FOR FINAL APPROVAL | Page 15 

13/31 — Multiplayer Product & Match Rules Specification v1.1 

### **27. Dependency Register** 

|**ID**|**Dependency**|**Why required**|**Boundary**|**Status**|
|---|---|---|---|---|
|DEP-MVP-001|Stable persistent identity /<br>PLAYER_ID|Required to resume same player<br>across relaunch/network<br>transitions and enforce one-<br>active-match ownership.|Product requires stable opaque<br>identity; Task 02 does not choose<br>auth technology.|MVP REQUIRED|
|DEP-MVP-002|Authoritative multiplayer session|Required for deterministic<br>state/timer/finality behavior.|Architecture must satisfy product<br>constraints without Task 02<br>prescribing stack.|MVP REQUIRED|
|DEP-MVP-003|Reconnect capability|Required to restore same match<br>state within 60s grace.|Must resync authoritative state;<br>transport/protocol is architecture<br>choice.|MVP REQUIRED|
|DEP-MVP-004|Diagnostic telemetry|Required to distinguish<br>timeout/disconnect/background/le<br>ave/AFK/forfeit/cancel/abort for<br>support and audit.|Telemetry is observational; must<br>not change gameplay.|MVP REQUIRED|
|**ID**|**Future dependency**|**Potential use**|**MVP boundary**|**Status**|
|DEP-FUT-001|Ranked / MMR formula|Future system may consume<br>result_validity, winner_set,<br>survivor_set, forfeited_players.|No rating formula or penalty in Task<br>02.|FUTURE / NON-BLOCKING|
|DEP-FUT-002|Apple/Google/social account login|Possible future identity mechanism.|Not required; persistent guest<br>identity allowed.|FUTURE / NON-BLOCKING|
|DEP-FUT-003|Cross-device account recovery|Future account feature.|No cross-device/reinstall guarantees<br>in MVP.|FUTURE / NON-BLOCKING|
|DEP-FUT-004|Private rooms / friends / invites|Future access mode.|Not Task 03 MVP requirement.|FUTURE / NON-BLOCKING|
|DEP-FUT-005|Premade parties / party<br>matchmaking|Future grouped matchmaking.|Public queue is solo only.|FUTURE / NON-BLOCKING|
|DEP-FUT-006|Spectating|Future observer mode.|No spectator presence/admission in<br>MVP.|FUTURE / NON-BLOCKING|
|DEP-FUT-007|Rematch|Future post-match flow.|OUT OF MVP.|FUTURE / NON-BLOCKING|
|DEP-FUT-008|Surrender|Future explicit action if desired.|OUT OF MVP; Leave exists.|FUTURE / NON-BLOCKING|
|DEP-FUT-009|Push notifications|Potential reconnect/queue support.|Implementation not required by this<br>specification.|FUTURE / NON-BLOCKING|
|DEP-FUT-010|Discipline / cooldown system|May react to declines/leaves/forfeits.|No cooldown/penalty rules in MVP<br>spec.|FUTURE / NON-BLOCKING|



### **28. Non-Goals** 

- Software/backend architecture or service decomposition. 

- Server technology, cloud provider, database schema, cache/queue technology, networking protocol, or WebSocket implementation. 

- UX/UI screen layout, art, animation, audio, or presentation details. 

- Monetization. 

- Ranked/MMR formula or matchmaking skill algorithm. 

- Anti-cheat implementation. 

- Private rooms, friends/invites, premade parties, spectating, rematch, surrender, account linking, or cross-device recovery for MVP. 

These areas may receive product constraints only where necessary to make match behavior deterministic. 

### **29. Definition of Done — Product Contract Check** 

|**Acceptance question**|**Answer**|
|---|---|
|Can product determine what happens from PLAY to queue?|YES — Sections 5, 22, 23.|
|Can it determine legal 2/3/4 candidate formation for mixed queue ages?|YES — per-entry intersection rule in Section 5.|
|Is queue background/network overlap deterministic?|YES — Section 5.1 / MP-DR-016.|
|Is Match Found acceptance exact?|YES — 10s explicit ACCEPT/DECLINE.|
|Is roster commitment exact?|YES — MATCH_CREATED after all accept; roster immutable.|
|Is admission/start exact?|YES — 30s shared admission; derived READY; 3s start.|
|Are gameplay timer durations and locked outcomes exact?|YES — 20s/10s; Task 01 outcomes unchanged.|
|Is disconnect/reconnect exact?|YES — 60s continuous grace, no pause, no rollback.|
|Is background/suspend exact?|YES — TEMPORARILY_UNAVAILABLE + same grace.|
|Is connected AFK escalation exact?|YES — warning at 2, forfeit pending at 3; disconnect-window timeouts excluded.|
|Is intentional Leave exact?|YES — confirmed Leave creates irreversible pending forfeit.|
|Is match-level forfeit separate from gameplay elimination?|YES — Sections 16–17.|
|Can product decide whether a pending forfeit may finalize?|YES — ELIGIBLE_SURVIVOR gate.|



13/31 | Multiplayer Product Design | DRAFT FOR FINAL APPROVAL | Page 16 

13/31 — Multiplayer Product & Match Rules <u>Specification v1.1</u> 

|**Acceptance question**|**Answer**|
|---|---|
|Can zero eligible survivors create a winner?|NO — enter FORFEIT_RESOLUTION_WAIT; eventual no-contest if all expire.|
|Can product determine result with 1 survivor?|YES — unique winner, FORFEIT_COMPETITIVE.|
|Can product determine result with 2+ survivors?|YES — winner_set empty, survivor_set all non-forfeited,<br>FORFEIT_ADMINISTRATIVE.|
|Are cancellation/no-contest cases exact?|YES — Section 18.|
|Is result schema/finality exact?|YES — Section 19.|
|Is rematch behavior exact?|YES — OUT OF MVP.|
|Is stable identity explicit?|YES — persistent opaque PLAYER_ID.|
|Is MVP access scope explicit?|YES — public solo only.|
|Can every listed race be resolved without engineering inventing product rules?|YES — Section 24 and Edge Case Register.|



### **30. Remaining Open Decisions After v1.1** 

##### **Task 03 blocking status** 

Status: **NO BLOCKING PRODUCT DECISIONS** No match-lifecycle, timer, identity, matchmaking, reconnect, forfeit, or result-finalization decision remains open after Revision Request 02. 

The following are future-product dependencies only and are NON-BLOCKING FOR TASK 03: Ranked/MMR formula; registered/social account upgrades; cross-device account recovery; private rooms; friends/invites; parties; spectating; rematch; surrender; push notifications; discipline/cooldown system. 

If a future change introduces one of those features, it requires a separate approved change request and must not silently alter this v1.1 match contract. 

### **31. Handoff Requirements for Software Architecture** 

Task 03 must accept the following as locked product constraints. This section states required behavior/capabilities only; it does not prescribe implementation technology. 

|**Locked product constraint**|**Required Task 03 input**|
|---|---|
|MVP access|Public solo matchmaking only; no private rooms, parties, friends/invites, spectating,<br>surrender, or rematch.|
|Identity|Stable persistent opaque PLAYER_ID required; persistent guest identity allowed.|
|Queue ownership|One active QUEUE_ENTRY per PLAYER_ID; stable queue age and deterministic tie<br>ordering.|
|Dynamic matchmaking|Legal 2–4 candidates use per-entry allowed_group_sizes intersection; largest legal<br>group; oldest-first.|
|Queue liveness|30s background grace; 10s foreground disconnect grace; deterministic<br>ownership/deadline precedence with no extension.|
|Match Found|Explicit ACCEPT/DECLINE, 10s, no auto-accept; unaffected peer-failure requeue may<br>preserve priority.|
|MATCH_ROSTER|Immutable after MATCH_CREATED; seats assigned per locked gameplay contract; no<br>backfill/replacement.|
|Admission/start|30s shared admission; no manual READY; JOINED+connected => READY; 3s auto-<br>start.|
|Authoritative time|Single authoritative expiry for all timers; stale post-expiry input cannot roll back state.|
|Gameplay timers|20s normal action; 10s target selection; locked Task 01 expiry outcomes unchanged.|
|Reconnect|60s continuous in-match unavailability grace; same PLAYER_ID resync; no general<br>match pause.|
|Presence separation|Presence states remain independent from gameplay round states and targetability.|
|Background|In-match background/suspend=TEMPORARILY_UNAVAILABLE using same 60s<br>grace.|
|AFK|3 consecutive fully-connected decision timeouts => FORFEIT_PENDING; warning at 2;<br>manual decision resets.|
|Leave|Confirmed Leave => irreversible FORFEIT_PENDING; no reconnect grace.|
|Forfeit continuation|Finalized player forfeit ends whole gameplay match; no reduced-roster continuation.|
|Safe-boundary gate|Forfeit never interrupts locked pending decisions/effect atomicity; finalize only at safe<br>boundary.|
|Eligible survivor gate|At safe boundary, require >=1 ELIGIBLE_SURVIVOR; otherwise<br>FORFEIT_RESOLUTION_WAIT freezes gameplay while individual grace timers<br>continue.|
|Forfeit results|1 non-forfeited survivor => unique winner/FORFEIT_COMPETITIVE; 2+ => winner_set<br>empty/FORFEIT_ADMINISTRATIVE; never infer a multi-survivor winner.|
|No-survivor resolution|If all remaining players irreversibly expire with zero eligible survivor =><br>MATCH_ABORTED / ABORTED_NO_CONTEST.|
|Cancellation/no contest|Pre-start admission/session failures => MATCH_CANCELLED /<br>CANCELLED_NO_CONTEST; post-start integrity failure => MATCH_ABORTED /<br>ABORTED_NO_CONTEST.|



13/31 | Multiplayer Product Design | DRAFT FOR FINAL APPROVAL | Page 17 

||13/31 — Multiplayer Product&Match Rules Specification v1.1|
|---|---|
|**Locked product constraint**|**Required Task 03 input**|
|Result object|Must preserve match_id, roster, seat_order, termination_type, winner_set,<br>survivor_set, forfeited_players, final_total_scores, result_validity, finalization_reason<br>(plus forfeit reasons when relevant).|
|Finality/races|Terminal result immutable; first authoritative committed event/state transition governs<br>races.|
|Observability|Must distinguish timeout, disconnect, background, reconnect, AFK, Leave, forfeit,<br>cancellation, abort, and finalization causes for diagnostics.|



### **Appendix A. DR-005C / DR-005D Traceability** 

|**Legacy decision**|**Inherited locked constraint**|**v1.1 mapping**|**Closure**|**Status**|
|---|---|---|---|---|
|DR-005C — Disconnect /<br>Reconnect Grace|Disconnect alone does not change<br>gameplay round_state; v1.1<br>gameplay spec defers grace/pause<br>policy.|MP-DR-006 + MP-DR-007 +<br>Sections 9–12|60s continuous unavailability; no<br>pause; timers continue; targetability<br>unchanged; reconnect before expiry<br>resyncs; expiry creates irreversible<br>FORFEIT_PENDING.|APPROVED / CLOSED|
|DR-005D — Match Forfeit Policy|Gameplay spec defers<br>trigger/result/ranked policy and<br>requires atomic handoff with<br>pending gameplay decisions.|MP-DR-008/009/011/012/013 +<br>Sections 13–19|Triggers: confirmed Leave, 60s<br>unavailability, 3 connected AFK<br>timeouts. Safe-boundary finalization<br>+ eligible-survivor gate. Whole<br>match ends; 1 survivor wins, 2+<br>survivors no winner, zero eligible/all<br>expire => no-contest abort.|APPROVED / CLOSED|



### **Appendix B. Compact Product Transition Reference** 

|**From**|**Trigger**|**Condition**|**To / Action**|
|---|---|---|---|
|LOBBY|PLAY|Eligible PLAYER_ID|Create QUEUE_ENTRY →<br>MATCHMAKING|
|MATCHMAKING|Cancel|Still queued|Invalidate entry → LOBBY|
|MATCHMAKING|Legal candidate found|Intersection eligibility + priority|MATCH_FOUND_PENDING_ACCEPTAN<br>CE|
|MATCH_FOUND|All ACCEPT|Before 10s|MATCH_CREATED|
|MATCH_FOUND|Any DECLINE/timeout|Any|Dissolve; failing player lobby; unaffected<br>auto-requeue|
|MATCH_CREATED|Created|2–4 accepted players|Start 30s admission|
|WAITING_FOR_ADMISSION|All READY|Before deadline|MATCH_READY|
|WAITING_FOR_ADMISSION|30s expires|Any member not READY|MATCH_CANCELLED / NO_CONTEST|
|MATCH_READY|System|All ready|MATCH_STARTING 3s|
|MATCH_STARTING|3s expires|Any presence|MATCH_IN_PROGRESS|
|MATCH_IN_PROGRESS|Decision timeout|Normal action|Locked STOPPED + TIMEOUT|
|MATCH_IN_PROGRESS|Target timeout|Target decision|Locked auto-self-target|
|MATCH_IN_PROGRESS|Disconnect/background|Not already pending forfeit|60s grace; no pause|
|MATCH_IN_PROGRESS|Grace expiry / confirmed Leave / AFK-3|Forfeit trigger|FORFEIT_PENDING; reject new<br>decisions from pending player|
|MATCH_IN_PROGRESS|Safe boundary + pending|eligible_survivor_count>=1|MATCH_FINISHING /<br>FORFEIT_COMPLETION|
|MATCH_IN_PROGRESS|Safe boundary + pending|eligible_survivor_count=0|FORFEIT_RESOLUTION_WAIT|
|FORFEIT_RESOLUTION_WAIT|Non-forfeited player reconnects|Before own expiry|Eligible survivor appears →<br>FORFEIT_COMPLETION|
|FORFEIT_RESOLUTION_WAIT|All remaining expire|No eligible survivor|MATCH_ABORTED /<br>ABORTED_NO_CONTEST|
|MATCH_IN_PROGRESS|Locked GAME_ENDED|No earlier pending forfeit owns terminal<br>path|MATCH_FINISHING /<br>NORMAL_COMPLETION|
|MATCH_IN_PROGRESS / WAIT|Integrity failure|Authoritative result cannot be trusted|MATCH_ABORTED /<br>ABORTED_NO_CONTEST|
|MATCH_FINISHING|Result validated|Fields consistent|MATCH_COMPLETED|
|Terminal result|Return|Complete|LOBBY; manual PLAY|



### **Appendix C. Source-to-Spec Consistency Notes** 

|**Locked topic**|**Source**|**Product section**|**Consistency rule**|
|---|---|---|---|
|Supported 2–4 players|Official Rules / Digital v1.1|Sections 1, 5|No change; matchmaking creates only<br>legal 2/3/4 rosters.|
|MATCH_ROSTER immutable|Digital v1.1 Section 1|Sections 7, 16, 31|No backfill; forfeit ends whole match.|
|Connection state orthogonal|Digital v1.1 Section 4.1|Sections 10, 17, 22|Disconnect/background never mutate<br>ACTIVE/STOPPED/etc.|



13/31 | Multiplayer Product Design | DRAFT FOR FINAL APPROVAL | Page 18 

13/31 — Multiplayer Product & Match Rules <u>Specification v1.1</u> 

|**Locked topic**|**Source**|**Product section**|**Consistency rule**|
|---|---|---|---|
|Normal action timeout|Digital v1.1 DR-005A|Section 9|Task 02 sets 20s only;<br>STOPPED+TIMEOUT unchanged.|
|Target timeout|Digital v1.1 DR-005B|Section 9|Task 02 sets 10s only; automatic self-<br>target unchanged.|
|Started-effect atomicity|Digital v1.1 DR-009|Sections 14, 16|Forfeit waits for safe boundary; started<br>resolution completes.|
|Game end / tie-break|Digital v1.1 Section 20|Sections 19, 24|Normal completion uses locked unique<br>winner; product forfeit is separate.|
|Round states|Digital v1.1 Section 4|Section 17|Never reused as match-level forfeit<br>states.|



### **32. Final Consistency Audit** 

|**Audit**|**Requirement**|**Result**|
|---|---|---|
|1|No conflict with locked Digital Game Rules<br>Specification v1.1.|PASS — product-layer rules preserve gameplay<br>states, timeout outcomes, targetability, effect<br>atomicity, round/game end and tie-break.|
|2|DR-005C resolved.|PASS — 60s continuous reconnect/unavailability<br>grace, no global pause, gameplay timers continue.|
|3|DR-005D resolved.|PASS — explicit triggers, safe-boundary finalization,<br>whole-match termination, result model and no-<br>contest fallback.|
|4|No multi-survivor winner semantics.|PASS — 2+ non-forfeited survivors => winner_set<br>empty; survivor_set only.|
|5|No race can create winner with zero eligible<br>survivors.|PASS — FORFEIT_RESOLUTION_WAIT blocks<br>finalization until eligible survivor appears or all expire<br>into no-contest abort.|
|6|Matchmaking candidate legality deterministic.|PASS — per-entry allowed_group_sizes intersection<br>+ largest legal group + oldest-first + stable tie order.|
|7|Identity requirement explicit.|PASS — stable persistent opaque PLAYER_ID;<br>persistent guest identity allowed.|
|8|MVP access scope explicit.|PASS — public solo matchmaking only;<br>private/friends/parties/spectating excluded.|
|9|Queue background/network precedence<br>deterministic.|PASS — ownership/deadline rules prevent<br>overlapping timers from extending queue occupancy.|
|10|No remaining product decision blocks Software<br>Architecture.|PASS — all MP-DR decisions approved/closed or<br>approved out of MVP; future dependencies are non-<br>blocking.|



_End of 13/31 — Multiplayer Product & Match Rules Specification v1.1. Document status: DRAFT FOR FINAL APPROVAL. Upon Product Owner final approval, this specification is ready to serve as authoritative product input to Task 03 — Authoritative Multiplayer Technical Architecture._ 

13/31 | Multiplayer Product Design | DRAFT FOR FINAL APPROVAL | Page 19 

