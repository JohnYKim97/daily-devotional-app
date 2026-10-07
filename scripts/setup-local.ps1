<#
.SYNOPSIS
  One-command local setup for the Daily Devotional app (Windows / PowerShell).

.DESCRIPTION
  1. Installs missing prerequisites with winget (Node.js, .NET 8 SDK, PostgreSQL)
  2. Makes sure the PostgreSQL service is running
  3. Creates the local DailyDevotional database if it does not exist
  4. Stores backend secrets in .NET user-secrets (outside the repo)
  5. Restores backend NuGet packages
  6. Installs frontend dependencies with `npm ci` (does not modify package-lock.json)
  7. Optionally starts the API and the frontend (-Run)

  Safe to re-run: anything already done is skipped, and existing secrets are kept unless you pass -Force.

.PARAMETER Yes
  Install missing prerequisites without asking first.

.PARAMETER SkipInstall
  Never install anything; only check and report what is missing.

.PARAMETER Run
  After setup, start the API and the frontend in their own windows and open the browser.

.PARAMETER Force
  Overwrite secrets that are already set.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File .\scripts\setup-local.ps1
  powershell -ExecutionPolicy Bypass -File .\scripts\setup-local.ps1 -Yes -Run
  powershell -ExecutionPolicy Bypass -File .\scripts\setup-local.ps1 -PgPassword 'mypassword'
#>
[CmdletBinding()]
param(
  [string]$PgHost = 'localhost',
  [int]$PgPort = 5432,
  [string]$PgUser = 'postgres',
  [string]$PgPassword = 'password',
  [string]$DbName = 'DailyDevotional',
  [switch]$Yes,
  [switch]$SkipInstall,
  [switch]$Run,
  [switch]$Force
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$apiDir = Join-Path $repoRoot 'backend\DailyDevotional.Api\DailyDevotional.Api'

function Write-Step($text) { Write-Host "`n== $text" -ForegroundColor Cyan }
function Write-Ok($text) { Write-Host "   OK  $text" -ForegroundColor Green }
function Write-Warn($text) { Write-Host "   !!  $text" -ForegroundColor Yellow }
function Write-Fail($text) { Write-Host "   XX  $text" -ForegroundColor Red }

# Installers update PATH for new terminals only; pull the fresh value into this session.
function Update-SessionPath {
  $machine = [Environment]::GetEnvironmentVariable('Path', 'Machine')
  $user = [Environment]::GetEnvironmentVariable('Path', 'User')
  $env:Path = "$machine;$user"
}

function Test-NodeOk {
  if (-not (Get-Command node -ErrorAction SilentlyContinue)) { return $false }
  $v = [version](node -v).TrimStart('v')
  return ($v.Major -ge 24) -or
         ($v.Major -eq 22 -and $v.Minor -ge 12) -or
         ($v.Major -eq 20 -and $v.Minor -ge 19)
}

function Test-DotnetOk {
  if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { return $false }
  return [bool]((dotnet --list-sdks) -match '^8\.')
}

function Find-Psql {
  $found = Get-ChildItem 'C:\Program Files\PostgreSQL\*\bin\psql.exe' -ErrorAction SilentlyContinue |
    Sort-Object FullName -Descending | Select-Object -First 1
  if ($found) { return $found.FullName }
  $cmd = Get-Command psql -ErrorAction SilentlyContinue
  if ($cmd) { return $cmd.Source }
  return $null
}

# ---------------------------------------------------------------- 1. prerequisites
Write-Step '1/6  Prerequisites (Node.js, .NET 8 SDK, PostgreSQL)'
Update-SessionPath

$missing = @()
if (Test-NodeOk) { Write-Ok "Node $(node -v)" }
else {
  $missing += @{ Name = 'Node.js LTS'; Id = 'OpenJS.NodeJS.LTS'; Override = $null }
}
if (Test-DotnetOk) { Write-Ok '.NET 8 SDK' }
else {
  $missing += @{ Name = '.NET 8 SDK'; Id = 'Microsoft.DotNet.SDK.8'; Override = $null }
}
if (Find-Psql) { Write-Ok 'PostgreSQL' }
else {
  $missing += @{
    Name = "PostgreSQL 17 (superuser password: $PgPassword)"
    Id = 'PostgreSQL.PostgreSQL.17'
    Override = "--mode unattended --superpassword $PgPassword"
  }
}

if ($missing.Count -gt 0) {
  Write-Warn ('Missing: ' + (($missing | ForEach-Object { $_.Name }) -join ', '))

  if ($SkipInstall) {
    Write-Fail 'Install them (see LOCAL_SETUP.md), open a NEW terminal and re-run. (-SkipInstall was given.)'
    exit 1
  }
  if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
    Write-Fail 'winget is not available. Install "App Installer" from the Microsoft Store, or install the tools above by hand (see LOCAL_SETUP.md).'
    exit 1
  }
  if (-not $Yes) {
    Write-Host '   These will be installed with winget. Windows may show administrator (UAC) prompts.'
    $answer = Read-Host '   Install now? [Y/n]'
    if ($answer -match '^(n|no)$') {
      Write-Fail 'Cancelled. Install them yourself and re-run.'
      exit 1
    }
  }

  foreach ($item in $missing) {
    Write-Host "   Installing $($item.Name) ..."
    $wingetArgs = @('install', '--id', $item.Id, '-e', '--accept-package-agreements', '--accept-source-agreements', '--silent')
    if ($item.Override) { $wingetArgs += @('--override', $item.Override) }
    & winget @wingetArgs
    if ($LASTEXITCODE -ne 0) { Write-Warn "winget exited with code $LASTEXITCODE for $($item.Id) (may already be installed; re-checking)" }
  }

  Update-SessionPath
  $stillMissing = @()
  if (-not (Test-NodeOk)) { $stillMissing += 'Node.js 20.19+/22.12+/24+' }
  if (-not (Test-DotnetOk)) { $stillMissing += '.NET 8 SDK' }
  if (-not (Find-Psql)) { $stillMissing += 'PostgreSQL' }
  if ($stillMissing.Count -gt 0) {
    Write-Fail ('Still missing: ' + ($stillMissing -join ', '))
    Write-Host '       Open a NEW terminal (so PATH refreshes) and re-run this script. If it persists, install by hand (see LOCAL_SETUP.md).'
    exit 1
  }
  Write-Ok 'prerequisites installed'
}

