# Phase 9 completion record

## Delivered scope

Implemented the server-side QR transaction service for approved reservations. A Prosumer can issue or reissue a QR bearer token for their own approved reservation. The API stores only a SHA-256 hash of the token; the raw token is returned in the issuance response and is never persisted or included in reservation responses.

Grid Operators can submit a scanned token to the central API for verification and can complete the transfer through a separate server-authorized endpoint. Completion uses an atomic MongoDB update requiring the reservation to still be `APPROVED` and the stored token hash to match. The API records `CompletedAt` and `CompletedByOperatorId`, then changes the reservation to `COMPLETED`.

Reissuing a token replaces the previous hash, so the previous token becomes invalid immediately. Invalid tokens return `400 Bad Request`; tokens for pending, cancelled, rejected, or completed reservations return `409 Conflict`. A completed token cannot be used for verification or another completion.

## Created files

- `backend/src/SmartSolarMicrogrid.Api/DTOs/Reservations/QrRequests.cs`
- `backend/src/SmartSolarMicrogrid.Api/DTOs/Reservations/QrResponses.cs`
- `backend/src/SmartSolarMicrogrid.Api/Services/QrTransactionService.cs`
- `backend/src/SmartSolarMicrogrid.Api/Controllers/QrController.cs`
- `backend/src/SmartSolarMicrogrid.Api/Configuration/QrServiceRegistration.cs`
- `docs/phase-9.md`

## Modified files

- `backend/src/SmartSolarMicrogrid.Api/Repositories/IReservationRepository.cs`: added hashed-token lookup, issuance, and atomic completion contracts.
- `backend/src/SmartSolarMicrogrid.Api/Repositories/ReservationRepository.cs`: implemented QR token and completion updates.
- `backend/src/SmartSolarMicrogrid.Api/Program.cs`: registered QR transaction management.
- `backend/src/SmartSolarMicrogrid.Api/SmartSolarMicrogrid.Api.http`: added QR issuance, verification, and completion examples.
- `docs/phases.md`: marked Phase 9 complete.

## API surface

Prosumer QR issuance:

- `GET /api/reservations/{id}/qr`

Grid Operator verification and completion:

- `POST /api/operator/verify-qr`
- `POST /api/operator/complete-transfer`

Backoffice users do not receive Grid Operator QR completion permissions. QR requests are validated against the central API and do not trust token contents as reservation data.

## Verification

- `dotnet build backend/SmartSolarMicrogrid.sln --configuration Release --no-restore`: passed with 0 warnings and 0 errors.
- Focused model/database tests: `4 passed`, `5 skipped` because `SMARTSOLAR_TEST_MONGODB_URI` is not configured.
- API diagnostics: no errors.

## Manual testing

1. Insert or approve a reservation with status `APPROVED` in the development database.
2. Log in as its owning Prosumer and request `/api/reservations/{id}/qr`; keep the returned token private.
3. Log in as a Grid Operator and verify the token; confirm reservation details are returned without exposing the token hash.
4. Complete the transfer and confirm status, completion time, and operator ID are persisted.
5. Submit the same token again and confirm verification/completion returns `409 Conflict`.
6. Reissue a token and confirm the previous token returns `400 Bad Request` while the new token verifies.
7. Try QR endpoints with a Prosumer, Backoffice user, invalid token, cancelled reservation, and pending reservation; confirm authorization and state errors are returned.

## Requirements satisfied

Phase 9 satisfies unpredictable server-generated QR tokens, hash-only persistence, owner-controlled issuance, operator-only central verification, atomic one-time completion, completion audit fields, invalid/cancelled/completed token rejection, and reissue invalidation. Native QR rendering/scanning, Android workflow, and web UI remain later phases.