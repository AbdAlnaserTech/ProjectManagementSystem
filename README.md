# Project Management System

Academic ASP.NET Core modular monolith. **Phases 1–2 only**: API host, SQL Server persistence model, initial EF migration and real-database integration tests. No CRUD endpoints, JWT or domain services are implemented. See [PLAN.md](PLAN.md), [database design](docs/database-design.md), [ERD](docs/erd.md), and [Phase 2 verification](docs/phase-2-results.md). The supplied text specification is the requirement source; no PDF was supplied.

## Prerequisites

Linux x64 cloud environment; .NET 10 LTS SDK 10.0.401; SQL Server 2022 Developer; Docker/Compose and Python 3 for local helper scripts. Explicit NuGet versions and lockfiles are committed. AutoMapper 16 academic license eligibility must be reviewed before later mapping implementation; mapping is not used yet.

## Setup, migration and verification

```bash
cd /workspace/ProjectManagementSystem
bash scripts/install.sh
# Configure ignored .env securely with a strong local MSSQL_SA_PASSWORD first.
docker compose up -d --wait --wait-timeout 180 sqlserver
bash scripts/migrate.sh
bash scripts/verify.sh
bash scripts/start-api.sh
```

Copy `.env.example` only if `.env` does not exist; choose a local-only strong SQL password and restrict file permissions. Never overwrite existing secrets or print their values. Alternatively provide `ConnectionStrings__DefaultConnection` securely for an existing SQL Server. No production connection string, password or JWT key is committed. The `.env` file is read by Compose and by the explicit database-command helper; ASP.NET does not load it automatically.

`scripts/install.sh` checksum-verifies the pinned Linux x64 SDK, restores locked dependencies and EF tools, then builds. Source scripts/env.sh before manual dotnet commands in the cloud. Other platforms can use their supported installer for the pinned SDK. `scripts/migrate.sh` explicitly applies the EF migration; app startup never migrates automatically. Repeating migration update is safe. Named SQL volume data is preserved by docker compose down; removing volumes discards data.

`scripts/verify.sh` requires real SQL Server and executes locked restore, build and all tests. It fails if credentials/server are missing; no database tests are silently skipped. The test principal needs create/drop-database rights on a disposable test server. Each test owns a uniquely named PmsTests_<guid> database and drops only that database. To use a separate test server, securely set PMS_TEST_CONNECTION; otherwise the helper uses the configured/local server with an isolated database per test.

The local SQL service binds to loopback port 1433. The local helper uses encrypted transport with TrustServerCertificate=True for the development self-signed server; production connections must validate server certificates and use suitable least-privilege identities. Restricted cloud network settings must retain `westus.data.mcr.microsoft.com` for SQL image blobs, in addition to the package-manager preset. Registry downloads retain TLS/checksum verification.

## Runtime and development

The default API script serves only operational liveness and future controllers at port 5080. `curl --fail http://127.0.0.1:5080/health/live` returns Healthy. This does not probe SQL readiness. `/api/v1/*` domain routes are intentionally absent. Runtime DbContext reads ConnectionStrings:DefaultConnection via application configuration; design-time tools require ConnectionStrings__DefaultConnection. Configuration is checked when the context is resolved; the host can serve liveness without persistence credentials.

For manual commands with local credentials without displaying them:

```bash
source scripts/env.sh
python scripts/with-database.py dotnet ef migrations has-pending-model-changes --project src/ProjectManagement.Api
python scripts/with-database.py dotnet run --no-launch-profile --project src/ProjectManagement.Api --urls http://127.0.0.1:5080
```

Schema supports employees, projects and ProjectTask with required FKs, case-insensitive unique email, constrained enum integers, ordered project dates and documented deletion behavior. Dates are datetime2(7) with a UTC convention; timezone/request validation remains future work. No database models are exposed through HTTP.

## Delivery boundaries

Phase 1 is merged into main. Phase 2 works on feature/phase-2-database. Check docs/phase-2-results.md for actual checks and delivery status. Do not start Phase 3 or merge the PR automatically. JWT, validation/mapping, Serilog, API versioning/Swagger and Postman scenarios remain planned phases.
