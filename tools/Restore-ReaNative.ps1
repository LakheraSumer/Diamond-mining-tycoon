$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$reaRoot = 'D:\games\rea-main\rea-main'
$env:npm_config_cache = Join-Path $projectRoot '.cache\npm'
$env:TEMP = Join-Path $projectRoot '.cache\tmp'
$env:TMP = $env:TEMP
$env:HUSKY = '0'
$env:TURBO_TELEMETRY_DISABLED = '1'
& npm.cmd pack rea-agents@6.7.0 --ignore-scripts --pack-destination (Join-Path $projectRoot '.cache') --json | Out-File -LiteralPath (Join-Path $projectRoot 'analysis\evidence\rea-package.json') -Encoding utf8
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& python (Join-Path $PSScriptRoot 'Restore-ReaNative.py')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Set-Location -LiteralPath $reaRoot
& npm.cmd run build:cached
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& node scripts/rea.mjs --help | Out-File -LiteralPath (Join-Path $projectRoot 'analysis\evidence\rea-help.txt') -Encoding utf8
& node scripts/rea.mjs doctor --help | Out-File -LiteralPath (Join-Path $projectRoot 'analysis\evidence\rea-doctor-help.txt') -Encoding utf8
exit $LASTEXITCODE
