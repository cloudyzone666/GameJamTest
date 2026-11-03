using System;
using System.Collections.Generic;
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
        private enum MenuCommand { Left, Right, Up, Down, Submit }
        private readonly Queue<MenuCommand> pendingCommands = new Queue<MenuCommand>();

        // A scene coordinator can resolve menu toggles before dispatching this input batch.
        public bool DeferInputUntilDispatched { get; set; }

        // The integration layer will decide whether Toggle means equip or unequip.
        public event Action<AbilityData> ToggleRequested;

        private bool CanHandleInput => isActiveAndEnabled && ui != null && ui.isActiveAndEnabled;

        private void Awake()
        {
            if (ui == null) ui = GetComponent<AbilityUI>();
            actions = new InputActionMap("AbilityEquipment");
            AddDirection("Left", "<Keyboard>/a", MenuCommand.Left);
            AddDirection("Right", "<Keyboard>/d", MenuCommand.Right);
            AddDirection("Up", "<Keyboard>/w", MenuCommand.Up);
            AddDirection("Down", "<Keyboard>/s", MenuCommand.Down);
            InputAction submit = actions.AddAction("Submit", InputActionType.Button, "<Keyboard>/space");
            submit.wantsInitialStateCheck = false;
            submit.performed += _ => ReceiveCommand(MenuCommand.Submit);
        }

        private void AddDirection(string name, string binding, MenuCommand command)
        {
            InputAction action = actions.AddAction(name, InputActionType.Button, binding);
            // Enabling the panel must not replay keys already held before it was enabled.
            action.wantsInitialStateCheck = false;
            action.performed += _ => ReceiveCommand(command);
        }

        private void ReceiveCommand(MenuCommand command)
        {
            if (!CanHandleInput) return;
            if (DeferInputUntilDispatched) pendingCommands.Enqueue(command);
            else ExecuteCommand(command);
        }

        public void DispatchPendingInput()
        {
            while (pendingCommands.Count > 0)
            {
                MenuCommand command = pendingCommands.Dequeue();
                if (CanHandleInput) ExecuteCommand(command);
            }
        }

        private void ExecuteCommand(MenuCommand command)
        {
            switch (command)
            {
                case MenuCommand.Left: ui.MoveSelection(Vector2Int.left); break;
                case MenuCommand.Right: ui.MoveSelection(Vector2Int.right); break;
                case MenuCommand.Up: ui.MoveSelection(new Vector2Int(0, -1)); break;
                case MenuCommand.Down: ui.MoveSelection(new Vector2Int(0, 1)); break;
                case MenuCommand.Submit: Submit(); break;
            }
        }

        private void Submit()
        {
            if (!CanHandleInput || ui.SelectedAbility == null) return;

            AbilityData ability = ui.SelectedAbility;
            if (logRequests)
                Debug.Log($"能力输入请求：切换装备 / {ability.AbilityId}", this);

            ToggleRequested?.Invoke(ability);
        }

        private void OnEnable() => actions.Enable();
        private void OnDisable()
        {
            actions?.Disable();
            pendingCommands.Clear();
        }
        private void OnDestroy() => actions?.Dispose();
    }
}
