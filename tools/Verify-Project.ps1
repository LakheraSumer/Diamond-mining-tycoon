$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Run-SimulationTests.ps1')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& (Join-Path $PSScriptRoot 'Compile-UnitySource.ps1') | Tee-Object -FilePath (Join-Path (Split-Path -Parent $PSScriptRoot) 'tests\results\unity-api-compile.txt')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& python (Join-Path $PSScriptRoot 'Record-Verification.py')
exit $LASTEXITCODE
