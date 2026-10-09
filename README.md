# BuildFlow AI

Intelligent Construction Site Resource, Procurement & Workforce Management System.

## Repository structure

- backend/BuildFlow.Api/ — ASP.NET Core 8 API, with Controllers, DTOs, Services,
  Interfaces, Models, Data, Repositories, Migrations, Middleware, Validators and
  Configuration.
- backend/agentic-ai/ — Python agents, tools, schemas, validators, workflows and config.
- frontend/buildflow-web/ — React/Vite app with shared components, layouts, pages,
  features, services, hooks, context, routes and utilities.
- mobile/buildflow_mobile/ — Flutter Android/iOS app with core configuration,
  theme/storage, models, services, screens, widgets, providers and routes.
- .github/workflows/ci.yml — API build/test discovery and React lint/build checks.
- plan/ — complete workflow and web/mobile design references.

The workflow document calls the React parent folder web/. This repository retains
the existing frontend/ name; frontend/buildflow-web is the equivalent location.
Empty directories contain .gitkeep files so Git preserves the structure.

## Current scope

The shared authentication foundation is implemented. The API persists users, roles,
and hashed refresh tokens in PostgreSQL; issues short-lived JWT access tokens;
rotates refresh tokens; invalidates sessions on logout; and exposes register, login,
refresh, logout, and current-user endpoints. Public registration always assigns the
restricted `SiteEngineer` role.

React provides public authentication pages, startup session validation, automatic
token refresh, protected routes, and role-aware navigation. Flutter provides the
same flow with secure platform token storage, splash restoration, protected home
navigation, and persisted light/dark/system themes. Construction, inventory,
procurement, and Member 04 workforce/equipment/scheduling modules are implemented.
Resource plans run through private downstream agents, backend validation, and
manager approval before committing bookings, reservations, or purchase orders.
See [Member 04](backend/BuildFlow.Api/MEMBER04.md) and the
[implementation and verification report](plan/Implementation_2026-10-04.md).

## Local development

API (.NET 8 SDK):

    dotnet restore backend/BuildFlow.Api/BuildFlow.Api.csproj
    dotnet ef database update --context BuildFlowDbContext --project backend/BuildFlow.Api
    dotnet ef database update --context AppDbContext --project backend/BuildFlow.Api
    dotnet run --project backend/BuildFlow.Api --launch-profile http

Web (Node.js compatible with the installed Vite version):

    cd frontend/buildflow-web
    npm ci
    npm run dev

Mobile (Flutter with Dart matching pubspec.yaml):

    cd mobile/buildflow_mobile
    flutter pub get
    flutter run

Before production, set `ConnectionStrings__DefaultConnection` and `Jwt__Secret`
through environment or secret configuration. The JWT secret must contain at least
32 characters. Do not commit production credentials.

Both migration contexts use the configured default connection or an explicit
`BUILDFLOW_CONNECTION_STRING` override. Apply the contexts in the order above.
The API runtime uses `ConnectionStrings__DefaultConnection`. Back up an existing
database and review legacy data before applying migrations; the implementation
report explains schema reconciliation and historical procurement links.

See each app README for validation and configuration notes. .env.example files
document local configuration. Real
.env files remain ignored. The API uses appsettings and ASP.NET Core configuration.

## Backend testing

The member-organized xUnit project is `tests/BuildFlow.Api.Tests`.
See [backend testing instructions](tests/BuildFlow.Api.Tests/README.md) for unit/API/PostgreSQL
commands, member filters, CI artifacts, and [verified local results](tests/BuildFlow.Api.Tests/RESULTS.md).
The root `BuildFlow.sln` includes the API and both backend testing projects.

## Architecture and next steps

Flutter/React → ASP.NET Core → PostgreSQL and the internal Python agent service.
Clients must not call Python directly. AI proposals require backend validation and
manager approval before final allocations or procurement.

Follow the plans in order: shared foundation/authentication, projects/sites,
inventory, procurement, scheduling, then integrated AI workflows and approvals.
LICENSE reserves the planned file location; project owners still need to choose
a license.
