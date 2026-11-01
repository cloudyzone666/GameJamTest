using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameJam2026.Abilities
{
    [CreateAssetMenu(fileName = "AbilityCatalog", menuName = "GameJam2026/Abilities/Catalog")]
    public sealed class AbilityCatalog : ScriptableObject
    {
        [SerializeField] private AbilityCategory[] categories = Array.Empty<AbilityCategory>();

        public IReadOnlyList<AbilityCategory> Categories =>
            Array.AsReadOnly(categories ?? Array.Empty<AbilityCategory>());

        public bool TryValidate(out string error)
        {
            if (categories == null || categories.Length == 0)
            {
                error = "The catalog must contain at least one category.";
                return false;
            }

            var types = new HashSet<AbilityType>();
            var ids = new HashSet<string>(StringComparer.Ordinal);

            for (int column = 0; column < categories.Length; column++)
            {
                AbilityCategory category = categories[column];
                if (category == null || !Enum.IsDefined(typeof(AbilityType), category.Type))
                {
                    error = $"Catalog column {column}: missing or invalid category.";
                    return false;
                }

                if (!types.Add(category.Type))
                {
                    error = $"Catalog category {category.Type} is duplicated.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(category.DisplayName) || category.Slots.Count == 0)
                {
                    error = $"Category {category.Type} requires a name and at least one position.";
                    return false;
                }

                IReadOnlyList<AbilityData> slots = category.Slots;
                for (int row = 0; row < slots.Count; row++)
                {
                    AbilityData ability = slots[row];
                    if (ability == null)
                        continue; // Empty positions are intentional and retain their coordinates.

                    if (!ability.TryValidate(out string abilityError))
                    {
                        error = $"{category.Type}[{row}]: {abilityError}";
                        return false;
                    }

                    if (ability.Type != category.Type)
                    {
                        error = $"{category.Type}[{row}]: ability type {ability.Type} does not match its category.";
                        return false;
                    }

                    if (!ids.Add(ability.AbilityId))
                    {
                        error = $"Ability ID '{ability.AbilityId}' appears more than once in the catalog.";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }

        [ContextMenu("Validate Configuration")]
        private void ValidateConfiguration()
        {
            if (TryValidate(out string error))
                Debug.Log($"{name}: catalog configuration is valid.", this);
            else
                Debug.LogError($"{name}: {error}", this);
        }
    }

    [Serializable]
    public sealed class AbilityCategory
    {
        [SerializeField] private AbilityType type;
        [SerializeField] private string displayName;
        [SerializeField] private AbilityData[] slots = Array.Empty<AbilityData>();

        public AbilityType Type => type;
        public string DisplayName => displayName;
        public IReadOnlyList<AbilityData> Slots => Array.AsReadOnly(slots ?? Array.Empty<AbilityData>());
    }
}
