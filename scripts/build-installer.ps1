param([Parameter(Mandatory=$true)][string]$CompilerPath)
$ErrorActionPreference='Stop'
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
& (Join-Path $PSScriptRoot 'build.ps1')
if ($LASTEXITCODE -ne 0) {throw 'Application build failed.'}
& $CompilerPath (Join-Path $repoRoot 'installer\LocalSave.iss')
if ($LASTEXITCODE -ne 0) {throw 'Installer compilation failed.'}
$setup=Join-Path $repoRoot 'dist\LocalSave-Setup-2.7.0.exe'
$hash=Get-FileHash -LiteralPath $setup -Algorithm SHA256
($hash.Hash.ToLower()+'  '+[IO.Path]::GetFileName($setup)) | Set-Content -LiteralPath ([IO.Path]::ChangeExtension($setup,'.sha256')) -Encoding ascii
Write-Output "Installer: $setup"
