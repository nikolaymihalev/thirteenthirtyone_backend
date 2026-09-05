# **13/31** 

## **Digital Game Rules Specification v1.1** 

Mobile Full Online Multiplayer - iOS / Android 

#### **Document status: DRAFT FOR FINAL APPROVAL** 

Role: Senior Systems Game Designer 

Date: 5 September 2026 

#### **Normative sources** 

|**Source**||**Authority**|**Use**<br>||
|---|---|---|---|---|
|13/31 Rules.pdf (16 pp.)||Primary source of truth|Oficial tabletop m<br>scoring,round and|echanics, card behavior,<br> game end.|
|13/31 - Digital Game Rules S|pecifcation v1.0|Revision baseline|Existingdigital form|alization and state model.|
|Revision Request 01 - v1.1<br>Interpretation rule: oficial<br>digital rules for v1.1. No oth<br>**Revision Summary**|tabletop rules rem<br>er mechanic or ba<br>**v1.0 -> v1.1**|Approved product decisions / mandatory<br>scope<br>ain the mechanical source of truth. App<br>lance change is introduced silently.|revision<br>Closes listed decis<br>corrections for aut<br>roved decisions in Revision|ions and requires structural<br>horitative online rules.<br>Request 01 are normative|
|**Change ID**|**Section**|**Change**<br>|**Reason**|**Status**|
|CH-001|0,2|Player count fxed at 2-4.|Approved DR-001.|APPROVED / CLOSED|
|||Uniform random seat|||
|CH-002|1-3, 20|<br>assignment; immutable seat<br>ring; random Round 1 starter;<br>explicit later-round rotation.|Approved DR-002/003.|APPROVED / CLOSED|
|CH-003|3|Opening deal order made<br>deterministic from round starter,<br>per-player fltered draw.|Approved DR-004.|APPROVED / CLOSED|
|CH-004|3|On-demand deck refll and<br>NUMERICAL_DECK_DEADLOCK<br>terminal rule integrated.|Approved DR-006/011.|APPROVED / CLOSED|
|CH-005|1, 5, 15-17|Added<br>EFFECT_DRAWER/EFFECT_SOU<br>RCE and separated<br>TURN_OWNER,<br>DRAW_RECIPIENT,<br>DECISION_OWNER,<br>EFFECT_TARGET.|Structural correction.|APPROVED|
|CH-006|5, 15|Nested DRAW 2 is<br>depth-frst/LIFO with<br>independent quotas; normal<br>DRAW resumes after self-target<br>DRAW 2.<br>|Approved DR-007/008.|APPROVED / CLOSED|
|CH-007|15, 17-18|Efect atomicity rewritten using<br>efect context and efect drawer;<br>added nested examples.|Approved DR-009 direction.|APPROVED / CLOSED|
|CH-008|20|Tie-break participants, repeated<br>ties, score storage, original seat<br>ringand starter rules formalized.|Approved DR-010 + tie-break<br>starter scope.|APPROVED / CLOSED|
|CH-009|4, 5, 16, 22|DR-005 split into action timeout,<br>target timeout, reconnect grace,<br>and forfeitpolicy.|Separates game-rule behavior<br>from multiplayer product policy.|A/B CLOSED; C/D OPEN|
|CH-010|2, 5, App. A|Removed TURN_IN_PROGRESS;<br>canonical turn state naming is<br>now unique.|Consistency correction.|APPROVED|



13/31 - Digital Game Rules Specification v1.1 | Systems Design 

### **0. Document Control and Normative Language** 

This specification defines the authoritative digital game-rules model for 13/31. It specifies deterministic gameplay state transitions. It does not define UX/UI, networking architecture, matchmaking implementation, persistence technology, anticheat systems, or ranked policy except where a deterministic gameplay rule is required. 

|**Term**|**Meaning**<br>|
|---|---|
|SHALL / MUST|Normative behavior required by an oficial rule, unavoidable derivation, or<br>approved v1.1 decision.|
|SHALL NOT / MUST NOT|Forbiddengameplaytransition or state mutation.|
|MAY|Allowed behavior that does not alter authoritative gameplay state unless<br>explicitlystated.<br>|
|CONFIRMED|Directlystated bythe oficial rules.<br>|
|DERIVED|A digital formalization that follows necessarily from confrmed rules without<br>changingthe mechanic.|
|APPROVED / CLOSED|A former ambiguity resolved by Revision Request 01 and integrated into<br>normative rules.<br>|
|DECISION REQUIRED / OPEN|Not uniquelydefned and not approved in this revision.|



#### **0.1 v1.1 approval posture** 

All gameplay-rule ambiguities from v1.0 concerning seats, round starters, opening deal order, deck refill, nested DRAW 2, DRAW 2 self-target, effect atomicity, tie-break continuation, and numerical-deck deadlock are closed and integrated below. Only multiplayer product-policy items DR-005C and DR-005D remain open; they are intentionally deferred to the Multiplayer Product & Match Rules Specification. 

### **1. Core Game Objects and Authoritative Roles** 

|**Object / Field**|**Defnition**|
|---|---|
|MATCH_ROSTER|Complete set of 2-4 players accepted for the match. Immutable after<br>MATCH_CREATED forgame-rulespurposes.|
|SEAT_INDEX|Immutable integer Seat 0 ... Seat N-1 assigned uniformly at random at<br>MATCH_CREATED.|
|SEAT_ORDER / SEAT_RING|Clockwise ringdefned strictlybyascendingseat index: 0 -> 1 -> ... -> N-1 -> 0.<br>|
|ROUND_STARTER|Participating player who receives the frst normal turn of the round and from<br>whom openingdeal order begins.|
|DRAW_PILE|Shared face-downpile used forphysical card draws.<br>|
|DISCARD_PILE|Shared pile containing fully resolved efect cards and, after ROUND_END,<br>numerical cards used in that round.|
|OPENING_SET_ASIDE|Efect cards encountered during opening deal; not resolved or discarded; shufled<br>back into DRAW_PILE after allparticipating players receive an openingnumber.|
|NUMBER_HISTORY|Ordered list of numerical cards received by one player during the current round.<br>Efect cards never enter this list.|
|CURRENT_SCORE|Mutable current-round score. Opening number initializes it; numerical cards add<br>value;+5/-5 modifyit.|
|ROUND_SCORE|Final score awarded for a normal or tie-break round result: current_score when<br>stopped,50 for PERFECT_31,0 for bust or ZERO.|
|TOTAL_SCORE|Sum of ROUND_SCORE values from completed normal rounds only. Tie-break<br>rounds never modifyTOTAL_SCORE.|
|TIEBREAK_ROUND_RESULT|Separate stored ROUND_SCORE result for a tie-break round; used only to<br>narrow/resolve tied leaders.|
|TURN_OWNER|ACTIVE participating player whose normal turn is currently being resolved. Forced<br>draws never replace TURN_OWNER.|
|DRAW_RECIPIENT|Player required to receive the next physical card in the top draw context. May<br>difer from TURN_OWNER duringDRAW 2.<br>|
|EFFECT_DRAWER / EFFECT_SOURCE|Player who physically drew the efect card that created the current efect-<br>resolution context. This player is not assumed to be TURN_OWNER. Canonical<br>implementation name: EFFECT_DRAWER.|
|DECISION_OWNER|Player responsible for choosing EFFECT_TARGET for the current efect. For all<br>current efect cards,DECISION_OWNER = EFFECT_DRAWER.<br>|
|EFFECT_TARGET|Exactlyone ACTIVEplayer selected as the target of the current efect.|
|SOURCE_EFFECT_CARD|Specifc efect-card instance whose efect context is resolving. Distinct from<br>EFFECT_DRAWER,which is aplayer.|
|DRAW_CONTEXT|Stack frame that requires numerical cards for one recipient. Types:<br>NORMAL_DRAW(remaining_numbers=1)or DRAW_2(remaining_numbers=2).<br>|
|EFFECT_CONTEXT|Stack frame created when an efect card is drawn. Stores source card,<br>EFFECT_DRAWER, DECISION_OWNER, selected EFFECT_TARGET and child<br>context(s)if any.|



13/31 - Digital Game Rules Specification v1.1 | Systems Design 

#### **1.1 Role-separation invariant** 

The server SHALL NOT infer effect ownership from the normal turn. At every effect resolution the roles TURN_OWNER, DRAW_RECIPIENT, EFFECT_DRAWER, DECISION_OWNER and EFFECT_TARGET are stored independently. 

Example: Player A is TURN_OWNER. A previously played DRAW 2 on B. While B is the DRAW_RECIPIENT, B draws another DRAW 2. For that new effect: TURN_OWNER=A; DRAW_RECIPIENT=B; EFFECT_DRAWER=B; DECISION_OWNER=B; EFFECT_TARGET=the ACTIVE player chosen by B. 

#### **1.2 Core invariants** 

