# Phase 3 completion record

## Scope and design

Added MongoDB configuration, the four required persistence models, typed collection access, and index creation. A singleton MongoClient reuses connection pools; MongoDbContext exposes collections for future repositories/services. No generic CRUD endpoints, authentication, account creation, booking rules, or QR workflow were added.

Startup validates settings, pings MongoDB, and awaits required indexes before serving requests. Initialization has a bounded timeout and fails safely if connectivity, permissions, duplicates, or index definitions prevent success. It never deletes conflicting data or indexes. Repeating initialization is safe when definitions match.

Liveness (`/api/health`) remains independent of MongoDB; readiness (`/api/health/ready`) checks the server and returns 200/503 without disclosing connection details. A database outage after successful startup changes readiness without terminating the API. Development has only a loopback URI; production/private URIs come from external configuration.

Models use BSON ObjectIds, UTC dates, string enums, and Decimal128 energy values. NIC normalization and a partial unique index support a prosumer business key while allowing staff without NICs. Latitude/longitude derive from a single persisted GeoJSON point with a 2dsphere index. QR storage reserves a hash field instead of a bearer token. Models do not replace later request DTO validation or service business rules.

## Created files

Under `backend/src/SmartSolarMicrogrid.Api/`:

- `Configuration/MongoDbSettings.cs`
- `Configuration/MongoServiceRegistration.cs`
- `Configuration/MongoDatabaseInitializer.cs`
- `Configuration/MongoReadinessCheck.cs`
- `Models/MongoDocument.cs`
- `Models/DomainEnums.cs`
- `Models/User.cs`
- `Models/SolarStationInfo.cs` (also schedule value types)
- `Models/EnergyBookingSlot.cs`
- `Models/EnergyReservation.cs`
- `Repositories/MongoDbContext.cs`
- `Repositories/MongoIndexInitializer.cs`

Under `backend/tests/SmartSolarMicrogrid.Api.Tests/`:

- `MongoModelTests.cs`
- `MongoConfigurationTests.cs`
- `MongoDatabaseTests.cs`

Also created `docs/phase-3.md` (this record).

## Modified files

- API `SmartSolarMicrogrid.Api.csproj`: MongoDB.Driver 3.10.0 dependency.
- API `Program.cs`: MongoDB DI registration.
- API `Controllers/HealthController.cs`: separate database readiness route.
- API `appsettings.json` and `appsettings.Development.json`: database name, bounded timeouts, local development URI.
- API `SmartSolarMicrogrid.Api.http`: readiness request.
- Tests `ApiFoundationTests.cs`: isolate foundation tests from real database initialization; application startup still requires MongoDB.
- `README.md`, `backend/README.md`, `database/README.md`, and `docs/phases.md`: current progress, database setup, schema/index documentation, and test instructions.

The original empty `Readme` is preserved. Phase 1/2 completion notes remain historical records.

## Validation and fixes

Final verification on 2026-09-16:

- Release build: **0 warnings, 0 errors**.
- Full suite with real MongoDB: **36 passed, 0 failed, 0 skipped**.
- Without a database test URI: **31 passed, 5 explicitly skipped**.
- Live Kestrel: liveness/readiness returned Healthy, and generated OpenAPI included the readiness route.
- Stopping the test MongoDB server made live readiness return **503** while liveness remained **200**.
- Documentation links and whitespace checks passed.

Docker was installed but its daemon was not running, so verification used an official portable MongoDB 8.0.17 download, checked against the published SHA-256 checksum. It ran only on `127.0.0.1:27028` with data in an OS temporary directory. Both temporary servers were stopped afterward; no Windows service, Docker installation, or persistent user database configuration was changed. Follow the database README to start/configure your own development server.

Configuration and model tests cover safe settings errors, URI/database naming rules, normalized NIC, ObjectId/date/enum/decimal serialization, schedule/coordinate round trips, password/QR hash JSON exclusion, unreachable startup, and safe 503 readiness with working liveness. Existing foundation tests still cover validation/errors, CORS, and Swagger.

Real database tests use a configured `SMARTSOLAR_TEST_MONGODB_URI` and random test database names. They verify all four collections, repeatable index creation, unique NIC/station/reservation codes, staff with missing NIC, geospatial searches, decimal persistence, and fresh API startup. They are explicitly skipped if no URI is supplied and drop only their own test databases.

NuGet restore initially encountered sandbox network restrictions; authorized restore succeeded. Tests caught the incompatibility between C# `required` and JSON-ignored PasswordHash; removing that modifier preserved safe JSON exclusion. Later authentication services must require a generated password hash before persisting a user.

## Manual verification

1. Start MongoDB using the [database setup instructions](../database/README.md).
2. Run restore/build/tests using the [backend commands](../backend/README.md). Set `SMARTSOLAR_TEST_MONGODB_URI` to include real database tests.
3. Start the API and open `http://localhost:5080/swagger`. Execute both health endpoints; expect 200/Healthy.
4. Open database `SmartSolarMicrogrid` in Compass/mongosh. Verify Users, SolarStationInfo, EnergyBookingSlots, and EnergyReservations plus the named indexes in the database README.
5. Restart the API: the same indexes should initialize without duplication or errors.
6. With only your local test database, stop MongoDB while the API is running. Liveness should remain 200; readiness should return 503/Unhealthy after the configured timeout. Restart MongoDB to restore readiness.
7. Stop the API, leave MongoDB stopped, and attempt API startup: expect a safe initialization failure instead of accepting requests without indexes.

## Requirement coverage and remaining work

Implemented: server-side MongoDB access/configuration, the four required collection models, internal ObjectIds, unique NIC/station/reservation indexes, useful search/relationship indexes, geospatial persistence, UTC/decimal serialization, safe database startup/readiness handling, and documented setup/tests.

Not implemented: registration/login/JWT/roles, user or station management endpoints, schedule validation, reservation rules, capacity transactions, QR generation/verification, seed accounts, clients, or deployment. Email/login indexes will be selected with the authentication contract. MongoDB references are not foreign keys; future services enforce referential integrity and ownership. The formal marking rubric has not been supplied, so coverage refers to the provided assignment requirements. Phase 4 has not started.
