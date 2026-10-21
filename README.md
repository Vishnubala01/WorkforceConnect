# WorkforceConnect Portal

Phase 1 foundation: sign-in, roles and permissions, organizations, teams and an employee foundation.
React + TypeScript (Vite) frontend, ASP.NET Core 10 Web API, MySQL 8.

Read `docs/ARCHITECTURE.md` for the ER diagram, role and permission structure, auth and authorization flow, API list and phases.

## Rules the code follows

- A user's access comes from **User → Role → Permissions**. No user id or role name is checked in code.
- Organizations and teams are database rows. The UI fetches them, so adding "ABC" or a "QA" team needs no frontend change.
- The API enforces every permission (`[HasPermission]`) and the caller's **data scope** (All, Organization, Team, Self). Hiding things in the UI is convenience only.
- Ids sent by the client are never trusted; each request is checked against the signed-in user's scope.

## Run it

You need .NET 10 SDK, Node 20+ and MySQL 8.

```bash
# 1. API (http://localhost:5080). Creates the database and seeds roles, permissions, NNX / 7NX / HR & Operations and NNX's teams.
cd backend/src/WorkforceConnect.Api
# Edit ConnectionStrings:Default in appsettings.Development.json, or set it with:
#   dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost;Port=3306;Database=workforceconnect;User=root;Password=...;"
dotnet run

# 2. Frontend (http://localhost:5173, proxies /api to the API)
cd frontend
npm install
npm run dev
```

Development sign-in: `admin@workforceconnect.local` / `ChangeMe!123` (Super Admin).
Demo Manager, Team Lead and Employee accounts are also seeded in development (password `Demo!12345`).

To create the database yourself instead, run `database/schema.sql` and set `Database:AutoCreate` to `false`.

## Before production

- Set `Jwt:Key` (32+ random characters), `Seed:AdminPassword` and the connection string through environment variables or a secret store. The values in `appsettings.Development.json` are for local use only.
- Set `Cors:AllowedOrigins` to your frontend's address, or serve both from one origin.
- Turn off `Seed:DemoData`.

## Checks

```bash
cd frontend && npm run typecheck && npm test && npm run build
cd backend && dotnet test
```

## Status

- Frontend: typechecks, 22 tests pass, production build succeeds, and every screen was checked in a browser against a mock API.
- Backend: code and unit tests are written, but **neither has been compiled or run**. The workspace this was built in has no .NET SDK and can't download one. Run `dotnet build` and `dotnet test` first and send me any errors.
