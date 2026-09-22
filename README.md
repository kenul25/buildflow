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

Repository scaffold only. Starter weather/counter demos and Vite artwork were
removed. Flutter desktop and web targets were removed; required Android/iOS project
files, metadata, configuration and lockfiles are retained. Named placeholder files
reserve the locations in the design documents without pretending features exist.

The API exposes GET /health (process health only, not database readiness).
React and Flutter launch minimal BuildFlow shells. Database configuration,
authentication, business modules, AI execution and full interfaces remain for
later milestones. Backend test discovery in CI becomes active when test projects
named *Tests.csproj are added; no backend tests exist yet.

## Local development

API (.NET 8 SDK):

    dotnet restore backend/BuildFlow.Api/BuildFlow.Api.csproj
    dotnet run --project backend/BuildFlow.Api --launch-profile http

Web (Node.js compatible with the installed Vite version):

    cd frontend/buildflow-web
    npm ci
    npm run dev

Mobile (Flutter with Dart matching pubspec.yaml):

    cd mobile/buildflow_mobile
    flutter pub get
    flutter run

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
