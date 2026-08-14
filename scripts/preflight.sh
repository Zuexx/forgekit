#!/usr/bin/env bash
# Reports whether this repository's documented AI workflow is operational on this machine.
# Every check runs before exiting, so one failure does not hide the rest — hence no `set -e`.
set -uo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BIN_DIR="$ROOT_DIR/node_modules/.bin"
FAILED=0

pass() { printf '  ok    %s\n' "$1"; }
fail() { printf '  FAIL  %s\n        fix: %s\n' "$1" "$2"; FAILED=1; }

echo "==> Declared tools"
for tool in openspec codegraph grillme; do
  if [ -x "$BIN_DIR/$tool" ]; then
    pass "$tool"
  else
    fail "$tool is not installed" "pnpm install"
  fi
done

echo "==> Workflow configuration"
if [ ! -x "$BIN_DIR/openspec" ]; then
  fail "cannot read workflow configuration" "pnpm install"
else
  probe="preflight-probe-$$"
  probe_dir="$ROOT_DIR/openspec/changes/$probe"
  trap 'rm -rf "$probe_dir"' EXIT
  if "$BIN_DIR/openspec" new change "$probe" >/dev/null 2>&1; then
    rules=$("$BIN_DIR/openspec" instructions proposal --change "$probe" --json 2>/dev/null \
      | node -pe 'JSON.parse(require("fs").readFileSync(0,"utf8")).rules?.length ?? 0' 2>/dev/null || echo 0)
    guidance=$("$BIN_DIR/openspec" instructions apply --change "$probe" --json 2>/dev/null \
      | node -pe 'JSON.parse(require("fs").readFileSync(0,"utf8")).operationGuidance?.length ?? 0' 2>/dev/null || echo 0)
    rm -rf "$probe_dir"
    if [ "$rules" -gt 0 ]; then
      pass "proposal rules readable ($rules)"
    else
      fail "openspec/config.yaml yields no proposal rules" \
           "check rules: in openspec/config.yaml — a plain scalar containing ': ' silently empties the block"
    fi
    if [ "$guidance" -gt 0 ]; then
      pass "apply guidance readable ($guidance)"
    else
      fail "openspec/config.yaml yields no apply guidance" \
           "check operations.apply.guidance in openspec/config.yaml"
    fi
  else
    fail "openspec could not create a probe change" "run: $BIN_DIR/openspec new change probe"
  fi
fi

echo "==> CodeGraph index"
INDEX="$ROOT_DIR/.codegraph/codegraph.db"
if [ ! -f "$INDEX" ]; then
  fail "no CodeGraph index" "$BIN_DIR/codegraph index"
else
  index_epoch=$(stat -f %m "$INDEX" 2>/dev/null || stat -c %Y "$INDEX")
  head_epoch=$(git -C "$ROOT_DIR" log -1 --format=%ct)
  if [ "$index_epoch" -lt "$head_epoch" ]; then
    fail "index is older than HEAD — impact analysis from it would be out of date" \
         "$BIN_DIR/codegraph index"
  else
    pass "index present and not older than HEAD"
  fi
fi

echo "==> Git hooks"
hooks_path=$(git -C "$ROOT_DIR" config --get core.hooksPath || true)
if [ "$hooks_path" = ".githooks" ]; then
  pass "core.hooksPath -> .githooks"
else
  fail "repository hooks are not enabled" "git config core.hooksPath .githooks"
fi

echo
if [ "$FAILED" -eq 0 ]; then
  echo "Workflow is operational."
else
  echo "Workflow is not operational. Fix the items above and re-run." >&2
fi
exit "$FAILED"
