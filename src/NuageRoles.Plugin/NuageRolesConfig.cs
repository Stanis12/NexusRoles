using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using MenuLib;
using MenuLib.MonoBehaviors;
using NuageRoles.Roles.Patches;
using UnityEngine;
using static MenuLib.MenuAPI;
using static MenuLib.MonoBehaviors.REPOSlider;

namespace NuageRoles
{
    public sealed class NuageRolesConfig
    {
        private static string RandomRoleName => NexusLocalization.RoleName(0);
        private static string DefaultRoleName => NexusLocalization.RoleName(4);
        private static string[] AllRoleChoices => new string[]
        {
            NexusLocalization.RoleName(0), NexusLocalization.RoleName(1), NexusLocalization.RoleName(2),
            NexusLocalization.RoleName(3), NexusLocalization.RoleName(4), NexusLocalization.RoleName(5),
            NexusLocalization.RoleName(6), NexusLocalization.RoleName(7), NexusLocalization.RoleName(8),
            NexusLocalization.RoleName(9), NexusLocalization.RoleName(10), NexusLocalization.RoleName(11),
            NexusLocalization.RoleName(12), NexusLocalization.RoleName(13)
        };

        private readonly ConfigFile configFile;

        private REPOSlider? roleSlider;
        private REPOPopupPage? configPage;
        private REPOButton? buttonOpen;
        private REPOButton? buttonOpenLobby;
        private REPOButton? buttonClose;
        private REPOToggle? afterDeathRoleToggle;
        private REPOToggle? mageTopMana;
        private Action? updateManaDisplay;

        public NuageRolesConfig(ConfigFile configFile)
        {
            this.configFile = configFile;
        }

        public ConfigEntry<KeyCode> ToggleKey { get; private set; } = null!;

        public ConfigEntry<KeyCode> ShowSpellsKey { get; private set; } = null!;

        public ConfigEntry<KeyCode> HealKey { get; private set; } = null!;

        public ConfigEntry<KeyCode> SpeedKey { get; private set; } = null!;

        public ConfigEntry<KeyCode> OverchargeKey { get; private set; } = null!;

        public ConfigEntry<KeyCode> JumpKey { get; private set; } = null!;

        public ConfigEntry<KeyCode> StaminaKey { get; private set; } = null!;

        public ConfigEntry<KeyCode> ScoutKey { get; private set; } = null!;

        public ConfigEntry<KeyCode> ChargeItemKey { get; private set; } = null!;

        public ConfigEntry<float> RunnerSpeedMultiplier { get; private set; } = null!;

        public ConfigEntry<float> RunnerStaminaMultiplier { get; private set; } = null!;

        public ConfigEntry<float> TankHealthMultiplier { get; private set; } = null!;

        public ConfigEntry<float> TankSpeedMultiplier { get; private set; } = null!;

        public ConfigEntry<int> TankPassiveRegenAmount { get; private set; } = null!;

        public ConfigEntry<int> TankPassiveRegenIntervalSeconds { get; private set; } = null!;

        public ConfigEntry<float> GamblerPositiveSpeedMultiplier { get; private set; } = null!;

        public ConfigEntry<float> GamblerPositiveStaminaMultiplier { get; private set; } = null!;

        public ConfigEntry<float> GamblerPositiveHealthMultiplier { get; private set; } = null!;

        public ConfigEntry<float> GamblerPositiveStrengthMultiplier { get; private set; } = null!;

        public ConfigEntry<float> GamblerPositiveJumpMultiplier { get; private set; } = null!;

        public ConfigEntry<float> GamblerNegativeSpeedMultiplier { get; private set; } = null!;

        public ConfigEntry<float> GamblerNegativeStaminaMultiplier { get; private set; } = null!;

        public ConfigEntry<float> GamblerNegativeHealthMultiplier { get; private set; } = null!;

        public ConfigEntry<float> GamblerNegativeStrengthMultiplier { get; private set; } = null!;

        public ConfigEntry<float> GamblerNegativeJumpMultiplier { get; private set; } = null!;

        public ConfigEntry<float> StrongmanStrengthMultiplier { get; private set; } = null!;

        public ConfigEntry<float> StrongmanStrengthBonus { get; private set; } = null!;

        public ConfigEntry<float> RangedLooterGrabRangeMultiplier { get; private set; } = null!;

        public ConfigEntry<float> RangedLooterStrengthMultiplier { get; private set; } = null!;

        public ConfigEntry<float> AthleticStrengthMultiplier { get; private set; } = null!;

        public ConfigEntry<int> AthleticStaminaBonus { get; private set; } = null!;

        public ConfigEntry<float> AthleticJumpForceBonus { get; private set; } = null!;

        public ConfigEntry<float> ReaperHealthMultiplier { get; private set; } = null!;

        public ConfigEntry<int> ReaperMaxHealthBonusPerKill { get; private set; } = null!;

        public ConfigEntry<int> ReaperHealAmountPerKill { get; private set; } = null!;

        public ConfigEntry<int> ReaperBuffCooldownFrames { get; private set; } = null!;

        public ConfigEntry<float> ScoutStaminaMultiplier { get; private set; } = null!;

        public ConfigEntry<float> ScoutSprintRechargeMultiplier { get; private set; } = null!;

        public ConfigEntry<int> ScoutAbilityDurationSeconds { get; private set; } = null!;

        public ConfigEntry<int> ScoutAbilityCooldownSeconds { get; private set; } = null!;

        public ConfigEntry<int> MageMaxMana { get; private set; } = null!;

        public ConfigEntry<int> MageHealAmount { get; private set; } = null!;

        public ConfigEntry<float> MageSpeedMultiplier { get; private set; } = null!;

        public ConfigEntry<float> MageJumpForceBonus { get; private set; } = null!;

        public ConfigEntry<float> MageOverchargeMultiplier { get; private set; } = null!;

        public ConfigEntry<int> MageSpeedBoostDurationSeconds { get; private set; } = null!;

        public ConfigEntry<int> MageJumpBoostDurationSeconds { get; private set; } = null!;

        public ConfigEntry<int> MageOverchargeDurationSeconds { get; private set; } = null!;

        public ConfigEntry<int> EngineerChargeStaminaCost { get; private set; } = null!;

