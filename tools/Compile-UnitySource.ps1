# Compiler/API check against the installed editor and its bundled uGUI sources.
# This does not import assets, run Unity, activate a license, or build a player.
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$editorData = 'D:\Unity\6000.3.26f1\Editor\Data'
$outputRoot = Join-Path $projectRoot '.cache\compile-only'
New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null
$compiler = Join-Path $editorData 'DotNetSdkRoslyn\csc.dll'
$dotnet = 'D:\dotnet\dotnet.exe'
$refs = @(Get-ChildItem -LiteralPath (Join-Path $editorData 'NetStandard\ref\2.1.0') -Filter '*.dll')
$refs += @(Get-ChildItem -LiteralPath (Join-Path $editorData 'Managed\UnityEngine') -Filter '*.dll')
$uiRoot = Join-Path $editorData 'Resources\PackageManager\BuiltInPackages\com.unity.ugui\Runtime\UGUI'
function Compile([string]$name, [string[]]$sources, [string[]]$references, [string]$symbols) {
    $response = Join-Path $outputRoot ($name + '.rsp')
    $arguments = @('-nologo', '-nostdlib+', '-target:library', '-langversion:9.0', ('-define:' + $symbols), ('-out:"' + (Join-Path $outputRoot ($name + '.dll')) + '"'))
    $arguments += $references | ForEach-Object { '-r:"' + $_ + '"' }
    $arguments += $sources | ForEach-Object { '"' + $_ + '"' }
    Set-Content -LiteralPath $response -Value $arguments -Encoding utf8
    & $dotnet $compiler -noconfig ('@' + $response)
    if ($LASTEXITCODE -ne 0) { throw "$name compiler check failed" }
    Write-Output "$name compiler check passed"
}
$baseSymbols = 'UNITY_6000_3_OR_NEWER,UNITY_6000_0_OR_NEWER,UNITY_2023_2_OR_NEWER,UNITY_2022_1_OR_NEWER,UNITY_2021_1_OR_NEWER,UNITY_2020_1_OR_NEWER,UNITY_2019_1_OR_NEWER,ENABLE_LEGACY_INPUT_MANAGER,PACKAGE_PHYSICS'
$referencePaths = @($refs | ForEach-Object { $_.FullName })
Compile 'UnityEngine.UI' @(Get-ChildItem $uiRoot -Filter '*.cs' -Recurse | ForEach-Object { $_.FullName }) $referencePaths $baseSymbols
Compile 'DiamondMine.Simulation' @(Get-ChildItem (Join-Path $projectRoot 'Assets\_Project\Scripts\Simulation') -Filter '*.cs' | ForEach-Object { $_.FullName }) $referencePaths $baseSymbols
$runtimeReferences = $referencePaths + @((Join-Path $outputRoot 'UnityEngine.UI.dll'), (Join-Path $outputRoot 'DiamondMine.Simulation.dll'))
$runtimeSources = @(Get-ChildItem (Join-Path $projectRoot 'Assets\_Project\Scripts\Runtime') -Filter '*.cs' | ForEach-Object { $_.FullName })
Compile 'DiamondMine.Runtime' $runtimeSources $runtimeReferences ($baseSymbols + ',UNITY_ANDROID')
Compile 'DiamondMine.Runtime.Development' $runtimeSources $runtimeReferences ($baseSymbols + ',UNITY_ANDROID,DEVELOPMENT_BUILD')
Compile 'DiamondMine.Runtime.Ios' $runtimeSources $runtimeReferences ($baseSymbols + ',UNITY_IOS')
$editorReferences = $runtimeReferences + @((Join-Path $outputRoot 'DiamondMine.Runtime.dll'))
$editorReferences += @(Get-ChildItem (Join-Path $editorData 'Managed') -Filter 'UnityEditor*.dll' | Where-Object { $_.Name -ne 'UnityEditor.dll' } | ForEach-Object { $_.FullName })
Compile 'DiamondMine.Editor' @(Get-ChildItem (Join-Path $projectRoot 'Assets\_Project\Editor') -Filter '*.cs' | ForEach-Object { $_.FullName }) $editorReferences ($baseSymbols + ',UNITY_EDITOR')
