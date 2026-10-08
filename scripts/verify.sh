#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
source scripts/env.sh
dotnet restore --locked-mode
dotnet tool restore
dotnet build --no-restore
python scripts/with-database.py dotnet test --no-build --no-restore --logger 'trx;LogFileName=verification.trx'
