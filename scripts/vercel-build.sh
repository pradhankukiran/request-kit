#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

SDK_VERSION="$(sed -n 's/.*"version"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' global.json | head -n 1)"

if [[ -z "$SDK_VERSION" ]]; then
  echo "Unable to determine .NET SDK version from global.json." >&2
  exit 1
fi

DOTNET_CMD="$(command -v dotnet || true)"

if [[ -z "$DOTNET_CMD" ]]; then
  export DOTNET_ROOT="$ROOT_DIR/.vercel/dotnet"
  export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"

  mkdir -p "$DOTNET_ROOT"
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o "$ROOT_DIR/.vercel/dotnet-install.sh"
  bash "$ROOT_DIR/.vercel/dotnet-install.sh" --version "$SDK_VERSION" --install-dir "$DOTNET_ROOT" --no-path
  rm -f "$ROOT_DIR/.vercel/dotnet-install.sh"

  DOTNET_CMD="$DOTNET_ROOT/dotnet"
fi

"$DOTNET_CMD" publish src/RequestKit.Wasm/RequestKit.Wasm.csproj -c Release -o output --nologo
