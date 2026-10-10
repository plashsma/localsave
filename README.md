<p align="center"><img src="assets/logo.svg" width="88" alt="LocalSave logo"></p>

# LocalSave

**Local automatic saving for desktop Office, Photoshop and Illustrator.**

**LocalSave 2.7.0 preview** includes a compact Windows installer (about 2.05 MB),
in-place updates, and preferences stored in AppData. No OneDrive account is
required. The installer checks for .NET Framework 4.8+ without bundling it.

Created by **PLASHSMA**, with a glass-style interface, separate app intervals,
independent saving workers, and save-health warnings with an amber tray icon.
Choose intervals from 1 second or check for changes about every 250 ms.
Photoshop and Illustrator support is opt-in; live Adobe saving remains unverified.

**[Download v2.7.0 preview](https://github.com/plashsma/localsave/releases/tag/v2.7.0)**
· **[Step-by-step user guide](docs/USER_GUIDE.md)**
· **[Install, update and uninstall](docs/INSTALLATION.md)**
· **[Troubleshooting](docs/TROUBLESHOOTING.md)**
![LocalSave interface](assets/overview.png)

## Get started

1. Download **LocalSave-Setup-2.7.0.exe** from the release's **Assets** section.
2. Run Setup, read the personal-use terms, and complete installation.
3. Save each new Office file manually once to choose its name and location.
4. In **Save settings**, select your apps and saving mode, then **Apply settings**.
5. Optionally enable **Start LocalSave when I sign into Windows** and apply again.

Closing the window hides it to the tray. Use **Exit** from the tray menu to stop
saving. To upgrade, exit LocalSave and run newer Setup; applied preferences stay
in AppData. Remove the program through **Windows Settings → Apps**. The in-app
**Reset preferences** action clears settings and known logs separately.
## Features

| Feature | Behavior |
| --- | --- |
| Interval mode | Checks changed local files every 1–3600 seconds |
| Per-app intervals | Set separate timings; 0 inherits the shared default |
| Independent workers | Each app owns its schedule and COM thread |
| Save-health warnings | Amber app status and limited tray notices for detected pending changes |
| After changes | Checks about every 250 ms; saves when Office is ready |
| App selection | Choose Word, Excel, PowerPoint, or a combination |
| Adobe apps | Opt-in native saves for existing local PSD/PSB and AI files |
| Folder restriction | Limit saves to a local folder and its subfolders |
| Excel discovery | Checks registered instances and Excel's desktop windows |
| Manual controls | Pause/resume, save now, activity viewer and tray menu |
| Local privacy | No networking, telemetry or keyboard recording in the helper |

## Requirements and limits

- Windows 10/11, .NET Framework 4.8+, and installed, activated desktop Office.
  Web Office, macOS and mobile devices are unsupported.
- A new file must be saved manually once. Read-only, network, web and redirected
  paths, Excel add-ins, and files already using native Office AutoSave are skipped.
- Excel cell edits must be committed with **Enter**, **Tab**, or by leaving the
  cell. This app cannot save every keystroke inside an uncommitted cell.
- Excel checks multiple desktop instances. Inaccessible instances may be missed;
  Word and PowerPoint use one registered instance per app.
- Busy Office apps or save dialogs can delay a cycle. One failed file does not
  prevent the helper from attempting other files it can access.
- This is periodic saving, not Microsoft's cloud AutoSave, collaboration or
  version history. Keep AutoRecover and backups enabled.

## Preview release status

Version **2.7.0 is a preview**. **109 behavior/settings checks, 22 UI checks,
and 10 isolated installer checks** passed locally. Automated app checks use test
doubles where desktop apps are required. Live Office/Adobe acceptance testing
remains pending; test disposable files before relying on the utility.
The EXE is **unsigned**. Downloading from GitHub does not give it a trusted
publisher signature or guarantee approval by Windows security software. See
[security and distribution](SECURITY.md). Do not disable protections to run it.

## Documentation

- [User guide: installation, modes, startup and uninstall](docs/USER_GUIDE.md)
- [Photoshop and Illustrator: setup, limits and live verification](docs/ADOBE.md)
- [Troubleshooting: Excel, busy apps and skipped files](docs/TROUBLESHOOTING.md)
- [Build and test from source](docs/BUILDING.md)
- [Publish a repository and release, step by step](docs/PUBLISHING.md)
- [Release history](CHANGELOG.md)
- [Contributing](CONTRIBUTING.md)

## License and credit

Created by **PLASHSMA**. New releases starting with **2.7.0** use the
[LocalSave Personal Use License](LICENSE): free for individuals' personal,
noncommercial use. Company and organizational use, employment work, paid client
work and other commercial use are not permitted under that license.
Personal coursework is allowed; institutional deployment is not.

This is source-available software with use restrictions, not an open-source
license. Earlier MIT-licensed copies and material retain their MIT permissions;
see [the preserved MIT notice](licenses/MIT-PREVIOUS.txt). The public 2.2.0
[MIT release](https://github.com/plashsma/localsave/releases/tag/v2.2.0) is not retroactively restricted. Copyright (c) 2026 PLASHSMA.

