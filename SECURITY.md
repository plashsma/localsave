# Security and privacy

LocalSave 2.2 is an **unsigned preview**. It is not security-certified, and its
publication on GitHub is not a guarantee of safety or Windows reputation.

## Implemented measures

- Normal-user execution (`asInvoker`), without requesting administrator rights.
- No telemetry, document uploads, networking code or keyboard hooks in the helper.
- Explicit per-user startup and optional folder restriction.
- Rejection of nonlocal and redirected paths for document saves and app storage.
- Bounded, validated XML settings with DTDs/external entities disabled.
- Atomic settings replacement and bounded activity logs without document paths/content.
- Removal of only known app files during uninstall; no recursive user-folder deletion.
- No changes to Office macro settings, Windows security settings or Office alerts.

Office still controls its own saving, dialogs, macros, event handlers and sync
services. These measures do not create a boundary against another malicious
process running as the same Windows user. No file durability, recovery or
version-history guarantee is provided by the helper.

## Distribution

Do not disable endpoint protections to run or distribute the unsigned preview.
Use trusted publisher signing or an appropriate managed distribution process
before making broad deployment claims. Signing alone does not guarantee immediate
SmartScreen reputation. Microsoft explains the current behavior in
[SmartScreen reputation for app developers](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/smartscreen-reputation).

## Reporting

For non-sensitive problems, open a GitHub issue with the version and minimal
steps. Do not include private Office files, credentials or personal data.
For a sensitive vulnerability, use GitHub's private vulnerability reporting
option if the maintainer has enabled it. This repository does not invent an
unverified security contact or promise private reporting is already enabled.