- Only a PARTICIPATING + ACTIVE player may take a normal turn. 

- Only ACTIVE players may be selected as EFFECT_TARGET. 

- A terminal round state never returns to ACTIVE within the same round. 

- An ACTIVE player cannot remain at score 31 or higher after a resolution checkpoint: 31 immediately becomes PERFECT_31; 32+ immediately becomes BUST_OVER_31. 

- Effect cards never modify NUMBER_HISTORY and never participate in Rule 13. 

- Numerical cards remain associated with their player until ROUND_END, including after that player becomes inactive. 

- Gameplay resolution is sequential and authoritative. While one gameplay decision is pending, no other gameplay decision mutates the same match state. 

- A physical numerical card can satisfy only the remaining_numbers counter of the DRAW_CONTEXT that directly received it; it never double-counts toward ancestor contexts. 

- Resolved effect cards enter DISCARD_PILE only after their entire effect context, including child/nested draw contexts, has completed. 

### **2. Full Game Session Lifecycle** 

|**State**|**Entry condition**|**Requiredprocessing**|**Next state**|
|---|---|---|---|
|MATCH_CREATED|Authoritative server receives complete<br>roster.|Validate 2-4 players. Uniformly randomize<br>roster into Seat 0..N-1. Seat ring becomes<br>immutable.|PLAYERS_JOINED|
|PLAYERS_JOINED|All expected players are admitted to<br>session.|Track connection status separately from<br>gameplayround state.<br>|GAME_SETUP|
|GAME_SETUP|Roster and seats fxed.|Create/shufle 112-card deck; set<br>TOTAL_SCORE=0; choose Round 1 starter<br>uniformlyat random amongseats.|ROUND_SETUP|
|ROUND_SETUP|Normal or tie-break round is to begin.|Set participation set; reset round felds;<br>perform opening deal from<br>ROUND_STARTER clockwise; restore set-<br>aside efects.|TURN_START|
|TURN_START|Round has >=1 ACTIVE participant.|<br>Assign TURN_OWNER to the required<br>ACTIVE seat for this turn.|WAITING_FOR_PLAYER_ACTION|
|WAITING_FOR_PLAYER_ACTION|TURN_OWNER is ACTIVE.|Accept DRAW or STOP; timer expiry<br>follows DR-005A.|Resolution state / END_TURN|
|END_TURN|No unresolved efect/draw context<br>remains.|Clear current turn context.|ROUND_END_CHECK|
|ROUND_END_CHECK|Stable resolution boundary.|If any ACTIVE participant remains, choose<br>next turn owner;otherwise end round.|TURN_START / ROUND_END|
|ROUND_END|No ACTIVE participants and no started<br>efect remains unresolved.|Finalize round, move numerical histories<br>to discard.|SCORES_UPDATED /<br>TIEBREAK_RESULT_EVAL|
|SCORES_UPDATED|Normal round only.|Add each normal ROUND_SCORE to<br>TOTAL_SCORE.|GAME_END_CHECK|
|GAME_END_CHECK|Normal score update complete.|If max total <150, start next normal round.<br>If threshold reached, resolve unique high<br>or tie.|ROUND_SETUP / TIEBREAK_SETUP /<br>GAME_ENDED|
|TIEBREAK_SETUP|At least two tied highest players remain.|Use original seat ring; set only tied leaders<br>as participants; determine starter by<br>rotation;run normal round mechanics.|ROUND_SETUP|
|TIEBREAK_RESULT_EVAL|Tie-break round complete.|Compare tie-break ROUND_SCORE only.<br>Unique high wins; tied high subset<br>continues. Do not modifyTOTAL_SCORE.|GAME_ENDED / TIEBREAK_SETUP|
|GAME_ENDED|Unique winner determined.|Freeze authoritativegameplayresult.|Terminal|



#### **2.1 Seat assignment, seat order, and initial starter** 

**1.** At MATCH_CREATED, authoritative server SHALL uniformly randomize the complete 2-4 player roster into Seat 0 ... Seat N-1. 

**2.** Seat assignment and the resulting SEAT_RING are immutable for the entire match, including tie-break rounds. 

**3.** Clockwise progression always means ascending seat order with wraparound: Seat 0 -> Seat 1 -> ... -> Seat N-1 -> Seat 0. 

13/31 - Digital Game Rules Specification v1.1 | Systems Design 

**4.** Round 1 ROUND_STARTER SHALL be selected uniformly at random from all participating seats, independently of seat assignment. 

**5.** For each later normal round, candidate starter is the seat immediately clockwise after the previous normal round starter. Because all matched players participate in normal rounds, candidate starter is the new ROUND_STARTER. 

**DR-001 - Supported player count** 

**Status:** APPROVED / CLOSED 

**Normative rule:** v1 supports exactly 2-4 players. MATCH_CREATED SHALL reject or not create gameplay sessions outside this range. 

##### **DR-002 - Seat order and initial first player** 

**Status:** APPROVED / CLOSED 

**Normative rule:** Uniform random seat assignment at MATCH_CREATED; immutable ascending seat ring; independent uniformly random Round 1 starter. 

##### **DR-003 - First player in later rounds** 

**Status:** APPROVED / CLOSED 

**Normative rule:** Rotate ROUND_STARTER one seat clockwise after each normal round. Tie-break adaptation is defined in Section 20.2. 

### **3. Round Lifecycle** 

**1.** Determine round participation. Normal round: every matched player participates. Tie-break round: only currently tied highest players participate; all others are NOT_PARTICIPATING for that round. 

**2.** Set each participating player round_state=ACTIVE, current_score=0, round_score=unset, NUMBER_HISTORY empty, termination_reason=NONE. 

**3.** Determine ROUND_STARTER using Section 2.1 for normal rounds or Section 20.2 for tie-break rounds. 

**4.** Perform the opening deal from ROUND_STARTER clockwise through participating seats only. Complete each player's filtered opening draw before moving to the next participating seat. 

**5.** After all participating players have an opening numerical card, shuffle all OPENING_SET_ASIDE effect cards back into DRAW_PILE. 

**6.** Enter TURN_START with ROUND_STARTER as the first TURN_OWNER if still ACTIVE. 

**7.** Round continues until every participant is non-ACTIVE. A sole remaining ACTIVE player continues taking turns; the round does not auto-end merely because only one remains. 

#### **3.1 Opening deal - normative filtered draw** 

- For the current participating player, draw physical cards until a numerical card is obtained. 

- Every encountered effect card is placed in OPENING_SET_ASIDE. It is not executed and does not enter DISCARD_PILE. 

- Do not advance to the next participating seat until the current player has received exactly one opening numerical card. 

- Append that numerical card to NUMBER_HISTORY and set CURRENT_SCORE to its value. No Rule 13 check is possible because the player has only one number. 

- After every <u>participant has one opening number, shuffle all set-aside effects back into DRAW_PILE as one operation.</u> 

##### **DR-004 - Opening deal order** 

**Status:** APPROVED / CLOSED 

**Normative rule:** Opening deal begins at current ROUND_STARTER and proceeds clockwise through participating seats. Each player's filtered draw fully completes before the next seat is processed. 

#### **3.2 Deck continuity and on-demand refill** 

The match uses one continuing draw/discard cycle. The 112-card deck is created and shuffled at GAME_SETUP; it is not automatically rebuilt to 112 cards at each round. 

- Fully resolved effect cards enter DISCARD_PILE immediately after their complete effect context resolves. 

13/31 - Digital Game Rules Specification v1.1 | Systems Design 

- Numerical cards remain in player NUMBER_HISTORY until ROUND_END and then all move to DISCARD_PILE. 

- OPENING_SET_ASIDE effects are not discard; they return directly to DRAW_PILE after opening deal completes. 

- Before every physical DRAW_CARD: if DRAW_PILE contains at least one card, draw its top card. If DRAW_PILE is empty, shuffle the entire current DISCARD_PILE to create the new DRAW_PILE, then continue. 

- The engine SHALL NOT pre-emptively reshuffle merely because a DRAW 2 or other sequence may require multiple future <u>physical cards.</u> 

##### **DR-006 - Deck refill timing** 

**Status:** APPROVED / CLOSED 

**Normative rule:** Use on-demand refill only when DRAW_PILE is physically empty before a physical draw. No pre-emptive "insufficient cards" refill. 

#### **3.3 NUMERICAL_DECK_DEADLOCK** 

Before each physical draw for a DRAW_CONTEXT that still requires one or more numerical cards, the server SHALL test numerical-card availability across DRAW_PILE and DISCARD_PILE. If both contain zero numerical cards, enter NUMERICAL_DECK_DEADLOCK immediately, even if effect cards remain physically drawable. 

**1.** Set round_state=FORCED_STOP for every still-ACTIVE participating player. 

**2.** For each such player set round_score=current_score and termination_reason=NUMERICAL_DECK_DEADLOCK. 