$psqlPath = Find-Psql

# ---------------------------------------------------------------- 2. PostgreSQL service
Write-Step '2/6  PostgreSQL service'
$pgService = Get-Service -Name 'postgresql*' -ErrorAction SilentlyContinue | Select-Object -First 1
if ($pgService) {
  if ($pgService.Status -ne 'Running') {
    try {
      Start-Service $pgService.Name -ErrorAction Stop
      Write-Ok "started $($pgService.Name)"
    } catch {
      Write-Warn "Could not start $($pgService.Name) (needs an administrator terminal). Start it from services.msc, then re-run."
    }
  } else { Write-Ok "$($pgService.Name) is running" }
} else {
  Write-Warn "No local PostgreSQL service found; assuming PostgreSQL is reachable at ${PgHost}:$PgPort."
}

# ---------------------------------------------------------------- 3. database
Write-Step "3/6  Database '$DbName' on ${PgHost}:$PgPort"
$env:PGPASSWORD = $PgPassword
$exists = & $psqlPath -U $PgUser -h $PgHost -p $PgPort -tAc "SELECT 1 FROM pg_database WHERE datname='$DbName'" 2>&1
if ($LASTEXITCODE -ne 0) {
  Write-Fail "Could not connect to PostgreSQL: $exists"
  Write-Host '       Is the service running, and is the password right? Re-run with -PgPassword <yours>.'
  exit 1
}
if ("$exists".Trim() -eq '1') {
  Write-Ok 'already exists'
} else {
  $createdb = Join-Path (Split-Path $psqlPath) 'createdb.exe'
  & $createdb -U $PgUser -h $PgHost -p $PgPort $DbName
  if ($LASTEXITCODE -ne 0) { throw 'createdb failed' }
  Write-Ok 'created (tables are created and seeded automatically when the API first starts)'
}
Remove-Item Env:\PGPASSWORD

