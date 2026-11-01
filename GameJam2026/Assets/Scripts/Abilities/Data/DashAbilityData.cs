using System.Collections.Generic;
using UnityEngine;

namespace GameJam2026.Abilities
{
    [CreateAssetMenu(fileName = "DashAbility", menuName = "GameJam2026/Abilities/Dash Ability")]
    public sealed class DashAbilityData : AbilityData
    {
        [SerializeField, Min(0f)] private float distance;

        public override AbilityType Type => AbilityType.Dash;
        public float Distance => distance;

        public override IReadOnlyList<AbilityParameter> GetParameters()
        {
            return new[] { new AbilityParameter("冲刺距离", distance) };
        }

        public override bool TryValidate(out string error)
        {
            if (!base.TryValidate(out error))
                return false;

            if (!IsNonNegativeFinite(distance))
            {
                error = "Dash distance must be finite and non-negative.";
                return false;
            }

            error = null;
            return true;
        }
    }
}
