# Phase 4 authentication

## API contract

| Endpoint | Access | Result |
| --- | --- | --- |
| POST /api/auth/prosumer/register | Anonymous; rate limited | 201 with a safe user DTO, always PROSUMER/PENDING |
| POST /api/auth/login | Anonymous; rate limited | 200 with bearer access token, UTC expiry, and safe user DTO |
| GET /api/auth/me | Valid bearer token | Current authenticated user's profile/role/status |

Registration fields: `nic`, `fullName`, `email`, `phone`, `password`. NIC format is 12 digits or 9 digits followed by V/X. Password length is 12-128 characters; spaces are allowed and passwords are never trimmed. Email and NIC uniqueness are enforced by MongoDB indexes, including concurrent requests. NIC is normalized to uppercase; email is stored lowercase and its unique index/query collation is case insensitive. Login accepts `identifier` (email for any role or NIC for a prosumer) and `password`.

Unknown request properties are rejected. Clients cannot submit role, status, user ID, or hash fields during registration. Responses never include password hashes or session versions. Invalid input returns 400; invalid credentials 401; correct credentials for a pending/deactivated account 403; duplicate NIC/email 409. Error responses use Problem Details and trace IDs.

Login and registration share a limit of ten requests per client IP per minute, returning 429 and Retry-After when exceeded. The limiter is local to each API process; distributed limits and trusted-proxy configuration belong to deployment work. It does not trust arbitrary forwarded headers.

## Accounts and permissions

New prosumers are PENDING and receive no token. Backoffice now approves them with `PATCH /api/users/{id}/status` (ACTIVE); see [Phase 5 management](user-management.md). ACTIVE and DEACTIVATION_REQUESTED accounts may sign in: a deactivation request does not immediately disable an account. PENDING and DEACTIVATED accounts cannot sign in. Unknown roles are rejected.

All controller endpoints require authentication by default through `MapControllers().RequireAuthorization()`. Only login, registration, and health routes explicitly allow anonymous access. Development Swagger/OpenAPI remains accessible for setup/testing.

| Policy | Permitted role(s) |
| --- | --- |
| BackofficeOnly | BACKOFFICE |
| OperatorOnly | GRID_OPERATOR |
| ProsumerOnly | PROSUMER |
| Staff | BACKOFFICE, GRID_OPERATOR |

Operators do not receive Backoffice permissions. Phase 5 applies these policies to real administration and self-service endpoints. Role policy probes used by earlier tests still exist only in the test assembly.

## Passwords and JWTs

Passwords use ASP.NET Core PasswordHasher with salted PBKDF2 and 210,000 iterations. Successful login upgrades older compatible hashes. Unknown-account attempts still perform dummy hash verification. Credentials, hashes, and tokens are not included in application log messages.

JWTs are signed using HS256. Validation requires the configured signing key, issuer, audience, signature, expiration, and allowed algorithm, with zero clock skew. Keep server clocks synchronized. Claims include server-derived `sub` (ObjectId), `role`, optional `nic`, `ver` (TokenVersion), `jti`, and issuance/expiry times. Tokens are signed, not encrypted; clients should treat them as secrets.

Every authenticated request reloads the user from MongoDB and checks current status, role, NIC, and TokenVersion. Deleted/deactivated users, changed roles, and incremented TokenVersion invalidate existing tokens. Future password-change/session-revocation services must increment TokenVersion. MongoDB failure cannot bypass this check. `/api/auth/me` uses this authenticated user, never a user ID/NIC from query input.

Tokens expire after 30 minutes by default. No refresh-token, password-reset, or logout endpoint is part of Phase 4. Clients remove tokens locally when signing out and sign in again after expiry. Later work can add deliberate refresh/revocation flows.

## JWT setup

Non-secret settings in appsettings.json:

```json
{
  "Jwt": {
    "Issuer": "SmartSolarMicrogrid.Api",
    "Audience": "SmartSolarMicrogrid.Clients",
    "AccessTokenMinutes": 60
  }
}
```

`Jwt:SigningKey` must be Base64 encoding of at least 32 cryptographically random bytes. There is no checked-in default, and startup rejects missing/invalid settings. A random development key was saved in this machine's .NET user secrets during Phase 4. Your existing Atlas secret was retained. On another machine, run this from the repository root using PowerShell:

