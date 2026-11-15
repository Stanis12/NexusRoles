using System.Collections;
using System.IO;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using NexusRoles;
using Photon.Pun;
using UnityEngine;

namespace NexusRoles.Roles.Patches
{
    public class guiManager : MonoBehaviour
    {
        private bool lastManaPosition = true;

        public static bool showGUI = true;

        public static bool isMage = false;

        private static bool showSpells = false;

        private bool isDead = false;

        public string text = string.Empty;

        public Color color = Color.white;

        public string descText = string.Empty;

        public Color descColor = Color.white;

        private Texture2D? _backgroundTexture;

        public static Texture2D? manaTexture;

        public static Font? customFont;

        private static AssetBundle? fontBundle;

        private static AssetBundle? assetBundle;

        private static bool missingAssetBundleLogged;

        public static int aviableMana = 0;

        public static int manaRegenRate = 0;

        public static int manaTicks = 0;

        public bool speedActive;

        private float crouchSpeedBefore;

        private float moveSpeedBefore;

        private float sprintSpeedBefore;

        private float speedRemainingSeconds;

        private bool isOvercharged = false;

        private float overchargeRemainingSeconds;

        private bool jumpActive = false;

        private float jumpOriginal;

        private float jumpRemainingSeconds;

        private int manaUsageTicker = 0;

        private bool showManaUsage = false;

        private string manaUsageText = "Used x (- y Mana)";

        private bool fontLoaded = false;

        private int passiveTankRegenTicker = 0;

        public static int GetMageMaxMana()
        {
            return Mathf.Clamp(NexusRolesPlugin.Settings.MageMaxMana.Value, 3, 12);
        }

        private static int GetMageHealAmount()
        {
            return Mathf.Max(1, NexusRolesPlugin.Settings.MageHealAmount.Value);
        }

        private static float GetMageSpeedMultiplier()
        {
            return Mathf.Max(0.1f, NexusRolesPlugin.Settings.MageSpeedMultiplier.Value);
        }

        private static float GetMageJumpForceBonus()
        {
            return Mathf.Max(0f, NexusRolesPlugin.Settings.MageJumpForceBonus.Value);
        }

        private static float GetMageOverchargeMultiplier()
        {
            return Mathf.Max(1f, NexusRolesPlugin.Settings.MageOverchargeMultiplier.Value);
        }

        private static float GetMageSpeedBoostDurationSeconds()
        {
            return Mathf.Max(1f, NexusRolesPlugin.Settings.MageSpeedBoostDurationSeconds.Value);
        }

        private static float GetMageJumpBoostDurationSeconds()
        {
            return Mathf.Max(1f, NexusRolesPlugin.Settings.MageJumpBoostDurationSeconds.Value);
        }

        private static float GetMageOverchargeDurationSeconds()
        {
            return Mathf.Max(1f, NexusRolesPlugin.Settings.MageOverchargeDurationSeconds.Value);
        }

