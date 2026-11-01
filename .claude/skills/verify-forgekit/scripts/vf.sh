#!/usr/bin/env bash
# Verification harness for ForgeKit: launches an isolated API + app pair on a disposable
# SQLite database, checks it, and tears down only what it started.
#
#   bash .claude/skills/verify-forgekit/scripts/vf.sh launch [--no-build]
#   bash .claude/skills/verify-forgekit/scripts/vf.sh doctor
#   bash .claude/skills/verify-forgekit/scripts/vf.sh env        # prints exports for the journeys
#   bash .claude/skills/verify-forgekit/scripts/vf.sh sql "<query>"
#   bash .claude/skills/verify-forgekit/scripts/vf.sh cleanup
#
# State lives in .verify/current.env; each run gets .verify/<run-id>/{db,logs,evidence}.
# Cleanup deletes db/ and logs/ and keeps evidence/.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../../.." && pwd)"
VERIFY_DIR="$ROOT/.verify"
STATE="$VERIFY_DIR/current.env"
API_PORT="${VF_API_PORT:-5050}"
APP_PORT="${VF_APP_PORT:-3100}"

die() { echo "vf: $*" >&2; exit 1; }

port_in_use() {
  # Any HTTP answer, or a refused-but-bound socket, counts as taken.
  curl -s -o /dev/null --max-time 2 "http://localhost:$1/" && return 0
  return 1
}

wait_for() { # url, seconds, label, pid, log
  local url=$1 secs=$2 label=$3 pid=$4 log=$5 i
  for ((i = 0; i < secs; i++)); do
    if curl -s -o /dev/null -f --max-time 2 "$url"; then return 0; fi
    if ! kill -0 "$pid" 2>/dev/null; then
      tail -n 20 "$log" >&2
      # Windows reserves rotating port blocks (Hyper-V/WSL); binding one fails with 10013.
      grep -q "10013\|EACCES\|address already in use\|EADDRINUSE" "$log" &&
        echo "vf: $label could not bind its port. On Windows check 'netsh int ipv4 show excludedportrange protocol=tcp' and set VF_API_PORT / VF_APP_PORT outside those ranges." >&2
      die "$label exited during startup (log: $log); run 'vf.sh cleanup'"
    fi
    sleep 1
  done
  die "$label did not become ready at $url within ${secs}s (see $RUN_DIR/logs)"
}

# Record the process to kill later. On Windows (Git Bash) the background PID is an MSYS
# PID; the Windows PID is what taskkill needs to take the whole tree (pnpm -> node).
record_pid() { # name pid
  local winpid=""
  [ -r "/proc/$2/winpid" ] && winpid=$(cat "/proc/$2/winpid")
  echo "${1}_PID=$2" >>"$STATE"
  echo "${1}_WINPID=$winpid" >>"$STATE"
}

# The listener on a port this run claimed. launch refuses taken ports, so it is ours.
port_owner_winpid() { # port
  command -v netstat >/dev/null 2>&1 || return 0
  netstat -ano 2>/dev/null | awk -v p=":$1" '$2 ~ p"$" && $4 == "LISTENING" {print $5; exit}'
}

kill_tree() { # pid winpid
  local pid=$1 winpid=$2
  if [ -n "$winpid" ]; then
    taskkill //PID "$winpid" //T //F >/dev/null 2>&1 || true
  elif [ -n "$pid" ]; then
    # Started with setsid, so the PID is also the process-group id.
    kill -- "-$pid" 2>/dev/null || kill "$pid" 2>/dev/null || true
  fi
}

start_bg() { # logfile, command...
  local log=$1; shift
  if command -v setsid >/dev/null 2>&1; then
    setsid "$@" >"$log" 2>&1 &
  else
    "$@" >"$log" 2>&1 &
  fi
  BG_PID=$!
}

