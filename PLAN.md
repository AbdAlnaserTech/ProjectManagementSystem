# Project Management System — implementation plan

## Scope and source
The supplied MASTER DEVELOPMENT PLAN is the requirement source; no specification PDF was supplied. This task implements **Phase 1 only**. Phases 2–9 require subsequent approval. Repository inspection found an empty repository with no commits or remote branches, including no published `main`. Work is on `setup/phase-1`; no user files were overwritten.

## Architecture and stack
A modular monolith: one ASP.NET Core API, controllers delegating to async services, DTO boundaries, EF Core SQL Server persistence, dependency injection. No microservices. .NET 10 LTS (SDK pinned in global.json), compatible explicit NuGet versions and committed dependency lockfiles. Dependencies are prepared now; domain behavior, JWT, validation, mapping, logging, versioning and Swagger are implemented in their respective phases. Local EF tools use a manifest. SQL Server 2022 is the intended development/integration-test database; SQLite and EF InMemory will not stand in for transaction tests.

Future API folders: Controllers, Models, Data, Configurations, DTOs, Services, Interfaces, Validators, Mappings, Middleware, Migrations. Create them with their first implementation rather than empty placeholder classes.

## Proposed decisions awaiting review
These are proposals, not implemented requirements; reconcile with the PDF if supplied.
- Employee deletion: restrict if referenced as project manager or task assignee; use IsActive for retirement. Project deletion: cascade its tasks, with one transactional operation.
- Enums: strings in API JSON, bounded integers in SQL, explicit validation of undefined values.
- Dates: UTC DateTime values; require offset/UTC at API boundary; EndDate >= StartDate. Whether task DueDate must be inside the project window remains to be confirmed; propose yes. No invented future-date restriction.
- Email: trim and normalize before uniqueness comparison; enforce uniqueness in the database too. Define maximum lengths in Phase 2 and document them.
- Only active employees may be assigned tasks or manage projects. Decide handling when an assigned employee becomes inactive; propose retaining assignments but disallowing new ones.
- Completing a project completes Pending/InProgress tasks and preserves Cancelled tasks. Reject creating or reopening unfinished tasks in Completed/Cancelled projects. Reopening projects will not silently reopen completed tasks.
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
- [ ] Implement Employee, Project, ProjectTask and enums.
- [ ] Fluent API: unique email, lengths, required fields, foreign keys, deletion and enum constraints.
- [ ] SQL Server DbContext, environment-only connection string, design-time configuration.
- [ ] Generate/apply initial migration; inspect SQL and create Mermaid ERD.
- [ ] Validate database constraints on real SQL Server.

### Phase 3 — CRUD
- [ ] Request/response DTOs, services/interfaces and async controller endpoints for all three entities.
- [ ] Search/filter/pagination with bounds, stable ordering and metadata.
- [ ] Enforce relationships and consistent 201/204/400/404/409 behavior.

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

## GitHub delivery constraint
The remote repository is empty. During user-authorized final review, an empty local main baseline was created and connected to existing Phase 1 history without rewriting commits. Native Git pushes of main and setup/phase-1 were denied with HTTP 403. A GitHub PR needs both branches published; authorize repository write access before retrying. No PR or automatic merge occurred. See docs/phase-1-results.md.
