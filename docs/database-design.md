# Phase 2 persistence decisions

Source: the supplied MASTER DEVELOPMENT PLAN and PLAN.md; no PDF was supplied. This phase implements persistence only, with no domain API, DTOs, JWT or CRUD services.

## Constraints and relationships

Every business entity uses SQL Server IDENTITY integer keys. Fluent API configurations are discovered by the DbContext. Required strings and foreign keys are non-nullable. Name/title fields are nvarchar(200), Email nvarchar(320), optional descriptions nvarchar(2000). These lengths are explicit design choices, not quoted specification limits. SQL check constraints reject empty/space-only required text; complete Unicode whitespace and email-format validation are future request-validation concerns.

Email uses a unique index with explicit `Latin1_General_100_CI_AS_SC` collation. This makes comparison case-insensitive and accent-sensitive independently of database defaults. SQL Server also compares trailing spaces equivalently, preventing trailing-space duplicates. Stored spelling is retained in Phase 2; trimming/normalizing submitted email belongs to the later service/request boundary, not a hidden SaveChanges rewrite. This is an academic global-email case-insensitive policy, rather than an attempt to model all mail-server local-part semantics.

`IsActive` has both a CLR initializer and SQL default of true. EF's sentinel is true so omitted/default values use the server default while explicit false persists correctly; a real SQL integration test covers both paths.

A project requires one manager; a task requires one project and one assignee. Both Employee foreign keys use NO ACTION: a referenced employee cannot be deleted. Use IsActive for retirement later. Project-to-task deletion uses SQL CASCADE. This avoids SQL Server multiple-cascade paths through Employee. Deleting a project removes its tasks while preserving all employees; deleting a task does not delete its project or employee. There is no soft-delete column, join table or additional entity.

## Enums and dates

Enums have explicitly pinned ordinal values (0–3) and are stored as SQL integers. Three SQL check constraints reject any undefined enum value even when bypassing EF. See the ERD for the mappings. String enum representation in API JSON is planned for later DTO/API implementation; it is not enabled here.

Dates use required datetime2(7), preserving 100 ns precision with no timezone storage. EndDate >= StartDate is enforced in SQL; equal dates are valid. DateTime.Kind is not retained by SQL Server. UTC is the documented timestamp convention; a later request boundary must validate/normalize offsets and mark responses as UTC. Phase 2 does not silently convert local timestamps or pretend SQL enforces timezone semantics. No future-date or DueDate-within-project constraint is invented. A cross-table due-date rule and active-employee eligibility will require explicit service logic in their approved phases.

## DbContext and migrations

Runtime DbContext is registered with SQL Server through DI, reading `ConnectionStrings:DefaultConnection` (environment form: `ConnectionStrings__DefaultConnection`). The design-time factory reads that environment variable only and fails clearly if missing. No real connection string is stored in appsettings. Runtime options are validated when DbContext is resolved; the liveness-only host can start without database configuration. No automatic startup migrations, retries, secrets logging, seeding or production data mutation is added. EF tools apply the migration explicitly.

`20261008130648_InitialCreate` creates three tables, constraints, indexes and cascade policy. Up/Down were inspected. Down drops dependent tables first and is destructive; do not run rollback against valuable data. A repeated database-update is safe and does not recreate schema or data. All migrations are committed with their model snapshot/designer.

## Local operation and tests

Start SQL: `docker compose up -d --wait --wait-timeout 180 sqlserver`. Preserve ignored .env and the named volume. Source scripts/env.sh, then:

```bash
python scripts/with-database.py dotnet ef database update --project src/ProjectManagement.Api
bash scripts/verify.sh
python scripts/with-database.py dotnet ef migrations has-pending-model-changes --project src/ProjectManagement.Api
```

The helper propagates securely configured connection strings, or builds a local development connection using the ignored .env password without displaying it. `Encrypt=True;TrustServerCertificate=True` is a local self-signed-server development setting; real deployments must use validated server certificates and appropriate least-privilege identities. Never commit credential values. The helper does not override an existing PMS_TEST_CONNECTION, which can designate a dedicated test server.

Tests require real SQL Server and permission to create/drop uniquely named `PmsTests_<guid>` databases. Each test migrates a fresh owned database and drops only that database during cleanup. No SQLite/EF InMemory substitution and no silent skip on missing configuration. Do not run tests with a principal that lacks disposable-database privileges; use an isolated development/test SQL instance. The application database is never dropped by the fixture.

Database integration tests exercise migration repeatability, navigation relationships, unique email behavior through direct SQL, server defaults, explicit inactive employees, all foreign keys, database-side cascade/restricted deletion, required text/null/length constraints, enum constraints, date ordering, and rollback of the employee insert when a dependent project fails. API startup tests remain included. Domain transactions and advanced business rules are later work, not claimed complete.

## Deferred decisions

No row-version columns were added: concurrency requirements are not specified and need review before advanced mutations. Completion cascade, reassignment eligibility, email input normalization, full request date validation and batch limits remain proposals in PLAN.md. AutoMapper licensing review remains relevant before mapping is implemented. This phase exposes no entities over HTTP.
