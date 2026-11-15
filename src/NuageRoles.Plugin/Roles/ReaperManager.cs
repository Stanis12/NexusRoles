using HarmonyLib;
using NuageRoles;
using Photon.Pun;
using UnityEngine;

namespace NuageRoles.Roles
{
    public class ReaperManager : MonoBehaviourPun
    {
        public bool isReaper;
        public int kills;
        public int enemyDeathTimer = 0;

        private void Start()
        {
            ReaperEventListener.Ensure();
        }

        private void OnDestroy()
        {
            try
            {
                if (photonView != null && photonView.IsMine && photonView.ViewID != 0)
                {
                    PhotonNetwork.RemoveRPCs(photonView);
                }
            }
            catch (System.Exception ex)
            {
                NuageRolesPlugin.Logger.LogWarning((object)$"[RpMr] OnDestroy: RemoveRPCs exception: {ex}");
            }
        }

        [PunRPC]
        public void setReaperStatusRPC(string steamID, bool isReaper)
        {
            if (RunManager.instance == null || RunManager.instance.levelCurrent == null)
            {
                NuageRolesPlugin.Logger.LogWarning((object)$"[RpMr] setReaperStatusRPC: RunManager/level null - ignoring for {steamID}");
                return;
            }
            if (!RunManager.instance.levels.Contains(RunManager.instance.levelCurrent))
            {
                NuageRolesPlugin.Logger.LogInfo((object)$"[RpMr] setReaperStatusRPC: not a runlevel - ignoring for {steamID}");
                return;
            }

            var avatar = SemiFunc.PlayerAvatarGetFromSteamID(steamID);
            if (avatar != null)
            {
                var rm = avatar.GetComponent<ReaperManager>();
                if (rm != null)
                {
                    if (rm.isReaper != isReaper)
                    {
                        NuageRolesPlugin.Logger.LogInfo((object)$"[RpMr] setReaperStatusRPC: received for {steamID}, setTo={isReaper}");
                        rm.isReaper = isReaper;
#if DEBUG
                        NuageRolesPlugin.Logger.LogInfo((object)$"[RpMr] setReaperStatusRPC: Applied isReaper={isReaper} to avatar steamID={steamID}");
#endif
                    }
                }
            }

            if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
                ReaperEvents.RaiseReaperStatusChange(steamID, isReaper);
            }
            else
            {
                NuageRolesPlugin.Logger.LogWarning((object)$"[RpMr] setReaperStatusRPC: Not in room/connected - not notifying master for {steamID}");
            }
        }

        public void OnEnemyKilled(PlayerAvatar killerAvatar)
        {
            if (killerAvatar == null) return;
            string killerSteam = killerAvatar.steamID;

            if (PhotonNetwork.IsMasterClient)
            {
                var reapers = ReaperEventListener.GetMasterReapers();
                NuageRolesPlugin.Logger.LogInfo((object)$"[RpMr] Master local kill: broadcasting buffs for {reapers.Length} reapers (killer={killerSteam})");
                if (reapers.Length > 0)
                    ReaperEvents.RaiseApplyBuffs(killerSteam, reapers);
                return;
            }

            ReaperEvents.RaiseRequestApplyBuffs(killerSteam);
#if DEBUG
            NuageRolesPlugin.Logger.LogInfo((object)$"[RpMr] Client requested master to apply buffs for killer={killerSteam}");
#endif
        }

        internal void ApplyReaperStats(PlayerAvatar avatar)
        {
            if (avatar == null) return;
            int health = (int)AccessTools.Field(typeof(PlayerHealth), "health").GetValue(avatar.playerHealth);
            int maxHealthBefore = (int)AccessTools.Field(typeof(PlayerHealth), "maxHealth").GetValue(avatar.playerHealth);
            int maxHealth = maxHealthBefore + Mathf.Max(0, NuageRolesPlugin.Settings.ReaperMaxHealthBonusPerKill.Value);
            AccessTools.Field(typeof(PlayerHealth), "maxHealth").SetValue(avatar.playerHealth, maxHealth);

            int healAmount = Mathf.Min(Mathf.Max(0, NuageRolesPlugin.Settings.ReaperHealAmountPerKill.Value), Mathf.Max(0, maxHealth - health));
            if (healAmount > 0)
            {
                avatar.playerHealth.Heal(healAmount);
            }
#if DEBUG
            NuageRolesPlugin.Logger.LogInfo((object)$"[RpMr] ApplyReaperStats: applied local buff to {avatar.steamID} (health {health} -> {(int)AccessTools.Field(typeof(PlayerHealth), "health").GetValue(avatar.playerHealth)}, max {maxHealthBefore} -> {maxHealth})");
#endif
        }

        private void Update()
        {
            if (enemyDeathTimer > 0) enemyDeathTimer--;
        }
    }
}
