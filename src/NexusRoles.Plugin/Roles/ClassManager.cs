using System;
using System.Linq;
using HarmonyLib;
using NexusRoles;
using NexusRoles.Roles.Patches;
using Photon.Pun;
using UnityEngine;
using Random = System.Random;

namespace NexusRoles.Roles
{
    public class ClassManager
    {
        private Random rnd = new Random();

        public int chosenRoleId;

        public ReaperManager rMan = null!;

        public readonly Harmony harmonyPatcher = new Harmony("patches.NexusRolesPlugin.mod");

        public static int stackKills;

        public static bool isTank;

        public static bool isScout;

        public static bool isEngineer;

        public static bool isSavior;

        public ClassManager()
        {
            // The constructor is intentionally empty; role assignment reads the current player values.
        }

        private static float MinMultiplier(float value)
        {
            return Mathf.Max(0.1f, value);
        }

        private static float NonNegative(float value)
        {
            return Mathf.Max(0f, value);
        }

        private static int NonNegative(int value)
        {
            return Mathf.Max(0, value);
        }

        private static int ScaleHealthValue(int value, float multiplier)
        {
            if (value <= 0)
            {
                return value;
            }

            return Mathf.Max(1, Mathf.RoundToInt(value * MinMultiplier(multiplier)));
        }

        private void assignRandomEnabledRole()
        {
            int fallbackRoleId = NexusRolesPlugin.Settings.ResolveEnabledRoleIdOrRandom(-1, rnd);
            if (fallbackRoleId <= 0)
            {
                NexusRolesPlugin.Logger.LogWarning((object)"Unable to roll a fallback role because all roles are disabled.");
                return;
            }

            assignRole(fallbackRoleId, PlayerController.instance);
            NexusRolesPlugin.Logger.LogInfo((object)"You got assigned a new random enabled role because this one was disabled.");
        }

        public int genGamblerEffectNr()
        {
            return rnd.Next(0, 5);
        }

        public object[] genGamblerEffects()
        {
            int posEffectNr = genGamblerEffectNr();
            int negEffectNr = genGamblerEffectNr();
            while (posEffectNr == negEffectNr)
            {
                posEffectNr = genGamblerEffectNr();
                negEffectNr = genGamblerEffectNr();
            }
            string text = NexusLocalization.GamblerEffect(posEffectNr, true);
            string text2 = NexusLocalization.GamblerEffect(negEffectNr, false);
            string text3 = NexusRolesPlugin.Settings.GetRoleDescription(3) + " " + text + ", " + NexusLocalization.Text("but", "но", "але") + " " + text2 + "!";
            return new object[3] { text3, posEffectNr, negEffectNr };
        }

        public void assignRoleFromConfig(PlayerController __instance)
        {
            chosenRoleId = NexusRolesPlugin.Settings.GetSelectedRoleId(rnd);
            assignRole(chosenRoleId, __instance);
        }

        private void modifyStrength(string steamID, float newStrength)
        {
            if (!SemiFunc.IsMultiplayer())
            {
                PlayerAvatar.instance.physGrabber.grabStrength = newStrength;
                return;
            }
            PunManager.instance.photonView.RPC("setStrengthRPC", (RpcTarget)0, new object[2] { steamID, newStrength });
        }

        private void setHealth(string steamID, int maxHealth, int health)
        {
            if (!SemiFunc.IsMultiplayer())
            {
                PlayerAvatar.instance.playerHealth.health = health;
                PlayerAvatar.instance.playerHealth.maxHealth = maxHealth;
                return;
            }
            PunManager.instance.photonView.RPC("setHealthRPC", (RpcTarget)0, new object[3] { steamID, maxHealth, health });
        }

