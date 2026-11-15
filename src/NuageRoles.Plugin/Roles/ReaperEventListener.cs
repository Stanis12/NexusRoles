using System.Collections.Generic;
using ExitGames.Client.Photon;
using NuageRoles;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace NuageRoles.Roles
{
    internal class ReaperEventListener : MonoBehaviourPunCallbacks, IOnEventCallback
    {
        private static bool _created;
        private static readonly HashSet<string> masterReapers = new HashSet<string>();

        public static void Ensure()
        {
            if (_created) return;
            _created = true;
            var go = new GameObject("NuageRoles_ReaperEventListener");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<ReaperEventListener>();
            NuageRolesPlugin.Logger.LogInfo("ReaperEventListener created!");
        }

        public static string[] GetMasterReapers()
        {
            lock (masterReapers)
            {
                var arr = new string[masterReapers.Count];
                masterReapers.CopyTo(arr);
                return arr;
            }
        }

        public override void OnEnable() => PhotonNetwork.AddCallbackTarget(this);
        public override void OnDisable() => PhotonNetwork.RemoveCallbackTarget(this);

        private static int GetNonNegativeInt(object[] data, int index, int fallback)
        {
            if (index >= data.Length || data[index] == null)
            {
                return Mathf.Max(0, fallback);
            }

            if (data[index] is int i)
            {
                return Mathf.Max(0, i);
            }

            if (data[index] is short s)
            {
                return Mathf.Max(0, s);
            }

            if (data[index] is byte b)
            {
                return b;
            }

            return Mathf.Max(0, fallback);
        }

        public override void OnJoinedRoom()
        {
            lock (masterReapers) masterReapers.Clear();
#if DEBUG
            NuageRolesPlugin.Logger.LogInfo("[RpEsLr] OnJoinedRoom: cleared master registry");
#endif
        }

        public override void OnLeftRoom()
        {
            lock (masterReapers) masterReapers.Clear();
#if DEBUG
            NuageRolesPlugin.Logger.LogInfo("[RpEsLr] OnLeftRoom: cleared master registry");
#endif
        }

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            lock (masterReapers) masterReapers.Clear();
#if DEBUG
            NuageRolesPlugin.Logger.LogInfo("[RpEsLr] OnMasterClientSwitched: cleared master registry (will repopulate by status events)");
#endif
        }

        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code == ReaperEvents.EV_REAPER_STATUS_CHANGE)
            {
                if (photonEvent.CustomData is not object[] data || data.Length < 2)
                    return;

                string? steam = data[0] as string;
                if (string.IsNullOrEmpty(steam))
                    return;

                bool isReaper = false;

                if (data[1] is bool b)
                    isReaper = b;
                else if (data[1] is byte bt)
                    isReaper = bt != 0;
                else if (data[1] is int i)
                    isReaper = i != 0;
                else
                    return;

                if (PhotonNetwork.IsMasterClient)
                {
                    lock (masterReapers)
                    {
                        if (isReaper)
                        {
                            if (masterReapers.Add(steam))
                                NuageRolesPlugin.Logger.LogInfo($"[RpEsLr] Master: Registered reaper {steam}");
                        }
                        else
                        {
                            if (masterReapers.Remove(steam))
                                NuageRolesPlugin.Logger.LogInfo($"[RpEsLr] Master: Unregistered reaper {steam}");
                        }
                    }
                }
                return;
            }

            if (photonEvent.Code == ReaperEvents.EV_REQUEST_REAPER_BUFFS)
            {
                if (!PhotonNetwork.IsMasterClient) return;
                string killer = string.Empty;

                if (photonEvent.CustomData is object[] data && data.Length > 0)
                    killer = data[0] as string ?? string.Empty;

                string[] reapers;
                lock (masterReapers)
                {
                    reapers = new string[masterReapers.Count];
                    masterReapers.CopyTo(reapers);
                }

                NuageRolesPlugin.Logger.LogInfo($"[RpEsLr] Master: received EV_REQUEST_REAPER_BUFFS for killer={killer} reapersCount={reapers.Length}");
                if (reapers.Length > 0)
                {
                    ReaperEvents.RaiseApplyBuffs(killer, reapers);
                }
                else
                {
                    NuageRolesPlugin.Logger.LogInfo("[RpEsLr] Master: no reapers registered - nothing to broadcast");
                }
                return;
            }

            if (photonEvent.Code == ReaperEvents.EV_APPLY_REAPER_BUFFS)
            {
                try
                {
                    if (photonEvent.CustomData is not object[] data || data.Length < 2)
                        return;

                    string killer = data[0] as string ?? string.Empty;

                    string[] reapers;
                    if (data[1] is string[] arr)
                        reapers = arr;
                    else
                        return;

                    int maxHealthBonus = GetNonNegativeInt(data, 2, NuageRolesPlugin.Settings.ReaperMaxHealthBonusPerKill.Value);
                    int healAmountPerKill = GetNonNegativeInt(data, 3, NuageRolesPlugin.Settings.ReaperHealAmountPerKill.Value);
                    int buffCooldownFrames = GetNonNegativeInt(data, 4, NuageRolesPlugin.Settings.ReaperBuffCooldownFrames.Value);
#if DEBUG
                    NuageRolesPlugin.Logger.LogInfo($"[RpEsLr] giveReaperStatsRPC: received via event from killer={killer} reapersCount={reapers.Length}");
#endif
                    foreach (var rSteam in reapers)
                    {
                        var avatar = SemiFunc.PlayerAvatarGetFromSteamID(rSteam);
                        if (avatar == null)
                        {
                            NuageRolesPlugin.Logger.LogWarning($"[RpEsLr] giveReaperStatsRPC: cannot find avatar for steamID={rSteam} on this client");
                            continue;
                        }
                        var rm = avatar.GetComponent<ReaperManager>();
                        if (rm == null)
                        {
                            NuageRolesPlugin.Logger.LogWarning($"[RpEsLr] giveReaperStatsRPC: avatar {rSteam} has no ReaperManager on this client");
                            continue;
                        }

                        int health = (int)HarmonyLib.AccessTools.Field(typeof(PlayerHealth), "health").GetValue(avatar.playerHealth);
                        if (health <= 0)
                        {
                            NuageRolesPlugin.Logger.LogInfo($"[RpEsLr] giveReaperStatsRPC: avatar {rSteam} is dead (health={health}) - skipping");
                            continue;
                        }
                        if (rm.enemyDeathTimer > 0)
                        {
                            NuageRolesPlugin.Logger.LogInfo($"[RpEsLr] giveReaperStatsRPC: avatar {rSteam} in cooldown (enemyDeathTimer={rm.enemyDeathTimer}) - skipping");
                            continue;
                        }

                        int maxBefore = (int)HarmonyLib.AccessTools.Field(typeof(PlayerHealth), "maxHealth").GetValue(avatar.playerHealth);
                        int healthBefore = health;
                        int maxAfter = maxBefore + maxHealthBonus;
                        HarmonyLib.AccessTools.Field(typeof(PlayerHealth), "maxHealth").SetValue(avatar.playerHealth, maxAfter);

                        int healAmount = Mathf.Min(healAmountPerKill, Mathf.Max(0, maxAfter - healthBefore));
                        if (healAmount > 0)
                        {
                            avatar.playerHealth.Heal(healAmount);
                        }

                        rm.kills = 0;
                        rm.enemyDeathTimer = buffCooldownFrames;
#if DEBUG
                        NuageRolesPlugin.Logger.LogInfo(
                            $"[RpEsLr] giveReaperStatsRPC: Applied buff to {rSteam} (health {healthBefore} -> {(int)HarmonyLib.AccessTools.Field(typeof(PlayerHealth), "health").GetValue(avatar.playerHealth)}, max {maxBefore} -> {maxAfter})");
#endif
                    }
                }
                catch (System.Exception ex)
                {
                    NuageRolesPlugin.Logger.LogError($"[RpEsLr]: Event handler exception: {ex}");
                }
            }
        }
    }
}
