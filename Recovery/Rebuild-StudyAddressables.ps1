param(
    [string]$UnityEditor = $env:UNITY_EDITOR_PATH
)
$ErrorActionPreference='Stop'
$taskRoot=$PSScriptRoot
$taskSource=Join-Path $taskRoot 'StudyProject'
$taskWorkspace=Join-Path $taskRoot 'AddressablesBuildProject'
$taskMarker=Join-Path $taskWorkspace '.recovery-addressables-build-workspace'
$taskEditorTools=Join-Path $taskRoot 'Tools\AddressablesEditorTools'

if([string]::IsNullOrWhiteSpace($UnityEditor)){
    $taskVersion=((Get-Content -LiteralPath (Join-Path $taskSource 'ProjectSettings\ProjectVersion.txt') | Select-String '^m_EditorVersion: ').Line -replace '^m_EditorVersion: ','')
    $taskChoices=@((Join-Path $env:ProgramFiles ('Unity\Hub\Editor\'+$taskVersion+'\Editor\Unity.exe')),('G:\Unity\'+$taskVersion+'\Editor\Unity.exe'))
    $UnityEditor=$taskChoices | Where-Object {Test-Path -LiteralPath $_} | Select-Object -First 1
}
if([string]::IsNullOrWhiteSpace($UnityEditor)){throw 'Set UNITY_EDITOR_PATH or pass -UnityEditor with the path to Unity.exe.'}
if(!(Test-Path -LiteralPath $UnityEditor)){throw "Unity editor not found: $UnityEditor"}
if((Test-Path -LiteralPath $taskWorkspace) -and !(Test-Path -LiteralPath $taskMarker)){
    throw 'AddressablesBuildProject exists without the recovery workspace marker; refusing to overwrite an unrelated project.'
}
$taskNormalized=$taskWorkspace.Replace('\','/')
$taskRunning=Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Where-Object {
    $_.CommandLine -and $_.CommandLine.Replace('\','/').IndexOf($taskNormalized,[StringComparison]::OrdinalIgnoreCase) -ge 0
}
if($taskRunning){throw 'The independent Addressables build workspace is already open in Unity.'}
New-Item -ItemType Directory -Path $taskWorkspace -Force | Out-Null
[IO.File]::WriteAllText($taskMarker,'Owned independent workspace for rebuilding recovered original Addressables assets.')

foreach($taskFolder in @('Assets','Packages','ProjectSettings')){
    if($taskFolder -eq 'Assets'){
        & robocopy (Join-Path $taskSource $taskFolder) (Join-Path $taskWorkspace $taskFolder) /E /XD Editor /NFL /NDL /NJH /NJS /NP
    }else{
        & robocopy (Join-Path $taskSource $taskFolder) (Join-Path $taskWorkspace $taskFolder) /E /NFL /NDL /NJH /NJS /NP
    }
    if($LASTEXITCODE -gt 7){throw "Could not snapshot $taskFolder"}
}
$taskEditorDirectory=Join-Path $taskWorkspace 'Assets\Editor'
New-Item -ItemType Directory -Path $taskEditorDirectory -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $taskRoot 'Tools\AddressablesBuild\BuildRecoveredAddressables.cs') -Destination $taskEditorDirectory -Force
foreach($taskFile in @('RecoveredShaderVariantFilter.cs','RecoveredShaderVariants.json')){
    Copy-Item -LiteralPath (Join-Path $taskSource ('Assets\Editor\'+$taskFile)) -Destination $taskEditorDirectory -Force
}
$taskEditorSources=@(Get-ChildItem -LiteralPath $taskEditorDirectory -Recurse -Filter '*.cs')
if($taskEditorSources.Count -ne 2 -or @($taskEditorSources | Where-Object {$_.Name -notin @('BuildRecoveredAddressables.cs','RecoveredShaderVariantFilter.cs')}).Count -ne 0){
    throw 'The independent build snapshot contains unexpected Editor scripts.'
}
foreach($taskAssembly in @('Unity.ResourceManager.dll','Unity.Addressables.dll')){
    Copy-Item -LiteralPath (Join-Path $taskEditorTools $taskAssembly) -Destination (Join-Path $taskWorkspace ('Assets\Plugins\'+$taskAssembly)) -Force
}
$taskSettings=Join-Path $taskWorkspace 'ProjectSettings\ProjectSettings.asset'
$taskText=[regex]::Replace([IO.File]::ReadAllText($taskSettings),'(?m)^  productName:.*$','  productName: Recovery.AddressablesBuildProject')
[IO.File]::WriteAllText($taskSettings,$taskText)
$taskLog=Join-Path $taskRoot 'Logs\addressables-rebuild.log'
New-Item -ItemType Directory -Path (Join-Path $taskRoot 'Logs') -Force | Out-Null
$taskArguments=@('-batchmode','-projectPath',('"'+$taskWorkspace+'"'),'-executeMethod','BuildRecoveredAddressables.Run','-logFile',('"'+$taskLog+'"'))
$taskProcess=Start-Process -FilePath $UnityEditor -ArgumentList $taskArguments -WindowStyle Hidden -PassThru
$taskProcess.WaitForExit()
if($taskProcess.ExitCode -ne 0){throw "Addressables build failed; see $taskLog"}
$taskReport=Get-Content -LiteralPath (Join-Path $taskRoot 'Evidence\addressables-windows-build.json') -Raw | ConvertFrom-Json
if(!$taskReport.passed){throw 'The build did not produce a successful catalog report.'}
if(Select-String -LiteralPath $taskLog -Pattern '^Shader error','error CS' -Quiet){throw "Shader/script compilation errors were reported; see $taskLog"}
$taskDestination=Join-Path $taskSource 'Assets\StreamingAssets\aa'
if(Test-Path -LiteralPath $taskDestination){
    $taskBackup=Join-Path $taskRoot ('Backups\AddressablesBuildInstall\'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))
    New-Item -ItemType Directory -Path $taskBackup -Force | Out-Null
    Copy-Item -LiteralPath $taskDestination -Destination $taskBackup -Recurse
}
& robocopy (Join-Path $taskWorkspace 'Assets\StreamingAssets\aa') $taskDestination /E /NFL /NDL /NJH /NJS /NP
if($LASTEXITCODE -gt 7){throw 'Could not install rebuilt local Addressables data.'}
Write-Output ('Rebuilt '+$taskReport.assetCount+' original prefabs / '+$taskReport.assetKeyCount+' asset keys for Windows; refreshed StudyProject StreamingAssets/aa.')
