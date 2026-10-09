# Phase 3 CRUD contract and decisions

## Endpoints and responses

| Resource | List / create | Read / replace / delete |
| --- | --- | --- |
| Employees | GET / POST `/api/v1/employees` | GET / PUT / DELETE `/api/v1/employees/{id}` |
| Projects | GET / POST `/api/v1/projects` | GET / PUT / DELETE `/api/v1/projects/{id}` |
| Tasks | GET / POST `/api/v1/tasks` | GET / PUT / DELETE `/api/v1/tasks/{id}` |

GET and PUT return 200; POST returns 201 with a Location header resolving to the new GET endpoint; DELETE returns 204. Missing route resources return 404, invalid request fields/references return 400, and duplicate email/restricted employee deletion return 409. Races detected by SQL unique/FK constraints also return sanitized 409 errors. An unexpected server/database failure returns sanitized 500, including in Development. Errors use ProblemDetails; input errors additionally expose an errors dictionary. A traceId supports correlation without returning exceptions, SQL or credentials.

These are literal `/api/v1` routes; API versioning infrastructure and Swagger integration remain Phase 7. JWT remains Phase 4, so no authentication is applied to these development CRUD routes yet. No batch endpoints, project completion cascade, FluentValidation, AutoMapper or Serilog integration is introduced.

## Request bodies

Employee POST/PUT: fullName (required, trimmed, <=200), email (required, trimmed, basic email format, <=320), isActive (optional, defaults true). Emails are normalized using Trim + ToLowerInvariant on both create/update. SQL's existing unique case-insensitive index remains authoritative for concurrent writers. Updating an employee with its own normalized email is allowed. Employee deactivation does not delete or reassign existing resources.

Project POST/PUT: name (required, trimmed, <=200), description (optional, <=2000), startDate and endDate (required explicit-offset ISO timestamps), status (required enum), managerId (required positive employee ID). Dates compare as UTC instants; EndDate >= StartDate, including equality. New managers must exist and be active. Changing the manager requires an active employee; retaining an existing inactive manager while editing other fields is allowed. Completing a project changes only its status in Phase 3; its tasks are untouched.

Task POST/PUT: title (required, trimmed, <=200), description (optional, <=2000), priority/status (required enums), dueDate (required explicit-offset ISO timestamp), projectId/assignedEmployeeId (required positive IDs). Both references must exist. New/replacement assignees must be active; retaining an inactive assignee is allowed. Moving a task without changing the assignee retains that assignment. No date-window, future-date or completed-project restriction is invented.

PUT replaces all documented fields rather than patching them. Optional descriptions become null when omitted/blank; isActive defaults true when omitted. IDs are route-generated/server-managed, never written from a DTO. Incoming unrelated JSON fields are ignored by the normal serializer; responses expose only explicit DTO fields, never navigation collections or EF metadata.

Readable enum names are emitted (Planning/Active/Completed/Cancelled, Low/Medium/High/Critical, Pending/InProgress/Completed/Cancelled). Defined numeric values 0–3 are accepted for compatibility; undefined numbers/names are rejected in both JSON bodies and query filters. Missing enums and dates are rejected rather than silently becoming enum zero/default timestamps.

Timestamp JSON must include `Z` or an explicit `±HH:mm` offset. Offset-free/local, invalid and missing values return 400. Offset values normalize to UTC before writing SQL datetime2(7). Responses reconstruct the stored UTC convention and emit ISO 8601 ending in Z, preserving 100 ns precision. Existing database dates are interpreted according to the Phase 2 UTC convention; no migration rewrites historical values.

## Query parameters and pagination

Every collection supports page (default 1) and pageSize (default 20, range 1–100). Nonpositive values, oversized pageSize and offsets exceeding the SQL/EF integer limit return 400. Empty collections return totalCount/totalPages 0; an out-of-range page returns empty items with the actual count metadata. Ordering is always ascending Id.

Response envelope: `items`, `page`, `pageSize`, `totalCount`, `totalPages`.

- Employees: search (FullName OR Email), isActive.
- Projects: search (Name OR Description), status, managerId.
- Tasks: projectId, assignedEmployeeId, status, priority.

Filters combine with AND; search matches either specified field and follows column/database collations. Search is trimmed and blank search is ignored. Positive filter IDs may legitimately match nothing; missing related IDs in a write body are invalid input. Queries remain IQueryable: filtering/counting/ordering/paging execute on SQL Server, using AsNoTracking and direct response projections. Related summaries are joined in the page query instead of per-item queries. A command-interceptor integration test proves exactly one COUNT plus one joined SQL OFFSET/FETCH page query. Count/page are two read statements, not a snapshot transaction; concurrent changes can affect their relative consistency.

## Boundaries and remaining work

Controllers handle HTTP only; scoped service interfaces implement async persistence operations and propagate CancellationToken through every database call. Basic DataAnnotations/IValidatableObject checks and manual projection mapping are temporary and should integrate with FluentValidation/AutoMapper in Phase 6 without changing the DTO contract unnecessarily. The built-in exception handler is only a safe CRUD boundary; full Phase 6 logging/error infrastructure remains deferred. No startup migrations or dependency/migration/model changes are needed.

Existing SQL deletion rules are preserved. SQL FKs/unique indexes protect against deletion/reference races and duplicate emails. Active-employee eligibility is checked when validating an assignment; concurrent deactivation and lost updates are not serialized in this phase. No row-version or new transaction/concurrency requirement is invented. Advanced transactional operations and completion cascades remain Phase 5.
