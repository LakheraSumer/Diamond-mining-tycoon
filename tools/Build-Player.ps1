param(
    [ValidateSet("apk", "aab", "ios")]
    [string]$Target = "apk"
)
$ErrorActionPreference = "Stop"
$Editor = "D:\Unity\6000.3.26f1\Editor\Unity.exe"
$Project = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path $Editor)) {
    Write-Error "Unity 6000.3.26f1 was not found at $Editor. Install the pinned editor and required platform modules before building."
    exit 2
}
New-Item -ItemType Directory -Force -Path "$Project\Builds" | Out-Null
$env:TEMP = Join-Path $Project '.cache\tmp'
$env:TMP = $env:TEMP
New-Item -ItemType Directory -Force -Path $env:TEMP | Out-Null
$androidTools = Join-Path $Project '.cache\android-tools'
if (Test-Path -LiteralPath (Join-Path $androidTools 'OpenJDK\bin\java.exe')) { $env:DIAMOND_ANDROID_JDK = Join-Path $androidTools 'OpenJDK' }
if (Test-Path -LiteralPath (Join-Path $androidTools 'NDK\source.properties')) { $env:DIAMOND_ANDROID_NDK = Join-Path $androidTools 'NDK' }
if (Test-Path -LiteralPath (Join-Path $androidTools 'SDK\platforms\android-36\android.jar')) { $env:DIAMOND_ANDROID_SDK = Join-Path $androidTools 'SDK' }
$method = switch ($Target) {
    "apk" { "DiamondMine.Editor.BuildCommands.BuildAndroidApk" }
    "aab" { "DiamondMine.Editor.BuildCommands.BuildAndroidAab" }
    "ios" { "DiamondMine.Editor.BuildCommands.ExportIos" }
}
$log = Join-Path $Project "Builds\unity-$Target.log"
$buildTarget = if ($Target -eq 'ios') { 'iOS' } else { 'Android' }
$arguments = @('-batchmode','-nographics','-quit','-projectPath', ('"' + $Project + '"'), '-buildTarget', $buildTarget, '-logFile', ('"' + $log + '"'), '-executeMethod', $method)
$playerBuild = Start-Process -FilePath $Editor -ArgumentList $arguments -WindowStyle Hidden -PassThru
$playerBuild.WaitForExit()
$playerBuild.Refresh()
$code = $playerBuild.ExitCode
Write-Host "Unity exited $code. Log: $log"
if ($Target -eq "ios") {
    Write-Host "A Unity iOS export is an Xcode project. It is not a signed app, an archive, or a TestFlight upload."
}
exit $code
