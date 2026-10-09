# Troubleshooting

## Excel does not save

Follow these steps in order:

1. Save the workbook manually once to a normal local folder.
2. Verify **Excel** is selected in **Save settings**, and click **Apply settings**.
3. Check any folder restriction: the workbook must be inside the selected folder.
4. Edit a cell and press **Enter**. Text still being entered in a cell is not yet
   committed, and Excel may reject automation during edit mode.
5. Close any Excel dialog. Leave Excel and LocalSave running as the same normal user.
6. Click **Save now** and inspect the Excel row in **Overview**.
7. Open **Activity log** and copy the latest Excel status/error when reporting a bug.

Version 2.2 tries both the registered Excel application and the native object model
of Excel's desktop windows. This improves detection of multiple instances but
does not bypass process security or guarantee access to every instance.

## Understand the status

| Status or reason | Meaning / next step |
| --- | --- |
| Not running or not available | The helper could not connect to the app |
| No open documents | The connected instance has no documents in its normal collection |
| Save new file once first | Choose a name and location with a manual save |
| Read-only file | Save a writable copy manually |
| Outside selected folder | Change the folder restriction or move the document yourself |
| Nonlocal or redirected path | Use a normal local folder without a junction or placeholder redirection |
| Office AutoSave already on | The app's own AutoSave is handling the document |
| Excel is editing or busy | Commit the cell edit or close the dialog; the helper retries |
| Retry in … | A call failed; check the operation and error code in the log |

An eligible file is allowed to be saved. It does not necessarily contain changes.
A successful save count increases only when Office confirms the file as saved.

## PowerPoint repeatedly logs an error

Version 2.1 reset a retry counter before processing a file failure, which could
produce a log line on every scan. Version 2.2 fixes that counter and records the
operation, for example `read Path` or `save file`, alongside the HRESULT.

An HRESULT such as `0x80048240` alone does not uniquely identify the failed request.
Copy the complete new operation line, describe what you were doing, and note
whether the presentation had been saved manually.

## Word or another app is busy

Rejected calls can happen while typing, running a macro, calculating, or showing a
dialog. The helper retries and leaves the controls responsive. An Office call
that remains blocked can delay the other apps in that scan. Save manually when
necessary; never assume the configured interval proves every save succeeded.

## Startup opens an old version

Exit the old helper. Run the newest EXE, verify the startup checkbox and click
**Apply settings** to update the path. Keep the new EXE in that location.

## Windows blocks the EXE

The preview is unsigned. The repository and checksum are not a trusted publisher
signature. Ask the maintainer for a properly signed build, or follow your
organization's application policy. Do not disable SmartScreen, antivirus or
Smart App Control to work around a block.

## Report a bug

Use the repository's **Issues** section. Include:

- LocalSave version, Windows version, and Office version/bitness.
- Selected apps, mode and interval.
- Whether the file was saved once, writable, and inside any allowed folder.
- Whether committing the Excel cell edit changed the result.
- The app's status and a few recent relevant operation/error lines.

Do not upload private documents, account tokens, passwords or full personal paths.
