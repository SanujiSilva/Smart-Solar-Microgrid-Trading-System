# Phase 6 completion record

## Delivered scope

Implemented solar station management in the ASP.NET Core API. The API now supports authenticated station listing, station details, Backoffice station creation and updates, schedule management, reservation-aware deactivation, staff availability updates, and nearby active-station lookup.

Station coordinates are persisted as one GeoJSON `Point` in longitude/latitude order. This keeps the latitude and longitude values consistent and allows MongoDB to use a `2dsphere` index for nearby searches. API responses expose latitude and longitude through station DTOs without exposing MongoDB implementation details.

Station mutations are kept in `StationService`; controllers only authorize, bind DTOs, and translate successful creation to `201 Created`. The service validates geographic bounds, capacity and availability ranges, station status transitions, time zones, operating periods, overlapping periods, and optimistic-concurrency revisions. A station with pending or approved reservations cannot have its operating configuration changed or be deactivated.

## Created files

- `docs/phase-6.md`

## Existing implementation used for Phase 6

- `backend/src/SmartSolarMicrogrid.Api/Models/SolarStationInfo.cs`
- `backend/src/SmartSolarMicrogrid.Api/DTOs/Stations/StationRequests.cs`
- `backend/src/SmartSolarMicrogrid.Api/DTOs/Stations/StationResponses.cs`
- `backend/src/SmartSolarMicrogrid.Api/Repositories/IStationRepository.cs`
- `backend/src/SmartSolarMicrogrid.Api/Repositories/StationRepository.cs`
- `backend/src/SmartSolarMicrogrid.Api/Services/StationService.cs`
- `backend/src/SmartSolarMicrogrid.Api/Controllers/StationsController.cs`
- `backend/src/SmartSolarMicrogrid.Api/Configuration/StationServiceRegistration.cs`

## Phase record updates

- `docs/phases.md`: marked Phase 6 complete.

The station implementation and MongoDB indexes listed above were already present in the backend and were verified as the Phase 6 implementation surface. No slot, reservation, QR, web, or Android code was changed.

## API surface

Authenticated reads:

- `GET /api/stations`
- `GET /api/stations/{id}`
- `GET /api/stations/{id}/schedule`
- `GET /api/stations/nearby?latitude={lat}&longitude={lon}&radiusKm={km}&limit={n}`

Backoffice operations:

- `POST /api/stations`
- `PUT /api/stations/{id}`
- `PUT /api/stations/{id}/schedule`
- `PATCH /api/stations/{id}/deactivate`

Backoffice and Grid Operator operation:

- `PATCH /api/stations/{id}/availability`

The API remains authoritative for station status, schedule, capacity, availability, and deactivation rules. Slots and reservations are intentionally left to Phases 7 and 8.

## Verification

- `dotnet test backend/tests/SmartSolarMicrogrid.Api.Tests/SmartSolarMicrogrid.Api.Tests.csproj --no-restore --filter "FullyQualifiedName~MongoModelTests|FullyQualifiedName~MongoDatabaseTests"`
- Result: build succeeded; 4 non-Mongo tests passed and 5 Mongo-backed tests were skipped because `SMARTSOLAR_TEST_MONGODB_URI` was not configured.

With MongoDB configured, the skipped tests verify station-code uniqueness, GeoJSON/decimal serialization, geospatial lookup, index initialization, and API readiness.

## Manual testing

1. Start MongoDB and the API, then log in as a Backoffice user.
2. Create an `ACTIVE` station with valid coordinates and at least one weekly operating period.
3. Retrieve it through `GET /api/stations` and `GET /api/stations/{id}`.
4. Query `/api/stations/nearby` using coordinates near the station and confirm it is returned.
5. Log in as a Grid Operator and update availability; confirm a Prosumer cannot perform that operation.
6. Attempt to deactivate a station with unresolved pending or approved reservations and confirm `409 Conflict`.

## Requirements satisfied

Phase 6 satisfies station CRUD, unique station code, MongoDB geospatial storage/indexing, nearby lookup, operating schedule validation, role-aware station administration, availability updates, meaningful validation/conflict responses, and protection against deactivating a station with unresolved reservations. Energy slot behavior, reservation rules, QR workflows, and client applications remain for later phases.