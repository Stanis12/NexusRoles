using System.Collections;
using NexusRoles;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace NexusRoles.Roles.Patches
{
    public class EngineerManager : MonoBehaviourPun
    {
        private const float ChargeVerifyDelaySeconds = 0.5f;
        private const float ChargeRequestTimeoutSeconds = 8f;

        private PlayerAvatar? playerAvatar;
        private float sacrificedEnergy;
        private float regenTimer;
        private bool chargeRequestPending;
        private int nextChargeRequestId;
        private int pendingChargeRequestId;
        private int pendingItemViewId;
        private int pendingControllerActorNumber;
        private float pendingStaminaCost;

        private void Awake()
        {
            playerAvatar = GetComponent<PlayerAvatar>();
        }

        private void Update()
        {
            if (!ClassManager.isEngineer) return;

            if (playerAvatar == null || PlayerAvatar.instance == null || playerAvatar != PlayerAvatar.instance)
                return;

            if (!SemiFunc.RunIsLevel() || SemiFunc.RunIsShop())
                return;

            float staminaCost = Mathf.Max(0f, NexusRolesPlugin.Settings.EngineerChargeStaminaCost.Value);
            float staminaRestoreAmount = Mathf.Max(0f, NexusRolesPlugin.Settings.EngineerStaminaRestoreAmount.Value);
            float staminaRestoreInterval = Mathf.Max(1f, NexusRolesPlugin.Settings.EngineerStaminaRestoreIntervalSeconds.Value);

            if (PlayerAvatar.instance.playerHealth.health <= 0)
            {
                if (sacrificedEnergy > 0f)
                {
                    PlayerController.instance.EnergyStart += sacrificedEnergy;
                    sacrificedEnergy = 0f;
                    regenTimer = 0f;
                }

                return;
            }

            if (sacrificedEnergy > 0f)
            {
                regenTimer += Time.deltaTime;
                while (regenTimer >= staminaRestoreInterval && sacrificedEnergy > 0f)
                {
                    regenTimer -= staminaRestoreInterval;
                    float restoredEnergy = Mathf.Min(staminaRestoreAmount, sacrificedEnergy);
                    PlayerController.instance.EnergyStart += restoredEnergy;
                    PlayerController.instance.EnergyCurrent += restoredEnergy;
                    sacrificedEnergy -= restoredEnergy;
                }

                if (sacrificedEnergy <= 0f)
                    regenTimer = 0f;
            }
            else
            {
                regenTimer = 0f;
            }

            if (Input.GetKeyDown(NexusRolesPlugin.Settings.ChargeItemKey.Value))
                TryChargeHeldItem(staminaCost);
        }

        private void TryChargeHeldItem(float staminaCost)
        {
            if (chargeRequestPending || PlayerController.instance.EnergyStart < staminaCost) return;

            PhysGrabber? grabber = PhysGrabber.instance;
            if (grabber == null || !grabber.grabbed || grabber.grabbedPhysGrabObject == null) return;

            PhysGrabObject grabbedObject = grabber.grabbedPhysGrabObject;
            ItemBattery? battery = FindBattery(grabbedObject);
            if (battery == null)
            {
                NexusRolesPlugin.Logger.LogInfo("Engineer: no ItemBattery found on held object '" + grabbedObject.name + "'.");
                return;
            }

            float lifeBefore = battery.batteryLife;
            if (lifeBefore >= 100f) return;

            int totalBars = Mathf.Max(1, battery.batteryBars);
            float lifeExpected = Mathf.Min(lifeBefore + 100f / totalBars, 100f);
            PhotonView? itemView = FindItemPhotonView(grabbedObject, battery);

            // Offline or locally controlled items can still be charged directly.
            if (itemView == null || !PhotonNetwork.InRoom || itemView.IsMine)
            {
                ApplyLocalCharge(battery, grabbedObject.name, staminaCost, lifeBefore, lifeExpected, totalBars);
                return;
            }

            // Battery state should be changed by the current Photon controller. Requesting
            // ownership is asynchronous and Request mode also needs an owner callback, so
            // send the charge request to the controller instead of writing before transfer.
            Player? controller = itemView.Controller ?? PhotonNetwork.MasterClient;
            if (controller == null)
            {
                NexusRolesPlugin.Logger.LogWarning("Engineer: no Photon controller found for '" + grabbedObject.name + "'.");
                return;
            }

            if (PhotonNetwork.LocalPlayer != null && controller.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                ApplyLocalCharge(battery, grabbedObject.name, staminaCost, lifeBefore, lifeExpected, totalBars);
                return;
            }

            RequestChargeFromController(controller, itemView, grabbedObject.name, staminaCost);
        }

        private void ApplyLocalCharge(ItemBattery battery, string itemName, float staminaCost, float lifeBefore, float lifeExpected, int totalBars)
        {
            battery.batteryLife = lifeExpected;
            SpendStamina(staminaCost);
            NexusRolesPlugin.Logger.LogInfo("Engineer: charged '" + itemName + "' from " + lifeBefore.ToString("0.#") + " to " + lifeExpected.ToString("0.#") + " (bars: " + totalBars + ").");
            StartCoroutine(VerifyCharge(battery, itemName, lifeBefore, lifeExpected));
        }

        private void RequestChargeFromController(Player controller, PhotonView itemView, string itemName, float staminaCost)
        {
            if (photonView == null || !photonView.IsMine)
            {
                NexusRolesPlugin.Logger.LogWarning("Engineer: cannot send a charge request from the local player's PhotonView.");
                return;
            }

            int requestId = ++nextChargeRequestId;
            chargeRequestPending = true;
            pendingChargeRequestId = requestId;
            pendingItemViewId = itemView.ViewID;
            pendingControllerActorNumber = controller.ActorNumber;
            pendingStaminaCost = staminaCost;

            ReserveStamina(staminaCost);
            NexusRolesPlugin.Logger.LogInfo("Engineer: requesting charge for '" + itemName + "' from Photon controller " + controller.ActorNumber + ".");
            photonView.RPC(nameof(ChargeItemOnControllerRPC), controller, itemView.ViewID, requestId);
            StartCoroutine(ChargeRequestTimeout(requestId));
        }

        [PunRPC]
        public void ChargeItemOnControllerRPC(int itemViewId, int requestId, PhotonMessageInfo info)
        {
            if (info.Sender == null || photonView == null || info.Sender.ActorNumber != photonView.OwnerActorNr)
                return;

            PhotonView? itemView = PhotonView.Find(itemViewId);
            ItemBattery? battery = itemView != null ? FindBattery(itemView) : null;
            bool success = false;
            float lifeBefore = 0f;
            float lifeAfter = 0f;

            if (itemView != null && itemView.IsMine && battery != null)
            {
                lifeBefore = battery.batteryLife;
                if (lifeBefore < 100f)
                {
                    int bars = Mathf.Max(1, battery.batteryBars);
                    lifeAfter = Mathf.Min(lifeBefore + 100f / bars, 100f);
                    battery.batteryLife = lifeAfter;
                    success = lifeAfter > lifeBefore;
                }
            }

            NexusRolesPlugin.Logger.LogInfo("Engineer: controller handled charge request " + requestId + " for item view " + itemViewId + " (success: " + success + ").");
            photonView.RPC(nameof(ChargeItemResultRPC), info.Sender, requestId, itemViewId, success, lifeBefore, lifeAfter);
        }

        [PunRPC]
        public void ChargeItemResultRPC(int requestId, int itemViewId, bool success, float lifeBefore, float lifeAfter, PhotonMessageInfo info)
        {
            if (!chargeRequestPending || requestId != pendingChargeRequestId || itemViewId != pendingItemViewId)
                return;

            if (info.Sender == null || info.Sender.ActorNumber != pendingControllerActorNumber || photonView == null || !photonView.IsMine)
                return;

            float staminaCost = pendingStaminaCost;
            ClearPendingCharge();

            if (!success)
            {
                RefundStamina(staminaCost);
                NexusRolesPlugin.Logger.LogWarning("Engineer: controller rejected charge request " + requestId + "; stamina was refunded.");
                return;
            }

            sacrificedEnergy += staminaCost;
            PhotonView? itemView = PhotonView.Find(itemViewId);
            ItemBattery? battery = itemView != null ? FindBattery(itemView) : null;
            if (battery != null)
                StartCoroutine(VerifyCharge(battery, battery.gameObject.name, lifeBefore, lifeAfter));

            NexusRolesPlugin.Logger.LogInfo("Engineer: remote charge request " + requestId + " confirmed (" + lifeBefore.ToString("0.#") + " -> " + lifeAfter.ToString("0.#") + ").");
        }

        private IEnumerator ChargeRequestTimeout(int requestId)
        {
            yield return new WaitForSeconds(ChargeRequestTimeoutSeconds);

            if (!chargeRequestPending || requestId != pendingChargeRequestId)
                yield break;

            float staminaCost = pendingStaminaCost;
            ClearPendingCharge();
            RefundStamina(staminaCost);
            NexusRolesPlugin.Logger.LogWarning("Engineer: charge request " + requestId + " timed out; stamina was refunded.");
        }

        private void ClearPendingCharge()
        {
            chargeRequestPending = false;
            pendingChargeRequestId = 0;
            pendingItemViewId = 0;
            pendingControllerActorNumber = 0;
            pendingStaminaCost = 0f;
        }

        private void SpendStamina(float staminaCost)
        {
            ReserveStamina(staminaCost);
            sacrificedEnergy += staminaCost;
        }

        private static void ReserveStamina(float staminaCost)
        {
            PlayerController.instance.EnergyStart -= staminaCost;
            PlayerController.instance.EnergyCurrent = Mathf.Max(0f, PlayerController.instance.EnergyCurrent - staminaCost);
        }

        private static void RefundStamina(float staminaCost)
        {
            PlayerController.instance.EnergyStart += staminaCost;
            PlayerController.instance.EnergyCurrent = Mathf.Min(
                PlayerController.instance.EnergyStart,
                PlayerController.instance.EnergyCurrent + staminaCost);
        }

        private static ItemBattery? FindBattery(PhysGrabObject grabbedObject)
        {
            ItemBattery? battery = grabbedObject.GetComponentInChildren<ItemBattery>(true);
            if (battery == null)
                battery = grabbedObject.GetComponentInParent<ItemBattery>();
            return battery;
        }

        private static ItemBattery? FindBattery(PhotonView itemView)
        {
            ItemBattery? battery = itemView.GetComponentInChildren<ItemBattery>(true);
            if (battery == null)
                battery = itemView.GetComponentInParent<ItemBattery>();
            return battery;
        }

        private static PhotonView? FindItemPhotonView(PhysGrabObject grabbedObject, ItemBattery battery)
        {
            PhotonView? view = battery.GetComponentInParent<PhotonView>();
            if (view == null)
                view = grabbedObject.GetComponentInParent<PhotonView>();
            if (view == null)
                view = grabbedObject.GetComponentInChildren<PhotonView>(true);
            return view;
        }

        private static IEnumerator VerifyCharge(ItemBattery battery, string itemName, float lifeBefore, float lifeExpected)
        {
            yield return new WaitForSeconds(ChargeVerifyDelaySeconds);

            if (battery == null) yield break;

            float lifeNow = battery.batteryLife;
            if (lifeNow <= lifeBefore + 0.01f)
            {
                NexusRolesPlugin.Logger.LogWarning("Engineer: charge on '" + itemName + "' did not stick (before " + lifeBefore.ToString("0.#") + ", expected " + lifeExpected.ToString("0.#") + ", now " + lifeNow.ToString("0.#") + "). The game may have overwritten it through network sync.");
            }
            else
            {
                NexusRolesPlugin.Logger.LogInfo("Engineer: charge on '" + itemName + "' confirmed (now " + lifeNow.ToString("0.#") + ").");
            }
        }
    }
}
