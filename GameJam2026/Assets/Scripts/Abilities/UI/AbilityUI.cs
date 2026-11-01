using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace GameJam2026.Abilities.UI
{
    // Presents selection and equipment snapshots. Commands are handled outside this view.
    public sealed class AbilityUI : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private AbilityCatalog catalog;
        [SerializeField] private AbilitySystemConfig systemConfig;
        [SerializeField] private Font fontOverride;
        [Header("Inventory")]
        [SerializeField] private Transform inventoryColumns;
        [SerializeField] private AbilitySlotView slotPrefab;
        [Header("Equipment display")]
        [SerializeField] private Transform equipmentPositions;
        [SerializeField] private Transform capacityCells;
        [SerializeField] private Text equippedCount;
        [SerializeField] private Text remainingCapacity;
        [SerializeField] private Text operationFeedback;
        [Header("Selection details")]
        [SerializeField] private Image detailIcon;
        [SerializeField] private Text detailIconFallback;
        [SerializeField] private Text detailName;
        [SerializeField] private Text detailType;
        [SerializeField] private Text detailCost;
        [SerializeField] private Text detailDescription;
        [SerializeField] private Text detailParameters;
        [SerializeField] private Text detailStatus;

        private readonly List<List<AbilitySlotView>> columns = new List<List<AbilitySlotView>>();
        private readonly List<Image> equipmentIndicators = new List<Image>();
        private readonly List<Image> capacityIndicators = new List<Image>();
        private const string DefaultOperationHint = "WASD：选择 · Space：装备 / 卸载";
        private AbilityManager equipment;
        private Font displayFont;
        private Font ownedFont;
        private bool initialized;
        public int SelectedColumn { get; private set; } = -1;
        public int SelectedRow { get; private set; } = -1;
        public int SlotCount { get; private set; }
        public AbilityData SelectedAbility { get; private set; }

        private void Awake()
        {
            if (!ValidateBindings(out string error))
            {
                Debug.LogError($"AbilityUI: {error}", this);
                enabled = false;
                return;
            }
            displayFont = fontOverride;
            if (displayFont == null)
            {
                // Local Windows whitebox fallback; no system font is copied into Assets.
                ownedFont = Font.CreateDynamicFontFromOSFont(
                    new[] { "Microsoft YaHei", "SimHei", "Arial Unicode MS", "Arial" }, 20);
                displayFont = ownedFont;
            }
            foreach (Text label in GetComponentsInChildren<Text>(true)) label.font = displayFont;
            BuildInitialCapacity();
            BuildInventory();
            initialized = true;
            SelectSlot(0, 0);
        }

        private bool ValidateBindings(out string error)
        {
            if (catalog == null || systemConfig == null || inventoryColumns == null || slotPrefab == null
                || equipmentPositions == null || capacityCells == null || equippedCount == null
                || remainingCapacity == null || operationFeedback == null
                || detailIcon == null || detailIconFallback == null
                || detailName == null || detailType == null || detailCost == null
                || detailDescription == null || detailParameters == null || detailStatus == null)
            {
                error = "Required scene references are missing. See the AbilityUI Inspector.";
                return false;
            }
            return catalog.TryValidate(out error) && systemConfig.TryValidate(out error);
        }

        private void BuildInitialCapacity()
        {
            ShowCapacity(0, systemConfig.MaxEquippedCount, 0, systemConfig.TotalCapacity);
        }

        private void ShowCapacity(int count, int maximum, int used, int total)
        {
            equippedCount.text = $"已装备 {count} / {maximum}";
            remainingCapacity.text = $"剩余能力格 {total - used} / {total}";
            ResizeIndicators(equipmentIndicators, equipmentPositions, "Position", maximum);
            ResizeIndicators(capacityIndicators, capacityCells, "Capacity", total);
            for (int index = 0; index < equipmentIndicators.Count; index++)
                equipmentIndicators[index].color = index < count
                    ? new Color32(148, 210, 239, 255) : new Color32(57, 65, 77, 255);
            for (int index = 0; index < capacityIndicators.Count; index++)
                capacityIndicators[index].color = index < used
                    ? new Color32(57, 65, 77, 255) : new Color32(149, 202, 222, 255);
        }

        private static void ResizeIndicators(List<Image> indicators, Transform parent, string prefix, int count)
        {
            while (indicators.Count > count)
            {
                int last = indicators.Count - 1;
                Image indicator = indicators[last];
                indicators.RemoveAt(last);
                // Remove it from layout now; Destroy itself is deferred until the end of the frame.
                indicator.gameObject.SetActive(false);
                indicator.transform.SetParent(null, false);
                Destroy(indicator.gameObject);
            }
            while (indicators.Count < count)
                indicators.Add(CreateIndicator(parent, $"{prefix}_{indicators.Count}"));
        }

        private static Image CreateIndicator(Transform parent, string objectName)
        {
            var item = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            item.transform.SetParent(parent, false);
            Image image = item.GetComponent<Image>();
            image.raycastTarget = false;
            LayoutElement layout = item.GetComponent<LayoutElement>();
            layout.flexibleWidth = 1;
            layout.flexibleHeight = 1;
            return image;
        }

        private void BuildInventory()
        {
            // Built once in Awake. Re-enabling the panel does not duplicate views/listeners.
            for (int columnIndex = 0; columnIndex < catalog.Categories.Count; columnIndex++)
            {
                AbilityCategory category = catalog.Categories[columnIndex];
                var column = new GameObject($"Column_{columnIndex}", typeof(RectTransform),
                    typeof(VerticalLayoutGroup), typeof(LayoutElement));
                column.transform.SetParent(inventoryColumns, false);
                var layout = column.GetComponent<VerticalLayoutGroup>();
                layout.spacing = 12;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
                column.GetComponent<LayoutElement>().flexibleWidth = 1;

                var header = new GameObject("CategoryName", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
                header.transform.SetParent(column.transform, false);
                Text text = header.GetComponent<Text>();
                text.font = displayFont;
                text.fontSize = 24;
                text.text = category.DisplayName;
                text.color = new Color32(222, 231, 241, 255);
                text.alignment = TextAnchor.MiddleCenter;
                text.raycastTarget = false;
                header.GetComponent<LayoutElement>().preferredHeight = 36;

                var slots = new List<AbilitySlotView>();
                columns.Add(slots);
                for (int rowIndex = 0; rowIndex < category.Slots.Count; rowIndex++)
                {
                    int columnCoordinate = columnIndex;
                    int rowCoordinate = rowIndex;
                    AbilitySlotView view = Instantiate(slotPrefab, column.transform);
                    view.name = $"Slot_{columnIndex}_{rowIndex}";
                    view.Bind(category.Slots[rowIndex], displayFont,
                        () => SelectSlot(columnCoordinate, rowCoordinate));
                    slots.Add(view);
                    SlotCount++;
                }
            }
        }

        public bool SelectSlot(int column, int row)
        {
            if (column < 0 || column >= columns.Count || row < 0 || row >= columns[column].Count)
                return false;
            if (SelectedColumn >= 0) columns[SelectedColumn][SelectedRow].SetSelected(false);
            SelectedColumn = column;
            SelectedRow = row;
            AbilitySlotView selected = columns[column][row];
            selected.SetSelected(true);
            SelectedAbility = selected.Ability;
            ShowDetails(catalog.Categories[column]);
            ShowOperationFeedback(DefaultOperationHint, false);
            return true;
        }

        // Called by the equipment controller after initialization and after state changes.
        // Keep the manager only for queries when the selection changes later.
        public void RefreshEquipment(AbilityManager manager)
        {
            equipment = manager;
            if (!initialized) return;
            ShowCapacity(manager?.EquippedCount ?? 0, manager?.MaxEquippedCount ?? systemConfig.MaxEquippedCount,
                manager?.UsedCapacity ?? 0, manager?.TotalCapacity ?? systemConfig.TotalCapacity);
            foreach (List<AbilitySlotView> column in columns)
                foreach (AbilitySlotView slot in column)
                    slot.SetEquippedCount(manager?.GetEquippedCount(slot.Ability) ?? 0);
            if (SelectedColumn >= 0) ShowDetails(catalog.Categories[SelectedColumn]);
        }

        public void ShowOperationFeedback(string message, bool isError)
        {
            if (operationFeedback == null) return;
            operationFeedback.text = message ?? string.Empty;
            operationFeedback.color = isError
                ? new Color32(236, 151, 140, 255) : new Color32(154, 174, 192, 255);
        }

        // Row indices grow downward in the inventory. Empty slots keep their positions.
        public bool MoveSelection(Vector2Int direction)
        {
            if (SelectedColumn < 0 || SelectedRow < 0
                || Mathf.Abs(direction.x) + Mathf.Abs(direction.y) != 1)
                return false;

            int column = Mathf.Clamp(SelectedColumn + direction.x, 0, columns.Count - 1);
            int row = Mathf.Clamp(SelectedRow + direction.y, 0, columns[column].Count - 1);
            if (column == SelectedColumn && row == SelectedRow) return false;
            return SelectSlot(column, row);
        }

        private void ShowDetails(AbilityCategory category)
        {
            bool configured = SelectedAbility != null;
            detailIcon.sprite = configured ? SelectedAbility.Icon : null;
            detailIcon.enabled = detailIcon.sprite != null;
            detailIconFallback.gameObject.SetActive(detailIcon.sprite == null);
            detailIconFallback.text = configured ? "图标\n占位" : "空";
            detailName.text = configured ? SelectedAbility.DisplayName : "未配置能力";
            detailType.text = $"类型：{category.DisplayName} / {category.Type}";
            detailCost.text = configured ? $"能力格消耗：{SelectedAbility.SlotCost}" : "能力格消耗：—";
            detailDescription.text = configured ? SelectedAbility.Description : "此位置尚未配置能力。";
            int count = equipment?.GetEquippedCount(SelectedAbility) ?? 0;
            detailStatus.text = configured
                ? count > 0 ? $"已装备 · ×{count}" : "未装备 · ×0"
                : "空置位置 · 无装备状态";
            if (!configured)
            {
                detailParameters.text = "暂无能力参数";
                return;
            }
            var builder = new StringBuilder();
            foreach (AbilityParameter parameter in SelectedAbility.GetParameters())
            {
                if (builder.Length > 0) builder.Append('\n');
                builder.Append(parameter.DisplayName).Append("：")
                    .Append(parameter.Value.ToString("0.###", CultureInfo.InvariantCulture));
                if (!string.IsNullOrEmpty(parameter.Unit)) builder.Append(' ').Append(parameter.Unit);
            }
            detailParameters.text = builder.Length > 0 ? builder.ToString() : "暂无能力参数";
        }

        private void OnDestroy()
        {
            if (ownedFont != null) Destroy(ownedFont);
        }
    }
}
