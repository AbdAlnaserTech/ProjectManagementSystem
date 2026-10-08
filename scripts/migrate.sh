#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
source scripts/env.sh
python scripts/with-database.py dotnet ef database update --project src/ProjectManagement.Api
