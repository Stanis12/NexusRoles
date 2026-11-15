using NexusRoles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NexusRoles.Roles.Patches
{
    public class ManaUI : MonoBehaviour  // Don't inherit from SemiUI
    {
        public TextMeshProUGUI? textMana;
        public TextMeshProUGUI? textManaMax;
        public static ManaUI? instance;

        private RectTransform? rectTransform;
        private Image? manaImage;

        private void Start()
        {
            instance = this;
            rectTransform = GetComponent<RectTransform>();

            // Find UI elements
            textMana = GetComponentInChildren<TextMeshProUGUI>();

            var maxObj = transform.Find("EnergyMax");
            if (maxObj != null)
                textManaMax = maxObj.GetComponent<TextMeshProUGUI>();
            else
            {
                // Fallback search
                var texts = GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length > 1)
                    textManaMax = texts[1];
            }

            // Find mana image
            manaImage = GetComponentInChildren<Image>();

            // Set initial mana
            if (guiManager.isMage)
                SetMana(guiManager.aviableMana, guiManager.GetMageMaxMana());

            NexusRolesPlugin.Logger.LogInfo("ManaUI initialized successfully");
        }

        private void Update()
        {
            // Simple update - just update mana value if needed
            // No complex animation logic that can cause errors
            if (guiManager.isMage && textMana != null)
            {
                SetMana(guiManager.aviableMana, guiManager.GetMageMaxMana());
            }
        }

        public void SetMana(float mana, float maxMana)
        {
            if (textMana == null || textManaMax == null)
            {
                // Try to find components again
                textMana = GetComponentInChildren<TextMeshProUGUI>();
                var maxObj = transform.Find("EnergyMax");
                if (maxObj != null)
                    textManaMax = maxObj.GetComponent<TextMeshProUGUI>();
            }

            if (textMana != null)
                textMana.text = Mathf.Ceil(mana).ToString();

            if (textManaMax != null)
                textManaMax.text = $"<b>/</b>{Mathf.Ceil(maxMana)}";
        }
    }
}
