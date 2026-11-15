# 4.1.1

- Rebranded source namespaces, plugin implementation, Unity asset project, and asset bundle as NexusRoles.
- Updated repository metadata and project credits for Stanis12/NexusRoles.
- Kept the original BepInEx plugin GUID and configuration keys for upgrade compatibility.

# 4.1.0

- Renamed the displayed mod to NexusRoles while preserving its plugin GUID and existing configuration keys.
- Added Acrobat and Savior roles. Savior can sacrifice configured partial health or all current health to revive a nearby ally, with a 120-second cooldown.
- Added English, Russian, and Ukrainian in-game text selected automatically from the system language.

# 4.0.1

- Moved to the community-maintained `REPOModding` releases of MenuLib (2.6.2) and REPOConfig (1.2.7). The old `nickklmao` Thunderstore packages are deprecated. The plugin GUIDs did not change, and the MenuLib API used by NuageRoles is source-compatible.
- Engineer: the held item's battery is now also searched on parent objects, the Engineer requests Photon ownership of the item before charging it, and the charge is verified a moment later so a value overwritten by network sync is reported in the BepInEx log.

# 4.0.0

- NuageRoles is now a maintained fork of REPO Roles Classic.
- Updated the packaged assets with a recreated `NuageRoles_assets` bundle.
- Fixed Engineer item charging so stamina cost and stamina regeneration work correctly.
- Added new config options for role balancing, Mage spells, Engineer stamina behavior, role selection, enabled roles, and NuageRoles keybinds.

# 3.1.2

- Bound custom upgrade activation events on spawned runtime instances instead of the loaded prefab asset.
- Re-enabled custom upgrade toggles once purchased in the shop and marked upgrade items as physical shop items.

# 3.1.1

- Rewired role upgrades through REPOLib callbacks while preserving the Classic saved stat keys.
- Created a runtime placeholder for missing upgrade `Particle Effects` children so clean prefabs can still register in the shop.

# 3.1.0

- Integrated the successful REPO Roles CE fork as NuageRoles.
- Added the recreated `NuageRoles_assets` bundle and Unity source project.
- Removed the failed previous rewrite/runtime asset generation approach.
