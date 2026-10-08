# Phase 1 validation and delivery

## Verified in the current instance
- Empty checkout inspected, native Git read access succeeds; remote has no advertised refs and no main branch.
- Isolated branch: setup/phase-1. No worktree or overwritten existing source.
- .NET SDK 10.0.401 installed under /workspace/.dotnet; downloaded archive SHA-512 matched Microsoft release metadata.
- NuGet restore and frozen-lockfile restore succeeded. Audit rejected AutoMapper 14.0.0 (GHSA-rvv3-g6hj-g44x); switched to 16.2.0 and restored with no reported advisory warnings. Audit remains enabled and warnings are errors.
- Local dotnet-ef 10.0.12 tool restored and version command executed successfully.
- scripts/install.sh executed successfully again with the installed SDK; it does not rewrite source or lockfiles.
- Build succeeded: 0 warnings, 0 errors.
- Tests executed: 2 passed, 0 failed, 0 skipped (xUnit startup tests using WebApplicationFactory). TRX generated at tests/ProjectManagement.Tests/TestResults/phase1.trx, ignored by Git.
- scripts/start-api.sh launched the real host; HTTP GET /health/live returned 200 and Healthy.
- Shell scripts passed bash syntax checks. Docker Compose configuration validation passed without creating services.

## Blocked / not executed
- Docker daemon 28.4.0 and Compose v2.40.3 are available, with adequate machine resources.
- docker pull mcr.microsoft.com/mssql/server:2022-latest failed with Forbidden while downloading image configuration/layers. Diagnosis: registry manifest works, config blob redirects to westus.data.mcr.microsoft.com, proxy rejects that destination with HTTP 403.
- The destination was saved in the environment network draft, preserving the package-manager preset. Draft saving does not apply runtime policy. Save/apply settings and retry docker compose up -d sqlserver, then verify SQL SELECT 1 via its health check.
- SQL startup, database SELECT 1, migrations and transactional tests have not run. Database-dependent setup remains incomplete; process liveness does not prove database readiness.
- No PDF was supplied. Proposed business rules, endpoint designs, date boundaries, deletion policy and AutoMapper license eligibility need review before their implementation phases.
- Remote main does not exist, so a PR cannot currently be opened against that base. No push/merge has been attempted. Local-only commits require preservation: cloud fresh-task restoration has not been verified and is not reliable for local-only commits. Publish/preserve through a supported reviewed workflow; do not reset or automatically push to hide the limitation.

## Scope boundary
Phase 2 has not started. Entities, migrations, domain controllers, JWT, validation, mapping, Serilog, versioned OpenAPI, ERD, Postman scenarios and SQL integration tests remain planned deliverables. All files in this formerly empty checkout are newly created; no existing tracked files were modified.
