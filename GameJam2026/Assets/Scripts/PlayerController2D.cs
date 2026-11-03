using GameJam2026.Abilities;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerController2D : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 5f;

    private Rigidbody2D body;
    private PlayerJump jump;
    private PlayerDash dash;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction dashAction;
    private float moveInput;
    private float lastFacing = 1f;
    private bool jumpRequested;
    private bool dashRequested;
    private bool jumpAfterDash;
    private bool previousStepWasDash;

    public bool GameplayInputEnabled { get; private set; } = true;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        jump = GetComponent<PlayerJump>();
        dash = GetComponent<PlayerDash>();

        moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Axis");
        moveAction.AddCompositeBinding("1DAxis(whichSideWins=0)")
            .With("Negative", "<Keyboard>/a")
            .With("Positive", "<Keyboard>/d");

        jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
        jumpAction.wantsInitialStateCheck = false;
        jumpAction.performed += OnJumpPerformed;

        dashAction = new InputAction("Dash", InputActionType.Button, "<Keyboard>/leftShift");
        dashAction.wantsInitialStateCheck = false;
        dashAction.performed += OnDashPerformed;
    }

    private void OnEnable()
    {
        if (dash != null) dash.Interrupted += OnDashInterrupted;
        if (GameplayInputEnabled)
        {
            moveAction.Enable();
            jumpAction.Enable();
            dashAction.Enable();
        }
    }

    private void Update()
    {
        moveInput = GameplayInputEnabled ? moveAction.ReadValue<float>() : 0f;
        if (moveInput != 0f) lastFacing = Mathf.Sign(moveInput);
    }

    private void FixedUpdate()
    {
        bool hasDash = dash != null && dash.isActiveAndEnabled;
        if (hasDash) dash.CompletePreviousPhysicsStep();

        // Read the current action for direction so Shift+D in one batch uses D,
        // even if no rendered Update occurred between the input and physics step.
        float directionInput = GameplayInputEnabled ? moveAction.ReadValue<float>() : 0f;
        if (directionInput != 0f) lastFacing = Mathf.Sign(directionInput);
        if (GameplayInputEnabled && dashRequested && hasDash)
            dash.TryStart(directionInput != 0f ? directionInput : lastFacing);

        float dashVelocity = 0f;
        bool dashStep = hasDash && dash.TryCalculateVelocity(Time.fixedDeltaTime, out dashVelocity);
        bool executeJump = GameplayInputEnabled && jumpRequested;
        if (dashStep)
        {
            if (executeJump) jumpAfterDash = true;
            executeJump = false;
        }
        else if (jumpAfterDash)
        {
            // PlayerJump rechecks actual ground contact and equipment at completion.
            executeJump = GameplayInputEnabled;
            jumpAfterDash = false;
        }

        float horizontalVelocity = moveInput * moveSpeed;
        Vector2 velocity = jump != null && jump.isActiveAndEnabled
            ? jump.CalculateVelocity(horizontalVelocity, executeJump, Time.fixedDeltaTime, previousStepWasDash)
            : new Vector2(horizontalVelocity, body.velocity.y);
        // Dash bypasses the ordinary movement budget; keep gravity and vertical motion.
        if (dashStep) velocity.x = dashVelocity;
        previousStepWasDash = dashStep;
        jumpRequested = false;
        dashRequested = false;
        body.velocity = velocity;
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        // Keep a short press until the next physics step; menu opening clears it first.
        if (GameplayInputEnabled) jumpRequested = true;
    }

    private void OnDashPerformed(InputAction.CallbackContext context)
    {
        if (GameplayInputEnabled) dashRequested = true;
    }

    private void OnDashInterrupted()
    {
        jumpAfterDash = false;
        StopHorizontalMovement();
        // Retain previousStepWasDash until the completed dash displacement is accounted for.
    }

    private void OnDisable()
    {
        dash?.Cancel();
        if (dash != null) dash.Interrupted -= OnDashInterrupted;
        StopHorizontalMovement();
        moveAction?.Disable();
        jumpAction?.Disable();
        dashAction?.Disable();
        jumpRequested = false;
        dashRequested = false;
        jumpAfterDash = false;
    }

    // The menu blocks control without disabling this component or pausing physics.
    public void SetGameplayInputEnabled(bool value)
    {
        GameplayInputEnabled = value;
        if (!value)
        {
            moveAction?.Disable();
            jumpAction?.Disable();
            dashAction?.Disable();
            jumpRequested = false;
            dashRequested = false;
            jumpAfterDash = false;
            dash?.Cancel();
            StopHorizontalMovement();
        }
        else if (isActiveAndEnabled)
        {
            moveAction?.Enable();
            jumpAction?.Enable();
            dashAction?.Enable();
        }
    }

    private void StopHorizontalMovement()
    {
        moveInput = 0f;

        if (body != null)
        {
            body.velocity = new Vector2(0f, body.velocity.y);
        }
    }

    private void OnDestroy()
    {
        moveAction?.Dispose();
        jumpAction?.Dispose();
        dashAction?.Dispose();
    }
}
