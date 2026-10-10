param([string]$SetupPath,[string]$PreviousSetupPath,[string]$MissingRuntimeSetupPath)
$ErrorActionPreference='Stop'
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (!$SetupPath) {$SetupPath=Join-Path $repoRoot 'artifacts\LocalSave-Smoke-Setup.exe'}
$installDir=Join-Path $repoRoot ('artifacts\installer-smoke-'+[Guid]::NewGuid().ToString('N'))
$registryPath='HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\LocalSave-Installer-Smoke-Test_is1'
if(Test-Path -LiteralPath $registryPath) {throw 'Another smoke installation exists; do not overwrite it.'}
$preferences=Join-Path $env:LOCALAPPDATA 'LocalOfficeAutoSave\settings.xml'
$beforeHash=if(Test-Path -LiteralPath $preferences) {(Get-FileHash -LiteralPath $preferences).Hash} else {''}
$count=0
function Check($pass,$name) {if(!$pass) {throw $name}; $script:count++; Write-Output "PASS installer: $name"}
function Install {
    param([string]$sourceSetup=$SetupPath,[bool]$reuseDirectory=$false)
    $arguments=@('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/NOICONS','/TASKS=""',('/LOG="'+(Join-Path $repoRoot 'artifacts\installer-smoke.log')+'"'))
    if(!$reuseDirectory) {$arguments+=('/DIR="'+$installDir+'"')}
    $process=Start-Process -FilePath $sourceSetup -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
    if($process.ExitCode -ne 0) {throw "Smoke installation failed: $($process.ExitCode)"}
}
$installed=$false
try {
    $initialVersion=if($PreviousSetupPath) {'2.6.0'} else {'2.7.0'}
    if($PreviousSetupPath) {Install $PreviousSetupPath} else {Install}; $installed=$true
    $exe=Join-Path $installDir 'LocalSave.exe';$uninstaller=Join-Path $installDir 'unins000.exe'
    Check ((Test-Path -LiteralPath $exe) -and (Test-Path -LiteralPath $uninstaller)) 'installs app and standard uninstaller'
    Check ((Get-ItemProperty -LiteralPath $registryPath).DisplayVersion -eq $initialVersion) 'registers installed-app version'
    Check ((Get-FileHash -LiteralPath $exe).Hash -eq (Get-FileHash -LiteralPath (Join-Path $repoRoot ('dist\LocalSave-'+$initialVersion+'.exe'))).Hash) 'installed application matches the built EXE'
    $sentinel=Join-Path $installDir 'user-file.txt';[IO.File]::WriteAllText($sentinel,'preserve on update and uninstall')
    Install $SetupPath $true
    Check ([IO.File]::ReadAllText($sentinel) -eq 'preserve on update and uninstall') 'upgrade preserves unknown user files'
    Check ((Get-ItemProperty -LiteralPath $registryPath).DisplayVersion -eq '2.7.0' -and (Get-FileHash -LiteralPath $exe).Hash -eq (Get-FileHash -LiteralPath (Join-Path $repoRoot 'dist\LocalSave-2.7.0.exe')).Hash) 'new setup upgrades in place without uninstalling and reuses the previous folder'
    $terms=[IO.File]::ReadAllText((Join-Path $installDir 'LICENSE.txt'))
    Check ($terms.Contains('LocalSave Personal Use License 1.0') -and $terms.Contains('Prior MIT notice') -and $terms.Contains('Permission is hereby granted')) 'installed license includes personal restrictions and preserves prior MIT notice'
    $process=Start-Process -FilePath $uninstaller -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART') -WindowStyle Hidden -Wait -PassThru
    if($process.ExitCode -ne 0) {throw "Smoke uninstall failed: $($process.ExitCode)"};$installed=$false
    Check (!(Test-Path -LiteralPath $exe) -and !(Test-Path -LiteralPath $registryPath)) 'uninstall removes application and installed-app entry'
    Check (Test-Path -LiteralPath $sentinel) 'uninstall leaves unknown user files alone'
    $afterHash=if(Test-Path -LiteralPath $preferences) {(Get-FileHash -LiteralPath $preferences).Hash} else {''}
    Check ($afterHash -eq $beforeHash) 'AppData preferences remain unchanged'
    if($MissingRuntimeSetupPath) {
        $process=Start-Process -FilePath $MissingRuntimeSetupPath -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/NOICONS',('/DIR="'+$installDir+'"')) -WindowStyle Hidden -Wait -PassThru
        Check ($process.ExitCode -ne 0 -and !(Test-Path -LiteralPath $exe) -and !(Test-Path -LiteralPath $registryPath)) 'missing .NET blocks installation without changing installed files or entries'
    }
} finally {
    if($installed -and (Test-Path -LiteralPath (Join-Path $installDir 'unins000.exe'))) {
        Start-Process -FilePath (Join-Path $installDir 'unins000.exe') -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART') -WindowStyle Hidden -Wait
    }
    $resolved=[IO.Path]::GetFullPath($installDir)
    if(!$resolved.StartsWith([IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts'))+'\',[StringComparison]::OrdinalIgnoreCase)) {throw 'Unexpected test cleanup path'}
    if(Test-Path -LiteralPath (Join-Path $resolved 'user-file.txt')) {Remove-Item -LiteralPath (Join-Path $resolved 'user-file.txt')}
    if((Test-Path -LiteralPath $resolved) -and @(Get-ChildItem -LiteralPath $resolved -Force).Count -eq 0) {Remove-Item -LiteralPath $resolved}
}
Write-Output "All $count installer checks passed."
