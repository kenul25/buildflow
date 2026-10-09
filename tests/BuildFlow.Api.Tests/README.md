# Backend tests

One .NET 8 xUnit project, organized by member. Run commands from the repository root.
The root `BuildFlow.sln` includes the API, this project, and the existing console
regression runner; the solution also lets WebApplicationFactory locate the API source.

## Ownership and coverage

| Folder | Component | Coverage |
| --- | --- | --- |
| Member01 | Construction / project / site management | Resource effort calculation, service pagination, DTO validation, controller engineer filtering, project CRUD/archive, date rejection, project ownership |
| Member02 | Material / inventory management | Stock invariants, service pagination, adjustment validation, controller analysis, warehouse persistence, reservation shortage/release |
| Member03 | Supplier / procurement management | UTC deadline handling, supplier validation, comparison controller rejection, supplier persistence, quotation cost/budget/expiry filtering, service request rejection |
| Member04 | Workforce / equipment / scheduling | Overlap and time-window rules, service write permissions, controller pagination, worker persistence/archive, conflicting shifts |
| Shared | Authentication / authorization | Password hashing/validation, anonymous/invalid/expired/disabled-user tokens, login, refresh rotation/replay rejection, logout revocation |
| Infrastructure | Shared setup | Disposable PostgreSQL database, API test host, isolated unit-test helpers |

Each member has separate service, validation, controller, and API integration files.
`Member=01` through `Member=04` traits identify ownership. Shared security tests
should have an agreed team owner/reviewer; the files themselves do not prove who
contributed. Each member should commit and explain their own contribution.

`Category=Unit` tests do not open a database connection. They use real pure logic,
DTO validation, and mocked service/repository boundaries. Focused controller tests
call actions directly; authorization/model binding is verified through HTTP in the
integration tests. Business logic coupled to EF/PostgreSQL transactions is tested
against PostgreSQL rather than an in-memory database.

`Category=Integration` tests run the real API pipeline using WebApplicationFactory,
real signed JWTs and real PostgreSQL. The fixture creates a uniquely named
`buildflow_xunit_<guid>` database, applies BuildFlowDbContext then AppDbContext
migrations, starts the API, and drops only its own database on disposal. Users and
records are unique per scenario; integration execution is serialized to avoid
shared-fixture interference. These scenarios do not call external AI services.

## Run locally

Prerequisites: .NET 8 SDK. Integration tests additionally need PostgreSQL and an
account allowed to create/drop test databases. Configure the connection explicitly:

```powershell
$env:BUILDFLOW_TEST_CONNECTION_STRING = 'Host=localhost;Database=postgres;Username=your_test_user;Password=your_test_password'
```

No application connection string or user secrets are read automatically by the new
fixture. Use your local testing account; keep the value out of Git.

```powershell
dotnet restore tests/BuildFlow.Api.Tests/BuildFlow.Api.Tests.csproj
dotnet build tests/BuildFlow.Api.Tests/BuildFlow.Api.Tests.csproj --no-restore --configuration Release

# Unit tests require no PostgreSQL setup.
dotnet test tests/BuildFlow.Api.Tests/BuildFlow.Api.Tests.csproj --no-build --configuration Release --filter "Category=Unit" --logger "trx;LogFileName=unit.trx" --results-directory .artifacts/test-results/backend

# Real API and PostgreSQL tests; fails if the explicit connection is missing.
dotnet test tests/BuildFlow.Api.Tests/BuildFlow.Api.Tests.csproj --no-build --configuration Release --filter "Category=Integration" --logger "trx;LogFileName=integration.trx" --results-directory .artifacts/test-results/backend

# Run all tests, or filter a member/category.
dotnet test tests/BuildFlow.Api.Tests/BuildFlow.Api.Tests.csproj --configuration Release
dotnet test tests/BuildFlow.Api.Tests/BuildFlow.Api.Tests.csproj --configuration Release --filter "Member=02"
dotnet test tests/BuildFlow.Api.Tests/BuildFlow.Api.Tests.csproj --configuration Release --filter "Member=02&Category=Unit"
```

## CI and assignment evidence

`.github/workflows/ci.yml` runs on pushes and pull requests, including main. Its
backend job starts PostgreSQL 18, restores/builds the API and test projects, retains
the existing `tests/BuildFlow.Integration` regression runner, and explicitly runs
this project's unit and integration categories. Both result files are uploaded as
`backend-test-results`, including on failures when files were produced. Integration
tests still run after a unit-test failure if the test project built successfully.

Local raw TRX results are in `.artifacts/test-results/backend/` (ignored by Git).
[RESULTS.md](RESULTS.md) records the verified local run and each test outcome.
GitHub-hosted execution remains to be verified after pushing these changes.

For the demonstration, explain Arrange/Act/Assert, each rejected operation's
expected status, and why PostgreSQL is needed for migrations, locks, transactions,
and persistence. Show your member-filtered results, the Actions run for the
submitted commit, and the downloaded TRX artifacts. Do not treat UI or Python
agent tests as replacements for these .NET backend tests.

Existing Python, React, and Flutter tests remain in their own app directories and
continue running in the existing CI jobs. This change does not establish new
passing results for those suites.
