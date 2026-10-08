#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
source scripts/env.sh
export ASPNETCORE_ENVIRONMENT=Development
exec dotnet run --no-launch-profile --project src/ProjectManagement.Api --urls http://127.0.0.1:5080
