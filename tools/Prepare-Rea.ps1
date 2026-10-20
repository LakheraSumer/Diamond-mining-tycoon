$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$reaRoot = 'D:\games\rea-main\rea-main'
$evidenceRoot = Join-Path $projectRoot 'analysis\evidence'
New-Item -ItemType Directory -Force -Path $evidenceRoot | Out-Null
$env:npm_config_cache = Join-Path $projectRoot '.cache\npm'
$env:TEMP = Join-Path $projectRoot '.cache\tmp'
$env:TMP = $env:TEMP
$env:HUSKY = '0'
$env:TURBO_TELEMETRY_DISABLED = '1'
New-Item -ItemType Directory -Force -Path $env:TEMP | Out-Null
Set-Location -LiteralPath $reaRoot
if ((& node --version) -ne 'v24.18.0') { throw 'Expected checkout-pinned Node v24.18.0' }
if ((& npm.cmd --version) -ne '11.16.0') { throw 'Expected checkout-pinned npm 11.16.0' }
$lockHash = (Get-FileHash -LiteralPath package-lock.json -Algorithm SHA256).Hash
# Use locked dependencies; suppress dependency lifecycle scripts, including Frida downloads.
# REA build commands are run explicitly after inspection. No agent registration is changed.
& npm.cmd ci --ignore-scripts --no-audit --no-fund
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& npm.cmd run build:cached
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
if ((Get-FileHash -LiteralPath package-lock.json -Algorithm SHA256).Hash -ne $lockHash) { throw 'REA lockfile changed unexpectedly' }
& node scripts/rea.mjs --help | Out-File -LiteralPath (Join-Path $evidenceRoot 'rea-help.txt') -Encoding utf8
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& node scripts/rea.mjs doctor --help | Out-File -LiteralPath (Join-Path $evidenceRoot 'rea-doctor-help.txt') -Encoding utf8
exit $LASTEXITCODE
