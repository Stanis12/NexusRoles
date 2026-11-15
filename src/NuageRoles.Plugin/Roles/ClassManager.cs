using System;
using System.Linq;
using HarmonyLib;
using NuageRoles;
using NuageRoles.Roles.Patches;
using Photon.Pun;
using UnityEngine;
using Random = System.Random;

namespace NuageRoles.Roles
{
    public class ClassManager
    {
        private Random rnd = new Random();

        public int chosenRoleId;

        public ReaperManager rMan = null!;

        public readonly Harmony harmonyPatcher = new Harmony("patches.NuageRolesPlugin.mod");

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
            int fallbackRoleId = NuageRolesPlugin.Settings.ResolveEnabledRoleIdOrRandom(-1, rnd);
            if (fallbackRoleId <= 0)
            {
                NuageRolesPlugin.Logger.LogWarning((object)"Unable to roll a fallback role because all roles are disabled.");
                return;
            }

            assignRole(fallbackRoleId, PlayerController.instance);
            NuageRolesPlugin.Logger.LogInfo((object)"You got assigned a new random enabled role because this one was disabled.");
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
            string text3 = NuageRolesPlugin.Settings.GetRoleDescription(3) + " " + text + ", " + NexusLocalization.Text("but", "но", "але") + " " + text2 + "!";
            return new object[3] { text3, posEffectNr, negEffectNr };
        }

