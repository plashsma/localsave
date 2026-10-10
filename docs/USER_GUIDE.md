# LocalSave: step-by-step user guide

## 1. Download and install

1. Open [the v2.7.0 release](https://github.com/plashsma/localsave/releases/tag/v2.7.0).
2. Expand **Assets** and download **LocalSave-Setup-2.7.0.exe**. Source ZIPs are for developers.
3. Run Setup, read the personal-use license, and complete installation.
4. Launch LocalSave from the Start menu as your normal Windows user.
5. If .NET Framework 4.8+ is missing, Setup offers the official Microsoft page;
   install it and rerun Setup. No runtime or drivers are bundled.

See [Installation](INSTALLATION.md) for requirements and update details.
The release includes a SHA-256 checksum for detecting an unexpected file change.
It does not replace a publisher signature. To compare the downloaded EXE:

```powershell
Get-FileHash -LiteralPath .\LocalSave-Setup-2.7.0.exe -Algorithm SHA256
```

Compare the result to `LocalSave-Setup-2.7.0.sha256` in the same release.

## 2. Prepare your documents

1. Open Word, Excel or PowerPoint on the same PC.
2. Create a disposable test file.
3. Save it manually to a writable local folder with a name and normal Office format.
4. Repeat for each Office app you intend to use.

LocalSave needs that first manual save to know where the file belongs. It skips
untitled documents, read-only files and locations outside its allowed scope.

## 3. Choose your saving mode

Open **Save settings**.

- **At an interval:** choose a number from **1** to **3600** seconds. It checks
  after the previous scan has finished, so a slow save can extend the interval.
- **After changes:** checks about every **250 milliseconds** while Office responds.
  A check is not necessarily a save; unchanged files are left alone.

Select **Word**, **Excel**, and/or **PowerPoint**, then click **Apply settings**.
Selecting no apps means no documents will be saved.

Photoshop and Illustrator are also available as opt-in choices.
Use existing local PSD/PSB and AI projects; see the [Adobe guide](ADOBE.md).

### Separate intervals

1. Select **At an interval** and choose the shared default.
2. Click **App intervals...** in the Applications card.
3. Enter seconds for each app, for example Word 1, Excel 2, PowerPoint 5,
   Photoshop 30 and Illustrator 15. **0** uses the shared default.
4. Click **Use intervals**, then **Apply settings**. Cancel discards dialog edits.

Intervals range from 1 to 3600 seconds and apply only in At an interval mode.
After changes checks each app about every 250 ms; your intervals are retained
for switching back. Upgrading old settings preserves the shared default.
Each app has its own worker and schedule: a blocked Photoshop call does not
hold up Excel or Illustrator. The interval starts after that app's previous
scan finishes; a busy app may defer saving. Save now requests all enabled apps
independently, including while paused. A call already in progress cannot be
cancelled by Pause. Exit waits for all current calls to finish.

### Save-health warnings

Warnings are enabled by default. When LocalSave detects an eligible file with
unsaved changes but cannot confirm saving, it highlights the affected app in
amber and shows a tray warning after **120 seconds**. It tracks each file
separately, so saving another file cannot hide a pending failure.
The tray icon also becomes an **amber warning triangle with an
exclamation mark**, with a “saving needs attention” tooltip. It stays visible
while the warning remains active, even after the notification disappears.
Recovery or pausing restores the normal icon. Double-click the tray icon to
open LocalSave and check the affected app.

1. Open **Save settings → Save warnings...**.
2. Enable or disable warnings and choose a delay from **15–3600 seconds**.
3. Click **Use warnings**, then **Apply settings**.

For interval mode, the effective delay is the larger of your warning delay
and that app's interval plus 30 seconds. After changes uses the chosen delay.
The delay starts when unsaved changes are first observed, including before a
blocking Save call. Pausing suppresses warnings; resuming can warn immediately
if changes have remained pending. Save now remains available while paused.

There is one notification per app while its pending changes remain unresolved;
notifications are grouped and at least a minute apart. Status stays visible
after the notification disappears. Windows notification settings may hide tray
balloons. Open LocalSave to inspect the highlighted app, finish edits/dialogs,
or save manually. Successful confirmation or observing a manual save clears
the warning. A complete scan removes closed or newly excluded files.

These warnings require detected unsaved changes. They cannot diagnose documents
the app refuses to expose, excluded/new files, or uncommitted Excel cell edits.
File paths are kept only in memory for tracking and never written to logs.

### Optional: restrict the folder

1. Enable **Only save files inside this folder and its subfolders**.
2. Click **Browse**, choose a normal local folder, then **Apply settings**.
3. Open a file inside that folder to verify the restriction.

Files outside the chosen folder are skipped. Junctions, symbolic links and other
redirected paths are rejected by this preview.

## 4. Verify saving

1. Make a small edit to the disposable file.
2. In Excel, finish the cell edit with **Enter** or **Tab**.
3. Wait for the configured interval, or click **Save now**.
4. Check **Overview** for a successful save and per-app status.
5. Close and reopen the file to confirm the edit persisted.
6. Repeat this test in all selected apps before using important documents.

If saving does not occur, see [Troubleshooting](TROUBLESHOOTING.md). A skipped
file or busy app should now have a reason in the status or activity log.

## 5. Pause, hide and exit

- **Pause saving** stops automatic checks. **Save now** still works while paused.
- **Resume saving** restores automatic checks.
- **Hide to tray**, or the window's close button, keeps the helper running.
- Find the LocalSave icon under the notification area's hidden-icons arrow.
- Right-click the icon for controls, activity log or **Exit**.
- **Exit** stops the helper. It does not close Office.

## 6. Start with Windows

1. Install LocalSave using Setup.
2. Open **Save settings**.
3. Enable **Start LocalSave when I sign into Windows**.
4. Click **Apply settings**.

This creates a startup entry only for your Windows user. The app starts in the
tray. Starting with 2.5.2, if you move the EXE or install a new version, run it
once from its permanent location. It automatically updates an already-enabled
startup entry. It never turns startup on if you previously disabled it.

## 7. Update to a newer version

1. Exit the old helper using its tray menu.
2. Download the new Setup from Releases and run it; no uninstall is needed.
3. Your applied preferences load automatically from
   `%LOCALAPPDATA%\LocalOfficeAutoSave\settings.xml`, regardless of the EXE's
   name, version or location. Apps, intervals, mode, warning options and folder
   restriction remain unchanged for the same Windows user.
4. An enabled Windows startup entry automatically follows the new EXE on its
   first launch (2.5.2+). Click **Apply settings** only if you change preferences
   or the app reports that startup updating failed.
5. Repeat the disposable-file checks.

## 8. Uninstall or reset

1. Exit LocalSave from its tray menu and finish any pending save dialogs.
2. Open **Windows Settings → Apps → Installed apps → LocalSave → Uninstall**.
3. The uninstaller removes installed files, shortcuts and the startup entry.

Preferences and logs stay in AppData for future reinstallation. To clear them,
use **Privacy & help → Reset preferences** and confirm before uninstalling.
Your documents are not removed.
## Privacy

Settings and logs live in `%LOCALAPPDATA%\LocalOfficeAutoSave`.
Click **Apply settings** to persist changes. Merely replacing or deleting the
EXE does not remove these preferences. **Reset preferences** deliberately clears
them. Each Windows user has separate settings; another PC starts with defaults.
New logs contain timestamps with timezone offsets, app names, counts, states,
operations and error codes; document names and contents are omitted.
The helper makes no network requests. Office can still sync files through its
own enabled services and can run existing document save-event handlers.

