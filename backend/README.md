# ASP.NET Core API foundation

`SmartSolarMicrogrid.sln` contains the .NET 10 API in `src/SmartSolarMicrogrid.Api` and xUnit integration tests in `tests/SmartSolarMicrogrid.Api.Tests`.

## Build and run

From the repository root with .NET SDK 10.0.302 installed:

```powershell
dotnet restore backend/SmartSolarMicrogrid.sln
dotnet build backend/SmartSolarMicrogrid.sln --no-restore --configuration Release
dotnet test backend/SmartSolarMicrogrid.sln --no-build --configuration Release
dotnet run --project backend/src/SmartSolarMicrogrid.Api --launch-profile http
```

Restore needs NuGet network access. Start MongoDB first using the [database setup](../database/README.md). The HTTP profile listens at `http://localhost:5080` in Development. Stop with Ctrl+C. A missing/unreachable database or failed index creation prevents startup.

| URL | Expected behavior |
| --- | --- |
| `/api/health` | 200 with `status: Healthy` and current `timestampUtc`; no caching |
| `/api/health/ready` | 200 when MongoDB responds; 503 `Unhealthy` during an outage |
| `/openapi/v1.json` | Generated OpenAPI document in Development |
| `/swagger` | Interactive Swagger UI in Development |
| `/api/does-not-exist` | 404 Problem Details with a `traceId` when requesting JSON |

Liveness checks the process independently of database readiness. Health responses never expose database credentials or topology. Tests inject validation/exception probe controllers from the test assembly; those controllers are not part of the deployed API.

To use local HTTPS, trust the development certificate if necessary and use the HTTPS profile:

```powershell
dotnet dev-certs https --trust
dotnet run --project backend/src/SmartSolarMicrogrid.Api --launch-profile https
```

HTTPS listens at `https://localhost:7080`. Development allows HTTP for local testing. Outside Development the pipeline enables HSTS and HTTPS redirection; configure the deployment certificate, HTTPS endpoint/port, hostname, and proxy settings in the deployment phase. Launch profiles are local development settings and do not configure IIS.

## Configuration

- `appsettings.json`: logging levels, allowed hosts (`localhost;127.0.0.1`), empty browser origin allowlist.
- `appsettings.Development.json`: enables the planned React origin `http://localhost:5173`.
- `Cors:AllowedOrigins`: exact HTTP(S) origins without trailing slashes or paths. Invalid origins fail startup. CORS is a browser policy, not authentication.
- `AllowedHosts`: configure real hostnames when hosting outside localhost.
- Environment variables override settings, for example `Cors__AllowedOrigins__0` and `AllowedHosts`.
- Development user secrets are enabled by the project `UserSecretsId`. `MongoDb:ConnectionString` defaults to loopback only in Development; production requires an explicit URI. Database name/timeouts are in `appsettings.json`. See [MongoDB configuration](../database/README.md) for private URIs and test setup. JWT settings remain Phase 4. Do not put secrets in tracked settings or launch profiles.

Logs are structured JSON on the console; no Windows Event Log write permission is required. Unhandled exceptions are logged server-side, while callers receive a safe 500 response with a trace ID. `[ApiController]` provides automatic 400 validation responses; future request DTOs must declare their validation requirements. JSON follows the framework's camelCase convention.

Swagger UI uses the built-in OpenAPI generator; both are exposed only in Development. Microsoft.OpenApi is explicitly pinned to patched version 2.7.5 because its transitive default triggered [GHSA-v5pm-xwqc-g5wc](https://github.com/advisories/GHSA-v5pm-xwqc-g5wc).

## Scope

Phases 2-3 establish the API foundation and MongoDB persistence infrastructure. One singleton MongoClient provides pooled connections, MongoDbContext exposes typed collections, and an async initializer creates indexes before serving requests. Models stay internal; future controllers must use DTOs and business services. Authentication, domain CRUD, reservation rules, web, and Android remain unimplemented.
