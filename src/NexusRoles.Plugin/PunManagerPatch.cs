using HarmonyLib;
using NexusRoles.Roles;
using NexusRoles.Roles.Patches;
using Photon.Pun;
using Unity.VisualScripting;
using UnityEngine;

namespace NexusRoles
{
    [HarmonyPatch(typeof(PunManager))]
    internal static class PunManagerPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("Start")]
        private static void Start_Postfix(PunManager __instance)
        {
            if (((Object)__instance).GetComponent<StrengthManager>() == null)
                ((Object)__instance).AddComponent<StrengthManager>();

            if (((Object)__instance).GetComponent<HealthManager>() == null)
                ((Object)__instance).AddComponent<HealthManager>();

            if (((Object)__instance).GetComponent<ScoutMarker>() == null)
                ((Object)__instance).AddComponent<ScoutMarker>();
        }
    }
}
