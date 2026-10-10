# Photoshop and Illustrator saving (2.5 preview)

LocalSave can attach to a running Windows desktop Photoshop or Illustrator
instance through its registered COM automation interface. It does not launch
either app, install a plug-in, record keystrokes, or require a cloud account
itself. Adobe's own activation requirements still apply.

## Enable and verify

1. Exit the previous LocalSave from its tray menu, then run LocalSave 2.7.0.
2. Open Photoshop or Illustrator normally, at the same privilege level.
3. Create a disposable test project and save it locally first: **PSD or PSB**
   for Photoshop, **AI** for Illustrator.
4. In **Save settings**, enable Photoshop and/or Illustrator and click
   **Apply settings**. These options are disabled by default, including after
   importing older settings.
5. Start with a 30-second interval for large artwork. Make a small edit, finish
   the active tool/text operation and any dialogs, then choose **Save now**.
6. Look for an increased save count and **Last saved** in that app's status.
   Close and reopen the disposable project to verify the change persisted.
7. Test the interval and After changes modes before using important artwork.

## Boundaries

- Only existing local PSD/PSB and AI files are eligible. JPEG, PNG, TIFF, PDF,
  EPS, cloud documents, untitled projects, read-only files, redirected paths,
  and files outside an enabled folder restriction are skipped.
- LocalSave invokes the document's normal **Save** method. It does not choose
  export settings, convert formats, use Save As, close documents, or suppress
  app dialogs. A save is counted only when the app reports the document saved.
- Only the running instance exposed through Windows COM is scanned. Some Adobe
  versions or sessions may not expose a running automation object. If the row
  says automation unavailable, save manually; LocalSave will keep checking.
- Photoshop/Illustrator tool operations or dialogs may defer a save. Busy calls
  retry with backoff. Each app has its own worker; a slow artwork save does not
  delay the other apps. Set timings in **App intervals...**. The one-second
  setting is a check interval, not a
  guarantee that a large project finishes saving every second.
- This overwrites the current project. It does not provide backup history.
  Keep Adobe recovery and independent backups enabled.
- The executable embeds the Personal Use License and preserves the prior MIT notice, available in **Privacy & help**.

## Validation status

The 2.7.0 preview passed 109 behavior/settings checks and 22 UI checks. Adobe tests use
test doubles with Adobe-style properties, including busy errors, save
confirmation, opt-in migration, format exclusions and read-only handling.
The layout was rendered and reviewed. Live saving has not yet been verified in
Photoshop or Illustrator, so compatibility remains provisional.

Automation references:
[Adobe Photoshop scripting](https://helpx.adobe.com/photoshop/using/scripting.html)
and [Adobe Illustrator scripting](https://helpx.adobe.com/illustrator/desktop/automate-visualize-data/automate-actions/install-and-run-scripts.html).
