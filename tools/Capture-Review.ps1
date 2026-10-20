$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$editor = 'D:\Unity\6000.3.26f1\Editor\Unity.exe'
$env:TEMP = Join-Path $projectRoot '.cache\tmp'
$env:TMP = $env:TEMP
New-Item -ItemType Directory -Force -Path $env:TEMP, (Join-Path $projectRoot 'Logs') | Out-Null
$log = Join-Path $projectRoot 'Logs\review-capture.log'
$arguments = @('-batchmode','-projectPath',('"' + $projectRoot + '"'),'-logFile',('"' + $log + '"'),'-screen-width','1080','-screen-height','1920','-executeMethod','DiamondMine.Editor.ReviewCapture.CapturePhone')
$capture = Start-Process -FilePath $editor -ArgumentList $arguments -WindowStyle Hidden -PassThru
$capture.WaitForExit()
$capture.Refresh()
Write-Output ('Unity capture exit: ' + $capture.ExitCode + '; log: ' + $log)
exit $capture.ExitCode
