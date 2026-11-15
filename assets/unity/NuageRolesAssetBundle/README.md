# NuageRoles Asset Bundle Rebuild Project

This Unity project recreates the asset bundle used by the NuageRoles fork without extracting or reusing the original closed assets.

Use this project to author clean replacement assets, validate the bundle contract, build the bundle file, and copy it beside the mod DLL for testing.

## Unity Version

Open this project with Unity `2022.3.62f3`.

The mod currently loads one bundle by filename from the plugin folder:

- `NuageRoles_assets`

Unity lowercases asset bundle names internally. The editor script builds with Unity's internal name, then copies the final file into `AssetBundles/Dist` with the exact casing above.

## Asset Contract

`NuageRoles_assets` must contain:

- `emptyBottle.png` as a `Texture2D`
- `filledBottle.png` as a `Texture2D`
- `font.ttf` as a legacy Unity `Font`

It must also contain both an `Item` ScriptableObject and a `GameObject` prefab with each of these exact asset names:

- `Mana Regeneration Upgrade`
- `Scout Cooldown Upgrade`

The mod loads the `Item` and the prefab by the same display name and type, so the `.asset` and `.prefab` files intentionally share the same object name.

## Source Art

Put clean replacement art here:

- `Assets/NuageRoles/SourceArt/Gui/manaEmpty.png`
- `Assets/NuageRoles/SourceArt/Gui/manaFull.png`
- `Assets/NuageRoles/SourceArt/Gui/font.ttf`
- `Assets/NuageRoles/SourceArt/Upgrades/manaUpgrade.png`
- `Assets/NuageRoles/SourceArt/Upgrades/scoutUpgrade.png`

The upgrade PNGs are not treated as icons. They are expected to be 512x512 unfolded box textures using the same panel layout as the original `Upgrade Pack` mesh. `Nuage Roles/2. Recreate Assets` maps those panels onto a generated replacement mesh based on the same structural UV layout, without reusing the original closed mesh asset.

`font.ttf` must be a redistributable font, for example a font under the SIL Open Font License. Do not use a system font unless its license allows redistribution with the mod.

`Nuage Roles/1. Setup Workspace` copies replacement PNGs from this mod's `assets/source/textures` folder when the target source-art files are missing.

In this repository, Unity-imported source PNG copies and generated bundle assets
are ignored. Keep canonical replacement PNGs under `mods/NuageRoles/assets/source/textures`,
keep redistributable font/license files under `Assets/NuageRoles/SourceArt/Gui`,
and recreate the Unity generated assets with `Nuage Roles/2. Recreate Assets`.

## Rebuild Steps

1. Open this Unity project.
2. Run `Nuage Roles/1. Setup Workspace`.
3. Add or replace any missing source art, especially `SourceArt/Gui/font.ttf`.
4. Run `Nuage Roles/2. Recreate Assets` for a clean regeneration from the source art and local authoring stubs.
5. Edit the generated `Item`, `Value`, and prefab assets by hand as needed.
6. Run `Nuage Roles/Advanced/2. Normalize Generated Upgrade Prefabs` after changing source upgrade textures or after editing the starter prefabs.
7. Run `Nuage Roles/3. Validate`.
8. Run `Nuage Roles/4. Build Bundle`.
9. For r2modman testing, package the mod from the main repo with a fresh zip. Do not patch an installed r2modman profile in place.

The built file lands in:

- `AssetBundles/Dist/NuageRoles_assets`

`Nuage Roles/2. Recreate Assets` deletes and rebuilds only `Assets/NuageRoles/Generated/NuageRoles_assets`, generated materials, and generated meshes. Use `Nuage Roles/Advanced/1. Create Missing Starter Assets` instead if you want to preserve existing generated assets and only fill in missing files.

`Nuage Roles/4. Build Bundle` does not recreate or normalize generated assets. If the prefab preview looks correct, build should preserve that exact prefab and only write files under `AssetBundles/Build` and `AssetBundles/Dist`.

Asset data belongs in this Unity project. The plugin should not patch `Value` prices, mesh scale, UVs, materials, or prefab child transforms at runtime. If an upgrade looks wrong or has the wrong price in-game, fix the generated `Item`, `Value`, prefab, mesh, or material here, then run `Validate` and `Build Bundle` again.

The top-level menu is intentionally just the normal workflow:

- `1. Setup Workspace`
- `2. Recreate Assets`
- `3. Validate`
- `4. Build Bundle`
- `5. Copy Bundle To Mod Assets Folder`

