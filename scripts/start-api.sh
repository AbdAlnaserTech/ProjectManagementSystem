#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
source scripts/env.sh
export ASPNETCORE_ENVIRONMENT=Development
exec python scripts/with-database.py dotnet run --no-launch-profile --project src/ProjectManagement.Api --urls "${PMS_API_URLS:-http://127.0.0.1:5080}"