        public ConfigEntry<int> EngineerStaminaRestoreAmount { get; private set; } = null!;

        public ConfigEntry<int> EngineerStaminaRestoreIntervalSeconds { get; private set; } = null!;

        public ConfigEntry<string> CustomRoleNameRunner { get; private set; } = null!;

        public ConfigEntry<string> CustomRoleNameTank { get; private set; } = null!;

        public ConfigEntry<string> CustomRoleNameGambler { get; private set; } = null!;

        public ConfigEntry<string> CustomRoleNameStrongman { get; private set; } = null!;

        public ConfigEntry<string> CustomRoleNameRL { get; private set; } = null!;

        public ConfigEntry<string> CustomRoleNameAthletic { get; private set; } = null!;

        public ConfigEntry<string> CustomRoleNameMage { get; private set; } = null!;

        public ConfigEntry<string> CustomRoleNameReaper { get; private set; } = null!;

        public ConfigEntry<string> CustomRoleNameScout { get; private set; } = null!;

        public ConfigEntry<string> CustomRoleNameRegular { get; private set; } = null!;

        public ConfigEntry<string> CustomRoleNameEngineer { get; private set; } = null!;

        public ConfigEntry<string> CustomRoleDecRunner { get; private set; } = null!;

        public ConfigEntry<string> CustomRoleDecTank { get; private set; } = null!;

        public ConfigEntry<string> CustomRoleDecGambler { get; private set; } = null!;

        public ConfigEntry<string> CustomRoleDecStrongman { get; private set; } = null!;

        public ConfigEntry<string> CustomRoleDecRL { get; private set; } = null!;

        public ConfigEntry<string> CustomRoleDecAthletic { get; private set; } = null!;

        public ConfigEntry<string> CustomRoleDecMage { get; private set; } = null!;

        public ConfigEntry<string> CustomRoleDecReaper { get; private set; } = null!;

        public ConfigEntry<string> CustomRoleDecScout { get; private set; } = null!;

        public ConfigEntry<string> CustomRoleDecRegular { get; private set; } = null!;

        public ConfigEntry<string> CustomRoleDecEngineer { get; private set; } = null!;

        public ConfigEntry<string> SavedRole { get; private set; } = null!;

        public ConfigEntry<bool> EnableRoleUpgrades { get; private set; } = null!;

        public ConfigEntry<bool> AssignRoleAfterRevive { get; private set; } = null!;

        public ConfigEntry<bool> MageTopManaConf { get; private set; } = null!;

        public ConfigEntry<bool> ShowGUIAtStart { get; private set; } = null!;

        public ConfigEntry<bool> EnableRunner { get; private set; } = null!;

        public ConfigEntry<bool> EnableTank { get; private set; } = null!;

        public ConfigEntry<bool> EnableGambler { get; private set; } = null!;

        public ConfigEntry<bool> EnableStrongman { get; private set; } = null!;

        public ConfigEntry<bool> EnableRL { get; private set; } = null!;

        public ConfigEntry<bool> EnableAthletic { get; private set; } = null!;

        public ConfigEntry<bool> EnableMage { get; private set; } = null!;

        public ConfigEntry<bool> EnableReaper { get; private set; } = null!;

        public ConfigEntry<bool> EnableScout { get; private set; } = null!;

        public ConfigEntry<bool> EnableRegular { get; private set; } = null!;

        public ConfigEntry<bool> EnableEngineer { get; private set; } = null!;

        public ConfigEntry<bool> EnableAcrobat { get; private set; } = null!;

        public ConfigEntry<bool> EnableSavior { get; private set; } = null!;

        public ConfigEntry<float> AcrobatHealthMultiplier { get; private set; } = null!;

        public ConfigEntry<float> AcrobatStrengthMultiplier { get; private set; } = null!;

        public ConfigEntry<float> AcrobatSpeedMultiplier { get; private set; } = null!;

        public ConfigEntry<float> AcrobatJumpForceBonus { get; private set; } = null!;

        public ConfigEntry<KeyCode> SaviorKey { get; private set; } = null!;

        public ConfigEntry<string> SaviorSacrificeMode { get; private set; } = null!;

        public ConfigEntry<int> SaviorPartialCostPercent { get; private set; } = null!;

        public ConfigEntry<int> SaviorReviveHealth { get; private set; } = null!;

        public bool AfterDeathNewRole => AssignRoleAfterRevive.Value;

        public bool MageTopManaTop => MageTopManaConf.Value;

        public string ChosenRoleName => GetSelectableRoleName(SavedRole.Value);

        public string GetRoleDisplayName(int roleId)
        {
            ConfigEntry<string>? entry = roleId switch
            {
                1 => CustomRoleNameRunner,
                2 => CustomRoleNameTank,
                3 => CustomRoleNameGambler,
                4 => CustomRoleNameStrongman,
                5 => CustomRoleNameRL,
                6 => CustomRoleNameAthletic,
                7 => CustomRoleNameMage,
                8 => CustomRoleNameReaper,
                9 => CustomRoleNameScout,
                10 => CustomRoleNameRegular,
                11 => CustomRoleNameEngineer,
                _ => null
            };

            if (entry == null || NexusLocalization.IsDefaultRoleName(roleId, entry.Value))
            {
                return NexusLocalization.RoleName(roleId);
            }

            return entry.Value;
        }

        public string GetRoleDescription(int roleId)
        {
            ConfigEntry<string>? entry = roleId switch
            {
                1 => CustomRoleDecRunner,
                2 => CustomRoleDecTank,
                3 => CustomRoleDecGambler,
                4 => CustomRoleDecStrongman,
                5 => CustomRoleDecRL,
                6 => CustomRoleDecAthletic,
                7 => CustomRoleDecMage,
                8 => CustomRoleDecReaper,
                9 => CustomRoleDecScout,
                10 => CustomRoleDecRegular,
                11 => CustomRoleDecEngineer,
                _ => null
            };

            if (entry == null || NexusLocalization.IsDefaultRoleDescription(roleId, entry.Value))
            {
                return NexusLocalization.RoleDescription(roleId);
            }

            return entry.Value;
        }

