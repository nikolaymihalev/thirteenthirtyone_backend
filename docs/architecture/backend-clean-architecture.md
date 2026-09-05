# Backend Clean Architecture

## Context and authority

13/31 uses DDD and Clean Architecture inside a modular authoritative backend.
The MVP is one backend deployable, with a separate Edge Gateway planned later.
Unity is the future mobile client. PostgreSQL is the intended durable source of
truth; Redis-compatible storage is intended only for transient coordination.
None of those integrations is implemented by this foundation.

No upstream project documents were present at bootstrap. Future implementation
must consult these in order and report conflicts before making behavior decisions:

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

**Game.Domain** owns the eventual business vocabulary: entities, aggregates,
immutable value objects, typed IDs, invariants, and domain events. It has no
database, EF attributes, HTTP, ASP.NET Core, logging framework, serialization
behavior, transport DTOs, network, cloud, or container composition concerns.

**Game.Engine** will implement deterministic rule transitions using Domain.
Conceptually: state + deterministic input -> new state + domain events +
deterministic boundary metadata. It must not read a hidden wall clock, network,
filesystem, or static/global Random. Time and randomness must eventually enter
through explicit deterministic inputs. No engine API or algorithm exists yet.

**Application** will coordinate use cases and define ports for external
capabilities. Unlike Engine, it coordinates workflows and effects rather than
deciding game rules. It does not implement SQL, HTTP endpoints, WebSockets,
or duplicate domain invariants. Command/query frameworks are not required.

**Infrastructure** will implement aggregate-specific and other application ports.
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
The standard DI container and health checks are its only current composition.
Adapter/use-case registrations should be added here when real implementations
exist. There are no empty AddApplication/AddInfrastructure extension methods.
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
test gap. Each library has one internal assembly marker for type inspection.

Domain, Engine, Application, and Protocol currently allow no package references,
explicit assembly references, or framework references beyond Microsoft.NETCore.App.
Adding a justified inner-layer package requires deliberately updating this policy
after architecture review. Compiled tests reject ASP.NET Core, EF Core,
Microsoft.Extensions, PostgreSQL, Redis, AWS, and System.Net dependencies there.
Test-only libraries and host test infrastructure never enter production projects.

These checks do not prove determinism or detect every indirect BCL API call,
reflection-based dependency, custom build-time reference injection, or semantic
violation. Future engine behavior tests and code review must enforce explicit time,
randomness, IO, and aggregate invariants. There is no gameplay to test yet.

Integration tests boot the actual host through WebApplicationFactory in Development
and Production, exercise liveness, verify environment configuration, and check that
the root has no endpoint. Liveness proves the host responds, not database readiness.
Configuration conventions and build/format commands are in the root README.

## Modular MVP

Keeping authoritative workflows in one backend avoids distributed transactions,
extra service protocols, and operational complexity during MVP development.
Explicit boundaries preserve testability and allow future separation if measured
scaling or ownership needs justify it. Modules are not independent microservices.