        public void setReaperStatus(string steamID, bool isReaper)
        {
            if (!SemiFunc.IsMultiplayer())
            {
                ((Component)PlayerAvatar.instance).GetComponent<ReaperManager>().isReaper = isReaper;
            }
            else if ((UnityEngine.Object)(object)PlayerAvatar.instance.photonView != null)
            {
                // only send in run levels
                if (RunManager.instance == null || RunManager.instance.levelCurrent == null) return;
                if (!RunManager.instance.levels.Contains(RunManager.instance.levelCurrent)) return;

                if (PlayerAvatar.instance != null && PlayerAvatar.instance.photonView != null)
                {
                    PlayerAvatar.instance.photonView.RPC("setReaperStatusRPC", RpcTarget.All, steamID, isReaper);
#if DEBUG
                    NexusRolesPlugin.Logger.LogInfo((object)$"[CsMr] setReaperStatus: sent RPC for {steamID}, setTo={isReaper}");
#endif
                }
            }
        }

        public void assignRole(int roleId, PlayerController __instance)
        {
            NexusRolesPlugin.Update_ManaRegeneration();
            NexusRolesPlugin.Update_ScoutCooldown();
            guiManager gui = NexusRolesPlugin.GUIinstance;
            if ((UnityEngine.Object)(object)gui != null)
            {
                gui.ClearMageTemporaryEffects();
            }

            float currentMoveSpeed = __instance.MoveSpeed;
            float currentSprintSpeed = __instance.SprintSpeed;
            float currentCrouchSpeed = __instance.CrouchSpeed;
            float currentJumpForce = __instance.JumpForce;
            float currentMaxEnergy = __instance.EnergyStart;
            float currentEnergy = __instance.EnergyCurrent;
            float currentGrabStrength = PlayerAvatar.instance.physGrabber.grabStrength;
            float currentGrabRange = PlayerAvatar.instance.physGrabber.grabRange;
            int currentMaxHealth = (int)AccessTools.Field(typeof(PlayerHealth), "maxHealth").GetValue(PlayerAvatar.instance.playerHealth);
            int currentHealth = (int)AccessTools.Field(typeof(PlayerHealth), "health").GetValue(PlayerAvatar.instance.playerHealth);

            if ((UnityEngine.Object)(object)((Component)PlayerAvatar.instance).GetComponent<ReaperManager>() != null)
            {
                rMan = ((Component)PlayerAvatar.instance).GetComponent<ReaperManager>();
            }
            else
            {
                NexusRolesPlugin.Logger.LogError((object)"Failed to get Reaper Manager! Please contact the mod developer about this.");
            }

            string text = SemiFunc.PlayerGetName(PlayerAvatar.instance);
            string key = SemiFunc.PlayerGetSteamID(PlayerAvatar.instance);

            stackKills = 0;

            guiManager.isMage = false;
            isTank = false;
            setReaperStatus(PlayerController.instance.playerSteamID, isReaper: false);
            isScout = false;
            isEngineer = false;
            isSavior = false;
            SaviorManager? saviorManager = PlayerAvatar.instance.GetComponent<SaviorManager>();
            saviorManager?.SetRoleState(false);

            if (!SemiFunc.RunIsLevel() || SemiFunc.RunIsShop())
            {
                return;
            }

            bool[] roleStatus = {
                NexusRolesPlugin.Settings.EnableRunner.Value,
                NexusRolesPlugin.Settings.EnableTank.Value,
                NexusRolesPlugin.Settings.EnableGambler.Value,
                NexusRolesPlugin.Settings.EnableStrongman.Value,
                NexusRolesPlugin.Settings.EnableRL.Value,
                NexusRolesPlugin.Settings.EnableAthletic.Value,
                NexusRolesPlugin.Settings.EnableMage.Value,
                NexusRolesPlugin.Settings.EnableReaper.Value,
                NexusRolesPlugin.Settings.EnableScout.Value,
                NexusRolesPlugin.Settings.EnableRegular.Value,
                NexusRolesPlugin.Settings.EnableEngineer.Value,
                NexusRolesPlugin.Settings.EnableAcrobat.Value,
                NexusRolesPlugin.Settings.EnableSavior.Value
            };

            if (!roleStatus.Any(isEnabled => isEnabled))
            {
                NexusRolesPlugin.Logger.LogError((object)"WARNING! You disabled all roles in the config file. You will not get any roles until you change it back.");
                return;
            }

            if (NexusRolesPlugin.Settings.ShowGUIAtStart.Value)
            {
                guiManager.showGUI = true;
            }
            else
            {
                guiManager.showGUI = false;
            }

            harmonyPatcher.PatchAll(typeof(PunManagerPatch));

            int requestedRoleId = roleId;
            roleId = NexusRolesPlugin.Settings.ResolveEnabledRoleIdOrRandom(roleId, rnd);
            if (roleId <= 0)
            {
                NexusRolesPlugin.Logger.LogWarning((object)"Unable to find an enabled role. Check the NexusRoles role config.");
                return;
            }
            if (requestedRoleId != roleId)
            {
                NexusRolesPlugin.Logger.LogInfo((object)"Selected role was unavailable, rolling an enabled role instead.");
            }

            switch (roleId)
            {
                case 1: // Runner
                    {
                        if (!NexusRolesPlugin.Settings.EnableRunner.Value)
                        {
                            assignRandomEnabledRole();
                            break;
                        }
                        guiManager.ResetManaUI();
                        NexusRolesPlugin.Logger.LogInfo("Resetting Mana UI.");
                        NexusRolesPlugin.Logger.LogInfo((object)("Assigning role " + NexusRolesPlugin.Settings.GetRoleDisplayName(1) + "."));

                        float speedMultiplier = MinMultiplier(NexusRolesPlugin.Settings.RunnerSpeedMultiplier.Value);
                        float staminaMultiplier = MinMultiplier(NexusRolesPlugin.Settings.RunnerStaminaMultiplier.Value);
                        __instance.CrouchSpeed = currentCrouchSpeed * speedMultiplier;
                        __instance.MoveSpeed = currentMoveSpeed * speedMultiplier;
                        __instance.SprintSpeed = currentSprintSpeed * speedMultiplier;
                        __instance.EnergyStart = currentMaxEnergy * staminaMultiplier;
                        __instance.EnergyCurrent = currentEnergy * staminaMultiplier;

                        NexusRolesPlugin.GUIinstance.text = NexusRolesPlugin.Settings.GetRoleDisplayName(1);
                        NexusRolesPlugin.GUIinstance.color = new Color(0.973f, 1f, 0.196f);
                        NexusRolesPlugin.GUIinstance.descText = NexusRolesPlugin.Settings.GetRoleDescription(1) + "\n" + NexusLocalization.ContinuePrompt(NexusRolesPlugin.Settings.ToggleKey.Value);
                        NexusRolesPlugin.GUIinstance.descColor = new Color(0.973f, 1f, 0.196f);
                        break;
                    }
                case 2: // Tank
                    {
                        if (!NexusRolesPlugin.Settings.EnableTank.Value)
                        {
                            assignRandomEnabledRole();
                            break;
                        }
                        guiManager.ResetManaUI();
                        NexusRolesPlugin.Logger.LogInfo("Resetting Mana UI.");
                        NexusRolesPlugin.Logger.LogInfo((object)("Assigning role " + NexusRolesPlugin.Settings.GetRoleDisplayName(2) + "."));

                        float healthMultiplier = MinMultiplier(NexusRolesPlugin.Settings.TankHealthMultiplier.Value);
                        float speedMultiplier = MinMultiplier(NexusRolesPlugin.Settings.TankSpeedMultiplier.Value);
                        setHealth(PlayerController.instance.playerSteamID, ScaleHealthValue(currentMaxHealth, healthMultiplier), ScaleHealthValue(currentHealth, healthMultiplier));
                        __instance.CrouchSpeed = currentCrouchSpeed * speedMultiplier;
                        __instance.MoveSpeed = currentMoveSpeed * speedMultiplier;
                        __instance.SprintSpeed = currentSprintSpeed * speedMultiplier;
                        isTank = true;

                        NexusRolesPlugin.GUIinstance.text = NexusRolesPlugin.Settings.GetRoleDisplayName(2);
                        NexusRolesPlugin.GUIinstance.color = Color.gray;
                        NexusRolesPlugin.GUIinstance.descText = NexusRolesPlugin.Settings.GetRoleDescription(2) + "\n" + NexusLocalization.ContinuePrompt(NexusRolesPlugin.Settings.ToggleKey.Value);
                        NexusRolesPlugin.GUIinstance.descColor = Color.gray;
                        break;
                    }
                case 3: // Gambler
                    {
                        if (!NexusRolesPlugin.Settings.EnableGambler.Value)
                        {
                            assignRandomEnabledRole();
                            break;
                        }
                        guiManager.ResetManaUI();
                        NexusRolesPlugin.Logger.LogInfo("Resetting Mana UI.");
                        NexusRolesPlugin.Logger.LogInfo((object)("Assigning role " + NexusRolesPlugin.Settings.GetRoleDisplayName(3) + "."));

                        object[] array = genGamblerEffects();

                        if ((int)array[1] == 0)
                        {
                            float speedMultiplier = MinMultiplier(NexusRolesPlugin.Settings.GamblerPositiveSpeedMultiplier.Value);
                            __instance.CrouchSpeed = currentCrouchSpeed * speedMultiplier;
                            __instance.MoveSpeed = currentMoveSpeed * speedMultiplier;
                            __instance.SprintSpeed = currentSprintSpeed * speedMultiplier;
                        }
                        else if ((int)array[1] == 1)
                        {
                            float staminaMultiplier = MinMultiplier(NexusRolesPlugin.Settings.GamblerPositiveStaminaMultiplier.Value);
                            __instance.EnergyStart = currentMaxEnergy * staminaMultiplier;
                            __instance.EnergyCurrent = currentEnergy * staminaMultiplier;
                        }
                        else if ((int)array[1] == 2)
                        {
                            float healthMultiplier = MinMultiplier(NexusRolesPlugin.Settings.GamblerPositiveHealthMultiplier.Value);
                            setHealth(PlayerController.instance.playerSteamID, ScaleHealthValue(currentMaxHealth, healthMultiplier), ScaleHealthValue(currentHealth, healthMultiplier));
                        }
                        else if ((int)array[1] == 3)
                        {
                            modifyStrength(PlayerController.instance.playerSteamID, currentGrabStrength * MinMultiplier(NexusRolesPlugin.Settings.GamblerPositiveStrengthMultiplier.Value));
                        }
                        else if ((int)array[1] == 4)
                        {
                            __instance.JumpForce = currentJumpForce * MinMultiplier(NexusRolesPlugin.Settings.GamblerPositiveJumpMultiplier.Value);
                        }

                        if ((int)array[2] == 0)
                        {
                            float speedMultiplier = MinMultiplier(NexusRolesPlugin.Settings.GamblerNegativeSpeedMultiplier.Value);
                            __instance.CrouchSpeed = currentCrouchSpeed * speedMultiplier;
                            __instance.MoveSpeed = currentMoveSpeed * speedMultiplier;
                            __instance.SprintSpeed = currentSprintSpeed * speedMultiplier;
                        }
                        else if ((int)array[2] == 1)
                        {
                            float staminaMultiplier = MinMultiplier(NexusRolesPlugin.Settings.GamblerNegativeStaminaMultiplier.Value);
                            __instance.EnergyStart = currentMaxEnergy * staminaMultiplier;
                            __instance.EnergyCurrent = currentEnergy * staminaMultiplier;
                        }
                        else if ((int)array[2] == 2)
                        {
                            float healthMultiplier = MinMultiplier(NexusRolesPlugin.Settings.GamblerNegativeHealthMultiplier.Value);
                            setHealth(PlayerController.instance.playerSteamID, ScaleHealthValue(currentMaxHealth, healthMultiplier), ScaleHealthValue(currentHealth, healthMultiplier));
                        }
                        else if ((int)array[2] == 3)
                        {
                            modifyStrength(PlayerController.instance.playerSteamID, currentGrabStrength * MinMultiplier(NexusRolesPlugin.Settings.GamblerNegativeStrengthMultiplier.Value));
                        }
                        else if ((int)array[2] == 4)
                        {
                            __instance.JumpForce = currentJumpForce * MinMultiplier(NexusRolesPlugin.Settings.GamblerNegativeJumpMultiplier.Value);
                        }

                        NexusRolesPlugin.GUIinstance.text = NexusRolesPlugin.Settings.GetRoleDisplayName(3);
                        NexusRolesPlugin.GUIinstance.color = new Color(0.576f, 0f, 0.831f);
                        NexusRolesPlugin.GUIinstance.descColor = new Color(0.576f, 0f, 0.831f);
                        NexusRolesPlugin.GUIinstance.descText = array[0]?.ToString() + "\n" + NexusLocalization.ContinuePrompt(NexusRolesPlugin.Settings.ToggleKey.Value);
                        break;
                    }
                case 4: // Strongman
                    {
                        if (!NexusRolesPlugin.Settings.EnableStrongman.Value)
                        {
                            assignRandomEnabledRole();
                            break;
                        }
                        guiManager.ResetManaUI();
                        NexusRolesPlugin.Logger.LogInfo("Resetting Mana UI.");
                        NexusRolesPlugin.Logger.LogMessage((object)("Assigning role " + NexusRolesPlugin.Settings.GetRoleDisplayName(4) + "."));

                        NexusRolesPlugin.Logger.LogInfo((object)("Strength before: " + currentGrabStrength));
                        modifyStrength(PlayerController.instance.playerSteamID, currentGrabStrength * MinMultiplier(NexusRolesPlugin.Settings.StrongmanStrengthMultiplier.Value) + NonNegative(NexusRolesPlugin.Settings.StrongmanStrengthBonus.Value));
                        NexusRolesPlugin.Logger.LogInfo((object)("Strength after: " + PhysGrabber.instance.grabStrength));

                        NexusRolesPlugin.GUIinstance.text = NexusRolesPlugin.Settings.GetRoleDisplayName(4);
                        NexusRolesPlugin.GUIinstance.color = new Color(0.761f, 0.055f, 0.055f);
                        NexusRolesPlugin.GUIinstance.descText = NexusRolesPlugin.Settings.GetRoleDescription(4) + "\n" + NexusLocalization.ContinuePrompt(NexusRolesPlugin.Settings.ToggleKey.Value);
                        NexusRolesPlugin.GUIinstance.descColor = new Color(0.761f, 0.055f, 0.055f);
                        break;
                    }
                case 5: // Ranged Looter
                    {
                        if (!NexusRolesPlugin.Settings.EnableRL.Value)
                        {
                            assignRandomEnabledRole();
                            break;
                        }
                        guiManager.ResetManaUI();
                        NexusRolesPlugin.Logger.LogInfo("Resetting Mana UI.");
                        NexusRolesPlugin.Logger.LogMessage((object)("Assigning role " + NexusRolesPlugin.Settings.GetRoleDisplayName(5) + "."));

                        PlayerAvatar.instance.physGrabber.grabRange = currentGrabRange * MinMultiplier(NexusRolesPlugin.Settings.RangedLooterGrabRangeMultiplier.Value);
                        modifyStrength(PlayerController.instance.playerSteamID, currentGrabStrength * MinMultiplier(NexusRolesPlugin.Settings.RangedLooterStrengthMultiplier.Value));

                        NexusRolesPlugin.GUIinstance.text = NexusRolesPlugin.Settings.GetRoleDisplayName(5);
                        NexusRolesPlugin.GUIinstance.color = new Color(0.592f, 0.969f, 0.663f);
                        NexusRolesPlugin.GUIinstance.descText = NexusRolesPlugin.Settings.GetRoleDescription(5) + "\n" + NexusLocalization.ContinuePrompt(NexusRolesPlugin.Settings.ToggleKey.Value);
                        NexusRolesPlugin.GUIinstance.descColor = new Color(0.592f, 0.969f, 0.663f);
                        break;
                    }
                case 6: // Athletic
                    {
                        if (!NexusRolesPlugin.Settings.EnableAthletic.Value)
                        {
                            assignRandomEnabledRole();
                            break;
                        }
                        guiManager.ResetManaUI();
                        NexusRolesPlugin.Logger.LogInfo("Resetting Mana UI.");
                        NexusRolesPlugin.Logger.LogMessage((object)("Assigning role " + NexusRolesPlugin.Settings.GetRoleDisplayName(6) + "."));

                        float staminaBonus = NonNegative(NexusRolesPlugin.Settings.AthleticStaminaBonus.Value);
                        modifyStrength(PlayerController.instance.playerSteamID, currentGrabStrength * MinMultiplier(NexusRolesPlugin.Settings.AthleticStrengthMultiplier.Value));
                        __instance.EnergyStart = currentMaxEnergy + staminaBonus;
                        __instance.EnergyCurrent = currentEnergy + staminaBonus;
                        __instance.JumpForce = currentJumpForce + NonNegative(NexusRolesPlugin.Settings.AthleticJumpForceBonus.Value);

                        NexusRolesPlugin.GUIinstance.text = NexusRolesPlugin.Settings.GetRoleDisplayName(6);
                        NexusRolesPlugin.GUIinstance.color = Color.white;
                        NexusRolesPlugin.GUIinstance.descText = NexusRolesPlugin.Settings.GetRoleDescription(6) + "\n" + NexusLocalization.ContinuePrompt(NexusRolesPlugin.Settings.ToggleKey.Value);
                        NexusRolesPlugin.GUIinstance.descColor = Color.white;
                        break;
                    }
                case 7: // Mage
                    {
                        if (!NexusRolesPlugin.Settings.EnableMage.Value)
                        {
                            assignRandomEnabledRole();
                            break;
                        }

                        NexusRolesPlugin.Logger.LogMessage((object)("Assigning role " + NexusRolesPlugin.Settings.GetRoleDisplayName(7) + "."));

                        // Set mage flag and initial mana
                        guiManager.isMage = true;
                        guiManager.aviableMana = guiManager.GetMageMaxMana();
                        guiManager.manaTicks = 0;

                        // Create UI if it doesn't exist (hidden by default)
                        ManaHelper.CreateUI();

                        // Update display based on current config
                        if (NexusRolesPlugin.GUIinstance != null)
                        {
                            NexusRolesPlugin.GUIinstance.UpdateManaDisplay();
                        }

                        // Adjust health for mage
                        if ((double)currentHealth * 0.5 > 0.0)
                        {
                            setHealth(PlayerController.instance.playerSteamID, (int)(currentMaxHealth * 0.5), (int)(currentHealth * 0.5));
                        }
                        else
                        {
                            setHealth(PlayerController.instance.playerSteamID, (int)(currentMaxHealth * 0.5), currentHealth);
                        }

                        // Set GUI text
                        NexusRolesPlugin.GUIinstance!.text = NexusRolesPlugin.Settings.GetRoleDisplayName(7);
                        NexusRolesPlugin.GUIinstance!.color = new Color(0f, 0.384f, 1f);
                        NexusRolesPlugin.GUIinstance!.descText = NexusRolesPlugin.Settings.GetRoleDescription(7) + "\n" +
                            NexusLocalization.Text("Press [", "Нажми [", "Натисни [") + NexusRolesPlugin.Settings.ShowSpellsKey.Value +
                            NexusLocalization.Text("], to view spells. ", "], чтобы посмотреть заклинания. ", "], щоб переглянути закляття. ") +
                            NexusLocalization.ContinuePrompt(NexusRolesPlugin.Settings.ToggleKey.Value);
                        NexusRolesPlugin.GUIinstance!.descColor = new Color(0f, 0.384f, 1f);
                        break;
                    }
                case 8: // Reaper
                    {
                        if (!NexusRolesPlugin.Settings.EnableReaper.Value)
                        {
                            assignRandomEnabledRole();
                            break;
                        }
                        guiManager.ResetManaUI();
                        NexusRolesPlugin.Logger.LogInfo("Resetting Mana UI.");
                        NexusRolesPlugin.Logger.LogMessage((object)("Assigning role " + NexusRolesPlugin.Settings.GetRoleDisplayName(8) + "."));

                        harmonyPatcher.PatchAll(typeof(ReaperPatch));
                        setReaperStatus(PlayerController.instance.playerSteamID, isReaper: true);

                        float healthMultiplier = MinMultiplier(NexusRolesPlugin.Settings.ReaperHealthMultiplier.Value);
                        setHealth(PlayerController.instance.playerSteamID, ScaleHealthValue(currentMaxHealth, healthMultiplier), ScaleHealthValue(currentHealth, healthMultiplier));

                        NexusRolesPlugin.GUIinstance.text = NexusRolesPlugin.Settings.GetRoleDisplayName(8);
                        NexusRolesPlugin.GUIinstance.color = new Color(0.141f, 0.6f, 0.502f);
                        NexusRolesPlugin.GUIinstance.descText = NexusRolesPlugin.Settings.GetRoleDescription(8) + "\n" + NexusLocalization.ContinuePrompt(NexusRolesPlugin.Settings.ToggleKey.Value);
                        NexusRolesPlugin.GUIinstance.descColor = new Color(0.141f, 0.6f, 0.502f);
                        break;
                    }
                case 9: // Scout
                    {
                        if (!NexusRolesPlugin.Settings.EnableScout.Value)
                        {
                            assignRandomEnabledRole();
                            break;
                        }
                        guiManager.ResetManaUI();
                        NexusRolesPlugin.Logger.LogInfo("Resetting Mana UI.");
                        NexusRolesPlugin.Logger.LogMessage((object)("Assigning role " + NexusRolesPlugin.Settings.GetRoleDisplayName(9) + "."));

                        isScout = true;
                        float staminaMultiplier = MinMultiplier(NexusRolesPlugin.Settings.ScoutStaminaMultiplier.Value);
                        __instance.EnergyStart = currentMaxEnergy * staminaMultiplier;
                        __instance.EnergyCurrent = currentEnergy * staminaMultiplier;
                        __instance.sprintRechargeAmount *= MinMultiplier(NexusRolesPlugin.Settings.ScoutSprintRechargeMultiplier.Value);

                        NexusRolesPlugin.GUIinstance.text = NexusRolesPlugin.Settings.GetRoleDisplayName(9);
                        NexusRolesPlugin.GUIinstance.color = new Color(0.902f, 0.733f, 0.11f);
                        NexusRolesPlugin.GUIinstance.descText = NexusRolesPlugin.Settings.GetRoleDescription(9).Replace("[G]", "[" + NexusRolesPlugin.Settings.ScoutKey.Value + "]") + "\n" + NexusLocalization.ContinuePrompt(NexusRolesPlugin.Settings.ToggleKey.Value);
                        NexusRolesPlugin.GUIinstance.descColor = new Color(0.902f, 0.733f, 0.11f);
                        break;
                    }
                case 10: // Regular
                    {
                        if (!NexusRolesPlugin.Settings.EnableRegular.Value)
                        {
                            assignRandomEnabledRole();
                            break;
                        }
                        guiManager.ResetManaUI();
                        NexusRolesPlugin.Logger.LogInfo("Resetting Mana UI.");
                        NexusRolesPlugin.Logger.LogMessage((object)("Assigning role " + NexusRolesPlugin.Settings.GetRoleDisplayName(10) + "."));

                        __instance.CrouchSpeed = currentCrouchSpeed;
                        __instance.MoveSpeed = currentMoveSpeed;
                        __instance.SprintSpeed = currentSprintSpeed;
                        __instance.JumpForce = currentJumpForce;
                        __instance.EnergyStart = currentMaxEnergy;
                        __instance.EnergyCurrent = currentEnergy;

                        NexusRolesPlugin.GUIinstance.text = NexusRolesPlugin.Settings.GetRoleDisplayName(10);
                        NexusRolesPlugin.GUIinstance.color = Color.white;
                        NexusRolesPlugin.GUIinstance.descText = NexusRolesPlugin.Settings.GetRoleDescription(10) + "\n" + NexusLocalization.ContinuePrompt(NexusRolesPlugin.Settings.ToggleKey.Value);
                        NexusRolesPlugin.GUIinstance.descColor = Color.white;
                        break;
                    }
                case 11: // Engineer
                    {
                        if (!NexusRolesPlugin.Settings.EnableEngineer.Value)
                        {
                            assignRandomEnabledRole();
                            break;
                        }
                        guiManager.ResetManaUI();
                        NexusRolesPlugin.Logger.LogInfo("Resetting Mana UI.");
                        NexusRolesPlugin.Logger.LogMessage((object)("Assigning role " + NexusRolesPlugin.Settings.GetRoleDisplayName(11) + "."));

                        isEngineer = true;

                        NexusRolesPlugin.GUIinstance.text = NexusRolesPlugin.Settings.GetRoleDisplayName(11);
                        NexusRolesPlugin.GUIinstance.color = new Color(0.8f, 0.5f, 0.2f);
                        NexusRolesPlugin.GUIinstance.descText = NexusRolesPlugin.Settings.GetRoleDescription(11) + "\n" +
                            NexusLocalization.Text("Hold an item and press [", "Держи предмет и нажми [", "Тримай предмет і натисни [") +
                            NexusRolesPlugin.Settings.ChargeItemKey.Value + NexusLocalization.Text("] to charge it.", "], чтобы зарядить его.", "], щоб зарядити його.") +
                            "\n" + NexusLocalization.ContinuePrompt(NexusRolesPlugin.Settings.ToggleKey.Value);
                        NexusRolesPlugin.GUIinstance.descColor = new Color(0.8f, 0.5f, 0.2f);
                        break;
                    }
                case 12: // Acrobat
                    {
                        if (!NexusRolesPlugin.Settings.EnableAcrobat.Value)
                        {
                            assignRandomEnabledRole();
                            break;
                        }

                        guiManager.ResetManaUI();
                        float healthMultiplier = MinMultiplier(NexusRolesPlugin.Settings.AcrobatHealthMultiplier.Value);
                        float strengthMultiplier = MinMultiplier(NexusRolesPlugin.Settings.AcrobatStrengthMultiplier.Value);
                        float speedMultiplier = MinMultiplier(NexusRolesPlugin.Settings.AcrobatSpeedMultiplier.Value);
                        setHealth(PlayerController.instance.playerSteamID,
                            ScaleHealthValue(currentMaxHealth, healthMultiplier), ScaleHealthValue(currentHealth, healthMultiplier));
                        modifyStrength(PlayerController.instance.playerSteamID, currentGrabStrength * strengthMultiplier);
                        __instance.CrouchSpeed = currentCrouchSpeed * speedMultiplier;
                        __instance.MoveSpeed = currentMoveSpeed * speedMultiplier;
                        __instance.SprintSpeed = currentSprintSpeed * speedMultiplier;
                        __instance.JumpForce = currentJumpForce + NonNegative(NexusRolesPlugin.Settings.AcrobatJumpForceBonus.Value);

                        NexusRolesPlugin.GUIinstance.text = NexusLocalization.RoleName(12);
                        NexusRolesPlugin.GUIinstance.color = new Color(0.3f, 0.9f, 0.7f);
                        NexusRolesPlugin.GUIinstance.descText = NexusLocalization.RoleDescription(12) + "\n" + NexusLocalization.ContinuePrompt(NexusRolesPlugin.Settings.ToggleKey.Value);
                        NexusRolesPlugin.GUIinstance.descColor = new Color(0.3f, 0.9f, 0.7f);
                        break;
                    }
                case 13: // Savior
                    {
                        if (!NexusRolesPlugin.Settings.EnableSavior.Value)
                        {
                            assignRandomEnabledRole();
                            break;
                        }

                        guiManager.ResetManaUI();
                        isSavior = true;
                        saviorManager?.SetRoleState(true);
                        bool sacrificeAll = NexusLocalization.IsAllSacrifice(NexusRolesPlugin.Settings.SaviorSacrificeMode.Value);
                        int partialPercent = Mathf.Clamp(NexusRolesPlugin.Settings.SaviorPartialCostPercent.Value, 10, 90);

                        NexusRolesPlugin.GUIinstance.text = NexusLocalization.RoleName(13);
                        NexusRolesPlugin.GUIinstance.color = new Color(0.95f, 0.8f, 0.35f);
                        NexusRolesPlugin.GUIinstance.descText = NexusLocalization.RoleDescription(13) + "\n" +
                            NexusLocalization.SaviorPrompt(NexusRolesPlugin.Settings.SaviorKey.Value, sacrificeAll, partialPercent) +
                            "\n" + NexusLocalization.ContinuePrompt(NexusRolesPlugin.Settings.ToggleKey.Value);
                        NexusRolesPlugin.GUIinstance.descColor = new Color(0.95f, 0.8f, 0.35f);
                        break;
                    }
            }
        }
    }
}
