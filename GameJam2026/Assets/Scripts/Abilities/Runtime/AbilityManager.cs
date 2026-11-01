using System;
using System.Collections.Generic;

namespace GameJam2026.Abilities
{
    // The scene/UI owns this object. No input, singleton, or ability execution lives here.
    public sealed class AbilityManager
    {
        private readonly List<EquippedAbility> equippedAbilities = new List<EquippedAbility>();

        public IReadOnlyList<EquippedAbility> EquippedAbilities { get; }
        public int TotalCapacity { get; }
        public int MaxEquippedCount { get; }
        public int EquippedCount => equippedAbilities.Count;
        public int UsedCapacity { get; private set; }
        public int RemainingCapacity => TotalCapacity - UsedCapacity;

        // Raised once, after a successful mutation. Subscribers read the complete state.
        public event Action EquipmentChanged;

        public AbilityManager(AbilitySystemConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (!config.TryValidate(out string error))
                throw new ArgumentException(error, nameof(config));

            // Inspector edits affect the next manager, not an active equipment session.
            TotalCapacity = config.TotalCapacity;
            MaxEquippedCount = config.MaxEquippedCount;
            EquippedAbilities = equippedAbilities.AsReadOnly();
        }

        public AbilityEquipResult CheckEquip(AbilityData ability)
        {
            if (ability == null || !ability.TryValidate(out _))
                return AbilityEquipResult.InvalidAbility;

            if (EquippedCount >= MaxEquippedCount)
                return AbilityEquipResult.MaxEquippedCountReached;

            // Compare against remaining capacity instead of adding possibly large costs.
            if (ability.SlotCost > RemainingCapacity)
                return AbilityEquipResult.NotEnoughCapacity;

            return AbilityEquipResult.Success;
        }

        public AbilityEquipResult TryEquip(AbilityData ability, out EquippedAbility instance)
        {
            instance = null;
            AbilityEquipResult result = CheckEquip(ability);
            if (result != AbilityEquipResult.Success)
                return result;

            instance = new EquippedAbility(ability);
            equippedAbilities.Add(instance);
            UsedCapacity += instance.ChargedSlotCost;
            EquipmentChanged?.Invoke();
            return AbilityEquipResult.Success;
        }

        public bool TryUnequip(Guid instanceId, out EquippedAbility removedInstance)
        {
            for (int index = 0; index < equippedAbilities.Count; index++)
            {
                if (equippedAbilities[index].InstanceId == instanceId)
                {
                    removedInstance = RemoveAt(index);
                    EquipmentChanged?.Invoke();
                    return true;
                }
            }

            removedInstance = null;
            return false;
        }

        // Match the stable definition ID. Do not change IDs during an equipment session.
        // A toggle request can remove the last equipped matching instance.
        public bool TryUnequipLast(AbilityData ability, out EquippedAbility removedInstance)
        {
            if (ability != null)
            {
                for (int index = equippedAbilities.Count - 1; index >= 0; index--)
                {
                    if (string.Equals(equippedAbilities[index].AbilityId, ability.AbilityId,
                        StringComparison.Ordinal))
                    {
                        removedInstance = RemoveAt(index);
                        EquipmentChanged?.Invoke();
                        return true;
                    }
                }
            }

            removedInstance = null;
            return false;
        }

        // Definitions with the same ID represent the same logical ability.
        public int GetEquippedCount(AbilityData ability)
        {
            if (ability == null)
                return 0;

            int count = 0;
            foreach (EquippedAbility instance in equippedAbilities)
            {
                if (string.Equals(instance.AbilityId, ability.AbilityId, StringComparison.Ordinal))
                    count++;
            }

            return count;
        }

        private EquippedAbility RemoveAt(int index)
        {
            EquippedAbility instance = equippedAbilities[index];
            equippedAbilities.RemoveAt(index);
            // Refund the charged amount even if the shared asset was edited meanwhile.
            UsedCapacity -= instance.ChargedSlotCost;
            return instance;
        }
    }
}
