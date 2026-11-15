using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using NexusRoles.Roles;
using NexusRoles.Roles.Patches;
using Photon.Pun;
using REPOLib;
using REPOLib.Modules;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace NexusRoles
{
    [BepInPlugin(NexusRolesPlugin.PluginGuid, NexusRolesPlugin.PluginName, NexusRolesPlugin.PluginVersion)]
    [BepInDependency("REPOLib", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("nickklmao.menulib", "2.6.2")]
    [BepInDependency("nickklmao.repoconfig", "1.2.7")]
    [BepInDependency("PxntxrezStudio.REPOConomyPlus", BepInDependency.DependencyFlags.SoftDependency)]
    public class NexusRolesPlugin : BaseUnityPlugin
    {
        // Keep the legacy GUID so BepInEx treats this as the existing plugin and reuses its config.
        public const string PluginGuid = "com.theohay.nuageroles";
        public const string PluginName = "NexusRoles";
        public const string PluginVersion = "4.1.1";
        public const string AssetBundleName = "NexusRoles_assets";
        private const string ManaUpgradeStatKey = "playerUpgradeManaRegeneration";
        private const string ScoutUpgradeStatKey = "playerUpgradeScoutCooldownReduction";
        private static readonly Vector3 UpgradeColliderSize = new Vector3(0.17882313f, 0.26898482f, 0.07221024f);
        private static readonly RoleUpgradeDefinition[] RoleUpgradeDefinitions =
        {
            new RoleUpgradeDefinition("Mana Regeneration Upgrade", "ManaRegeneration", ManaUpgradeStatKey),
            new RoleUpgradeDefinition("Scout Cooldown Upgrade", "ScoutCooldownReduction", ScoutUpgradeStatKey)
        };
        private static PhysAttribute? upgradePhysAttributePreset;

        public static NexusRolesPlugin Instance { get; private set; } = null!;
        internal static new ManualLogSource Logger { get; private set; } = null!;
        public static NexusRolesConfig Settings { get; private set; } = null!;

        public readonly Harmony harmony = new Harmony(PluginGuid);

        public static Font newFont = null!;

        public static guiManager GUIinstance = null!;

        public static string getPath()
        {
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), AssetBundleName);
        }

        private void Awake()
        {
            Instance = this;
            Logger = base.Logger;
            Settings = new NexusRolesConfig(Config);
            Settings.Bind();
            Settings.RegisterMenu(delegate
            {
                if (GUIinstance != null)
                {
                    GUIinstance.UpdateManaDisplay();
                }
            });
            if ((UnityEngine.Object)(object)Instance == null)
            {
                Instance = this;
            }
            harmony.PatchAll(typeof(NexusRolesPlugin));
            harmony.PatchAll(typeof(Sender));
            Logger.LogInfo($"Plugin {PluginName} v{PluginVersion} loaded successfully.");
            SceneManager.sceneLoaded += delegate
            {
                if (GUIinstance == null)
                {
                    GameObject val = new GameObject();
                    GUIinstance = val.AddComponent<guiManager>();
                    GUIinstance.text = "";
                    GUIinstance.color = Color.cyan;
                    UnityEngine.Object.DontDestroyOnLoad((UnityEngine.Object)(object)val);
                }
            };
            harmony.PatchAll(typeof(PunManagerPatch));
            harmony.PatchAll(typeof(PlayerAvatarPatch));
            harmony.PatchAll(typeof(StatsManagerPatch));
            harmony.PatchAll(typeof(ShopManagerPatch));
            harmony.PatchAll(typeof(EnemyHealthPatch));
            BundleLoader.LoadBundle(getPath(), delegate (AssetBundle assetBundle)
            {
                foreach (RoleUpgradeDefinition upgrade in RoleUpgradeDefinitions)
                {
                    try
                    {
                        string upgradeName = upgrade.AssetName;
                        Item item = assetBundle.LoadAsset<Item>(upgradeName);
                        if (item == null)
                        {
                            Logger.LogError($"Failed loading Item asset '{upgradeName}' from bundle.");
                            continue;
                        }

                        // load prefabs
                        GameObject prefab = assetBundle.LoadAsset<GameObject>(upgradeName);

                        if (prefab == null)
                        {
                            Logger.LogError($"Failed loading prefab for upgrade '{upgradeName}'. Make sure the prefab is included in the bundle.");
                            continue;
                        }

                        // unique name
                        string assetName = $"REPORoles_upgrade {upgradeName}";
                        prefab.name = assetName;
                        item.name = assetName;
                        item.itemName = $"{upgradeName}";
                        item.physicalItem = true;

                        if (!ValidateUpgradeItem(item, upgradeName))
                        {
                            continue;
                        }

                        ItemAttributes? itemAttributes = PrepareUpgradePrefab(prefab, item, upgrade);
                        if (itemAttributes == null)
                        {
                            continue;
                        }

                        // Register the prefab/item with REPOLib
                        var prefabRef = Items.RegisterItem(itemAttributes);
                        if (prefabRef == null)
                        {
                            Logger.LogWarning($"Items.RegisterItem returned null for '{upgradeName}'. It may already be registered or there was an error.");
                        }
                        else
                        {
                            Logger.LogInfo($"Registered upgrade item '{upgradeName}'.");
                        }

                        // Register the upgrades with REPOLib
                        PlayerUpgrade? playerUpgrade = Upgrades.RegisterUpgrade(
                            upgrade.UpgradeId,
                            item,
                            (player, level) => ApplyRoleUpgradeLevel(upgrade, player, level),
                            (player, level) => ApplyRoleUpgradeLevel(upgrade, player, level));
                        if (playerUpgrade == null)
                        {
                            if (Upgrades.TryGetUpgrade(upgrade.UpgradeId, out PlayerUpgrade? existingUpgrade) && existingUpgrade != null)
                            {
                                AttachUpgradeDictionary(upgrade, existingUpgrade);
                                Logger.LogWarning($"Role upgrade '{upgradeName}' was already registered; attached existing REPOLib id '{upgrade.UpgradeId}'.");
                            }
                            else
                            {
                                Logger.LogWarning($"Failed to register role upgrade '{upgradeName}' with REPOLib id '{upgrade.UpgradeId}'.");
                            }
                        }
                        else
                        {
                            AttachUpgradeDictionary(upgrade, playerUpgrade);
                            Logger.LogInfo($"Registered role upgrade '{upgradeName}' with REPOLib id '{upgrade.UpgradeId}' and stat key '{upgrade.StatKey}'.");
                        }
                    }
                    catch (System.Exception e)
                    {
                        Logger.LogError($"Exception while registering upgrade '{upgrade.AssetName}': {e}");
                    }
                }
            }, false);
        }

        private static bool ValidateUpgradeItem(Item item, string upgradeName)
        {
            if (item.value == null)
            {
                Logger.LogError($"Upgrade Item asset '{upgradeName}' has no Value asset. Fix the bundle and rebuild it.");
                return false;
            }

            if (item.itemType != SemiFunc.itemType.item_upgrade)
            {
                Logger.LogError($"Upgrade Item asset '{upgradeName}' has itemType '{item.itemType}', expected '{SemiFunc.itemType.item_upgrade}'. Fix the bundle and rebuild it.");
                return false;
            }

            if (item.itemVolume != SemiFunc.itemVolume.upgrade)
            {
                Logger.LogError($"Upgrade Item asset '{upgradeName}' has itemVolume '{item.itemVolume}', expected '{SemiFunc.itemVolume.upgrade}'. Fix the bundle and rebuild it.");
                return false;
            }

            if (!item.maxPurchase || item.maxPurchaseAmount <= 0)
            {
                Logger.LogError($"Upgrade Item asset '{upgradeName}' must enable maxPurchase and set maxPurchaseAmount above zero. Fix the bundle and rebuild it.");
                return false;
            }

            if (item.maxAmount <= 0 || item.maxAmountInShop <= 0 || item.minPlayerCount <= 0)
            {
                Logger.LogError($"Upgrade Item asset '{upgradeName}' has invalid shop counts. Fix the bundle and rebuild it.");
                return false;
            }

            Logger.LogInfo($"Loaded upgrade Item asset '{upgradeName}' with value seed {item.value.valueMin}-{item.value.valueMax}.");
            return true;
        }

        private static ItemAttributes? PrepareUpgradePrefab(GameObject prefab, Item item, RoleUpgradeDefinition upgrade)
        {
            string upgradeName = upgrade.AssetName;
            Rigidbody rigidbody = prefab.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                Logger.LogError($"Upgrade prefab '{upgradeName}' has no Rigidbody. Fix the bundle prefab and rebuild it.");
                return null;
            }

            PhotonView photonView = prefab.GetComponent<PhotonView>() ?? prefab.AddComponent<PhotonView>();
            PhotonTransformView photonTransformView = prefab.GetComponent<PhotonTransformView>() ?? prefab.AddComponent<PhotonTransformView>();
            RoomVolumeCheck roomVolumeCheck = prefab.GetComponent<RoomVolumeCheck>() ?? prefab.AddComponent<RoomVolumeCheck>();
            if (roomVolumeCheck.CurrentRooms == null)
            {
                roomVolumeCheck.CurrentRooms = new List<RoomVolume>();
            }
            roomVolumeCheck.Continuous = true;
            roomVolumeCheck.currentSize = UpgradeColliderSize;
            roomVolumeCheck.CheckPosition = Vector3.zero;

            PhysGrabObject physGrabObject = prefab.GetComponent<PhysGrabObject>() ?? prefab.AddComponent<PhysGrabObject>();
            physGrabObject.rb = rigidbody;
            physGrabObject.clientNonKinematic = true;
            physGrabObject.overrideTagsAndLayers = true;
            physGrabObject.ignoreGrabPointCentering = false;
            if (physGrabObject.playerGrabbing == null)
            {
                physGrabObject.playerGrabbing = new List<PhysGrabber>();
            }

            ConfigureUpgradeTemplateChildren(prefab.transform, upgradeName);
            BoxCollider? grabCollider = ConfigureGrabCollider(prefab.transform, physGrabObject, upgradeName);
            if (grabCollider == null)
            {
                return null;
            }

            PhysGrabObjectImpactDetector impactDetector = prefab.GetComponent<PhysGrabObjectImpactDetector>() ?? prefab.AddComponent<PhysGrabObjectImpactDetector>();
            ConfigureUpgradeImpactDetector(impactDetector);

            NotValuableObject notValuableObject = prefab.GetComponent<NotValuableObject>() ?? prefab.AddComponent<NotValuableObject>();
            notValuableObject.physAttributePreset = GetUpgradePhysAttributePreset();
            notValuableObject.hasHealth = false;
            notValuableObject.healthDestroy = false;
            notValuableObject.healthMax = 0;

            ItemAttributes itemAttributes = prefab.GetComponent<ItemAttributes>() ?? prefab.AddComponent<ItemAttributes>();
            itemAttributes.item = item;
            itemAttributes.costOffset = new Vector3(0f, 0.18f, 0f);
            SetFieldIfAvailable(typeof(ItemAttributes), itemAttributes, "roomVolumeCheck", roomVolumeCheck);
            SetFieldIfAvailable(typeof(ItemAttributes), itemAttributes, "physGrabObject", physGrabObject);
            SetFieldIfAvailable(typeof(ItemAttributes), itemAttributes, "photonView", photonView);
            SetFieldIfAvailable(typeof(PhysGrabObject), physGrabObject, "roomVolumeCheck", roomVolumeCheck);
            SetFieldIfAvailable(typeof(PhysGrabObject), physGrabObject, "photonView", photonView);
            SetFieldIfAvailable(typeof(PhysGrabObject), physGrabObject, "photonTransformView", photonTransformView);
            SetFieldIfAvailable(typeof(PhysGrabObject), physGrabObject, "impactDetector", impactDetector);

            REPOLibItemUpgrade repoLibUpgrade = prefab.GetComponent<REPOLibItemUpgrade>() ?? prefab.AddComponent<REPOLibItemUpgrade>();
            AccessTools.Field(typeof(REPOLibItemUpgrade), "_upgradeId").SetValue(repoLibUpgrade, upgrade.UpgradeId);
            if (!ConfigureUpgradeConsumption(prefab, upgrade))
            {
                return null;
            }

            NexusRolesUpgradeRuntimeBinder runtimeBinder = prefab.GetComponent<NexusRolesUpgradeRuntimeBinder>() ?? prefab.AddComponent<NexusRolesUpgradeRuntimeBinder>();
            runtimeBinder.upgradeName = upgradeName;

            Logger.LogInfo($"Prepared upgrade prefab '{upgradeName}' with runtime shop item components, REPOLib upgrade id '{upgrade.UpgradeId}', and collider '{grabCollider.name}'.");
            return itemAttributes;
        }

        private static bool ConfigureUpgradeConsumption(GameObject prefab, RoleUpgradeDefinition upgrade)
        {
            string upgradeName = upgrade.AssetName;
            EnsureParticleEffects(prefab.transform, upgradeName);

            ItemToggle itemToggle = prefab.GetComponent<ItemToggle>() ?? prefab.AddComponent<ItemToggle>();
            itemToggle.playSound = false;
            itemToggle.autoTurnOffWhenEquipped = true;
            itemToggle.onToggle = new UnityEvent();

            ItemUpgrade itemUpgrade = prefab.GetComponent<ItemUpgrade>() ?? prefab.AddComponent<ItemUpgrade>();
            itemUpgrade.isPlayerUpgrade = true;
            itemUpgrade.upgradeEvent = new UnityEvent();
            return true;
        }

        internal static void AttachRegisteredUpgradeDictionaries()
        {
            foreach (RoleUpgradeDefinition upgrade in RoleUpgradeDefinitions)
            {
                if (Upgrades.TryGetUpgrade(upgrade.UpgradeId, out PlayerUpgrade? playerUpgrade) && playerUpgrade != null)
                {
                    AttachUpgradeDictionary(upgrade, playerUpgrade);
                }
            }
        }

        private static void AttachUpgradeDictionary(RoleUpgradeDefinition upgrade, PlayerUpgrade playerUpgrade)
        {
            Dictionary<string, int>? dictionary = EnsureUpgradeDictionary(upgrade.StatKey);
            if (dictionary == null)
            {
                return;
            }

            playerUpgrade.PlayerDictionary = dictionary;
        }

        private static void ApplyRoleUpgradeLevel(RoleUpgradeDefinition upgrade, PlayerAvatar player, int level)
        {
            if (player == null)
            {
                return;
            }

            string steamId = SemiFunc.PlayerGetSteamID(player);
            if (string.IsNullOrEmpty(steamId))
            {
                Logger.LogWarning($"Could not apply role upgrade '{upgrade.AssetName}': player steam ID was empty.");
                return;
            }

            Upgrader.SetStat(level, steamId, upgrade.StatKey);
            if (player.isLocal)
            {
                ApplyLocalRoleUpgradeEffects(upgrade.StatKey);
            }

            Logger.LogInfo($"Applied role upgrade '{upgrade.AssetName}' at level {level} for {steamId}.");
        }

        private static Dictionary<string, int>? EnsureUpgradeDictionary(string statKey)
        {
            if (StatsManager.instance == null)
            {
                return null;
            }

            if (!StatsManager.instance.dictionaryOfDictionaries.TryGetValue(statKey, out Dictionary<string, int> dictionary))
            {
                dictionary = new Dictionary<string, int>();
                StatsManager.instance.dictionaryOfDictionaries.Add(statKey, dictionary);
            }

            return dictionary;
        }

        private static void ApplyLocalRoleUpgradeEffects(string statKey)
        {
            if (statKey == ManaUpgradeStatKey)
            {
                Update_ManaRegeneration();
            }
            else if (statKey == ScoutUpgradeStatKey)
            {
                Update_ScoutCooldown();
            }
        }

        private static void ConfigureUpgradeImpactDetector(PhysGrabObjectImpactDetector impactDetector)
        {
            impactDetector.particleDisable = true;
            impactDetector.particleBreakSmokeDisable = true;
            impactDetector.hasFreezeFrameOnBreakParticles = false;
            impactDetector.particleMultiplier = 0f;
            impactDetector.playerHurtDisable = true;
            impactDetector.slidingDisable = true;
            impactDetector.indestructibleBreakEffects = false;
            impactDetector.canHurtLogic = false;
            impactDetector.destroyDisable = false;
            impactDetector.isCart = false;
            impactDetector.fragility = 0f;
            impactDetector.durability = 100f;
            SetFieldIfAvailable(typeof(PhysGrabObjectImpactDetector), impactDetector, "impactDisable", true);
            SetFieldIfAvailable(typeof(PhysGrabObjectImpactDetector), impactDetector, "breakLogic", false);
            SetFieldIfAvailable(typeof(PhysGrabObjectImpactDetector), impactDetector, "audioActive", false);
            SetFieldIfAvailable(typeof(PhysGrabObjectImpactDetector), impactDetector, "breakLevelHeavy", 0);
            SetFieldIfAvailable(typeof(PhysGrabObjectImpactDetector), impactDetector, "breakLevelMedium", 0);
            SetFieldIfAvailable(typeof(PhysGrabObjectImpactDetector), impactDetector, "breakLevelLight", 0);
        }

        private static PhysAttribute GetUpgradePhysAttributePreset()
        {
            if (upgradePhysAttributePreset == null)
            {
                upgradePhysAttributePreset = ScriptableObject.CreateInstance<PhysAttribute>();
                upgradePhysAttributePreset.name = "Repo Roles Upgrade PhysAttribute";
                upgradePhysAttributePreset.mass = 0.25f;
            }

            return upgradePhysAttributePreset!;
        }

        private static void ConfigureUpgradeTemplateChildren(Transform root, string upgradeName)
        {
            Transform forceGrabPoint = root.Find("Force Grab Point");
            if (forceGrabPoint != null)
            {
                SetLayerIfAvailable(forceGrabPoint.gameObject, "PhysGrabObject");
                TrySetTag(forceGrabPoint.gameObject, "Untagged");
            }
            else
            {
                Logger.LogWarning($"Upgrade prefab '{upgradeName}' is missing child 'Force Grab Point'. Fix the bundle prefab and rebuild it.");
            }

            if (root.Find("ItemEquipCube") == null)
            {
                Logger.LogWarning($"Upgrade prefab '{upgradeName}' is missing child 'ItemEquipCube'. Fix the bundle prefab and rebuild it.");
            }
        }

        private static void EnsureParticleEffects(Transform root, string upgradeName)
        {
            Transform particleEffects = root.Find("Particle Effects");
            if (particleEffects == null)
            {
                GameObject particleEffectsObject = new GameObject("Particle Effects");
                particleEffects = particleEffectsObject.transform;
                particleEffects.SetParent(root, false);
                Logger.LogWarning($"Upgrade prefab '{upgradeName}' is missing child 'Particle Effects'. Created an empty runtime placeholder.");
            }

            particleEffects.gameObject.SetActive(false);
        }

        private static BoxCollider? ConfigureGrabCollider(Transform root, PhysGrabObject physGrabObject, string upgradeName)
        {
            Transform colliderTransform = root.Find("Semi Box Collider");
            if (colliderTransform == null)
            {
                Logger.LogError($"Upgrade prefab '{upgradeName}' is missing child 'Semi Box Collider'. Fix the bundle prefab and rebuild it.");
                return null;
            }

            SetLayerIfAvailable(colliderTransform.gameObject, "PhysGrabObject");
            TrySetTag(colliderTransform.gameObject, "Phys Grab Object");

            BoxCollider collider = colliderTransform.GetComponent<BoxCollider>();
            if (collider == null)
            {
                Logger.LogError($"Upgrade prefab '{upgradeName}' child 'Semi Box Collider' has no BoxCollider. Fix the bundle prefab and rebuild it.");
                return null;
            }

            if (colliderTransform.GetComponent<PhysGrabObjectBoxCollider>() == null)
            {
                colliderTransform.gameObject.AddComponent<PhysGrabObjectBoxCollider>();
            }

            PhysGrabObjectCollider physCollider = colliderTransform.GetComponent<PhysGrabObjectCollider>() ?? colliderTransform.gameObject.AddComponent<PhysGrabObjectCollider>();
            SetFieldIfAvailable(typeof(PhysGrabObjectCollider), physCollider, "physGrabObject", physGrabObject);

            return collider;
        }

        private static void SetLayerIfAvailable(GameObject gameObject, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0)
            {
                gameObject.layer = layer;
            }
        }

        private static void TrySetTag(GameObject gameObject, string tag)
        {
            try
            {
                gameObject.tag = tag;
            }
            catch (UnityException)
            {
                Logger.LogWarning($"Could not set upgrade prefab tag '{tag}'.");
            }
        }

        private static void SetFieldIfAvailable(Type type, object instance, string fieldName, object value)
        {
            FieldInfo field = AccessTools.Field(type, fieldName);
            if (field != null)
            {
                field.SetValue(instance, value);
            }
        }

        private sealed class RoleUpgradeDefinition
        {
            public RoleUpgradeDefinition(string assetName, string upgradeId, string statKey)
            {
                AssetName = assetName;
                UpgradeId = upgradeId;
                StatKey = statKey;
            }

            public string AssetName { get; }

            public string UpgradeId { get; }

            public string StatKey { get; }
        }

        private sealed class NexusRolesUpgradeRuntimeBinder : MonoBehaviour
        {
            public string upgradeName = string.Empty;

            private ItemToggle? itemToggle;
            private ItemUpgrade? itemUpgrade;
            private REPOLibItemUpgrade? repoLibUpgrade;
            private ItemAttributes? itemAttributes;
            private bool eventsBound;

            private void Awake()
            {
                RefreshComponents();
                BindRuntimeEvents();
            }

            private void Start()
            {
                RefreshComponents();
                BindRuntimeEvents();
                EnableToggleWhenUsable();
            }

            private void Update()
            {
                if (itemToggle == null || itemUpgrade == null || repoLibUpgrade == null || itemAttributes == null)
                {
                    RefreshComponents();
                }

                if (!eventsBound)
                {
                    BindRuntimeEvents();
                }

                EnableToggleWhenUsable();
            }

            private void RefreshComponents()
            {
                itemToggle = GetComponent<ItemToggle>();
                itemUpgrade = GetComponent<ItemUpgrade>();
                repoLibUpgrade = GetComponent<REPOLibItemUpgrade>();
                itemAttributes = GetComponent<ItemAttributes>();
            }

            private void BindRuntimeEvents()
            {
                if (itemToggle == null || itemUpgrade == null || repoLibUpgrade == null)
                {
                    return;
                }

                itemToggle.playSound = false;
                itemToggle.autoTurnOffWhenEquipped = true;
                itemToggle.onToggle ??= new UnityEvent();
                itemToggle.onToggle.RemoveAllListeners();
                itemToggle.onToggle.AddListener(itemUpgrade.PlayerUpgrade);

                itemUpgrade.isPlayerUpgrade = true;
                itemUpgrade.upgradeEvent ??= new UnityEvent();
                itemUpgrade.upgradeEvent.RemoveAllListeners();
                itemUpgrade.upgradeEvent.AddListener(repoLibUpgrade.Upgrade);

                SetFieldIfAvailable(typeof(REPOLibItemUpgrade), repoLibUpgrade, "_itemToggle", itemToggle);
                eventsBound = true;
            }

            private void EnableToggleWhenUsable()
            {
                if (itemToggle != null && !itemToggle.enabled && IsUsable())
                {
                    itemToggle.enabled = true;
                }
            }

            private bool IsUsable()
            {
                return !SemiFunc.RunIsShop() || IsPurchased();
            }

            private bool IsPurchased()
            {
                if (StatsManager.instance == null || itemAttributes?.item == null)
                {
                    return false;
                }

                return StatsManager.instance.itemsPurchased.TryGetValue(itemAttributes.item.name, out int purchasedCount)
                    && purchasedCount > 0;
            }
        }

        public static void Update_ManaRegeneration()
        {
            if (LevelGenerator.Instance.Generated && !SemiFunc.MenuLevel())
            {
                int stat = Upgrader.GetStat(PlayerAvatar.instance.steamID, "playerUpgradeManaRegeneration");
                guiManager.manaRegenRate = Upgrader.GetStat(PlayerAvatar.instance.steamID, "playerUpgradeManaRegeneration");
            }
        }

        public static void Update_ScoutCooldown()
        {
            if (LevelGenerator.Instance.Generated && !SemiFunc.MenuLevel())
            {
                int stat = Upgrader.GetStat(PlayerAvatar.instance.steamID, "playerUpgradeScoutCooldownReduction");
                ScoutMarker.reductionUpgrades = Upgrader.GetStat(PlayerAvatar.instance.steamID, "playerUpgradeScoutCooldownReduction");
            }
        }
    }
}
