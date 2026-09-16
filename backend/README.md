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

Restore needs NuGet network access. Configure MongoDB using the [database setup](../database/README.md) and configure a JWT signing key using [authentication setup](../docs/authentication.md). This machine's development secrets already contain Atlas and a generated signing key. The HTTP profile listens at `http://localhost:5080` in Development. Stop with Ctrl+C. Missing JWT configuration, an unreachable database, or failed index creation prevents startup.

| URL | Expected behavior |
| --- | --- |
| `/api/health` | 200 with `status: Healthy` and current `timestampUtc`; no caching |
| `/api/health/ready` | 200 when MongoDB responds; 503 `Unhealthy` during an outage |
| `/api/auth/login` (POST) | Email/NIC + password login; returns bearer token for eligible accounts |
| `/api/auth/prosumer/register` (POST) | Validated registration; creates PROSUMER/PENDING |
| `/api/auth/me` | Safe current-user DTO; requires bearer token |
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
- Development user secrets are enabled by the project `UserSecretsId`. `MongoDb:ConnectionString` defaults to loopback only in Development; production requires an explicit URI. Database name/timeouts are in `appsettings.json`. See [MongoDB configuration](../database/README.md) for private URIs and test setup.
- `Jwt:Issuer`, `Jwt:Audience`, and `Jwt:AccessTokenMinutes` are non-secret settings. `Jwt:SigningKey` is a Base64 random key of at least 32 bytes, provided outside source control. See [authentication configuration and bootstrap](../docs/authentication.md). Do not put secrets in tracked settings or launch profiles.

Logs are structured JSON on the console; no Windows Event Log write permission is required. Unhandled exceptions are logged server-side, while callers receive a safe 500 response with a trace ID. `[ApiController]` provides automatic 400 validation responses; future request DTOs must declare their validation requirements. JSON follows the framework's camelCase convention.

Swagger UI uses the built-in OpenAPI generator; both are exposed only in Development. Microsoft.OpenApi is explicitly pinned to patched version 2.7.5 because its transitive default triggered [GHSA-v5pm-xwqc-g5wc](https://github.com/advisories/GHSA-v5pm-xwqc-g5wc).

## Scope

Phases 2-4 establish the API/MongoDB foundation and authentication. AuthService owns registration/login decisions; UserRepository uses async MongoDB operations and indexes. Default controller authorization and named role policies are available for later endpoints. Models stay internal and responses use DTOs. User administration/approval, domain CRUD, reservation rules, web, and Android remain unimplemented.