cmd_launch() {
  local build=1
  [ "${1:-}" = "--no-build" ] && build=0
  [ -f "$STATE" ] && die "a run is already recorded in $STATE; run 'vf.sh cleanup' first"
  port_in_use "$API_PORT" && die "port $API_PORT is taken; set VF_API_PORT"
  port_in_use "$APP_PORT" && die "port $APP_PORT is taken; set VF_APP_PORT"

  RUN_ID="$(date +%Y%m%d-%H%M%S)"
  RUN_DIR="$VERIFY_DIR/$RUN_ID"
  mkdir -p "$RUN_DIR/db" "$RUN_DIR/logs" "$RUN_DIR/evidence"
  local db="$RUN_DIR/db/forgekit.db"
  # Windows paths for the .NET and Node processes, which do not understand /d/...
  local db_native="$db"
  command -v cygpath >/dev/null 2>&1 && db_native="$(cygpath -w "$db")"

  {
    echo "RUN_ID=$RUN_ID"
    echo "RUN_DIR=$RUN_DIR"
    echo "DB=$db"
    echo "API_URL=http://localhost:$API_PORT"
    echo "APP_URL=http://localhost:$APP_PORT"
  } >"$STATE"

  echo "==> schema: API (EF, AppDbContext)"
  (cd "$ROOT/api" && dotnet tool restore >/dev/null &&
    dotnet ef database update --project ForgeKit.Api.Migrations.Sqlite \
      --startup-project ForgeKit.Api.Migrations.Sqlite --context AppDbContext \
      --connection "Data Source=$db_native") >"$RUN_DIR/logs/migrate-api.log" 2>&1 ||
    die "API migration failed; see $RUN_DIR/logs/migrate-api.log"

  echo "==> schema: auth (Better Auth CLI)"
  (cd "$ROOT/app" && SQLITE_DATABASE_PATH="$db_native" Database__Provider=Sqlite \
    pnpm auth.migration --yes) >"$RUN_DIR/logs/migrate-auth.log" 2>&1 ||
    die "auth migration failed; see $RUN_DIR/logs/migrate-auth.log"

  local secret
  secret="$(openssl rand -base64 32)"
  if [ "$build" = 1 ]; then
    echo "==> app: production build (pass --no-build to reuse app/.next)"
    (cd "$ROOT/app" && BETTER_AUTH_SECRET="$secret" BETTER_AUTH_URL="http://localhost:$APP_PORT" \
      pnpm build) >"$RUN_DIR/logs/build.log" 2>&1 || die "app build failed; see $RUN_DIR/logs/build.log"
  fi
  [ -f "$ROOT/app/.next/BUILD_ID" ] || die "no app build in app/.next; launch without --no-build"

  echo "==> app: start on :$APP_PORT"
  cd "$ROOT/app"
  start_bg "$RUN_DIR/logs/app.log" env SQLITE_DATABASE_PATH="$db_native" Database__Provider=Sqlite \
    BETTER_AUTH_SECRET="$secret" BETTER_AUTH_URL="http://localhost:$APP_PORT" \
    node node_modules/next/dist/bin/next start -p "$APP_PORT"
  record_pid APP "$BG_PID"
  APP_BG=$BG_PID

  echo "==> api: start on :$API_PORT"
  cd "$ROOT/api"
  start_bg "$RUN_DIR/logs/api.log" env ASPNETCORE_ENVIRONMENT=Development \
    ASPNETCORE_URLS="http://localhost:$API_PORT" Database__Provider=Sqlite \
    ConnectionStrings__Sqlite="Data Source=$db_native" \
    dotnet run --project ForgeKit.Api --no-launch-profile
  record_pid API "$BG_PID"
  API_BG=$BG_PID
  cd "$ROOT"

  wait_for "http://localhost:$API_PORT/health/live" 180 "API" "$API_BG" "$RUN_DIR/logs/api.log"
  wait_for "http://localhost:$APP_PORT/sign-in" 120 "app" "$APP_BG" "$RUN_DIR/logs/app.log"
  echo "ready: run $RUN_ID, evidence in $RUN_DIR/evidence"
  cmd_doctor
}

load_state() {
  [ -f "$STATE" ] || die "no recorded run ($STATE missing); run 'vf.sh launch'"
  # shellcheck disable=SC1090
  . "$STATE"
}

cmd_doctor() {
  load_state
  local ok=1 code
  echo "run:      $RUN_ID ($RUN_DIR)"
  code=$(curl -s -o "$RUN_DIR/logs/doctor-health.json" -w '%{http_code}' --max-time 5 "$API_URL/health" || true)
  echo "api:      $API_URL/health -> $code $(cat "$RUN_DIR/logs/doctor-health.json" 2>/dev/null | head -c 120)"
  [ "$code" = 200 ] || ok=0
  code=$(curl -s -o /dev/null -w '%{http_code}' --max-time 10 "$APP_URL/sign-in" || true)
  echo "app:      $APP_URL/sign-in -> $code"
  [ "$code" = 200 ] || ok=0
  # The app must be ours: its auth must answer from this run's database.
  code=$(curl -s -o /dev/null -w '%{http_code}' --max-time 10 "$APP_URL/api/auth/get-session" || true)
  echo "auth:     $APP_URL/api/auth/get-session -> $code"
  [ "$code" = 200 ] || ok=0
  [ -f "$DB" ] && echo "database: $DB" || { echo "database: missing ($DB)"; ok=0; }
  [ "$ok" = 1 ] && echo "doctor: OK" || die "doctor: NOT OK — do not drive this instance"
}

cmd_env() {
  load_state
  echo "export VF_RUN_DIR='$RUN_DIR' VF_EVIDENCE='$RUN_DIR/evidence' VF_DB='$DB' VF_API_URL='$API_URL' VF_APP_URL='$APP_URL'"
}

cmd_sql() {
  load_state
  [ -n "${1:-}" ] || die "usage: vf.sh sql \"<query>\""
  local db_native="$DB"
  command -v cygpath >/dev/null 2>&1 && db_native="$(cygpath -w "$DB")"
  # better-sqlite3 is already an app dependency; no sqlite3 binary required. Read-only.
  (cd "$ROOT/app" && VF_DB="$db_native" VF_SQL="$1" node -e '
    const Database = require("better-sqlite3")
    const db = new Database(process.env.VF_DB, { readonly: true })
    console.log(JSON.stringify(db.prepare(process.env.VF_SQL).all(), null, 2))
  ')
}

cmd_cleanup() {
  load_state
  kill_tree "${APP_PID:-}" "${APP_WINPID:-}"
  kill_tree "${API_PID:-}" "${API_WINPID:-}"
  local port owner
  for port in "${APP_URL##*:}" "${API_URL##*:}"; do
    owner=$(port_owner_winpid "$port")
    [ -n "$owner" ] && kill_tree "" "$owner"
  done
  sleep 2
  if ! rm -rf "$RUN_DIR/db" "$RUN_DIR/logs"; then
    echo "vf: could not delete $RUN_DIR/db (still locked); state kept, rerun cleanup" >&2
    exit 1
  fi
  rm -f "$STATE"
  echo "stopped run $RUN_ID; evidence kept in $RUN_DIR/evidence"
  ls "$RUN_DIR/evidence"
}

case "${1:-}" in
  launch) shift; cmd_launch "$@" ;;
  doctor) cmd_doctor ;;
  env) cmd_env ;;
  sql) shift; cmd_sql "$@" ;;
  cleanup) cmd_cleanup ;;
  *) sed -n '2,13p' "${BASH_SOURCE[0]}"; exit 2 ;;
esac
