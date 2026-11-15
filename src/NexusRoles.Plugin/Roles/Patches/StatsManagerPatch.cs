using System.Collections.Generic;
using HarmonyLib;
using NexusRoles;

namespace NexusRoles.Roles.Patches
{
    [HarmonyPatch(typeof(StatsManager))]
    [HarmonyAfter("REPOLib")]
    internal static class StatsManagerPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Start")]
        private static void StatsPrefix(StatsManager __instance)
        {
            if (!__instance.dictionaryOfDictionaries.ContainsKey("playerUpgradeManaRegeneration"))
            {
                __instance.dictionaryOfDictionaries.Add("playerUpgradeManaRegeneration", new Dictionary<string, int>());
            }

            if (!__instance.dictionaryOfDictionaries.ContainsKey("playerUpgradeScoutCooldownReduction"))
            {
                __instance.dictionaryOfDictionaries.Add("playerUpgradeScoutCooldownReduction", new Dictionary<string, int>());
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("RunStartStats")]
        private static void RunStartStatsPostfix()
        {
            NexusRolesPlugin.AttachRegisteredUpgradeDictionaries();
        }
    }
}
