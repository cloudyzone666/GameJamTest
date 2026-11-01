using System;

namespace GameJam2026.Abilities
{
    // One runtime copy of a shared ability definition. This is not a saved asset.
    public sealed class EquippedAbility
    {
        public Guid InstanceId { get; }
        public AbilityData Ability { get; }
        public string AbilityId { get; }
        public int ChargedSlotCost { get; }

        internal EquippedAbility(AbilityData ability)
        {
            InstanceId = Guid.NewGuid();
            Ability = ability;
            AbilityId = ability.AbilityId;
            ChargedSlotCost = ability.SlotCost;
        }
    }
}
