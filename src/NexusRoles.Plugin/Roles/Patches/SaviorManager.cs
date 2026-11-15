using System.Collections;
using System.Reflection;
using HarmonyLib;
using NexusRoles;
using Photon.Pun;
using UnityEngine;

namespace NexusRoles.Roles.Patches
{
    public sealed class SaviorManager : MonoBehaviourPun
    {
        private const float ReviveCooldownSeconds = 120f;
        private const float ReviveRange = 6f;

        private static readonly FieldInfo? PlayerDeathHeadField = AccessTools.Field(typeof(PlayerAvatar), "playerDeathHead");
        private PlayerAvatar? playerAvatar;
        private bool isSavior;
        private float messageUntil;
        private string message = string.Empty;

        public bool IsSavior => isSavior;

        private void Awake()
        {
            playerAvatar = GetComponent<PlayerAvatar>();
        }

        public void SetRoleState(bool enabled)
        {
            isSavior = enabled;
            if (!GameManager.Multiplayer() || photonView == null)
            {
                return;
            }

            photonView.RPC("SetSaviorStateRPC", RpcTarget.AllBuffered, enabled);
        }

        [PunRPC]
        public void SetSaviorStateRPC(bool enabled, PhotonMessageInfo info)
        {
            if (info.Sender != null && photonView != null && info.Sender.ActorNumber != photonView.OwnerActorNr)
            {
                return;
            }

            isSavior = enabled;
        }

        private void Update()
        {
            if (!isSavior || playerAvatar == null || PlayerAvatar.instance != playerAvatar
                || !SemiFunc.RunIsLevel() || SemiFunc.RunIsShop() || playerAvatar.playerHealth.health <= 0)
            {
                return;
            }

            if (Input.GetKeyDown(NexusRolesPlugin.Settings.SaviorKey.Value))
            {
                TryReviveNearestAlly();
            }
        }

        private void TryReviveNearestAlly()
        {
            if (playerAvatar == null) return;

            PlayerAvatar? target = FindNearestFallenAlly();
            if (target == null)
            {
                ShowMessage(NexusLocalization.ReviveNoTarget);
                return;
            }

            int currentHealth = playerAvatar.playerHealth.health;
            bool sacrificeAll = NexusLocalization.IsAllSacrifice(NexusRolesPlugin.Settings.SaviorSacrificeMode.Value);
            int sacrificeAmount = sacrificeAll
                ? currentHealth
                : Mathf.CeilToInt(currentHealth * Mathf.Clamp(NexusRolesPlugin.Settings.SaviorPartialCostPercent.Value, 10, 90) / 100f);

            if (!sacrificeAll && sacrificeAmount >= currentHealth)
            {
                sacrificeAmount = currentHealth - 1;
            }

            if (sacrificeAmount <= 0 || (!sacrificeAll && currentHealth <= 1))
            {
                ShowMessage(NexusLocalization.ReviveNotEnoughHealth);
                return;
            }

            int reviveHealth = Mathf.Clamp(NexusRolesPlugin.Settings.SaviorReviveHealth.Value, 1, 100);
            string donorSteamId = playerAvatar.steamID;
            string targetSteamId = target.steamID;

            if (!GameManager.Multiplayer())
            {
                ApplySacrificeLocally(sacrificeAmount, targetSteamId, reviveHealth);
                return;
            }

            if (PunManager.instance == null || PunManager.instance.photonView == null)
            {
                return;
            }

            ShowMessage(NexusLocalization.Text("Revive requested.", "Запрошено воскрешение.", "Запит на воскресіння надіслано."));
            PunManager.instance.photonView.RPC("RequestSaviorReviveRPC", RpcTarget.MasterClient,
                donorSteamId, targetSteamId, sacrificeAmount, reviveHealth);
        }

        private PlayerAvatar? FindNearestFallenAlly()
        {
            if (playerAvatar == null) return null;

            PlayerAvatar? closest = null;
            float closestDistance = ReviveRange;
            foreach (PlayerAvatar candidate in SemiFunc.PlayerGetAll())
            {
                if (candidate == null || candidate == playerAvatar || candidate.playerHealth == null
                    || candidate.playerHealth.health > 0 || !HasDeathHead(candidate))
                {
                    continue;
                }

                float distance = Vector3.Distance(playerAvatar.transform.position, candidate.transform.position);
                if (distance <= closestDistance)
                {
                    closest = candidate;
                    closestDistance = distance;
                }
            }

            return closest;
        }

        internal static bool HasDeathHead(PlayerAvatar avatar)
        {
            object? deathHead = PlayerDeathHeadField?.GetValue(avatar);
            return deathHead is Object unityObject && unityObject != null;
        }

        [PunRPC]
        public void ApplySacrificeRPC(int amount, string targetSteamId, int reviveHealth, PhotonMessageInfo info)
        {
            if (playerAvatar == null || !photonView.IsMine || !isSavior
                || playerAvatar.playerHealth.health < amount || amount <= 0)
            {
                return;
            }

            ApplySacrificeLocally(amount, targetSteamId, reviveHealth);
        }

        private void ApplySacrificeLocally(int amount, string targetSteamId, int reviveHealth)
        {
            if (playerAvatar == null || playerAvatar.playerHealth.health < amount || amount <= 0)
            {
                return;
            }

            playerAvatar.playerHealth.Hurt(amount, false, -1, true);
            if (GameManager.Multiplayer())
            {
                StartCoroutine(ConfirmSacrificeAfterSync(amount, targetSteamId, reviveHealth));
            }
            else
            {
                HealthManager healthManager = PunManager.instance.GetComponent<HealthManager>();
                healthManager.CompleteSaviorRevive(playerAvatar.steamID, targetSteamId, amount, reviveHealth, 0);
            }
        }

        private IEnumerator ConfirmSacrificeAfterSync(int amount, string targetSteamId, int reviveHealth)
        {
            yield return new WaitForSeconds(0.25f);

            if (playerAvatar == null || PunManager.instance == null || PunManager.instance.photonView == null)
            {
                yield break;
            }

            PunManager.instance.photonView.RPC("ConfirmSaviorSacrificeRPC", RpcTarget.MasterClient,
                playerAvatar.steamID, targetSteamId, amount, reviveHealth);
        }

        [PunRPC]
        public void SetReviveCooldownRPC(int sacrificedHealth)
        {
            ShowMessage(NexusLocalization.ReviveSuccess + sacrificedHealth.ToString() + NexusLocalization.ReviveSuccessSuffix);
            messageUntil = Time.time + 4f;
        }

        [PunRPC]
        public void ShowReviveFailureRPC(int reason)
        {
            ShowMessage(reason == 1 ? NexusLocalization.ReviveNotEnoughHealth
                : reason == 2 ? NexusLocalization.ReviveCooldown
                : NexusLocalization.ReviveNoTarget);
        }

        private void ShowMessage(string value)
        {
            message = value;
            messageUntil = Time.time + 3f;
        }

        private void OnGUI()
        {
            if (messageUntil <= Time.time || playerAvatar == null || PlayerAvatar.instance != playerAvatar)
            {
                return;
            }

            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                fontSize = Mathf.Clamp(Screen.height / 45, 14, 28)
            };
            GUI.Label(new Rect(Screen.width * 0.15f, Screen.height * 0.72f, Screen.width * 0.7f, 80f), message, style);
        }
    }
}
