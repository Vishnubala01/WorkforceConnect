# WorkforceConnect Portal — Architecture (Phase 1: Role-Based Foundation)

Stack: **React 18 + TypeScript (Vite)** · **ASP.NET Core 10 Web API** · **MySQL 8 (EF Core + Pomelo)**

> The product spec describes the frontend as Angular. This build uses React as requested in the
> project brief; the folder structure in section 10 maps the Angular layout 1:1
> (guards -> route guard components, interceptors -> API client, services -> API modules).

---

## 1. Architecture overview

```
 Browser (React SPA)                         ASP.NET Core 10 API                       MySQL 8
┌──────────────────────┐   HTTPS + JWT    ┌─────────────────────────────┐          ┌──────────────┐
│ AuthContext          │ ───────────────► │ API  (controllers,           │          │ Organizations│
│ permission-driven UI │                  │      middleware, auth policy)│          │ Teams        │
│ route guards         │ ◄─────────────── │ Application (services, DTOs, │ ───────► │ Employees    │
│ API client           │   JSON / 401/403 │      DataScope, interfaces)  │  EF Core │ Users        │
└──────────────────────┘                  │ Domain (entities, enums)     │          │ Roles        │
                                          │ Infrastructure (DbContext,   │          │ Permissions  │
                                          │      config, seeder, JWT)    │          │ RolePermiss. │
                                          └─────────────────────────────┘          └──────────────┘
```

Dependency direction: `Api -> Application -> Domain`, `Infrastructure -> Application + Domain`.
Domain has no dependencies.

Two independent security layers on every request:

1. **Permission** — "What can this user do?" (`Organization.Create`, ...). Enforced by an
   authorization policy on each endpoint.
2. **Data scope** — "What data can this user reach?" (all / organization / team / self). Enforced
   inside the services on every read and write.

Nothing is keyed on a user id, role name or organization/team name in code. Organizations and
teams are database rows; the UI renders whatever the API returns.

---

## 2. ER diagram

```
                ┌───────────────┐        ┌───────────────────┐        ┌─────────────────┐
                │     Roles     │ 1────n │  RolePermissions  │ n────1 │   Permissions   │
                │ Id, Name,     │        │ RoleId, PermId    │        │ Id, Name,       │
                │ DataScope     │        └───────────────────┘        │ Module          │
                └──────┬────────┘                                     └─────────────────┘
                       │ 1
                       │ n
                ┌──────▼────────┐ 0..1 ─ 1 ┌──────────────────────┐
                │     Users     │──────────│      Employees       │
                │ Email, Hash,  │ EmployeeId│ Code, Name, Email,   │
                │ RoleId,       │          │ OrganizationId,      │
                │ IsActive      │          │ TeamId?              │
                └───┬───────┬───┘          └───────▲──────▲───────┘
                    │n      │n                     │n     │n
        ┌───────────▼──┐ ┌──▼───────────┐          │      │
        │UserOrgAccess │ │UserTeamAccess│          │      │
        │UserId, OrgId │ │UserId, TeamId│          │      │
        └───────┬──────┘ └──────┬───────┘          │      │
                │n              │n                 │      │
        ┌───────▼───────────────▼───┐ 1────n ┌─────┴──────┴──┐
        │       Organizations       │────────│     Teams     │
        │ Name, Description, Active │        │ OrgId, Name   │
        └───────────────────────────┘        └───────────────┘
```

Relationships

