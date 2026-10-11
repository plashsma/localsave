# Update notifications

Starting with 2.8.0, LocalSave checks public GitHub releases 60 seconds after
launch. Enable **Start LocalSave when I sign into Windows** for checks after
sign-in. This feature does not automatically enable startup or install a service.
Automatic attempts occur at most once per 24 hours. While running, an hourly
timer checks whether another attempt is due.

Automatic checks and preview releases are enabled by default in this preview.
Change them in **Save settings → Updates... → Use choices → Apply settings**.
Preferences and notification history stay in AppData across upgrades. A newer
version produces one tray notice per version. Save-health warnings take priority.
Windows may suppress tray notices; **Check for updates** is also available in
the tray menu and Privacy & help.

Manual checks work immediately, even if automatic checks are disabled. Offline
failures do not interrupt saving. The checker uses a separate background thread,
TLS 1.2 with normal certificate verification, timeouts, a bounded response and
a fixed public GitHub API endpoint. It ignores drafts, equal/older versions,
and optionally previews. Only this repository's release-tag links are accepted.

Click a notice to open the official release page. Download Setup, exit LocalSave
from the tray, and run Setup to upgrade in place. There are no automatic downloads,
installations, executable launches or elevated permissions from this checker.

GitHub receives your IP address, ordinary request metadata and a generic
LocalSave user-agent. No documents, file names, preferences, license keys or
personal identifier are sent. No GitHub login/token is required. Saving itself
remains local. Disable automatic checks to avoid these requests; manually
checking or opening a release page still contacts GitHub when requested.
