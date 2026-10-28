param([string]$Ids='')
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$workspace=Join-Path $root '.cache\timber-workspace'
foreach ($folder in @('Assets','Packages','ProjectSettings')) {
    & robocopy (Join-Path $root $folder) (Join-Path $workspace $folder) /E /R:1 /W:1 /NFL /NDL /NJH /NJS /NP /XD _Recovery
    if ($LASTEXITCODE -ge 8) { throw "Unable to sync $folder" }
}
$env:DMT_CHARACTER_IDS=$Ids
$env:DIAMOND_QA_EVIDENCE_ROOT=$root
$env:TEMP=Join-Path $root '.cache\tmp'
$env:TMP=$env:TEMP
New-Item -ItemType Directory -Force -Path $env:TEMP | Out-Null
$log=Join-Path $root 'Logs\crew-unity.log'
$arguments=@('-batchmode','-projectPath',('"'+$workspace+'"'),'-force-d3d11','-logFile',('"'+$log+'"'),'-executeMethod','DiamondMine.Editor.CrewVisualReview.Run')
$process=Start-Process -FilePath 'D:\Unity\6000.3.26f1\Editor\Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit();$process.Refresh()
Write-Output ('Unity crew review exit: '+$process.ExitCode+'; '+$log)
exit $process.ExitCode