**3.** Cancel all pending draw obligations and unwind all pending draw/effect contexts without drawing additional cards. 

**4.** Proceed through ROUND_END_CHECK -> ROUND_END standard flow. 

**5.** Emit mandatory telemetry event NUMERICAL_DECK_DEADLOCK with match/round identifiers and card-zone counts. Telemetry is observational and does not alter gameplay state. 

##### **DR-011 - No numerical cards available** 

**Status:** APPROVED / CLOSED 

**Normative rule:** If a pending numerical obligation exists and numerical-card count is zero in both DRAW_PILE and DISCARD_PILE, force-stop all remaining ACTIVE players with current scores and end the round through standard flow. 

### **4. Player Round States and Connection State** 

|**Round state**|**Can take normal turn?**|**Valid efect target?**|**Round score**|**Return ACTIVE?**|**Round-end role**|
|---|---|---|---|---|---|
|ACTIVE|Yes,when selected.|Yes.|Unset until terminal.|N/A|Prevents round end.|
|STOPPED|No.|No.|current_score|No|Inactive.<br>termination_reason<br>distinguishes<br>PLAYER_CHOICE vs<br>TIMEOUT.|
|FORCED_STOP|No.|No.|current_score|No|Inactive. Used by STOP<br>card and deadlock rule;<br>reason identifes cause.|
|BUST_13|No.|No.|0|No|Inactive.|
|BUST_OVER_31|No.|No.|0|No|Inactive.|
|PERFECT_31|No.|No.|50|No|Inactive.|
|ZEROED|No.|No.|0|No|Inactive.|
|NOT_PARTICIPATING|No.|No.|N/A for this round|No within this round|Does not prevent tie-break<br>round end.|



#### **4.1 Connection state is orthogonal** 

|**Connection state**|**Gameplay meaning**|
|---|---|
|CONNECTED|Player may receive prompts and submit decisions. round_state determines<br>gameplaylegality.|
|DISCONNECTED|No automatic score or round-state change by itself. The player retains<br>underlying round_state until an approved timer/reconnect/forfeit policy<br>causes a separate action.|



#### **4.2 termination_reason** 

Where multiple causes share the same gameplay state, the engine SHOULD store a non-mechanical termination_reason for replay/analytics. This reason does not change targetability or scoring. 

13/31 - Digital Game Rules Specification v1.1 | Systems Design 

|**round_state**|**Example termination_reason**|**Gameplay behavior**|
|---|---|---|
|STOPPED|PLAYER_CHOICE|VoluntarySTOP; preserve current_score.|
|STOPPED|TIMEOUT|Server action timer expired; behavior is voluntary-<br>equivalent STOP.|
|FORCED_STOP|STOP_EFFECT|STOP card caused stop.|
|FORCED_STOP|NUMERICAL_DECK_DEADLOCK|Emergency deadlock rule force-stopped<br>remainingACTIVEplayers.|



#### **4.3 Multiplayer timer decisions** 

##### **DR-005A - Normal Turn Action Timeout** 

**Status:** APPROVED / CLOSED 

**Normative rule:** If ACTIVE TURN_OWNER does not choose DRAW or STOP before the authoritative server action timer expires: set round_state=STOPPED; round_score=current_score; termination_reason=TIMEOUT; then END_TURN. This is gameplay-equivalent to voluntary STOP. 

##### **DR-005B - Effect Target Selection Timeout** 

**Status:** APPROVED / CLOSED 

**Normative rule:** If DECISION_OWNER does not submit a legal target before the authoritative target-selection timer expires, automatically set EFFECT_TARGET=DECISION_OWNER, provided that player is still ACTIVE. All v1 effect cards permit selftarget. 

Target-timeout reachability note: while WAITING_FOR_TARGET_SELECTION is pending, authoritative gameplay resolution is serialized; no other gameplay decision can change DECISION_OWNER from ACTIVE to inactive. DISCONNECTED alone also does not alter round_state. Therefore self-target is deterministic under this specification. If the future Multiplayer Product & Match Rules Specification introduces an asynchronous forfeit/state mutation during a pending target decision, that specification MUST define the atomic handoff before such mutation is enabled; this is part of DR-005D, not a new game mechanic here. 

##### **DR-005C - Disconnect / Reconnect Grace** 

**Status:** OPEN / DECISION REQUIRED 

**Question:** What reconnect grace, pause behavior, or reconnection window applies to DISCONNECTED players? 

**Scope:** No duration or reconnect-grace value is defined in this game-rules revision. Resolve in Multiplayer Product & Match Rules Specification. 

##### **DR-005D - Match Forfeit Policy** 

**Status:** OPEN / DECISION REQUIRED 

**Question:** Does prolonged disconnect/eventual absence cause forfeit, when, how is match result calculated, and what are ranked consequences? 

**Scope:** No forfeit threshold or ranked consequence is defined here. Resolve in Multiplayer Product & Match Rules Specification. 

### **5. Turn Lifecycle and Canonical Turn State Machine** 

TURN_IN_PROGRESS is removed. It had no unique transition responsibility and duplicated the combination of TURN_START plus the explicit resolution substates below. The canonical state names in this section are normative. 

|**State**|**Decision owner**|**Entry /processing**|**Possible next state**|
|---|---|---|---|
|TURN_START|System|Select/assign required ACTIVE<br>TURN_OWNER for the new normal turn.|WAITING_FOR_PLAYER_ACTION|
|WAITING_FOR_PLAYER_ACTION|TURN_OWNER|Accept DRAW or STOP; server timer may<br>expire.|PUSH_NORMAL_DRAW /<br>APPLY_VOLUNTARY_STOP|
|PUSH_NORMAL_DRAW|System|Push NORMAL_DRAW context:<br>recipient=TURN_OWNER,<br>remaining_numbers=1.<br>|DRAW_CARD|
|DRAW_CARD|System|Run numerical-deadlock precheck; refll<br>on demand if pile empty; physically draw<br>top card for top-context<br>DRAW_RECIPIENT.|IDENTIFY_CARD /<br>NUMERICAL_DECK_DEADLOCK|
|IDENTIFY_CARD|System|Classify drawn card as NUMBER or<br>EFFECT.|ADD_NUMBER_CARD /<br>CREATE_EFFECT_CONTEXT|
|CREATE_EFFECT_CONTEXT|System|Create EFFECT_CONTEXT; assign role<br>felds per Section 1; EFFECT_DRAWER is<br>current DRAW_RECIPIENT.|WAITING_FOR_TARGET_SELECTION|



13/31 - Digital Game Rules Specification v1.1 | Systems Design 

|**State**|**Decision owner**|**Entry /processing**|**Possible next state**|
|---|---|---|---|
|WAITING_FOR_TARGET_SELECTION|DECISION_OWNER|Select exactly one ACTIVE target; on timer<br>expiryuse DR-005B.<br>|APPLY_EFFECT|
|APPLY_EFFECT|System|Apply efect atomically. DRAW 2 creates<br>child DRAW_CONTEXT; +5/-5/STOP/ZERO<br>applydirect target rules.|DRAW_CARD / POST_RESOLUTION|
|ADD_NUMBER_CARD|System|Append number to direct<br>DRAW_RECIPIENT history; add value;<br>decrement only top DRAW_CONTEXT<br>remaining_numbers.|CHECK_13|
|CHECK_13|System|If last two numerical values sum to 13,<br>terminal bust.|POST_RESOLUTION / CHECK_OVER_31|
|CHECK_OVER_31|System|If current_score >=32, terminal bust.|POST_RESOLUTION /<br>CHECK_PERFECT_31|
|CHECK_PERFECT_31|System|If current_score ==31, terminal<br>PERFECT_31.|POST_RESOLUTION|
|POST_RESOLUTION|System|Complete/pop top contexts or resume<br>legal parent. Cancel pending obligations<br>of inactive required recipients.|DRAW_CARD / END_TURN|
|APPLY_VOLUNTARY_STOP|System|Set STOPPED +<br>round_score=current_score; reason<br>PLAYER_CHOICE or TIMEOUT.|END_TURN|
|NUMERICAL_DECK_DEADLOCK|System|Apply Section 3.3 to all remaining ACTIVE<br>players.<br>|END_TURN|
|END_TURN|System|No started efect/draw context remains<br>for this turn. Clear turn-scoped role<br>pointers.|ROUND_END_CHECK|
|ROUND_END_CHECK|System|If any participant ACTIVE choose next<br>turn;otherwise end round.|TURN_START / ROUND_END|



#### **5.1 Canonical normal DRAW flow** 

WAITING_FOR_PLAYER_ACTION -> DRAW -> PUSH_NORMAL_DRAW -> DRAW_CARD -> IDENTIFY_CARD. 

- NUMBER branch: ADD_NUMBER_CARD -> CHECK_13 -> CHECK_OVER_31 -> CHECK_PERFECT_31 -> POST_RESOLUTION. 

