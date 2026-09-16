# MongoDB setup and data model

Phase 3 implements typed document models, MongoDB configuration, named indexes, startup initialization, and readiness. Authentication, domain operations, and seed data remain for later phases.

## Local setup

Use MongoDB Community Server 8.0 (Phase 3 verification uses 8.0.17) or a compatible Atlas cluster. See the [official Windows ZIP setup](https://www.mongodb.com/docs/v8.0/tutorial/install-mongodb-on-windows-zip/). MongoDB Compass is an optional viewer, not the database server.

With Docker Desktop running, an alternative local-only development server is:

```powershell
docker run --name smartsolar-mongodb -d -p 127.0.0.1:27017:27017 -v smartsolar-mongodb-data:/data/db mongo:8.0.17
```

Use `docker start smartsolar-mongodb` on later runs and `docker stop smartsolar-mongodb` to stop it. This local example has no authentication and binds only to loopback; deployments must use authenticated server configuration. A standalone server is sufficient for Phase 3. Multi-document transactions in Phase 8 require a replica set or Atlas.

The Development API defaults to `mongodb://127.0.0.1:27017` and database `SmartSolarMicrogrid`. It creates the four empty collections and indexes at startup; no manual inserts or seed accounts are required. Start the API using the [backend instructions](../backend/README.md), then visit `/api/health/ready`.

For a different server or Atlas, keep the real URI outside source control. From the repository root, set it with .NET user secrets for Development:

```powershell
dotnet user-secrets set "MongoDb:ConnectionString" "<your MongoDB URI>" --project backend/src/SmartSolarMicrogrid.Api
```

Alternatively provide `MongoDb__ConnectionString` through the process/deployment environment. Other keys are `MongoDb__DatabaseName`, `MongoDb__TimeoutSeconds` (1-60, default 5), and `MongoDb__InitializationTimeoutSeconds` (1-300, default 30). Database names are deliberately restricted to 1-63 ASCII letters/digits/underscores/hyphens, starting with a letter/digit; reserved admin/local/config databases are rejected. Production has no default URI and fails configuration validation when it is absent. The configured database name, rather than a database path in the URI, selects the application database; set URI `authSource` explicitly where required.

The application account needs access to the configured database, including create-index permissions during startup. Atlas also needs an allowed client network address. The connection URI and underlying server errors are not returned by health endpoints.

## Documents

| Collection | Fields |
| --- | --- |
| Users | Id, NIC, FullName, Email, Phone, PasswordHash, Role, Status, CreatedAt, UpdatedAt |
| SolarStationInfo | Id, StationCode, Name, Address, Location (derived Latitude/Longitude), CapacityKWh, AvailableBatterySlots, Status, OperatingSchedule, CreatedAt, UpdatedAt |
| EnergyBookingSlots | Id, StationId, StartTime, EndTime, Capacity, AvailableCapacity, Status, CreatedAt, UpdatedAt |
| EnergyReservations | Id, ReservationCode, ProsumerNIC, StationId, SlotId, EnergyAmount, ReservationDateTime, Status, QrTokenHash, CreatedAt, UpdatedAt, CompletedAt, CompletedByOperatorId |

Internal IDs and references are BSON ObjectIds. Property names are retained in BSON (PascalCase), except `Id` maps to `_id`. C# timestamps are UTC `DateTime` values, stored as BSON dates with millisecond precision. Energy and slot capacity use `decimal`/BSON Decimal128, measured in kWh; battery slot counts remain integers. Future services must set/update timestamps and enforce valid state and capacity changes.

NIC and ProsumerNIC setters trim whitespace and uppercase identifiers. Empty/missing staff NICs are omitted from BSON; a partial unique index applies to string NICs. Prosumer registration must require a valid NIC in its later phase. Models are persistence objects, not request DTOs; raw database writes bypass model normalization. NIC edits will not be exposed by future profile DTOs. Password hashes remain persisted server-side and are excluded from JSON as an additional safeguard; controllers must still use response DTOs.

Enums are stored as readable strings. User roles: BACKOFFICE, GRID_OPERATOR, PROSUMER. User statuses: PENDING, ACTIVE, DEACTIVATION_REQUESTED, DEACTIVATED. Reservation statuses: PENDING, APPROVED, CANCELLED, COMPLETED, REJECTED. Station statuses: INACTIVE, ACTIVE, MAINTENANCE, DEACTIVATED. Slot statuses: CLOSED, OPEN, CANCELLED. New model defaults grant no staff role, station availability, or approved booking.

Stations persist one GeoJSON `Location` with `[longitude, latitude]`; `Latitude` and `Longitude` are derived properties, preventing inconsistent duplicate coordinates. Future station DTOs expose the requested numeric latitude/longitude fields. `OperatingSchedule` contains an IANA `TimeZoneId` (default Asia/Colombo) and weekly periods: day, opening minute, and closing minute after local midnight. An empty list means no declared periods. Phase 6 must validate opening/closing ranges, overlaps, and availability; overnight periods are split at midnight (closing minute can be 1440).

## Indexes and initialization

| Collection | Index name | Fields / behavior |
| --- | --- | --- |
| Users | ux_users_nic | NIC ascending, unique, partial filter NIC type string |
| SolarStationInfo | ux_stations_code | StationCode ascending, unique |
| SolarStationInfo | ix_stations_location | Location 2dsphere |
| EnergyBookingSlots | ix_slots_station_start | StationId + StartTime ascending |
| EnergyReservations | ux_reservations_code | ReservationCode ascending, unique |
| EnergyReservations | ix_reservations_prosumer_date | ProsumerNIC ascending + ReservationDateTime descending |
| EnergyReservations | ix_reservations_station_status_date | StationId + Status + ReservationDateTime ascending |
| EnergyReservations | ix_reservations_slot_status | SlotId + Status ascending |

MongoDB also creates each collection's `_id_` index. Startup pings the database, then creates these named indexes before accepting requests. Repeating the same definitions is safe. Existing duplicate values, incompatible index definitions, or missing permissions cause startup failure; no existing data/indexes are silently deleted or changed. Inspect existing records/indexes with Compass or mongosh and resolve the conflict deliberately before restarting. This is bootstrap initialization, not a general migration system.

MongoDB does not enforce foreign keys. Services validate references and lifecycle changes. Define additional indexes from actual query patterns rather than adding every possible combination.

The model stores `QrTokenHash` instead of a raw bearer `QrToken`, and excludes it from JSON. Phase 9 will implement unpredictable tokens, verification, and reissue/display semantics. No QR transaction behavior exists yet.

Future development seeding must be explicit and environment-gated, with credentials supplied through configuration. Seed roles, stations, slots, and representative reservation states without hard-coded production passwords.

## Verification

The normal `dotnet test` run verifies configuration, serialization, safe outage handling, and the existing API foundation. Real database tests are explicitly skipped unless a URI is provided:

```powershell
$env:SMARTSOLAR_TEST_MONGODB_URI = 'mongodb://127.0.0.1:27017'
dotnet test backend/SmartSolarMicrogrid.sln --configuration Release
Remove-Item Env:SMARTSOLAR_TEST_MONGODB_URI
```

These tests create randomly named `smartsolar_tests_<guid>` databases, check index initialization/uniqueness, coordinate queries, decimal persistence, and real API startup/readiness, then drop only their own test databases. Use a development server/account permitted to create/drop test databases; never a production connection.

For manual inspection, connect Compass/mongosh to the same URI and database. Confirm all four collection names and their indexes. `/api/health` stays live when MongoDB goes down after successful startup; `/api/health/ready` returns 503 with only `Unhealthy` and a UTC timestamp. Restore MongoDB and readiness recovers. If MongoDB is unavailable at startup, the application stops rather than serving without required indexes.

## References

- [MongoDB C# index creation](https://www.mongodb.com/docs/drivers/csharp/current/indexes/)
- [MongoDB C# database commands](https://www.mongodb.com/docs/drivers/csharp/current/run-command/)
- [MongoDB naming limits](https://www.mongodb.com/docs/manual/reference/limits/)
