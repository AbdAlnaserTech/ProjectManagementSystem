# Phase 1 final review — 2026-10-08

## Repository and GitHub
- Branch: `setup/phase-1`. Required PLAN.md, README.md and this report exist and are committed; ignored build/test outputs and local secrets are excluded.
- Original Phase 1 commits `1842d86` and `071e7c9` were preserved without rewriting.
- Read-only `git ls-remote --symref origin` succeeded and returned no refs both before and after push attempts. The remote repository remains empty; no remote HEAD/default branch can be verified. Direct GitHub API access is blocked by the proxy, so no API-derived default-branch claim is made.
- Created local `main` at `32d2a54` as an empty baseline. Merged it into Phase 1 with `--allow-unrelated-histories` (`ca0023f`), retaining existing commits and making main an ancestor so the full scaffold is reviewable. No force push, reset, deletion or repository recreation occurred.
- Attempted normal pushes of `main` and `setup/phase-1`. Both failed with HTTP 403: `Permission to AbdAlnaserTech/ProjectManagementSystem.git denied to AbdAlnaserTech.` Native Git authentication supports reads but currently denies writes.
- GitHub CLI also reported its injected token invalid; no token was printed or extracted. No PR was created because neither branch exists remotely. The final documentation commit has not been retried against the unchanged denied permission.
- Remaining delivery action: authorize repository write access for the cloud GitHub connection, then push main and setup/phase-1 and verify remote SHA values. Confirm remote default branch is main through GitHub settings; create a PR targeting main. Do not merge automatically. Local-only commit restoration in a fresh cloud task remains unverified.

## Verification rerun
- .NET SDK 10.0.401, runtime 10.0.12; original SDK archive SHA-512 matched Microsoft release metadata.
- `bash scripts/verify.sh`: `dotnet restore --locked-mode`, `dotnet tool restore`, `dotnet build --no-restore`, `dotnet test --no-build --no-restore` all succeeded.
- Build: **0 warnings, 0 errors**. NuGet audit and warning-as-error policy remain enabled. AutoMapper is pinned to 16.2.0 after the initial 14.0.0 advisory diagnosis.
- Tests: **2 passed, 0 failed, 0 skipped**, duration 196 ms. Current-run TRX: tests/ProjectManagement.Tests/TestResults/phase1.trx (ignored).
- Live API request: GET /health/live returned HTTP 200 and `Healthy`. The API liveness endpoint does not probe SQL Server.

## SQL Server: blocker resolved
- Initial pull failed because config/layer blobs redirected from mcr.microsoft.com to westus.data.mcr.microsoft.com and the network proxy denied that destination.
- Required setting: environment settings → Internet/network access → restricted access → custom allowed domains: add `westus.data.mcr.microsoft.com` (hostname only), preserving existing custom domains and the package-manager preset. Save/apply the setting; draft saving alone is insufficient. This domain was already present in the saved draft.
- During this review, an actual redirected registry config-blob GET succeeded with HTTP 200, proving runtime access through the authorized proxy route. Only then was Docker pull retried. No proxy bypass or verification disabling was used.
- Pull succeeded with layer checksum verification. Image digest: `sha256:4402d880dd4c34bfa7d8705e56a86cd6c88da80a1f6bbbe741f999e76264a090`.
- Created a random local-only SQL password in ignored, mode-0600 `.env`; no existing configuration was overwritten and no password was displayed or committed.
- `docker compose up -d --wait --wait-timeout 180 sqlserver` succeeded. Container: projectmanagementsystem-sqlserver-1, healthy, bound to 127.0.0.1:1433. Named database volume retained.
- Explicit authenticated sqlcmd query returned `Ready = 1`, SQL Server version **16.0.4295.3**. SQL startup and SELECT 1 are verified. The local self-signed server certificate is trusted with sqlcmd -C as documented for development; download TLS and artifact verification were preserved.

## Scope and pending decisions
Phase 1 development infrastructure is verified in the current instance. Phase 2 has not started. SQL migrations and transaction/domain tests were not run because domain persistence is not yet implemented. Entities, JWT, validation/mapping, Serilog, versioned OpenAPI, ERD and Postman scenarios remain assigned to later phases. No PDF was supplied; PLAN.md contains proposed business rules for review. AutoMapper licensing/academic eligibility must be reviewed before mapping implementation. GitHub delivery and fresh-task restoration remain unverified; no full synchronization or publication claim is made.
