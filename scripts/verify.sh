#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
source scripts/env.sh
dotnet restore --locked-mode
dotnet tool restore
dotnet build --no-restore
dotnet test --no-build --no-restore --logger 'trx;LogFileName=phase1.trx'
