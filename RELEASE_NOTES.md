# LocalSave 2.8.0 — Update notifications preview

- Checks public GitHub releases 60 seconds after launch; with Windows startup enabled, this happens after sign-in.
- Automatic checks are daily; preview releases can be excluded, and checks can be disabled in Save settings → Updates....
- One update notice per version; manual checks are available in the tray and Privacy & help.
- Opens the official release page only on user action. No automatic download or installation.
- Trusted-certificate build support signs the app, installer and uninstaller with SHA-256 and RFC3161 timestamps, verifies signatures, and writes final checksums.

**This preview is unsigned:** no trusted certificate has been supplied. Signing support is prepared, not a Windows-warning bypass. Signing end-to-end cannot be verified until a trusted identity is available.

121 behavior/settings checks, 23 UI checks, 10 isolated installer checks and the required-signing guard passed. Live HTTPS release discovery passed. Live Office/Adobe acceptance testing remains pending.

Update checks send ordinary request metadata to GitHub, never documents or preferences. See [Update notifications](https://github.com/plashsma/localsave/blob/main/docs/UPDATES.md) and [Signing](https://github.com/plashsma/localsave/blob/main/docs/SIGNING.md). The personal-use terms and preserved prior MIT permissions continue to apply.



