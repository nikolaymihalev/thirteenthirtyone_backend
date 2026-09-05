# Backend Clean Architecture

## Context and authority

13/31 uses DDD and Clean Architecture inside a modular authoritative backend.
The MVP is one backend deployable, with a separate Edge Gateway planned later.
Unity is the future mobile client. PostgreSQL is the intended durable source of
truth; Redis-compatible storage is intended only for transient coordination.
None of those integrations is implemented by this foundation.

The locked documents in `docs/source-of-truth` must be consulted in this order;
report conflicts before making behavior decisions:

1. Official 13/31 Rules (tabletop source of truth).
2. Digital Game Rules Specification v1.1 (approved/locked).
3. Multiplayer Product & Match Rules Specification v1.1 (approved/locked).
4. Authoritative Multiplayer Technical Architecture v1.1 (approved/locked).

This guide defines engineering boundaries, not product or gameplay behavior.

## Dependency direction

Arrows indicate permitted direct project references. This is an exhaustive graph:

```text
Game.Domain    -> (none)
Game.Engine    -> Game.Domain
Application    -> Game.Domain, Game.Engine
Infrastructure -> Application, Game.Domain
Protocol       -> (none)
GameBackend    -> Application, Infrastructure, Protocol
```

Every project uses the `ThirteenThirtyOne` prefix. Any direct edge absent above is
forbidden, including Protocol -> Engine and Application -> Protocol. Transitive
SDK references do not grant permission to use a forbidden layer's types. Tests
enforce direct project edges and compiled dependencies separately. No cycles exist.

## Responsibilities

**Game.Domain** owns the gameplay vocabulary: snapshots,
immutable value objects, typed IDs, invariants, and domain events. It has no
database, EF attributes, HTTP, ASP.NET Core, logging framework, serialization
behavior, transport DTOs, network, cloud, or container composition concerns.

**Game.Engine** implements deterministic rule transitions using Domain.
Conceptually: state + deterministic input -> new state + domain events +
deterministic boundary metadata. It must not read a hidden wall clock, network,
filesystem, or static/global Random. Timeouts and seeded randomness enter through
explicit inputs. See `game-engine.md` for the implemented API and compatibility contract.

**Application** coordinates use cases and defines ports for external
capabilities. Unlike Engine, it coordinates workflows and effects rather than
deciding game rules. It does not implement SQL, HTTP endpoints, WebSockets,
or duplicate domain invariants. DevelopmentGameplay calls the engine, maps immutable
safe projections and persists accepted transitions through IGameSessionStore. A failed
expected-hash replacement returns conflict without retrying the gameplay input.

**Infrastructure** implements application ports, currently InMemoryGameSessionStore.
It may depend on Application and Domain. Adapters own external-system details,
not gameplay or player-visible policy. Because inner layers define the contracts
and never reference their implementations, adapters can be replaced without
changing domain rules. Replacement still requires equivalent adapter semantics
and integration testing; it is not automatic portability.

**Protocol** will own versioned wire commands, events, snapshots, errors, and
eventual Protobuf contracts. These models differ from Domain models, carry no
business behavior, and cannot reference inner domain/application types.

**GameBackend** is the ASP.NET Core composition root. It owns startup,
configuration, dependency registration, HTTP concerns, and host logging.
The standard DI container registers the development service and singleton in-memory
store only in Development. The host maps the whole `/dev/games` group and Swagger
only in that environment. It references Application projections, never engine/domain
types. There are no empty AddApplication/AddInfrastructure extension methods.
Identity, Matchmaking, Match Runtime, Presence, Deadlines, Forfeit, Results, and
Outbox remain planned logical modules. No empty module folders or fake services
are necessary to represent them.

## DDD conventions

- Aggregates define transaction consistency boundaries. External code invokes
  aggregate behavior; it does not mutate internal entities directly.
- Value objects are immutable and identified by value.
- Prefer strongly typed IDs for domain identifiers. Add types as actual domain
  concepts emerge, without a speculative third-party ID framework.
- Domain events are facts inside the domain, not transport events, database
  records, or integration events by default.
- Repository ports are aggregate-specific and belong to the appropriate inner
  layer. Do not introduce `IRepository<T>` or a generic base repository without a
  proven requirement.
- Use a domain service only for behavior that naturally belongs to no entity,
  aggregate, or value object.
- Entities protect invariants; do not create public-setter DTO-shaped entities
  to mirror storage records.
- No speculative BaseEntity, Common, Utils, managers, generic frameworks, or
  base services. Framework adoption must solve observed complexity.

Use file-scoped namespaces, one primary type per file, PascalCase type/namespace
names, and standard .NET naming. Keep the full ThirteenThirtyOne public prefix.
Formatting and dependency rules are automated. Semantic DDD decisions require
review and behavior tests when actual domain code is introduced.

## Transport boundary

Future flow:

```text
wire command -> validation/mapping -> application/domain command
             -> authoritative result -> mapped wire event/snapshot
```

Mapping belongs at the outer boundary. Never expose Domain entities directly or
include full deck order, RNG state, or server-only resolution context in Protocol
by default. No actual protocol messages exist in this iteration.

## Enforcement and limitations

Architecture tests use stable NetArchTest.Rules 1.3.2 for compiled type dependency
checks; it is small and requires no production instrumentation. Assembly reference
checks supplement it. Evaluated MSBuild item checks enforce the exact graph in
both Debug and Release, including references unused by code and references from
imported props/targets at evaluation time. This closes the common empty-assembly
test gap. The reserved Protocol boundary retains an internal assembly marker.

Domain, Engine, Application, and Protocol currently allow no package references,
explicit assembly references, or framework references beyond Microsoft.NETCore.App.
Adding a justified inner-layer package requires deliberately updating this policy
after architecture review. Compiled tests reject ASP.NET Core, EF Core,
Microsoft.Extensions, PostgreSQL, Redis, AWS, and System.Net dependencies there.
Test-only libraries and host test infrastructure never enter production projects.

These checks do not prove determinism or detect every indirect BCL API call,
reflection-based dependency, custom build-time reference injection, or semantic
violation. Engine behavior tests and code review enforce explicit inputs and invariants.
Additional compiled checks prohibit clock, hidden randomness, IO and threading in the core.

Integration tests boot the actual host through WebApplicationFactory in Development
and non-Development environments, exercise liveness, deterministic complete-game HTTP
replay, concurrency and secret-safe projections. The root has no endpoint.
Liveness proves the host responds, not database readiness.
Configuration conventions and build/format commands are in the root README.

## Modular MVP

Keeping authoritative workflows in one backend avoids distributed transactions,
extra service protocols, and operational complexity during MVP development.
Explicit boundaries preserve testability and allow future separation if measured
scaling or ownership needs justify it. Modules are not independent microservices.