        public void assignRoleFromConfig(PlayerController __instance)
        {
            chosenRoleId = NuageRolesPlugin.Settings.GetSelectedRoleId(rnd);
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
                    NuageRolesPlugin.Logger.LogInfo((object)$"[CsMr] setReaperStatus: sent RPC for {steamID}, setTo={isReaper}");
#endif
                }
            }
        }

        public void assignRole(int roleId, PlayerController __instance)
        {
            NuageRolesPlugin.Update_ManaRegeneration();
            NuageRolesPlugin.Update_ScoutCooldown();
            guiManager gui = NuageRolesPlugin.GUIinstance;
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
                NuageRolesPlugin.Logger.LogError((object)"Failed to get Reaper Manager! Please contact the mod developer about this.");
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
                NuageRolesPlugin.Settings.EnableRunner.Value,
                NuageRolesPlugin.Settings.EnableTank.Value,
                NuageRolesPlugin.Settings.EnableGambler.Value,
                NuageRolesPlugin.Settings.EnableStrongman.Value,
                NuageRolesPlugin.Settings.EnableRL.Value,
                NuageRolesPlugin.Settings.EnableAthletic.Value,
                NuageRolesPlugin.Settings.EnableMage.Value,
                NuageRolesPlugin.Settings.EnableReaper.Value,
                NuageRolesPlugin.Settings.EnableScout.Value,
                NuageRolesPlugin.Settings.EnableRegular.Value,
                NuageRolesPlugin.Settings.EnableEngineer.Value,
                NuageRolesPlugin.Settings.EnableAcrobat.Value,
                NuageRolesPlugin.Settings.EnableSavior.Value
            };

            if (!roleStatus.Any(isEnabled => isEnabled))
            {
                NuageRolesPlugin.Logger.LogError((object)"WARNING! You disabled all roles in the config file. You will not get any roles until you change it back.");
                return;
            }

            if (NuageRolesPlugin.Settings.ShowGUIAtStart.Value)
            {
                guiManager.showGUI = true;
            }
            else
            {
                guiManager.showGUI = false;
            }

            harmonyPatcher.PatchAll(typeof(PunManagerPatch));

            int requestedRoleId = roleId;
            roleId = NuageRolesPlugin.Settings.ResolveEnabledRoleIdOrRandom(roleId, rnd);
            if (roleId <= 0)
            {
                NuageRolesPlugin.Logger.LogWarning((object)"Unable to find an enabled role. Check the NuageRoles role config.");
                return;
            }
            if (requestedRoleId != roleId)
            {
                NuageRolesPlugin.Logger.LogInfo((object)"Selected role was unavailable, rolling an enabled role instead.");
            }

            switch (roleId)
            {
                case 1: // Runner
                    {
                        if (!NuageRolesPlugin.Settings.EnableRunner.Value)
                        {
                            assignRandomEnabledRole();
                            break;
                        }
                        guiManager.ResetManaUI();
                        NuageRolesPlugin.Logger.LogInfo("Resetting Mana UI.");
                        NuageRolesPlugin.Logger.LogInfo((object)("Assigning role " + NuageRolesPlugin.Settings.GetRoleDisplayName(1) + "."));

                        float speedMultiplier = MinMultiplier(NuageRolesPlugin.Settings.RunnerSpeedMultiplier.Value);
                        float staminaMultiplier = MinMultiplier(NuageRolesPlugin.Settings.RunnerStaminaMultiplier.Value);
                        __instance.CrouchSpeed = currentCrouchSpeed * speedMultiplier;
                        __instance.MoveSpeed = currentMoveSpeed * speedMultiplier;
                        __instance.SprintSpeed = currentSprintSpeed * speedMultiplier;
                        __instance.EnergyStart = currentMaxEnergy * staminaMultiplier;
                        __instance.EnergyCurrent = currentEnergy * staminaMultiplier;

                        NuageRolesPlugin.GUIinstance.text = NuageRolesPlugin.Settings.GetRoleDisplayName(1);
                        NuageRolesPlugin.GUIinstance.color = new Color(0.973f, 1f, 0.196f);
                        NuageRolesPlugin.GUIinstance.descText = NuageRolesPlugin.Settings.GetRoleDescription(1) + "\n" + NexusLocalization.ContinuePrompt(NuageRolesPlugin.Settings.ToggleKey.Value);
                        NuageRolesPlugin.GUIinstance.descColor = new Color(0.973f, 1f, 0.196f);
                        break;
                    }
                case 2: // Tank
                    {
                        if (!NuageRolesPlugin.Settings.EnableTank.Value)
                        {
                            assignRandomEnabledRole();
                            break;
                        }
                        guiManager.ResetManaUI();
                        NuageRolesPlugin.Logger.LogInfo("Resetting Mana UI.");
                        NuageRolesPlugin.Logger.LogInfo((object)("Assigning role " + NuageRolesPlugin.Settings.GetRoleDisplayName(2) + "."));

                        float healthMultiplier = MinMultiplier(NuageRolesPlugin.Settings.TankHealthMultiplier.Value);
                        float speedMultiplier = MinMultiplier(NuageRolesPlugin.Settings.TankSpeedMultiplier.Value);
                        setHealth(PlayerController.instance.playerSteamID, ScaleHealthValue(currentMaxHealth, healthMultiplier), ScaleHealthValue(currentHealth, healthMultiplier));
                        __instance.CrouchSpeed = currentCrouchSpeed * speedMultiplier;
                        __instance.MoveSpeed = currentMoveSpeed * speedMultiplier;
                        __instance.SprintSpeed = currentSprintSpeed * speedMultiplier;
                        isTank = true;

                        NuageRolesPlugin.GUIinstance.text = NuageRolesPlugin.Settings.GetRoleDisplayName(2);
                        NuageRolesPlugin.GUIinstance.color = Color.gray;
                        NuageRolesPlugin.GUIinstance.descText = NuageRolesPlugin.Settings.GetRoleDescription(2) + "\n" + NexusLocalization.ContinuePrompt(NuageRolesPlugin.Settings.ToggleKey.Value);
                        NuageRolesPlugin.GUIinstance.descColor = Color.gray;
                        break;
                    }
                case 3: // Gambler
                    {
                        if (!NuageRolesPlugin.Settings.EnableGambler.Value)
                        {
                            assignRandomEnabledRole();
                            break;
                        }
                        guiManager.ResetManaUI();
                        NuageRolesPlugin.Logger.LogInfo("Resetting Mana UI.");
                        NuageRolesPlugin.Logger.LogInfo((object)("Assigning role " + NuageRolesPlugin.Settings.GetRoleDisplayName(3) + "."));

                        object[] array = genGamblerEffects();

                        if ((int)array[1] == 0)
                        {
                            float speedMultiplier = MinMultiplier(NuageRolesPlugin.Settings.GamblerPositiveSpeedMultiplier.Value);
                            __instance.CrouchSpeed = currentCrouchSpeed * speedMultiplier;
                            __instance.MoveSpeed = currentMoveSpeed * speedMultiplier;
                            __instance.SprintSpeed = currentSprintSpeed * speedMultiplier;
                        }
                        else if ((int)array[1] == 1)
                        {
                            float staminaMultiplier = MinMultiplier(NuageRolesPlugin.Settings.GamblerPositiveStaminaMultiplier.Value);
                            __instance.EnergyStart = currentMaxEnergy * staminaMultiplier;
                            __instance.EnergyCurrent = currentEnergy * staminaMultiplier;
                        }
                        else if ((int)array[1] == 2)
                        {
                            float healthMultiplier = MinMultiplier(NuageRolesPlugin.Settings.GamblerPositiveHealthMultiplier.Value);
                            setHealth(PlayerController.instance.playerSteamID, ScaleHealthValue(currentMaxHealth, healthMultiplier), ScaleHealthValue(currentHealth, healthMultiplier));
                        }
                        else if ((int)array[1] == 3)
                        {
                            modifyStrength(PlayerController.instance.playerSteamID, currentGrabStrength * MinMultiplier(NuageRolesPlugin.Settings.GamblerPositiveStrengthMultiplier.Value));
                        }
                        else if ((int)array[1] == 4)
                        {
                            __instance.JumpForce = currentJumpForce * MinMultiplier(NuageRolesPlugin.Settings.GamblerPositiveJumpMultiplier.Value);
                        }

                        if ((int)array[2] == 0)
                        {
                            float speedMultiplier = MinMultiplier(NuageRolesPlugin.Settings.GamblerNegativeSpeedMultiplier.Value);
                            __instance.CrouchSpeed = currentCrouchSpeed * speedMultiplier;
                            __instance.MoveSpeed = currentMoveSpeed * speedMultiplier;
                            __instance.SprintSpeed = currentSprintSpeed * speedMultiplier;
                        }
                        else if ((int)array[2] == 1)
                        {
                            float staminaMultiplier = MinMultiplier(NuageRolesPlugin.Settings.GamblerNegativeStaminaMultiplier.Value);
                            __instance.EnergyStart = currentMaxEnergy * staminaMultiplier;
                            __instance.EnergyCurrent = currentEnergy * staminaMultiplier;
                        }
                        else if ((int)array[2] == 2)
                        {
                            float healthMultiplier = MinMultiplier(NuageRolesPlugin.Settings.GamblerNegativeHealthMultiplier.Value);
                            setHealth(PlayerController.instance.playerSteamID, ScaleHealthValue(currentMaxHealth, healthMultiplier), ScaleHealthValue(currentHealth, healthMultiplier));
                        }
                        else if ((int)array[2] == 3)
                        {
                            modifyStrength(PlayerController.instance.playerSteamID, currentGrabStrength * MinMultiplier(NuageRolesPlugin.Settings.GamblerNegativeStrengthMultiplier.Value));
                        }
                        else if ((int)array[2] == 4)
                        {
                            __instance.JumpForce = currentJumpForce * MinMultiplier(NuageRolesPlugin.Settings.GamblerNegativeJumpMultiplier.Value);
                        }

                        NuageRolesPlugin.GUIinstance.text = NuageRolesPlugin.Settings.GetRoleDisplayName(3);
                        NuageRolesPlugin.GUIinstance.color = new Color(0.576f, 0f, 0.831f);
                        NuageRolesPlugin.GUIinstance.descColor = new Color(0.576f, 0f, 0.831f);
                        NuageRolesPlugin.GUIinstance.descText = array[0]?.ToString() + "\n" + NexusLocalization.ContinuePrompt(NuageRolesPlugin.Settings.ToggleKey.Value);
                        break;
                    }
                case 4: // Strongman
                    {
                        if (!NuageRolesPlugin.Settings.EnableStrongman.Value)
                        {
                            assignRandomEnabledRole();
                            break;
                        }
                        guiManager.ResetManaUI();
                        NuageRolesPlugin.Logger.LogInfo("Resetting Mana UI.");
                        NuageRolesPlugin.Logger.LogMessage((object)("Assigning role " + NuageRolesPlugin.Settings.GetRoleDisplayName(4) + "."));

                        NuageRolesPlugin.Logger.LogInfo((object)("Strength before: " + currentGrabStrength));
                        modifyStrength(PlayerController.instance.playerSteamID, currentGrabStrength * MinMultiplier(NuageRolesPlugin.Settings.StrongmanStrengthMultiplier.Value) + NonNegative(NuageRolesPlugin.Settings.StrongmanStrengthBonus.Value));
                        NuageRolesPlugin.Logger.LogInfo((object)("Strength after: " + PhysGrabber.instance.grabStrength));

                        NuageRolesPlugin.GUIinstance.text = NuageRolesPlugin.Settings.GetRoleDisplayName(4);
                        NuageRolesPlugin.GUIinstance.color = new Color(0.761f, 0.055f, 0.055f);
                        NuageRolesPlugin.GUIinstance.descText = NuageRolesPlugin.Settings.GetRoleDescription(4) + "\n" + NexusLocalization.ContinuePrompt(NuageRolesPlugin.Settings.ToggleKey.Value);
                        NuageRolesPlugin.GUIinstance.descColor = new Color(0.761f, 0.055f, 0.055f);
                        break;
                    }
                case 5: // Ranged Looter
                    {
                        if (!NuageRolesPlugin.Settings.EnableRL.Value)
                        {
                            assignRandomEnabledRole();
                            break;
                        }
                        guiManager.ResetManaUI();
                        NuageRolesPlugin.Logger.LogInfo("Resetting Mana UI.");
                        NuageRolesPlugin.Logger.LogMessage((object)("Assigning role " + NuageRolesPlugin.Settings.GetRoleDisplayName(5) + "."));

                        PlayerAvatar.instance.physGrabber.grabRange = currentGrabRange * MinMultiplier(NuageRolesPlugin.Settings.RangedLooterGrabRangeMultiplier.Value);
                        modifyStrength(PlayerController.instance.playerSteamID, currentGrabStrength * MinMultiplier(NuageRolesPlugin.Settings.RangedLooterStrengthMultiplier.Value));

                        NuageRolesPlugin.GUIinstance.text = NuageRolesPlugin.Settings.GetRoleDisplayName(5);
                        NuageRolesPlugin.GUIinstance.color = new Color(0.592f, 0.969f, 0.663f);
                        NuageRolesPlugin.GUIinstance.descText = NuageRolesPlugin.Settings.GetRoleDescription(5) + "\n" + NexusLocalization.ContinuePrompt(NuageRolesPlugin.Settings.ToggleKey.Value);
                        NuageRolesPlugin.GUIinstance.descColor = new Color(0.592f, 0.969f, 0.663f);
                        break;
                    }
                case 6: // Athletic
                    {
                        if (!NuageRolesPlugin.Settings.EnableAthletic.Value)
                        {
                            assignRandomEnabledRole();
                            break;
                        }
                        guiManager.ResetManaUI();
                        NuageRolesPlugin.Logger.LogInfo("Resetting Mana UI.");
                        NuageRolesPlugin.Logger.LogMessage((object)("Assigning role " + NuageRolesPlugin.Settings.GetRoleDisplayName(6) + "."));

                        float staminaBonus = NonNegative(NuageRolesPlugin.Settings.AthleticStaminaBonus.Value);
                        modifyStrength(PlayerController.instance.playerSteamID, currentGrabStrength * MinMultiplier(NuageRolesPlugin.Settings.AthleticStrengthMultiplier.Value));
                        __instance.EnergyStart = currentMaxEnergy + staminaBonus;
                        __instance.EnergyCurrent = currentEnergy + staminaBonus;
                        __instance.JumpForce = currentJumpForce + NonNegative(NuageRolesPlugin.Settings.AthleticJumpForceBonus.Value);

                        NuageRolesPlugin.GUIinstance.text = NuageRolesPlugin.Settings.GetRoleDisplayName(6);
                        NuageRolesPlugin.GUIinstance.color = Color.white;
                        NuageRolesPlugin.GUIinstance.descText = NuageRolesPlugin.Settings.GetRoleDescription(6) + "\n" + NexusLocalization.ContinuePrompt(NuageRolesPlugin.Settings.ToggleKey.Value);
                        NuageRolesPlugin.GUIinstance.descColor = Color.white;
                        break;
                    }
                case 7: // Mage
                    {
                        if (!NuageRolesPlugin.Settings.EnableMage.Value)
                        {
                            assignRandomEnabledRole();
                            break;
                        }

                        NuageRolesPlugin.Logger.LogMessage((object)("Assigning role " + NuageRolesPlugin.Settings.GetRoleDisplayName(7) + "."));

                        // Set mage flag and initial mana
                        guiManager.isMage = true;
                        guiManager.aviableMana = guiManager.GetMageMaxMana();
                        guiManager.manaTicks = 0;

                        // Create UI if it doesn't exist (hidden by default)
                        ManaHelper.CreateUI();

                        // Update display based on current config
                        if (NuageRolesPlugin.GUIinstance != null)
                        {
                            NuageRolesPlugin.GUIinstance.UpdateManaDisplay();
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
                        NuageRolesPlugin.GUIinstance!.text = NuageRolesPlugin.Settings.GetRoleDisplayName(7);
                        NuageRolesPlugin.GUIinstance!.color = new Color(0f, 0.384f, 1f);
                        NuageRolesPlugin.GUIinstance!.descText = NuageRolesPlugin.Settings.GetRoleDescription(7) + "\n" +
                            NexusLocalization.Text("Press [", "Нажми [", "Натисни [") + NuageRolesPlugin.Settings.ShowSpellsKey.Value +
                            NexusLocalization.Text("], to view spells. ", "], чтобы посмотреть заклинания. ", "], щоб переглянути закляття. ") +
                            NexusLocalization.ContinuePrompt(NuageRolesPlugin.Settings.ToggleKey.Value);
                        NuageRolesPlugin.GUIinstance!.descColor = new Color(0f, 0.384f, 1f);
                        break;
                    }
                case 8: // Reaper
                    {
                        if (!NuageRolesPlugin.Settings.EnableReaper.Value)
                        {
                            assignRandomEnabledRole();
                            break;
                        }
                        guiManager.ResetManaUI();
                        NuageRolesPlugin.Logger.LogInfo("Resetting Mana UI.");
                        NuageRolesPlugin.Logger.LogMessage((object)("Assigning role " + NuageRolesPlugin.Settings.GetRoleDisplayName(8) + "."));

                        harmonyPatcher.PatchAll(typeof(ReaperPatch));
                        setReaperStatus(PlayerController.instance.playerSteamID, isReaper: true);

                        float healthMultiplier = MinMultiplier(NuageRolesPlugin.Settings.ReaperHealthMultiplier.Value);
                        setHealth(PlayerController.instance.playerSteamID, ScaleHealthValue(currentMaxHealth, healthMultiplier), ScaleHealthValue(currentHealth, healthMultiplier));

                        NuageRolesPlugin.GUIinstance.text = NuageRolesPlugin.Settings.GetRoleDisplayName(8);
                        NuageRolesPlugin.GUIinstance.color = new Color(0.141f, 0.6f, 0.502f);
                        NuageRolesPlugin.GUIinstance.descText = NuageRolesPlugin.Settings.GetRoleDescription(8) + "\n" + NexusLocalization.ContinuePrompt(NuageRolesPlugin.Settings.ToggleKey.Value);
                        NuageRolesPlugin.GUIinstance.descColor = new Color(0.141f, 0.6f, 0.502f);
                        break;
                    }
                case 9: // Scout
                    {
                        if (!NuageRolesPlugin.Settings.EnableScout.Value)
                        {
                            assignRandomEnabledRole();
                            break;
                        }
                        guiManager.ResetManaUI();
                        NuageRolesPlugin.Logger.LogInfo("Resetting Mana UI.");
                        NuageRolesPlugin.Logger.LogMessage((object)("Assigning role " + NuageRolesPlugin.Settings.GetRoleDisplayName(9) + "."));

                        isScout = true;
                        float staminaMultiplier = MinMultiplier(NuageRolesPlugin.Settings.ScoutStaminaMultiplier.Value);
                        __instance.EnergyStart = currentMaxEnergy * staminaMultiplier;
                        __instance.EnergyCurrent = currentEnergy * staminaMultiplier;
                        __instance.sprintRechargeAmount *= MinMultiplier(NuageRolesPlugin.Settings.ScoutSprintRechargeMultiplier.Value);

                        NuageRolesPlugin.GUIinstance.text = NuageRolesPlugin.Settings.GetRoleDisplayName(9);
                        NuageRolesPlugin.GUIinstance.color = new Color(0.902f, 0.733f, 0.11f);
                        NuageRolesPlugin.GUIinstance.descText = NuageRolesPlugin.Settings.GetRoleDescription(9).Replace("[G]", "[" + NuageRolesPlugin.Settings.ScoutKey.Value + "]") + "\n" + NexusLocalization.ContinuePrompt(NuageRolesPlugin.Settings.ToggleKey.Value);
                        NuageRolesPlugin.GUIinstance.descColor = new Color(0.902f, 0.733f, 0.11f);
                        break;
                    }
                case 10: // Regular
                    {
                        if (!NuageRolesPlugin.Settings.EnableRegular.Value)
                        {
                            assignRandomEnabledRole();
                            break;
                        }
                        guiManager.ResetManaUI();
                        NuageRolesPlugin.Logger.LogInfo("Resetting Mana UI.");
                        NuageRolesPlugin.Logger.LogMessage((object)("Assigning role " + NuageRolesPlugin.Settings.GetRoleDisplayName(10) + "."));

                        __instance.CrouchSpeed = currentCrouchSpeed;
                        __instance.MoveSpeed = currentMoveSpeed;
                        __instance.SprintSpeed = currentSprintSpeed;
                        __instance.JumpForce = currentJumpForce;
                        __instance.EnergyStart = currentMaxEnergy;
                        __instance.EnergyCurrent = currentEnergy;

                        NuageRolesPlugin.GUIinstance.text = NuageRolesPlugin.Settings.GetRoleDisplayName(10);
                        NuageRolesPlugin.GUIinstance.color = Color.white;
                        NuageRolesPlugin.GUIinstance.descText = NuageRolesPlugin.Settings.GetRoleDescription(10) + "\n" + NexusLocalization.ContinuePrompt(NuageRolesPlugin.Settings.ToggleKey.Value);
                        NuageRolesPlugin.GUIinstance.descColor = Color.white;
                        break;
                    }
                case 11: // Engineer
                    {
                        if (!NuageRolesPlugin.Settings.EnableEngineer.Value)
                        {
                            assignRandomEnabledRole();
                            break;
                        }
                        guiManager.ResetManaUI();
                        NuageRolesPlugin.Logger.LogInfo("Resetting Mana UI.");
                        NuageRolesPlugin.Logger.LogMessage((object)("Assigning role " + NuageRolesPlugin.Settings.GetRoleDisplayName(11) + "."));

                        isEngineer = true;

                        NuageRolesPlugin.GUIinstance.text = NuageRolesPlugin.Settings.GetRoleDisplayName(11);
                        NuageRolesPlugin.GUIinstance.color = new Color(0.8f, 0.5f, 0.2f);
                        NuageRolesPlugin.GUIinstance.descText = NuageRolesPlugin.Settings.GetRoleDescription(11) + "\n" +
                            NexusLocalization.Text("Hold an item and press [", "Держи предмет и нажми [", "Тримай предмет і натисни [") +
                            NuageRolesPlugin.Settings.ChargeItemKey.Value + NexusLocalization.Text("] to charge it.", "], чтобы зарядить его.", "], щоб зарядити його.") +
                            "\n" + NexusLocalization.ContinuePrompt(NuageRolesPlugin.Settings.ToggleKey.Value);
                        NuageRolesPlugin.GUIinstance.descColor = new Color(0.8f, 0.5f, 0.2f);
                        break;
                    }
                case 12: // Acrobat
                    {
                        if (!NuageRolesPlugin.Settings.EnableAcrobat.Value)
                        {
                            assignRandomEnabledRole();
                            break;
                        }

                        guiManager.ResetManaUI();
                        float healthMultiplier = MinMultiplier(NuageRolesPlugin.Settings.AcrobatHealthMultiplier.Value);
                        float strengthMultiplier = MinMultiplier(NuageRolesPlugin.Settings.AcrobatStrengthMultiplier.Value);
                        float speedMultiplier = MinMultiplier(NuageRolesPlugin.Settings.AcrobatSpeedMultiplier.Value);
                        setHealth(PlayerController.instance.playerSteamID,
                            ScaleHealthValue(currentMaxHealth, healthMultiplier), ScaleHealthValue(currentHealth, healthMultiplier));
                        modifyStrength(PlayerController.instance.playerSteamID, currentGrabStrength * strengthMultiplier);
                        __instance.CrouchSpeed = currentCrouchSpeed * speedMultiplier;
                        __instance.MoveSpeed = currentMoveSpeed * speedMultiplier;
                        __instance.SprintSpeed = currentSprintSpeed * speedMultiplier;
                        __instance.JumpForce = currentJumpForce + NonNegative(NuageRolesPlugin.Settings.AcrobatJumpForceBonus.Value);

                        NuageRolesPlugin.GUIinstance.text = NexusLocalization.RoleName(12);
                        NuageRolesPlugin.GUIinstance.color = new Color(0.3f, 0.9f, 0.7f);
                        NuageRolesPlugin.GUIinstance.descText = NexusLocalization.RoleDescription(12) + "\n" + NexusLocalization.ContinuePrompt(NuageRolesPlugin.Settings.ToggleKey.Value);
                        NuageRolesPlugin.GUIinstance.descColor = new Color(0.3f, 0.9f, 0.7f);
                        break;
                    }
                case 13: // Savior
                    {
                        if (!NuageRolesPlugin.Settings.EnableSavior.Value)
                        {
                            assignRandomEnabledRole();
                            break;
                        }

                        guiManager.ResetManaUI();
                        isSavior = true;
                        saviorManager?.SetRoleState(true);
                        bool sacrificeAll = NexusLocalization.IsAllSacrifice(NuageRolesPlugin.Settings.SaviorSacrificeMode.Value);
                        int partialPercent = Mathf.Clamp(NuageRolesPlugin.Settings.SaviorPartialCostPercent.Value, 10, 90);

                        NuageRolesPlugin.GUIinstance.text = NexusLocalization.RoleName(13);
                        NuageRolesPlugin.GUIinstance.color = new Color(0.95f, 0.8f, 0.35f);
                        NuageRolesPlugin.GUIinstance.descText = NexusLocalization.RoleDescription(13) + "\n" +
                            NexusLocalization.SaviorPrompt(NuageRolesPlugin.Settings.SaviorKey.Value, sacrificeAll, partialPercent) +
                            "\n" + NexusLocalization.ContinuePrompt(NuageRolesPlugin.Settings.ToggleKey.Value);
                        NuageRolesPlugin.GUIinstance.descColor = new Color(0.95f, 0.8f, 0.35f);
                        break;
                    }
            }
        }
    }
}
