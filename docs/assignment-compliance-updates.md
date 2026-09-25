# Prosumer administration, reservation editing and NIC keys

Backoffice can create an active prosumer from **Prosumers → Add prosumer**, and edit contact details from **Details / Edit**. NIC is immutable. Self-registration on Android still creates a pending account requiring Backoffice approval. Only Backoffice can reactivate accounts.

Backoffice and Grid Operators can use **Reservations → New reservation** to book for an active prosumer by NIC. Select a station and scheduled slot, then enter the energy amount. **Details → Edit reservation** changes energy or moves the booking to another slot, including at another station. Prosumers remain restricted to their own bookings.

Android booking details now include **Change station or scheduled slot**. Select a station, select its slot, and confirm the change. Selection alone does not submit a reservation change.

## API contracts

| Route | Access | Body / behavior |
|---|---|---|
| POST `/api/prosumers` | Backoffice | `nic`, `fullName`, `email`, `phone`, `password`; creates ACTIVE account |
| PUT `/api/prosumers/{nic}` | Backoffice | `fullName`, `email`, `phone`; preserves NIC and authentication identity |
| PUT `/api/users/{id}` | Backoffice | Contact editing also supports prosumers |
| POST `/api/reservations` | Prosumer / Backoffice / Grid Operator | `slotId`, `energyAmount`; staff must also supply `prosumerNIC` |
| PUT `/api/reservations/{id}` | Owner / Backoffice / Grid Operator | `energyAmount`, optional `slotId`; omission keeps the current slot |

The API enforces active prosumer status, station availability, operating schedule, open slot status, capacity, and the seven-day booking window. Updates require at least twelve hours before the original booking; a replacement slot must also be at least twelve hours away. Capacity is returned to the old slot and deducted from the new slot in the existing MongoDB trading transaction. Failure rolls back the whole operation.

Changing energy or slot resets APPROVED bookings to PENDING and removes the old QR token hash. A new approval and QR token are required. Submitting unchanged values preserves status and QR. Staff cannot change the reservation owner or bypass booking rules. Approval remains Backoffice-only.

## MongoDB identity migration

The live user collection is now `UsersByIdentity`. A prosumer's BSON `_id` is their normalized NIC string; staff `_id` remains an ObjectId. `UserId` stores the existing ObjectId authentication reference, which remains the API `id` and JWT subject. Existing tokens, staff completion references, and NIC-based reservations retain their identities.

API startup creates the new collection indexes, then copies legacy `Users` documents into `UsersByIdentity` in a transaction and records `users-nic-primary-key-v1` in `SchemaMigrations`. The original `Users` collection is retained unchanged. Repeated startup skips a completed migration. Invalid NICs, duplicates or other failures abort the transaction and prevent startup rather than partially migrating accounts.

Stop all old API instances before starting the updated version: old code writes `Users`, while new code writes `UsersByIdentity`. Migration requires a replica set or Atlas, as do existing trading writes. Do not switch back to the old application after new user writes without reconciling the collections. Startup errors may require correcting legacy records or increasing `MongoDb:InitializationTimeoutSeconds` for a larger dataset.

## Android SQLite upgrade

Database version 2 uses `principal_key TEXT NOT NULL PRIMARY KEY`: its value is NIC for prosumers and the server user ID for staff. A CHECK constraint enforces this relationship. `singleton_id` remains a unique session selector, not a primary key. The v1-to-v2 upgrade copies the existing session transactionally and retains the station cache. No password is stored locally. The internal authentication reference remains available as `user_id`.

## Verification

Backend integration tests cover staff creation/editing, immutable NIC, permissions, staff booking, rescheduling, capacity preservation, QR invalidation, and migration preservation/rollback. They use randomly named `ss_test_*` databases when `SMARTSOLAR_TEST_MONGODB_URI` is configured. Web Playwright tests exercise staff forms and both staff reservation roles. Android unit tests cover selected-slot submission and restoration. Run `python scripts/test-sqlite-migration.py` to validate the actual upgrade SQL with SQLite.
