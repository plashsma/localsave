# Contributing

Start with [Building](docs/BUILDING.md). Keep source, version metadata, manifest,
changelog and documentation consistent. Keep the installer compact.
Preserve the copyright, personal-use license and applicable prior MIT notices.

For a bug fix, add a regression check that exercises the reported behavior and
run the build script. Use disposable local Office documents for live tests and
record the Office/Windows versions and architecture tested.

Preserve normal-user operation, explicit startup, bounded private logs and the
folder restrictions. Do not add credential collection, silent cloud uploads,
keyboard logging or security-setting workarounds.

Do not commit runtime settings/logs, private documents, certificates, passwords,
tokens, compiled scratch tools or older binaries. Attach release binaries to
GitHub Releases rather than tracking them in Git.

New releases use the [LocalSave Personal Use License](LICENSE). Contributions
submitted for inclusion must be offered under those terms and be yours to license.
Earlier MIT grants remain valid for the copies and material they cover.
