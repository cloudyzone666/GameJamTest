using UnityEngine;

namespace GameJam2026.Abilities
{
    [CreateAssetMenu(fileName = "AbilitySystemConfig", menuName = "GameJam2026/Abilities/System Config")]
    public sealed class AbilitySystemConfig : ScriptableObject
    {
        [SerializeField, Min(0)] private int totalCapacity;
        [SerializeField, Min(0)] private int maxEquippedCount;

        public int TotalCapacity => totalCapacity;
        public int MaxEquippedCount => maxEquippedCount;

        public bool TryValidate(out string error)
        {
            if (totalCapacity < 0 || maxEquippedCount < 0)
            {
                error = "Capacity and maximum equipped count must be non-negative.";
                return false;
            }

            error = null;
            return true;
        }

        [ContextMenu("Validate Configuration")]
        private void ValidateConfiguration()
        {
            if (TryValidate(out string error))
                Debug.Log($"{name}: system configuration is valid.", this);
            else
                Debug.LogError($"{name}: {error}", this);
        }
    }
}
