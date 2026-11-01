using System;
using GameJam2026.Abilities.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameJam2026.Abilities.Input
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AbilityUI))]
    public sealed class AbilityInput : MonoBehaviour
    {
        [SerializeField] private AbilityUI ui;
        [Tooltip("Log input requests for debugging. The integration component applies equipment changes.")]
        [SerializeField] private bool logRequests;

        private InputActionMap actions;

        // The integration layer will decide whether Toggle means equip or unequip.
        public event Action<AbilityData> ToggleRequested;

        private bool CanHandleInput => ui != null && ui.isActiveAndEnabled;

        private void Awake()
        {
            if (ui == null) ui = GetComponent<AbilityUI>();
            actions = new InputActionMap("AbilityEquipment");
            AddDirection("Left", "<Keyboard>/a", Vector2Int.left);
            AddDirection("Right", "<Keyboard>/d", Vector2Int.right);
            AddDirection("Up", "<Keyboard>/w", new Vector2Int(0, -1));
            AddDirection("Down", "<Keyboard>/s", new Vector2Int(0, 1));
            InputAction submit = actions.AddAction("Submit", InputActionType.Button, "<Keyboard>/space");
            submit.wantsInitialStateCheck = false;
            submit.performed += OnSubmit;
        }

        private void AddDirection(string name, string binding, Vector2Int direction)
        {
            InputAction action = actions.AddAction(name, InputActionType.Button, binding);
            // Enabling the panel must not replay keys already held before it was enabled.
            action.wantsInitialStateCheck = false;
            action.performed += _ =>
            {
                if (CanHandleInput) ui.MoveSelection(direction);
            };
        }

        private void OnSubmit(InputAction.CallbackContext context)
        {
            if (!CanHandleInput || ui.SelectedAbility == null) return;

            AbilityData ability = ui.SelectedAbility;
            if (logRequests)
                Debug.Log($"能力输入请求：切换装备 / {ability.AbilityId}", this);

            ToggleRequested?.Invoke(ability);
        }

        private void OnEnable() => actions.Enable();
        private void OnDisable() => actions?.Disable();
        private void OnDestroy() => actions?.Dispose();
    }
}
