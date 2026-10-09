# LocalSave: step-by-step user guide

## 1. Download and run

1. Open [the v2.2.0 release](https://github.com/plashsma/localsave/releases/tag/v2.2.0).
2. Expand **Assets** and download **LocalSave-2.2.exe**. The automatically generated
   **Source code** ZIP is for developers, not the runnable app.
3. Save the EXE in a permanent folder. No installer or companion scripts are needed.
4. Run it as your normal Windows user, at the same privilege level as Office.
5. If Windows or your organization's policy blocks the unsigned preview, contact
   the maintainer or your administrator. Do not turn off security protections.

The release includes a SHA-256 checksum for detecting an unexpected file change.
It does not replace a publisher signature. To compare the downloaded EXE:

```powershell
Get-FileHash -LiteralPath .\LocalSave-2.2.exe -Algorithm SHA256
```

Compare the result to `LocalSave-2.2.sha256` in the same release.

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

1. Put the EXE in the permanent location you intend to keep.
2. Open **Save settings**.
3. Enable **Start LocalSave when I sign into Windows**.
4. Click **Apply settings**.

This creates a startup entry only for your Windows user. The app starts in the
tray. If you move the EXE, open it from its new location and apply settings again
to update the startup path.

## 7. Update to a newer version

1. Exit the old helper using its tray menu.
2. Download the new EXE from Releases and run it.
3. Review your saved settings; they are stored separately from the executable.
4. Click **Apply settings** to update startup to the new EXE.
5. Repeat the disposable-file checks.

## 8. Uninstall

1. Open **Privacy & help**.
2. Click **Uninstall / reset** and confirm.
3. Finish any Office save dialogs if the helper is waiting for a current call.
4. The app removes its startup entry, settings and known logs, then closes.
5. Delete the portable EXE afterward.

Your Office documents are not removed. Old standalone script versions have their
own uninstall launcher; this app does not terminate unrelated PowerShell processes.

## Privacy

Settings and logs live in `%LOCALAPPDATA%\LocalOfficeAutoSave`.
New logs contain timestamps with timezone offsets, app names, counts, states,
operations and error codes; document names and contents are omitted.
The helper makes no network requests. Office can still sync files through its
own enabled services and can run existing document save-event handlers.