Everything else lives under `Nuage Roles/Advanced`.

## Manual Upgrade Assets

The upgrade `Item` assets are authored with clean local stubs in `Assets/NuageRoles/Runtime`.

Those stubs mirror the public serialized fields of the game's `Item`, `Value`, and related helper types. Unity builds them into the authoring project's default `Assembly-CSharp` assembly, so the bundle can resolve against the game's real `Assembly-CSharp` types at runtime.

`Advanced/1. Create Missing Starter Assets` creates placeholders only when files are missing. It does not overwrite existing GUI, upgrade `Item`, `Value`, or prefab assets.

Expected files:

- `Assets/NuageRoles/Generated/NuageRoles_assets/Items/Mana Regeneration Upgrade.asset`
- `Assets/NuageRoles/Generated/NuageRoles_assets/Items/Scout Cooldown Upgrade.asset`
- `Assets/NuageRoles/Generated/NuageRoles_assets/Values/Mana Regeneration Upgrade Value.asset`
- `Assets/NuageRoles/Generated/NuageRoles_assets/Values/Scout Cooldown Upgrade Value.asset`
- `Assets/NuageRoles/Generated/NuageRoles_assets/Prefabs/Mana Regeneration Upgrade.prefab`
- `Assets/NuageRoles/Generated/NuageRoles_assets/Prefabs/Scout Cooldown Upgrade.prefab`

You can also create replacement `Item` and `Value` assets from Unity's `Create/Nuage Roles` menu.

For each `Item` asset, keep these important fields:

- `itemName`: exact upgrade display name.
- `itemType`: `item_upgrade`.
- `itemVolume`: `upgrade`.
- `value`: matching `Value` asset.
- `Value.valueMin` / `Value.valueMax`: currently `1250`. R.E.P.O. multiplies this by the shop value multiplier (`4`) and divides by `1000`, so `1250` displays as about `$5K` before vanilla multiplayer discount and repeat-upgrade scaling.
- `maxAmount`: `1`.
- `maxAmountInShop`: `1`.
- `minPlayerCount`: `1`.
- `maxPurchase`: enabled.
- `maxPurchaseAmount`: desired upgrade cap, currently `5`.
- `physicalItem`: enabled.

For each prefab, keep the root GameObject name exactly equal to the upgrade name. The starter prefab is intentionally small and upright, close to the stock R.E.P.O. upgrade item shape:

- Root: `Rigidbody`, no root `BoxCollider`.
- Child `Force Grab Point`: local position `0, -0.1, 0`.
- Child `ItemEquipCube`: local scale `0.16587609, 0.16587609, 0.16587609`.
- Child `Semi Box Collider`: local scale `0.17882313, 0.26898482, 0.07221024`, with a `BoxCollider` centered at zero and size one.
- Child `Mesh`: replacement visible model. Local position `0, -0.134, 0`, local scale `1, 1, 1`. The generated mesh has the same dimensions as the original `Upgrade Pack` structure: 24 vertices, 36 indices, and bounds about `0.176 x 0.267 x 0.070`. The UV layout follows the original structure, except the bottom face intentionally reuses the top texture panel to avoid the near-black underside.

Generated mesh and material assets live under:

- `Assets/NuageRoles/Generated/Meshes`
- `Assets/NuageRoles/Generated/Materials`

The normal game upgrade prefab also has runtime-only scripts on the root and on `Semi Box Collider`. Do not serialize those in this authoring project unless Unity has the real game assemblies loaded and you are intentionally hand-authoring them. The mod currently adds the important runtime pieces after loading the bundle.

Do not add `Icon` or `Icon Back` sprite children to generated prefabs. Those were an earlier placeholder approach and will fail validation now. The upgrade art should be assigned through the `Mesh` child's material.

Keep upgrade prefabs physically small. The stock upgrade collider is only about `0.18 x 0.27 x 0.07` Unity units. Oversized meshes, sprite bounds, or a flat horizontal model are the easiest ways to make shop items appear as giant panels.

`Nuage Roles/3. Validate` checks these dimensions. If validation fails after hand-editing a prefab, either resize the model/collider manually or run `Nuage Roles/Advanced/2. Normalize Generated Upgrade Prefabs` to reset the starter shape.

