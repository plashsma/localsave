param([string]$OutputDirectory = '', [string]$OutputName = 'LocalSave-2.2.exe')
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$source = Join-Path $repoRoot 'src'
$tests = Join-Path $repoRoot 'tests'
$artifacts = Join-Path $repoRoot 'artifacts'
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) { $OutputDirectory = Join-Path $repoRoot 'dist' }
$output = [IO.Path]::GetFullPath($OutputDirectory)
if ([IO.Path]::GetFileName($OutputName) -ne $OutputName -or $OutputName -notmatch '\.exe$') { throw 'OutputName must be an EXE filename, without a path.' }
$compiler = Join-Path $env:SystemRoot 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { $compiler = Join-Path $env:SystemRoot 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (-not (Test-Path -LiteralPath $compiler)) { throw 'The .NET Framework C# compiler is not available on this Windows PC.' }
New-Item -ItemType Directory -Force -Path $artifacts,$output | Out-Null
$brand = Join-Path $source 'Brand.cs'
$core = Join-Path $source 'Core.cs'
$excel = Join-Path $source 'ExcelConnection.cs'
$desktop = Join-Path $source 'Desktop.cs'
$iconGenerator = Join-Path $artifacts 'MakeIcon.exe'
$icon = Join-Path $artifacts 'LocalSave.ico'
& $compiler /nologo /target:exe /reference:System.Drawing.dll "/out:$iconGenerator" $brand (Join-Path $PSScriptRoot 'MakeIcon.cs')
if ($LASTEXITCODE -ne 0) { throw 'Icon generator compilation failed.' }
& $iconGenerator $icon
if ($LASTEXITCODE -ne 0) { throw 'Icon generation failed.' }
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:Accessibility.dll "/win32manifest:$source\app.manifest" "/win32icon:$icon" "/out:$output\$OutputName" $brand $core $excel $desktop
if ($LASTEXITCODE -ne 0) { throw 'Application compilation failed.' }
$behaviorTests = Join-Path $artifacts 'Tests.exe'
& $compiler /nologo /target:exe /main:Tests /reference:System.Windows.Forms.dll /reference:System.Drawing.dll "/out:$behaviorTests" $brand $core $excel (Join-Path $tests 'Tests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Behavior test compilation failed.' }
& $behaviorTests (Join-Path $artifacts 'test-data')
if ($LASTEXITCODE -ne 0) { throw 'Behavior tests failed.' }
$uiTests = Join-Path $artifacts 'UITests.exe'
& $compiler /nologo /target:exe /main:UITests /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:Accessibility.dll "/out:$uiTests" $brand $core $excel $desktop (Join-Path $tests 'UITests.cs')
if ($LASTEXITCODE -ne 0) { throw 'UI test compilation failed.' }
& $uiTests
if ($LASTEXITCODE -ne 0) { throw 'UI tests failed.' }
$exePath = Join-Path $output $OutputName
$checksumPath = [IO.Path]::ChangeExtension($exePath,'.sha256')
$hash = Get-FileHash -LiteralPath $exePath -Algorithm SHA256
($hash.Hash.ToLower() + '  ' + $OutputName) | Set-Content -LiteralPath $checksumPath -Encoding ascii
Write-Output "Built: $exePath"
