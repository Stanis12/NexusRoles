using BepInEx.Logging;
using HarmonyLib;
using NuageRoles;
using UnityEngine;

namespace NuageRoles.Roles
{
    [HarmonyPatch(typeof(TruckScreenText))]
    internal static class Sender
    {
        private static bool hasRanOnce;

        public static ClassManager manager = new ClassManager();

        [HarmonyPatch("ArrowPointAtGoalLogic")]
        [HarmonyPrefix]
        private static void PrefixMethod()
        {
            if (!hasRanOnce && !SemiFunc.RunIsShop())
            {
                manager.assignRoleFromConfig(PlayerController.instance);
                NuageRolesPlugin.Logger.LogInfo((object)"Successfully rolled role!");
                hasRanOnce = true;
            }
        }

        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        private static void StartPrefix()
        {
            hasRanOnce = false;
        }
    }
}
