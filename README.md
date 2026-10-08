# Project Management System

Academic ASP.NET Core modular monolith. **Phase 1 scaffold only**: the host and development/test infrastructure exist; domain CRUD, SQL persistence, authentication and Swagger are not implemented yet. See [PLAN.md](PLAN.md) for the full phased checklist and proposed business rules. No specification PDF was supplied.

## Toolchain

- Linux x64 cloud environment; .NET 10 LTS SDK 10.0.401 (global.json).
- SQL Server 2022 Developer, Docker/Compose for future relational integration tests.
- Explicit package versions and lockfiles; EF Core SQL Server/design and JWT 10.0.12, FluentValidation 12.1.1, AutoMapper 16.2.0, Serilog, API versioning and Swagger dependencies are prepared, not yet wired up.
- AutoMapper 16 licensing must be reviewed for academic eligibility before Phase 6; no license key is required for the current scaffold, which does not invoke mapping.

## Setup and verification

```bash
cd /workspace/ProjectManagementSystem
bash scripts/install.sh
bash scripts/verify.sh
bash scripts/start-api.sh
```

The installation script uses workspace-local CLI state/caches, verifies the SDK SHA-512 against Microsoft's published release metadata, performs locked restore and builds. It is tailored to Linux x64; other developers may install the pinned SDK with their platform's supported installer. Source `scripts/env.sh` before manual dotnet commands in the cloud.

In a second shell, `curl --fail http://127.0.0.1:5080/health/live` must return `Healthy`. This is process liveness only, not a database-readiness check. `/api/v1/*` domain endpoints do not exist in Phase 1. Two startup tests exercise the real ASP.NET host, including the phase boundary. Future domain and SQL transaction tests belong to later phases.

## SQL Server development prerequisite

The API does not connect to SQL in Phase 1. To prepare the database on a machine with registry access:

1. Copy `.env.example` to ignored `.env`; set a unique local strong `MSSQL_SA_PASSWORD` (SQL password policy: at least 8 characters and 3 character categories).
2. `docker compose up -d sqlserver`
3. `docker compose ps` — wait for healthy. The health check executes `SELECT 1`, not just a port probe.
4. Configure `ConnectionStrings__DefaultConnection` securely once persistence is added in Phase 2. Do not commit production connection strings or passwords.

The container listens only on loopback. `sqlcmd -C` trusts the local development server's self-signed certificate; use validated server certificates in production. Developer edition is for development/testing only. The named volume survives container restarts. `docker compose down` stops services without deleting data; do not remove volumes unless intentionally discarding local data.

The current environment cannot download registry blobs from `westus.data.mcr.microsoft.com` (HTTP proxy 403). That destination was added to the cloud configuration draft; it must be saved/applied before retrying. See [validation results](docs/phase-1-results.md).

## Secrets and later configuration

No passwords, JWT keys or production database strings are committed. Authentication is not enabled yet. Future phases will read `Auth__Username`, `Auth__Password`, `Jwt__SigningKey`, issuer/audience and `ConnectionStrings__DefaultConnection` from secure configuration. `appsettings.Local.json` and `.env*` are ignored; .NET does not automatically read `.env` or appsettings.Local.json (Compose reads `.env`; explicit application loading would be implemented later if needed).

## Delivery status

Work is on `setup/phase-1`. Remote main is absent in this previously empty repository. No push, merge or PR was performed. A PR requires a published base branch. Phase 2 starts only after user approval. Postman scenarios, ERD, EF migrations, OpenAPI and Serilog/sample logs are intentionally pending their planned phases.
