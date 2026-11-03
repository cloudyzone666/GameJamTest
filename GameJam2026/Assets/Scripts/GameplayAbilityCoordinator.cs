using GameJam2026.Abilities;
using GameJam2026.Abilities.Input;
using GameJam2026.Abilities.Integration;
using GameJam2026.Abilities.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum GameplayInputState { Gameplay, AbilityMenu }

// Scene wiring and input mode only. Equipment remains owned by the existing controller.
[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public sealed class GameplayAbilityCoordinator : MonoBehaviour
{
    [SerializeField] private Canvas abilityCanvas;
    [SerializeField] private AbilityUI abilityUI;
    [SerializeField] private AbilityInput abilityInput;
    [SerializeField] private AbilityEquipmentController equipmentController;
    [SerializeField] private PlayerController2D player;
    [SerializeField] private PlayerAbilityController playerAbilities;
    [SerializeField] private bool startWithMenuOpen;

    private GraphicRaycaster raycaster;
    private InputActionMap menuActions;
    private bool toggleRequested;
    private bool closeRequested;
    private bool initialized;
    private AbilityEquipmentController boundEquipmentOwner;

    public GameplayInputState State { get; private set; } = GameplayInputState.Gameplay;

    private void Awake()
    {
        if (abilityCanvas == null || abilityUI == null || abilityInput == null
            || equipmentController == null)
        {
            Debug.LogError("GameplayAbilityCoordinator: Canvas, UI, Input and equipment controller are required.", this);
            enabled = false;
            return;
        }

        if (player != null && (playerAbilities == null || playerAbilities.gameObject != player.gameObject))
        {
            Debug.LogError("GameplayAbilityCoordinator: Player Abilities must reference the existing Player.", this);
            enabled = false;
            return;
        }

        raycaster = abilityCanvas.GetComponent<GraphicRaycaster>();
        abilityInput.DeferInputUntilDispatched = true;
        abilityInput.enabled = false;
        abilityCanvas.enabled = false;
        if (raycaster != null) raycaster.enabled = false;
        if (player != null) player.SetGameplayInputEnabled(false);

        menuActions = new InputActionMap("AbilityMenuMode");
        InputAction toggle = menuActions.AddAction("Toggle", InputActionType.Button, "<Keyboard>/tab");
        InputAction close = menuActions.AddAction("Close", InputActionType.Button, "<Keyboard>/escape");
        toggle.wantsInitialStateCheck = false;
        close.wantsInitialStateCheck = false;
        toggle.performed += _ => toggleRequested = true;
        close.performed += _ => closeRequested = true;
    }

    private void OnEnable()
    {
        menuActions?.Enable();
        InputSystem.onAfterUpdate += AfterInputUpdate;
        if (initialized) BindPlayerAbilities();
    }

    private void Start()
    {
        // All scene Awakes, including the equipment session and UI construction, have finished.
        BindPlayerAbilities();
        initialized = true;
        SetMenuOpen(startWithMenuOpen);
    }

    private void BindPlayerAbilities()
    {
        if (boundEquipmentOwner != null) boundEquipmentOwner.SessionEnded -= OnEquipmentSessionEnded;
        boundEquipmentOwner = equipmentController;
        if (boundEquipmentOwner != null) boundEquipmentOwner.SessionEnded += OnEquipmentSessionEnded;
        if (playerAbilities != null)
            playerAbilities.BindManager(boundEquipmentOwner != null ? boundEquipmentOwner.Manager : null);
    }

    private void OnEquipmentSessionEnded()
    {
        if (boundEquipmentOwner != null) boundEquipmentOwner.SessionEnded -= OnEquipmentSessionEnded;
        boundEquipmentOwner = null;
        if (playerAbilities != null) playerAbilities.BindManager(null);
    }

    private void AfterInputUpdate()
    {
        bool toggle = toggleRequested;
        bool close = closeRequested;
        toggleRequested = false;
        closeRequested = false;
        if (!initialized) return;

        // Process the mode first, independent of action callback ordering in this input batch.
        // Closing disables AbilityInput and drops queued Space/navigation commands.
        if (close) SetMenuOpen(false);
        else if (toggle) SetMenuOpen(State != GameplayInputState.AbilityMenu);

        // The host can fail without its GameObject being destroyed (for example,
        // disabling just the view or Canvas). Never leave an unusable menu owning input.
        if (!CanOpenMenu || (State == GameplayInputState.AbilityMenu
            && (!abilityCanvas.isActiveAndEnabled || !abilityInput.isActiveAndEnabled)))
        {
            SetMenuOpen(false);
            return;
        }
        if (State == GameplayInputState.AbilityMenu) abilityInput.DispatchPendingInput();
    }

    // Canvas/Input are deliberately disabled while closed, so only require them
    // to exist here. Their enabled state is checked separately while the menu owns input.
    private bool CanOpenMenu => abilityCanvas != null && abilityCanvas.gameObject.activeInHierarchy
        && abilityUI != null && abilityUI.isActiveAndEnabled && abilityInput != null
        && equipmentController != null && equipmentController.isActiveAndEnabled
        && equipmentController.Manager != null;

    public void SetMenuOpen(bool open)
    {
        open = open && CanOpenMenu;

        if (open)
        {
            if (player != null) player.SetGameplayInputEnabled(false);
            State = GameplayInputState.AbilityMenu;
            abilityUI.RefreshEquipment(equipmentController.Manager);
            abilityCanvas.enabled = true;
            if (raycaster != null) raycaster.enabled = true;
            abilityInput.enabled = true;
        }
        else
        {
            if (abilityInput != null) abilityInput.enabled = false;
            if (raycaster != null) raycaster.enabled = false;
            if (abilityCanvas != null) abilityCanvas.enabled = false;
            State = GameplayInputState.Gameplay;
            if (player != null) player.SetGameplayInputEnabled(true);
        }
    }

    private void OnDisable()
    {
        InputSystem.onAfterUpdate -= AfterInputUpdate;
        menuActions?.Disable();
        toggleRequested = false;
        closeRequested = false;
        SetMenuOpen(false);
    }

    private void OnDestroy()
    {
        OnEquipmentSessionEnded();
        menuActions?.Dispose();
    }
}
