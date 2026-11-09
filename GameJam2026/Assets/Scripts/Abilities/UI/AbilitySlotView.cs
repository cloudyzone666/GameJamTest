using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GameJam2026.Abilities.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class AbilitySlotView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private GameObject selectionFrame;
        [SerializeField] private Image icon;
        [SerializeField] private Text iconFallback;
        [SerializeField] private Text abilityName;
        [SerializeField] private Text cost;
        [SerializeField] private Text equippedCount;

        private UnityAction clickHandler;
        public AbilityData Ability { get; private set; }
        public bool IsSelected { get; private set; }
        public int EquippedCount { get; private set; }

        public void Bind(AbilityData ability, Font font, Action onSelected)
        {
            Button button = GetComponent<Button>();
            if (clickHandler != null) button.onClick.RemoveListener(clickHandler);
            clickHandler = () => onSelected?.Invoke();
            button.onClick.AddListener(clickHandler);
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            Ability = ability;
            foreach (Text label in GetComponentsInChildren<Text>(true)) label.font = font;

            bool configured = ability != null;
            abilityName.text = configured ? ability.DisplayName : "未配置";
            cost.text = configured ? $"消耗 {ability.SlotCost} 格" : "空置位置";
            icon.sprite = configured ? ability.Icon : null;
            icon.enabled = icon.sprite != null;
            iconFallback.gameObject.SetActive(icon.sprite == null);
            iconFallback.text = configured ? "能力" : "空";
            // Empty positions stay clickable so the details can show their empty state.
            button.interactable = true;
            SetEquippedCount(EquippedCount);
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            RefreshAppearance();
        }

        public void SetEquippedCount(int count)
        {
            EquippedCount = Ability != null ? Mathf.Max(0, count) : 0;
            equippedCount.text = Ability != null ? $"装备 ×{EquippedCount}" : "—";
            RefreshAppearance();
        }

        private void RefreshAppearance()
        {
            // Equipment color stays visible independently from the selection border.
            selectionFrame.SetActive(IsSelected);
            // Image color tints the metal sprite; keep it light enough to retain the drawn bevels.
            background.color = EquippedCount > 0 ? new Color32(222, 242, 183, 255)
                : Ability != null ? new Color32(200, 220, 203, 255)
                : new Color32(139, 157, 143, 255);
            equippedCount.color = EquippedCount > 0 ? new Color32(193, 179, 129, 255)
                : new Color32(166, 181, 164, 255);
        }

        private void OnDestroy()
        {
            if (clickHandler != null) GetComponent<Button>().onClick.RemoveListener(clickHandler);
        }
    }
}
