$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$editor = 'D:\Unity\6000.3.26f1\Editor\Unity.exe'
$env:TEMP = Join-Path $projectRoot '.cache\tmp'
$env:TMP = $env:TEMP
New-Item -ItemType Directory -Force -Path $env:TEMP, (Join-Path $projectRoot 'Logs') | Out-Null
$log = Join-Path $projectRoot 'Logs\editor-validation.log'
$arguments = @('-batchmode','-nographics','-quit','-projectPath',('"' + $projectRoot + '"'),'-logFile',('"' + $log + '"'),'-executeMethod','DiamondMine.Editor.BuildCommands.ValidateProject')
$validation = Start-Process -FilePath $editor -ArgumentList $arguments -WindowStyle Hidden -PassThru
$validation.WaitForExit()
$validation.Refresh()
Write-Output ('Unity validation exit: ' + $validation.ExitCode + '; log: ' + $log)
exit $validation.ExitCode