```powershell
$jwtBytes = New-Object byte[] 32
$rng = [Security.Cryptography.RandomNumberGenerator]::Create()
try { $rng.GetBytes($jwtBytes) } finally { $rng.Dispose() }
@{ 'Jwt:SigningKey' = [Convert]::ToBase64String($jwtBytes) } |
    ConvertTo-Json -Compress |
    dotnet user-secrets set --project backend/src/SmartSolarMicrogrid.Api
```

Changing the key invalidates existing tokens. Do not regenerate it on every startup. User secrets are a development facility; production supplies `Jwt__SigningKey` through its secret configuration. Use HTTPS when transmitting real credentials or tokens; the HTTP launch profile is for loopback development.

## Create the first development Backoffice account

The one-shot bootstrap command runs only in Development, initializes MongoDB indexes, hashes the supplied password, creates an ACTIVE/BACKOFFICE user without NIC, and exits without starting HTTP. It refuses to overwrite or reactivate an existing Backoffice account. There are no hard-coded credentials and no automatic account creation on normal startup. Run the command once; subsequent staff accounts can now be created by an authenticated Backoffice user through POST `/api/users`.

In PowerShell at the repository root, collect your own details and send them to user secrets without printing the password:

```powershell
$bootstrapName = Read-Host 'Backoffice full name'
$bootstrapEmail = Read-Host 'Backoffice email'
$bootstrapPhone = Read-Host 'Backoffice phone'
$bootstrapSecurePassword = Read-Host 'Password (12-128 characters)' -AsSecureString
$bootstrapValues = @{
    'Bootstrap:FullName' = $bootstrapName
    'Bootstrap:Email' = $bootstrapEmail
    'Bootstrap:Phone' = $bootstrapPhone
    'Bootstrap:Password' = [Net.NetworkCredential]::new('', $bootstrapSecurePassword).Password
}
$bootstrapValues | ConvertTo-Json -Compress |
    dotnet user-secrets set --project backend/src/SmartSolarMicrogrid.Api
Remove-Variable bootstrapValues, bootstrapSecurePassword
dotnet run --project backend/src/SmartSolarMicrogrid.Api --launch-profile http -- --bootstrap-backoffice
```

After successful creation, remove the bootstrap values; the stored user password hash remains:

```powershell
dotnet user-secrets remove 'Bootstrap:Password' --project backend/src/SmartSolarMicrogrid.Api
dotnet user-secrets remove 'Bootstrap:Email' --project backend/src/SmartSolarMicrogrid.Api
dotnet user-secrets remove 'Bootstrap:FullName' --project backend/src/SmartSolarMicrogrid.Api
dotnet user-secrets remove 'Bootstrap:Phone' --project backend/src/SmartSolarMicrogrid.Api
```

No account was created in your Atlas database during verification. Integration tests create isolated test users locally and remove their test databases.

## Manual verification

1. Configure MongoDB/JWT and bootstrap your Backoffice account as above.
2. Run `dotnet run --project backend/src/SmartSolarMicrogrid.Api --launch-profile http` and open `http://localhost:5080/swagger`.
3. Execute POST `/api/auth/login` with your Backoffice email as `identifier` and your password. Expect a bearer token and safe user response.
4. Click Swagger **Authorize**, paste only the access token, and execute GET `/api/auth/me`. Expect your profile with BACKOFFICE role. Clear authorization and repeat: expect 401.
5. Register a prosumer with your chosen valid NIC/email/phone and a 12-128 character password. Expect 201 and PENDING, with no token. Login returns 403 until a Backoffice user approves the account through PATCH `/api/users/{id}/status` with ACTIVE.
6. Repeat registration using the same NIC or email: expect 409. Add a role/status property or invalid NIC: expect 400.
7. Run the real database test suite for all role combinations, expired/invalid tokens, revocation, concurrent registration, hashing, and throttling. These tests do not require you to manually edit account roles/statuses.

## References

- [Microsoft: JWT bearer authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0)
- [Microsoft: password hasher configuration](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration?view=aspnetcore-10.0)
- [Microsoft: OpenAPI customization](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/customize-openapi?view=aspnetcore-10.0)