- EFFECT branch: CREATE_EFFECT_CONTEXT -> WAITING_FOR_TARGET_SELECTION -> APPLY_EFFECT. The effect context completes according to its own rules. If a child DRAW 2 context is created, it resolves before the effect context completes. 

- After an effect completes, the underlying NORMAL_DRAW context still has remaining_numbers=1 unless it directly received its own numerical card. Therefore effect-generated numbers never satisfy the normal draw quota. 

- If TURN_OWNER becomes non-ACTIVE, the NORMAL_DRAW obligation is cancelled after all already-started effect contexts that must complete have completed; transition to END_TURN. 

#### **5.2 Context stack and resume rule** 

Resolution uses a depth-first/LIFO stack of DRAW_CONTEXT and EFFECT_CONTEXT frames. The most recently created child context resolves first. A parent context resumes only after the child context completes and only if its required recipient remains ACTIVE and the parent context is still legal. 

### **6. DRAW Specification** 

Choosing DRAW creates one NORMAL_DRAW context for TURN_OWNER with remaining_numbers=1. The action is not complete until that context directly receives one numerical card, unless TURN_OWNER becomes non-ACTIVE or NUMERICAL_DECK_DEADLOCK ends the round. 

|**Scenario**|**Normative result**|
|---|---|
|DRAW -> +5 on opponent -> 7|Resolve +5 completely. NORMAL_DRAW remains pending. Draw again; 7 is<br>received byNORMAL_DRAW;run number checks;if survived,turn ends.<br>|
|DRAW -> STOP on self|STOP efect makes TURN_OWNER FORCED_STOP. Complete efect; cancel<br>pendingNORMAL_DRAW;END_TURN.<br>|
|DRAW -> ZERO on self|ZEROED / round 0. Complete efect; cancel pending NORMAL_DRAW;<br>END_TURN.<br>|
|DRAW -> +5 on self -> score 31|PERFECT_31 / 50. Complete efect;cancelpendingNORMAL_DRAW;END_TURN.<br>|
|DRAW -> +5 on self -> score 32+|BUST_OVER_31 / 0. Complete efect; cancel pending NORMAL_DRAW;<br>END_TURN.|
|DRAW -> DRAW 2 self -> survives two forced numbers|DRAW 2 child context completes, then NORMAL_DRAW resumes with<br>remaining_numbers still 1; player must receive one additional normal numerical<br>card.|



13/31 - Digital Game Rules Specification v1.1 | Systems Design 

### **7. Number Card Resolution and Rule 13** 

**1.** Append the received numerical card to DRAW_RECIPIENT.NUMBER_HISTORY. 

**2.** Increase DRAW_RECIPIENT.CURRENT_SCORE by the card value. 

**3.** Decrement remaining_numbers only in the DRAW_CONTEXT that directly received this card. 

**4.** If NUMBER_HISTORY contains at least two cards, compare only the final two numerical values. 

**5.** If their sum equals 13: set BUST_13, round_score=0, and stop further score checks for this card. 

**6.** Otherwise run CHECK_OVER_31, then CHECK_PERFECT_31. 

Effect cards never enter NUMBER_HISTORY, never change which numerical cards are adjacent, and never trigger Rule 13 by themselves. 

#### **7.1 Priority collision** 

CONFIRMED: if a newly received numerical card simultaneously makes the last two numbers sum to 13 and makes CURRENT_SCORE exactly 31, Rule 13 wins: BUST_13, round_score=0. By the same priority order, if that numerical card creates both 13 and score 32+, the result is BUST_13 because CHECK_13 occurs first. 

### **8. Bust Over 31 Specification** 

After a numerical card passes CHECK_13, if CURRENT_SCORE >=32, set BUST_OVER_31 and round_score=0. The player immediately becomes non-ACTIVE. For a score-increasing effect that does not add a numerical card, Rule 13 is skipped and the effect performs its own score terminal checks: >31 first, then =31. In the current card set this applies to +5. 

### **9. Perfect 31 Specification** 

If CURRENT_SCORE becomes exactly 31 after a valid score change and Rule 13 has not already triggered on a newly added numerical card, set PERFECT_31, round_score=50, and immediately make the player non-ACTIVE. Perfect 31 may be reached through a numerical card or +5. 

|**Cause**|**Rule 13?**|**31 result**|
|---|---|---|
|Numerical card|Yes,frst.|If no 13 and score==31 -> PERFECT_31 / 50.|
|+5|No.|If score==31 -> PERFECT_31 / 50.|
|-5|No.|Cannot create 31 from a legal ACTIVE score<br>because it onlydecreases score.|



### **10. +5 Effect Specification** 

|**Property**|**Normative rule**|
|---|---|
|Valid target|Exactlyone ACTIVEplayer. Self and opponent targetingare allowed.|
|Apply|target.current_score += 5. NUMBER_HISTORY unchanged.|
|Rule 13|Not checked;+5 is not a numerical card.|
|Post-efect score checks|If score >=32 -> BUST_OVER_31 / 0. Else if score ==31 -> PERFECT_31 / 50.<br>Else target remains ACTIVE.|
|Completion|After score checks and any resulting terminal state are fnalized, +5<br>EFFECT_CONTEXT completes and card enters DISCARD_PILE.|



### **11. -5 Effect Specification** 

|**Property**|**Normative rule**|
|---|---|
|Valid target|Exactlyone ACTIVEplayer. Self and opponent targetingare allowed.|
|Apply|target.current_score = max(0,target.current_score - 5).|
|NUMBER_HISTORY|Unchanged;no number is removed and last numerical card is unchanged.|
|Rule 13|Not checked.<br>|
|Score becomes 0|Target remains ACTIVE. -5 only modifes score; ZERO is the distinct card that<br>removes aplayer at score 0.<br>|
|Later numerical card|Add its value to modifed CURRENT_SCORE; Rule 13 still uses<br>NUMBER_HISTORY only.|



13/31 - Digital Game Rules Specification v1.1 | Systems Design 

### **12. STOP Action Specification (Voluntary)** 

When WAITING_FOR_PLAYER_ACTION, TURN_OWNER may choose STOP instead of DRAW. Set round_state=STOPPED, round_score=current_score, termination_reason=PLAYER_CHOICE, then END_TURN. A normal action timeout uses the same STOPPED gameplay state with termination_reason=TIMEOUT. 

### **13. STOP Effect Card Specification** 

|**Property**|**Normative rule**|
|---|---|
|Valid target|Exactlyone ACTIVEplayer;self allowed.|
|Apply|Target becomes FORCED_STOP, round_score=current_score,<br>termination_reason=STOP_EFFECT.<br>|
|Furtherparticipation|None in this round;no turns and no efect targeting.<br>|
|Self during own NORMAL_DRAW|Turn owner becomes inactive; after efect completes, pending NORMAL_DRAW is<br>cancelled and turn ends.|
|During DRAW 2|If DRAW_RECIPIENT targets self, remaining quota for draw contexts requiring that<br>recipient is cancelled after the already-started efect completes. If another player<br>is targeted,recipient continues if still ACTIVE.|



### **14. ZERO Effect Specification** 

|**Property**|**Normative rule**|
|---|---|
|Valid target|Exactlyone ACTIVEplayer;self-target explicitlylegal.|
|Apply|Set target.current_score=0,round_score=0,round_state=ZEROED.|
|NUMBER_HISTORY|Not removed until ROUND_END.|
|Furtherparticipation|None;target is non-ACTIVE and invalid for future targets.<br>|
|Self during own NORMAL_DRAW|After ZERO efect context completes, pending NORMAL_DRAW is cancelled;<br>END_TURN.|
|Self during DRAW 2|Recipient becomes inactive; remaining numerical quota requiring that recipient is<br>cancelled after started efect completion.|



### **15. DRAW 2 Specification** 

DRAW 2 selects exactly one ACTIVE EFFECT_TARGET. The selected player becomes DRAW_RECIPIENT of a new child DRAW_CONTEXT with type=DRAW_2 and remaining_numbers=2. That child context resolves depth-first before the parent effect context completes. 

#### **15.1 DRAW 2 context fields** 

|**Field**|**Meaning**<br>|
|---|---|
|SOURCE_EFFECT_CARD|Specifc DRAW 2 card instance.<br>|
|EFFECT_DRAWER|Player who physically drew this DRAW 2. May difer from TURN_OWNER and<br>recipient.|
|DECISION_OWNER|EFFECT_DRAWER. Chooses the ACTIVE EFFECT_TARGET.|
|EFFECT_TARGET / DRAW_RECIPIENT|Selected ACTIVEplayer who must receive numerical cards.|
|remaining_numbers|Starts at 2. Decrements only when this DRAW_2 context directly receives a<br>numerical card.|
|completion|remaining_numbers==0 OR required recipient becomes non-ACTIVE OR global<br>NUMERICAL_DECK_DEADLOCK.|



#### **15.2 Required A-G outcomes** 

