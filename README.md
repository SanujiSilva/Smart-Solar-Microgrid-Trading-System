# Smart Solar Microgrid Trading System

A university enterprise application for managing solar microgrid stations, reserving energy, and verifying energy transfers.

## Current progress

**Phase 1 complete: solution architecture and folders.** No API, web, or Android application has been generated yet. Phase 2 is the ASP.NET Core API foundation.

## Architecture

React + Bootstrap 5 and native Android (Kotlin + XML) communicate with an ASP.NET Core Web API using REST/JSON. The API owns all business rules and accesses MongoDB. Android SQLite stores permitted local reference/cache information only.

See [architecture](docs/architecture.md), [data model](database/README.md), and [implementation phases](docs/phases.md).

```text
backend/
  src/SmartSolarMicrogrid.Api/
    Controllers/  Models/  DTOs/  Services/  Repositories/
    Configuration/  Middleware/  Helpers/
  tests/SmartSolarMicrogrid.Api.Tests/
web/
android/
database/
docs/
```

The existing repository is the solution root; no additional nested root is needed. Empty reserved directories contain `.gitkeep` files so Git preserves them.

## Prerequisites and setup plan

- Backend: .NET SDK 10 (10.0.302 detected during Phase 1); target .NET 10 in Phase 2.
- Database: MongoDB; use a replica set when implementing multi-document transactions for reservations.
- Web: Node.js/npm; compatible versions will be recorded when React is scaffolded in Phase 11.
- Android: Android Studio, Android SDK, and its compatible JDK; versions will be recorded in Phase 15.
- Maps: Google Maps Android API key restricted to the application; integration begins in Phase 20.
- Deployment: Windows IIS and a Hosting Bundle matching the backend runtime; setup begins in Phase 24.

### Configuration and execution

There are no runnable projects yet. Backend run/build commands will be added in Phase 2; MongoDB connection/database configuration in Phase 3; JWT issuer, audience, signing secret, and bootstrap account setup in Phase 4. Keep development secrets in .NET user secrets or environment variables and production secrets in deployment configuration. Never commit credentials.

Web run commands and API base URL configuration arrive in Phase 11. Android API base URL and build instructions arrive in Phase 15, SQLite setup in Phase 16, and Maps key configuration in Phase 20. IIS publishing, HTTPS, and production configuration instructions arrive in Phase 24. These are planned deliverables, not currently available features.

## Phase 1 verification

1. Inspect the folder tree above and confirm each reserved directory exists.
2. Read the architecture and check that both clients reach MongoDB only through the API.
3. Check the role boundaries and all twelve business rules in the architecture document.
4. Run `git diff --check` to check tracked changes for whitespace errors; inspect new files with `git status --short`.

Compilation and automated application tests are not applicable until projects exist. See [Phase 1 completion notes](docs/phase-1.md) for scope and remaining work.
