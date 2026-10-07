#!/usr/bin/env bash
#
# One-command local setup for the Daily Devotional app (macOS).
# Windows developers use scripts/setup-local.ps1 instead.
#
#   1. Installs missing prerequisites with Homebrew (Node.js, .NET 8 SDK, PostgreSQL 17)
#   2. Starts the PostgreSQL service and makes sure a `postgres` role exists
#   3. Creates the local DailyDevotional database if it does not exist
#   4. Stores backend secrets in .NET user-secrets (outside the repo)
#   5. Restores backend NuGet packages
#   6. Installs frontend dependencies with `npm ci` (does not modify package-lock.json)
#   7. Optionally starts the API and the frontend (--run)
#
# Safe to re-run: anything already done is skipped, and existing secrets are kept unless you pass --force.
#
# Usage:  bash scripts/setup-local.sh [options]
#   --run                  start the API and the frontend (new Terminal windows) and open the browser
#   --yes                  install missing prerequisites without asking
#   --skip-install         never install anything; only report what is missing
#   --force                overwrite secrets that are already set
#   --pg-host <host>       default: localhost
#   --pg-port <port>       default: 5432
#   --pg-user <user>       default: postgres
#   --pg-password <pw>     default: password
#   --db-name <name>       default: DailyDevotional

set -euo pipefail

PG_HOST=localhost
PG_PORT=5432
PG_USER=postgres
PG_PASSWORD=password
DB_NAME=DailyDevotional
ASSUME_YES=0
SKIP_INSTALL=0
RUN_APP=0
FORCE=0

while [[ $# -gt 0 ]]; do
  case "$1" in
    --run) RUN_APP=1 ;;
    --yes) ASSUME_YES=1 ;;
    --skip-install) SKIP_INSTALL=1 ;;
    --force) FORCE=1 ;;
    --pg-host) PG_HOST="$2"; shift ;;
    --pg-port) PG_PORT="$2"; shift ;;
    --pg-user) PG_USER="$2"; shift ;;
    --pg-password) PG_PASSWORD="$2"; shift ;;
    --db-name) DB_NAME="$2"; shift ;;
    -h|--help) sed -n '2,26p' "$0"; exit 0 ;;
    *) echo "Unknown option: $1 (see --help)"; exit 1 ;;
  esac
  shift
done

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
API_DIR="$REPO_ROOT/backend/DailyDevotional.Api/DailyDevotional.Api"

step() { printf '\n\033[36m== %s\033[0m\n' "$1"; }
ok()   { printf '\033[32m   OK  %s\033[0m\n' "$1"; }
warn() { printf '\033[33m   !!  %s\033[0m\n' "$1"; }
fail() { printf '\033[31m   XX  %s\033[0m\n' "$1"; }

if [[ "$(uname -s)" != "Darwin" ]]; then
  fail "This script is for macOS. On Windows use scripts/setup-local.ps1."
  exit 1
fi

# ---------------------------------------------------------------- helpers
# Homebrew installs some formulae "keg-only" (not linked into PATH). Add their bin dirs for this session.
add_brew_paths() {
  command -v brew >/dev/null 2>&1 || return 0
  local formula prefix
  for formula in postgresql@17 dotnet@8; do
    prefix="$(brew --prefix "$formula" 2>/dev/null || true)"
    if [[ -n "$prefix" && -d "$prefix/bin" ]]; then
      case ":$PATH:" in *":$prefix/bin:"*) ;; *) export PATH="$prefix/bin:$PATH" ;; esac
    fi
  done
  # Homebrew's dotnet@8 needs DOTNET_ROOT to find its runtime.
  local dotnet_prefix
  dotnet_prefix="$(brew --prefix dotnet@8 2>/dev/null || true)"
  if [[ -n "$dotnet_prefix" && -d "$dotnet_prefix/libexec" ]]; then
    export DOTNET_ROOT="$dotnet_prefix/libexec"
  fi
}

node_ok() {
  command -v node >/dev/null 2>&1 || return 1
  local v major minor
  v="$(node -v | sed 's/^v//')"
  major="${v%%.*}"; minor="${v#*.}"; minor="${minor%%.*}"
  [[ "$major" -ge 24 ]] || { [[ "$major" -eq 22 && "$minor" -ge 12 ]]; } || { [[ "$major" -eq 20 && "$minor" -ge 19 ]]; }
}

dotnet_ok() {
  command -v dotnet >/dev/null 2>&1 || return 1
  dotnet --list-sdks 2>/dev/null | grep -q '^8\.'
}

postgres_ok() {
  command -v psql >/dev/null 2>&1 && command -v pg_isready >/dev/null 2>&1
}

# ---------------------------------------------------------------- 1. prerequisites
step "1/6  Prerequisites (Node.js, .NET 8 SDK, PostgreSQL)"

if ! command -v brew >/dev/null 2>&1; then
  fail "Homebrew is not installed. Install it from https://brew.sh, open a NEW terminal, and re-run."
  exit 1
