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
navigation, and persisted light/dark/system themes. Business modules and Agentic AI
execution remain for later milestones.

## Local development

API (.NET 8 SDK):

    dotnet restore backend/BuildFlow.Api/BuildFlow.Api.csproj
    dotnet ef database update --project backend/BuildFlow.Api
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

See each app README for validation and configuration notes. .env.example files
document future configuration; copy locally when implementing the loaders. Real
.env files remain ignored. The API uses appsettings and ASP.NET Core configuration.

## Architecture and next steps

Flutter/React → ASP.NET Core → PostgreSQL and the internal Python agent service.
Clients must not call Python directly. AI proposals require backend validation and
manager approval before final allocations or procurement.

Follow the plans in order: shared foundation/authentication, projects/sites,
inventory, procurement, scheduling, then integrated AI workflows and approvals.
LICENSE reserves the planned file location; project owners still need to choose
a license.
