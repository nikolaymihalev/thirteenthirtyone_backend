# 13/31 backend

13/31 is a mobile online multiplayer card game for iOS and Android. The 13-31
platform comprises a planned Unity/C# mobile client and an authoritative .NET
backend. This checkout contains the foundation, deterministic engine and Development gameplay harness. The
technical solution and namespace root is `ThirteenThirtyOne`.

Implemented: deterministic gameplay, immutable snapshots, ChaCha20 RNG, canonical hashes,
replay tests, application orchestration, atomic in-memory sessions, Development-only
HTTP gameplay and Swagger, JSON logging and `GET /health/live`. Persistence, identity,
matchmaking, WebSocket, Unity, Edge Gateway and cloud infrastructure remain future work.

## Structure and direct dependencies

| Project under `src/` | Responsibility | Direct project references |
| --- | --- | --- |
| ThirteenThirtyOne.Game.Domain | Immutable state, invariants, inputs and events | None |
| ThirteenThirtyOne.Game.Engine | Deterministic rules, RNG, validation and hashing | Game.Domain |
| ThirteenThirtyOne.Application | Development use cases, safe projections and session port | Game.Domain, Game.Engine |
| ThirteenThirtyOne.Infrastructure | Atomic in-memory session adapter | Application, Game.Domain |
| ThirteenThirtyOne.Protocol | Wire contracts (boundary only) | None |
| ThirteenThirtyOne.GameBackend | ASP.NET Core host and composition root | Application, Infrastructure, Protocol |

Five projects under `tests/` cover domain invariants, engine rules and golden replays,
application use cases, concurrent store/HTTP flows and architecture. Protocol remains
reserved for future production wire contracts; development DTOs do not belong there.

## Prerequisites and commands

Install stable .NET SDK **10.0.400** (or a later patch in the 10.0.4xx feature
band). `global.json` rejects previews and prevents feature-band/major upgrades.
All projects target `net10.0`, using the SDK-default C# version. Package restore
requires NuGet.org access. No database, containers, or cloud credentials are needed.

Run from the repository root:

```sh
dotnet --info
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build
dotnet format --verify-no-changes --no-restore
```

Use `dotnet format --no-restore` to apply formatting. For CI, set `CI=true` or
pass `-p:ContinuousIntegrationBuild=true` to the build. Nullable checking,
warnings as errors, deterministic compilation, SDK analyzers, and build-time
code style enforcement are centralized in `Directory.Build.props`.
XML documentation generation is not globally required.

Start the host:

```sh
dotnet run --project src/ThirteenThirtyOne.GameBackend --no-launch-profile -- --urls http://localhost:5080
curl http://localhost:5080/health/live
```

The endpoint returns HTTP 200 with `Healthy`. It reports process liveness only;
there are no dependency checks or readiness endpoint yet. `/` returns 404.
Stop the process with Ctrl+C. Set `ASPNETCORE_ENVIRONMENT=Development` to load
development overrides; without an environment setting the host uses Production.

For manual gameplay, set `$env:ASPNETCORE_ENVIRONMENT = 'Development'` in PowerShell
before starting with the command above, then open http://localhost:5080/swagger/index.html.
See the [gameplay API guide](docs/testing/development-gameplay-api.md) and editable
[HTTP requests](http/development-gameplay.http). All games disappear on process restart.
Gameplay routes and Swagger are absent outside Development.

## Configuration and secrets

`WebApplication.CreateBuilder` retains normal ASP.NET Core precedence: base
appsettings, environment appsettings, user secrets in Development when configured,
environment variables, then command-line arguments (highest priority).
For example, `Logging__LogLevel__Default=Debug` overrides the base log level.
JSON console logs include UTC timestamps and scopes.

Never put production secrets in appsettings or source control. Use environment
variables for runtime settings; AWS Secrets Manager integration will be introduced
when needed, with its precedence explicitly documented then. Bind real future
configuration sections to strongly typed options and validate them at startup.
No speculative options or secret-provider packages are installed. Local settings
files are ignored as a precaution, but are not automatically loaded by the host.
Restrict `AllowedHosts` to deployment hostnames when deployment is configured.

## Architecture and testing

Read [the architecture guide](docs/architecture/backend-clean-architecture.md)
and [ADR 0001](docs/architecture/adr/0001-backend-solution-structure.md).
Architecture tests inspect evaluated MSBuild references in Debug and Release,
including unused references and imports, and compiled dependencies with
[NetArchTest.Rules](https://www.nuget.org/packages/NetArchTest.Rules/1.3.2).
Run these tests from a built source checkout with the SDK available on PATH.

Packages are pinned centrally:

| Package | Version | Purpose |
| --- | --- | --- |
| Microsoft.NET.Test.Sdk | 17.14.1 | VSTest discovery/execution |
| xunit | 2.9.3 | Test framework and assertions |
| xunit.runner.visualstudio | 3.1.3 | xUnit adapter for dotnet test and IDEs |
| NetArchTest.Rules | 1.3.2 | Compiled type dependency checks |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.0 | Real host bootstrapping through WebApplicationFactory |
| Swashbuckle.AspNetCore | 10.2.3 | Development OpenAPI generation and interactive Swagger UI in host |

Versions are pinned centrally in `Directory.Packages.props`. The host uses the ASP.NET
Core shared framework and Swagger; Domain, Engine, Application and Protocol use no packages.

The locked sources are indexed in [docs/README.md](docs/README.md). Read the
[engine contract](docs/architecture/game-engine.md) and
[rule traceability](docs/architecture/game-engine-rule-traceability.md).
Next recommended milestone: **PostgreSQL Persistence + Authoritative Transaction Primitives**.
