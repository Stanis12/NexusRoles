# Publishing NexusRoles

This repository is the mod's standalone source repository. The Thunderstore
upload archive is generated under `release/` and is intentionally ignored by
Git; attach that archive to a GitHub Release and upload the same file to
Thunderstore.

## 1. Check the release

Before publishing, finish the open items in [`PUBLICATION_CHECKLIST.md`](PUBLICATION_CHECKLIST.md).
In particular, confirm permission to redistribute the inherited code and
verify the attribution/source for replacement textures. Run the game and test
the mod in multiplayer.

Build and package from PowerShell at the repository root:

```powershell
dotnet restore src/NuageRoles.Plugin/NuageRoles.Plugin.csproj --locked-mode
dotnet build src/NuageRoles.Plugin/NuageRoles.Plugin.csproj -c Release --no-restore -p:CopyFilesToPluginOutputDirectoryOnBuild=false
./tools/package.ps1 -SkipBuild
```

Or run `./tools/package.ps1` to do all three steps. It creates
`release/NexusRoles-<version>.zip`. Do not upload a ZIP of the whole source
folder; Thunderstore requires the package files at the archive root, with the
plugin DLL and bundle under `plugins/`.

## 2. Push the source to GitHub with Git

Open the GitHub repository that should host this standalone mod. If you have not
created it yet, create an empty repository named `NexusRoles` under the account
or organization that will own the project. Do not initialize it with a README,
license, or `.gitignore`, because those files already exist locally. Before the
first commit, set `website_url` in `manifest.json` to this repository's URL and
regenerate the Thunderstore archive after changing it.

From PowerShell in this repository folder:

```powershell
git status
git add .
git status
git commit -m "Prepare NexusRoles 4.1.0 for release"
git branch -M main
git remote add origin https://github.com/OWNER/NexusRoles.git
git push -u origin main
```

Replace `OWNER` with the GitHub account or organization name. `git status` after
`git add .` is a review step: confirm `local-only/`, build outputs, and the
Thunderstore ZIP are absent before committing. The ignored `local-only/` folder
contains the old user-supplied image and historical patch; neither is part of
the public repository.

If `origin` is already configured, update it instead of adding it again:

```powershell
git remote -v
git remote set-url origin https://github.com/OWNER/NexusRoles.git
```

## 3. Create a GitHub Release

Tag the version already present in `manifest.json` and `CHANGELOG.md`:

```powershell
git tag v4.1.0
git push origin v4.1.0
```

On GitHub, open the repository's **Releases** page, choose **Draft a new
release**, select tag `v4.1.0`, copy the `4.1.0` entry from `CHANGELOG.md`,
attach `release/NexusRoles-4.1.0.zip`, review the page, then publish the release.

For later updates, increment `version_number` in `manifest.json`, update the
top entry in `CHANGELOG.md`, run the packaging script, commit and push, then
create and push the matching `vX.Y.Z` tag. Never reuse a published tag.

## 4. Upload to Thunderstore

Use the Thunderstore package upload page with the intended team/owner account.
Upload the same `release/NexusRoles-4.1.0.zip` and verify the package name,
description, dependencies, icon, README rendering, and version before
submitting. Since the manifest package name is `NexusRoles`, this is a new
Thunderstore package identity; it will not update `NuageRoles` automatically.

