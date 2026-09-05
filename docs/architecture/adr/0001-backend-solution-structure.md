# Backend Solution Structure — DDD + Clean Architecture

Status: Accepted

## Context

13/31 requires deterministic authoritative gameplay and a modular backend without
microservice complexity. The Unity client and future separate Edge Gateway require
wire contracts independent of authoritative domain state. Infrastructure must not
determine game rules. This iteration establishes boundaries only.

## Decision

Use `ThirteenThirtyOne.sln` with six net10.0 production projects and five xUnit
test projects. Pin stable SDK 10.0.400, use the SDK-default stable language version,
and centralize build properties and NuGet package versions.

Adopt this exhaustive direct-reference graph:

```text
ThirteenThirtyOne.Game.Domain    -> none
ThirteenThirtyOne.Game.Engine    -> Game.Domain
ThirteenThirtyOne.Application    -> Game.Domain, Game.Engine
ThirteenThirtyOne.Infrastructure -> Application, Game.Domain
ThirteenThirtyOne.Protocol       -> none
ThirteenThirtyOne.GameBackend    -> Application, Infrastructure, Protocol
```

GameBackend is the single authoritative backend composition root. Future modules
stay logical modules within that deployable. Engine remains a pure deterministic
library, Domain owns invariants, Application owns orchestration and ports,
Infrastructure owns adapters, and Protocol owns independent transport models.

Enforce boundaries with evaluated project-reference checks and NetArchTest.Rules
compiled checks. Maintain Domain, Engine, Application, Integration, and Architecture
test suites. Bootstrap only default configuration, JSON console logging, and a
liveness endpoint. Defer gameplay, persistence, identity, transport, Edge Gateway,
Unity, and AWS implementation.

## Consequences

Positive: deterministic game code can be tested without a host or external system;
dependency direction is executable policy; transport models cannot accidentally
expose domain models; adapters are isolated; centralized settings keep projects
consistent; a modular deployable reduces MVP operational complexity.

Negative: additional projects and explicit mapping add maintenance overhead;
cross-boundary changes require intentional coordination; architecture tests need
a source checkout and installed SDK; legitimate new dependencies require policy
review; static checks do not replace semantic DDD or determinism review. A single
backend deployable cannot independently scale each logical module without later
architectural work.

See [the architecture guide](../backend-clean-architecture.md) for conventions,
authority order, enforcement scope, and limitations.
