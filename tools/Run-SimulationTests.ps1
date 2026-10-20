$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$env:TEMP = Join-Path $projectRoot '.cache\tmp'
$env:TMP = $env:TEMP
$env:DOTNET_CLI_HOME = Join-Path $projectRoot '.cache\dotnet-home'
if (-not $env:NUGET_PACKAGES) { $env:NUGET_PACKAGES = 'D:\nuget' }
if (-not $env:DOTNET_ROOT) { $env:DOTNET_ROOT = 'D:\dotnet' }
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:PATH = $env:DOTNET_ROOT + ';' + $env:PATH
New-Item -ItemType Directory -Force -Path $env:TEMP, $env:DOTNET_CLI_HOME | Out-Null
& (Join-Path $env:DOTNET_ROOT 'dotnet.exe') test (Join-Path $projectRoot 'tests\DiamondMine.Simulation.Tests\DiamondMine.Simulation.Tests.csproj') --no-restore --nologo --verbosity minimal --logger 'trx;LogFileName=simulation.trx' --results-directory (Join-Path $projectRoot 'tests\results')
exit $LASTEXITCODE
