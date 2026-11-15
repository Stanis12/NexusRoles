# Publication checklist

## Prepared in this repository

- [x] Thunderstore manifest, README, changelog, and release package layout.
- [x] Credits and separate license/provenance notices.
- [x] Release build of `NexusRoles.Plugin.dll`.
- [x] Recreated asset bundle included in the package.
- [x] No old upstream closed asset bundle intentionally included.

## Must be resolved before public release

- [ ] Confirm the upstream inherited-code license and permission to redistribute
  this fork. See `THIRD_PARTY_NOTICES.md`.
- [ ] Confirm Navrim's CC BY 4.0 attribution and record the original source URL.
- [ ] Set `manifest.json` `website_url` to the final public GitHub repository URL.
- [x] Replace the unverified Tumblr image with an original geometric icon and
  exclude the Tumblr image from Git/release archives.
- [x] Include the Noto Sans OFL notice in the Thunderstore archive.
- [ ] Test the package in R.E.P.O. and capture BepInEx logs; the Engineer network
  charging change is not verified in-game yet.
- [ ] Create the GitHub remote and choose the repository visibility/owner.
- [ ] Confirm the Thunderstore team/owner name before uploading. The manifest
  package name is `NexusRoles`, so it will be a new package identity.

Do not mark this release as legally cleared until the upstream code terms and
Navrim asset attribution are confirmed. The local archive is a build artifact
for review, not evidence of those permissions.
