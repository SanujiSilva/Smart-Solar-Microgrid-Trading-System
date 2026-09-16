# Smart Solar Microgrid web client

Phase 11 provides the React + TypeScript foundation using Vite, Bootstrap 5, React Router, and Axios. The current screen is a connected foundation shell; authentication and role-specific workflows begin in Phases 12-14.

## Run

```powershell
npm install
npm run dev
```

Copy `.env.example` to `.env.local` when the API is not running at `http://localhost:5080/api`. The web client reads `VITE_API_BASE_URL` and stores only the bearer access-token boundary in session storage; the API remains authoritative for identity, roles, and business rules.

Build and lint:

```powershell
npm run build
npm run lint
```