        private static int SecondsToFixedTicks(float seconds)
        {
            return Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(0.1f, seconds) / Time.fixedDeltaTime));
        }

        public static AssetBundle? LoadAssetBundle(string name)
        {
            if ((Object)(object)assetBundle! != null)
            {
                return assetBundle;
            }

            string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            string path = Path.Combine(directory, name);
            string fileName = Path.GetFileName(name);
            foreach (AssetBundle loadedBundle in AssetBundle.GetAllLoadedAssetBundles())
            {
                if ((Object)(object)loadedBundle == null)
                {
                    continue;
                }

                if (string.Equals(loadedBundle.name, name, System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(loadedBundle.name, fileName, System.StringComparison.OrdinalIgnoreCase) ||
                    loadedBundle.Contains("emptyBottle.png") ||
                    loadedBundle.Contains("filledBottle.png") ||
                    loadedBundle.Contains("font.ttf"))
                {
                    assetBundle = loadedBundle;
                    return assetBundle;
                }
            }

            if (!File.Exists(path))
            {
                if (!missingAssetBundleLogged)
                {
                    NexusRolesPlugin.Logger.LogError($"Missing asset bundle: expected '{path}'. Put {NexusRolesPlugin.AssetBundleName} next to NexusRoles.Plugin.dll.");
                    missingAssetBundleLogged = true;
                }
                return null;
            }

            assetBundle = AssetBundle.LoadFromFile(path);
            if ((Object)(object)assetBundle == null && !missingAssetBundleLogged)
            {
                NexusRolesPlugin.Logger.LogError($"Failed to load asset bundle '{path}'.");
                missingAssetBundleLogged = true;
            }

            return assetBundle;
        }

        public static Texture2D? LoadBottleTextureFromFile()
        {
            AssetBundle? val = LoadAssetBundle(NexusRolesPlugin.AssetBundleName);
            if ((Object)(object)val! == null)
            {
                return null;
            }
            Texture2D? result = val!.LoadAsset<Texture2D>("emptyBottle.png");
            if ((Object)(object)result! == null)
            {
                NexusRolesPlugin.Logger.LogError($"Missing texture asset 'emptyBottle.png' in {NexusRolesPlugin.AssetBundleName}.");
            }
            return result;
        }

        public static Texture2D? LoadFullBottleTextureFromFile()
        {
            AssetBundle? val = LoadAssetBundle(NexusRolesPlugin.AssetBundleName);
            if ((Object)(object)val! == null)
            {
                return null;
            }
            Texture2D? result = val!.LoadAsset<Texture2D>("filledBottle.png");
            if ((Object)(object)result! == null)
            {
                NexusRolesPlugin.Logger.LogError($"Missing texture asset 'filledBottle.png' in {NexusRolesPlugin.AssetBundleName}.");
            }
            return result;
        }

        public static Texture2D? LoadTextureByName(string bundleName, string assetName)
        {
            AssetBundle? val = LoadAssetBundle(bundleName);
            if ((Object)(object)val! == null)
            {
                return null;
            }
            Texture2D? result = val!.LoadAsset<Texture2D>(assetName);
            if ((Object)(object)result! == null)
            {
                NexusRolesPlugin.Logger.LogError($"Missing texture asset '{assetName}' in {bundleName}.");
            }
            return result;
        }

        public static void loadFont()
        {
            if ((Object)(object)fontBundle! == null)
            {
                fontBundle = LoadAssetBundle(NexusRolesPlugin.AssetBundleName);
                if ((Object)(object)fontBundle! == null)
                {
                    return;
                }
                customFont = fontBundle!.LoadAsset<Font>("font.ttf");
                if ((Object)(object)customFont! == null)
                {
                    NexusRolesPlugin.Logger.LogError($"Missing font asset 'font.ttf' in {NexusRolesPlugin.AssetBundleName}.");
                }
            }
            if ((Object)(object)fontBundle! == null)
            {
                NexusRolesPlugin.Logger.LogError((object)"Missing Assets: Put the assets file next to the dll in your plugins folder. If this does not fix it contact us on our discord (see thunderstore description or README)");
            }
        }

        private void Start()
        {
            _backgroundTexture = LoadBottleTextureFromFile();
            if ((Object)(object)_backgroundTexture! != null)
            {
                _backgroundTexture.Apply();
            }
            manaTexture = LoadFullBottleTextureFromFile();
            if ((Object)(object)manaTexture! != null)
            {
                manaTexture.Apply();
            }
        }

        private void OnGUI()
        {
            float num = Screen.width / 2 - 100;
            float num2 = Screen.height / 2 - 60;
            if (!fontLoaded)
            {
                loadFont();
                fontLoaded = true;
            }
            if (SemiFunc.RunIsLevel() && !SemiFunc.RunIsShop() && showGUI)
            {
                GUIStyle val = new GUIStyle();
                val.fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.07f), 24, 60);
                val.fontStyle = (FontStyle)1;
                val.richText = true;
                val.wordWrap = true;
                val.normal.textColor = color;
                val.alignment = (TextAnchor)4;
                val.font = customFont;
                GUIStyle val2 = new GUIStyle(val);
                val2.normal.textColor = Color.black;
                val2.alignment = (TextAnchor)4;
                val2.font = customFont;
                float num3 = 2f;
                float titleWidth = Mathf.Clamp(Screen.width * 0.8f, 280f, 1000f);
                float titleHeight = val.CalcHeight(new GUIContent(this.text), titleWidth);
                Rect titleRect = new Rect((Screen.width - titleWidth) * 0.5f, num2, titleWidth, titleHeight);
                GUI.Label(new Rect(titleRect.x - num3, titleRect.y, titleRect.width, titleRect.height), this.text, val2);
                GUI.Label(new Rect(titleRect.x + num3, titleRect.y, titleRect.width, titleRect.height), this.text, val2);
                GUI.Label(new Rect(titleRect.x, titleRect.y - num3, titleRect.width, titleRect.height), this.text, val2);
                GUI.Label(new Rect(titleRect.x, titleRect.y + num3, titleRect.width, titleRect.height), this.text, val2);
                GUI.Label(titleRect, this.text, val);

                float num4 = Screen.width / 2 - 100;
                float num5 = Screen.height / 2 + 40;
                GUIStyle val3 = new GUIStyle();
                val3.fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.035f), 16, 34);
                val3.fontStyle = (FontStyle)1;
                val3.richText = true;
                val3.wordWrap = true;
                val3.normal.textColor = color;
                val3.alignment = (TextAnchor)4;
                val3.font = customFont;
                GUIStyle val4 = new GUIStyle(val3);
                val4.normal.textColor = Color.black;
                val4.alignment = (TextAnchor)4;
                val4.font = customFont;
                float num6 = 2f;
                float descWidth = Mathf.Clamp(Screen.width * 0.75f, 280f, 900f);
                float descHeight = val3.CalcHeight(new GUIContent(descText), descWidth);
                Rect descRect = new Rect((Screen.width - descWidth) * 0.5f, num5, descWidth, descHeight);
                GUI.Label(new Rect(descRect.x - num6, descRect.y, descRect.width, descRect.height), descText, val4);
                GUI.Label(new Rect(descRect.x + num6, descRect.y, descRect.width, descRect.height), descText, val4);
                GUI.Label(new Rect(descRect.x, descRect.y - num6, descRect.width, descRect.height), descText, val4);
                GUI.Label(new Rect(descRect.x, descRect.y + num6, descRect.width, descRect.height), descText, val4);
                GUI.Label(descRect, descText, val3);
            }
            PlayerHealth? localPlayerHealth = GetLocalPlayerHealth();
            if (!isMage || !SemiFunc.RunIsLevel() || SemiFunc.RunIsShop() || IsUnityNull(localPlayerHealth) || localPlayerHealth!.health <= 0)
            {
                return;
            }
            GUIStyle val5 = new GUIStyle();
            val5.alignment = (TextAnchor)1;
            val5.normal.background = manaTexture;
            val5.font = customFont;
            GUIStyle val6 = new GUIStyle();
            val6.alignment = (TextAnchor)1;
            val6.normal.background = _backgroundTexture;
            val6.font = customFont;
            if (showManaUsage && SemiFunc.RunIsLevel() && !SemiFunc.RunIsShop() && isMage)
            {
                int num7 = Screen.width / 2 - 100;
                int num8 = Screen.height - 125;
                GUIStyle val7 = new GUIStyle();
                val7.fontSize = 20;
                val7.fontStyle = (FontStyle)1;
                val7.richText = true;
                val7.normal.textColor = new Color(0f, 0.384f, 1f);
                val7.alignment = (TextAnchor)4;
                val7.font = customFont;
                GUIStyle val8 = new GUIStyle(val7);
                val8.normal.textColor = Color.black;
                val8.alignment = (TextAnchor)4;
                val8.font = customFont;
                float num9 = 1.5f;
                GUI.Label(new Rect((float)num7 - num9, (float)num8, 200f, 50f), manaUsageText, val8);
                GUI.Label(new Rect((float)num7 + num9, (float)num8, 200f, 50f), manaUsageText, val8);
                GUI.Label(new Rect((float)num7, (float)num8 - num9, 200f, 50f), manaUsageText, val8);
                GUI.Label(new Rect((float)num7, (float)num8 + num9, 200f, 50f), manaUsageText, val8);
                GUI.Label(new Rect((float)num7 - num9, (float)num8 - num9, 200f, 50f), manaUsageText, val8);
                GUI.Label(new Rect((float)num7 - num9, (float)num8 + num9, 200f, 50f), manaUsageText, val8);
                GUI.Label(new Rect((float)num7 + num9, (float)num8 - num9, 200f, 50f), manaUsageText, val8);
                GUI.Label(new Rect((float)num7 + num9, (float)num8 + num9, 200f, 50f), manaUsageText, val8);
                GUI.Label(new Rect((float)num7, (float)num8, 200f, 50f), manaUsageText, val7);
            }
            if (SemiFunc.RunIsLevel() && !SemiFunc.RunIsShop() && isMage && NexusRolesPlugin.Settings.MageTopManaTop)
            {
                if ((Object)(object)ManaHelper.val! == null)
                {
                    ManaHelper.val = GameObject.Find("Energy");
                }
                int maxMana = GetMageMaxMana();
                float boxSize = 50f;
                float startX = Screen.width * 0.5f - maxMana * boxSize * 0.5f;
                for (int i = 0; i < maxMana; i++)
                {
                    Rect manaRect = new Rect(startX + i * boxSize, 50f, boxSize, boxSize);
                    GUI.Box(manaRect, GUIContent.none, val6);
                    if (aviableMana >= i + 1)
                    {
                        GUI.Box(manaRect, GUIContent.none, val5);
                    }
                }
            }
            if (showSpells && SemiFunc.RunIsLevel() && !SemiFunc.RunIsShop())
            {
                // tighter body (single newlines, no extra blank lines)
                string headerText = NexusLocalization.Text("SPELLS", "ЗАКЛИНАНИЯ", "ЗАКЛЯТТЯ");
                string bodyText =
                    "[" + NexusRolesPlugin.Settings.HealKey.Value.ToString() + "] " + NexusLocalization.Text("Restore ", "Восстановить ", "Відновити ") + GetMageHealAmount() + NexusLocalization.Text(" health (1 Mana)\n", " ед. здоровья (1 мана)\n", " од. здоров’я (1 мана)\n")
                    + "[" + NexusRolesPlugin.Settings.StaminaKey.Value.ToString() + "] " + NexusLocalization.Text("Fully restore stamina (3 Mana)\n", "Полностью восстановить выносливость (3 маны)\n", "Повністю відновити витривалість (3 мани)\n")
                    + "[" + NexusRolesPlugin.Settings.SpeedKey.Value.ToString() + "] " + NexusLocalization.Text("Speed boost for ", "Ускорение на ", "Прискорення на ") + GetMageSpeedBoostDurationSeconds().ToString("0.#") + NexusLocalization.Text("s (2 Mana)\n", " с (2 маны)\n", " с (2 мани)\n")
                    + "[" + NexusRolesPlugin.Settings.OverchargeKey.Value.ToString() + "] " + NexusLocalization.Text("Boost spells by x", "Усилить заклинания в ", "Посилити закляття у ") + GetMageOverchargeMultiplier().ToString("0.#") + NexusLocalization.Text(" for ", " раза на ", " раза на ") + GetMageOverchargeDurationSeconds().ToString("0.#") + NexusLocalization.Text("s (3 Mana)\n", " с (3 маны)\n", " с (3 мани)\n")
                    + "[" + NexusRolesPlugin.Settings.JumpKey.Value.ToString() + "] " + NexusLocalization.Text("Jump higher for ", "Прыгать выше в течение ", "Стрибати вище протягом ") + GetMageJumpBoostDurationSeconds().ToString("0.#") + NexusLocalization.Text("s (2 Mana)", " с (2 маны)", " с (2 мани)");

                // center anchors
                float centerX = Screen.width * 0.5f;
                float centerY = Screen.height * 0.5f;

                // width available to wrap the body
                float areaWidth = Mathf.Clamp(Screen.width * 0.6f, 260f, 1200f);

                // base styles
                GUIStyle headerStyle = new GUIStyle();
                headerStyle.richText = true;
                headerStyle.wordWrap = true;
                headerStyle.alignment = TextAnchor.MiddleCenter;
                headerStyle.font = customFont;
                headerStyle.fontStyle = FontStyle.Bold;

                GUIStyle bodyStyle = new GUIStyle(headerStyle); // share common settings
                bodyStyle.wordWrap = true;

                GUIStyle outlineHeader = new GUIStyle(headerStyle);
                outlineHeader.normal.textColor = Color.black;
                GUIStyle outlineBody = new GUIStyle(bodyStyle);
                outlineBody.normal.textColor = Color.black;

                // helper to find font size that keeps rendered height below a limit
                int GetFittingFontSize(string s, GUIStyle style, float width, int maxSize, int minSize, float maxAllowedHeight)
                {
                    for (int size = maxSize; size >= minSize; size--)
                    {
                        style.fontSize = size;
                        float needed = style.CalcHeight(new GUIContent(s), width);
                        if (needed <= maxAllowedHeight)
                            return size;
                    }
                    return minSize;
                }

                // dynamic max font based on screen size (keeps scaling on very large screens)
                int maxHeader = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.09f), 24, 120); // ~9% of height
                int maxBody = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.045f), 14, 72);  // ~4.5% of height
                int minHeader = 16;
                int minBody = 12;

                // limit: don't allow a single label to be taller than screen minus margin
                float maxAllowedLabelHeight = Screen.height - 60f;

                // find header size then body size. We allow header to be big; body must fit in remaining space
                // First pick header size that fits (but header can be up to maxHeader)
                int headerSize = GetFittingFontSize(headerText, headerStyle, areaWidth, maxHeader, minHeader, maxAllowedLabelHeight);
                headerSize = Mathf.Max(minHeader, headerSize + 4); // keep your +4 bump
                headerStyle.fontSize = headerSize;
                outlineHeader.fontSize = headerSize;

                // compute header rendered height
                float headerHeight = headerStyle.CalcHeight(new GUIContent(headerText), areaWidth);

                // Remaining vertical space for body (allow some margin)
                float remainingForBody = Mathf.Max(60f, Screen.height - headerHeight - 80f);

                // pick body size to fit inside remaining space
                int bodySize = GetFittingFontSize(bodyText, bodyStyle, areaWidth, maxBody, minBody, remainingForBody);
                bodySize = Mathf.Max(minBody, bodySize + 4); // same +4 bump
                bodyStyle.fontSize = bodySize;
                outlineBody.fontSize = bodySize;

                // compute body rendered height
                float bodyHeight = bodyStyle.CalcHeight(new GUIContent(bodyText), areaWidth);

                // small gap between header and body. keep it proportional and small so spacing isn't huge
                float gap = Mathf.Clamp(Mathf.RoundToInt(headerSize * 0.25f), 4, 14);

                // total block height and rect centered on screen
                float totalHeight = headerHeight + gap + bodyHeight;
                Rect blockRect = new Rect(centerX - areaWidth * 0.5f, centerY - totalHeight * 0.5f, areaWidth, totalHeight);

                // positions for header and body inside the block
                Rect headerRect = new Rect(blockRect.x, blockRect.y, blockRect.width, headerHeight);
                Rect bodyRect = new Rect(blockRect.x, blockRect.y + headerHeight + gap, blockRect.width, bodyHeight);

                // draw outline / shadow for header and body (offset in 8 directions)
                float offset = Mathf.Clamp(Screen.height * 0.0025f, 1f, 3f); // scale shadow a bit with resolution
                                                                             // header outline
                GUI.Label(new Rect(headerRect.x - offset, headerRect.y, headerRect.width, headerRect.height), headerText, outlineHeader);
                GUI.Label(new Rect(headerRect.x + offset, headerRect.y, headerRect.width, headerRect.height), headerText, outlineHeader);
                GUI.Label(new Rect(headerRect.x, headerRect.y - offset, headerRect.width, headerRect.height), headerText, outlineHeader);
                GUI.Label(new Rect(headerRect.x, headerRect.y + offset, headerRect.width, headerRect.height), headerText, outlineHeader);
                // body outline (do same offsets)
                GUI.Label(new Rect(bodyRect.x - offset, bodyRect.y, bodyRect.width, bodyRect.height), bodyText, outlineBody);
                GUI.Label(new Rect(bodyRect.x + offset, bodyRect.y, bodyRect.width, bodyRect.height), bodyText, outlineBody);
                GUI.Label(new Rect(bodyRect.x, bodyRect.y - offset, bodyRect.width, bodyRect.height), bodyText, outlineBody);
                GUI.Label(new Rect(bodyRect.x, bodyRect.y + offset, bodyRect.width, bodyRect.height), bodyText, outlineBody);

                // main colored labels
                GUIStyle headerColor = new GUIStyle(headerStyle);
                headerColor.normal.textColor = new Color(0f, 0.384f, 1f);
                GUIStyle bodyColor = new GUIStyle(bodyStyle);
                bodyColor.normal.textColor = new Color(0f, 0.384f, 1f);

                GUI.Label(headerRect, headerText, headerColor);
                GUI.Label(bodyRect, bodyText, bodyColor);
            }
        }

        public void InitializeMageDisplay()
        {
            if (!isMage) return;

            // Clean up any existing UI
            if (ManaHelper.val2 != null)
            {
                GameObject.Destroy(ManaHelper.val2);
                ManaHelper.val2 = null;
                ManaHelper.manaUI = null;
            }

            // Reset last position to force update
            lastManaPosition = !NexusRolesPlugin.Settings.MageTopManaTop;

            // Update display based on config
            UpdateManaDisplay();

            // Set initial mana value
            if (!NexusRolesPlugin.Settings.MageTopManaTop && ManaHelper.manaUI != null)
            {
                ManaHelper.manaUI.SetMana(aviableMana, GetMageMaxMana());
            }
        }

        public static void ResetManaUI()
        {
            if (ManaHelper.val2 != null)
            {
                Object.Destroy(ManaHelper.val2);
                ManaHelper.val2 = null;
            }
            ManaHelper.manaUI = null;
        }

        public void UpdateManaDisplay()
        {
            if (!isMage) return;

            // Ensure UI exists
            if (ManaHelper.val2 == null)
            {
                ManaHelper.CreateUI();
            }

            // Show/hide based on config
            if (NexusRolesPlugin.Settings.MageTopManaTop)
            {
                // TOP display: Hide custom UI (OnGUI will draw)
                ManaHelper.ShowUI(false);
            }
            else
            {
                // LEFT display: Show custom UI
                ManaHelper.ShowUI(true);
                ManaHelper.UpdateManaValue();
            }
        }

        private void updateMana()
        {
            if (!isMage) return;

            // Update mana value in the appropriate display
            if (NexusRolesPlugin.Settings.MageTopManaTop)
            {
                // TOP display updates automatically in OnGUI
            }
            else
            {
                // LEFT display: Update the custom UI
                ManaHelper.UpdateManaValue();
            }
        }

        private float CurrentOverchargeMultiplier()
        {
            return isOvercharged ? GetMageOverchargeMultiplier() : 1f;
        }

        private static bool IsUnityNull(Object? value)
        {
            return value == null;
        }

        private static PlayerHealth? GetLocalPlayerHealth()
        {
            PlayerAvatar? localAvatar = PlayerAvatar.instance;
            if (IsUnityNull(localAvatar))
            {
                return null;
            }

            return localAvatar!.playerHealth;
        }

        private PlayerAvatar? GetGrabbedMageHealTarget()
        {
            PlayerAvatar? localAvatar = PlayerAvatar.instance;
            if (IsUnityNull(localAvatar))
            {
                return null;
            }

            PhysGrabber? grabber = localAvatar?.physGrabber;
            if (IsUnityNull(grabber) || !grabber!.grabbed)
            {
                return null;
            }

            StaticGrabObject? grabbedStaticGrabObject = grabber.grabbedStaticGrabObject;
            if (IsUnityNull(grabbedStaticGrabObject))
            {
                return null;
            }

            foreach (PlayerAvatar avatar in SemiFunc.PlayerGetAll())
            {
                if (IsUnityNull(avatar) || avatar == localAvatar)
                {
                    continue;
                }

                PlayerHealthGrab? healthGrab = avatar.healthGrab;
                if (IsUnityNull(healthGrab))
                {
                    continue;
                }

                StaticGrabObject? healthGrabObject = healthGrab!.staticGrabObject;
                if (IsUnityNull(healthGrabObject))
                {
                    continue;
                }

                if (healthGrabObject == grabbedStaticGrabObject)
                {
                    return avatar;
                }
            }

            return null;
        }

        private bool CanReceiveMageHeal(PlayerHealth? playerHealth)
        {
            return !IsUnityNull(playerHealth) &&
                playerHealth!.health > 0 &&
                playerHealth.health < playerHealth.maxHealth;
        }

        private bool TryCastMageHeal(PlayerHealth? playerHealth, bool healingAlly)
        {
            if (aviableMana < 1 || !CanReceiveMageHeal(playerHealth))
            {
                return false;
            }

            int missingHealth = Mathf.Max(0, playerHealth!.maxHealth - playerHealth.health);
            int healAmount = Mathf.Min(missingHealth, Mathf.CeilToInt(GetMageHealAmount() * CurrentOverchargeMultiplier()));
            if (healAmount <= 0)
            {
                return false;
            }

            if (healingAlly)
            {
                playerHealth.HealOther(healAmount, true);
                PlayerAvatar? localAvatar = PlayerAvatar.instance;
                if (!IsUnityNull(localAvatar))
                {
                    localAvatar!.HealedOther();
                }
                SpendMageMana(1, NexusLocalization.Text("Healed ally (-1 Mana)", "Союзник исцелён (-1 мана)", "Союзника зцілено (-1 мана)"));
                return true;
            }

            playerHealth.Heal(healAmount, true);
            SpendMageMana(1, NexusLocalization.Text("Used Heal (-1 Mana)", "Использовано исцеление (-1 мана)", "Використано зцілення (-1 мана)"));
            return true;
        }

        private void SpendMageMana(int amount, string usageText)
        {
            aviableMana = Mathf.Max(0, aviableMana - amount);
            showManaUsage = true;
            manaUsageTicker = 0;
            manaUsageText = usageText;
            UpdateManaDisplay();
        }

        private void RestoreMageSpeedBoost()
        {
            if (!speedActive)
            {
                return;
            }

            if (PlayerController.instance != null)
            {
                PlayerController.instance.CrouchSpeed = crouchSpeedBefore;
                PlayerController.instance.MoveSpeed = moveSpeedBefore;
                PlayerController.instance.SprintSpeed = sprintSpeedBefore;
            }

            speedActive = false;
            speedRemainingSeconds = 0f;
        }

        private void RestoreMageJumpBoost()
        {
            if (!jumpActive)
            {
                return;
            }

            if (PlayerController.instance != null)
            {
                PlayerController.instance.JumpForce = jumpOriginal;
            }

            jumpActive = false;
            jumpRemainingSeconds = 0f;
        }

        public void ClearMageTemporaryEffects()
        {
            RestoreMageSpeedBoost();
            RestoreMageJumpBoost();
            isOvercharged = false;
            overchargeRemainingSeconds = 0f;
            showSpells = false;
        }

        private void Update()
        {
            PlayerHealth? localPlayerHealth = GetLocalPlayerHealth();
            int localHealth = IsUnityNull(localPlayerHealth) ? 0 : localPlayerHealth!.health;

            // Check if mana position config has changed and update display
            if (isMage && lastManaPosition != NexusRolesPlugin.Settings.MageTopManaTop)
            {
                UpdateManaDisplay();
                lastManaPosition = NexusRolesPlugin.Settings.MageTopManaTop;
            }
            if (SemiFunc.RunIsLevel() && localHealth <= 0)
            {
                isDead = true;
            }
            if (!SemiFunc.RunIsLevel())
            {
                isMage = false;
                if (Sender.manager != null && (Object)(object)PlayerAvatar.instance != null)
                {
                    Sender.manager.setReaperStatus(PlayerController.instance.playerSteamID, isReaper: false);
                }
                ClassManager.isTank = false;
                isDead = false;
            }
            if (!NexusRolesPlugin.Settings.AfterDeathNewRole)
            {
                isDead = false;
            }
            if (isDead && localHealth > 0 && NexusRolesPlugin.Settings.AfterDeathNewRole)
            {
                if (Sender.manager != null)
                {
                    Sender.manager.assignRoleFromConfig(PlayerController.instance);
                    NexusRolesPlugin.Logger.LogInfo((object)"Assigning new role on respawn!");
                }
                isDead = false;
            }
            if (Input.GetKeyDown(NexusRolesPlugin.Settings.ToggleKey.Value) && !ChatManager.instance.chatActive)
            {
                showGUI = !showGUI;
            }
            if (localHealth > 0 && isMage && !ChatManager.instance.chatActive)
            {
                if (Input.GetKeyDown(NexusRolesPlugin.Settings.ShowSpellsKey.Value))
                {
                    showSpells = !showSpells;
                }
                if (Input.GetKeyDown(NexusRolesPlugin.Settings.HealKey.Value))
                {
                    PlayerAvatar? healTarget = GetGrabbedMageHealTarget();
                    if (IsUnityNull(healTarget) || !TryCastMageHeal(healTarget!.playerHealth, healingAlly: true))
                    {
                        TryCastMageHeal(localPlayerHealth, healingAlly: false);
                    }
                }
                if (Input.GetKeyDown(NexusRolesPlugin.Settings.SpeedKey.Value) && !speedActive)
                {
                    crouchSpeedBefore = PlayerController.instance.CrouchSpeed;
                    moveSpeedBefore = PlayerController.instance.MoveSpeed;
                    sprintSpeedBefore = PlayerController.instance.SprintSpeed;
                    if (aviableMana >= 2)
                    {
                        speedActive = true;
                        float speedMultiplier = GetMageSpeedMultiplier();
                        PlayerController.instance.CrouchSpeed = crouchSpeedBefore * speedMultiplier;
                        PlayerController.instance.MoveSpeed = moveSpeedBefore * speedMultiplier;
                        PlayerController.instance.SprintSpeed = sprintSpeedBefore * speedMultiplier;
                        speedRemainingSeconds = GetMageSpeedBoostDurationSeconds() * CurrentOverchargeMultiplier();
                        SpendMageMana(2, NexusLocalization.Text("Used Speed (-2 Mana)", "Использовано ускорение (-2 маны)", "Використано прискорення (-2 мани)"));
                    }
                }
                if (Input.GetKeyDown(NexusRolesPlugin.Settings.OverchargeKey.Value) && !isOvercharged && aviableMana >= 3)
                {
                    isOvercharged = true;
                    overchargeRemainingSeconds = GetMageOverchargeDurationSeconds();
                    SpendMageMana(3, NexusLocalization.Text("Used Overcharge (-3 Mana)", "Использована перезарядка (-3 маны)", "Використано надзаряд (-3 мани)"));
                }
                if (Input.GetKeyDown(NexusRolesPlugin.Settings.JumpKey.Value) && !jumpActive)
                {
                    jumpOriginal = PlayerController.instance.JumpForce;
                    if (aviableMana >= 2)
                    {
                        jumpActive = true;
                        PlayerController.instance.JumpForce = jumpOriginal + GetMageJumpForceBonus();
                        jumpRemainingSeconds = GetMageJumpBoostDurationSeconds() * CurrentOverchargeMultiplier();
                        SpendMageMana(2, NexusLocalization.Text("Used Jump Boost (-2 Mana)", "Использовано усиление прыжка (-2 маны)", "Використано посилення стрибка (-2 мани)"));
                    }
                }
                if (Input.GetKeyDown(NexusRolesPlugin.Settings.StaminaKey.Value) && PlayerController.instance.EnergyCurrent < PlayerController.instance.EnergyStart && aviableMana >= 3)
                {
                    PlayerController.instance.EnergyCurrent = PlayerController.instance.EnergyStart;
                    SpendMageMana(3, NexusLocalization.Text("Used Stamina Refill (-3 Mana)", "Использовано восстановление выносливости (-3 маны)", "Використано відновлення витривалості (-3 мани)"));
                }
            }
            if (speedActive)
            {
                speedRemainingSeconds -= Time.deltaTime;
                if (speedRemainingSeconds <= 0f)
                {
                    RestoreMageSpeedBoost();
                }
            }
            if (jumpActive)
            {
                jumpRemainingSeconds -= Time.deltaTime;
                if (jumpRemainingSeconds <= 0f)
                {
                    RestoreMageJumpBoost();
                }
            }
            if (isOvercharged)
            {
                overchargeRemainingSeconds -= Time.deltaTime;
                if (overchargeRemainingSeconds <= 0f)
                {
                    isOvercharged = false;
                }
            }
            if (!isMage)
            {
                // Clean up UI when no longer a mage
                if (ManaHelper.val2 != null)
                {
                    GameObject.Destroy(ManaHelper.val2);
                    ManaHelper.val2 = null;
                    ManaHelper.manaUI = null;
                }
            }
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

        private void FixedUpdate()
        {
            int tankRegenIntervalTicks = SecondsToFixedTicks(NexusRolesPlugin.Settings.TankPassiveRegenIntervalSeconds.Value);
            if (passiveTankRegenTicker < tankRegenIntervalTicks && ClassManager.isTank && SemiFunc.RunIsLevel() && !SemiFunc.RunIsShop() && !isDead)
            {
                passiveTankRegenTicker++;
            }
            if (passiveTankRegenTicker >= tankRegenIntervalTicks && SemiFunc.RunIsLevel() && !SemiFunc.RunIsShop() && ClassManager.isTank)
            {
                passiveTankRegenTicker = 0;
                int num = (int)AccessTools.Field(typeof(PlayerHealth), "maxHealth").GetValue(PlayerAvatar.instance.playerHealth);
                int num2 = (int)AccessTools.Field(typeof(PlayerHealth), "health").GetValue(PlayerAvatar.instance.playerHealth);
                int regenAmount = Mathf.Max(0, NexusRolesPlugin.Settings.TankPassiveRegenAmount.Value);
                if (num2 < num && regenAmount > 0)
                {
                    setHealth(PlayerController.instance.playerSteamID, num, Mathf.Min(num, num2 + regenAmount));
                }
            }
            manaUsageTicker++;
            if (manaUsageTicker >= 420)
            {
                showManaUsage = false;
            }
            int maxMana = GetMageMaxMana();
            if (isMage && aviableMana > maxMana)
            {
                aviableMana = maxMana;
                UpdateManaDisplay();
            }
            if (aviableMana < maxMana)
            {
                manaTicks++;
            }
            if (manaTicks >= 1300 - manaRegenRate * 120 && isMage && aviableMana < maxMana)
            {
                aviableMana++;
                manaTicks = 0;
                UpdateManaDisplay();
            }
        }
    }
}
