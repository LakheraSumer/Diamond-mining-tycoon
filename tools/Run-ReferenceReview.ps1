param([ValidateSet('ui','gallery','crew','assets')][string]$Review='ui')
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
$workspace=Join-Path $project '.cache\timber-workspace'
foreach($folder in @('Assets','Packages','ProjectSettings')) {
    & robocopy (Join-Path $project $folder) (Join-Path $workspace $folder) /E /R:1 /W:1 /NFL /NDL /NJH /NJS /NP /XD _Recovery
    if($LASTEXITCODE -ge 8){ throw "Unable to sync $folder" }
}
if($Review -eq 'gallery') {
    Copy-Item -LiteralPath (Join-Path $project 'tools\art\VisualGalleryCapture.cs') -Destination (Join-Path $workspace 'Assets\_Project\Editor\VisualGalleryCapture.cs') -Force
}
$method=switch($Review) {
    'ui' {'DiamondMine.Editor.ReferenceUiReview.Run'}
    'gallery' {'DiamondMine.Editor.VisualGalleryCapture.Run'}
    'crew' {'DiamondMine.Editor.CrewVisualReview.Run'}
    'assets' {'DiamondMine.Editor.ReferenceAssetImport.Run'}
}
$env:DIAMOND_QA_EVIDENCE_ROOT=$project
$env:DMT_REVIEW_SKIP_IMPORT='1'
$env:TEMP=Join-Path $project '.cache\tmp'
$env:TMP=$env:TEMP
New-Item -ItemType Directory -Force -Path $env:TEMP | Out-Null
$log=Join-Path $project ('Logs\reference-'+$Review+'-unity.log')
$arguments=@('-batchmode','-projectPath',('"'+$workspace+'"'),'-force-d3d11','-logFile',('"'+$log+'"'),'-executeMethod',$method)
$process=Start-Process -FilePath 'D:\Unity\6000.3.26f1\Editor\Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit();$process.Refresh()
Write-Output ('Unity '+$Review+' review exit: '+$process.ExitCode+'; '+$log)
exit $process.ExitCode
