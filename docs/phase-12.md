# Phase 12 completion record

## Delivered scope

Connected the web client to the backend authentication contract. The client posts `identifier` and `password` to `/api/auth/login`, stores the returned access token in session storage, restores sessions through `/api/auth/me`, clears invalid tokens on `401`, and supports local logout.

Protected routes redirect anonymous users to `/login` and preserve the requested path for after authentication. The workspace navigation is derived from the server-returned role: Backoffice sees administrative entry points, Grid Operators see operational entry points, and Prosumers see their own dashboard/reservation/station entry points. The API remains responsible for enforcing the permissions; the navigation is only presentation guidance.

The login screen handles loading, invalid credentials, inactive accounts, and API failures without exposing server internals. Feature pages are intentionally placeholders until Phases 13 and 14.

## Created files

- `web/src/auth/authContext.ts`
- `web/src/auth/AuthProvider.tsx`
- `web/src/auth/useAuth.ts`
- `web/src/pages/LoginPage.tsx`
- `web/src/pages/RoleHomePage.tsx`
- `web/src/routes/ProtectedRoute.tsx`
- `docs/phase-12.md`

## Modified files

- `web/src/App.tsx`: added authentication provider, protected routing, role-aware navigation, and logout.
- `web/src/App.css`: added login, workspace, navigation, loading, and responsive states.
- `docs/phases.md`: marked Phase 12 complete.
- `README.md` and `web/README.md`: updated the current web/authentication boundary.

## API contract used

- `POST /api/auth/login` with `{ identifier, password }`.
- `GET /api/auth/me` with the bearer token.
- Login response fields: `accessToken`, `tokenType`, `expiresAtUtc`, and safe `user` details including `role` and `status`.

## Verification

```powershell
Push-Location web
npm run build
npm run lint
Pop-Location
```

Both commands pass with no TypeScript errors and no Oxlint warnings.

## Manual testing

1. Run `npm run dev` from `web` and open the Vite URL.
2. Visit `/dashboard` while signed out; confirm redirection to `/login`.
3. Sign in with a valid Backoffice, Grid Operator, or approved Prosumer account.
4. Refresh the browser; confirm `/api/auth/me` restores the session and the correct navigation appears.
5. Select Sign out; confirm the token is removed and protected routes redirect to login.
6. Try invalid credentials and an inactive account; confirm readable errors appear without exposing API details.

## Requirements satisfied

Phase 12 satisfies API-backed web authentication, session restoration, protected routing, logout, role-aware navigation, safe error/loading states, and server-authoritative permission boundaries. Backoffice and Grid Operator feature interfaces remain for Phases 13-14.