|**Question**|**Normative result**|**Status**|
|---|---|---|
|A. Who targets efects drawn during DRAW 2?|The forced DRAW_RECIPIENT who physically drew that<br>efect becomes EFFECT_DRAWER and<br>DECISION_OWNER.|DERIVED / STRUCTURAL|
|B. Rule 13 after frst number?|DRAW 2 stops for that recipient immediately; BUST_13 /<br>0.|CONFIRMED|
|C. >31 after frst number?|DRAW 2 stops immediately;BUST_OVER_31 / 0.|CONFIRMED|
|<br>D. Perfect 31 after frst number?|DRAW 2 stops immediately;PERFECT_31 / 50.|DERIVED|
|<br>E. Recipient draws STOP and self-targets?|Resolve STOP atomically; recipient FORCED_STOP;<br>remaining quota requiringthat recipient is cancelled.|DERIVED + APPROVED ATOMICITY|
|F. Recipient draws ZERO and self-targets?|Resolve ZERO atomically; recipient ZEROED; remaining<br>quota requiringthat recipient is cancelled.|CONFIRMED / DERIVED|
|G. DRAW 2 creates DRAW 2?|Legal. New DRAW 2 resolves depth-frst/LIFO with<br>independentquota;then legalparent resumes.|APPROVED / CLOSED|



13/31 - Digital Game Rules Specification v1.1 | Systems Design 

#### **15.3 Nested DRAW 2 - depth-first / LIFO** 

**1.** When a DRAW_RECIPIENT encounters a DRAW 2 effect, create a new EFFECT_CONTEXT for that card and obtain its target from the new EFFECT_DRAWER. 

**2.** Applying that DRAW 2 pushes a new child DRAW_CONTEXT with remaining_numbers=2. 

**3.** Resolve the child context completely before returning to the parent effect/draw context. 

**4.** Each numerical card decrements only the child context that directly received it. A numerical card never simultaneously satisfies parent quotas. 

**5.** When child DRAW 2 completes, pop its DRAW_CONTEXT, finish/discard its SOURCE_EFFECT_CARD, then resume the parent context only if the parent required recipient remains ACTIVE and the parent remains legal. 

##### **DR-007 - Nested DRAW 2 semantics** 

**Status:** APPROVED / CLOSED 

**Normative rule:** Nested DRAW 2 is depth-first/LIFO. Every DRAW 2 has an independent remaining_numbers=2 quota. Child numerical cards do not count toward ancestor quotas. 

#### **15.4 DRAW 2 self-target during normal DRAW** 

If TURN_OWNER chooses normal DRAW, then physically draws DRAW 2 and targets self, the original NORMAL_DRAW context remains below the DRAW 2 effect context. The two forced numerical cards satisfy only the DRAW_2 child context. If TURN_OWNER remains ACTIVE after the DRAW 2 fully resolves, the engine resumes the original NORMAL_DRAW with remaining_numbers still 1 and continues drawing until TURN_OWNER directly receives one normal numerical card. 

Normative sequence when survived: normal DRAW -> DRAW 2 self -> forced number 1 -> forced number 2 -> DRAW 2 effect completes -> resume normal DRAW -> one additional normal number -> normal turn completion. 

##### **DR-008 - DRAW 2 self-target during normal DRAW** 

**Status:** APPROVED / CLOSED 

**Normative rule:** Option B approved: forced DRAW 2 numbers do not satisfy the parent NORMAL_DRAW. Resume and require the normal draw's own numerical card if TURN_OWNER remains ACTIVE. 

#### **15.5 Effect atomicity and inactive roles** 

An EFFECT_CONTEXT becomes started once the effect card has been drawn, its context created, and target resolution has begun. A started effect SHALL resolve according to its own stored context. No rollback occurs merely because TURN_OWNER, EFFECT_DRAWER, or DRAW_RECIPIENT later becomes inactive during nested resolution. 

- Already-started effect resolution completes according to its own context. 

- After a child effect/context completes, any pending draw obligation whose required recipient is now non-ACTIVE is cancelled. 

- If TURN_OWNER is non-ACTIVE after all started child contexts complete, the normal turn is not restored; transition toward END_TURN. 

- A parent context resumes only if its required recipient remains ACTIVE and the parent context is still legal. 

- There is no rollback of cards already drawn, score changes already applied, terminal states already reached, or child effects already started. 

#### **15.6 Atomicity examples** 

Example 1 - TURN_OWNER becomes inactive while another recipient is resolving nested DRAW 2: A is TURN_OWNER. A's DRAW 2 targets B. During B's forced draw, B draws STOP and targets A. STOP resolves; A becomes FORCED_STOP. B is still ACTIVE, so the already-started parent DRAW 2 on B continues until B receives its remaining required numbers or becomes inactive. After that DRAW 2 completes, A's normal turn does not resume; END_TURN follows. 

Example 2 - EFFECT_DRAWER / parent recipient becomes inactive while its child DRAW 2 is resolving: A is TURN_OWNER. A's DRAW 2 targets B. B draws a nested DRAW 2 and targets C, so for the nested effect EFFECT_DRAWER=B and 

DRAW_RECIPIENT=C. During C's forced draw, C draws ZERO and targets B. B becomes ZEROED, but the already-started nested DRAW 2 on C continues until C finishes its own quota or becomes inactive. When nested DRAW 2 completes, the parent DRAW 2 requiring B does not resume because B is no longer ACTIVE. If A is still ACTIVE, resolution returns to A's parent NORMAL_DRAW; otherwise END_TURN. 

13/31 - Digital Game Rules Specification v1.1 | Systems Design 

##### **DR-009 - Effect atomicity** 

##### **Status:** APPROVED / CLOSED 

**Normative rule:** Already-started effect resolution is atomic with respect to later inactivity of TURN_OWNER, EFFECT_DRAWER, or DRAW_RECIPIENT. Complete the started context, then cancel illegal obligations and resume only legal parents. 

### **16. Target Selection Rules** 

- Every current effect card (+5, -5, DRAW 2, STOP, ZERO) requires exactly one EFFECT_TARGET. 

- EFFECT_TARGET must be ACTIVE at target-selection resolution time. 

- Self-target is legal for all five current effect cards. 

- STOPPED, FORCED_STOP, BUST_13, BUST_OVER_31, PERFECT_31, ZEROED and NOT_PARTICIPATING players are invalid targets. 

- DECISION_OWNER is always the EFFECT_DRAWER for the current card, including effects drawn during forced DRAW 2. 

- If exactly one ACTIVE player exists and that player is DECISION_OWNER, self is the only legal target and the effect must resolve. 

- On target-selection timeout, DR-005B automatically selects DECISION_OWNER as self-target while that player remains ACTIVE. 

- A forced DRAW_RECIPIENT never receives a normal DRAW/STOP action choice merely because they are drawing cards; normal action authority remains with TURN_OWNER. 

### **17. Canonical Card / Effect Resolution Priority** 

|**Priority**|**Event**|**Normative rule**|
|---|---|---|
|1|Draw-context viability|If top DRAW_CONTEXT still needs a number and no<br>numerical card exists in DRAW_PILE or DISCARD_PILE -><br>NUMERICAL_DECK_DEADLOCK.<br>|
|2|Physical draw availability|If DRAW_PILE empty, shufle DISCARD_PILE into new<br>DRAW_PILE. Nopre-emptive refll.|
|3|Card drawn<br>|Draw topcard for top-context DRAW_RECIPIENT.|
|4|Card identifed|NUMBER or EFFECT.|
|5A|NUMBER added|Append to NUMBER_HISTORY; add value; decrement<br>onlydirect DRAW_CONTEXTquota.|
|6A|CHECK_13|If last two numerical values sum 13 -> BUST_13 / 0; stop<br>checks.|
|7A|CHECK_OVER_31|If current_score >=32 -> BUST_OVER_31 / 0;stopchecks.|
|8A|CHECK_PERFECT_31|If current_score ==31 -> PERFECT_31 / 50.|
|5B|EFFECT context created|Set SOURCE_EFFECT_CARD,<br>EFFECT_DRAWER=DRAW_RECIPIENT,<br>DECISION_OWNER=EFFECT_DRAWER.|
|6B|Target selected|Choose exactly one ACTIVE EFFECT_TARGET; timeout<br>follows DR-005B.|
|7B|Efect applied|Apply +5/-5/STOP/ZERO, or create child DRAW_2<br>context.|
|8B|Efect-specifc terminal checks|+5: >31 then =31; -5 clamp only; STOP/ZERO directly<br>terminal.<br>|
|9|Child contexts|Resolve nested/child contexts depth-frst. Started efect<br>completes even if roleplayers become inactive.<br>|
|10|Efect completion|After child resolution and required efect semantics<br>complete, move SOURCE_EFFECT_CARD to<br>DISCARD_PILE.|
|11|Context activity check|Cancel pending numerical obligation if its required<br>recipient is no longer ACTIVE.|
|12|Context completion/resume|If top context still legal and needs numbers -><br>DRAW_CARD;elsepopand resume legalparent.|
|13|Turn completion|When no pending started context remains and normal<br>action complete or TURN_OWNER inactive -><br>END_TURN.|
|14|Round-end check|If ACTIVE participant count >0 -> TURN_START; else<br>ROUND_END.|



