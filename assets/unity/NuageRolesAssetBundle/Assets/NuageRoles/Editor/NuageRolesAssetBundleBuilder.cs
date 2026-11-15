using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace NuageRoles.AssetBundles.Editor
{
    public static class NuageRolesAssetBundleBuilder
    {
        private const string Root = "Assets/NuageRoles";
        private const string SourceArtRoot = Root + "/SourceArt";
        private const string SourceGuiRoot = SourceArtRoot + "/Gui";
        private const string SourceUpgradeRoot = SourceArtRoot + "/Upgrades";
        private const string RuntimeRoot = Root + "/Runtime";
        private const string GeneratedRoot = Root + "/Generated";
        private const string BundleGeneratedRoot = GeneratedRoot + "/NuageRoles_assets";
        private const string GuiGeneratedRoot = BundleGeneratedRoot + "/Gui";
        private const string ItemRoot = BundleGeneratedRoot + "/Items";
        private const string PrefabRoot = BundleGeneratedRoot + "/Prefabs";
        private const string ValueRoot = BundleGeneratedRoot + "/Values";
        private const string MaterialRoot = GeneratedRoot + "/Materials";
        private const string MeshRoot = GeneratedRoot + "/Meshes";
        private const string ThirdPartyRoot = Root + "/ThirdParty/REPO";

        private const string BuildRoot = "AssetBundles/Build";
        private const string DistRoot = "AssetBundles/Dist";
        private const string BundleName = "NuageRoles_assets";
        private static readonly string[] ObsoleteBundleNames = { "NuageRoles_assets1", "NuageRoles_assets2" };
        private static readonly Vector3 UpgradeColliderSize = new Vector3(0.17882313f, 0.26898482f, 0.07221024f);
        private static readonly Vector3 UpgradeItemEquipCubeScale = Vector3.one * 0.16587609f;
        private static readonly Vector3 UpgradeMeshLocalPosition = new Vector3(0f, -0.134f, 0f);

        private static readonly UpgradeDefinition[] Upgrades =
        {
            new UpgradeDefinition(
                "Mana Regeneration Upgrade",
                "manaUpgrade.png",
                "Increases mage mana regeneration speed.",
                new Color(0.63f, 0.42f, 0.78f),
                1250f,
                5),
            new UpgradeDefinition(
                "Scout Cooldown Upgrade",
                "scoutUpgrade.png",
                "Reduces scout ability cooldown.",
                new Color(0.05f, 0.58f, 0.4f),
                1250f,
                5)
        };

        [MenuItem("Nuage Roles/1. Setup Workspace", false, 10)]
        public static void SetupWorkspace()
        {
            EnsureProjectFolders();
            CopySiblingReplacementArtIfMissing();
            AssetDatabase.Refresh();
            ConfigureKnownImporters();
            Debug.Log("NuageRoles asset bundle workspace is ready.");
        }

        [MenuItem("Nuage Roles/Advanced/4. Import R.E.P.O. Game Assemblies", false, 140)]
        public static void ImportRepoGameAssemblies()
        {
            EnsureProjectFolders();

            string managedPath = FindRepoManagedPath();
            if (string.IsNullOrEmpty(managedPath))
            {
                throw new BuildFailedException(
                    "Could not find the R.E.P.O. managed assemblies. Set REPO_MANAGED_DIR or install the game in the default Steam location.");
            }

            int copied = 0;
            foreach (string sourcePath in Directory.GetFiles(managedPath, "*.dll", SearchOption.TopDirectoryOnly))
            {
                string fileName = Path.GetFileName(sourcePath);
                if (!ShouldImportGameAssembly(fileName))
                {
                    continue;
                }

                string destinationPath = ToAbsolutePath(ThirdPartyRoot + "/" + fileName);
                if (!File.Exists(destinationPath) || File.GetLastWriteTimeUtc(destinationPath) < File.GetLastWriteTimeUtc(sourcePath))
                {
                    File.Copy(sourcePath, destinationPath, true);
                    copied++;
                }
            }

            AssetDatabase.Refresh();
            ConfigurePluginImporters();

            Debug.Log($"Imported {copied} local-only R.E.P.O. assembly file(s) from {managedPath}.");
            Debug.Log("Imported optional local-only R.E.P.O. layout reference assemblies. The asset authoring stubs in Assets/NuageRoles/Runtime are still used for bundle assets.");
        }

        [MenuItem("Nuage Roles/Advanced/1. Create Missing Starter Assets", false, 110)]
        public static void CreateMissingStarterAssets()
        {
            EnsureProjectFolders();
            CopySiblingReplacementArtIfMissing();
            AssetDatabase.Refresh();
            ConfigureKnownImporters();

            CreateGuiBundleAssets(false);
            CreateUpgradeBundleAssets(false);
            AssignBundleNames();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ValidateProject();
        }

        [MenuItem("Nuage Roles/2. Recreate Assets", false, 20)]
        public static void RecreateAssets()
        {
            RegenerateGeneratedBundleAssets();
        }

        public static void RegenerateGeneratedBundleAssets()
        {
            EnsureProjectFolders();
            CopySiblingReplacementArtIfMissing();
            AssetDatabase.Refresh();
            ConfigureKnownImporters();

            DeleteGeneratedAssetTree(BundleGeneratedRoot);
            DeleteGeneratedAssetTree(MaterialRoot);
            DeleteGeneratedAssetTree(MeshRoot);
            AssetDatabase.Refresh();

            EnsureProjectFolders();
            CreateGuiBundleAssets(true);
            CreateUpgradeBundleAssets(true);
            AssignBundleNames();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ValidateProject();
            Debug.Log("Regenerated NuageRoles bundle assets from source art and local authoring stubs.");
        }

        [MenuItem("Nuage Roles/Advanced/3. Assign Bundle Names", false, 130)]
        public static void AssignBundleNames()
        {
            AssignFolderToBundle(BundleGeneratedRoot, BundleName);
            AssetDatabase.RemoveUnusedAssetBundleNames();
            Debug.Log("Assigned NuageRoles single asset bundle name.");
        }

        [MenuItem("Nuage Roles/Advanced/2. Normalize Generated Upgrade Prefabs", false, 120)]
        public static void NormalizeGeneratedUpgradePrefabs()
        {
            EnsureProjectFolders();
            CopySiblingReplacementArtIfMissing();
            AssetDatabase.Refresh();
            ConfigureKnownImporters();

            foreach (UpgradeDefinition upgrade in Upgrades)
            {
                Texture2D texture = LoadUpgradeTexture(upgrade);
                CreateOrNormalizeUpgradePrefab(upgrade, texture);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateProject();
        }

        [MenuItem("Nuage Roles/4. Build Bundle", false, 40)]
        public static void BuildBundles()
        {
            EnsureProjectFolders();
            AssetDatabase.Refresh();
            ValidateRequiredGeneratedAssets();
            AssignBundleNames();
            AssetDatabase.SaveAssets();
            ValidateProject(false);

            Directory.CreateDirectory(ToAbsolutePath(BuildRoot));
            Directory.CreateDirectory(ToAbsolutePath(DistRoot));

            AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(
                ToAbsolutePath(BuildRoot),
                BuildAssetBundleOptions.ChunkBasedCompression,
                BuildTarget.StandaloneWindows64);

            if (manifest == null)
            {
                throw new BuildFailedException("Unity did not produce an AssetBundle manifest.");
            }

            DeleteObsoleteBundleCopies(BuildRoot);
            DeleteObsoleteBundleCopies(DistRoot);
            CopyBuiltBundleToDist(BundleName);

            ValidateBuiltBundles();
            Debug.Log($"Built NuageRoles bundle in {ToAbsolutePath(DistRoot)}.");
        }

        [MenuItem("Nuage Roles/5. Copy Bundle To Mod Assets Folder", false, 50)]
        public static void CopyDistBundlesToModPluginFolder()
        {
            string bundleFolder = Path.GetFullPath(Path.Combine(ProjectRoot, "..", "..", "bundles"));
            if (!Directory.Exists(bundleFolder))
            {
                throw new DirectoryNotFoundException($"Could not find mod bundle folder: {bundleFolder}");
            }

            DeleteObsoleteBundleCopies(bundleFolder);
            CopyRequiredDistBundle(bundleFolder, BundleName);
            Debug.Log($"Copied NuageRoles bundle to {bundleFolder}.");
        }

        public static void BuildBundleToModAssetsFolder()
        {
            BuildBundles();
            CopyDistBundlesToModPluginFolder();
        }

        [MenuItem("Nuage Roles/3. Validate", false, 30)]
        public static void ValidateProjectFromMenu()
        {
            ValidateProject();
        }

        public static void ValidateProject(bool validateExistingDistBundle = true)
        {
            List<string> errors = new List<string>();
            List<string> warnings = new List<string>();

            RequireAsset(errors, GuiGeneratedRoot + "/emptyBottle.png", "GUI empty mana texture");
            RequireAsset(errors, GuiGeneratedRoot + "/filledBottle.png", "GUI full mana texture");
            RequireAsset(errors, GuiGeneratedRoot + "/font.ttf", "GUI font");

            Type itemType = FindLoadedRepoType("Item", typeof(ScriptableObject));
            Type valueType = FindLoadedRepoType("Value", typeof(ScriptableObject));
            if (itemType == null)
            {
                warnings.Add("Item authoring type is not loaded. Wait for Unity compilation to finish, then validate again.");
            }

            if (valueType == null)
            {
                warnings.Add("Value authoring type is not loaded. Wait for Unity compilation to finish, then validate again.");
            }

            foreach (UpgradeDefinition upgrade in Upgrades)
            {
                RequireAsset(errors, PrefabPath(upgrade), $"prefab for {upgrade.AssetName}");
                RequireAsset(errors, ItemPath(upgrade), $"Item asset for {upgrade.AssetName}");
                RequireAsset(errors, ValuePath(upgrade), $"Value asset for {upgrade.AssetName}");
                ValidateNoMissingScriptYaml(errors, ValuePath(upgrade), $"Value asset for {upgrade.AssetName}");
                ValidateNoMissingScriptYaml(errors, ItemPath(upgrade), $"Item asset for {upgrade.AssetName}");
                ValidateNoMissingScriptYaml(errors, PrefabPath(upgrade), $"prefab for {upgrade.AssetName}");
                ValidateGeneratedScriptableObject(errors, ValuePath(upgrade), "Value", $"Value asset for {upgrade.AssetName}");
                ValidateGeneratedScriptableObject(errors, ItemPath(upgrade), "Item", $"Item asset for {upgrade.AssetName}");
                ValidateLocalAuthoringScript(errors, ValuePath(upgrade), $"Value asset for {upgrade.AssetName}");
                ValidateLocalAuthoringScript(errors, ItemPath(upgrade), $"Item asset for {upgrade.AssetName}");
                ValidateUpgradeValueAsset(errors, ValuePath(upgrade), upgrade);
                ValidateUpgradeItemAsset(errors, ItemPath(upgrade), upgrade);
                ValidateGeneratedPrefab(errors, PrefabPath(upgrade), upgrade);
                WarnIfPrefabHasMonoBehaviours(warnings, PrefabPath(upgrade), $"prefab for {upgrade.AssetName}");
                ValidateBundleLabel(errors, PrefabPath(upgrade), BundleName);
                ValidateBundleLabel(errors, ItemPath(upgrade), BundleName);
                ValidateBundleLabel(errors, ValuePath(upgrade), BundleName);
            }

            ValidateBundleLabel(errors, GuiGeneratedRoot + "/emptyBottle.png", BundleName);
            ValidateBundleLabel(errors, GuiGeneratedRoot + "/filledBottle.png", BundleName);
            ValidateBundleLabel(errors, GuiGeneratedRoot + "/font.ttf", BundleName);

            string distBundle = ToAbsolutePath(DistRoot + "/" + BundleName);
            if (validateExistingDistBundle && File.Exists(distBundle))
            {
                ValidateBuiltBundles(errors, warnings);
            }

            foreach (string warning in warnings)
            {
                Debug.LogWarning(warning);
            }

            if (errors.Count > 0)
            {
                throw new BuildFailedException("NuageRoles asset bundle validation failed:\n" + string.Join("\n", errors));
            }

            Debug.Log("NuageRoles asset bundle validation passed.");
        }

        [MenuItem("Nuage Roles/Advanced/5. Diagnose Loaded R.E.P.O. Types", false, 150)]
        public static void DiagnoseLoadedRepoTypes()
        {
            LogTypeCandidate("Item", typeof(ScriptableObject));
            LogRuntimeMonoScriptCandidate("Item", typeof(ScriptableObject));
            LogTypeCandidate("Value", typeof(ScriptableObject));
            LogRuntimeMonoScriptCandidate("Value", typeof(ScriptableObject));
            LogTypeCandidate("ItemAttributes", typeof(Component));
            LogRuntimeMonoScriptCandidate("ItemAttributes", typeof(Component));
        }

        private static void EnsureProjectFolders()
        {
            foreach (string folder in new[]
            {
                Root,
                SourceArtRoot,
                SourceGuiRoot,
                SourceUpgradeRoot,
                RuntimeRoot,
                GeneratedRoot,
                BundleGeneratedRoot,
                GuiGeneratedRoot,
                ItemRoot,
                PrefabRoot,
                ValueRoot,
                MaterialRoot,
                MeshRoot,
                ThirdPartyRoot
            })
            {
                Directory.CreateDirectory(ToAbsolutePath(folder));
            }
        }

        private static void CopySiblingReplacementArtIfMissing()
        {
            string sourceTextureRoot = Path.GetFullPath(Path.Combine(ProjectRoot, "..", "..", "source", "textures"));
            if (!Directory.Exists(sourceTextureRoot))
            {
                return;
            }

            CopyIfMissing(Path.Combine(sourceTextureRoot, "manaEmpty.png"), ToAbsolutePath(SourceGuiRoot + "/manaEmpty.png"));
            CopyIfMissing(Path.Combine(sourceTextureRoot, "manaFull.png"), ToAbsolutePath(SourceGuiRoot + "/manaFull.png"));
            CopyIfMissing(Path.Combine(sourceTextureRoot, "manaUpgrade.png"), ToAbsolutePath(SourceUpgradeRoot + "/manaUpgrade.png"));
            CopyIfMissing(Path.Combine(sourceTextureRoot, "scoutUpgrade.png"), ToAbsolutePath(SourceUpgradeRoot + "/scoutUpgrade.png"));
        }

        private static void CreateGuiBundleAssets(bool overwriteExisting)
        {
            CopyGeneratedAsset(SourceGuiRoot + "/manaEmpty.png", GuiGeneratedRoot + "/emptyBottle.png", overwriteExisting);
            CopyGeneratedAsset(SourceGuiRoot + "/manaFull.png", GuiGeneratedRoot + "/filledBottle.png", overwriteExisting);
            CopyGeneratedAsset(SourceGuiRoot + "/font.ttf", GuiGeneratedRoot + "/font.ttf", overwriteExisting);
            AssetDatabase.Refresh();

            ConfigureTextureImporter(GuiGeneratedRoot + "/emptyBottle.png", TextureImporterType.Default, true);
            ConfigureTextureImporter(GuiGeneratedRoot + "/filledBottle.png", TextureImporterType.Default, true);
        }

        private static void CreateUpgradeBundleAssets(bool normalizeExistingPrefabs)
        {
            foreach (UpgradeDefinition upgrade in Upgrades)
            {
                CreateValueAsset(upgrade);
                if (normalizeExistingPrefabs || !File.Exists(ToAbsolutePath(PrefabPath(upgrade))))
                {
                    Texture2D texture = LoadUpgradeTexture(upgrade);
                    CreateOrNormalizeUpgradePrefab(upgrade, texture);
                }
                CreateItemAsset(upgrade);
            }
        }

        private static ScriptableObject CreateValueAsset(UpgradeDefinition upgrade)
        {
            string path = ValuePath(upgrade);
            Value existing = AssetDatabase.LoadAssetAtPath<Value>(path);
            if (existing != null)
            {
                return existing;
            }

            Value valueAsset = ScriptableObject.CreateInstance<Value>();
            valueAsset.name = upgrade.AssetName + " Value";
            valueAsset.valueMin = upgrade.Price;
            valueAsset.valueMax = upgrade.Price;
            AssetDatabase.CreateAsset(valueAsset, path);
            return valueAsset;
        }

        private static ScriptableObject CreateItemAsset(UpgradeDefinition upgrade)
        {
            string path = ItemPath(upgrade);
            Item existing = AssetDatabase.LoadAssetAtPath<Item>(path);
            if (existing != null)
            {
                return existing;
            }

            Item itemAsset = ScriptableObject.CreateInstance<Item>();
            itemAsset.name = upgrade.AssetName;
            itemAsset.disabled = false;
            itemAsset.itemName = upgrade.AssetName;
            itemAsset.itemNameLocalized = null;
            itemAsset.description = upgrade.Description;
            itemAsset.itemType = SemiFunc.itemType.item_upgrade;
            itemAsset.emojiIcon = SemiFunc.emojiIcon.drone_heal;
            itemAsset.itemVolume = SemiFunc.itemVolume.upgrade;
            itemAsset.itemSecretShopType = SemiFunc.itemSecretShopType.none;
            itemAsset.colorPreset = null;
            itemAsset.prefab = new PrefabRef();
            itemAsset.value = AssetDatabase.LoadAssetAtPath<Value>(ValuePath(upgrade));
            itemAsset.maxAmount = 1;
            itemAsset.maxAmountInShop = 1;
            itemAsset.minPlayerCount = 1;
            itemAsset.maxPurchase = true;
            itemAsset.maxPurchaseAmount = upgrade.MaxPurchaseAmount;
            itemAsset.spawnRotationOffset = Quaternion.identity;
            itemAsset.physicalItem = true;
            AssetDatabase.CreateAsset(itemAsset, path);
            return itemAsset;
        }

        private static void CreateOrNormalizeUpgradePrefab(UpgradeDefinition upgrade, Texture2D texture)
        {
            string path = PrefabPath(upgrade);
            Material material = CreateOrUpdateMaterial(upgrade, texture);
            Mesh boxMesh = CreateOrUpdateBoxMesh(upgrade);

            if (File.Exists(ToAbsolutePath(path)))
            {
                GameObject existingRoot = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    ApplyUpgradePrefabDefaults(existingRoot, upgrade, material, boxMesh);
                    PrefabUtility.SaveAsPrefabAsset(existingRoot, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(existingRoot);
                }
                return;
            }

            GameObject root = new GameObject(upgrade.AssetName);
            ApplyUpgradePrefabDefaults(root, upgrade, material, boxMesh);

            AddItemAttributesIfAvailable();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void ApplyUpgradePrefabDefaults(GameObject root, UpgradeDefinition upgrade, Material material, Mesh boxMesh)
        {
            root.name = upgrade.AssetName;
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            Rigidbody rigidbody = root.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = root.AddComponent<Rigidbody>();
            }

            rigidbody.mass = 0.25f;
            rigidbody.drag = 0f;
            rigidbody.angularDrag = 0.05f;
            rigidbody.useGravity = true;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

            BoxCollider rootCollider = root.GetComponent<BoxCollider>();
            if (rootCollider != null)
            {
                UnityEngine.Object.DestroyImmediate(rootCollider);
            }

            DestroyChildIfExists(root.transform, "Body");
            DestroyChildIfExists(root.transform, "Top");
            DestroyChildIfExists(root.transform, "Icon");
            DestroyChildIfExists(root.transform, "Icon Back");

            Transform forceGrabPoint = FindOrCreateChild(root.transform, "Force Grab Point");
            forceGrabPoint.localPosition = new Vector3(0f, -0.1f, 0f);
            forceGrabPoint.localRotation = Quaternion.identity;
            forceGrabPoint.localScale = Vector3.one;

            Transform itemEquipCube = FindOrCreateChild(root.transform, "ItemEquipCube");
            itemEquipCube.localPosition = Vector3.zero;
            itemEquipCube.localRotation = Quaternion.identity;
            itemEquipCube.localScale = UpgradeItemEquipCubeScale;

            Transform grabColliderTransform = FindOrCreateChild(root.transform, "Semi Box Collider");
            grabColliderTransform.localPosition = Vector3.zero;
            grabColliderTransform.localRotation = Quaternion.identity;
            grabColliderTransform.localScale = UpgradeColliderSize;

            BoxCollider grabCollider = grabColliderTransform.GetComponent<BoxCollider>();
            if (grabCollider == null)
            {
                grabCollider = grabColliderTransform.gameObject.AddComponent<BoxCollider>();
            }

            grabCollider.center = Vector3.zero;
            grabCollider.size = Vector3.one;
            grabCollider.isTrigger = false;

            Transform body = FindOrCreateChild(root.transform, "Mesh");
            body.localPosition = UpgradeMeshLocalPosition;
            body.localRotation = Quaternion.identity;
            body.localScale = Vector3.one;
            ConfigureMeshChild(body, boxMesh, material);
        }

        private static void DestroyChildIfExists(Transform root, string childName)
        {
            Transform child = root.Find(childName);
            if (child != null)
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        private static Transform FindOrCreateChild(Transform root, string childName)
        {
            Transform child = root.Find(childName);
            if (child != null)
            {
                return child;
            }

            GameObject childObject = new GameObject(childName);
            childObject.transform.SetParent(root, false);
            return childObject.transform;
        }

        private static void ConfigureMeshChild(Transform transform, Mesh mesh, Material material)
        {
            MeshFilter meshFilter = transform.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = transform.gameObject.AddComponent<MeshFilter>();
            }

            MeshRenderer meshRenderer = transform.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = transform.gameObject.AddComponent<MeshRenderer>();
            }

            Collider childCollider = transform.GetComponent<Collider>();
            if (childCollider != null)
            {
                UnityEngine.Object.DestroyImmediate(childCollider);
            }

            SpriteRenderer spriteRenderer = transform.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                UnityEngine.Object.DestroyImmediate(spriteRenderer);
            }

            meshFilter.sharedMesh = mesh;
            meshRenderer.sharedMaterial = material;
        }

        private static void AddItemAttributesIfAvailable()
        {
            // The plugin adds ItemAttributes and REPOLibItemUpgrade at runtime after loading
            // the prefab, so the authoring project does not need to serialize game components.
        }

        private static Texture2D LoadUpgradeTexture(UpgradeDefinition upgrade)
        {
            string texturePath = SourceUpgradeRoot + "/" + upgrade.SourceTextureName;
            ConfigureTextureImporter(texturePath, TextureImporterType.Default, false, true, TextureWrapMode.Repeat);
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        }

        private static Material CreateOrUpdateMaterial(UpgradeDefinition upgrade, Texture2D texture)
        {
            string path = MaterialRoot + "/" + SafeFileName(upgrade.AssetName) + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = FindUpgradeMaterialShader();
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                Shader shader = FindUpgradeMaterialShader();
                if (shader != null)
                {
                    material.shader = shader;
                }
            }

            material.name = Path.GetFileNameWithoutExtension(path);
            Color materialColor = texture != null ? Color.white : upgrade.Tint;
            material.color = materialColor;
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", materialColor);
            }
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", materialColor);
            }
            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }
            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", 0.3f);
            }
            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", 0f);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Mesh CreateOrUpdateBoxMesh(UpgradeDefinition upgrade)
        {
            string path = MeshRoot + "/" + SafeFileName(upgrade.AssetName) + ".asset";
            DeleteAssetFile(path);
            Mesh mesh = new Mesh();
            mesh.name = Path.GetFileNameWithoutExtension(path);
            mesh.vertices = new[]
            {
                new Vector3(0.08817109f, 0.26743123f, -0.03508858f),
                new Vector3(0.08817109f, 0f, -0.03508856f),
                new Vector3(-0.08817109f, 0f, -0.03508856f),
                new Vector3(-0.08817109f, 0.26743123f, -0.03508858f),
                new Vector3(-0.08817109f, 0f, 0.03508855f),
                new Vector3(-0.08817109f, 0.26743123f, 0.03508854f),
                new Vector3(-0.08817109f, 0.26743123f, -0.03508858f),
                new Vector3(-0.08817109f, 0f, -0.03508856f),
                new Vector3(0.08817109f, 0f, -0.03508856f),
                new Vector3(0.08817109f, 0.26743123f, -0.03508858f),
                new Vector3(0.08817109f, 0.26743123f, 0.03508854f),
                new Vector3(0.08817109f, 0f, 0.03508855f),
                new Vector3(0.08817109f, 0f, -0.03508856f),
                new Vector3(0.08817109f, 0f, 0.03508855f),
                new Vector3(-0.08817109f, 0f, 0.03508855f),
                new Vector3(-0.08817109f, 0f, -0.03508856f),
                new Vector3(0.08817109f, 0f, 0.03508855f),
                new Vector3(0.08817109f, 0.26743123f, 0.03508854f),
                new Vector3(-0.08817109f, 0.26743123f, 0.03508854f),
                new Vector3(-0.08817109f, 0f, 0.03508855f),
                new Vector3(0.08817109f, 0.26743123f, 0.03508854f),
                new Vector3(0.08817109f, 0.26743123f, -0.03508858f),
                new Vector3(-0.08817109f, 0.26743123f, -0.03508858f),
                new Vector3(-0.08817109f, 0.26743123f, 0.03508854f)
            };
            // The bottom face intentionally reuses the top-panel UVs; the old bottom panel is nearly black.
            mesh.uv = new[]
            {
                new Vector2(0.35791016f, 0.54199219f),
                new Vector2(0.35791016f, 0.00292969f),
                new Vector2(0.00244141f, 0.00292969f),
                new Vector2(0.00244141f, 0.54199219f),
                new Vector2(0.85498047f, 0.00292969f),
                new Vector2(0.85498047f, 0.54199219f),
                new Vector2(0.99658203f, 0.54199219f),
                new Vector2(0.99658203f, 0.00292969f),
                new Vector2(0.35791016f, 0.00292969f),
                new Vector2(0.35791016f, 0.54199219f),
                new Vector2(0.49951172f, 0.54199219f),
                new Vector2(0.49951172f, 0.00292969f),
                new Vector2(0.27954102f, 0.68945312f),
                new Vector2(0.27954102f, 0.54785156f),
                new Vector2(0.63525391f, 0.54785156f),
                new Vector2(0.63525391f, 0.68945312f),
                new Vector2(0.49951172f, 0.00292969f),
                new Vector2(0.49951172f, 0.54199219f),
                new Vector2(0.85498047f, 0.54199219f),
                new Vector2(0.85498047f, 0.00292969f),
                new Vector2(0.27954102f, 0.54785156f),
                new Vector2(0.27954102f, 0.68945312f),
                new Vector2(0.63525391f, 0.68945312f),
                new Vector2(0.63525391f, 0.54785156f)
            };
            mesh.triangles = new[]
            {
                0, 1, 2,
                0, 2, 3,
                4, 5, 6,
                4, 6, 7,
                8, 9, 10,
                8, 10, 11,
                12, 13, 14,
                12, 14, 15,
                16, 17, 18,
                16, 18, 19,
                20, 21, 22,
                20, 22, 23
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.UploadMeshData(false);

            AssetDatabase.CreateAsset(mesh, path);
            EditorUtility.SetDirty(mesh);
            AssetDatabase.SaveAssets();
            return mesh;
        }

        private static Shader FindUpgradeMaterialShader()
        {
            return Shader.Find("Standard")
                ?? Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Unlit/Texture")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Diffuse");
        }

        private static void ConfigureKnownImporters()
        {
            ConfigureTextureImporter(SourceGuiRoot + "/manaEmpty.png", TextureImporterType.Default, true);
            ConfigureTextureImporter(SourceGuiRoot + "/manaFull.png", TextureImporterType.Default, true);

            foreach (UpgradeDefinition upgrade in Upgrades)
            {
                ConfigureTextureImporter(SourceUpgradeRoot + "/" + upgrade.SourceTextureName, TextureImporterType.Default, false, true, TextureWrapMode.Repeat);
            }
        }

        private static void ConfigureTextureImporter(
            string projectPath,
            TextureImporterType textureType,
            bool readable,
            bool mipmaps = false,
            TextureWrapMode wrapMode = TextureWrapMode.Clamp)
        {
            TextureImporter importer = AssetImporter.GetAtPath(projectPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = textureType;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.mipmapEnabled = mipmaps;
            importer.isReadable = readable;
            importer.wrapMode = wrapMode;
            if (textureType == TextureImporterType.Sprite)
            {
                importer.spritePixelsPerUnit = 512f;
            }
            importer.SaveAndReimport();
        }

        private static void ConfigurePluginImporters()
        {
            foreach (string dllPath in Directory.GetFiles(ToAbsolutePath(ThirdPartyRoot), "*.dll", SearchOption.TopDirectoryOnly))
            {
                string assetPath = ToProjectPath(dllPath);
                PluginImporter importer = AssetImporter.GetAtPath(assetPath) as PluginImporter;
                if (importer == null)
                {
                    continue;
                }

                importer.SetCompatibleWithAnyPlatform(false);
                importer.SetCompatibleWithEditor(false);
                importer.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows, false);
                importer.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows64, false);
                importer.SaveAndReimport();
            }
        }

        private static void AssignFolderToBundle(string folder, string bundleName)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            foreach (string guid in AssetDatabase.FindAssets(string.Empty, new[] { folder }))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(assetPath))
                {
                    continue;
                }

                AssetImporter importer = AssetImporter.GetAtPath(assetPath);
                if (importer != null)
                {
                    importer.assetBundleName = bundleName.ToLowerInvariant();
                }
            }
        }

        private static void CopyBuiltBundleToDist(string bundleName)
        {
            string buildFolder = ToAbsolutePath(BuildRoot);
            string source = Directory.GetFiles(buildFolder, "*", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(path => Path.GetFileName(path).Equals(bundleName, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrEmpty(source))
            {
                throw new BuildFailedException($"Could not find built bundle for {bundleName} in {buildFolder}.");
            }

            string destination = ToAbsolutePath(DistRoot + "/" + bundleName);
            File.Copy(source, destination, true);

            string manifestSource = source + ".manifest";
            if (File.Exists(manifestSource))
            {
                File.Copy(manifestSource, destination + ".manifest", true);
            }
        }

        private static void CopyRequiredDistBundle(string destinationFolder, string bundleName)
        {
            string source = ToAbsolutePath(DistRoot + "/" + bundleName);
            if (!File.Exists(source))
            {
                throw new FileNotFoundException($"Build the bundles first. Missing {source}");
            }

            File.Copy(source, Path.Combine(destinationFolder, bundleName), true);
        }

        private static void DeleteObsoleteBundleCopies(string destinationFolder)
        {
            string absoluteFolder = Path.IsPathRooted(destinationFolder)
                ? destinationFolder
                : ToAbsolutePath(destinationFolder);

            foreach (string obsoleteBundleName in ObsoleteBundleNames)
            {
                string obsoletePath = Path.Combine(absoluteFolder, obsoleteBundleName);
                if (File.Exists(obsoletePath))
                {
                    File.Delete(obsoletePath);
                }

                string obsoleteManifestPath = obsoletePath + ".manifest";
                if (File.Exists(obsoleteManifestPath))
                {
                    File.Delete(obsoleteManifestPath);
                }
            }
        }

        private static void ValidateRequiredGeneratedAssets()
        {
            List<string> missing = new List<string>();
            AddIfMissing(missing, GuiGeneratedRoot + "/emptyBottle.png");
            AddIfMissing(missing, GuiGeneratedRoot + "/filledBottle.png");
            AddIfMissing(missing, GuiGeneratedRoot + "/font.ttf");

            foreach (UpgradeDefinition upgrade in Upgrades)
            {
                AddIfMissing(missing, ItemPath(upgrade));
                AddIfMissing(missing, ValuePath(upgrade));
                AddIfMissing(missing, PrefabPath(upgrade));
            }

            if (missing.Count > 0)
            {
                throw new BuildFailedException("Missing generated assets:\n" + string.Join("\n", missing));
            }
        }

        private static void ValidateBuiltBundles()
        {
            List<string> errors = new List<string>();
            List<string> warnings = new List<string>();
            ValidateBuiltBundles(errors, warnings);

            foreach (string warning in warnings)
            {
                Debug.LogWarning(warning);
            }

            if (errors.Count > 0)
            {
                throw new BuildFailedException("NuageRoles built bundle validation failed:\n" + string.Join("\n", errors));
            }
        }

        private static void ValidateBuiltBundles(List<string> errors, List<string> warnings)
        {
            ValidateBundleFile(errors, warnings, BundleName, new[]
                {
                    "emptybottle.png",
                    "filledbottle.png",
                    "font.ttf"
                }
                .Concat(Upgrades.SelectMany(upgrade => new[]
                {
                    SafeFileName(upgrade.AssetName).ToLowerInvariant() + ".asset",
                    (SafeFileName(upgrade.AssetName) + " Value").ToLowerInvariant() + ".asset",
                    SafeFileName(upgrade.AssetName).ToLowerInvariant() + ".prefab"
                }))
                .ToArray());
        }

        private static void ValidateBundleFile(List<string> errors, List<string> warnings, string bundleName, string[] requiredAssetFileNames)
        {
            string bundlePath = ToAbsolutePath(DistRoot + "/" + bundleName);
            if (!File.Exists(bundlePath))
            {
                warnings.Add($"Built bundle is missing: {bundlePath}");
                return;
            }

            AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle == null)
            {
                errors.Add($"Could not load built bundle: {bundlePath}");
                return;
            }

            try
            {
                string[] names = bundle.GetAllAssetNames();
                foreach (string required in requiredAssetFileNames)
                {
                    bool found = names.Any(name => name.EndsWith("/" + required, StringComparison.OrdinalIgnoreCase));
                    if (!found)
                    {
                        errors.Add($"{bundleName} does not contain an asset ending with {required}.");
                    }
                }
            }
            finally
            {
                bundle.Unload(true);
            }
        }

        private static void ValidateBundleLabel(List<string> errors, string assetPath, string expectedBundleName)
        {
            AssetImporter importer = AssetImporter.GetAtPath(assetPath);
            if (importer == null)
            {
                return;
            }

            if (!importer.assetBundleName.Equals(expectedBundleName, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"{assetPath} is assigned to bundle '{importer.assetBundleName}', expected '{expectedBundleName}'.");
            }
        }

        private static void RequireAsset(List<string> errors, string path, string label)
        {
            if (!File.Exists(ToAbsolutePath(path)))
            {
                errors.Add($"Missing {label}: {path}");
            }
        }

        private static void ValidateNoMissingScriptYaml(List<string> errors, string path, string label)
        {
            string absolutePath = ToAbsolutePath(path);
            if (!File.Exists(absolutePath))
            {
                return;
            }

            string text = File.ReadAllText(absolutePath);
            if (text.Contains("m_Script: {fileID: 0}", StringComparison.Ordinal))
            {
                errors.Add($"{label} has a missing script reference: {path}");
            }
        }

        private static void WarnIfPrefabHasMonoBehaviours(List<string> warnings, string path, string label)
        {
            string absolutePath = ToAbsolutePath(path);
            if (!File.Exists(absolutePath))
            {
                return;
            }

            string text = File.ReadAllText(absolutePath);
            if (text.Contains("--- !u!114", StringComparison.Ordinal))
            {
                warnings.Add($"{label} contains MonoBehaviour script components. This is usually unnecessary; the plugin adds shop, grab, Photon, ItemAttributes, and REPOLib upgrade scripts at runtime.");
            }
        }

        private static void ValidateGeneratedScriptableObject(List<string> errors, string path, string expectedTypeName, string label)
        {
            if (!File.Exists(ToAbsolutePath(path)))
            {
                return;
            }

            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null)
            {
                errors.Add($"{label} could not be loaded by Unity: {path}");
                return;
            }

            if (asset is not ScriptableObject)
            {
                errors.Add($"{label} is a {asset.GetType().FullName}, expected a ScriptableObject-backed {expectedTypeName}: {path}");
                return;
            }

            if (!asset.GetType().Name.Equals(expectedTypeName, StringComparison.Ordinal))
            {
                errors.Add($"{label} loaded as {asset.GetType().FullName}, expected {expectedTypeName}: {path}");
            }
        }

        private static void ValidateLocalAuthoringScript(List<string> errors, string path, string label)
        {
            ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (asset == null)
            {
                return;
            }

            MonoScript script = MonoScript.FromScriptableObject(asset);
            string scriptPath = script == null ? string.Empty : AssetDatabase.GetAssetPath(script);
            if (!scriptPath.StartsWith(RuntimeRoot + "/", StringComparison.Ordinal))
            {
                errors.Add($"{label} uses '{scriptPath}' as its script. Expected a local authoring script under {RuntimeRoot}.");
            }
        }

        private static void ValidateUpgradeValueAsset(List<string> errors, string path, UpgradeDefinition upgrade)
        {
            Value value = AssetDatabase.LoadAssetAtPath<Value>(path);
            if (value == null)
            {
                return;
            }

            if (!value.name.Equals(upgrade.AssetName + " Value", StringComparison.Ordinal))
            {
                errors.Add($"{path} object name is '{value.name}', expected '{upgrade.AssetName} Value'.");
            }

            if (!Mathf.Approximately(value.valueMin, upgrade.Price))
            {
                errors.Add($"{path} valueMin is {value.valueMin}, expected {upgrade.Price}.");
            }

            if (!Mathf.Approximately(value.valueMax, upgrade.Price))
            {
                errors.Add($"{path} valueMax is {value.valueMax}, expected {upgrade.Price}.");
            }
        }

        private static void ValidateUpgradeItemAsset(List<string> errors, string path, UpgradeDefinition upgrade)
        {
            Item item = AssetDatabase.LoadAssetAtPath<Item>(path);
            if (item == null)
            {
                return;
            }

            if (!item.name.Equals(upgrade.AssetName, StringComparison.Ordinal))
            {
                errors.Add($"{path} object name is '{item.name}', expected '{upgrade.AssetName}'.");
            }

            if (!string.Equals(item.itemName, upgrade.AssetName, StringComparison.Ordinal))
            {
                errors.Add($"{path} itemName is '{item.itemName}', expected '{upgrade.AssetName}'.");
            }

            if (item.itemType != SemiFunc.itemType.item_upgrade)
            {
                errors.Add($"{path} itemType is {item.itemType}, expected item_upgrade.");
            }

            if (item.itemVolume != SemiFunc.itemVolume.upgrade)
            {
                errors.Add($"{path} itemVolume is {item.itemVolume}, expected upgrade.");
            }

            if (item.value == null)
            {
                errors.Add($"{path} value is not assigned.");
            }
            else if (!AssetDatabase.GetAssetPath(item.value).Equals(ValuePath(upgrade), StringComparison.Ordinal))
            {
                errors.Add($"{path} value points to {AssetDatabase.GetAssetPath(item.value)}, expected {ValuePath(upgrade)}.");
            }

            if (item.maxAmount != 1)
            {
                errors.Add($"{path} maxAmount is {item.maxAmount}, expected 1.");
            }

            if (item.maxAmountInShop != 1)
            {
                errors.Add($"{path} maxAmountInShop is {item.maxAmountInShop}, expected 1.");
            }

            if (item.minPlayerCount != 1)
            {
                errors.Add($"{path} minPlayerCount is {item.minPlayerCount}, expected 1.");
            }

            if (!item.maxPurchase)
            {
                errors.Add($"{path} maxPurchase is disabled.");
            }

            if (item.maxPurchaseAmount != upgrade.MaxPurchaseAmount)
            {
                errors.Add($"{path} maxPurchaseAmount is {item.maxPurchaseAmount}, expected {upgrade.MaxPurchaseAmount}.");
            }

            if (!item.physicalItem)
            {
                errors.Add($"{path} physicalItem is disabled.");
            }
        }

        private static void ValidateGeneratedPrefab(List<string> errors, string path, UpgradeDefinition upgrade)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                return;
            }

            if (!prefab.name.Equals(upgrade.AssetName, StringComparison.Ordinal))
            {
                errors.Add($"{path} main prefab object is named '{prefab.name}', expected '{upgrade.AssetName}'.");
            }

            Rigidbody rigidbody = prefab.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                errors.Add($"{path} is missing a Rigidbody on the root object.");
            }

            BoxCollider rootCollider = prefab.GetComponent<BoxCollider>();
            if (rootCollider != null)
            {
                errors.Add($"{path} still has a root BoxCollider. Use the 'Semi Box Collider' child instead.");
            }

            foreach (string childName in new[] { "Force Grab Point", "ItemEquipCube", "Semi Box Collider", "Mesh" })
            {
                if (prefab.transform.Find(childName) == null)
                {
                    errors.Add($"{path} is missing child '{childName}'.");
                }
            }

            foreach (string staleChildName in new[] { "Body", "Top", "Icon", "Icon Back" })
            {
                if (prefab.transform.Find(staleChildName) != null)
                {
                    errors.Add($"{path} still contains stale child '{staleChildName}'. Recreate or normalize the generated prefab.");
                }
            }

            Transform forceGrabPoint = prefab.transform.Find("Force Grab Point");
            if (forceGrabPoint != null)
            {
                ValidateVector(errors, path, "Force Grab Point.localPosition", forceGrabPoint.localPosition, new Vector3(0f, -0.1f, 0f), 0.001f);
            }

            Transform itemEquipCube = prefab.transform.Find("ItemEquipCube");
            if (itemEquipCube != null)
            {
                ValidateVector(errors, path, "ItemEquipCube.localScale", itemEquipCube.localScale, UpgradeItemEquipCubeScale, 0.001f);
            }

            Transform grabColliderTransform = prefab.transform.Find("Semi Box Collider");
            if (grabColliderTransform != null)
            {
                ValidateVector(errors, path, "Semi Box Collider.localPosition", grabColliderTransform.localPosition, Vector3.zero, 0.001f);
                ValidateVector(errors, path, "Semi Box Collider.localScale", grabColliderTransform.localScale, UpgradeColliderSize, 0.001f);

                BoxCollider collider = grabColliderTransform.GetComponent<BoxCollider>();
                if (collider == null)
                {
                    errors.Add($"{path} Semi Box Collider child is missing a BoxCollider.");
                }
                else
                {
                    ValidateVector(errors, path, "Semi Box Collider BoxCollider.center", collider.center, Vector3.zero, 0.001f);
                    ValidateVector(errors, path, "Semi Box Collider BoxCollider.size", collider.size, Vector3.one, 0.001f);
                }
            }

            Transform mesh = prefab.transform.Find("Mesh");
            if (mesh != null)
            {
                ValidateVector(errors, path, "Mesh.localPosition", mesh.localPosition, UpgradeMeshLocalPosition, 0.001f);
                ValidateVector(errors, path, "Mesh.localScale", mesh.localScale, Vector3.one, 0.001f);

                MeshFilter meshFilter = mesh.GetComponent<MeshFilter>();
                if (meshFilter == null || meshFilter.sharedMesh == null)
                {
                    errors.Add($"{path} Mesh child is missing a MeshFilter with a generated mesh.");
                }
                else
                {
                    if (meshFilter.sharedMesh.vertexCount != 24)
                    {
                        errors.Add($"{path} Mesh has {meshFilter.sharedMesh.vertexCount} vertices, expected 24 from the stock Upgrade Pack layout.");
                    }

                    ValidateVector(errors, path, "Mesh.bounds.size", meshFilter.sharedMesh.bounds.size, new Vector3(0.1763422f, 0.2674312f, 0.07017713f), 0.001f);
                }

                MeshRenderer meshRenderer = mesh.GetComponent<MeshRenderer>();
                if (meshRenderer == null || meshRenderer.sharedMaterial == null)
                {
                    errors.Add($"{path} Mesh child is missing a MeshRenderer with a generated material.");
                }
                else if (!MaterialHasAssignedTexture(meshRenderer.sharedMaterial))
                {
                    errors.Add($"{path} Mesh material has no upgrade texture assigned.");
                }
            }
        }

        private static bool MaterialHasAssignedTexture(Material material)
        {
            if (material == null)
            {
                return false;
            }

            if (material.mainTexture != null)
            {
                return true;
            }

            if (material.HasProperty("_MainTex") && material.GetTexture("_MainTex") != null)
            {
                return true;
            }

            return material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") != null;
        }

        private static void ValidateVector(List<string> errors, string path, string label, Vector3 actual, Vector3 expected, float tolerance)
        {
            if (Mathf.Abs(actual.x - expected.x) > tolerance ||
                Mathf.Abs(actual.y - expected.y) > tolerance ||
                Mathf.Abs(actual.z - expected.z) > tolerance)
            {
                errors.Add($"{path} {label} is {actual}, expected {expected}.");
            }
        }

        private static void AddIfMissing(List<string> missing, string projectPath)
        {
            if (!File.Exists(ToAbsolutePath(projectPath)))
            {
                missing.Add(projectPath);
            }
        }

        private static void CopyGeneratedAsset(string sourceProjectPath, string destinationProjectPath, bool overwriteExisting)
        {
            string source = ToAbsolutePath(sourceProjectPath);
            if (!File.Exists(source))
            {
                Debug.LogWarning($"Missing source asset: {sourceProjectPath}");
                return;
            }

            string destination = ToAbsolutePath(destinationProjectPath);
            if (!overwriteExisting && File.Exists(destination))
            {
                return;
            }

            File.Copy(source, destination, true);
        }

        private static void CopyIfMissing(string source, string destination)
        {
            if (!File.Exists(source) || File.Exists(destination))
            {
                return;
            }

            File.Copy(source, destination, false);
        }

        private static void WriteYamlAsset(string projectPath, string contents)
        {
            DeleteAssetFile(projectPath);

            string absolutePath = ToAbsolutePath(projectPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            File.WriteAllText(absolutePath, contents);

            AssetDatabase.ImportAsset(projectPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static void DeleteAssetFile(string projectPath)
        {
            if (AssetDatabase.LoadMainAssetAtPath(projectPath) != null || File.Exists(ToAbsolutePath(projectPath)))
            {
                AssetDatabase.DeleteAsset(projectPath);
            }

            string absolutePath = ToAbsolutePath(projectPath);
            string metaPath = absolutePath + ".meta";
            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
            }

            if (File.Exists(metaPath))
            {
                File.Delete(metaPath);
            }
        }

        private static void DeleteGeneratedAssetTree(string projectPath)
        {
            if (!projectPath.Equals(GeneratedRoot, StringComparison.Ordinal) &&
                !projectPath.StartsWith(GeneratedRoot + "/", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Refusing to delete non-generated asset path: {projectPath}");
            }

            if (AssetDatabase.IsValidFolder(projectPath) || AssetDatabase.LoadMainAssetAtPath(projectPath) != null)
            {
                AssetDatabase.DeleteAsset(projectPath);
            }

            string absolutePath = ToAbsolutePath(projectPath);
            if (Directory.Exists(absolutePath))
            {
                Directory.Delete(absolutePath, true);
            }
            else if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
            }

            string metaPath = absolutePath + ".meta";
            if (File.Exists(metaPath))
            {
                File.Delete(metaPath);
            }
        }

        private static string BuildValueYaml(ScriptReference scriptReference, UpgradeDefinition upgrade)
        {
            return
                "%YAML 1.1\n" +
                "%TAG !u! tag:unity3d.com,2011:\n" +
                "--- !u!114 &11400000\n" +
                "MonoBehaviour:\n" +
                "  m_ObjectHideFlags: 0\n" +
                "  m_CorrespondingSourceObject: {fileID: 0}\n" +
                "  m_PrefabInstance: {fileID: 0}\n" +
                "  m_PrefabAsset: {fileID: 0}\n" +
                "  m_GameObject: {fileID: 0}\n" +
                "  m_Enabled: 1\n" +
                "  m_EditorHideFlags: 0\n" +
                $"  m_Script: {{fileID: {scriptReference.FileId}, guid: {scriptReference.Guid}, type: 3}}\n" +
                $"  m_Name: {YamlString(upgrade.AssetName + " Value")}\n" +
                "  m_EditorClassIdentifier: \n" +
                $"  valueMin: {YamlFloat(upgrade.Price)}\n" +
                $"  valueMax: {YamlFloat(upgrade.Price)}\n";
        }

        private static string BuildItemYaml(ScriptReference scriptReference, UpgradeDefinition upgrade, string valueGuid)
        {
            string valueReference = string.IsNullOrEmpty(valueGuid)
                ? "{fileID: 0}"
                : $"{{fileID: 11400000, guid: {valueGuid}, type: 2}}";

            return
                "%YAML 1.1\n" +
                "%TAG !u! tag:unity3d.com,2011:\n" +
                "--- !u!114 &11400000\n" +
                "MonoBehaviour:\n" +
                "  m_ObjectHideFlags: 0\n" +
                "  m_CorrespondingSourceObject: {fileID: 0}\n" +
                "  m_PrefabInstance: {fileID: 0}\n" +
                "  m_PrefabAsset: {fileID: 0}\n" +
                "  m_GameObject: {fileID: 0}\n" +
                "  m_Enabled: 1\n" +
                "  m_EditorHideFlags: 0\n" +
                $"  m_Script: {{fileID: {scriptReference.FileId}, guid: {scriptReference.Guid}, type: 3}}\n" +
                $"  m_Name: {YamlString(upgrade.AssetName)}\n" +
                "  m_EditorClassIdentifier: \n" +
                "  disabled: 0\n" +
                $"  itemName: {YamlString(upgrade.AssetName)}\n" +
                "  itemNameLocalized: {fileID: 0}\n" +
                $"  description: {YamlString(upgrade.Description)}\n" +
                "  itemType: 3\n" +
                "  emojiIcon: 0\n" +
                "  itemVolume: 6\n" +
                "  itemSecretShopType: 0\n" +
                "  colorPreset: {fileID: 0}\n" +
                "  prefab:\n" +
                "    prefabName: \n" +
                "    resourcePath: \n" +
                "    bundle: {fileID: 0}\n" +
                $"  value: {valueReference}\n" +
                "  maxAmount: 1\n" +
                "  maxAmountInShop: 1\n" +
                "  minPlayerCount: 1\n" +
                "  maxPurchase: 1\n" +
                $"  maxPurchaseAmount: {upgrade.MaxPurchaseAmount}\n" +
                "  spawnRotationOffset: {x: 0, y: 0, z: 0, w: 1}\n" +
                "  physicalItem: 1\n";
        }

        private static string YamlString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return "\"" + value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", string.Empty)
                .Replace("\n", "\\n") + "\"";
        }

        private static string YamlFloat(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string FindRepoManagedPath()
        {
            string envManaged = Environment.GetEnvironmentVariable("REPO_MANAGED_DIR");
            if (!string.IsNullOrWhiteSpace(envManaged) && Directory.Exists(envManaged))
            {
                return envManaged;
            }

            string envGame = Environment.GetEnvironmentVariable("REPO_GAME_DIR");
            if (!string.IsNullOrWhiteSpace(envGame))
            {
                string managed = Path.Combine(envGame, "REPO_Data", "Managed");
                if (Directory.Exists(managed))
                {
                    return managed;
                }
            }

            string[] candidates =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "steamapps", "common", "REPO", "REPO_Data", "Managed"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam", "steamapps", "common", "REPO", "REPO_Data", "Managed")
            };

            return candidates.FirstOrDefault(Directory.Exists);
        }

        private static bool ShouldImportGameAssembly(string fileName)
        {
            if (fileName.StartsWith("UnityEngine", StringComparison.OrdinalIgnoreCase) ||
                fileName.StartsWith("System", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("mscorlib.dll", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("netstandard.dll", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string assemblyName = Path.GetFileNameWithoutExtension(fileName);
            if (assemblyName.StartsWith("Unity.", StringComparison.OrdinalIgnoreCase) && IsAssemblyAlreadyLoaded(assemblyName))
            {
                return false;
            }

            return true;
        }

        private static bool IsAssemblyAlreadyLoaded(string assemblyName)
        {
            foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name.Equals(assemblyName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static Type FindLoadedType(string typeName)
        {
            foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static Type FindLoadedRepoType(string typeName, Type requiredBaseType)
        {
            Type fallback = null;

            foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(typeName);
                if (type == null)
                {
                    continue;
                }

                if (!requiredBaseType.IsAssignableFrom(type))
                {
                    continue;
                }

                if (IsRepoAssembly(assembly.GetName().Name))
                {
                    return type;
                }

                fallback ??= type;
            }

            return fallback;
        }

        private static bool TryGetRepoMonoScriptReference(string typeName, Type requiredBaseType, out ScriptReference scriptReference)
        {
            foreach (MonoScript monoScript in MonoImporter.GetAllRuntimeMonoScripts())
            {
                if (TryGetMonoScriptReference(monoScript, typeName, requiredBaseType, true, out scriptReference))
                {
                    return true;
                }
            }

            foreach (string dllPath in new[]
            {
                ThirdPartyRoot + "/Assembly-CSharp.dll",
                ThirdPartyRoot + "/Assembly-CSharp-firstpass.dll"
            })
            {
                if (!File.Exists(ToAbsolutePath(dllPath)))
                {
                    continue;
                }

                foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(dllPath))
                {
                    if (asset is MonoScript monoScript &&
                        TryGetMonoScriptReference(monoScript, typeName, requiredBaseType, true, out scriptReference))
                    {
                        return true;
                    }
                }
            }

            scriptReference = default;
            return false;
        }

        private static bool TryGetMonoScriptReference(
            MonoScript monoScript,
            string typeName,
            Type requiredBaseType,
            bool requireRepoAssembly,
            out ScriptReference scriptReference)
        {
            scriptReference = default;
            if (monoScript == null)
            {
                return false;
            }

            Type scriptClass;
            try
            {
                scriptClass = monoScript.GetClass();
            }
            catch
            {
                return false;
            }

            if (scriptClass == null ||
                !scriptClass.Name.Equals(typeName, StringComparison.Ordinal) ||
                !requiredBaseType.IsAssignableFrom(scriptClass))
            {
                return false;
            }

            if (requireRepoAssembly && !IsRepoAssembly(scriptClass.Assembly.GetName().Name))
            {
                return false;
            }

            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(monoScript, out string guid, out long localId))
            {
                return false;
            }

            scriptReference = new ScriptReference(guid, localId);
            return true;
        }

        private static bool IsRepoAssembly(string assemblyName)
        {
            return assemblyName.Equals("Assembly-CSharp", StringComparison.OrdinalIgnoreCase) ||
                   assemblyName.Equals("Assembly-CSharp-firstpass", StringComparison.OrdinalIgnoreCase);
        }

        private static void LogTypeCandidate(string typeName, Type requiredBaseType)
        {
            Debug.Log($"Looking for '{typeName}' assignable to {requiredBaseType.FullName}.");

            bool found = false;
            foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(typeName);
                if (type == null)
                {
                    continue;
                }

                found = true;
                Debug.Log(
                    $"Candidate {type.FullName} from {assembly.GetName().Name}; " +
                    $"base={type.BaseType?.FullName ?? "<none>"}; " +
                    $"assignable={requiredBaseType.IsAssignableFrom(type)}; " +
                    $"repoAssembly={IsRepoAssembly(assembly.GetName().Name)}");
            }

            if (!found)
            {
                Debug.LogWarning($"No loaded candidate found for '{typeName}'.");
            }
        }

        private static void LogRuntimeMonoScriptCandidate(string typeName, Type requiredBaseType)
        {
            Debug.Log($"Looking for runtime MonoScript '{typeName}' assignable to {requiredBaseType.FullName}.");

            bool found = false;
            foreach (MonoScript monoScript in MonoImporter.GetAllRuntimeMonoScripts())
            {
                Type scriptClass;
                try
                {
                    scriptClass = monoScript.GetClass();
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"Could not inspect MonoScript {monoScript.name}: {exception.Message}");
                    continue;
                }

                if (scriptClass == null || !scriptClass.Name.Equals(typeName, StringComparison.Ordinal))
                {
                    continue;
                }

                found = true;
                string path = AssetDatabase.GetAssetPath(monoScript);
                string reference = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(monoScript, out string guid, out long localId)
                    ? $"guid={guid}; fileID={localId}"
                    : "no serializable script reference";

                Debug.Log(
                    $"MonoScript candidate {scriptClass.FullName} from {scriptClass.Assembly.GetName().Name}; " +
                    $"path={path}; {reference}; " +
                    $"base={scriptClass.BaseType?.FullName ?? "<none>"}; " +
                    $"assignable={requiredBaseType.IsAssignableFrom(scriptClass)}; " +
                    $"repoAssembly={IsRepoAssembly(scriptClass.Assembly.GetName().Name)}");
            }

            if (!found)
            {
                Debug.LogWarning($"No runtime MonoScript candidate found for '{typeName}'.");
            }
        }

        private static string ItemPath(UpgradeDefinition upgrade)
        {
            return ItemRoot + "/" + SafeFileName(upgrade.AssetName) + ".asset";
        }

        private static string ValuePath(UpgradeDefinition upgrade)
        {
            return ValueRoot + "/" + SafeFileName(upgrade.AssetName) + " Value.asset";
        }

        private static string PrefabPath(UpgradeDefinition upgrade)
        {
            return PrefabRoot + "/" + SafeFileName(upgrade.AssetName) + ".prefab";
        }

        private static string SafeFileName(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }

            return value;
        }

        private static string ProjectRoot
        {
            get { return Directory.GetParent(Application.dataPath).FullName; }
        }

        private static string ToAbsolutePath(string projectPath)
        {
            return Path.GetFullPath(Path.Combine(ProjectRoot, projectPath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string ToProjectPath(string absolutePath)
        {
            string fullPath = Path.GetFullPath(absolutePath).Replace('\\', '/');
            string root = ProjectRoot.Replace('\\', '/').TrimEnd('/');
            if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Path is outside the Unity project: {absolutePath}");
            }

            return fullPath.Substring(root.Length + 1);
        }

        private readonly struct UpgradeDefinition
        {
            public readonly string AssetName;
            public readonly string SourceTextureName;
            public readonly string Description;
            public readonly Color Tint;
            public readonly float Price;
            public readonly int MaxPurchaseAmount;

            public UpgradeDefinition(string assetName, string sourceTextureName, string description, Color tint, float price, int maxPurchaseAmount)
            {
                AssetName = assetName;
                SourceTextureName = sourceTextureName;
                Description = description;
                Tint = tint;
                Price = price;
                MaxPurchaseAmount = maxPurchaseAmount;
            }
        }

        private readonly struct ScriptReference
        {
            public readonly string Guid;
            public readonly long FileId;

            public ScriptReference(string guid, long fileId)
            {
                Guid = guid;
                FileId = fileId;
            }
        }
    }
}
