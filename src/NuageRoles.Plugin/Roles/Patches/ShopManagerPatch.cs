using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using NuageRoles;
using UnityEngine;

namespace NuageRoles.Roles.Patches
{
    [HarmonyPatch(typeof(ShopManager))]
    public static class ShopManagerPatch
    {
        [HarmonyPatch(nameof(ShopManager.GetAllItemsFromStatsManager))]
        [HarmonyPostfix]
        private static void GetAllItemsFromStatsManagerPostfix(ShopManager __instance)
        {
            try
            {
                // Only the master rolls the shared shop in multiplayer.
                if (SemiFunc.IsMultiplayer() && !SemiFunc.IsMasterClientOrSingleplayer())
                {
                    return;
                }

                List<Item> allValidUpgrades = GetAllValidUpgrades(__instance);
                FilterAndMaintainUpgradeCount(__instance.potentialItemUpgrades, allValidUpgrades);

                __instance.potentialItems.RemoveAll(item =>
                    item != null &&
                    item.itemName != null &&
                    IsDisabledUpgrade(item));
            }
            catch (System.Exception e)
            {
                NuageRolesPlugin.Logger.LogError($"Error in ShopManager patch: {e}");
            }
        }

        private static List<Item> GetAllValidUpgrades(ShopManager shopManager)
        {
            List<Item> validUpgrades = new List<Item>();

            foreach (var item in shopManager.potentialItems.Concat(shopManager.potentialItemUpgrades))
            {
                if (item == null || item.itemName == null) continue;
                if (item.itemType != SemiFunc.itemType.item_upgrade) continue;
                if (IsDisabledUpgrade(item)) continue;

                validUpgrades.Add(item);
            }

            return validUpgrades.Distinct().ToList();
        }

        private static void FilterAndMaintainUpgradeCount(List<Item> upgradeList, List<Item> validUpgradesPool)
        {
            if (upgradeList == null) return;

            List<Item> filteredList = new List<Item>();

            foreach (var item in upgradeList)
            {
                if (item == null || item.itemName == null)
                {
                    filteredList.Add(item!);
                    continue;
                }

                if (IsDisabledUpgrade(item))
                {
                    if (validUpgradesPool.Count > 0)
                    {
                        int randomIndex = UnityEngine.Random.Range(0, validUpgradesPool.Count);
                        filteredList.Add(validUpgradesPool[randomIndex]);
                        NuageRolesPlugin.Logger.LogInfo($"Replaced {item.itemName} with {validUpgradesPool[randomIndex].itemName}");
                    }
                }
                else
                {
                    filteredList.Add(item);
                }
            }

            upgradeList.Clear();
            upgradeList.AddRange(filteredList);
        }

        private static bool IsDisabledUpgrade(Item item)
        {
            if (item == null || item.itemName == null) return false;

            bool isMageUpgrade = item.itemName.Contains("Mana Regeneration Upgrade");
            bool isScoutUpgrade = item.itemName.Contains("Scout Cooldown Upgrade");

            if (!isMageUpgrade && !isScoutUpgrade) return false;

            if (!NuageRolesPlugin.Settings.EnableRoleUpgrades.Value)
            {
                return true;
            }

            if (isMageUpgrade && !NuageRolesPlugin.Settings.EnableMage.Value)
            {
                return true;
            }

            if (isScoutUpgrade && !NuageRolesPlugin.Settings.EnableScout.Value)
            {
                return true;
            }

            return false;
        }
    }
}