### **18. End-Turn Rules** 

A normal turn ends only when there is no unresolved started effect/draw context that must still complete and one of the following holds: 

- TURN_OWNER voluntarily STOPPED or timed out into STOPPED. 

- TURN_OWNER directly completed the NORMAL_DRAW quota and survived all checks. 

13/31 - Digital Game Rules Specification v1.1 | Systems Design 

- TURN_OWNER became non-ACTIVE due to STOP, ZERO, BUST_13, BUST_OVER_31, or PERFECT_31, after already-started effect contexts finish as required. 

- NUMERICAL_DECK_DEADLOCK terminated all remaining ACTIVE players. 

Next-player selection: starting from the seat immediately clockwise after the completed TURN_OWNER, select the first PARTICIPATING player whose round_state is ACTIVE. Skip all other seats. If exactly one ACTIVE player remains, that player continues taking turns until becoming inactive; the round does not end early. 

### **19. End-Round Rules** 

**1.** ROUND_END_CHECK occurs only at a stable boundary after required started-effect atomicity is satisfied. 

**2.** If at least one participating player remains ACTIVE, round continues with TURN_START. 

**3.** If zero participating players are ACTIVE, enter ROUND_END. 

**4.** Every terminal player already has a finalized ROUND_SCORE from the transition that made them inactive. 

**5.** Move every numerical card from participating players' NUMBER_HISTORY into DISCARD_PILE, then clear round-only histories after replay/state persistence requirements are satisfied. 

**6.** Normal round: add each ROUND_SCORE to TOTAL_SCORE, then GAME_END_CHECK. 

**7.** Tie-break round: do not modify TOTAL_SCORE; store each participant's result as TIEBREAK_ROUND_RESULT, then TIEBREAK_RESULT_EVAL. 

### **20. End-Game and Tie-Break Rules** 

CONFIRMED normal end-game check: after each completed normal round, if at least one player has TOTAL_SCORE >=150, normal round progression stops. Highest TOTAL_SCORE determines winner unless two or more players share that highest total. 

|**Case**|**Result**|
|---|---|
|Noplayer has total >=150|Start next normal round usingnormal starter rotation.|
|At least one total >=150;unique highest total|GAME_ENDED;unique highest-totalplayer wins.|
|At least one total >=150;two or more tied at highest total|Onlythose tied highestplayers enter tie-break.|



#### **20.1 Tie-break participants and scoring** 

- Only players tied for highest TOTAL_SCORE participate in the first tie-break round. All other matched players are NOT_PARTICIPATING for that round. 

- Tie-break uses the normal round mechanics, card rules, effect targeting among ACTIVE tie-break participants, deck continuity, and round-state model. 

- At tie-break round end, compare that round's ROUND_SCORE values only. 

- If one participant has unique highest tie-break ROUND_SCORE, that player wins the match. 

- If two or more participants tie for highest tie-break ROUND_SCORE, only that tied-high subset participates in the next tiebreak round. 

- Repeat until a unique winner exists. 

- Tie-break ROUND_SCORE values are stored separately and SHALL NOT be added to TOTAL_SCORE. 

##### **DR-010 - Tie-break continuation** 

**Status:** APPROVED / CLOSED 

**Normative rule:** Only tied highest players participate. Compare highest score in each tie-break round; if tied again, only the tied-high subset continues in another tie-break. Repeat until a unique winner. Regular TOTAL_SCORE is never modified by tie-break rounds. 

#### **20.2 Tie-break seat order and starter** 

Tie-break never creates a new seat assignment. The original immutable SEAT_RING is preserved and non-participating seats are skipped for dealing, targeting, and turn selection. 

**1.** Take the previous round's actual ROUND_STARTER seat. 

**2.** Advance one seat clockwise on the original SEAT_RING to obtain the starter candidate. 

**3.** If candidate participates in the new tie-break round, candidate is ROUND_STARTER. 

**4.** If candidate does not participate, scan clockwise from candidate and choose the first participating seat. 

13/31 - Digital Game Rules Specification v1.1 | Systems Design 

**5.** For a repeated tie-break round, repeat the same rotation from the previous tie-break round's actual ROUND_STARTER, using the unchanged original SEAT_RING and the newly narrowed participant set. 

Example (4 players): Seats are A=Seat0, B=Seat1, C=Seat2, D=Seat3. The final normal round starter was B (Seat1). Only B and D are tied for highest TOTAL_SCORE. Tie-break starter candidate is C (Seat2), but C does not participate, so scan clockwise to D (Seat3). D starts the first tie-break and opening deal order is D -> B. If that tie-break also ties between B and D, rotate from actual starter D to candidate A (Seat0); A is not participating, so B (Seat1) becomes starter of the next tie-break. 

### **21. Edge Case Register** 

|**ID**|**Situation**<br>|**Expected behavior**|**Status**|
|---|---|---|---|
|EC-001|Numerical card creates Rule 13 and score 31<br>simultaneously.|BUST_13; round_score=0.|CONFIRMED|
|EC-002|Numerical card creates Rule 13 and score<br>32+ simultaneously.|BUST_13 because CHECK_13 precedes<br>over-31.|DERIVED|
|EC-003<br>EC-004|+5 makes exactly31.<br>+5 makes 32+|PERFECT_31;round_score=50.<br>BUSTOVER31;roundscore=0|CONFIRMED<br>CONFIRMED|
|EC-005|.<br>-5 makes exactly0.|__ _.<br>Player remains ACTIVE.|DERIVED|
|EC-006|Player at score 0 voluntarilySTOPs.|STOPPED;round_score=0.|DERIVED|
|EC-007|-5 on score <5.|Clampcurrent_score to 0.<br>|CONFIRMED|
|EC-008|Efect between two numerical cards.|Efect does not break numerical adjacency<br>for Rule 13.|CONFIRMED|
|EC-009|Duplicate numbers such as 8,8.|<br>Legal;no duplicatepenalty.<br>|CONFIRMED|
|EC-010|Voluntary STOP.|STOPPED; preserve current score; reason<br>PLAYER_CHOICE.|CONFIRMED|
|EC-011|Normal action timer expires.|STOPPED; preserve current score; reason<br>TIMEOUT.|APPROVED / CLOSED|
|EC-012|STOP card on target.|FORCED_STOP; preserve current score.<br>|CONFIRMED|
|EC-013|Turn owner draws STOP and targets self.|Efect completes; FORCED_STOP; cancel<br>pendingnormal draw.|CONFIRMED / DERIVED|
|EC-014|Turn owner draws ZERO and targets self.|ZEROED / 0; cancel pending normal draw<br>after efect comletes|CONFIRMED / DERIVED|
|EC-015|Onlyone ACTIVEplayer draws an efect.<br>|p.<br>Self is onlylegal target;efect must resolve.|DERIVED|
|EC-016|Efect target timer expires while decision<br>owner ACTIVE.|Auto self-target DECISION_OWNER.|APPROVED / CLOSED|
|EC-017|DRAW 2 frst number creates 13.|Recipient BUST_13; remaining quota<br>cancelled.|CONFIRMED|
|EC-018|DRAW 2 frst number creates 32+.|Recipient BUST_OVER_31; remaining quota<br>cancelled.|CONFIRMED|
|EC-019|DRAW 2 frst number creates 31.|Recipient PERFECT_31 / 50; remaining quota<br>cancelled|DERIVED|
|EC-020|DRAW 2 recipient draws +5, self-targets to<br>31.|.<br>PERFECT_31; forced draw ends after efect<br>completion.|DERIVED|
|EC-021|DRAW 2 recipient draws STOP, self-targets.|FORCED_STOP; remaining obligations<br>requiringrecipient cancelled.|DERIVED|
|EC-022|DRAW 2 recipient draws ZERO, self-targets.|ZEROED; remaining obligations requiring<br>recipient cancelled.|CONFIRMED / DERIVED|
|EC-023|Efect drawn during DRAW 2.|DRAW_RECIPIENT becomes<br>EFFECT_DRAWER and DECISION_OWNER.|DERIVED / STRUCTURAL|
|EC-024|DRAW 2 creates nested DRAW 2.|Resolve depth-frst/LIFO; independent<br>quotas.|APPROVED / CLOSED|
|EC-025|Nested numerical card arrives.|Counts only for direct child DRAW_CONTEXT,<br>never ancestor.|APPROVED / CLOSED|
|EC-026|Normal DRAW -> DRAW 2 self -> survives two<br>forced numbers.|Resume normal DRAW; require one<br>additional normal number.<br>|APPROVED / CLOSED|
|EC-027|A TURN_OWNER; B forced recipient; B draws<br>new DRAW 2.|For nested efect: EFFECT_DRAWER=B and<br>DECISION_OWNER=B; TURN_OWNER<br>remains A.|APPROVED / STRUCTURAL|
|EC-028|TURN_OWNER becomes inactive while<br>DRAW 2 on anotherplayer is resolving.|Started DRAW 2 completes for recipient; then<br>normal turn does not resume.|APPROVED / CLOSED|
|EC-029|EFFECT_DRAWER becomes ZEROED while its<br>started child DRAW 2 on C is resolving.|Child DRAW 2 continues on C; parent<br>requiring inactive drawer/recipient resumes<br>onlyif still legal.|APPROVED / CLOSED|
|EC-030|Parent DRAW 2 recipient becomes inactive<br>duringnested child efect.|Complete started child; cancel parent<br>remaining quota after child completes.|APPROVED / CLOSED|
|EC-031|Exactly one ACTIVE player remains.|Round continues; sole ACTIVE player keeps<br>takingturns.|CONFIRMED|
|EC-032|No ACTIVE players remain after stable<br>resolution.|ROUND_END.|CONFIRMED / DERIVED|
|EC-033|Next clockwise seat inactive or not<br>participating.|Skip to frst PARTICIPATING + ACTIVE seat.|DERIVED|
|EC-034|Inactive target submitted.|Reject as illegal; remain waiting for valid<br>target until timeout.|CONFIRMED + DIGITAL|
|EC-035|Opening deal reveals efect.|Set aside; do not resolve; continue same<br>player until number.|CONFIRMED|
|EC-036|Multiple opening efects for one player.|Set all aside; same player continues until<br>number.|CONFIRMED|
|EC-037|Opening deal complete.|Shufle all set-aside efects back into draw<br>pile.|CONFIRMED|
|EC-038|Opening deal order in 4-player match.|Start at ROUND_STARTER; fnish each player<br>fltered draw; proceed clockwise.|APPROVED / CLOSED|
|EC-039|Draw pile empty before physical draw;<br>discard nonempty.|<br>Shufle discard into new draw pile and<br>continue.|APPROVED / CLOSED|
||DRAW 2 begins with one physical card left in|Draw it normally; do not pre-emptively refll.<br>||
|EC-040|<br>draw pile.|Refll only when later physical draw sees<br>empty pile.|APPROVED / CLOSED|



