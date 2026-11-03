using UnityEngine;

namespace GameJam2026.Abilities
{
    // Calculates jump motion. PlayerController2D remains the only velocity writer.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerAbilityController))]
    public sealed class PlayerJump : MonoBehaviour
    {
        [SerializeField, Range(0.01f, 1f)] private float minimumGroundNormalY = 0.65f;

        private readonly ContactPoint2D[] contacts = new ContactPoint2D[16];
        private Rigidbody2D body;
        private PlayerAbilityController abilities;
        private float previousPhysicsX;

        public bool IsGrounded { get; private set; }
        public bool IsJumping { get; private set; }
        public float RemainingHorizontalDistance { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            abilities = GetComponent<PlayerAbilityController>();
        }

        // Called once before each physics step, including while gameplay input is blocked.
        public Vector2 CalculateVelocity(float horizontalVelocity, bool jumpRequested, float deltaTime,
            bool previousStepWasDash = false)
        {
            if (IsJumping && !previousStepWasDash)
            {
                // Use the completed physics step, not rendered/interpolated Transform positions.
                // Reversing direction costs distance; being blocked by a wall costs none.
                float travelled = Mathf.Abs(body.position.x - previousPhysicsX);
                RemainingHorizontalDistance = Mathf.Max(0f, RemainingHorizontalDistance - travelled);
            }
            previousPhysicsX = body.position.x;

            Vector2 velocity = body.velocity;
            IsGrounded = HasGroundContact(velocity.y);
            if (IsGrounded && IsJumping)
            {
                IsJumping = false;
                RemainingHorizontalDistance = 0f;
            }

            if (jumpRequested && IsGrounded && TryGetJump(out JumpAbilityData definition))
            {
                float gravity = -Physics2D.gravity.y * body.gravityScale;
                if (gravity > 0f && definition.VerticalHeight > 0f)
                {
                    // Current project scale: a 16px tile at PPU 16 is one world unit.
                    velocity.y = Mathf.Sqrt(2f * gravity * definition.VerticalHeight);
                    RemainingHorizontalDistance = definition.MaxHorizontalDistance;
                    IsJumping = true;
                    IsGrounded = false;
                }
            }

            if (IsJumping)
            {
                // Clip the final step to the remaining path budget, without adding inertia.
                float maximumSpeed = deltaTime > 0f ? RemainingHorizontalDistance / deltaTime : 0f;
                horizontalVelocity = Mathf.Clamp(horizontalVelocity, -maximumSpeed, maximumSpeed);
            }
            velocity.x = horizontalVelocity;
            return velocity;
        }

        private bool TryGetJump(out JumpAbilityData definition)
        {
            definition = null;
            if (!abilities.isActiveAndEnabled
                || !abilities.TryGetEquippedAbility(AbilityType.Jump, out AbilityData equipped)) return false;
            definition = equipped as JumpAbilityData;
            return definition != null && definition.TryValidate(out _);
        }

        private bool HasGroundContact(float verticalVelocity)
        {
            // Takeoff contacts may still exist for one step. Never re-arm while ascending.
            if (verticalVelocity > 0.01f) return false;
            var filter = new ContactFilter2D { useTriggers = false };
            int count = body.GetContacts(filter, contacts);
            for (int index = 0; index < count; index++)
                if (contacts[index].normal.y >= minimumGroundNormalY) return true;
            return false;
        }
    }
}
