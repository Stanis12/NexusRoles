using System.Collections;
using NuageRoles;
using Photon.Pun;
using UnityEngine;

namespace NuageRoles.Roles.Patches
{
    public class EngineerManager : MonoBehaviourPun
    {
        // How long to wait before checking that a charge was not overwritten by the game (network sync).
        private const float ChargeVerifyDelaySeconds = 0.5f;

        private PlayerAvatar? playerAvatar;
        private float sacrificedEnergy = 0f;
        private float regenTimer = 0f;

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

            float staminaCost = Mathf.Max(0f, NuageRolesPlugin.Settings.EngineerChargeStaminaCost.Value);
            float staminaRestoreAmount = Mathf.Max(0f, NuageRolesPlugin.Settings.EngineerStaminaRestoreAmount.Value);
            float staminaRestoreInterval = Mathf.Max(1f, NuageRolesPlugin.Settings.EngineerStaminaRestoreIntervalSeconds.Value);

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
                {
                    regenTimer = 0f;
                }
            }
            else
            {
                regenTimer = 0f;
            }

            if (Input.GetKeyDown(NuageRolesPlugin.Settings.ChargeItemKey.Value))
            {
                TryChargeHeldItem(staminaCost);
            }
        }

        private void TryChargeHeldItem(float staminaCost)
        {
            if (PlayerController.instance.EnergyStart < staminaCost) return;

            PhysGrabber? grabber = PhysGrabber.instance;
            if (grabber == null || !grabber.grabbed || grabber.grabbedPhysGrabObject == null) return;

            PhysGrabObject grabbedObject = grabber.grabbedPhysGrabObject;
            ItemBattery? battery = FindBattery(grabbedObject);
            if (battery == null)
            {
                NuageRolesPlugin.Logger.LogInfo("Engineer: no ItemBattery found on held object '" + grabbedObject.name + "'.");
                return;
            }

            if (battery.batteryLife >= 100f) return;

            // In multiplayer the battery value is synced by the owner of the item's PhotonView.
            // If somebody else owns it, our local change gets overwritten, so ask for ownership first.
            RequestOwnershipIfNeeded(grabbedObject, battery);

            float lifeBefore = battery.batteryLife;
            int totalBars = Mathf.Max(1, battery.batteryBars);
            float oneBarCharge = 100f / totalBars;
            float lifeExpected = Mathf.Min(lifeBefore + oneBarCharge, 100f);

            battery.batteryLife = lifeExpected;

            PlayerController.instance.EnergyStart -= staminaCost;
            sacrificedEnergy += staminaCost;
            PlayerController.instance.EnergyCurrent = Mathf.Max(0f, PlayerController.instance.EnergyCurrent - staminaCost);

            NuageRolesPlugin.Logger.LogInfo("Engineer: charged '" + grabbedObject.name + "' from " + lifeBefore.ToString("0.#") + " to " + lifeExpected.ToString("0.#") + " (bars: " + totalBars + ").");
            StartCoroutine(VerifyCharge(battery, grabbedObject.name, lifeBefore, lifeExpected));
        }

        private static ItemBattery? FindBattery(PhysGrabObject grabbedObject)
        {
            // Most items carry the battery on the grabbed object or below it, but some items
            // (weapons included) may keep it on a parent, so look upwards as a fallback.
            ItemBattery? battery = grabbedObject.GetComponentInChildren<ItemBattery>(true);
            if (battery == null)
            {
                battery = grabbedObject.GetComponentInParent<ItemBattery>();
            }

            return battery;
        }

        private static void RequestOwnershipIfNeeded(PhysGrabObject grabbedObject, ItemBattery battery)
        {
            PhotonView? view = grabbedObject.GetComponentInParent<PhotonView>();
            if (view == null)
            {
                view = battery.GetComponentInParent<PhotonView>();
            }

            if (view == null || view.IsMine || view.OwnershipTransfer == OwnershipOption.Fixed) return;

            view.RequestOwnership();
        }

        private static IEnumerator VerifyCharge(ItemBattery battery, string itemName, float lifeBefore, float lifeExpected)
        {
            yield return new WaitForSeconds(ChargeVerifyDelaySeconds);

            if (battery == null) yield break;

            float lifeNow = battery.batteryLife;
            if (lifeNow <= lifeBefore + 0.01f)
            {
                NuageRolesPlugin.Logger.LogWarning("Engineer: charge on '" + itemName + "' did not stick (before " + lifeBefore.ToString("0.#") + ", expected " + lifeExpected.ToString("0.#") + ", now " + lifeNow.ToString("0.#") + "). The game most likely overwrote it through network sync.");
            }
            else
            {
                NuageRolesPlugin.Logger.LogInfo("Engineer: charge on '" + itemName + "' confirmed (now " + lifeNow.ToString("0.#") + ").");
            }
        }
    }
}