13/31 - Digital Game Rules Specification v1.1 | Systems Design 

|**ID**|**Situation**|**Expected behavior**|**Status**|
|---|---|---|---|
|EC-041|No numerical cards in draw or discard while<br>numerical obligationpending.|NUMERICAL_DECK_DEADLOCK; force-stop<br>all remainingACTIVEplayers.|APPROVED / CLOSED|
|EC-042|Deadlock while some ACTIVE current scores<br>are 0.|FORCED_STOP; round_score=0 for those<br>players.|APPROVED / CLOSED|
|EC-043|Deadlock occurs with pending nested<br>efects/draws.<br>|Cancel all pending draw<br>obligations/contexts; no further physical<br>draw;standard round-end fow.<br>|APPROVED / CLOSED|
|EC-044|Resolved efect in discard and pile later reflls<br>same round.|Efect may re-enter draw pile and be drawn<br>again.|DERIVED|
|EC-045|Numerical cards of inactive player.|Remain in NUMBER_HISTORY until<br>ROUND_END,then discard.|DERIVED|
|EC-046|Disconnected but gameplay-ACTIVE player.|Disconnect alone does not change round<br>state; timers may resolve gameplay decisions<br>per A/B.|APPROVED + OPEN PRODUCT POLICY|
|EC-047|Non-turnplayer submits DRAW/STOP.|Reject as illegal;no state change.|DIGITAL INVARIANT|
|EC-048|Turn owner submits stale second action after<br>state advanced.|Reject stale/illegal action; no rollback.|DIGITAL INVARIANT|
|EC-049|Player reaches total 150 while personally<br>inactive but round continues.|Do not end match mid-round; threshold<br>checked after<br>ROUND_END/SCORES_UPDATED.|CONFIRMED|
|EC-050|Multipleplayers >=150 with unequal totals.|Unique highest total wins.|CONFIRMED|
|EC-051|Highest total tied after threshold.|Onlytied highestplayers enter tie-break.|CONFIRMED / DERIVED|
|EC-052|Tie-break round has unique highest round<br>score.|That player wins; regular totals unchanged.|CONFIRMED / APPROVED|
|EC-053|Tie-break round ties again at highest score.|Only tied-high subset continues to another<br>tie-break.|APPROVED / CLOSED|
|EC-054|Tie-break excludes some original seats.|Preserve original seat ring; skip<br>nonparticipants.|APPROVED / CLOSED|
|EC-055|Tie-break starter candidate not participating.|First participating seat clockwise after<br>candidate becomes starter.|APPROVED / CLOSED|
|EC-056|Repeated tie-break narrows from 3<br>participants to 2.|Next round uses same original ring, narrowed<br>participant set, and starter rotation from prior<br>actual starter.|APPROVED / CLOSED|
|EC-057|Tie-break round scores would push a total<br>higher.|Do not add them to TOTAL_SCORE; store<br>separately.|APPROVED / CLOSED|
|EC-058|Seat assignment after match creation.|Immutable; never re-randomize for later<br>rounds/tie-break.|APPROVED / CLOSED|
|EC-059|Round 1 starter versus Seat 0.|Starter is independently uniform; need not be<br>Seat 0.<br>|APPROVED / CLOSED|
|EC-060|Future external forfeit attempts to mutate<br>round state during pending target selection.|Not defned in this rules spec; Multiplayer<br>Product & Match Rules Specifcation must<br>defne atomic handof under DR-005D.|OPEN PRODUCT POLICY|



### **22. Decision Log** 

|**ID**|**Topic**|**Normative decision / scope**|**Status**|
|---|---|---|---|
|DR-001|Supportedplayer count|Exactly2-4players.|APPROVED / CLOSED|
|DR-002|Seat order / initial starter|Uniform random seat assignment;<br>immutable ring; uniform random Round 1<br>starter.|APPROVED / CLOSED|
|DR-003|Later normal-round starter|Rotate one seat clockwise.|APPROVED / CLOSED|
|DR-004|Opening deal order|ROUND_STARTER clockwise; complete<br>eachplayer fltered draw before next.|APPROVED / CLOSED|
|DR-005A|Normal turn timeout|Voluntary-equivalent STOPPED +<br>termination_reason=TIMEOUT.|APPROVED / CLOSED|
|DR-005B|Efect target timeout|Auto self-target DECISION_OWNER if<br>ACTIVE.|APPROVED / CLOSED|
|DR-005C|Disconnect / reconnect grace|Exact grace/pause/reconnect policy<br>deferred.|OPEN / DECISION REQUIRED|
|DR-005D|Match forfeit policy|Forfeit timing/result/ranked<br>consequences deferred.|OPEN / DECISION REQUIRED|
|DR-006|Deck refll|On-demand only when DRAW_PILE<br>empty.|APPROVED / CLOSED|
|DR-007|Nested DRAW 2|Depth-frst/LIFO;independentquotas.|APPROVED / CLOSED|
|DR-008|DRAW 2 self during normal DRAW|Resume parent NORMAL_DRAW; forced<br>numbers do not satisfyit.|APPROVED / CLOSED|
|DR-009|Efect atomicity|Started efect completes by stored<br>context despite later inactivity of role<br>players.|APPROVED / CLOSED|
|DR-010|Tie-break continuation|Tied-high subset repeats tie-break until<br>unique winner;regular totals unchanged.|APPROVED / CLOSED|
|DR-011|No numerical card available|NUMERICAL_DECK_DEADLOCK forced-<br>stoprule.|APPROVED / CLOSED|



### **23. Implementation Acceptance Rules** 

A game-rules implementation conforms to v1.1 only if all of the following are true: 

- MATCH_CREATED accepts exactly 2-4 players for this ruleset; seats are a uniform random permutation and remain immutable. 

13/31 - Digital Game Rules Specification v1.1 | Systems Design 

- Round 1 starter is uniformly random; later normal/tie-break starter logic follows Sections 2.1 and 20.2 exactly. 

- Every accepted player input is legal for the current authoritative state and correct DECISION_OWNER. 

- TURN_OWNER, DRAW_RECIPIENT, EFFECT_DRAWER, DECISION_OWNER and EFFECT_TARGET are represented separately and never inferred as identical merely because a normal turn exists. 

- TURN_IN_PROGRESS is not used as a gameplay state. Canonical turn transitions use TURN_START -> WAITING_FOR_PLAYER_ACTION -> explicit resolution substates -> END_TURN -> ROUND_END_CHECK. 

