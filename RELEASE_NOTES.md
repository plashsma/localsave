# LocalSave 2.2.0 — Preview

Created by **PLASHSMA**.

## Download

Download **LocalSave-2.2.exe** from Assets. It is a single portable EXE; the source
ZIP is optional and intended for developers. Requires Windows, .NET Framework
4.8+, and installed desktop Word, Excel or PowerPoint.

## What's included

- 1–3600 second interval saving and a 250 ms change-check mode.
- Per-app controls, optional folder restriction, tray controls and uninstall.
- Improved Excel discovery and clear skipped-file/busy status.
- Fixed retry/log flooding and added operation-specific diagnostics.
- Brighter glass-style interface and PLASHSMA credit.

## Start

1. Exit older helpers.
2. Put the EXE in a permanent folder and run it.
3. Save each new document manually once.
4. Select apps/mode in **Save settings** and click **Apply settings**.
5. Commit Excel cell edits with **Enter**; test with disposable documents first.

If startup was enabled for an earlier EXE, applying settings updates its path.
See the repository's User guide for the complete steps.

## Preview limitations

- 38 behavior/settings checks and 7 UI checks passed locally; Office tests use
  test doubles. Complete live Office and Excel-window verification remains pending.
- This is periodic local saving, not cloud AutoSave or version history.
- Office may reject calls while busy. New files require an initial manual save.
- The EXE is unsigned. Windows/organization policies may block it; do not disable
  protections. The SHA-256 asset is an integrity check, not a publisher signature.

## License

LocalSave source code and the 2.2.0 release executable are licensed under the
[MIT License](https://github.com/plashsma/localsave/blob/main/LICENSE).
Copyright (c) 2026 PLASHSMA. The release includes LICENSE.txt with the full terms.
Preserve the copyright and license notice when redistributing copies or
substantial portions of the software.
