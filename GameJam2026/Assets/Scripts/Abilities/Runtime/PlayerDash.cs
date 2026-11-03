using System;
using UnityEngine;

namespace GameJam2026.Abilities
{
    // Horizontal dash calculation only. The movement controller owns Rigidbody velocity.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerAbilityController))]
    public sealed class PlayerDash : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float collisionSkin = 0.01f;

        private readonly RaycastHit2D[] hits = new RaycastHit2D[16];
        private Rigidbody2D body;
        private PlayerAbilityController abilities;
        private float direction;
        private float remainingTime;
        private float scheduledStepDuration;

        public bool IsDashing { get; private set; }
        public float Speed { get; private set; }
        public event Action Interrupted;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            abilities = GetComponent<PlayerAbilityController>();
        }

        private void OnEnable() => abilities.AbilitiesChanged += OnAbilitiesChanged;

        private void OnDisable()
        {
            abilities.AbilitiesChanged -= OnAbilitiesChanged;
            Cancel();
        }

        private void OnAbilitiesChanged()
        {
            if (!abilities.HasAbility(AbilityType.Dash)) Cancel();
        }

        public bool TryStart(float horizontalDirection)
        {
            if (!isActiveAndEnabled || IsDashing || !abilities.isActiveAndEnabled
                || !abilities.TryGetEquippedAbility(AbilityType.Dash, out AbilityData equipped)
                || !(equipped is DashAbilityData definition) || !definition.TryValidate(out _)
                || definition.Distance <= 0f) return false;

            // A 16px tile at PPU 16 is one world unit in this project.
            float speed = definition.Distance / definition.Duration;
            if (float.IsNaN(speed) || float.IsInfinity(speed)) return false;
            if (speed > Physics2D.maxTranslationSpeed)
            {
                Debug.LogWarning("Dash speed exceeds the current Physics2D maximum translation speed. "
                    + "Increase Duration or reduce Distance in the ability asset.", this);
                return false;
            }
            Speed = speed;
            direction = horizontalDirection < 0f ? -1f : 1f;
            remainingTime = definition.Duration;
            scheduledStepDuration = 0f;
            IsDashing = true;
            return true;
        }

        // Finish the step that has actually been simulated before accepting new actions.
        public void CompletePreviousPhysicsStep()
        {
            if (!IsDashing) return;
            remainingTime = Mathf.Max(0f, remainingTime - scheduledStepDuration);
            scheduledStepDuration = 0f;
            if (remainingTime <= 0.000001f) IsDashing = false;
        }

        public bool TryCalculateVelocity(float deltaTime, out float horizontalVelocity)
        {
            horizontalVelocity = 0f;
            if (!IsDashing) return false;
            if (deltaTime <= 0f || !abilities.isActiveAndEnabled || !abilities.HasAbility(AbilityType.Dash))
            {
                Cancel();
                return false;
            }

            float stepDuration = Mathf.Min(remainingTime, deltaTime);
            float stepDistance = Speed * stepDuration;
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
            Vector2 heading = new Vector2(direction, 0f);
            int count = body.Cast(heading, filter, hits, stepDistance + collisionSkin);
            for (int index = 0; index < count; index++)
            {
                // Floor/ceiling contact must not stop a horizontal dash.
                if (hits[index].collider == null || hits[index].normal.x * direction >= -0.5f) continue;
                float allowedDistance = Mathf.Max(0f, hits[index].distance - collisionSkin);
                if (allowedDistance >= stepDistance) continue;
                stepDistance = allowedDistance;
                // Complete at the next step, after the wall-limited movement was simulated.
                remainingTime = stepDuration;
            }

            scheduledStepDuration = stepDuration;
            // Fractional final durations cover only their remaining distance in a full physics step.
            horizontalVelocity = direction * stepDistance / deltaTime;
            return true;
        }

        public void Cancel()
        {
            if (!IsDashing) return;
            IsDashing = false;
            remainingTime = 0f;
            scheduledStepDuration = 0f;
            Interrupted?.Invoke();
        }
    }
}
