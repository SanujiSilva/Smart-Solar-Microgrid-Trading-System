# Phase 4 completion record

## Scope and architecture

Implemented prosumer registration, login, password hashing, JWT issuing/validation, a protected current-user endpoint, and reusable role authorization policies. Controllers accept validated DTOs and call AuthService; UserRepository performs async MongoDB access. Clients cannot choose their role/status or authorize themselves. No Phase 5 user-management or approval endpoints were added.

New prosumers are always PENDING; approval remains Phase 5. Accounts with DEACTIVATION_REQUESTED retain access until deactivation is approved. Staff log in by email; prosumers can use email or NIC. All controller routes are protected unless explicitly anonymous. A development-only bootstrap command creates the first Backoffice user from externally supplied credentials.

JWTs have an externally configured random signing key, fixed issuer/audience, limited lifetime, server-derived claims, and current-user validation on every authenticated request. Role/status/NIC/version changes are checked centrally. Passwords use framework PBKDF2 hashing; older hashes are upgraded after successful login. Registration uniqueness relies on indexes, including a new case-insensitive unique email index, so concurrent requests cannot create duplicates.

## Files created

Under `backend/src/SmartSolarMicrogrid.Api/`:

- `Controllers/AuthController.cs`
- `DTOs/Auth/AuthRequests.cs`, `DTOs/Auth/AuthResponses.cs`
- `Configuration/JwtSettings.cs`, `Configuration/AuthPolicies.cs`
- `Configuration/JwtBearerEventsHandler.cs`, `Configuration/AuthServiceRegistration.cs`
- `Configuration/BearerOpenApiTransformer.cs`
- `Helpers/ApiException.cs`
- `Repositories/IUserRepository.cs`, `Repositories/UserRepository.cs`
- `Services/PasswordService.cs`, `Services/JwtTokenService.cs`, `Services/AuthService.cs`, `Services/BackofficeBootstrapService.cs`

Tests: `AuthTestSettings.cs`, `AuthIntegrationTests.cs`, and `JwtConfigurationTests.cs` in `backend/tests/SmartSolarMicrogrid.Api.Tests/`.

Documentation: `docs/authentication.md` and this completion record.

## Files modified

- API project file: JwtBearer 10.0.10 package.
- `Program.cs`: authentication, authorization, rate limiting, default controller protection, explicit bootstrap mode.
- `Configuration/ApiServiceRegistration.cs`: Swagger bearer security metadata.
- `Controllers/HealthController.cs`: explicit anonymous health access.
- `Models/User.cs`: TokenVersion for session revocation.
- `Repositories/MongoIndexInitializer.cs`: unique email index with case-insensitive collation.
- `Middleware/GlobalExceptionHandler.cs`: safe expected errors and 401 challenge header.
- `appsettings.json`: non-secret JWT issuer/audience/lifetime.
- `SmartSolarMicrogrid.Api.http`: authentication request examples.
- Existing foundation/MongoDB tests: isolated JWT test settings and distinct fixture emails compatible with uniqueness.
- Root/backend/database READMEs and `docs/phases.md`: configuration, contracts, schema changes, and progress.

Development `Jwt:SigningKey` was generated and stored outside the repository using .NET user secrets. Existing MongoDB configuration was retained. The original empty `Readme` remains untouched.

## Validation

- Release build: **0 warnings, 0 errors**.
- Full suite against isolated local MongoDB: **89 passed, 0 failed, 0 skipped**.
- Tests cover registration validation/privilege injection, pending status, salted hashes, hash upgrades, generic credential failures, concurrent duplicates, case-insensitive email uniqueness, all role-policy combinations, protected defaults, expired/future/wrong-key/wrong-issuer/wrong-audience/unsigned tokens, mismatched claims, status/role/version changes, deletion, throttling, bootstrap, and Swagger security metadata.
- Existing Phase 2/3 regression tests pass.
- Atlas startup successfully created/verified indexes; live readiness returned Healthy, Swagger advertised Bearer authorization, and anonymous `/api/auth/me` returned 401. No Atlas accounts were created or modified during these checks.
- The actual `--bootstrap-backoffice` command exited successfully against an isolated local smoke-test database using a temporary generated password. Both verification servers were stopped afterward. Documentation links and whitespace checks passed.

NuGet restore required network permission after sandbox access was blocked; authorized restore succeeded. No outstanding compilation or test failures remain.

## Testing and requirement coverage

Follow [authentication setup and manual testing](authentication.md) for signing-key setup, initial Backoffice creation, login, Swagger authorization, and pending prosumer registration. Follow [database testing](../database/README.md) to run real database tests; without a test URI, real database tests are explicitly skipped.

Satisfied in this phase: secure password hashing, JWT and role claims, server-side status validation, protected endpoints, separated role policies, auth DTO validation, safe responses, externally stored secrets, concurrent registration uniqueness, and a development bootstrap mechanism.

Still pending: Backoffice user management, prosumer approval/reactivation/profile endpoints, business ownership checks on future domain endpoints, stations/slots/reservations/QR, clients, and deployment. No refresh-token/password-reset workflow is included. Rate limiting is per API instance; production proxy/distributed configuration remains later work. The formal marking rubric has not been supplied. **Phase 5 has not started.**
