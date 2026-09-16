# Smart Solar Microgrid Trading System

A university enterprise application for managing solar microgrid stations, reserving energy, and verifying energy transfers.

## Current progress

**Phases 1-4 complete.** The API includes MongoDB infrastructure, prosumer registration, login, password hashing, JWT authentication, a protected current-user endpoint, and role policies. New prosumers remain PENDING until approval is implemented. Phase 5 is user/prosumer management.

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

- Backend: .NET SDK 10.0.302 or a newer patch in the 10.0.3xx SDK band (`global.json`); targets .NET 10.
- Database: MongoDB; use a replica set when implementing multi-document transactions for reservations.
- Web: Node.js/npm; compatible versions will be recorded when React is scaffolded in Phase 11.
- Android: Android Studio, Android SDK, and its compatible JDK; versions will be recorded in Phase 15.
- Maps: Google Maps Android API key restricted to the application; integration begins in Phase 20.
- Deployment: Windows IIS and a Hosting Bundle matching the backend runtime; setup begins in Phase 24.

### Configuration and execution

Configure MongoDB using the [database setup instructions](database/README.md) and JWT using [authentication setup](docs/authentication.md), then run from the repository root. This machine already has Atlas and a generated JWT key in development user secrets.

```powershell
dotnet restore backend/SmartSolarMicrogrid.sln
dotnet build backend/SmartSolarMicrogrid.sln --no-restore --configuration Release
dotnet test backend/SmartSolarMicrogrid.sln --no-build --configuration Release
dotnet run --project backend/src/SmartSolarMicrogrid.Api --launch-profile http
```

Open `http://localhost:5080/swagger` for interactive documentation, `/api/health` for API liveness, and `/api/health/ready` for MongoDB readiness. MongoDB must be available at startup so required indexes can be created. See [backend instructions](backend/README.md) for configuration, HTTPS, and expected responses.

Development defaults to the non-secret URI `mongodb://127.0.0.1:27017` and database `SmartSolarMicrogrid`, overridden by user secrets when configured. Private MongoDB URIs and `Jwt:SigningKey` belong in user secrets/environment configuration. Both environments require a valid signing key; production also requires an explicit MongoDB URI. See [authentication instructions](docs/authentication.md) to create your first development Backoffice account and test login. Never commit credentials.

Web run commands and API base URL configuration arrive in Phase 11. Android API base URL and build instructions arrive in Phase 15, SQLite setup in Phase 16, and Maps key configuration in Phase 20. IIS publishing, HTTPS, and production configuration instructions arrive in Phase 24. These are planned deliverables, not currently available features.

## Verification

1. Inspect the folder tree above and confirm each reserved directory exists.
2. Read the architecture and check that both clients reach MongoDB only through the API.
3. Check the role boundaries and all twelve business rules in the architecture document.
4. Run `git diff --check` to check tracked changes for whitespace errors; inspect new files with `git status --short`.

5. Run the build/test commands above, then verify login and authorization as described in [Phase 4 completion notes](docs/phase-4.md). Set `SMARTSOLAR_TEST_MONGODB_URI` to enable real database/authentication integration tests; otherwise they are explicitly skipped.

The [Phase 1](docs/phase-1.md), [Phase 2](docs/phase-2.md), and [Phase 3](docs/phase-3.md) notes remain historical records. User management/approval, domain CRUD, reservation rules, and client functionality remain for later phases.