        public void Bind()
        {
            SavedRole = configFile.Bind(
                new ConfigDefinition("Role", "Your selected role"),
                DefaultRoleName,
                new ConfigDescription(NexusLocalization.Text("Preferred role for this player. Disabled roles temporarily act as Random.", "Предпочитаемая роль игрока. Если роль отключена, временно выбирается случайная роль.", "Бажана роль гравця. Якщо роль вимкнено, тимчасово обирається випадкова роль."), new AcceptableValueList<string>(AllRoleChoices)));
            AssignRoleAfterRevive = configFile.Bind(
                new ConfigDefinition("Role", "Reaasign roles after being revived"),
                false,
                new ConfigDescription(NexusLocalization.Text("When enabled, a player receives a new role after dying and being revived.", "Если включено, после смерти и воскрешения игрок получает новую роль.", "Якщо ввімкнено, після смерті та воскресіння гравець отримує нову роль.")));
            MageTopManaConf = configFile.Bind(
                new ConfigDefinition("Role", "Place where mana is shown"),
                true,
                new ConfigDescription(NexusLocalization.Text("Controls Mage mana display placement. True shows mana at the top; false shows it beside stamina.", "Положение маны Мага: сверху или рядом с индикатором выносливости.", "Визначає позицію мани Мага: зверху або поруч з індикатором витривалості.")));
            ShowGUIAtStart = configFile.Bind(
                new ConfigDefinition("GUI", "Show GUI when being assigned a role"),
                true,
                new ConfigDescription(NexusLocalization.Text("Shows the role information overlay when a role is assigned.", "Показывает описание роли после её назначения.", "Показує опис ролі після її призначення.")));
            EnableRoleUpgrades = configFile.Bind(
                new ConfigDefinition("Upgrades", "Enable Role Upgrades in Shop"),
                true,
                new ConfigDescription(NexusLocalization.Text("Enables NexusRoles upgrade items in the shop.", "Добавляет предметы улучшения ролей в магазин.", "Додає предмети покращення ролей до магазину.")));

            EnableRunner = configFile.Bind(new ConfigDefinition("Role", "Enable Runner"), true, new ConfigDescription(NexusLocalization.Text("Allows Runner to be selected or randomly assigned.", "Позволяет выбрать Бегуна или получить эту роль случайно.", "Дозволяє обрати Бігуна або отримати цю роль випадково.")));
            EnableTank = configFile.Bind(new ConfigDefinition("Role", "Enable Tank"), true, new ConfigDescription(NexusLocalization.Text("Allows Tank to be selected or randomly assigned.", "Позволяет выбрать Танка или получить эту роль случайно.", "Дозволяє обрати Танка або отримати цю роль випадково.")));
            EnableGambler = configFile.Bind(new ConfigDefinition("Role", "Enable Gambler"), true, new ConfigDescription(NexusLocalization.Text("Allows Gambler to be selected or randomly assigned.", "Позволяет выбрать Игрока или получить эту роль случайно.", "Дозволяє обрати Гравця або отримати цю роль випадково.")));
            EnableStrongman = configFile.Bind(new ConfigDefinition("Role", "Enable Strongman"), true, new ConfigDescription(NexusLocalization.Text("Allows Strongman to be selected or randomly assigned.", "Позволяет выбрать Силача или получить эту роль случайно.", "Дозволяє обрати Силача або отримати цю роль випадково.")));
            EnableRL = configFile.Bind(new ConfigDefinition("Role", "Enable Ranged Looter"), true, new ConfigDescription(NexusLocalization.Text("Allows Ranged Looter to be selected or randomly assigned.", "Позволяет выбрать Дальнобойщика или получить эту роль случайно.", "Дозволяє обрати Далекобійника або отримати цю роль випадково.")));
            EnableAthletic = configFile.Bind(new ConfigDefinition("Role", "Enable Athletic"), true, new ConfigDescription(NexusLocalization.Text("Allows Athletic to be selected or randomly assigned.", "Позволяет выбрать Атлета или получить эту роль случайно.", "Дозволяє обрати Атлета або отримати цю роль випадково.")));
            EnableMage = configFile.Bind(new ConfigDefinition("Role", "Enable Mage"), true, new ConfigDescription(NexusLocalization.Text("Allows Mage to be selected or randomly assigned.", "Позволяет выбрать Мага или получить эту роль случайно.", "Дозволяє обрати Мага або отримати цю роль випадково.")));
            EnableReaper = configFile.Bind(new ConfigDefinition("Role", "Enable Reaper"), true, new ConfigDescription(NexusLocalization.Text("Allows Reaper to be selected or randomly assigned.", "Позволяет выбрать Жнеца или получить эту роль случайно.", "Дозволяє обрати Жнеця або отримати цю роль випадково.")));
            EnableScout = configFile.Bind(new ConfigDefinition("Role", "Enable Scout"), true, new ConfigDescription(NexusLocalization.Text("Allows Scout to be selected or randomly assigned.", "Позволяет выбрать Разведчика или получить эту роль случайно.", "Дозволяє обрати Розвідника або отримати цю роль випадково.")));
            EnableRegular = configFile.Bind(new ConfigDefinition("Role", "Enable Regular"), true, new ConfigDescription(NexusLocalization.Text("Allows Regular to be selected or randomly assigned.", "Позволяет выбрать Обычного или получить эту роль случайно.", "Дозволяє обрати Звичайного або отримати цю роль випадково.")));
            EnableEngineer = configFile.Bind(new ConfigDefinition("Role", "Enable Engineer"), true, new ConfigDescription(NexusLocalization.Text("Allows Engineer to be selected or randomly assigned.", "Позволяет выбрать Инженера или получить эту роль случайно.", "Дозволяє обрати Інженера або отримати цю роль випадково.")));
            EnableAcrobat = configFile.Bind(new ConfigDefinition("Role", "Enable Acrobat"), true, new ConfigDescription(NexusLocalization.Text("Allows Acrobat to be selected or randomly assigned.", "Позволяет выбирать Акробата или получить эту роль случайно.", "Дозволяє обрати Акробата або отримати цю роль випадково.")));
            EnableSavior = configFile.Bind(new ConfigDefinition("Role", "Enable Savior"), true, new ConfigDescription(NexusLocalization.Text("Allows Savior to be selected or randomly assigned.", "Позволяет выбирать Спасителя или получить эту роль случайно.", "Дозволяє обрати Рятівника або отримати цю роль випадково.")));

            ToggleKey = configFile.Bind(new ConfigDefinition("GUI", "GUI Toggle Key"), (KeyCode)114, new ConfigDescription("Key used to toggle the role information overlay."));
            ScoutKey = configFile.Bind(new ConfigDefinition("GUI", "Activate Scout Ability"), (KeyCode)103, new ConfigDescription("Key used to activate the Scout enemy marker ability."));
            ShowSpellsKey = configFile.Bind(new ConfigDefinition("Mage", "Show Spells"), (KeyCode)109, new ConfigDescription("Key used to show or hide the Mage spell list."));
            HealKey = configFile.Bind(new ConfigDefinition("Mage", "Healing Spell"), (KeyCode)104, new ConfigDescription("Key used to cast Mage heal."));
            SpeedKey = configFile.Bind(new ConfigDefinition("Mage", "Speed Spell"), (KeyCode)106, new ConfigDescription("Key used to cast Mage speed boost."));
            OverchargeKey = configFile.Bind(new ConfigDefinition("Mage", "Overcharge Spell"), (KeyCode)111, new ConfigDescription("Key used to cast Mage overcharge."));
            JumpKey = configFile.Bind(new ConfigDefinition("Mage", "Jump Boost Spell"), (KeyCode)107, new ConfigDescription("Key used to cast Mage jump boost."));
            StaminaKey = configFile.Bind(new ConfigDefinition("Mage", "Stamina Refill Spell"), (KeyCode)110, new ConfigDescription("Key used to cast Mage stamina refill."));
            ChargeItemKey = configFile.Bind(new ConfigDefinition("Engineer", "Charge Held Item"), (KeyCode)99, new ConfigDescription("Key used by Engineer to charge the held item."));
            SaviorKey = configFile.Bind(new ConfigDefinition("Savior", "Revive Ally"), KeyCode.V, new ConfigDescription(NexusLocalization.Text("Key used to revive a nearby fallen ally.", "Клавиша для воскрешения павшего союзника поблизости.", "Клавіша для воскресіння союзника, що впав, поруч.")));
            SaviorSacrificeMode = configFile.Bind(new ConfigDefinition("Savior", "Health Sacrifice"), NexusLocalization.Text("Part", "Часть", "Частина"), new ConfigDescription(NexusLocalization.Text("Choose whether to sacrifice part or all of your current health.", "Выберите, пожертвовать частью или всем текущим здоровьем.", "Оберіть, пожертвувати частиною чи всім поточним здоров’ям."), new AcceptableValueList<string>(NexusLocalization.Text("Part", "Часть", "Частина"), NexusLocalization.Text("All", "Всё", "Усе"))));
            string localizedSacrificeMode = NexusLocalization.IsAllSacrifice(SaviorSacrificeMode.Value)
                ? NexusLocalization.Text("All", "Всё", "Усе")
                : NexusLocalization.Text("Part", "Часть", "Частина");
            if (SaviorSacrificeMode.Value != localizedSacrificeMode)
            {
                SaviorSacrificeMode.Value = localizedSacrificeMode;
            }
            SaviorPartialCostPercent = configFile.Bind(new ConfigDefinition("Savior", "Partial Health Cost Percent"), 50, new ConfigDescription(NexusLocalization.Text("Percent of current health sacrificed in Part mode.", "Процент текущего здоровья, который тратится в режиме «Часть».", "Відсоток поточного здоров’я, що витрачається в режимі «Частина»."), new AcceptableValueRange<int>(10, 90)));
            SaviorReviveHealth = configFile.Bind(new ConfigDefinition("Savior", "Ally Revive Health"), 25, new ConfigDescription(NexusLocalization.Text("Health restored to the revived ally, including the 1 HP granted by the game.", "Здоровье, которое получит воскресший союзник, включая 1 HP, восстанавливаемое игрой.", "Здоров’я, яке отримає воскреслий союзник, включно з 1 HP від гри."), new AcceptableValueRange<int>(1, 100)));

            RunnerSpeedMultiplier = configFile.Bind(
                new ConfigDefinition("Runner", "Speed Multiplier"),
                1.5f,
                new ConfigDescription("Multiplier applied to Runner crouch, walk, and sprint speed.", new AcceptableValueRange<float>(0.1f, 5f)));
            RunnerStaminaMultiplier = configFile.Bind(
                new ConfigDefinition("Runner", "Stamina Multiplier"),
                1.5f,
                new ConfigDescription("Multiplier applied to Runner max and current stamina.", new AcceptableValueRange<float>(0.1f, 5f)));

            TankHealthMultiplier = configFile.Bind(
                new ConfigDefinition("Tank", "Health Multiplier"),
                2f,
                new ConfigDescription("Multiplier applied to Tank max and current health.", new AcceptableValueRange<float>(0.1f, 10f)));
            TankSpeedMultiplier = configFile.Bind(
                new ConfigDefinition("Tank", "Speed Multiplier"),
                0.9f,
                new ConfigDescription("Multiplier applied to Tank crouch, walk, and sprint speed.", new AcceptableValueRange<float>(0.1f, 5f)));
            TankPassiveRegenAmount = configFile.Bind(
                new ConfigDefinition("Tank", "Passive Regen Amount"),
                1,
                new ConfigDescription("Health restored each Tank passive regeneration tick.", new AcceptableValueRange<int>(0, 100)));
            TankPassiveRegenIntervalSeconds = configFile.Bind(
                new ConfigDefinition("Tank", "Passive Regen Interval Seconds"),
                5,
                new ConfigDescription("Seconds between Tank passive regeneration ticks.", new AcceptableValueRange<int>(1, 300)));

            GamblerPositiveSpeedMultiplier = configFile.Bind(
                new ConfigDefinition("Gambler", "Positive Speed Multiplier"),
                1.3f,
                new ConfigDescription("Positive Gambler speed effect multiplier.", new AcceptableValueRange<float>(0.1f, 5f)));
            GamblerPositiveStaminaMultiplier = configFile.Bind(
                new ConfigDefinition("Gambler", "Positive Stamina Multiplier"),
                1.8f,
                new ConfigDescription("Positive Gambler stamina effect multiplier.", new AcceptableValueRange<float>(0.1f, 10f)));
            GamblerPositiveHealthMultiplier = configFile.Bind(
                new ConfigDefinition("Gambler", "Positive Health Multiplier"),
                1.8f,
                new ConfigDescription("Positive Gambler health effect multiplier.", new AcceptableValueRange<float>(0.1f, 10f)));
            GamblerPositiveStrengthMultiplier = configFile.Bind(
                new ConfigDefinition("Gambler", "Positive Strength Multiplier"),
                1.3f,
                new ConfigDescription("Positive Gambler grab strength effect multiplier.", new AcceptableValueRange<float>(0.1f, 10f)));
            GamblerPositiveJumpMultiplier = configFile.Bind(
                new ConfigDefinition("Gambler", "Positive Jump Multiplier"),
                1.5f,
                new ConfigDescription("Positive Gambler jump force effect multiplier.", new AcceptableValueRange<float>(0.1f, 10f)));
            GamblerNegativeSpeedMultiplier = configFile.Bind(
                new ConfigDefinition("Gambler", "Negative Speed Multiplier"),
                0.8f,
                new ConfigDescription("Negative Gambler speed effect multiplier.", new AcceptableValueRange<float>(0.1f, 5f)));
            GamblerNegativeStaminaMultiplier = configFile.Bind(
                new ConfigDefinition("Gambler", "Negative Stamina Multiplier"),
                0.8f,
                new ConfigDescription("Negative Gambler stamina effect multiplier.", new AcceptableValueRange<float>(0.1f, 10f)));
            GamblerNegativeHealthMultiplier = configFile.Bind(
                new ConfigDefinition("Gambler", "Negative Health Multiplier"),
                0.8f,
                new ConfigDescription("Negative Gambler health effect multiplier.", new AcceptableValueRange<float>(0.1f, 10f)));
            GamblerNegativeStrengthMultiplier = configFile.Bind(
                new ConfigDefinition("Gambler", "Negative Strength Multiplier"),
                0.8f,
                new ConfigDescription("Negative Gambler grab strength effect multiplier.", new AcceptableValueRange<float>(0.1f, 10f)));
            GamblerNegativeJumpMultiplier = configFile.Bind(
                new ConfigDefinition("Gambler", "Negative Jump Multiplier"),
                0.7f,
                new ConfigDescription("Negative Gambler jump force effect multiplier.", new AcceptableValueRange<float>(0.1f, 10f)));

            StrongmanStrengthMultiplier = configFile.Bind(
                new ConfigDefinition("Strongman", "Strength Multiplier"),
                1.5f,
                new ConfigDescription("Multiplier applied to Strongman grab strength before the flat bonus.", new AcceptableValueRange<float>(0.1f, 10f)));
            StrongmanStrengthBonus = configFile.Bind(
                new ConfigDefinition("Strongman", "Strength Bonus"),
                0.5f,
                new ConfigDescription("Flat grab strength bonus added after the Strongman multiplier.", new AcceptableValueRange<float>(0f, 10f)));

            RangedLooterGrabRangeMultiplier = configFile.Bind(
                new ConfigDefinition("Ranged Looter", "Grab Range Multiplier"),
                2.5f,
                new ConfigDescription("Multiplier applied to Ranged Looter grab range.", new AcceptableValueRange<float>(0.1f, 10f)));
            RangedLooterStrengthMultiplier = configFile.Bind(
                new ConfigDefinition("Ranged Looter", "Strength Multiplier"),
                1.2f,
                new ConfigDescription("Multiplier applied to Ranged Looter grab strength.", new AcceptableValueRange<float>(0.1f, 10f)));

            AthleticStrengthMultiplier = configFile.Bind(
                new ConfigDefinition("Athletic", "Strength Multiplier"),
                1.4f,
                new ConfigDescription("Multiplier applied to Athletic grab strength.", new AcceptableValueRange<float>(0.1f, 10f)));
            AthleticStaminaBonus = configFile.Bind(
                new ConfigDefinition("Athletic", "Stamina Bonus"),
                20,
                new ConfigDescription("Flat max and current stamina bonus applied to Athletic.", new AcceptableValueRange<int>(0, 300)));
            AthleticJumpForceBonus = configFile.Bind(
                new ConfigDefinition("Athletic", "Jump Force Bonus"),
                3f,
                new ConfigDescription("Flat jump force bonus applied to Athletic.", new AcceptableValueRange<float>(0f, 50f)));

            AcrobatHealthMultiplier = configFile.Bind(
                new ConfigDefinition("Acrobat", "Health Multiplier"), 0.75f,
                new ConfigDescription(NexusLocalization.Text("Multiplier applied to Acrobat max and current health.", "Множитель максимального и текущего здоровья Акробата.", "Множник максимального й поточного здоров’я Акробата."), new AcceptableValueRange<float>(0.1f, 2f)));
            AcrobatStrengthMultiplier = configFile.Bind(
                new ConfigDefinition("Acrobat", "Strength Multiplier"), 0.75f,
                new ConfigDescription(NexusLocalization.Text("Multiplier applied to Acrobat grab strength.", "Множитель силы захвата Акробата.", "Множник сили захоплення Акробата."), new AcceptableValueRange<float>(0.1f, 2f)));
            AcrobatSpeedMultiplier = configFile.Bind(
                new ConfigDefinition("Acrobat", "Speed Multiplier"), 1.1f,
                new ConfigDescription(NexusLocalization.Text("Multiplier applied to Acrobat movement speed.", "Множитель скорости передвижения Акробата.", "Множник швидкості руху Акробата."), new AcceptableValueRange<float>(0.1f, 3f)));
            AcrobatJumpForceBonus = configFile.Bind(
                new ConfigDefinition("Acrobat", "Jump Force Bonus"), 3f,
                new ConfigDescription(NexusLocalization.Text("Flat jump force bonus applied to Acrobat.", "Бонус к силе прыжка Акробата.", "Бонус до сили стрибка Акробата."), new AcceptableValueRange<float>(0f, 50f)));

            ReaperHealthMultiplier = configFile.Bind(
                new ConfigDefinition("Reaper", "Health Multiplier"),
                1.5f,
                new ConfigDescription("Multiplier applied to Reaper max and current health when the role is assigned.", new AcceptableValueRange<float>(0.1f, 10f)));
            ReaperMaxHealthBonusPerKill = configFile.Bind(
                new ConfigDefinition("Reaper", "Max Health Bonus Per Enemy"),
                5,
                new ConfigDescription("Max health gained by each living Reaper when an enemy dies.", new AcceptableValueRange<int>(0, 1000)));
            ReaperHealAmountPerKill = configFile.Bind(
                new ConfigDefinition("Reaper", "Heal Amount Per Enemy"),
                30,
                new ConfigDescription("Health restored by each living Reaper when an enemy dies.", new AcceptableValueRange<int>(0, 1000)));
            ReaperBuffCooldownFrames = configFile.Bind(
                new ConfigDefinition("Reaper", "Buff Cooldown Frames"),
                50,
                new ConfigDescription("Update frames a Reaper must wait before receiving another enemy-death buff.", new AcceptableValueRange<int>(0, 10000)));

            ScoutStaminaMultiplier = configFile.Bind(
                new ConfigDefinition("Scout", "Stamina Multiplier"),
                2f,
                new ConfigDescription("Multiplier applied to Scout max and current stamina.", new AcceptableValueRange<float>(0.1f, 10f)));
            ScoutSprintRechargeMultiplier = configFile.Bind(
                new ConfigDefinition("Scout", "Sprint Recharge Multiplier"),
                2f,
                new ConfigDescription("Multiplier applied to Scout stamina recharge amount.", new AcceptableValueRange<float>(0.1f, 10f)));
            ScoutAbilityDurationSeconds = configFile.Bind(
                new ConfigDefinition("Scout", "Ability Duration Seconds"),
                5,
                new ConfigDescription("Duration of the Scout enemy marker ability.", new AcceptableValueRange<int>(1, 300)));
            ScoutAbilityCooldownSeconds = configFile.Bind(
                new ConfigDefinition("Scout", "Ability Cooldown Seconds"),
                40,
                new ConfigDescription("Base cooldown of the Scout enemy marker ability before upgrade reductions.", new AcceptableValueRange<int>(0, 600)));

            MageMaxMana = configFile.Bind(
                new ConfigDefinition("Mage", "Max Mana"),
                8,
                new ConfigDescription("Maximum mana available to the Mage.", new AcceptableValueRange<int>(3, 12)));
            MageHealAmount = configFile.Bind(
                new ConfigDefinition("Mage", "Heal Amount"),
                5,
                new ConfigDescription("Health restored by the Mage heal spell before overcharge is applied.", new AcceptableValueRange<int>(1, 100)));
            MageSpeedMultiplier = configFile.Bind(
                new ConfigDefinition("Mage", "Speed Multiplier"),
                1.4f,
                new ConfigDescription("Movement speed multiplier applied by the Mage speed spell.", new AcceptableValueRange<float>(0.1f, 5f)));
            MageJumpForceBonus = configFile.Bind(
                new ConfigDefinition("Mage", "Jump Force Bonus"),
                3f,
                new ConfigDescription("Flat JumpForce bonus applied by the Mage jump spell.", new AcceptableValueRange<float>(0f, 50f)));
            MageOverchargeMultiplier = configFile.Bind(
                new ConfigDefinition("Mage", "Overcharge Multiplier"),
                2f,
                new ConfigDescription("Multiplier applied by overcharge to heal amount and boost durations.", new AcceptableValueRange<float>(1f, 10f)));
            MageSpeedBoostDurationSeconds = configFile.Bind(
                new ConfigDefinition("Mage", "Speed Boost Duration Seconds"),
                30,
                new ConfigDescription("Base duration of the Mage speed spell in seconds.", new AcceptableValueRange<int>(1, 300)));
            MageJumpBoostDurationSeconds = configFile.Bind(
                new ConfigDefinition("Mage", "Jump Boost Duration Seconds"),
                30,
                new ConfigDescription("Base duration of the Mage jump spell in seconds.", new AcceptableValueRange<int>(1, 300)));
            MageOverchargeDurationSeconds = configFile.Bind(
                new ConfigDefinition("Mage", "Overcharge Duration Seconds"),
                20,
                new ConfigDescription("Duration of the Mage overcharge spell in seconds.", new AcceptableValueRange<int>(1, 300)));

            EngineerChargeStaminaCost = configFile.Bind(
                new ConfigDefinition("Engineer", "Charge Stamina Cost"),
                10,
                new ConfigDescription("Max stamina sacrificed when charging an item. The sacrificed stamina is restored over time.", new AcceptableValueRange<int>(0, 100)));
            EngineerStaminaRestoreAmount = configFile.Bind(
                new ConfigDefinition("Engineer", "Stamina Restore Amount"),
                5,
                new ConfigDescription("Amount of sacrificed max stamina restored each restore tick.", new AcceptableValueRange<int>(0, 100)));
            EngineerStaminaRestoreIntervalSeconds = configFile.Bind(
                new ConfigDefinition("Engineer", "Stamina Restore Interval Seconds"),
                15,
                new ConfigDescription("Seconds between Engineer stamina restore ticks.", new AcceptableValueRange<int>(1, 300)));

            CustomRoleNameRunner = configFile.Bind(new ConfigDefinition("Role Names", "Runner Name"), NexusLocalization.RoleName(1), new ConfigDescription("Display name for the Runner role."));
            CustomRoleNameTank = configFile.Bind(new ConfigDefinition("Role Names", "Tank Name"), NexusLocalization.RoleName(2), new ConfigDescription("Display name for the Tank role."));
            CustomRoleNameGambler = configFile.Bind(new ConfigDefinition("Role Names", "Gambler Name"), NexusLocalization.RoleName(3), new ConfigDescription("Display name for the Gambler role."));
            CustomRoleNameStrongman = configFile.Bind(new ConfigDefinition("Role Names", "Strongman Name"), NexusLocalization.RoleName(4), new ConfigDescription("Display name for the Strongman role."));
            CustomRoleNameRL = configFile.Bind(new ConfigDefinition("Role Names", "Ranged Looter Name"), NexusLocalization.RoleName(5), new ConfigDescription("Display name for the Ranged Looter role."));
            CustomRoleNameAthletic = configFile.Bind(new ConfigDefinition("Role Names", "Athletic Name"), NexusLocalization.RoleName(6), new ConfigDescription("Display name for the Athletic role."));
            CustomRoleNameMage = configFile.Bind(new ConfigDefinition("Role Names", "Mage Name"), NexusLocalization.RoleName(7), new ConfigDescription("Display name for the Mage role."));
            CustomRoleNameReaper = configFile.Bind(new ConfigDefinition("Role Names", "Reaper Name"), NexusLocalization.RoleName(8), new ConfigDescription("Display name for the Reaper role."));
            CustomRoleNameScout = configFile.Bind(new ConfigDefinition("Role Names", "Scout Name"), NexusLocalization.RoleName(9), new ConfigDescription("Display name for the Scout role."));
            CustomRoleNameRegular = configFile.Bind(new ConfigDefinition("Role Names", "Regular Name"), NexusLocalization.RoleName(10), new ConfigDescription("Display name for the Regular role."));
            CustomRoleNameEngineer = configFile.Bind(new ConfigDefinition("Role Names", "Engineer Name"), NexusLocalization.RoleName(11), new ConfigDescription("Display name for the Engineer role."));

            CustomRoleDecRunner = configFile.Bind(new ConfigDefinition("Role Descriptions", "Runner Description"), NexusLocalization.RoleDescription(1), new ConfigDescription("Role assignment text shown for Runner."));
            CustomRoleDecTank = configFile.Bind(new ConfigDefinition("Role Descriptions", "Tank Description"), NexusLocalization.RoleDescription(2), new ConfigDescription("Role assignment text shown for Tank."));
            CustomRoleDecGambler = configFile.Bind(new ConfigDefinition("Role Descriptions", "Gambler Description"), NexusLocalization.RoleDescription(3), new ConfigDescription("Role assignment text shown for Gambler before the rolled effects."));
            CustomRoleDecStrongman = configFile.Bind(new ConfigDefinition("Role Descriptions", "Strongman Description"), NexusLocalization.RoleDescription(4), new ConfigDescription("Role assignment text shown for Strongman."));
            CustomRoleDecRL = configFile.Bind(new ConfigDefinition("Role Descriptions", "Ranged Looter Description"), NexusLocalization.RoleDescription(5), new ConfigDescription("Role assignment text shown for Ranged Looter."));
            CustomRoleDecAthletic = configFile.Bind(new ConfigDefinition("Role Descriptions", "Athletic Description"), NexusLocalization.RoleDescription(6), new ConfigDescription("Role assignment text shown for Athletic."));
            CustomRoleDecMage = configFile.Bind(new ConfigDefinition("Role Descriptions", "Mage Description"), NexusLocalization.RoleDescription(7), new ConfigDescription("Role assignment text shown for Mage."));
            CustomRoleDecReaper = configFile.Bind(new ConfigDefinition("Role Descriptions", "Reaper Description"), NexusLocalization.RoleDescription(8), new ConfigDescription("Role assignment text shown for Reaper."));
            CustomRoleDecScout = configFile.Bind(new ConfigDefinition("Role Descriptions", "Scout Description"), NexusLocalization.RoleDescription(9), new ConfigDescription("Role assignment text shown for Scout. [G] is replaced with the configured Scout key."));
            CustomRoleDecRegular = configFile.Bind(new ConfigDefinition("Role Descriptions", "Regular Description"), NexusLocalization.RoleDescription(10), new ConfigDescription("Role assignment text shown for Regular."));
            CustomRoleDecEngineer = configFile.Bind(new ConfigDefinition("Role Descriptions", "Engineer Description"), NexusLocalization.RoleDescription(11), new ConfigDescription("Role assignment text shown for Engineer."));

            ValidateSavedRole();
        }

