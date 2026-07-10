#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

require_command() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Missing required command: $1" >&2
    exit 127
  fi
}

require_command dotnet
require_command pnpm
require_command openspec
require_command gitleaks

echo "==> API"
(
  cd "$ROOT_DIR/api"
  dotnet restore ForgeKit.sln
  dotnet build ForgeKit.sln --configuration Release --no-restore --disable-build-servers
  dotnet test ForgeKit.sln --configuration Release --no-build --disable-build-servers
  dotnet tool restore
)

echo "==> App"
(
  cd "$ROOT_DIR/app"
  pnpm install --frozen-lockfile
  pnpm check
  pnpm lint
  BETTER_AUTH_SECRET="$(openssl rand -base64 32)" \
    BETTER_AUTH_URL="http://localhost:3000" \
    pnpm build
)

echo "==> OpenSpec"
(
  cd "$ROOT_DIR"
  openspec validate --all --strict --no-interactive
)

echo "==> Secrets"
(
  cd "$ROOT_DIR"
  gitleaks dir --redact .
  gitleaks git --redact --log-opts=--all
)

echo "Verification completed."
