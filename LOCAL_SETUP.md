# Local Setup (Windows and macOS)

How to run the Daily Devotional app locally.

- **Frontend:** Angular 22, http://localhost:4200
- **Backend:** ASP.NET Core 8 API, http://localhost:5184 (Swagger at `/swagger`)
- **Database:** PostgreSQL. The API applies EF migrations (and seeds sample readings) automatically on startup.

## Quick start (new developer checklist)

You only do **two things by hand**: collect your keys, and run one script.

1. **Get your keys first** (see [Team workflow](#team-workflow-who-needs-what)). The script will ask for them, and you can skip any and add them later by re-running it:
   - **From the project lead** (via the team password manager):
     - the shared **dev** Google OAuth client ID and secret, and being added as a **test user** on that client
     - the shared **dev** ESV API key
   - Optional: your own Anthropic API key (AI commentary).
2. **Run the setup script** from the repo root.

   **Windows** (PowerShell):

   ```powershell
   powershell -ExecutionPolicy Bypass -File .\scripts\setup-local.ps1 -Run
   ```

   **macOS** (Terminal; needs [Homebrew](https://brew.sh)):

   ```bash
   bash scripts/setup-local.sh --run
   ```

   `-Run` / `--run` starts the API and the frontend when setup finishes and opens http://localhost:4200. Leave it off to start them yourself (section 5).

That's it. Everything in the next section is done for you.

### What the script does for you (don't do these by hand)

| # | Step | Windows (`setup-local.ps1`) | macOS (`setup-local.sh`) |
|---|---|---|---|
| 1 | **Install Node.js, .NET 8 SDK, PostgreSQL 17** | Only what is missing or too old, via `winget`. Asks first. Windows may show administrator (UAC) prompts. | Only what is missing or too old, via Homebrew (`node`, `dotnet@8`, `postgresql@17`). Asks first. Homebrew itself must already be installed. |
| 2 | **Make the tools usable straight away** | Refreshes PATH for the current session. | Adds Homebrew's "keg-only" `dotnet@8` and `postgresql@17` to PATH and sets `DOTNET_ROOT` for the session. |
| 3 | **Start PostgreSQL** | Starts the Windows service if stopped. The installer sets the `postgres` password to `password`. | Runs `brew services start postgresql@17` and creates the `postgres` role with password `password` (Homebrew's Postgres doesn't have one). |
| 4 | **Create the `DailyDevotional` database** | Skipped if it exists. Tables and sample data are created by the API on its first start. | Same. |
| 5 | **Generate the JWT key, set issuer and audience** | Stored in .NET user-secrets, outside the repo. | Same. |
| 6 | **Set a custom connection string** | Only if you pass a non-default `-PgPassword`, `-PgHost`, `-PgPort` or `-PgUser`. | Only if you pass `--pg-password`, `--pg-host`, `--pg-port` or `--pg-user`. |
| 7 | **Prompt for Google client ID/secret, ESV key, Anthropic key, admin email** | Press Enter to skip any. Secret input is hidden. The admin email is the Google account you sign in with; it makes you an admin so the schedule Import button appears. | Same. |
| 8 | **Restore backend packages** | `dotnet restore`. | Same. |
| 9 | **Install frontend packages** | `npm ci` (never rewrites `package-lock.json`). Stops this repo's own leftover dev-server processes first so Windows can replace `node_modules`. | `npm ci`. |
| 10 | **Start the API and frontend and open the browser** | Only with `-Run` (two PowerShell windows). | Only with `--run` (two Terminal windows). |

It is safe to re-run at any time. Anything already done is skipped, and existing secrets are kept unless you force it.

| Windows option | macOS option | Effect |
|---|---|---|
| `-Run` | `--run` | Start the API and frontend after setup, then open the browser |
| `-Yes` | `--yes` | Install missing prerequisites without asking |
| `-SkipInstall` | `--skip-install` | Never install anything; only report what is missing |
| `-Force` | `--force` | Overwrite secrets that are already set (for example to enter a new key). This also generates a new JWT key, which signs you out of the local app, and re-prompts for every optional value. |
| `-PgPassword <pw>` | `--pg-password <pw>` | Your PostgreSQL password if it isn't `password` (also host, port, user and database name options) |

### What the scripts cannot do

- Get you the Google client credentials, the ESV key or the Anthropic key. The Google and ESV values come from the project lead; the Anthropic key is optional and comes from your own Anthropic account.
- Install Homebrew on macOS. Install it first from https://brew.sh.
- Run without administrator approval for the Windows installers. If you decline a UAC prompt, install that tool by hand (section 1) and re-run.
- Support Linux. The macOS script exits on anything that isn't macOS.

The sections numbered 1 to 5 below are the manual equivalent of the scripts. **You don't need them unless a script fails** or you can't use it.

## Team workflow: who needs what

Nothing secret is committed to this repo. Every secret lives in each developer's own .NET user-secrets store, so the same setup runs on every machine.

| Value | Per developer or shared? | Where to get it |
|---|---|---|
| JWT key / issuer / audience | **Own.** The script generates it. | Nothing to ask for |
| Local PostgreSQL database | **Own.** Never share a database; a bad migration would break everyone. | The script creates it |
| ESV API key | **Shared dev key**, created by the project lead. | Project lead, via the team password manager |
| Google OAuth client ID and secret | **Shared dev client**, created by the project lead. | Project lead, via the team password manager |
| Admin email | **Own.** The Google account you sign in with. | The script prompts for it |
| Anthropic API key | **Own, or skip.** The feature is optional and costs money per use. | https://console.anthropic.com |

Rules:

- **Never use production secrets locally**, and never point a local run at the production `DATABASE_URL`. Startup runs migrations against whatever database it connects to.
- **The shared Google client and ESV key must be dev-only**, separate from production. The Google client needs `http://localhost:5184/signin-google` as an authorized redirect URI, and its secret only works against localhost, which limits the risk if it leaks. Using a separate ESV key keeps local testing from using up production's rate limit, and it can be rotated without affecting production.
- While the Google consent screen is in "Testing" mode, each developer's Google account must be added as a test user, or sign-in is refused.
- Share secrets through a password manager (1Password, Bitwarden), never through git, chat or email.
- If a shared dev secret leaks, rotate it. That doesn't affect production.

**For the project lead**, one-time:

1. In Google Cloud Console, create a separate "dev" OAuth client (Web application) with authorized redirect URI `http://localhost:5184/signin-google`.
2. Add each developer's Google account as a test user on the consent screen.
3. Create a separate **dev** ESV API key at https://api.esv.org (not the production one).
4. Put the Google client ID/secret and the ESV key in the team password manager and share them with the developers.

**How production gets its values:** on Railway they're set as environment variables on the API service. ASP.NET Core reads them automatically, with `__` standing in for `:` (for example `Authentication__Google__ClientId`, `ESV__ApiKey`, `Anthropic__ApiKey`). Locally the same keys come from user-secrets. The code is identical either way.

## 1. Prerequisites (manual; the script does this)

| Tool | Required version | Why |
|---|---|---|
| Node.js | 20.19+, 22.12+ or 24+ | Angular 22 |
| .NET SDK | 8.0 | The API targets `net8.0` (an SDK 7 cannot build it) |
| PostgreSQL | 14+ (17 tested) | Database |

**Windows**: install with winget if missing (the Node and PostgreSQL installers may show an admin prompt):

```powershell
winget install --id Microsoft.DotNet.SDK.8 -e
winget install --id OpenJS.NodeJS.LTS -e
winget install --id PostgreSQL.PostgreSQL.17 -e --override '--mode unattended --superpassword password'
```

Open a new terminal afterwards so the updated PATH is picked up. Check with `node -v` and `dotnet --list-sdks`.

**macOS**: install with Homebrew:

```bash
brew install node dotnet@8 postgresql@17
brew services start postgresql@17
```

`dotnet@8` and `postgresql@17` are "keg-only", so add them to your PATH (put these in `~/.zshrc`, then open a new terminal):

```bash
export PATH="$(brew --prefix dotnet@8)/bin:$(brew --prefix postgresql@17)/bin:$PATH"
export DOTNET_ROOT="$(brew --prefix dotnet@8)/libexec"
```

## 2. Database (manual; the script does this)

The default connection string in `appsettings.json` is:

```
Host=localhost;Port=5432;Database=DailyDevotional;Username=postgres;Password=password
```

**Windows**: using `password` as the `postgres` password (as in the install command above) means no config changes are needed. Create the empty database:

```powershell
$env:PGPASSWORD = 'password'
& "C:\Program Files\PostgreSQL\17\bin\createdb.exe" -U postgres -h localhost DailyDevotional
```

**macOS**: Homebrew's PostgreSQL has no `postgres` role, so create it first, then the database:

```bash
psql -d postgres -c "CREATE ROLE postgres WITH SUPERUSER LOGIN PASSWORD 'password'"
PGPASSWORD=password createdb -U postgres -h localhost DailyDevotional
```

To use a different password or host, override the connection string with user-secrets (step 3) instead of editing `appsettings.json`:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=DailyDevotional;Username=postgres;Password=<yours>"
```

## 3. Backend secrets (manual; the script does this)

The API refuses to start without JWT settings. Store them in .NET user-secrets (kept outside the repo, per user and machine). Run from `backend/DailyDevotional.Api/DailyDevotional.Api`.

**Windows (PowerShell):**

```powershell
cd backend\DailyDevotional.Api\DailyDevotional.Api

# Required
$key = [Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }) -as [byte[]])
dotnet user-secrets set "Authentication:Jwt:Key" $key
dotnet user-secrets set "Authentication:Jwt:Issuer" "DailyDevotional.Api"
dotnet user-secrets set "Authentication:Jwt:Audience" "DailyDevotional.App"
```

**macOS (bash/zsh):**

```bash
cd backend/DailyDevotional.Api/DailyDevotional.Api

# Required
dotnet user-secrets set "Authentication:Jwt:Key" "$(openssl rand -base64 48)"
dotnet user-secrets set "Authentication:Jwt:Issuer" "DailyDevotional.Api"
dotnet user-secrets set "Authentication:Jwt:Audience" "DailyDevotional.App"
```

Optional, needed for specific features (same on both platforms):

```bash
# Google sign-in (shared dev client from the project lead)
# Its authorized redirect URI must include: http://localhost:5184/signin-google
dotnet user-secrets set "Authentication:Google:ClientId" "<client id>"
dotnet user-secrets set "Authentication:Google:ClientSecret" "<client secret>"

# Bible passage text (shared dev key from the project lead)
dotnet user-secrets set "ESV:ApiKey" "<key>"

# Makes the Google account you sign in with an admin (shows the schedule Import button)
dotnet user-secrets set "Authentication:AdminEmail" "<your Google email>"

# AI commentary (optional, your own key)
dotnet user-secrets set "Anthropic:ApiKey" "<key>"
```

Without the Google values the app starts but you cannot sign in. Without the ESV or Anthropic keys, those features will not work.

## 4. Frontend dependencies (manual; the script does this)

From the repo root:

```bash
npm ci
```

Use `npm ci` rather than `npm install`: it installs exactly what `package-lock.json` says and never rewrites it.

## 5. Run (the script does this with `-Run` / `--run`)

Backend (terminal 1). `--launch-profile http` sets the Development environment for you:

```bash
cd backend/DailyDevotional.Api/DailyDevotional.Api
dotnet run --launch-profile http
```

This also opens Swagger in your browser. To skip that, set the environment variable yourself:

```powershell
# Windows PowerShell
$env:ASPNETCORE_ENVIRONMENT = "Development"; dotnet run --no-launch-profile
```

```bash
# macOS
ASPNETCORE_ENVIRONMENT=Development dotnet run --no-launch-profile
```

The `Development` environment is required: .NET only reads user-secrets in Development, so without it the API crashes with "Authentication:Jwt:Key, Issuer, and Audience must all be configured".

Frontend (terminal 2, from the repo root):

```bash
npm start
```

Then open http://localhost:4200.

## How the pieces connect

- The dev frontend calls `http://localhost:5184/api` (`src/environments/environment.ts`).
- CORS allows `http://localhost:4200` by default (`AllowedOrigins` setting).
- After Google sign-in the API redirects to `http://localhost:4200/auth/callback` (`FrontendBaseUrl` setting) with a JWT.

## Quick checks

```powershell
# Windows
(Invoke-WebRequest http://localhost:4200 -UseBasicParsing).StatusCode              # 200
(Invoke-WebRequest http://localhost:5184/swagger/index.html -UseBasicParsing).StatusCode  # 200
```

```bash
# macOS
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:4200                 # 200
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5184/swagger/index.html  # 200
```

`GET /api/auth/me` returns 401 without a token, which is expected.

## Troubleshooting

| Symptom | Fix |
|---|---|
| `Authentication:Jwt:Key ... must all be configured` | Secrets missing, or not running in Development. See steps 3 and 5. |
| Port 5184 or 4200 already in use | Stop the other running copy of the API or `ng serve`. Windows: `Get-NetTCPConnection -LocalPort 5184 -State Listen`, then `Stop-Process -Id <id>`. macOS: `lsof -nP -iTCP:5184 -sTCP:LISTEN`, then `kill <pid>`. |
| `password authentication failed for user "postgres"` | The connection string password doesn't match your PostgreSQL install. Override it (step 2). |
| `ASPNETCORE_ENVIRONMENT=Development dotnet run` is not recognized (Windows) | That is bash syntax. In PowerShell use `$env:ASPNETCORE_ENVIRONMENT = "Development"`. |
| Build errors about `net8.0` | Install the .NET 8 SDK (step 1). |
| Angular CLI warns about the Node version | Upgrade Node (step 1). |
| `npm ci` fails with `EPERM ... unlink ... esbuild.exe` (Windows) | A running `ng serve` (or an editor/antivirus) has `node_modules` open. Stop `npm start`, close the window, and re-run. The Windows script now stops this repo's own dev-server processes automatically. |
| Windows script says it is blocked / not digitally signed | Run it as `powershell -ExecutionPolicy Bypass -File .\scripts\setup-local.ps1`. |
| macOS: `dotnet: command not found` or the wrong SDK after installing | `dotnet@8` is keg-only. Add it to PATH and set `DOTNET_ROOT` (step 1, macOS), then open a new terminal. |
| macOS: `role "postgres" does not exist` | Homebrew's PostgreSQL has no `postgres` role. Create it (step 2, macOS) or re-run the script, which does this. |
| macOS: `could not connect to server` | The service isn't running: `brew services start postgresql@17`. |
| macOS: script says it isn't macOS / Linux | The shell script supports macOS only. Follow the manual steps. |
| Google sign-in says "access blocked" or "redirect_uri_mismatch" | Your account isn't a test user on the dev OAuth client, or `http://localhost:5184/signin-google` isn't an authorized redirect URI. Ask the project lead. |
| Import button missing, or import returns 403 | `Authentication:AdminEmail` isn't set to the Google email you signed in with. Set it and restart the API. |
