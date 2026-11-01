using System.Collections.Generic;
using UnityEngine;

namespace GameJam2026.Abilities
{
    [CreateAssetMenu(fileName = "JumpAbility", menuName = "GameJam2026/Abilities/Jump Ability")]
    public sealed class JumpAbilityData : AbilityData
    {
        [SerializeField, Min(0f)] private float verticalHeight;
        [SerializeField, Min(0f)] private float maxHorizontalDistance;

        public override AbilityType Type => AbilityType.Jump;
        public float VerticalHeight => verticalHeight;
        public float MaxHorizontalDistance => maxHorizontalDistance;

        public override IReadOnlyList<AbilityParameter> GetParameters()
        {
            return new[]
            {
                new AbilityParameter("竖直跳跃高度", verticalHeight),
                new AbilityParameter("水平最大移动距离", maxHorizontalDistance)
            };
        }

        public override bool TryValidate(out string error)
        {
            if (!base.TryValidate(out error))
                return false;

            if (!IsNonNegativeFinite(verticalHeight) || !IsNonNegativeFinite(maxHorizontalDistance))
            {
                error = "Jump parameters must be finite and non-negative.";
                return false;
            }

            error = null;
            return true;
        }
    }
}