fi
add_brew_paths

missing_names=()
missing_cmds=()
if node_ok; then ok "Node $(node -v)"; else missing_names+=("Node.js"); missing_cmds+=("brew install node"); fi
if dotnet_ok; then ok ".NET 8 SDK"; else missing_names+=(".NET 8 SDK"); missing_cmds+=("brew install dotnet@8"); fi
if postgres_ok; then ok "PostgreSQL"; else missing_names+=("PostgreSQL 17"); missing_cmds+=("brew install postgresql@17"); fi

if [[ ${#missing_names[@]} -gt 0 ]]; then
  warn "Missing: ${missing_names[*]}"
  if [[ $SKIP_INSTALL -eq 1 ]]; then
    fail "Install them (see LOCAL_SETUP.md) and re-run. (--skip-install was given.)"
    exit 1
  fi
  if [[ $ASSUME_YES -eq 0 ]]; then
    printf '   These will be installed with Homebrew:\n'
    for c in "${missing_cmds[@]}"; do printf '     %s\n' "$c"; done
    read -r -p "   Install now? [Y/n] " answer
    if [[ "$answer" =~ ^[Nn] ]]; then fail "Cancelled. Install them yourself and re-run."; exit 1; fi
  fi
  for c in "${missing_cmds[@]}"; do
    echo "   Running: $c"
    $c
  done
  hash -r
  add_brew_paths
  still=()
  node_ok || still+=("Node.js 20.19+/22.12+/24+")
  dotnet_ok || still+=(".NET 8 SDK")
  postgres_ok || still+=("PostgreSQL")
  if [[ ${#still[@]} -gt 0 ]]; then
    fail "Still missing: ${still[*]}. Open a NEW terminal and re-run, or install by hand (see LOCAL_SETUP.md)."
    exit 1
  fi
  ok "prerequisites installed"
fi

# ---------------------------------------------------------------- 2. PostgreSQL service + role
step "2/6  PostgreSQL service"
if brew list postgresql@17 >/dev/null 2>&1; then
  if ! brew services list | grep -E '^postgresql@17\s+started' >/dev/null; then
    brew services start postgresql@17 >/dev/null
    ok "started postgresql@17 (brew services)"
  else
    ok "postgresql@17 is running"
  fi
else
  warn "postgresql@17 is not a Homebrew install; assuming PostgreSQL is reachable at $PG_HOST:$PG_PORT."
fi

for _ in $(seq 1 30); do
  pg_isready -h "$PG_HOST" -p "$PG_PORT" >/dev/null 2>&1 && break
  sleep 1
done
if ! pg_isready -h "$PG_HOST" -p "$PG_PORT" >/dev/null 2>&1; then
  fail "PostgreSQL is not accepting connections at $PG_HOST:$PG_PORT."
  exit 1
fi

# Homebrew's Postgres has no `postgres` role: its superuser is your macOS username, with no password.
# The app's default connection string uses postgres/password, so create that role if it's missing.
if [[ "$PG_HOST" == "localhost" && "$PG_USER" == "postgres" ]]; then
  role_exists="$(psql -h "$PG_HOST" -p "$PG_PORT" -d postgres -tAc "SELECT 1 FROM pg_roles WHERE rolname='postgres'" 2>/dev/null || true)"
  if [[ "$role_exists" != "1" ]]; then
    psql -h "$PG_HOST" -p "$PG_PORT" -d postgres -v ON_ERROR_STOP=1 \
      -c "CREATE ROLE postgres WITH SUPERUSER LOGIN PASSWORD '$PG_PASSWORD'" >/dev/null
    ok "created role 'postgres' (password: $PG_PASSWORD)"
  else
    ok "role 'postgres' exists"
  fi
fi

# ---------------------------------------------------------------- 3. database
step "3/6  Database '$DB_NAME' on $PG_HOST:$PG_PORT"
export PGPASSWORD="$PG_PASSWORD"
if ! exists="$(psql -U "$PG_USER" -h "$PG_HOST" -p "$PG_PORT" -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='$DB_NAME'" 2>&1)"; then
  fail "Could not connect to PostgreSQL: $exists"
  echo "       Is the password right? Re-run with --pg-password <yours>."
  exit 1
fi
if [[ "$exists" == "1" ]]; then
  ok "already exists"
else
  createdb -U "$PG_USER" -h "$PG_HOST" -p "$PG_PORT" "$DB_NAME"
  ok "created (tables are created and seeded automatically when the API first starts)"
fi
unset PGPASSWORD

# ---------------------------------------------------------------- 4. secrets
step "4/6  Backend secrets (.NET user-secrets, stored outside the repo)"
cd "$API_DIR"

existing_secrets="$(dotnet user-secrets list 2>/dev/null || true)"
has_secret() { grep -q "^$1 = " <<<"$existing_secrets"; }
set_secret() { dotnet user-secrets set "$1" "$2" >/dev/null; }

ensure_secret() { # name value label
  if [[ $FORCE -eq 1 ]] || ! has_secret "$1"; then
    set_secret "$1" "$2"
    ok "$3"
  else
    ok "$1 already set"
  fi
}

# Required JWT settings: generated locally, never shared between devs.
ensure_secret "Authentication:Jwt:Key" "$(openssl rand -base64 48 | tr -d '\n')" "Authentication:Jwt:Key generated"
ensure_secret "Authentication:Jwt:Issuer" "DailyDevotional.Api" "Authentication:Jwt:Issuer set"
ensure_secret "Authentication:Jwt:Audience" "DailyDevotional.App" "Authentication:Jwt:Audience set"

# Only needed if you use a non-default PostgreSQL password / host / port.
default_conn="Host=localhost;Port=5432;Database=$DB_NAME;Username=postgres;Password=password"
conn="Host=$PG_HOST;Port=$PG_PORT;Database=$DB_NAME;Username=$PG_USER;Password=$PG_PASSWORD"
if [[ "$conn" != "$default_conn" ]]; then
  set_secret "ConnectionStrings:DefaultConnection" "$conn"
  ok "ConnectionStrings:DefaultConnection overridden"
fi

# Optional values: press Enter to skip. Get the shared dev values from the project lead (see LOCAL_SETUP.md).
printf '\n   Optional values. Press Enter to skip any you don'\''t have yet (re-run the script later to add them).\n'
prompt_optional() { # name prompt secret(0/1)
  local name="$1" prompt="$2" is_secret="$3" value=""
  if [[ $FORCE -eq 0 ]] && has_secret "$name"; then ok "$name already set"; return; fi
  if [[ "$is_secret" == "1" ]]; then
    read -r -s -p "   $prompt: " value; echo
  else
    read -r -p "   $prompt: " value
  fi
  value="$(printf '%s' "$value" | sed 's/^[[:space:]]*//;s/[[:space:]]*$//')"
  if [[ -z "$value" ]]; then
    warn "$name skipped"
  else
    set_secret "$name" "$value"
    ok "$name set"
  fi
}
prompt_optional "Authentication:Google:ClientId"     "Google OAuth client ID (needed to sign in)" 0
prompt_optional "Authentication:Google:ClientSecret" "Google OAuth client secret (needed to sign in)" 1
prompt_optional "ESV:ApiKey"                         "ESV API key (passage text; get it from the project lead)" 1
prompt_optional "Anthropic:ApiKey"                   "Anthropic API key (AI commentary, optional)" 1
prompt_optional "Authentication:AdminEmail"          "Your Google sign-in email (makes you admin to import the schedule)" 0

# ---------------------------------------------------------------- 5. backend packages
step "5/6  Backend packages (dotnet restore)"
dotnet restore >/dev/null
ok "restored"

# ---------------------------------------------------------------- 6. frontend packages
step "6/6  Frontend packages (npm ci)"
cd "$REPO_ROOT"
npm ci
ok "installed"

# ---------------------------------------------------------------- done
printf '\n\033[32mSetup complete.\033[0m\n'

if [[ $RUN_APP -eq 1 ]]; then
  step "Starting the app"
  busy=()
  for port in 5184 4200; do
    if lsof -nP -iTCP:"$port" -sTCP:LISTEN >/dev/null 2>&1; then busy+=("$port"); fi
  done
  if [[ ${#busy[@]} -gt 0 ]]; then
    fail "Port(s) ${busy[*]} already in use: another copy of the API (5184) or the frontend (4200) is still running."
    echo "       Close that terminal (or stop the process) and re-run with --run. Setup itself completed."
    exit 1
  fi
  # Pass the PATH adjustments (keg-only brew tools) to the new Terminal windows.
  api_cmd="export PATH='$PATH' DOTNET_ROOT='${DOTNET_ROOT:-}' ASPNETCORE_ENVIRONMENT=Development; cd '$API_DIR' && dotnet run --no-launch-profile"
  web_cmd="export PATH='$PATH'; cd '$REPO_ROOT' && npm start"
  osascript -e "tell application \"Terminal\" to do script \"${api_cmd//\"/\\\"}\"" >/dev/null
  osascript -e "tell application \"Terminal\" to do script \"${web_cmd//\"/\\\"}\"" >/dev/null
  ok "API window and frontend window opened (first start takes ~30s: it builds, then migrates the database)"
  echo "   Close those two Terminal windows to stop the app."
  sleep 20
  open "http://localhost:4200"
else
  cat <<EOF

Run the backend (terminal 1):
  cd backend/DailyDevotional.Api/DailyDevotional.Api
  dotnet run --launch-profile http

Run the frontend (terminal 2, repo root):
  npm start

Then open http://localhost:4200
(Or re-run this script with --run to start both for you.)
EOF
fi
