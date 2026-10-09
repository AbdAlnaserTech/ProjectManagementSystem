# Project Management System — implementation plan

## Scope and source
The supplied MASTER DEVELOPMENT PLAN and Phase 3 CRUD instructions are the requirement sources; no specification PDF was supplied. Phase 2 is merged into remote main at `aadb090af6bdd541affc70686ea1d52d915b90cf`. This task implements **Phase 3 only** on `feature/phase-3-crud`, created after fetching and fast-forwarding main. Phases 4–9 require subsequent approval. Existing schema, migrations, commits and ignored local SQL configuration are preserved.

## Architecture and stack
A modular monolith: one ASP.NET Core API, controllers delegating to async services, DTO boundaries, EF Core SQL Server persistence, dependency injection. No microservices. .NET 10 LTS (SDK pinned in global.json), compatible explicit NuGet versions and committed dependency lockfiles. Dependencies are prepared now; domain behavior, JWT, validation, mapping, logging, versioning and Swagger are implemented in their respective phases. Local EF tools use a manifest. SQL Server 2022 is the intended development/integration-test database; SQLite and EF InMemory will not stand in for transaction tests.

Future API folders: Controllers, Models, Data, Configurations, DTOs, Services, Interfaces, Validators, Mappings, Middleware, Migrations. Create them with their first implementation rather than empty placeholder classes.

## Decisions and remaining proposals
Phase 2 schema decisions are documented in docs/database-design.md; Phase 3 API decisions are in docs/crud-api.md. Advanced/API security rules remain later proposals; reconcile with a PDF if supplied.
- Implemented database deletion: Employee FKs use NO ACTION; Project deletion cascades tasks. Employee retirement via IsActive is available; no retirement workflow is implemented.
- Implemented enums: explicitly numbered integers 0–3 with SQL CHECK constraints. Phase 3 emits readable enum names, accepts defined numeric values and rejects invalid/missing enums.
- Phase 3 dates: require explicit offset/Z, normalize to UTC, output Z; EndDate >= StartDate. Task due-date window/future-date restrictions are not implemented because they are not specified.
- Implemented email uniqueness: SQL case-insensitive/accent-sensitive unique index; trailing-space variants also conflict. Phase 3 trims/lowercases emails consistently on create/update. Lengths: names/titles 200, email 320, descriptions 2000.
- Phase 3 requires active employees for new/changed manager and assignee assignments. Deactivation preserves existing assignments; editing with an unchanged inactive assignment is allowed.
- Phase 5 proposal: completion cascades to Pending/InProgress tasks while preserving Cancelled tasks; task/project reopening rules require review. Phase 3 changes project status without altering tasks or inventing completed-project restrictions.
- Pagination: page starts at 1, default pageSize 20, maximum 100, stable Id ordering, response metadata. Employees search name/email and filter IsActive; projects search name/description and filter status/manager; tasks use required filters.
- Atomic project + tasks: POST /api/v1/projects/with-tasks. Batch reassignment: PATCH /api/v1/projects/{id}/tasks/reassign. Batch priority: PATCH /api/v1/projects/{id}/tasks/priority. Reject empty/duplicate task IDs; all IDs must exist and belong to that project. Limits and concurrency expectations need review; propose 100 IDs per request and SQL row-version conflict handling.
- JWT: one configured academic login, no user-registration feature; require configured username/password and >=32-byte signing key, issuer/audience and short expiration. Missing configuration fails clearly once authentication is implemented. Secrets are supplied securely, never committed or logged.
- Central errors: ProblemDetails with trace ID; 400 validation, 401 invalid login/token, 404 missing resources, 409 uniqueness/deletion/concurrency conflict, sanitized 500 errors.
- Liveness is a non-domain /health/live operational endpoint. It does not establish SQL readiness or authenticated/domain API behavior.

## Detailed phase checklist
### Phase 1 — inspection, setup, initialization
- [x] Inspect checkout, instructions, remote access, existing changes and tools.
- [x] Create isolated branch without creating a worktree.
- [x] Record plan, ambiguities and scope boundaries.
- [x] Install and checksum-verify .NET SDK in writable workspace.
- [x] Create solution, minimal API host and test project.
- [x] Pin toolchain/packages, prepare local EF tooling and frozen restore.
- [x] Add repeatable setup, startup, verification instructions and ignored local secrets.
- [x] Verify clean build, executed startup tests and live HTTP request (see docs/phase-1-results.md).
- [x] Start SQL Server and execute SELECT 1 (registry access verified; see final results).
- [x] Save cloud setup/start instructions and report exact blockers.
- [x] Stop for approval before Phase 2.

### Phase 2 — persistence
- [x] Implement Employee, Project, ProjectTask and enums.
- [x] Fluent API: unique email, lengths, required fields, foreign keys, deletion and enum constraints.
- [x] SQL Server DbContext, environment-only connection string, design-time configuration.
- [x] Generate/apply initial migration; inspect SQL and create Mermaid ERD.
- [x] Validate database constraints on real SQL Server.

### Phase 3 — CRUD
- [x] Request/response DTOs, services/interfaces and async controller endpoints for all three entities.
- [x] Search/filter/pagination with bounds, stable ordering and metadata.
- [x] Enforce relationships and consistent 201/204/400/404/409 behavior.

- [x] Explicit-offset timestamps, UTC output, readable enums and invalid-enum rejection.
- [x] Basic safe ProblemDetails handling; manual mapping/validation without Phase 6 integrations.
- [x] Comprehensive SQL Server-backed HTTP tests, existing tests, model/cleanup verification.
- [x] Update README, API decisions and Phase 3 verification report; commit/push and attempt PR.

### Phase 4 — authentication
- [ ] Configured login, secure comparison, token generation/validation.
- [ ] Public GET; authenticated mutation routes; login exception.
- [ ] Test missing/expired/malformed JWT and invalid credentials.

### Phase 5 — transactional operations
- [ ] Atomic project-with-tasks, rollback on failure.
- [ ] Completion cascade with approved rule.
- [ ] Atomic reassignment with eligibility and membership checks.
- [ ] Atomic batch priorities; prove unchanged state after any failure.

### Phase 6 — validation, mapping, errors, logging
- [ ] FluentValidation for every request, including batches and query parameters.
- [ ] AutoMapper profiles with configuration tests; never return entities.
- [ ] Central ProblemDetails handling and SQL error classification.
- [ ] Serilog console/daily files/request logging; sanitize sensitive data.

### Phase 7 — versioning/OpenAPI
- [ ] URL API versioning /api/v1, version policy and endpoint descriptions.
- [ ] Swagger schemas, JWT security and expected status codes.

### Phase 8 — tests/Postman
- [ ] SQL Server-backed integration fixture with isolated databases and cleanup.
- [ ] CRUD, validation, auth, filtering/paging and all transactional failure assertions.
- [ ] Postman success/failure collection using variables; no embedded secrets.

### Phase 9 — delivery
- [ ] README setup, secure configuration, API examples and business rules.
- [ ] ERD, migrations, generated OpenAPI, Postman JSON and sanitized sample log.
- [ ] Build/test on SQL Server; report passed/failed/skipped/unrun separately.
- [ ] Reviewable commits and PR where supported; never auto-merge.

## GitHub delivery status
Phase 2 is merged into main at aadb090. Phase 3 starts from that verified commit; its delivery results are in docs/phase-3-results.md. Never auto-merge. Phase 4 has not started.
