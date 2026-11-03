using System;
using GameJam2026.Abilities.Input;
using GameJam2026.Abilities.UI;
using UnityEngine;

namespace GameJam2026.Abilities.Integration
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AbilityUI), typeof(AbilityInput))]
    public sealed class AbilityEquipmentController : MonoBehaviour
    {
        [SerializeField] private AbilitySystemConfig systemConfig;
        [SerializeField] private AbilityUI ui;
        [SerializeField] private AbilityInput input;

        private bool started;
        private bool connected;

        // One scene-owned equipment session. Disabling the panel does not reset it.
        public AbilityManager Manager { get; private set; }

        // A plain C# manager does not become Unity-null when its scene owner is destroyed.
        public event Action SessionEnded;

        private void Awake()
        {
            if (ui == null) ui = GetComponent<AbilityUI>();
            if (input == null) input = GetComponent<AbilityInput>();
            if (systemConfig == null || ui == null || input == null)
            {
                Debug.LogError("AbilityEquipmentController: Configuration, UI and Input are required.", this);
                enabled = false;
                return;
            }
            if (!systemConfig.TryValidate(out string error))
            {
                Debug.LogError($"AbilityEquipmentController: {error}", this);
                enabled = false;
                return;
            }
            Manager = new AbilityManager(systemConfig);
        }

        private void Start()
        {
            // Every scene object's Awake has finished, including UI view construction.
            started = true;
            Connect();
        }

        private void OnEnable()
        {
            if (started) Connect();
        }

        private void Connect()
        {
            if (connected || Manager == null || ui == null || input == null) return;
            input.ToggleRequested += OnToggleRequested;
            Manager.EquipmentChanged += RefreshEquipment;
            connected = true;
            RefreshEquipment();
        }

        private void Disconnect()
        {
            if (!connected) return;
            if (input != null)
            {
                input.ToggleRequested -= OnToggleRequested;
            }
            if (Manager != null) Manager.EquipmentChanged -= RefreshEquipment;
            connected = false;
        }

        private bool CanHandleRequest(AbilityData ability)
        {
            return isActiveAndEnabled && Manager != null && ui != null
                && ui.isActiveAndEnabled && ability != null;
        }

        private void OnToggleRequested(AbilityData ability)
        {
            if (!CanHandleRequest(ability)) return;
            if (Manager.GetEquippedCount(ability) == 0)
            {
                Equip(ability);
                return;
            }

            if (Manager.TryUnequipLast(ability, out _))
                ui.ShowOperationFeedback($"已卸载：{ability.DisplayName}（剩余 ×{Manager.GetEquippedCount(ability)}）", false);
            else
                ui.ShowOperationFeedback("卸载失败：未找到能力实例。", true);
        }

        private void Equip(AbilityData ability)
        {
            AbilityEquipResult result = Manager.TryEquip(ability, out _);
            switch (result)
            {
                case AbilityEquipResult.Success:
                    ui.ShowOperationFeedback($"已装备：{ability.DisplayName}（×{Manager.GetEquippedCount(ability)}）", false);
                    break;
                case AbilityEquipResult.MaxEquippedCountReached:
                    ui.ShowOperationFeedback($"装备失败：最多装备 {Manager.MaxEquippedCount} 个能力实例。", true);
                    break;
                case AbilityEquipResult.NotEnoughCapacity:
                    ui.ShowOperationFeedback($"装备失败：能力格不足（需要 {ability.SlotCost}，剩余 {Manager.RemainingCapacity}）。", true);
                    break;
                default:
                    ui.ShowOperationFeedback("装备失败：能力配置无效。", true);
                    break;
            }
        }

        private void RefreshEquipment()
        {
            if (ui != null) ui.RefreshEquipment(Manager);
        }

        private void OnDisable() => Disconnect();
        private void OnDestroy()
        {
            Disconnect();
            Manager = null;
            SessionEnded?.Invoke();
        }
    }
}
