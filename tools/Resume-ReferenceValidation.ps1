param([switch]$BuildPlayers,[switch]$AndroidApk)
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$editor='D:\Unity\6000.3.26f1\Editor\Unity.exe'
$versionMatch=[regex]::Match((Get-Content -LiteralPath (Join-Path $project 'Assets\_Project\Scripts\Runtime\Branding.cs') -Raw),'public const string Version = "([^"]+)"')
if(-not $versionMatch.Success){throw 'Unable to read the project version.'}
$version=$versionMatch.Groups[1].Value
if(Get-Process -Name Unity -ErrorAction SilentlyContinue){throw 'Close the Unity Editor before running this batch review. Normal player saves are not used.'}
$env:TEMP=Join-Path $project '.cache\tmp';$env:TMP=$env:TEMP
foreach($check in @(
    @{Method='DiamondMine.Editor.ReferenceAssetImport.Run';Log='reference-import-assets-unity.log'},
    @{Method='DiamondMine.Editor.BuildCommands.ValidateProject';Log='reference-import-validate-unity.log'}
)){
    $log=Join-Path $project ('Logs\'+$check.Log)
    $arguments=@('-batchmode','-nographics','-quit','-projectPath',('"'+$project+'"'),'-logFile',('"'+$log+'"'),'-executeMethod',$check.Method)
    $process=Start-Process -FilePath $editor -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $process.WaitForExit();$process.Refresh()
    if($process.ExitCode -ne 0){throw ('Unity asset import/project validation failed. See '+$log)}
}
foreach($kind in @('crew','ui','gallery')){
    & (Join-Path $PSScriptRoot 'Run-ReferenceReview.ps1') -Review $kind
    if($LASTEXITCODE -ne 0){throw ('Unity '+$kind+' review failed. Inspect Logs and screenshot error files.')}
}
foreach($script in @('Compare-Crew.py','Compare-ReferenceUi.py','Verify-CharacterViews.py')){
    & python (Join-Path $PSScriptRoot ('art\'+$script))
    if($LASTEXITCODE -ne 0){throw ($script+' failed')}
}
& (Join-Path $PSScriptRoot 'Verify-Project.ps1')
if($LASTEXITCODE -ne 0){throw 'Rules/API verification failed'}
if($BuildPlayers -or $AndroidApk){
    # A separate workspace keeps platform build settings out of the shared project.
    $buildWorkspace=Join-Path $project '.cache\build-workspace'
    foreach($folder in @('Assets','Packages','ProjectSettings')){
        & robocopy (Join-Path $project $folder) (Join-Path $buildWorkspace $folder) /E /R:1 /W:1 /NFL /NDL /NJH /NJS /NP /XD _Recovery
        if($LASTEXITCODE -ge 8){throw ('Unable to sync '+$folder)}
    }
    $targets=if($BuildPlayers){@('apk','ios')}else{@('apk')}
    $buildInputs=@()
    foreach($folder in @('Assets','Packages','ProjectSettings')){
        $buildInputs+=Get-ChildItem -LiteralPath (Join-Path $project $folder) -Recurse -File | ForEach-Object {
            @{file=$_.FullName.Substring($project.Length+1).Replace('\','/');sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}
        }
    }
    @{createdUtc=[DateTime]::UtcNow.ToString('O');workspace=$buildWorkspace;inputs=$buildInputs} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $project 'Builds\latest-reference-build-inputs.json') -Encoding UTF8
    $previousApk=Join-Path $project ('Builds\Android\DiamondMineTycoon-'+$version+'-dev.apk')
    if(Test-Path -LiteralPath $previousApk){
        $archive=Join-Path $project 'Builds\Archive'
        New-Item -ItemType Directory -Force -Path $archive | Out-Null
        Copy-Item -LiteralPath $previousApk -Destination (Join-Path $archive ('DiamondMineTycoon-dev-before-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')+'.apk'))
    }
    foreach($target in $targets){
        & (Join-Path $PSScriptRoot 'Build-Player.ps1') -Target $target -ProjectPath $buildWorkspace
        if($LASTEXITCODE -ne 0){throw ($target+' build failed; see Builds/unity-'+$target+'.log')}
        if($target -eq 'apk'){
            & python (Join-Path $PSScriptRoot 'Inspect-Build.py') $previousApk
            if($LASTEXITCODE -ne 0){throw 'APK signature, alignment or static native-library inspection failed.'}
        }
    }
}
& python (Join-Path $PSScriptRoot 'art\Build-VisualGallery.py')
if($LASTEXITCODE -ne 0){throw 'Gallery assembly failed'}
& python (Join-Path $PSScriptRoot 'art\Package-ReferenceUpdate.py')
if($LASTEXITCODE -ne 0){throw 'Evidence checks blocked the verified update ZIP; see release/artwork/reference-update-validation.json'}
Write-Output 'Reference validation completed. Visual likeness and device acceptance remain separate reviews.'