        public void RegisterMenu(Action updateManaDisplay)
        {
            this.updateManaDisplay = updateManaDisplay;

            MenuAPI.AddElementToSettingsMenu((BuilderDelegate)delegate (Transform parent)
            {
                EnsureConfigPage();
                buttonOpen = MenuAPI.CreateREPOButton(NexusLocalization.Text("NexusRoles Settings", "Настройки NexusRoles", "Налаштування NexusRoles"), (Action)delegate
                {
                    RefreshRoleSlider();
                    EnsureConfigPage().OpenPage(false);
                }, parent, new Vector2(500f, 10f));
            });

            MenuAPI.AddElementToLobbyMenu((BuilderDelegate)delegate (Transform parent)
            {
                buttonOpenLobby = MenuAPI.CreateREPOButton(NexusLocalization.Text("NexusRoles Settings", "Настройки NexusRoles", "Налаштування NexusRoles"), (Action)delegate
                {
                    RefreshRoleSlider();
                    EnsureConfigPage().OpenPage(false);
                }, parent, new Vector2(500f, 500f));
            });
        }

        public int GetSelectedRoleId(System.Random random)
        {
            string roleName = GetSelectableRoleName(SavedRole.Value);
            if (roleName == RandomRoleName)
            {
                return GetRandomEnabledRoleId(random);
            }

            return GetRoleId(roleName);
        }

