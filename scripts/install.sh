#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
source scripts/env.sh
sdk_version=10.0.401
if ! command -v dotnet >/dev/null || [[ "$(dotnet --version)" != "$sdk_version" ]]; then
  sdk_archive=$(mktemp /tmp/pms-sdk.XXXXXX.tar.gz)
  trap 'rm -f "$sdk_archive"' EXIT
  curl -fsSL "https://builds.dotnet.microsoft.com/dotnet/Sdk/$sdk_version/dotnet-sdk-$sdk_version-linux-x64.tar.gz" -o "$sdk_archive"
  printf '%s  %s\n' '51c8b999af9e8dd9998c9edc5944e19a90788862068acd38694e098889054ce8c23d4f0c5cccfa16bf187d044562359e5ee69a9f8ad0bbe913ba90311fbce25b' "$sdk_archive" | sha512sum -c -
  mkdir -p "$DOTNET_ROOT"
  tar -xzf "$sdk_archive" -C "$DOTNET_ROOT"
fi
# Authoritative hash: Microsoft 10.0 release metadata for SDK 10.0.401/linux-x64.
dotnet restore --locked-mode
dotnet tool restore
dotnet build --no-restore
