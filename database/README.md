# Planned MongoDB data model

These are design contracts only. Database configuration, models, index creation, and seed code begin in later phases.

| Collection | Planned fields |
| --- | --- |
| Users | Id, NIC, FullName, Email, Phone, PasswordHash, Role, Status, CreatedAt, UpdatedAt |
| SolarStationInfo | Id, StationCode, Name, Address, Latitude, Longitude, CapacityKWh, AvailableBatterySlots, Status, OperatingSchedule, CreatedAt, UpdatedAt |
| EnergyBookingSlots | Id, StationId, StartTime, EndTime, Capacity, AvailableCapacity, Status, CreatedAt, UpdatedAt |
| EnergyReservations | Id, ReservationCode, ProsumerNIC, StationId, SlotId, EnergyAmount, ReservationDateTime, Status, QrToken, CreatedAt, UpdatedAt, CompletedAt, CompletedByOperatorId |

Use ObjectIds for internal IDs and references. NIC is the prosumer business key and must be normalized and unique. Staff may omit NIC; use a partial unique index on populated NIC strings so missing staff NIC values do not collide. Prosumers must have a NIC. Keep NIC immutable initially to preserve reservation references.

User roles: BACKOFFICE, GRID_OPERATOR, PROSUMER. User statuses: PENDING, ACTIVE, DEACTIVATION_REQUESTED, DEACTIVATED. Reservation statuses: PENDING, APPROVED, CANCELLED, COMPLETED, REJECTED. Define station/slot statuses and schedule shape in their implementation phases.

Planned indexes:

- Users: partial unique NIC; login identifier index once the login contract is finalized.
- SolarStationInfo: unique StationCode; geospatial index on a GeoJSON location derived from validated longitude/latitude for nearby queries.
- EnergyBookingSlots: StationId + StartTime for station slot lookup.
- EnergyReservations: unique ReservationCode; ProsumerNIC + ReservationDateTime; StationId + Status + ReservationDateTime; SlotId + Status.

MongoDB does not enforce foreign keys. Services validate references and lifecycle changes. Define additional indexes from actual query patterns rather than adding every possible combination.

QR tokens must be unpredictable, excluded from general list/log output, and checked against current reservation state. Prefer persisting a token hash with a deliberate contract for re-displaying/reissuing the QR in Phase 9.

Future development seeding must be explicit and environment-gated, with credentials supplied through configuration. Seed roles, stations, slots, and representative reservation states without hard-coded production passwords.
