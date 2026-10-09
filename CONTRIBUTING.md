# Contributing

Start with [Building](docs/BUILDING.md). Keep source, version metadata, manifest,
changelog and documentation consistent. Keep the executable a single portable
file and preserve PLASHSMA credit.

For a bug fix, add a regression check that exercises the reported behavior and
run the build script. Use disposable local Office documents for live tests and
record the Office/Windows versions and architecture tested.

Preserve normal-user operation, explicit startup, bounded private logs and the
folder restrictions. Do not add credential collection, silent cloud uploads,
keyboard logging or security-setting workarounds.

Do not commit runtime settings/logs, private documents, certificates, passwords,
tokens, compiled scratch tools or older binaries. Attach release binaries to
GitHub Releases rather than tracking them in Git.

No open-source license has been selected yet; discuss permissions with the
maintainer before redistributing or incorporating code under an assumed license.
