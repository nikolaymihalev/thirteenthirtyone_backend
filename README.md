# 13/31 backend foundation

13/31 is a mobile online multiplayer card game for iOS and Android. The 13-31
platform comprises a planned Unity/C# mobile client and an authoritative .NET
backend. This checkout currently contains only the backend foundation. The
technical solution and namespace root is `ThirteenThirtyOne`.

Implemented: project boundaries, centralized engineering settings, architecture
tests, host integration tests, default configuration, JSON console logging, and
`GET /health/live`. There is no gameplay, persistence, identity, matchmaking,
WebSocket, Unity, Edge Gateway, or cloud infrastructure implementation.

## Structure and direct dependencies

| Project under `src/` | Responsibility | Direct project references |
| --- | --- | --- |
| ThirteenThirtyOne.Game.Domain | Domain vocabulary and invariants (boundary only) | None |
| ThirteenThirtyOne.Game.Engine | Deterministic rules engine (boundary only) | Game.Domain |
| ThirteenThirtyOne.Application | Use cases and ports (boundary only) | Game.Domain, Game.Engine |
| ThirteenThirtyOne.Infrastructure | External adapters (boundary only) | Application, Game.Domain |
| ThirteenThirtyOne.Protocol | Wire contracts (boundary only) | None |
| ThirteenThirtyOne.GameBackend | ASP.NET Core host and composition root | Application, Infrastructure, Protocol |

Five projects under `tests/` cover Domain, Engine, Application, host integration,
and architecture. The three business-layer suites only verify assembly loading;
they make no claim of game correctness. Internal assembly markers let the
architecture suite inspect otherwise empty boundaries without inventing models.

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
XML documentation generation is intentionally not required for empty boundaries.

Start the host:

```sh
dotnet run --project src/ThirteenThirtyOne.GameBackend --no-launch-profile -- --urls http://localhost:5080
curl http://localhost:5080/health/live
```

The endpoint returns HTTP 200 with `Healthy`. It reports process liveness only;
there are no dependency checks or readiness endpoint yet. `/` returns 404.
Stop the process with Ctrl+C. Set `ASPNETCORE_ENVIRONMENT=Development` to load
development overrides; without an environment setting the host uses Production.

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

Only test projects have direct NuGet packages:

| Package | Version | Purpose |
| --- | --- | --- |
| Microsoft.NET.Test.Sdk | 17.14.1 | VSTest discovery/execution |
| xunit | 2.9.3 | Test framework and assertions |
| xunit.runner.visualstudio | 3.1.3 | xUnit adapter for dotnet test and IDEs |
| NetArchTest.Rules | 1.3.2 | Compiled type dependency checks |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.0 | Real host bootstrapping through WebApplicationFactory |

Versions are pinned centrally in `Directory.Packages.props`. No production NuGet
packages are needed; the host uses the ASP.NET Core shared framework.

The upstream rules and locked specifications were not present in this empty
workspace at bootstrap time. This foundation does not interpret game behavior.
Obtain those documents before the next task: **Deterministic Game Domain + Game
Engine implementation**.
