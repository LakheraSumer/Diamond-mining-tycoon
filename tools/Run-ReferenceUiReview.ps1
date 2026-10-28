param([string]$ProjectPath='')
$ErrorActionPreference='Stop'
$projectRoot=Split-Path -Parent $PSScriptRoot
$reviewWorkspace=if($ProjectPath){[IO.Path]::GetFullPath($ProjectPath)}else{Join-Path $projectRoot '.cache\timber-workspace'}
foreach($folder in @('Assets','Packages','ProjectSettings')) {
    & robocopy (Join-Path $projectRoot $folder) (Join-Path $reviewWorkspace $folder) /E /R:1 /W:1 /NFL /NDL /NJH /NJS /NP /XD _Recovery
    if($LASTEXITCODE -ge 8){throw "Unable to sync $folder into isolated review workspace."}
}
$env:DIAMOND_QA_EVIDENCE_ROOT=$projectRoot
$env:TEMP=Join-Path $projectRoot '.cache\tmp'
$env:TMP=$env:TEMP
New-Item -ItemType Directory -Force -Path $env:TEMP | Out-Null
$reviewLog=Join-Path $projectRoot 'Logs\reference-ui-unity.log'
$arguments=@('-batchmode','-projectPath',('"'+$reviewWorkspace+'"'),'-force-d3d11','-logFile',('"'+$reviewLog+'"'),'-executeMethod','DiamondMine.Editor.ReferenceUiReview.Run')
$process=Start-Process -FilePath 'D:\Unity\6000.3.26f1\Editor\Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit();$process.Refresh()
Write-Output ('Unity reference UI review exit: '+$process.ExitCode+'; '+$reviewLog)
exit $process.ExitCode
