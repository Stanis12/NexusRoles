using System.Collections.Generic;
using NexusRoles;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace NexusRoles.Roles.Patches
{
    public class HealthManager : MonoBehaviour
    {
        internal PhotonView photonView = null!;

        private readonly Dictionary<string, float> saviorCooldowns = new Dictionary<string, float>();
        private readonly Dictionary<string, SaviorRequest> pendingSaviorRequests = new Dictionary<string, SaviorRequest>();

        private sealed class SaviorRequest
        {
            public string TargetSteamId = string.Empty;
            public int HealthBefore;
            public int SacrificeAmount;
            public int ReviveHealth;
        }

        private void Start()
        {
            photonView = ((Component)this).GetComponent<PhotonView>();
        }

        [PunRPC]
        internal void setHealthRPC(string steamID, int maxHealth, int health)
        {
            PlayerAvatar val = SemiFunc.PlayerAvatarGetFromSteamID(steamID);
            if (val != null)
            {
                val.playerHealth.maxHealth = maxHealth;
                val.playerHealth.health = health;
            }
        }

        [PunRPC]
        internal void RequestSaviorReviveRPC(string donorSteamId, string targetSteamId, int sacrificeAmount, int reviveHealth, PhotonMessageInfo info)
        {
            if (GameManager.Multiplayer() && !PhotonNetwork.IsMasterClient)
            {
                return;
            }

            PlayerAvatar? donor = SemiFunc.PlayerAvatarGetFromSteamID(donorSteamId);
            PlayerAvatar? target = SemiFunc.PlayerAvatarGetFromSteamID(targetSteamId);
            SaviorManager? donorSavior = donor != null ? donor.GetComponent<SaviorManager>() : null;
            if (donor == null || target == null || donor == target || donor.playerHealth.health <= 0
                || target.playerHealth.health > 0 || !SaviorManager.HasDeathHead(target)
                || donorSavior == null || !donorSavior.IsSavior
                || Vector3.Distance(donor.transform.position, target.transform.position) > 6f)
            {
                NotifySaviorFailure(donor, 0);
                return;
            }

            if (GameManager.Multiplayer())
            {
                Player? sender = info.Sender;
                if (sender == null || donor.photonView == null || sender.ActorNumber != donor.photonView.OwnerActorNr)
                {
                    return;
                }
            }

            if (saviorCooldowns.TryGetValue(donorSteamId, out float readyAt) && Time.time < readyAt)
            {
                NotifySaviorFailure(donor, 2);
                return;
            }

            if (pendingSaviorRequests.ContainsKey(donorSteamId))
            {
                return;
            }

            int healthBefore = donor.playerHealth.health;
            if (sacrificeAmount <= 0 || sacrificeAmount > healthBefore)
            {
                NotifySaviorFailure(donor, 1);
                return;
            }

            var request = new SaviorRequest
            {
                TargetSteamId = targetSteamId,
                HealthBefore = healthBefore,
                SacrificeAmount = sacrificeAmount,
                ReviveHealth = Mathf.Clamp(reviveHealth, 1, 100)
            };

            if (!GameManager.Multiplayer())
            {
                CompleteSaviorRevive(donorSteamId, targetSteamId, sacrificeAmount, request.ReviveHealth, 0);
                return;
            }

            pendingSaviorRequests[donorSteamId] = request;
            SaviorManager? manager = donor.GetComponent<SaviorManager>();
            if (manager == null || manager.photonView == null)
            {
                pendingSaviorRequests.Remove(donorSteamId);
                return;
            }

            manager.photonView.RPC("ApplySacrificeRPC", RpcTarget.All, sacrificeAmount, targetSteamId, request.ReviveHealth);
        }

        [PunRPC]
        internal void ConfirmSaviorSacrificeRPC(string donorSteamId, string targetSteamId, int sacrificeAmount, int reviveHealth, PhotonMessageInfo info)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            PlayerAvatar? donor = SemiFunc.PlayerAvatarGetFromSteamID(donorSteamId);
            if (donor == null || donor.photonView == null || info.Sender == null
                || info.Sender.ActorNumber != donor.photonView.OwnerActorNr)
            {
                return;
            }

            CompleteSaviorRevive(donorSteamId, targetSteamId, sacrificeAmount, reviveHealth, info.Sender.ActorNumber);
        }

        internal void CompleteSaviorRevive(string donorSteamId, string targetSteamId, int sacrificeAmount, int reviveHealth, int senderActorNumber)
        {
            PlayerAvatar? donor = SemiFunc.PlayerAvatarGetFromSteamID(donorSteamId);
            PlayerAvatar? target = SemiFunc.PlayerAvatarGetFromSteamID(targetSteamId);
            SaviorRequest? request = null;

            if (GameManager.Multiplayer())
            {
                if (!PhotonNetwork.IsMasterClient || !pendingSaviorRequests.TryGetValue(donorSteamId, out request)
                    || request.TargetSteamId != targetSteamId || request.SacrificeAmount != sacrificeAmount)
                {
                    return;
                }
                pendingSaviorRequests.Remove(donorSteamId);
            }

            if (donor == null || target == null || target.playerHealth.health > 0 || !SaviorManager.HasDeathHead(target))
            {
                NotifySaviorFailure(donor, 0);
                return;
            }

            int expectedHealth = request != null ? request.HealthBefore - sacrificeAmount : donor.playerHealth.health;
            if (GameManager.Multiplayer() && donor.playerHealth.health > expectedHealth)
            {
                NotifySaviorFailure(donor, 1);
                return;
            }

            target.Revive(false);
            int restoreTo = Mathf.Clamp(reviveHealth, 1, target.playerHealth.maxHealth);
            target.playerHealth.HealOther(Mathf.Max(0, restoreTo - 1), true);

            saviorCooldowns[donorSteamId] = Time.time + 120f;
            SaviorManager? manager = donor.GetComponent<SaviorManager>();
            if (manager != null && manager.photonView != null)
            {
                manager.photonView.RPC("SetReviveCooldownRPC", RpcTarget.All, sacrificeAmount);
            }
        }

        private static void NotifySaviorFailure(PlayerAvatar? donor, int reason)
        {
            if (donor == null) return;
            SaviorManager? manager = donor.GetComponent<SaviorManager>();
            if (manager != null && manager.photonView != null && GameManager.Multiplayer())
            {
                manager.photonView.RPC("ShowReviveFailureRPC", RpcTarget.All, reason);
            }
        }
    }
}