        public int ResolveEnabledRoleIdOrRandom(int roleId, System.Random random)
        {
            if (IsRoleEnabled(roleId))
            {
                return roleId;
            }

            return GetRandomEnabledRoleId(random);
        }

        private REPOPopupPage EnsureConfigPage()
        {
            if (configPage != null)
            {
                return configPage;
            }

            configPage = MenuAPI.CreateREPOPopupPage("NexusRoles", 0, true, true, 1.5f);

            configPage.AddElementToScrollView(delegate (Transform scrollView)
            {
                if (roleSlider == null)
                {
                    roleSlider = MenuAPI.CreateREPOSlider(NexusLocalization.Text("Role", "Роль", "Роль"), NexusLocalization.Text("Choose your role", "Выберите роль", "Оберіть роль"), (Action<string>)SetSelectedRole, scrollView, GetAvailableRoleChoices(), GetSelectableRoleName(SavedRole.Value), new Vector2(0f, 0f), "", "", (BarBehavior)0);
                }

                return ((REPOElement)roleSlider).rectTransform;
            }, 0f, 0f);

            configPage.AddElementToScrollView(delegate (Transform scrollView)
            {
                if (afterDeathRoleToggle == null)
                {
                    afterDeathRoleToggle = MenuAPI.CreateREPOToggle(NexusLocalization.Text("Change role after revive", "Менять роль после воскрешения", "Змінювати роль після воскресіння"), (Action<bool>)SetReassignRoleAfterRevive, scrollView, new Vector2(0f, 0f), NexusLocalization.Text("ON", "ДА", "ТАК"), NexusLocalization.Text("OFF", "НЕТ", "НІ"), AssignRoleAfterRevive.Value);
                }

                return ((REPOElement)afterDeathRoleToggle).rectTransform;
            }, 0f, 0f);

            configPage.AddElementToScrollView(delegate (Transform scrollView)
            {
                if (mageTopMana == null)
                {
                    mageTopMana = MenuAPI.CreateREPOToggle(NexusLocalization.Text("Mage mana position", "Положение маны Мага", "Позиція мани Мага"), (Action<bool>)SetMageManaTop, scrollView, new Vector2(0f, 0f), NexusLocalization.Text("TOP", "СВЕРХУ", "ЗВЕРХУ"), NexusLocalization.Text("LEFT", "СЛЕВА", "ЗЛІВА"), MageTopManaConf.Value);
                }

                return ((REPOElement)mageTopMana).rectTransform;
            }, 0f, 0f);

            configPage.AddElement((BuilderDelegate)delegate (Transform parent)
            {
                if (buttonClose == null)
                {
                    buttonClose = MenuAPI.CreateREPOButton(NexusLocalization.Text("Back", "Назад", "Назад"), (Action)delegate
                    {
                        configPage.ClosePage(false);
                    }, parent, Vector2.zero);
                }
            });

            return configPage;
        }

