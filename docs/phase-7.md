# Phase 7 completion record

## Delivered scope

Implemented energy booking slot management in the ASP.NET Core API. Slots are owned by a station and expose authenticated listing/details plus Backoffice-only creation, update, and cancellation.

Slot times are accepted as ISO 8601 values and normalized to UTC before persistence. Capacity is stored as BSON Decimal128. The API rejects reversed or zero-length time ranges, non-positive capacity, available capacity greater than total capacity, invalid statuses, unknown stations, and deactivated stations.

Active slots at one station cannot overlap. The overlap query treats touching slots as valid, so a slot ending at 09:00 may be followed by one starting at 09:00. `DELETE /api/slots/{id}` is a soft delete that changes the status to `CANCELLED`; this preserves the record for future reservation references and history.

## Created files

- `backend/src/SmartSolarMicrogrid.Api/DTOs/Slots/SlotRequests.cs`
- `backend/src/SmartSolarMicrogrid.Api/DTOs/Slots/SlotResponses.cs`
- `backend/src/SmartSolarMicrogrid.Api/Repositories/ISlotRepository.cs`
- `backend/src/SmartSolarMicrogrid.Api/Repositories/SlotRepository.cs`
- `backend/src/SmartSolarMicrogrid.Api/Services/SlotService.cs`
- `backend/src/SmartSolarMicrogrid.Api/Controllers/SlotsController.cs`
- `backend/src/SmartSolarMicrogrid.Api/Configuration/SlotServiceRegistration.cs`
- `docs/phase-7.md`

## Modified files

- `backend/src/SmartSolarMicrogrid.Api/Program.cs`: registered slot services and repository.
- `backend/src/SmartSolarMicrogrid.Api/SmartSolarMicrogrid.Api.http`: added slot request examples.
- `docs/phases.md`: marked Phase 7 complete.

## API surface

Authenticated reads:

- `GET /api/stations/{stationId}/slots`
- `GET /api/slots/{id}`

Backoffice operations:

- `POST /api/stations/{stationId}/slots`
- `PUT /api/slots/{id}`
- `DELETE /api/slots/{id}`

`includeCancelled=true` can be used on station slot listing when historical cancelled slots are needed.

## Verification

- `dotnet build backend/src/SmartSolarMicrogrid.Api/SmartSolarMicrogrid.Api.csproj --no-restore`: passed with 0 warnings and 0 errors.
- `dotnet test backend/tests/SmartSolarMicrogrid.Api.Tests/SmartSolarMicrogrid.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~MongoModelTests|FullyQualifiedName~MongoDatabaseTests"`: passed; Mongo-backed checks require `SMARTSOLAR_TEST_MONGODB_URI` and are skipped when it is absent.

## Manual testing

1. Start MongoDB and the API, then log in as Backoffice.
2. Create a slot with UTC start/end values and verify it appears under the station.
3. Create an overlapping slot and confirm the API returns `409 Conflict`.
4. Send a request with `availableCapacity` greater than `capacity` and confirm `400 Bad Request`.
5. Update a slot, then delete it and confirm its status is `CANCELLED`.
6. Log in as a Grid Operator or Prosumer and confirm slot reads work while slot mutations return `403 Forbidden`.

## Requirements satisfied

Phase 7 satisfies station-scoped slot CRUD, UTC/Decimal128 persistence, capacity and time validation, overlap protection, role-based access, station existence checks, deactivated-station protection, cancellation retention, and meaningful HTTP responses. Reservation approval, capacity deduction during booking, seven-day/twelve-hour rules, and reservation-aware deletion remain Phase 8 scope.