- Every normal DRAW has its own quota of one direct numerical card. Effect-generated forced numerical cards cannot satisfy it. 

- Every DRAW 2 has an independent quota of two direct numerical cards and nested DRAW 2 uses depth-first/LIFO semantics. 

- Already-started effects complete atomically according to stored context; no rollback occurs because a role player later becomes inactive. 

- Rule 13 is checked only after numerical cards, before over-31 and Perfect 31. 

- Score-only +5 skips Rule 13 and checks >31 before =31; -5 clamps at 0 and does not terminate at 0. 

- All terminal round states immediately remove the player from normal-turn and effect-target eligibility. 

- Physical deck refill occurs only when DRAW_PILE is empty. No pre-emptive refill for multi-card obligations. 

- If a numerical obligation exists with zero numerical cards in both draw and discard, NUMERICAL_DECK_DEADLOCK deterministically ends the round per Section 3.3. 

- Normal action timeout produces STOPPED + TIMEOUT; target-selection timeout auto-targets self per DR-005B. 

- Round end occurs only at a stable boundary and iff no participating player remains ACTIVE. 

- Game-end threshold is evaluated only after completed normal rounds. 

- Tie-break rounds preserve original seat ring, never modify TOTAL_SCORE, and repeat among tied-high participants until a unique winner exists. 

- Replay of identical initial deck order, random seat/starter results, legal player decisions, and timer-expiry events produces the same authoritative state-transition sequence. 

- DR-005C and DR-005D are not hard-coded with unapproved durations, forfeit thresholds, match-result rules, or ranked consequences in the game-rules engine. 

### **24. Remaining Open Decisions After v1.1** 

Only the following unresolved items remain after this revision. They are multiplayer product/match policy, not unresolved card or turn mechanics. They SHALL be transferred to the next Multiplayer Product & Match Rules Specification. 

|**ID**|**Openquestion**|**Why not resolved here**|**Required next-owner output**|
|---|---|---|---|
|DR-005C|Disconnect / Reconnect Grace:<br>reconnect window, pause/no-pause<br>behavior, session rejoin conditions.|Requires multiplayer product/liveness<br>policy and concrete timing values;<br>oficial tabletop rules contain no<br>equivalent.|Approved reconnect/grace rules and<br>server-timer interaction.|
|DR-005D|Match Forfeit Policy: eventual forfeit<br>trigger, timing, match-result impact,<br>ranked consequences.|Product/ranked policy rather than<br>card-game rule; depends on<br>multiplayer mode design.|Approved forfeit/result/ranked policy,<br>including atomic interaction with<br>pending gameplaydecisions.|



No additional unresolved gameplay-rule ambiguity was identified during the v1.1 consistency audit. 

### **Appendix A. Compact Transition Reference** 

|**From**|**Trigger**|**Condition**|**To / Action**|
|---|---|---|---|
|MATCH_CREATED|Roster received|2-4 players|Uniform random seat assignment -><br>PLAYERS_JOINED|
|ROUND_SETUP|Round begins|Participation + starter resolved|Openingdeal -> TURN_START|
|TURN_START|System|Required ACTIVEparticipant exists|WAITING_FOR_PLAYER_ACTION|
|WAITING_FOR_PLAYER_ACTION|STOP|TURN_OWNER ACTIVE|STOPPED -> END_TURN|
|WAITING_FOR_PLAYER_ACTION|Timer expiry|TURN_OWNER ACTIVE|STOPPED + reason TIMEOUT -> END_TURN|
|WAITING_FOR_PLAYER_ACTION|DRAW|TURN_OWNER ACTIVE|Push NORMAL_DRAW(1)-> DRAW_CARD|
|DRAW_CARD|Precheck|Numerical obligation + no numbers in<br>draw/discard|NUMERICAL_DECK_DEADLOCK|
|DRAW_CARD|Pile empty|Discard nonempty|Shufle discard -> DRAW_CARD|
|DRAW_CARD|NUMBER|Recipient ACTIVE|<br>ADD_NUMBER_CARD|
|DRAW_CARD|EFFECT|Recipient ACTIVE|Create EFFECT_CONTEXT;<br>EFFECT_DRAWER=recipient -> target<br>selection|
|TARGET_SELECTION|Timer expiry|DECISION_OWNER ACTIVE|EFFECT_TARGET=DECISION_OWNER -><br>APPLY_EFFECT|
|ADD_NUMBER_CARD|last2 sum 13|Yes|BUST_13 / 0 -> POST_RESOLUTION|
|ADD_NUMBER_CARD|score >=32|No 13|BUST_OVER_31 / 0 -> POST_RESOLUTION|
|ADD_NUMBER_CARD|score ==31|No 13,not >31|PERFECT_31 / 50 -> POST_RESOLUTION|
|APPLY +5|score >=32|N/A|BUST_OVER_31 / 0|
|APPLY +5|score ==31|N/A|PERFECT_31 / 50|
|APPLY -5|any|N/A|Clampat 0;target remains ACTIVE|
|APPLY STOP|target ACTIVE|N/A|FORCED_STOP / current score|
|APPLY ZERO|target ACTIVE|N/A|ZEROED / 0|



13/31 - Digital Game Rules Specification v1.1 | Systems Design 

|**From**|**Trigger**|**Condition**|**To / Action**|
|---|---|---|---|
|APPLY DRAW 2|target ACTIVE|N/A|Push child DRAW_2(2)depth-frst|
|POST_RESOLUTION|Child complete|Parent recipient ACTIVE + legal|Resumeparent context|
|POST_RESOLUTION|Required recipient inactive|Any pending quota|Cancel that obligation;continue unwind|
|POST_RESOLUTION|No contexts|TURN_OWNER inactive or normal action<br>complete|END_TURN|
|END_TURN|Stable|AnyACTIVEparticipant|ROUND_END_CHECK -> next TURN_START|
|END_TURN|Stable|No ACTIVEparticipant|ROUND_END|
|GAME_END_CHECK|max total <150|Normal round complete|Next normal ROUND_SETUP|
|GAME_END_CHECK|max total >=150 unique|N/A|GAME_ENDED|
|GAME_END_CHECK|max total >=150 tied|N/A|TIEBREAK_SETUP|
|TIEBREAK_RESULT_EVAL|unique highest round score|N/A|GAME_ENDED|
|TIEBREAK_RESULT_EVAL|highest round score tied|N/A|Narrow subset -> next TIEBREAK_SETUP|
|**Appendix B. Sourc**<br>**Oficial rules topic**|**e-to-Spec Tracea**<br>**Rulepages**|**bility**<br>**v1.1 section**|**s**|
|Deck composition / 112 cards|1-2|1,2,3.2||
|Goal / 150-pointgame end<br>|2,14-16|2,19-20||
|Openingnumerical deal / efect fltering<br>|3|3,3.1||
|DRAW / efect chain|3-4|5-6,17||
|Rule 13 /priority|4-7|7,17||
|VoluntarySTOP|5-6|12,18||
|+5|8-9|8-10,17||
|-5|9-10|11||
|DRAW 2|10-11|15||
|STOP efect|11-12|13||
|ZERO<br>|12|14||
|Active / inactive defnition<br>|13|4,16,18-19||
|Round end / discard / reshufle|14|3.2-3.3,19||
|Game end / tie-break extra round|14-15|20||



### **Appendix C. Approved Decision Traceability** 

|**Decision**|**Revision Request 01 requirement**|**Integrated in**|
|---|---|---|
|DR-001|2-4players|0.1,2,2.1,22,23|
|DR-002|Random seats;immutable ring;random initial starter|1,2.1,20.2,22|
|DR-003|Rotate later-round starter<br>|2.1,20.2|
|DR-004|Opening deal from starter clockwise; fltered per<br>player<br>|3.1|
|DR-005A/B|Auto-STOP action timeout;self-target efect timeout|4.3,5,16,App. A|
|DR-005C/D|Remain open for Multiplayer Product & Match Rules<br>Spec<br>|4.3, 22, 24|
|DR-006|On-demand refll<br>|3.2,17,App. A|
|DR-007|Nested DRAW 2 depth-frst/LIFO, independent<br>quotas|5.2, 15.3, 17|
|DR-008|Self DRAW 2 does not satisfynormal DRAW<br>|6,15.4|
|DR-009|Started efect atomicitywith role separation|1.1,15.5-15.6,17|
|DR-010|Repeated tied-high-onlytie-break;separate scores|20,22|
|DR-011|NUMERICAL_DECK_DEADLOCK|3.3,5,17,App. A|



End of v1.1 specification. Document status remains DRAFT FOR FINAL APPROVAL. Upon Product Owner final approval, this exact rules model may be version-locked as the production gameplay contract; DR-005C and DR-005D remain explicitly delegated to the Multiplayer Product & Match Rules Specification. 

13/31 - Digital Game Rules Specification v1.1 | Systems Design 

