using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerController2D : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 5f;

    private Rigidbody2D body;
    private InputAction moveAction;
    private float moveInput;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();

        moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Axis");
        moveAction.AddCompositeBinding("1DAxis(whichSideWins=0)")
            .With("Negative", "<Keyboard>/a")
            .With("Positive", "<Keyboard>/d");
    }

    private void OnEnable()
    {
        moveAction.Enable();
    }

    private void Update()
    {
        moveInput = moveAction.ReadValue<float>();
    }

    private void FixedUpdate()
    {
        // Keep the vertical velocity produced by gravity and collisions.
        body.velocity = new Vector2(moveInput * moveSpeed, body.velocity.y);
    }

    private void OnDisable()
    {
        moveInput = 0f;

        if (body != null)
        {
            body.velocity = new Vector2(0f, body.velocity.y);
        }

        moveAction?.Disable();
    }

    private void OnDestroy()
    {
        moveAction?.Dispose();
    }
}
