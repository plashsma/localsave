# Changelog

## 2.7.0 — Personal-use licensing

- Introduce a custom Personal Use License for new material, free for individuals'
  personal noncommercial use. Exclude company/organizational, employment,
  freelance/client and other commercial use under the new grant.
- Preserve all prior MIT grants and their notice; the change is not retroactive.
- Update installer acceptance, installed LICENSE.txt, app license viewer and
  documentation. Use source-available terminology for restricted new releases.
- Keep compact packaging, upgrade identity and AppData preferences unchanged.
- 109 application, 22 UI and 10 installer checks pass. New license terms have
  not undergone legal review; live Office/Adobe acceptance testing remains pending.

## 2.6.1 — Compact installer and in-place updates

- Remove the bundled .NET runtime; retain detection and a Microsoft download
  page offer if .NET Framework 4.8+ is missing. No automatic runtime downloads.
- Explicitly reuse the previous install directory and the existing installer
  identity. Upgrade by running newer Setup, without uninstalling first.
- Preserve AppData preferences and existing startup opt-out during updates.
- 109 behavior, 22 UI and 9 isolated installer checks pass, including an actual
  version upgrade and simulated missing-runtime rejection.

## 2.6.0 — Windows installer (preview)

- Package a standard per-user installer with Start menu shortcuts, optional
  desktop shortcut and Windows installed-apps registration/uninstaller.
- Bundle the verified Microsoft .NET Framework 4.8 offline runtime; skip it
  when a newer runtime is installed and offer installation/elevation if needed.
- Use a stable installed EXE path, preserve AppData preferences through updates
  and uninstall, and relocate an already-enabled startup entry during setup.
- Block setup/uninstall while the running-app mutex exists; never kill Office
  or interrupt its save calls. Uninstall does not remove shared .NET or documents.
- Clarify that the in-app reset action clears preferences, not installed files.
- 109 behavior, 22 UI and 7 isolated installer checks pass. Missing-runtime and
  reboot acceptance tests remain pending, along with live Office/Adobe testing.

## 2.5.2 — Ready after updates (preview)

- Retain version-independent preferences in the Windows user's Local AppData
  folder, including app choices, intervals, mode, warnings and folder scope.
- Automatically point an already-enabled Windows startup entry at the current
  EXE on its first launch. Do not enable startup for opted-out users.
- Keep saving with restored preferences if updating startup fails, with a
  visible notice and Apply settings available to retry.
- Pass 109 behavior checks and 22 UI checks, including full preference reloads,
  startup relocation, opt-out preservation and failure handling.

## 2.5.1 — Warning tray icon (preview)

- Switch the tray icon to an amber warning triangle when detected unsaved
  changes exceed the save-health threshold. The tooltip also signals attention.
- Restore the normal icon on recovery or pause. Warning opt-out and disabled
  apps retain the existing save-health suppression behavior.
- Cache and safely dispose the warning icon, including repeated tray Exit cleanup.
- Pass 100 behavior checks and 22 UI checks; live Office/Adobe verification remains pending.

## 2.5.0 — Save-health warnings (preview)

- Track eligible unsaved files in memory until saving is confirmed. Other files'
  successful saves do not clear a pending failure.
- Highlight affected apps and show limited tray warnings after a configurable
  delay (default 120 seconds; 15–3600 seconds). Interval mode allows at least
  the app interval plus 30 seconds before warning.
- Warn even during a blocked native Save call using the independent UI timer.
- Suppress warnings while paused, disabled, or opted out. Notify once per app
  per unresolved episode, with a global 60-second notification cooldown.
- Clear pending health on confirmed/manual saves, complete scans of closed or
  newly excluded files, and disabled/closed applications. Never log file paths.
- Pass 100 behavior checks and 18 UI checks. Live app verification is pending.

## 2.4.0 — Per-app timing and independent workers (preview)

- Add an App intervals dialog with five overrides, 1–3600 seconds; 0 inherits
  the shared default. Existing settings preserve their previous timing.
- Give each app its own STA thread, schedule, COM discovery and retry state.
  A slow or blocked app no longer delays saving in the other apps.
- Aggregate confirmed save counts, last-save time and busy status across workers.
- Keep global Pause, Save now and uninstall working across all five workers.
- After changes continues checking each app about every 250 ms, independent of
  interval overrides. App intervals remain saved for switching modes.
- Pass 74 behavior checks and 14 UI checks, including a blocked Photoshop call
  while Excel and Illustrator save, actual interval scheduling and tray Exit.
- Live Office/Adobe acceptance testing remains pending.

## 2.3.1 — Tray exit fix

- Fix a null-reference error when Tray Exit disposes the main window and the
  application disposes it again. Cleanup now detaches and clears each resource.
- Ignore progress callbacks after shutdown starts and prevent repeated Exit.
- Replace the misleading startup-only error wording with a general error message.
- Verify repeated disposal, hidden-window tray Exit and final using-block cleanup.
- 56 core checks and 10 UI checks pass. Adobe support remains a preview.

## 2.3.0 — Adobe support preview

- Add opt-in Photoshop (existing PSD/PSB) and Illustrator (existing AI) saving
  through running Windows COM application instances.
- Show five app rows and persistent Adobe selections; old settings keep Adobe off.
- Skip cloud, unsaved, missing, read-only, export and restricted Adobe files.
- Confirm native saves and apply per-app retry backoff without naming artwork in logs.
- Embed the MIT license in the portable EXE and expose it in Privacy & help.
- 56 core checks and 7 UI checks pass; live Adobe compatibility is unverified.

## 2.2.0 — Preview

- Added Excel discovery through registered instances and native desktop windows.
- Added explicit skipped-file reasons and per-app last-save status.
- Recognized common Excel busy/edit-mode errors with prompt retries.
- Fixed failure counter resets that caused repeated error/log flooding.
- Continued other workbook saves when one workbook fails.
- Added operation names to error logs without document names/content.
- Strengthened the translucent glass interface and added PLASHSMA credit.
- Passed 38 behavior/settings checks and 7 UI checks locally.
- Live Excel/native-window acceptance testing and publisher signing remain incomplete.

## 2.1.0

- Replaced native button painting to fix rectangular artifacts.
- Improved navigation icons, keyboard interaction, accessibility and spacing.
- Stabilized the status heading and matched the Windows 11 frame where supported.

## 2.0.0

- Introduced the LocalSave interface and custom logo.
- Added background saving, app/folder controls, private logs and hardened settings.

## Earlier prototypes

- Portable script, then single EXE with pause, startup and uninstall.
- Added a 1-second minimum interval and 250 ms change-check mode.
- Corrected Word collection access to support both `Item` methods and properties.