| Relationship | Cardinality | On delete |
|---|---|---|
| Role -> Users | 1:n | Restrict |
| Role <-> Permission (RolePermissions) | n:n | Cascade (join rows only) |
| Organization -> Teams | 1:n | Restrict |
| Organization -> Employees | 1:n | Restrict |
| Team -> Employees | 1:n (employee's team optional) | Restrict |
| Employee -> User | 0..1 : 1 | Restrict |
| User <-> Organization (UserOrganizationAccess) | n:n | Cascade (join rows only) |
| User <-> Team (UserTeamAccess) | n:n | Cascade (join rows only) |

Deletes in the API are **soft deletes** (`IsActive = false`) and are refused with `409` while active
children exist.

---

## 3. Database schema

Full DDL: [`database/schema.sql`](../database/schema.sql). Tables: `Roles`, `Permissions`,
`RolePermissions`, `Organizations`, `Teams`, `Employees`, `Users`, `UserOrganizationAccess`,
`UserTeamAccess`. Unique constraints: role name, permission name, organization name,
`(OrganizationId, Name)` for teams, employee code, employee email, user email, one user per employee.

Reference data (permissions, roles, role-permission grants, organizations NNX / 7NX /
HR & Operations, the four NNX teams, the first Super Admin) is seeded by the API at startup
(`DbSeeder`, idempotent). It is data, not code paths: the frontend never contains it.

---

## 4. Role structure

A role is a row in `Roles` with a **data scope level**.

| Role | Data scope | Meaning |
|---|---|---|
| SuperAdmin | `All` | Every organization, team, employee |
| Manager | `Organization` | Organizations assigned in `UserOrganizationAccess` (+ own org), plus any teams assigned |
| TeamLead | `Team` | Teams assigned in `UserTeamAccess` (+ own team) |
| Employee | `Self` | Own employee record only |

New roles are inserted as rows; no code change is needed to add a role that uses an existing scope level.

## 5. Permission structure

Permission names are `Module.Action`. Phase 1 set:

```
Organization.View / Create / Edit / Delete
Team.View         / Create / Edit / Delete
Employee.View     / Create / Edit / Delete
Role.View         / Create / Edit / Delete
```

Default grants (editable in the database through `RolePermissions`, no code involved at runtime):

| Role | Grants |
|---|---|
| SuperAdmin | all 16 |
| Manager | Organization.View · Team.View/Create/Edit · Employee.View/Create/Edit |
| TeamLead | Organization.View · Team.View · Employee.View |
| Employee | Employee.View (limited to self by data scope) |

Future modules (`DailyStatus.*`, `Performance.*`, `Reports.*`) are added by inserting permission rows
and decorating new endpoints with `[HasPermission("...")]`.

---

## 6. Authentication flow

```
Login form ─► POST /api/auth/login {email, password}
                │  verify BCrypt hash (constant-time path for unknown email), user active?
                ▼
          JWT (HS256) { sub=userId, email, role, exp }  ── no permissions inside the token
                │
                ▼
          response { accessToken, expiresAt, user: {id,name,email,role,permissions[],dataScope,org,team} }
                │
                ▼
React AuthContext stores token (sessionStorage) + user ─► API client adds "Authorization: Bearer ..."
On page reload: GET /api/auth/me refreshes the user + permissions.   Any 401 -> logout.
```

Permissions are deliberately **not** embedded in the token. They are read from the database per
request, so revoking a permission or deactivating a user takes effect immediately.

## 7. Authorization flow

```
Request ─► JWT validated (signature, expiry, user still active)
        ─► [HasPermission("Organization.Create")] policy ─► PermissionHandler
                 loads user's permissions (cached for the request) ──► missing  ► 403
        ─► Controller ─► Service
                 loads DataScope (All / OrgIds / TeamIds / EmployeeId)
                 query filtered by scope  /  explicit scope check on the target
                 out of scope ─► 404 (existence is not revealed)
```

Never trusted from the client: `organizationId`, `teamId`, `userId`, `role`. Ids in routes/bodies are
only *requests*; the service verifies each against the caller's scope. The caller's identity comes only
from the validated token. A team's organization must match the organization it is created/updated under;
an employee's team must belong to the employee's organization.

---

## 8. API structure

| Area | Endpoint | Permission |
|---|---|---|
| Auth | `POST /api/auth/login` (anonymous, rate limited) | — |
| | `GET /api/auth/me` | authenticated |
| Organizations | `GET /api/organizations` | Organization.View (scoped list) |
| | `GET /api/organizations/{id}` | Organization.View + scope |
| | `POST /api/organizations` | Organization.Create + scope All |
| | `PUT /api/organizations/{id}` | Organization.Edit + scope |
| | `DELETE /api/organizations/{id}` | Organization.Delete + scope All |
| Teams | `GET /api/organizations/{organizationId}/teams` | Team.View + scope |
| | `GET /api/teams/{id}` | Team.View + scope |
| | `POST /api/teams` | Team.Create + organization in scope |
| | `PUT /api/teams/{id}` | Team.Edit + scope |
| | `DELETE /api/teams/{id}` | Team.Delete + scope |
| Employees | `GET /api/employees?organizationId=&teamId=&search=` | Employee.View (scoped list) |
| | `GET /api/employees/{id}` | Employee.View + scope |
| | `POST` / `PUT /{id}` / `DELETE /{id}` | Employee.Create / Edit / Delete + scope |
| Roles | `GET /api/roles` | Role.View |
| Permissions | `GET /api/permissions` | Role.View |

Errors use RFC 7807 problem details: `400` validation, `401` unauthenticated, `403` missing permission,
`404` not found / out of scope, `409` conflict (duplicate name, children exist).

---

## 9. User flow

```
Login ─► /api/auth/me context loaded ─► land on /organizations
  SuperAdmin:  Organizations (cards from API) ─► NNX ─► Teams (cards from API) ─► team (neutral page)
               Sidebar: Organizations · Employees · Roles & Permissions   (only what permissions allow)
               "New organization", "New team", edit/delete actions appear only with the permission
  Manager:     only assigned organizations; can add teams/employees inside them
  TeamLead:    only assigned teams, read-only
  Employee:    only own record
```

Adding organization "ABC" or team "QA" in the UI writes rows; the lists re-fetch and show them. No
frontend change is required.

## 10. Frontend folder structure (React + Vite + TypeScript)

```
frontend/src/
├── app/                 App.tsx: providers and route table (each route names the permission it needs)
├── core/
│   ├── auth/            AuthContext (login, logout, can()), authStorage (token only), permissions (names)
│   ├── guards/          RequireAuth, RequirePermission
│   ├── http/            apiClient: bearer header, 401 sign-out, problem-details to ApiError
│   ├── services/        authService, organizationService, teamService, employeeService, roleService
│   └── models/          TypeScript types mirroring the API DTOs
├── shared/
│   ├── components/      Modal, ConfirmDialog, Toast, States (loading/empty/error), Fields, PageHeader, Can
│   ├── hooks/           useAsync, useDebounced
│   └── utils/
├── features/
│   ├── authentication/  LoginPage
│   ├── organizations/   OrganizationsPage (cards), OrganizationPage (teams + team details panel)
│   ├── teams/           TeamFormModal
│   ├── employees/       EmployeesPage (search, filters, paging), EmployeeFormModal
│   ├── administration/  RolesPage (roles, data scope, permission matrix; read-only)
│   └── account/         AccountPage (own identity, role, scope, permissions)
├── layout/              AppShell; navigation.ts lists menu items with the permission that unlocks each
└── styles/              global.css (design tokens and components)
```

Selecting a team opens a plain details panel (name, description, employee count, edit/delete, link to
the filtered employee list). It is deliberately not a dashboard.

## 11. Backend folder structure

```
backend/
├── WorkforceConnect.slnx
├── src/
│   ├── WorkforceConnect.Domain/          Entities, Enums
│   ├── WorkforceConnect.Application/     DTOs, Interfaces, Services, Security (DataScope), Exceptions
│   ├── WorkforceConnect.Infrastructure/  AppDbContext, EF configurations, DbSeeder, JWT + password services
│   └── WorkforceConnect.Api/             Controllers, Middleware, Authorization, Program.cs
└── tests/WorkforceConnect.Tests/         Data-scope and permission-policy unit tests
```

## 12. Development phases

| Phase | Content | Status in this delivery |
|---|---|---|
| 1 | Project setup, MySQL, entities, auth, RBAC, permissions, data scope, org/team/employee APIs, Super Admin UI, org/team selection, route protection, tests | Built (backend not yet compiled or run, see README) |
| 2 | Team workspace (what happens after selecting a team) | Not started — spec pending |
| 3 | Daily status | Not started |
| 4 | Performance, goals | Not started |
| 5 | Reports, analytics | Not started |

Extension points: new permission rows + `[HasPermission]`; new role rows with a scope level; new
organization/team rows from the UI; `select()` in `OrganizationPage` is the single place that decides what selecting
a team does.