Do not add `ItemAttributes`, `REPOLibItemUpgrade`, `PhotonView`, `PhotonTransformView`, `RoomVolumeCheck`, `PhysGrabObjectCollider`, `PhysGrabObject`, or `PhysGrabObjectImpactDetector` in this authoring project. The mod adds those runtime components after loading the bundle, where the real game assemblies are guaranteed to exist.

The plugin adds only game-only runtime components that should not be serialized in this authoring project, including `PhotonView`, `PhotonTransformView`, `RoomVolumeCheck`, `PhysGrabObjectImpactDetector`, `PhysGrabObject`, `PhysGrabObjectBoxCollider`, `PhysGrabObjectCollider`, `ItemAttributes`, `ItemUpgrade`, `ItemToggle`, and `REPOLibItemUpgrade`. It validates the authored bundle data but should not rewrite prices, visuals, UVs, materials, or prefab transforms.

## Stock Upgrade Reference

This is the analysis target copied from the installed game structure, not from the closed mod asset bundle. Use it as a checklist, not as art to redistribute.

Stock `Item Upgrade Player Health` and `Item Upgrade Player Grab Range` share this root setup:

- Root scripts/components: `ItemAttributes`, `PhotonView`, `ItemUpgrade`, `PhysGrabObject`, `PhotonTransformView`, `NotValuableObject`, `Rigidbody`, `RoomVolumeCheck`, `PhysGrabObjectImpactDetector`, `LineBetweenTwoPoints`, `MapCustom`, `AudioSource`, `AudioLowPassFilter`, `AudioLowPassLogic`, `ItemToggle`, and the concrete stock upgrade script.
- Root `Rigidbody`: mass `0.25`, drag `0`, angular drag `0.05`, interpolation enabled.
- Root `ItemAttributes.costOffset`: `0, 0.18, 0`.
- Root `PhysGrabObject`: `clientNonKinematic = true`, `overrideTagsAndLayers = true`, `ignoreGrabPointCentering = false`.
- Root `RoomVolumeCheck`: `Continuous = true`, `currentSize = 0.17882313, 0.26898482, 0.07221024`.
- Root `PhysGrabObjectImpactDetector`: present. The grabber assumes this component exists.

Stock top-level children:

- `Force Grab Point`: layer `PhysGrabObject`, local position `0, -0.1, 0`.
- `ItemEquipCube`: local scale `0.16587609, 0.16587609, 0.16587609`.
- `Semi Box Collider`: layer `PhysGrabObject`, tag `Phys Grab Object`, local scale `0.17882313, 0.26898482, 0.07221024`, with `BoxCollider`, `PhysGrabObjectBoxCollider`, and `PhysGrabObjectCollider`.
- `Line Start`
- `Mesh`
- `Particle Effects`
- `Item Volume upgrade`
- `Light - Small Lamp`

The clean generated NuageRoles prefabs may omit `Particle Effects`; the plugin creates an empty inactive placeholder at runtime so vanilla `ItemUpgrade` can consume the item without requiring original particle assets.

If `Validate` warns that a prefab contains MonoBehaviour script components, remove those scripts unless you intentionally added a purely visual authoring script. A missing script warning/error means Unity could not resolve a serialized component and the bundle should not be tested yet.

## Runtime Notes

The bundle only provides assets. Upgrade behavior still belongs in the mod code.

The plugin registers the display-name assets with short REPOLib upgrade IDs that map onto the existing saved stat dictionaries:

- `Mana Regeneration Upgrade` uses REPOLib id `ManaRegeneration`, saved as `playerUpgradeManaRegeneration`.
- `Scout Cooldown Upgrade` uses REPOLib id `ScoutCooldownReduction`, saved as `playerUpgradeScoutCooldownReduction`.

If the shop item appears but buying it does not change mana regeneration or scout cooldown, inspect the plugin-side `RegisterUpgrade` callbacks, `REPOLibItemUpgrade` id, and `StatsManager` dictionary attachment before changing the asset bundle.

## Troubleshooting

- `Item` or `Value` shows a missing script: wait for Unity compilation to finish. The script should come from `Assets/NuageRoles/Runtime`.
- `NuageRoles asset bundle validation failed`: fix the listed asset name, field, script, prefab, or bundle-label issue, then run `Validate` again.
- `Missing GUI font`: add a redistributable `font.ttf` to `Assets/NuageRoles/SourceArt/Gui`.
- The bundle file has a lowercase name in `AssetBundles/Build`: use the copy in `AssetBundles/Dist`.
- The mod logs missing assets: copy `NuageRoles_assets` beside the mod DLL, not the `.manifest` file.
