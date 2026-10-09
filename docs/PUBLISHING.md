# Publish LocalSave on GitHub: step-by-step

This guide is for the project maintainer. End users should use the
[User guide](USER_GUIDE.md).

## 1. Create the repository

1. Sign into GitHub and choose **New repository**.
2. Set the owner to your account, name it `localsave`, and enter a description.
3. Choose **Public** if the source and downloads should be visible to everyone.
4. When importing an existing local Git repository, leave the initial README,
   license and `.gitignore` options unchecked to avoid an extra remote commit.
5. Create the repository. Keep its HTTPS URL.

GitHub's official guide: [Adding locally hosted code](https://docs.github.com/en/migrations/importing-source-code/using-the-command-line-to-import-source-code/adding-locally-hosted-code-to-github).

## 2. Prepare and upload source

Use only the repository folder. Do not upload runtime logs, settings, private
documents, credentials, scratch tests, earlier EXEs or the entire Codex workspace.

If the repository has not already been initialized:

```powershell
git init -b main
git add .
git commit -m "Publish LocalSave source and documentation"
git remote add origin https://github.com/plashsma/localsave.git
git push -u origin main
```

Authenticate using GitHub's supported browser/credential-manager flow. Do not
paste a token into a command, remote URL, source file or issue.

If GitHub already created an initial README commit, clone the repository first,
copy in the prepared files, commit and push. Do not force-push over remote work.

## 3. Check the repository

1. Confirm the README renders with its logo, screenshot and guide links.
2. Open **Actions** and confirm the Windows build completed successfully.
3. Review `git status` and the tracked files for unintended personal data.
4. Confirm the root `LICENSE` contains the MIT License and PLASHSMA copyright
   notice. Include that notice with redistributed software and release assets.

## 4. Build and test the release

1. Follow [Building](BUILDING.md).
2. Run the live disposable-file checks described there.
3. Update the source version, manifest, changelog, documentation and screenshot
   together when behavior or release version changes.
4. Generate the checksum after the final build (and after signing, if signed):

   ```powershell
   $hash = Get-FileHash -LiteralPath .\dist\LocalSave-2.2.exe -Algorithm SHA256
   ($hash.Hash.ToLower() + '  LocalSave-2.2.exe') |
       Set-Content -LiteralPath .\dist\LocalSave-2.2.sha256 -Encoding ascii
   ```

## 5. Create the GitHub release

1. Open the repository's **Releases** page and choose **Draft a new release**.
2. Create tag **v2.2.0** from the reviewed source commit on `main`.
3. Title it **LocalSave 2.2.0 — Preview**.
4. Paste the release notes from [RELEASE_NOTES.md](../RELEASE_NOTES.md).
5. Attach **LocalSave-2.2.exe**, its checksum, and **LICENSE.txt** containing
   the complete MIT License and copyright notice.
6. Mark it as a **pre-release** while live Excel verification and publisher signing
   remain incomplete, then publish.
7. Verify the tag points to the source version used for the attached EXE.

Official guide: [Managing releases](https://docs.github.com/en/repositories/releasing-projects-on-github/managing-releases-in-a-repository).

## 6. Verify the download

1. Open the published release and confirm both assets appear.
2. Download the EXE to a fresh folder and compare its checksum with the original.
3. Confirm its Windows file version and PLASHSMA credit.
4. Test it on a separate Windows PC with Office before claiming broad compatibility.

## 7. Improve distribution trust

Publishing to GitHub does not sign an EXE. Obtain trusted publisher signing,
timestamp final binaries, and keep the publisher identity consistent. Even signed
releases can lack SmartScreen reputation. A checksum detects a file change but
does not establish publisher identity. See [SECURITY.md](../SECURITY.md).
