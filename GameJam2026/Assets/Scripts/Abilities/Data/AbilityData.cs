using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameJam2026.Abilities
{
    // Definitions are shared assets. Per-player equipment state belongs elsewhere.
    public abstract class AbilityData : ScriptableObject
    {
        [SerializeField] private string abilityId;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField, TextArea] private string description;
        [SerializeField, Min(0)] private int slotCost;

        public string AbilityId => abilityId;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public string Description => description;
        public int SlotCost => slotCost;
        public abstract AbilityType Type { get; }

        // UI can display parameters without depending on a concrete ability class.
        public abstract IReadOnlyList<AbilityParameter> GetParameters();

        public virtual bool TryValidate(out string error)
        {
            if (string.IsNullOrWhiteSpace(abilityId))
            {
                error = "Ability ID must not be empty.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                error = "Ability display name must not be empty.";
                return false;
            }

            if (slotCost < 0)
            {
                error = "Ability slot cost must not be negative.";
                return false;
            }

            if (!Enum.IsDefined(typeof(AbilityType), Type))
            {
                error = "Ability type is not defined.";
                return false;
            }

            error = null;
            return true;
        }

        protected static bool IsNonNegativeFinite(float value)
        {
            return value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        [ContextMenu("Validate Configuration")]
        private void ValidateConfiguration()
        {
            if (TryValidate(out string error))
                Debug.Log($"{name}: ability configuration is valid.", this);
            else
                Debug.LogError($"{name}: {error}", this);
        }
    }

    public readonly struct AbilityParameter
    {
        public string DisplayName { get; }
        public float Value { get; }
        public string Unit { get; }

        public AbilityParameter(string displayName, float value, string unit = "")
        {
            DisplayName = displayName;
            Value = value;
            Unit = unit;
        }
    }
}
