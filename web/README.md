# Smart Solar Microgrid web client

Phases 11-12 provide the React + TypeScript foundation using Vite, Bootstrap 5, React Router, and Axios, plus API-backed login, session restoration, protected routing, logout, and role-aware navigation. Backoffice and Grid Operator feature screens begin in Phases 13-14.

## Run

```powershell
npm install
npm run dev
```

Copy `.env.example` to `.env.local` when the API is not running at `http://localhost:5080/api`. The web client reads `VITE_API_BASE_URL` and stores only the bearer access-token boundary in session storage; `/api/auth/me` restores and revalidates sessions, while the API remains authoritative for identity, roles, and business rules.

Build and lint:

```powershell
npm run build
npm run lint
```