        private void SetSelectedRole(string roleName)
        {
            string normalizedRole = NormalizeRoleName(roleName);
            SavedRole.Value = normalizedRole;
            configFile.Save();
            RefreshRoleSlider();
        }

        private void SetReassignRoleAfterRevive(bool enabled)
        {
            AssignRoleAfterRevive.Value = enabled;
            configFile.Save();
        }

        private void SetMageManaTop(bool enabled)
        {
            MageTopManaConf.Value = enabled;
            configFile.Save();

            if (guiManager.isMage)
            {
                updateManaDisplay?.Invoke();
            }
        }

        private void RefreshRoleSlider()
        {
            if (roleSlider == null)
            {
                return;
            }

            string[] availableChoices = GetAvailableRoleChoices();
            string selectedRole = GetSelectableRoleName(SavedRole.Value);
            int selectedIndex = Array.IndexOf(availableChoices, selectedRole);
            if (selectedIndex < 0)
            {
                selectedIndex = 0;
            }

            roleSlider.stringOptions = availableChoices;
            roleSlider.SetValue(selectedIndex, invokeCallback: false);
        }

        private void ValidateSavedRole()
        {
            string selectedRole = SavedRole.Value;
            string normalizedRole = NormalizeRoleName(selectedRole);
            if (!string.Equals(selectedRole, normalizedRole, StringComparison.Ordinal))
            {
                NuageRolesPlugin.Logger.LogWarning("Saved NuageRoles role '" + selectedRole + "' is invalid. Falling back to " + normalizedRole + ".");
                SavedRole.Value = normalizedRole;
                configFile.Save();
            }
        }

