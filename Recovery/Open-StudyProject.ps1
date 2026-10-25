param([string]$UnityEditor = $env:UNITY_EDITOR_PATH)
$ErrorActionPreference='Stop'
$taskProject=Join-Path $PSScriptRoot 'StudyProject'
$taskVersion=((Get-Content -LiteralPath (Join-Path $taskProject 'ProjectSettings\ProjectVersion.txt') | Select-String '^m_EditorVersion: ').Line -replace '^m_EditorVersion: ','')
if([string]::IsNullOrWhiteSpace($UnityEditor)){
    $taskChoices=@((Join-Path $env:ProgramFiles ('Unity\Hub\Editor\'+$taskVersion+'\Editor\Unity.exe')),('G:\Unity\'+$taskVersion+'\Editor\Unity.exe'))
    $UnityEditor=$taskChoices | Where-Object {Test-Path -LiteralPath $_} | Select-Object -First 1
}
if([string]::IsNullOrWhiteSpace($UnityEditor)){throw 'Set UNITY_EDITOR_PATH or pass -UnityEditor with the path to Unity.exe.'}
$taskEditor=$UnityEditor
if(-not(Test-Path -LiteralPath $taskEditor)){throw "Unity Editor not found: $taskEditor"}
$taskRunning=Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Where-Object {$_.CommandLine -like ('*'+$taskProject+'*')}
if($taskRunning){Write-Output 'StudyProject is already open. This launcher does not restart an existing editor.';return}
$taskLibraries=@(
    @{Name='Unity.Mathematics.dll';Expected='3A1BE62E9BC5B692432D47CA9AF877DB407157DCF520C131B499E5F5F45E7DD5';Damaged='C812927A2AE6BC684C11E7266FF074931DEE022A6D317D62961AE83CDAE7AF48'},
    @{Name='Unity.Burst.dll';Expected='BB77BC0CF4E7F5850602D5922D5946D30A6B443B554A565399F3E041E990377D';Damaged='97432DC1A7B3E25F4B7FDE9D4BA22714008ECA5ACED87B3381724BB828FBAE61'}
)
foreach($taskLibrary in $taskLibraries){
    $taskTarget=Join-Path $taskProject ('Assets\Plugins\'+$taskLibrary.Name)
    $taskHash=(Get-FileHash -LiteralPath $taskTarget).Hash
    if($taskHash -eq $taskLibrary.Expected){continue}
    if($taskHash -ne $taskLibrary.Damaged){throw "Unexpected library identity; inspect before replacing: $taskTarget"}
    $taskBackup=Join-Path $PSScriptRoot ('Backups\ProtectedLaunch\'+(Get-Date -Format 'yyyyMMdd-HHmmss-ffff'))
    New-Item -ItemType Directory -Path $taskBackup -Force | Out-Null
    Copy-Item -LiteralPath $taskTarget -Destination $taskBackup
    $taskCandidate=Join-Path $PSScriptRoot ('Tools\OfficialPlugins\'+$taskLibrary.Name)
    if((Get-FileHash -LiteralPath $taskCandidate).Hash -ne $taskLibrary.Expected){throw "Invalid official adapted candidate: $taskCandidate"}
    Copy-Item -LiteralPath $taskCandidate -Destination $taskTarget -Force
}
# Interactive editor requested by the user; only the two already adapted facades
# are excluded from API rewriting. Burst compilation remains enabled.
Start-Process -FilePath $taskEditor -ArgumentList @('-projectPath',('"'+$taskProject+'"'),'-disable-assembly-updater','Unity.Mathematics.dll','Unity.Burst.dll') -WindowStyle Normal
