# Install LocalSave 2.7.0

1. Exit an older LocalSave using its tray menu.
2. Run **LocalSave-Setup-2.7.0.exe**.
3. Accept the LocalSave Personal Use License and choose the installation folder.
4. Optionally choose a desktop shortcut. Start menu shortcuts are included.
5. Setup checks for .NET Framework 4.8 or newer. If missing, installation stops
   and offers to open Microsoft's download page. Install the runtime separately,
   then rerun Setup. The runtime is not bundled or downloaded automatically.
   With the runtime present, setup works offline and installs for your user.
6. Finish Setup and launch LocalSave. Your existing applied preferences load
   from `%LOCALAPPDATA%\LocalOfficeAutoSave\settings.xml`.

The default program folder is `%LOCALAPPDATA%\Programs\LocalSave`. The app is
listed in Windows **Settings → Apps → Installed apps**. It has a stable
`LocalSave.exe` filename and the same installer identity across future versions.
Already-enabled startup is redirected to the installed EXE; installation does
not enable startup for someone who opted out.

## Updating

**Do not uninstall first.** Exit the running app, then run the newer Setup EXE. Setup reuses the previous
installation folder and preserves AppData preferences. Do not use Reset
preferences as part of an update. Old portable EXEs are not deleted by Setup.

## Uninstalling

Exit LocalSave, then uninstall through Windows Settings or **Start → LocalSave →
Uninstall LocalSave**. Setup removes installed files, shortcuts, its Windows
entry and the startup entry. It keeps AppData preferences for reinstalling and
leaves documents alone. Microsoft .NET is shared with other software and is
not removed. To erase preferences too, use **Privacy & help → Reset preferences**
before uninstalling.

## Requirements and limits

- Windows 10/11 and installed supported desktop Office/Adobe apps. Their installers,
  drivers and activation are separate; LocalSave needs no device drivers.
- The compact Setup EXE contains LocalSave, its license and the uninstaller.
  It contains no .NET runtime or device drivers.
- The LocalSave app and Setup EXE are unsigned previews. Bundling Microsoft's
  signed runtime does not give LocalSave a publisher signature.
- 109 application checks, 22 UI checks and 10 isolated installer checks passed.
  Tests installed, upgraded and uninstalled using a separate test-only identity
  and mutex. Tests include a 2.6.0-to-2.7.0 upgrade without uninstalling and a
  simulated missing-runtime check. Live Office/Adobe testing remains pending.

## Build the installer

Use the official Inno Setup 6 compiler. The build runs the application tests
before compiling the small installer; no runtime download is needed:

```powershell
.\scripts\build-installer.ps1 -CompilerPath 'C:\Tools\Inno Setup 6\ISCC.exe'
```

The setup script is `installer/LocalSave.iss`. The output is
`dist/LocalSave-Setup-2.7.0.exe` with a SHA-256 file. New material uses the LocalSave Personal Use License. Earlier MIT permissions remain. The compiler
is a build tool and is not installed on end-user devices.

References: [Inno Setup](https://jrsoftware.org/isdl.php),
[Microsoft offline runtime](https://support.microsoft.com/en-us/servicing/dotnetframework/2019/10/microsoft-net-framework-4-8-offline-installer-for-windows),
[Microsoft runtime deployment](https://learn.microsoft.com/en-us/dotnet/framework/deployment/deployment-guide-for-developers).
