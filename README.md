# Project Management System

Academic ASP.NET Core modular monolith. **Phases 1–3**: API host, SQL Server persistence, initial EF migration and complete Employee/Project/Task CRUD APIs with real-database HTTP integration tests. JWT and advanced operations are not implemented. See [PLAN.md](PLAN.md), [database design](docs/database-design.md), [ERD](docs/erd.md), and [CRUD contract](docs/crud-api.md) and [Phase 3 verification](docs/phase-3-results.md). The supplied text specification is the requirement source; no PDF was supplied.

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

The API script injects configured/local SQL credentials securely through scripts/with-database.py and serves CRUD plus operational liveness at port 5080. Set PMS_API_URLS to use another binding. `curl --fail http://127.0.0.1:5080/health/live` returns Healthy. This does not probe SQL readiness. CRUD routes are `/api/v1/employees`, `/api/v1/projects` and `/api/v1/tasks`, with GET/POST on collections and GET/PUT/DELETE on /{id}. See docs/crud-api.md for request fields, errors, dates and filters. Runtime DbContext reads ConnectionStrings:DefaultConnection via application configuration; design-time tools require ConnectionStrings__DefaultConnection. Configuration is checked when the context is resolved; the host can serve liveness without persistence credentials.

For manual commands with local credentials without displaying them:

```bash
source scripts/env.sh
python scripts/with-database.py dotnet ef migrations has-pending-model-changes --project src/ProjectManagement.Api
python scripts/with-database.py dotnet run --no-launch-profile --project src/ProjectManagement.Api --urls http://127.0.0.1:5080
```

Schema supports employees, projects and ProjectTask with required FKs, case-insensitive unique email, constrained enum integers, ordered project dates and documented deletion behavior. Dates are datetime2(7); API inputs must carry an explicit UTC offset or Z and are normalized/output as UTC. No database models are exposed through HTTP.

## API examples

GET /api/v1/employees?search=alice&isActive=true&page=1&pageSize=20

POST /api/v1/employees:

```json
{"fullName":"Alice","email":"alice@example.test","isActive":true}
```

POST /api/v1/projects (replace managerId with a real active employee):

```json
{"name":"Release","startDate":"2026-01-01T00:00:00Z","endDate":"2026-12-31T00:00:00Z","status":"Planning","managerId":1}
```

POST /api/v1/tasks (replace both IDs with real resources):

```json
{"title":"Design","priority":"High","status":"Pending","dueDate":"2026-06-01T00:00:00Z","projectId":1,"assignedEmployeeId":1}
```

GET /api/v1/tasks?projectId=1&assignedEmployeeId=1&status=Pending&priority=High&page=1&pageSize=20

Collection results contain items/page/pageSize/totalCount/totalPages. POST returns 201 with Location, PUT 200, DELETE 204. PUT supplies a full replacement body. Invalid input returns 400, missing resources 404, duplicate/restricted deletion 409. Writes are unauthenticated in Phase 3; JWT access control starts only after Phase 4 approval.

## Delivery boundaries

Phases 1 and 2 were merged into main. Phase 3 is on feature/phase-3-crud; see docs/phase-3-results.md for exact checks/delivery. No database migration was changed. Phase 4, advanced transactions, FluentValidation/AutoMapper integration, Serilog, API versioning/Swagger and Postman scenarios remain later work. Do not start Phase 4 or merge the PR automatically.
