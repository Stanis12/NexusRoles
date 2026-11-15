using HarmonyLib;
using NexusRoles.Roles;
using NexusRoles.Roles.Patches;
using UnityEngine;

namespace NexusRoles
{
    public class ScoutMarker : MonoBehaviour
    {
        private int cooldownTicker = 0;

        private int activeTicker = 0;

        private bool isActive = false;

        private bool onCooldown = true;

        public static int reductionUpgrades;

        private static int SecondsToFixedTicks(float seconds)
        {
            return Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(0f, seconds) / Time.fixedDeltaTime));
        }

        private static int GetActiveTicks()
        {
            return SecondsToFixedTicks(NexusRolesPlugin.Settings.ScoutAbilityDurationSeconds.Value);
        }

        private static int GetCooldownTicks()
        {
            int baseCooldownTicks = Mathf.Max(0, Mathf.RoundToInt(Mathf.Max(0f, NexusRolesPlugin.Settings.ScoutAbilityCooldownSeconds.Value) / Time.fixedDeltaTime));
            int upgradedCooldownTicks = baseCooldownTicks - reductionUpgrades * 250;
            if (reductionUpgrades > 5)
            {
                upgradedCooldownTicks = Mathf.Min(baseCooldownTicks, 750);
            }

            return Mathf.Max(0, upgradedCooldownTicks);
        }

        private void ResetState()
        {
            cooldownTicker = 0;
            activeTicker = 0;
            isActive = false;
            onCooldown = true;
        }

        private void OnGUI()
        {
            if (!SemiFunc.RunIsLevel() || SemiFunc.RunIsShop() || PlayerAvatar.instance.playerHealth.health <= 0)
            {
                return;
            }
            GUIStyle val = new GUIStyle(GUI.skin.label)
            {
                alignment = (TextAnchor)4,
                fontSize = 22
            };
            val.normal.textColor = new Color(0.902f, 0.733f, 0.11f);
            if ((Object)(object)guiManager.customFont! != null)
            {
                val.font = guiManager.customFont;
            }
            if (EnemyDirector.instance.enemiesSpawned != null && ClassManager.isScout && isActive)
            {
                GUIStyle val2 = new GUIStyle(GUI.skin.label)
                {
                    alignment = (TextAnchor)4,
                    fontSize = 22
                };
                val2.normal.textColor = Color.red;
                if ((Object)(object)guiManager.customFont! != null)
                {
                    val2.font = guiManager.customFont;
                }
                foreach (EnemyParent item in EnemyDirector.instance.enemiesSpawned)
                {
                    if ((Object)(object)item == null || !item.EnableObject.activeInHierarchy)
                    {
                        continue;
                    }
                    Enemy? enemy = AccessTools.Field(typeof(EnemyParent), "Enemy").GetValue(item) as Enemy;
                    Camera? mainCamera = Camera.main;
                    if ((Object)(object)enemy! != null && (Object)(object)mainCamera! != null)
                    {
                        Vector3 val4 = mainCamera!.WorldToViewportPoint(enemy!.CenterTransform.position);
                        if (!(val4.z < 0f))
                        {
                            string enemyName = item.enemyName;
                            string text = $"{enemyName} [{val4.z:F0}m]";
                            Vector2 val5 = val2.CalcSize(new GUIContent(text));
                            float num = val4.x * (float)Screen.width - val5.x / 2f;
                            float num2 = (float)Screen.height - val4.y * (float)Screen.height - val5.y / 2f;
                            GUI.Label(new Rect(num, num2, val5.x, val5.y), text, val2);
                        }
                    }
                }
                if (isActive && !onCooldown)
                {
                    int num3 = Screen.width / 2 - 100;
                    int num4 = Screen.height - 125;
                    string text2 = Mathf.CeilToInt(activeTicker * Time.fixedDeltaTime).ToString();
                    string text3 = NexusLocalization.Text("Ability ends in ", "Способность закончится через ", "Здібність завершиться за ") + text2 + NexusLocalization.Text("s", " с", " с");
                    GUI.Label(new Rect((float)num3, (float)num4, 200f, 50f), text3, val);
                }
            }
            if (!isActive && ClassManager.isScout && onCooldown)
            {
                int num5 = Screen.width / 2 - 100;
                int num6 = Screen.height - 125;
                string text4 = Mathf.CeilToInt(cooldownTicker * Time.fixedDeltaTime).ToString();
                val.normal.textColor = new Color(0.851f, 0.667f, 0.098f);
                string text5 = NexusLocalization.Text("Ability ready in ", "Способность будет готова через ", "Здібність буде готова за ") + text4 + NexusLocalization.Text("s", " с", " с");
                GUI.Label(new Rect((float)num5, (float)num6, 200f, 60f), text5, val);
            }
            else if (!isActive && ClassManager.isScout && cooldownTicker <= 0)
            {
                int num7 = Screen.width / 2 - 100;
                int num8 = Screen.height - 125;
                val.normal.textColor = Color.green;
                string text7 = NexusLocalization.Text("Ability ready!\nPress [", "Способность готова!\nНажми [", "Здібність готова!\nНатисни [") + NexusRolesPlugin.Settings.ScoutKey.Value + NexusLocalization.Text("] to activate", "], чтобы активировать", "], щоб активувати");
                GUI.Label(new Rect((float)num7, (float)num8, 200f, 60f), text7, val);
            }
        }

        private void FixedUpdate()
        {
            if (!ClassManager.isScout)
            {
                ResetState();
                return;
            }
            if (isActive)
            {
                activeTicker--;
            }
            if (isActive && activeTicker <= 0)
            {
                isActive = false;
                onCooldown = true;
                cooldownTicker = GetCooldownTicks();
            }
            if (cooldownTicker <= 0)
            {
                onCooldown = false;
            }
            if (cooldownTicker >= 0 && onCooldown)
            {
                cooldownTicker--;
            }
        }

        private void Update()
        {
            if (ClassManager.isScout && SemiFunc.RunIsLevel() && !SemiFunc.RunIsShop() && !ChatManager.instance.chatActive && !onCooldown && Input.GetKeyDown(NexusRolesPlugin.Settings.ScoutKey.Value))
            {
                activeTicker = GetActiveTicks();
                isActive = true;
            }
        }
    }
}