        private string[] GetAvailableRoleChoices()
        {
            List<string> roleChoices = new List<string> { RandomRoleName };
            for (int roleId = 1; roleId < AllRoleChoices.Length; roleId++)
            {
                if (IsRoleEnabled(roleId))
                {
                    roleChoices.Add(AllRoleChoices[roleId]);
                }
            }

            return roleChoices.ToArray();
        }

        private string GetSelectableRoleName(string? roleName)
        {
            string normalizedRole = NormalizeRoleName(roleName);
            if (normalizedRole == RandomRoleName || IsRoleEnabled(normalizedRole))
            {
                return normalizedRole;
            }

            return RandomRoleName;
        }

        private string NormalizeRoleName(string? roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                return DefaultRoleName;
            }

            string legacyName = roleName.Trim();
            if (NexusLocalization.TryGetRoleId(legacyName, out int roleId))
            {
                return NexusLocalization.RoleName(roleId);
            }

            return DefaultRoleName;
        }

        private int GetRandomEnabledRoleId(System.Random random)
        {
            List<int> enabledRoleIds = GetEnabledRoleIds();
            if (enabledRoleIds.Count == 0)
            {
                return -1;
            }

            return enabledRoleIds[random.Next(enabledRoleIds.Count)];
        }

        private List<int> GetEnabledRoleIds()
        {
            List<int> enabledRoleIds = new List<int>();
            for (int roleId = 1; roleId < AllRoleChoices.Length; roleId++)
            {
                if (IsRoleEnabled(roleId))
                {
                    enabledRoleIds.Add(roleId);
                }
            }

            return enabledRoleIds;
        }

        private int GetRoleId(string roleName)
        {
            for (int i = 0; i < AllRoleChoices.Length; i++)
            {
                if (AllRoleChoices[i] == roleName)
                {
                    return i;
                }
            }

            return -1;
        }

        private bool IsRoleEnabled(string roleName)
        {
            return IsRoleEnabled(GetRoleId(roleName));
        }

        private bool IsRoleEnabled(int roleId)
        {
            switch (roleId)
            {
                case 1:
                    return EnableRunner.Value;
                case 2:
                    return EnableTank.Value;
                case 3:
                    return EnableGambler.Value;
                case 4:
                    return EnableStrongman.Value;
                case 5:
                    return EnableRL.Value;
                case 6:
                    return EnableAthletic.Value;
                case 7:
                    return EnableMage.Value;
                case 8:
                    return EnableReaper.Value;
                case 9:
                    return EnableScout.Value;
                case 10:
                    return EnableRegular.Value;
                case 11:
                    return EnableEngineer.Value;
                case 12:
                    return EnableAcrobat.Value;
                case 13:
                    return EnableSavior.Value;
                default:
                    return false;
            }
        }
    }
}