# ---------------------------------------------------------------- 4. secrets
Write-Step '4/6  Backend secrets (.NET user-secrets, stored outside the repo)'
Push-Location $apiDir
try {
  $existing = @{}
  foreach ($line in (dotnet user-secrets list)) {
    if ($line -match '^(.+?) = ') { $existing[$Matches[1]] = $true }
  }

  function Set-Secret([string]$Name, [string]$Value) {
    dotnet user-secrets set $Name $Value | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Failed to set $Name" }
  }

  function Read-PlainSecret([string]$Prompt) {
    $secure = Read-Host $Prompt -AsSecureString
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try { [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) }
  }

  # Required JWT settings: generated locally, never shared between devs.
  if ($Force -or -not $existing.ContainsKey('Authentication:Jwt:Key')) {
    $bytes = New-Object byte[] 48
    [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    Set-Secret 'Authentication:Jwt:Key' ([Convert]::ToBase64String($bytes))
    Write-Ok 'Authentication:Jwt:Key generated'
  } else { Write-Ok 'Authentication:Jwt:Key already set' }

  if ($Force -or -not $existing.ContainsKey('Authentication:Jwt:Issuer')) {
    Set-Secret 'Authentication:Jwt:Issuer' 'DailyDevotional.Api'
    Write-Ok 'Authentication:Jwt:Issuer set'
  } else { Write-Ok 'Authentication:Jwt:Issuer already set' }

  if ($Force -or -not $existing.ContainsKey('Authentication:Jwt:Audience')) {
    Set-Secret 'Authentication:Jwt:Audience' 'DailyDevotional.App'
    Write-Ok 'Authentication:Jwt:Audience set'
  } else { Write-Ok 'Authentication:Jwt:Audience already set' }

  # Only needed if you use a non-default PostgreSQL password / host / port.
  $defaultConn = "Host=localhost;Port=5432;Database=$DbName;Username=postgres;Password=password"
  $conn = "Host=$PgHost;Port=$PgPort;Database=$DbName;Username=$PgUser;Password=$PgPassword"
  if ($conn -ne $defaultConn) {
    Set-Secret 'ConnectionStrings:DefaultConnection' $conn
    Write-Ok 'ConnectionStrings:DefaultConnection overridden'
  }

  # Optional values: press Enter to skip. Get the shared dev values from the project lead (see LOCAL_SETUP.md).
  Write-Host "`n   Optional values. Press Enter to skip any you don't have yet (re-run the script later to add them)." -ForegroundColor Gray
  $optional = @(
    @{ Name = 'Authentication:Google:ClientId';     Prompt = 'Google OAuth client ID (needed to sign in)';        Secret = $false },
    @{ Name = 'Authentication:Google:ClientSecret'; Prompt = 'Google OAuth client secret (needed to sign in)';    Secret = $true  },
    @{ Name = 'ESV:ApiKey';                         Prompt = 'ESV API key (passage text, from api.esv.org)';      Secret = $true  },
    @{ Name = 'Anthropic:ApiKey';                   Prompt = 'Anthropic API key (AI commentary, optional)';       Secret = $true  },
    @{ Name = 'Authentication:AdminEmail';          Prompt = 'Your Google sign-in email (makes you admin to import the schedule)'; Secret = $false }
  )
  foreach ($item in $optional) {
    if (-not $Force -and $existing.ContainsKey($item.Name)) {
      Write-Ok "$($item.Name) already set"
      continue
    }
    $value = if ($item.Secret) { Read-PlainSecret "   $($item.Prompt)" } else { Read-Host "   $($item.Prompt)" }
    if ([string]::IsNullOrWhiteSpace($value)) {
      Write-Warn "$($item.Name) skipped"
    } else {
      Set-Secret $item.Name $value.Trim()
      Write-Ok "$($item.Name) set"
    }
  }

  # ------------------------------------------------------------- 5. backend packages
  Write-Step '5/6  Backend packages (dotnet restore)'
  dotnet restore | Out-Null
  if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed' }
  Write-Ok 'restored'
}
finally { Pop-Location }

# ---------------------------------------------------------------- 6. frontend packages
Write-Step '6/6  Frontend packages (npm ci)'
Push-Location $repoRoot
try {
  # `npm ci` deletes node_modules first. A running `ng serve` keeps esbuild.exe open, and Windows then
  # refuses the delete (EPERM). Stop this repo's own dev-server processes before reinstalling.
  $locking = Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -in 'node.exe', 'esbuild.exe' -and $_.CommandLine -like "*$repoRoot*node_modules*" }
  if ($locking) {
    Write-Warn "Stopping $(@($locking).Count) running dev-server process(es) from this repo so npm can replace node_modules"
    $locking | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
    Start-Sleep -Seconds 2
  }
  npm ci
  if ($LASTEXITCODE -ne 0) { throw 'npm ci failed' }
  Write-Ok 'installed'
}
finally { Pop-Location }

# ---------------------------------------------------------------- done
Write-Host "`nSetup complete." -ForegroundColor Green

if ($Run) {
  Write-Step 'Starting the app'
  $busy = @(5184, 4200) | Where-Object {
    Get-NetTCPConnection -LocalPort $_ -State Listen -ErrorAction SilentlyContinue
  }
  if ($busy) {
    Write-Fail "Port(s) $($busy -join ', ') already in use: another copy of the API (5184) or the frontend (4200) is still running."
    Write-Host '       Close that window (or stop the process) and re-run with -Run. Setup itself completed.'
    exit 1
  }
  $apiCommand = "`$env:ASPNETCORE_ENVIRONMENT='Development'; Set-Location '$apiDir'; dotnet run --no-launch-profile"
  $webCommand = "Set-Location '$repoRoot'; npm start"
  Start-Process powershell -ArgumentList '-NoExit', '-Command', $apiCommand
  Start-Process powershell -ArgumentList '-NoExit', '-Command', $webCommand
  Write-Ok 'API window and frontend window opened (first start takes ~30s: it builds, then migrates the database)'
  Write-Host '   Close those two windows to stop the app.'
  Start-Sleep -Seconds 20
  Start-Process 'http://localhost:4200'
} else {
  Write-Host @"

Run the backend (terminal 1):
  cd backend\DailyDevotional.Api\DailyDevotional.Api
  dotnet run --launch-profile http

Run the frontend (terminal 2, repo root):
  npm start

Then open http://localhost:4200
(Or re-run this script with -Run to start both for you.)
"@
}
