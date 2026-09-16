# Phase 5 user and prosumer management

## Routes and access

All routes require JWT authentication. Backoffice policies are checked by controllers and management services. Prosumers use their server-authenticated identity for self-service; request-supplied IDs/NICs never select another account.

| Method and route | Role | Behavior |
| --- | --- | --- |
| GET /api/users | BACKOFFICE | Paginated list of all accounts, optionally filtered |
| POST /api/users | BACKOFFICE | Create an ACTIVE Backoffice or Grid Operator account |
| GET /api/users/{id} | BACKOFFICE | Get a user by internal ObjectId |
| PUT /api/users/{id} | BACKOFFICE | Update staff contact details |
| PATCH /api/users/{id}/status | BACKOFFICE | Approve, deactivate, reactivate, or decline a deactivation request |
| GET /api/prosumers | BACKOFFICE | Paginated prosumer list, including pending accounts |
| GET /api/prosumers/{nic} | BACKOFFICE | Get a prosumer by business NIC |
| PUT /api/prosumers/me | PROSUMER | Update own contact details |
| POST /api/prosumers/me/deactivation-request | PROSUMER | Request own account deactivation |
| PATCH /api/prosumers/{nic}/activate | BACKOFFICE | Reactivate a DEACTIVATED prosumer |

Creation returns 201 with a Location header pointing to the new user. Other successful operations return 200. Details/list responses include ID, NIC where applicable, contact fields, role/status, and UTC creation/update timestamps. They never include password hashes, TokenVersion, or Revision. All account responses disable caching.

## Requests and filtering

Create staff with `fullName`, `email`, `phone`, `password`, and `role` (BACKOFFICE or GRID_OPERATOR). The API assigns ACTIVE status, an ObjectId, and a salted password hash; clients cannot supply status/NIC/ID. Prosumers continue to register through `/api/auth/prosumer/register` and start PENDING.

Both profile update routes accept only `fullName`, `email`, and `phone`. Contact fields are trimmed before validation; emails are normalized to lowercase by the service. Roles, NICs, passwords, and internal session/revision fields are immutable through these routes. Password reset and role reassignment are outside this phase. Backoffice staff cannot edit a prosumer's contact details through the staff update route; prosumers control their own profile. Backoffice can review those accounts and manage their status.

Lists accept `page` (default 1, max 100000), `pageSize` (default 20, max 100), `search` (max 100 characters), `role`, and `status`. The prosumer list only accepts an omitted/PROSUMER role. `search` is a literal, case-insensitive substring match on name, email, or NIC; it is escaped rather than treated as a client-supplied regular expression. Results sort by CreatedAt descending with an ID tie-breaker. The response is `{ items, totalCount, page, pageSize }`. Count and rows are separate reads, so concurrent changes may be reflected between reads.

Examples:

```text
GET /api/prosumers?status=PENDING&page=1&pageSize=20
GET /api/prosumers?status=DEACTIVATION_REQUESTED
GET /api/users?role=GRID_OPERATOR&status=ACTIVE
GET /api/users?search=example
```

## Status workflow

`PATCH /api/users/{id}/status` accepts only `{ "status": "ACTIVE" }` or `{ "status": "DEACTIVATED" }`.

| Current status | Action | Result |
| --- | --- | --- |
| PENDING | Backoffice sets ACTIVE | Registration approved; login permitted |
| PENDING | Backoffice sets DEACTIVATED | Registration declined; login denied |
| ACTIVE | Prosumer requests own deactivation | DEACTIVATION_REQUESTED; access continues until reviewed |
| DEACTIVATION_REQUESTED | Backoffice sets DEACTIVATED | Request approved; login and previous tokens denied |
| DEACTIVATION_REQUESTED | Backoffice sets ACTIVE | Request declined; account active; sign in again |
| ACTIVE | Backoffice sets DEACTIVATED | Account disabled; login and previous tokens denied |
| DEACTIVATED | Backoffice sets ACTIVE | Account reactivated; new login required |

The prosumer-specific `/activate` route accepts only DEACTIVATED accounts; use the status route for pending approvals. Only Backoffice can activate/reactivate accounts through either route. Backoffice cannot deactivate its own account. Roles remain immutable, so this phase adds no role-demotion path.

Every actual Backoffice status change increments TokenVersion. Old tokens therefore remain invalid even after the account is reactivated. Setting the already-current status is an idempotent 200 without token revocation. Repeating an own deactivation request also returns 200 without duplicating a state change. A deactivation request itself does not revoke the requesting prosumer's session.

Each profile/status mutation also uses an atomic Revision check. If another request changes the account between reading and writing it, the stale operation returns 409 instead of overwriting the newer change. Legacy Phase 1-4 documents without Revision are treated as revision zero. This protects overlapping server operations; clients do not yet supply an ETag for edits based on an older displayed form.

Reservation-aware account deactivation restrictions have not been introduced: bookings are implemented in Phase 8. Existing reservation rules will be applied by those services when added.

## Errors

- 400: invalid input, unsupported role/status, invalid ObjectId/NIC, or invalid pagination.
- 401: missing/invalid/expired/revoked token or inactive authenticated account.
- 403: role does not permit the operation.
- 404: valid identifier but no matching account.
- 409: duplicate email/NIC, overlapping update, self-deactivation, wrong reactivation state, or an attempt to use staff editing for a prosumer profile.

Errors use the existing Problem Details structure. MongoDB unique indexes remain authoritative for duplicate prevention, including concurrent operations.

## Manual end-to-end test

1. Follow [authentication setup](authentication.md) to configure MongoDB/JWT and bootstrap your initial Backoffice account if one does not exist.
2. Start the API and open `http://localhost:5080/swagger`. Login as Backoffice and paste the access token into Authorize.
3. POST `/api/users` to create a Grid Operator. GET its Location and list `/api/users?role=GRID_OPERATOR`. Login as that Operator and verify `/api/users` returns 403.
4. Register a prosumer with the public registration endpoint. Login returns 403 while the account is PENDING.
5. Authorize as Backoffice; GET `/api/prosumers?status=PENDING`, copy the user's `id`, and PATCH `/api/users/{id}/status` with ACTIVE.
6. Login as the prosumer, authorize with that token, and PUT `/api/prosumers/me` with contact details. Trying to add a role/NIC field returns 400; querying another identity does not change the selected account.
7. POST `/api/prosumers/me/deactivation-request`. Expect DEACTIVATION_REQUESTED. Backoffice can find it using the corresponding status filter.
8. Authorize as Backoffice and set DEACTIVATED. The prosumer's old token now returns 401; a new login returns 403.
9. As Backoffice, PATCH `/api/prosumers/{nic}/activate`. The prosumer can now obtain a new token; the old token stays invalid.
10. Run the full MongoDB integration suite using [database test instructions](../database/README.md) for automated validation of this flow and the previous phases.

These steps deliberately create accounts in your configured development database. Automated verification uses random isolated local test databases and does not create or edit your Atlas users.
