#!/usr/bin/env bash
# Source this file; keep CLI state and package cache in writable cloud paths.
export DOTNET_ROOT="${DOTNET_ROOT:-/workspace/.dotnet}"
export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-/workspace/.dotnet-home}"
export NUGET_PACKAGES="${NUGET_PACKAGES:-/workspace/.nuget/packages}"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export PATH="$DOTNET_ROOT:$PATH"
