using System;
using System.Text;
using UnityEngine;

namespace GameJam2026.Abilities
{
    // A player-facing view of the existing equipment session, not a second inventory.
    [DisallowMultipleComponent]
    public sealed class PlayerAbilityController : MonoBehaviour
    {
        private AbilityManager manager;
        private bool connected;

        public bool IsBound => manager != null;
        public event Action AbilitiesChanged;

        public void BindManager(AbilityManager value)
        {
            if (ReferenceEquals(manager, value)) return;
            Disconnect();
            manager = value;
            Connect();
            if (isActiveAndEnabled) AbilitiesChanged?.Invoke();
        }

        public bool HasAbility(AbilityType type) => GetEquippedCount(type) > 0;
        public bool HasAbility(AbilityData definition) => GetEquippedCount(definition) > 0;

        public int GetEquippedCount(AbilityData definition)
        {
            return manager != null ? manager.GetEquippedCount(definition) : 0;
        }

        public int GetEquippedCount(AbilityType type)
        {
            if (manager == null) return 0;
            int count = 0;
            foreach (EquippedAbility instance in manager.EquippedAbilities)
                if (instance.Ability != null && instance.Ability.Type == type) count++;
            return count;
        }

        // Query order follows the real equipment list. This does not combine parameters
        // or choose how multiple different definitions should execute in later stages.
        public bool TryGetEquippedAbility(AbilityType type, out AbilityData definition)
        {
            if (manager != null)
            {
                foreach (EquippedAbility instance in manager.EquippedAbilities)
                {
                    if (instance.Ability == null || instance.Ability.Type != type) continue;
                    definition = instance.Ability;
                    return true;
                }
            }
            definition = null;
            return false;
        }

        private void Connect()
        {
            if (connected || manager == null || !isActiveAndEnabled) return;
            manager.EquipmentChanged += OnEquipmentChanged;
            connected = true;
        }

        private void Disconnect()
        {
            if (!connected) return;
            manager.EquipmentChanged -= OnEquipmentChanged;
            connected = false;
        }

        private void OnEquipmentChanged() => AbilitiesChanged?.Invoke();

        private void OnEnable()
        {
            Connect();
            AbilitiesChanged?.Invoke();
        }

        private void OnDisable() => Disconnect();

        private void OnDestroy()
        {
            Disconnect();
            manager = null;
            AbilitiesChanged = null;
        }

        [ContextMenu("Debug/Log Equipped Abilities")]
        private void LogEquippedAbilities()
        {
            var message = new StringBuilder($"Player ability session: {(IsBound ? "connected" : "unbound")}");
            foreach (AbilityType type in Enum.GetValues(typeof(AbilityType)))
                message.Append($"\n{type}: {GetEquippedCount(type)}");
            Debug.Log(message.ToString(), this);
        }
    }
}
