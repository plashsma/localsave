# Build and test from source

## Requirements

- Windows with .NET Framework 4.8+ and its C# compiler.
- Windows PowerShell 5.1 or PowerShell 7.
- Git is optional if you download the source ZIP instead of cloning.

Office is not required for the automated test doubles. A live acceptance test does
require installed desktop Office and an interactive user session.

## Build

1. Clone or download the repository:

   ```powershell
   git clone https://github.com/plashsma/localsave.git
   Set-Location .\localsave
   ```

2. Review and run the build script:

   ```powershell
   .\scripts\build.ps1
   ```

3. The script generates the multi-size icon, compiles the AnyCPU EXE, runs the
   behavior tests and UI interaction/rendering tests, and writes:

   ```text
   dist/LocalSave-2.2.exe
   dist/LocalSave-2.2.sha256
   ```

4. Optional output directory/name:

   ```powershell
   .\scripts\build.ps1 -OutputDirectory .\dist -OutputName LocalSave.exe
   ```

The app uses the Windows/.NET libraries and does not restore external NuGet
packages. Do not change machine-wide execution policy if your environment blocks
the script; use the applicable organization-approved development workflow.

## Repository layout

```text
src/         Application, COM bridge, UI, logo drawing and manifest
tests/       Behavior/settings tests and UI interaction/rendering checks
scripts/     Build script and multi-size icon generator
assets/      README logo and screenshot
docs/        User, troubleshooting, build and publishing guides
.github/     Windows build workflow and bug-report template
```

## Validation limits

Version 2.2 passed **38 behavior/settings checks and 7 UI checks** locally.
Office-dependent tests use supplied test objects; they are not a live Office
certification. Junction integration checks are skipped when no test junction is
available. A complete live Office save test was not possible in the development
environment. CI shows the checks actually run in that environment.

Before a release, test disposable Word, Excel and PowerPoint files. Include Excel
edit mode, multiple Excel instances, failed saves, dialogs, folder restrictions,
pause/resume, startup, and uninstall. Record the versions/architectures tested.

Builds are not guaranteed byte-for-byte reproducible with the legacy compiler.
The checksum identifies the actual EXE attached to each release.
