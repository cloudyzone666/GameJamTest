using System;
using System.Collections;
using GameJam2026.Abilities.Integration;
using GameJam2026.Abilities.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using AbilityInputComponent = GameJam2026.Abilities.Input.AbilityInput;
using Object = UnityEngine.Object;

namespace GameJam2026.Abilities.Input.Tests
{
    // Observe the wired scenes without referencing their Assembly-CSharp player/coordinator types.
    public sealed class GameplayAbilityMenuIntegrationTests
    {
        private const string MainScenePath = "Assets/Scenes/SampleScene.unity";
        private const string EquipmentScenePath = "Assets/Scenes/AbilityEquipmentTest.unity";
        private Keyboard keyboard;
        private Keyboard previousKeyboard;
        private InputSettings originalInputSettings;
        private InputSettings temporaryInputSettings;
        private AbilityUI ui;
        private AbilityInputComponent input;
        private AbilityEquipmentController controller;
        private Canvas canvas;
        private GraphicRaycaster raycaster;

        [UnitySetUp]
        public IEnumerator LoadMainSceneAndCreateKeyboard()
        {
            yield return new EnterPlayMode();
            originalInputSettings = InputSystem.settings;
            temporaryInputSettings = Object.Instantiate(originalInputSettings);
            temporaryInputSettings.hideFlags = HideFlags.HideAndDontSave;
            temporaryInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            temporaryInputSettings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = temporaryInputSettings;
            AsyncOperation load = EditorSceneManager.LoadSceneAsyncInPlayMode(
                MainScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            Assert.That(load, Is.Not.Null);
            while (!load.isDone) yield return null;
            yield return null;
            BindSceneObjects();

            previousKeyboard = Keyboard.current;
            keyboard = InputSystem.AddDevice<Keyboard>("GameplayAbilityMenuTestKeyboard");
            SetKeys();
            AssertMenuOpen(false);
            // Let the real Play Mode physics loop start before yielding physics
            // instructions from a test body (Test Framework 1.1.33).
            PlayerJump jump = Object.FindObjectOfType<PlayerJump>();
            Assert.That(jump, Is.Not.Null);
            float deadline = Time.realtimeSinceStartup + 5f;
            while (!jump.IsGrounded && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(Application.isPlaying, Is.True);
            Assert.That(jump.IsGrounded, Is.True, "The real player must settle before input isolation tests.");
        }

        [UnityTearDown]
        public IEnumerator RestoreKeyboardSettingsAndLeavePlayMode()
        {
            try
            {
                if (keyboard != null && keyboard.added)
                {
                    SetKeys();
                    InputSystem.RemoveDevice(keyboard);
                }
                if (previousKeyboard != null && previousKeyboard.added)
                    previousKeyboard.MakeCurrent();
            }
            finally
            {
                if (originalInputSettings != null) InputSystem.settings = originalInputSettings;
                if (temporaryInputSettings != null) Object.DestroyImmediate(temporaryInputSettings);
                keyboard = null;
                previousKeyboard = null;
                originalInputSettings = null;
                temporaryInputSettings = null;
            }

            if (Application.isPlaying) yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator MainScene_ToggleAndEscape_PreserveManagerInstancesAndOneEquipmentHandler()
        {
            AbilityManager manager = controller.Manager;
            int changes = 0;
            manager.EquipmentChanged += () => changes++;
            Tap(Key.Tab);
            AssertMenuOpen(true);
            ui.SelectSlot(1, 0);
            Tap(Key.Space);
            Assert.That(manager.EquippedCount, Is.EqualTo(1));
            Guid equippedId = manager.EquippedAbilities[0].InstanceId;

            for (int cycle = 0; cycle < 3; cycle++)
            {
                Tap(Key.Tab);
                AssertMenuOpen(false);
                // Hidden UI must not navigate or mutate its actual equipment data.
                Tap(Key.D);
                Tap(Key.Space);
                Tap(Key.LeftShift);
                Assert.That(ui.SelectedColumn, Is.EqualTo(1));
                Assert.That(manager.EquippedCount, Is.EqualTo(1));
                Tap(Key.Tab);
                AssertMenuOpen(true);
                Assert.That(controller.Manager, Is.SameAs(manager));
                Assert.That(manager.EquippedAbilities[0].InstanceId, Is.EqualTo(equippedId));
                Assert.That(FindText("EquippedCount").text, Is.EqualTo("已装备 1 / 4"));
            }

            Assert.That(changes, Is.EqualTo(1), "Panel activation must not emit equipment mutations.");
            Tap(Key.Space);
            Assert.That(manager.EquippedCount, Is.Zero);
            Assert.That(changes, Is.EqualTo(2), "One Space press must still cause exactly one mutation.");
            Tap(Key.Escape);
            AssertMenuOpen(false);
            Tap(Key.Escape);
            AssertMenuOpen(false);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EquipmentScene_StartsOpen_AndReusesItsManagerAcrossMenuToggles()
        {
            AsyncOperation load = EditorSceneManager.LoadSceneAsyncInPlayMode(
                EquipmentScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            Assert.That(load, Is.Not.Null);
            yield return load;
            yield return null;
            BindSceneObjects();
            AssertMenuOpen(true);
            AbilityManager manager = controller.Manager;
            ui.SelectSlot(2, 0);
            Tap(Key.Space);
            Guid equippedId = manager.EquippedAbilities[0].InstanceId;

            Tap(Key.Escape);
            AssertMenuOpen(false);
            Tap(Key.Space);
            Assert.That(manager.EquippedCount, Is.EqualTo(1));
            Tap(Key.Tab);
            AssertMenuOpen(true);
            Assert.That(controller.Manager, Is.SameAs(manager));
            Assert.That(manager.EquippedAbilities[0].InstanceId, Is.EqualTo(equippedId));
            Assert.That(FindText("RemainingCapacity").text, Is.EqualTo("剩余能力格 8 / 12"));
            Tap(Key.Space);
            Assert.That(manager.EquippedCount, Is.Zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MenuTransitionAndSpace_InTheSameInputBatch_DoNotLeakEquipmentCommands()
        {
            AbilityManager manager = controller.Manager;
            ui.SelectSlot(1, 0);
            Tap(Key.Tab, Key.Space);
            AssertMenuOpen(true);
            Assert.That(manager.EquippedCount, Is.Zero, "Opening with Space must not equip immediately.");
            Tap(Key.Escape, Key.Space);
            AssertMenuOpen(false);
            Assert.That(manager.EquippedCount, Is.Zero);

            Tap(Key.Tab);
            Tap(Key.Space);
            Assert.That(manager.EquippedCount, Is.EqualTo(1));
            Guid equippedId = manager.EquippedAbilities[0].InstanceId;
            Tap(Key.Escape, Key.Space);
            AssertMenuOpen(false);
            Assert.That(manager.EquippedAbilities[0].InstanceId, Is.EqualTo(equippedId),
                "Closing must discard the pending Space request before UI dispatch.");

            Tap(Key.Tab);
            Tap(Key.Tab, Key.Escape, Key.Space);
            AssertMenuOpen(false, "Escape takes precedence when both menu keys are pressed.");
            Assert.That(manager.EquippedAbilities[0].InstanceId, Is.EqualTo(equippedId));
            yield return null;
        }

        [UnityTest]
        public IEnumerator OpenMenu_StopsHorizontalControlButNotGravity_AndClosingRestoresMovement()
        {
            Rigidbody2D body = GameObject.Find("Player").GetComponent<Rigidbody2D>();
            Assert.That(body, Is.Not.Null);
            body.position = new Vector2(3f, 2f);
            body.velocity = new Vector2(5f, -1f);
            float timeScale = Time.timeScale;
            Tap(Key.Tab);
            AssertMenuOpen(true);
            Assert.That(body.velocity.x, Is.Zero.Within(0.001f));
            Assert.That(body.velocity.y, Is.EqualTo(-1f).Within(0.001f),
                "Input isolation must preserve the existing vertical velocity.");
            Assert.That(Time.timeScale, Is.EqualTo(timeScale));

            ui.SelectSlot(1, 0);
            Vector2 before = body.position;
            SetKeys(Key.D, Key.Space, Key.LeftShift);
            yield return null;
            for (int step = 0; step < 4; step++) yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(ui.SelectedColumn, Is.EqualTo(2), "Menu WASD navigation must remain available.");
            Assert.That(controller.Manager.EquippedCount, Is.EqualTo(1));
            Assert.That(body.velocity.x, Is.Zero.Within(0.001f));
            Assert.That(body.position.x, Is.EqualTo(before.x).Within(0.01f));
            Assert.That(body.position.y, Is.LessThan(before.y));
            Assert.That(body.velocity.y, Is.LessThan(-1f));

            SetKeys();
            Tap(Key.Escape);
            AssertMenuOpen(false);
            float closedX = body.position.x;
            SetKeys(Key.A);
            yield return null;
            for (int step = 0; step < 3; step++) yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(body.velocity.x, Is.EqualTo(-5f).Within(0.01f));
            Assert.That(body.position.x, Is.LessThan(closedX));
            SetKeys();
            yield return null;
            for (int step = 0; step < 2; step++) yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(body.velocity.x, Is.Zero.Within(0.001f));
        }

        [UnityTest]
        public IEnumerator SpaceHeldAcrossMenuTransitions_IsNotReplayedUntilReleasedAndPressedAgain()
        {
            AbilityManager manager = controller.Manager;
            ui.SelectSlot(1, 0);
            SetKeys(Key.Space);
            SetKeys(Key.Space, Key.Tab);
            SetKeys(Key.Space);
            yield return null;
            AssertMenuOpen(true);
            Assert.That(manager.EquippedCount, Is.Zero);

            SetKeys();
            SetKeys(Key.Space);
            Assert.That(manager.EquippedCount, Is.EqualTo(1));
            Guid equippedId = manager.EquippedAbilities[0].InstanceId;
            SetKeys(Key.Space, Key.Escape);
            SetKeys(Key.Space);
            AssertMenuOpen(false);
            SetKeys(Key.Space, Key.Tab);
            SetKeys(Key.Space);
            yield return null;
            AssertMenuOpen(true);
            Assert.That(manager.EquippedAbilities[0].InstanceId, Is.EqualTo(equippedId));
            SetKeys();
            Tap(Key.Space);
            Assert.That(manager.EquippedCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator DisablingOrDestroyingMenuHost_ReleasesPlayerControlWithoutReplacingTheSession()
        {
            Rigidbody2D body = GameObject.Find("Player").GetComponent<Rigidbody2D>();
            Assert.That(body, Is.Not.Null);
            body.position = new Vector2(3f, 2f);
            body.velocity = Vector2.zero;
            AbilityManager manager = controller.Manager;
            Tap(Key.Tab);
            ui.SelectSlot(1, 0);
            Tap(Key.Space);
            Guid equippedId = manager.EquippedAbilities[0].InstanceId;

            ui.gameObject.SetActive(false);
            InputSystem.Update();
            Assert.That(canvas.enabled, Is.False);
            Assert.That(raycaster.enabled, Is.False);
            Assert.That(input.enabled, Is.False);
            SetKeys(Key.A);
            yield return null;
            for (int step = 0; step < 3; step++) yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(body.velocity.x, Is.EqualTo(-5f).Within(0.01f),
                "An unavailable menu host must release the player input gate.");
            SetKeys();

            ui.gameObject.SetActive(true);
            InputSystem.Update();
            AssertMenuOpen(false);
            Assert.That(controller.Manager, Is.SameAs(manager));
            Assert.That(manager.EquippedAbilities[0].InstanceId, Is.EqualTo(equippedId));
            Tap(Key.Tab);
            AssertMenuOpen(true);
            Assert.That(FindText("EquippedCount").text, Is.EqualTo("已装备 1 / 4"));

            Object.Destroy(ui.gameObject);
            yield return null;
            InputSystem.Update();
            Assert.That(Object.FindObjectsOfType<AbilityEquipmentController>(true), Is.Empty);
            SetKeys(Key.A);
            yield return null;
            for (int step = 0; step < 3; step++) yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(body.velocity.x, Is.EqualTo(-5f).Within(0.01f),
                "Destroying the menu host must not permanently lock player input.");
            SetKeys();
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator DisablingEachRequiredMenuComponent_ClosesSafelyAndRetainsEquipment()
        {
            Rigidbody2D body = GameObject.Find("Player").GetComponent<Rigidbody2D>();
            AbilityManager manager = controller.Manager;
            Tap(Key.Tab);
            ui.SelectSlot(1, 0);
            Tap(Key.Space);
            Guid equippedId = manager.EquippedAbilities[0].InstanceId;

            foreach (Behaviour component in new Behaviour[] { ui, input, canvas, controller })
            {
                string componentName = component.GetType().Name;
                component.enabled = false;
                InputSystem.Update();
                Assert.That(canvas.enabled, Is.False, componentName);
                Assert.That(raycaster.enabled, Is.False, componentName);
                Assert.That(input.enabled, Is.False, componentName);
                Assert.That(controller.Manager, Is.SameAs(manager), componentName);
                Assert.That(manager.EquippedAbilities[0].InstanceId, Is.EqualTo(equippedId), componentName);

                SetKeys(Key.A);
                yield return null;
                float movementTargetTime = Time.fixedTime + 2f * Time.fixedDeltaTime;
                while (Time.fixedTime < movementTargetTime) yield return null;
                Assert.That(body.velocity.x, Is.EqualTo(-5f).Within(0.01f),
                    $"Disabling {componentName} must release player control.");
                SetKeys();
                yield return null;
                float stopTargetTime = Time.fixedTime + Time.fixedDeltaTime;
                while (Time.fixedTime < stopTargetTime) yield return null;

                component.enabled = true;
                Tap(Key.Tab);
                AssertMenuOpen(true);
                Assert.That(controller.Manager, Is.SameAs(manager), componentName);
                Assert.That(manager.EquippedAbilities[0].InstanceId, Is.EqualTo(equippedId), componentName);
                Assert.That(FindText("EquippedCount").text, Is.EqualTo("已装备 1 / 4"));
            }

            Tap(Key.Escape);
            AssertMenuOpen(false);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator MultipleKeyboardEventsInOneUpdate_ModeChangesDiscardBothSidesOfTheBatch()
        {
            Rigidbody2D body = GameObject.Find("Player").GetComponent<Rigidbody2D>();
            PlayerJump jump = body.GetComponent<PlayerJump>();
            PlayerDash dash = body.GetComponent<PlayerDash>();
            Assert.That(jump, Is.Not.Null);
            Assert.That(dash, Is.Not.Null);
            float deadline = Time.realtimeSinceStartup + 5f;
            while (!jump.IsGrounded && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(jump.IsGrounded, Is.True);

            AbilityManager manager = controller.Manager;
            ui.SelectSlot(1, 0);
            Assert.That(ui.SelectedAbility, Is.TypeOf<JumpAbilityData>());
            Assert.That(manager.TryEquip(ui.SelectedAbility, out _), Is.EqualTo(AbilityEquipResult.Success));
            ui.SelectSlot(2, 0);
            Assert.That(ui.SelectedAbility, Is.TypeOf<DashAbilityData>());
            Assert.That(manager.TryEquip(ui.SelectedAbility, out _), Is.EqualTo(AbilityEquipResult.Success));
            ui.SelectSlot(1, 0);
            Guid jumpId = manager.EquippedAbilities[0].InstanceId;
            Guid dashId = manager.EquippedAbilities[1].InstanceId;
            int changes = 0;
            manager.EquipmentChanged += () => changes++;
            float originalX = body.position.x;

            QueueBatch(
                new KeyboardState(Key.D, Key.Space, Key.LeftShift),
                new KeyboardState(),
                new KeyboardState(Key.Tab),
                new KeyboardState(Key.Tab, Key.A, Key.Space, Key.LeftShift),
                new KeyboardState(),
                new KeyboardState(Key.W, Key.Space, Key.LeftShift),
                new KeyboardState());
            AssertMenuOpen(true);
            Assert.That(ui.SelectedColumn, Is.EqualTo(1));
            Assert.That(ui.SelectedRow, Is.Zero);
            Assert.That(changes, Is.Zero);
            float targetFixedTime = Time.fixedTime + 3f * Time.fixedDeltaTime;
            while (Time.fixedTime < targetFixedTime) yield return null;
            Assert.That(body.position.x, Is.EqualTo(originalX).Within(0.01f));
            Assert.That(jump.IsJumping, Is.False);
            Assert.That(dash.IsDashing, Is.False);

            QueueBatch(
                new KeyboardState(Key.D, Key.Space, Key.LeftShift),
                new KeyboardState(),
                new KeyboardState(Key.Escape),
                new KeyboardState(Key.Escape, Key.A, Key.Space, Key.LeftShift),
                new KeyboardState(),
                new KeyboardState(Key.S, Key.Space, Key.LeftShift),
                new KeyboardState());
            AssertMenuOpen(false);
            Assert.That(ui.SelectedColumn, Is.EqualTo(1));
            Assert.That(ui.SelectedRow, Is.Zero);
            Assert.That(changes, Is.Zero, "All queued menu commands must be dropped when the batch closes the UI.");
            Assert.That(manager.EquippedAbilities[0].InstanceId, Is.EqualTo(jumpId));
            Assert.That(manager.EquippedAbilities[1].InstanceId, Is.EqualTo(dashId));
            yield return null;
            targetFixedTime = Time.fixedTime + 3f * Time.fixedDeltaTime;
            while (Time.fixedTime < targetFixedTime) yield return null;
            Assert.That(body.position.x, Is.EqualTo(originalX).Within(0.01f));
            Assert.That(body.velocity.x, Is.Zero.Within(0.001f));
            Assert.That(jump.IsJumping, Is.False, "Space from the closing batch must not become a gameplay request.");
            Assert.That(dash.IsDashing, Is.False, "Shift from the closing batch must not become a gameplay request.");
            Assert.That(jump.IsGrounded, Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CoordinatorDisableEnableCycles_ReleaseControlAndDoNotDuplicateEquipmentInput()
        {
            Behaviour coordinator = null;
            foreach (MonoBehaviour behaviour in Object.FindObjectsOfType<MonoBehaviour>(true))
                if (behaviour.GetType().Name == "GameplayAbilityCoordinator") coordinator = behaviour;
            Assert.That(coordinator, Is.Not.Null);
            Rigidbody2D body = GameObject.Find("Player").GetComponent<Rigidbody2D>();
            AbilityManager manager = controller.Manager;
            Tap(Key.Tab);
            ui.SelectSlot(1, 0);
            Tap(Key.Space);
            Guid equippedId = manager.EquippedAbilities[0].InstanceId;
            int changes = 0;
            manager.EquipmentChanged += () => changes++;

            for (int cycle = 0; cycle < 3; cycle++)
            {
                coordinator.enabled = false;
                AssertMenuOpen(false);
                SetKeys(Key.D);
                yield return null;
                float movementTargetTime = Time.fixedTime + 2f * Time.fixedDeltaTime;
                while (Time.fixedTime < movementTargetTime) yield return null;
                Assert.That(body.velocity.x, Is.EqualTo(5f).Within(0.01f));
                SetKeys();
                yield return null;
                float stopTargetTime = Time.fixedTime + Time.fixedDeltaTime;
                while (Time.fixedTime < stopTargetTime) yield return null;

                coordinator.enabled = true;
                Tap(Key.Tab);
                AssertMenuOpen(true);
                Assert.That(controller.Manager, Is.SameAs(manager));
                Assert.That(manager.EquippedAbilities[0].InstanceId, Is.EqualTo(equippedId));
                Tap(Key.Space);
                Assert.That(manager.EquippedCount, Is.Zero);
                Assert.That(changes, Is.EqualTo(cycle * 2 + 1), "One Space press must remove one instance once.");
                Tap(Key.Space);
                Assert.That(manager.EquippedCount, Is.EqualTo(1));
                Assert.That(changes, Is.EqualTo(cycle * 2 + 2), "One Space press must equip one instance once.");
                equippedId = manager.EquippedAbilities[0].InstanceId;
            }

            Tap(Key.Escape);
            AssertMenuOpen(false);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator MainScene_KeyboardEquipmentPlayUnequipmentAndReload_KeepOneConsistentSession()
        {
            Rigidbody2D body = GameObject.Find("Player").GetComponent<Rigidbody2D>();
            PlayerAbilityController reader = body.GetComponent<PlayerAbilityController>();
            PlayerJump jump = body.GetComponent<PlayerJump>();
            PlayerDash dash = body.GetComponent<PlayerDash>();
            Assert.That(reader, Is.Not.Null);
            Assert.That(jump, Is.Not.Null);
            Assert.That(dash, Is.Not.Null);
            AbilityManager manager = controller.Manager;
            Assert.That(manager.EquippedCount, Is.Zero);
            Assert.That(reader.IsBound, Is.True);
            Assert.That(reader.HasAbility(AbilityType.Jump), Is.False);
            Assert.That(reader.HasAbility(AbilityType.Dash), Is.False);

            Tap(Key.Space, Key.LeftShift);
            float targetFixedTime = Time.fixedTime + 2f * Time.fixedDeltaTime;
            float deadline = Time.realtimeSinceStartup + 5f;
            while (Time.fixedTime < targetFixedTime && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(Time.fixedTime, Is.GreaterThanOrEqualTo(targetFixedTime));
            Assert.That(jump.IsJumping, Is.False, "An empty equipment session must not grant jumping.");
            Assert.That(dash.IsDashing, Is.False, "An empty equipment session must not grant dashing.");
            Assert.That(jump.IsGrounded, Is.True);

            // Use the shipped WASD/Space menu instead of bypassing navigation or equipment commands.
            Tap(Key.Tab);
            AssertMenuOpen(true);
            Assert.That(ui.SelectedColumn, Is.Zero);
            Assert.That(ui.SelectedRow, Is.Zero);
            Tap(Key.D);
            Assert.That(ui.SelectedAbility, Is.TypeOf<JumpAbilityData>());
            Tap(Key.Space);
            Assert.That(reader.HasAbility(AbilityType.Jump), Is.True);
            Tap(Key.D);
            Assert.That(ui.SelectedAbility, Is.TypeOf<DashAbilityData>());
            Tap(Key.Space);
            Assert.That(manager.EquippedCount, Is.EqualTo(2));
            Assert.That(manager.UsedCapacity, Is.EqualTo(6));
            Assert.That(reader.GetEquippedCount(AbilityType.Jump), Is.EqualTo(1));
            Assert.That(reader.GetEquippedCount(AbilityType.Dash), Is.EqualTo(1));
            Assert.That(FindText("EquippedCount").text, Is.EqualTo("已装备 2 / 4"));
            Assert.That(FindText("RemainingCapacity").text, Is.EqualTo("剩余能力格 6 / 12"));
            Tap(Key.Escape);
            AssertMenuOpen(false);
            Assert.That(controller.Manager, Is.SameAs(manager));

            float groundY = body.position.y;
            Tap(Key.Space);
            deadline = Time.realtimeSinceStartup + 5f;
            while (!jump.IsJumping && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(jump.IsJumping, Is.True, "Space must execute the ability equipped through the menu.");
            Assert.That(body.velocity.y, Is.GreaterThan(0f));
            Assert.That(body.position.y, Is.GreaterThan(groundY));
            deadline = Time.realtimeSinceStartup + 5f;
            while ((!jump.IsGrounded || jump.IsJumping) && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(jump.IsGrounded, Is.True, "The actual player must land after its jump.");
            Assert.That(jump.IsJumping, Is.False);
            Assert.That(jump.RemainingHorizontalDistance, Is.Zero);

            float dashStartX = body.position.x;
            Tap(Key.LeftShift);
            deadline = Time.realtimeSinceStartup + 5f;
            while (!dash.IsDashing && body.position.x <= dashStartX + 0.1f
                && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(dash.IsDashing || body.position.x > dashStartX + 0.1f, Is.True,
                "Shift must execute the dash equipped through the menu.");
            deadline = Time.realtimeSinceStartup + 5f;
            while (dash.IsDashing && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(dash.IsDashing, Is.False, "The dash must release movement control.");
            Assert.That(body.position.x, Is.GreaterThan(dashStartX + 0.1f));
            Assert.That(body.velocity.x, Is.Zero.Within(0.001f));
            // The original whitebox platform is shorter than a full dash; let the player land below it.
            deadline = Time.realtimeSinceStartup + 5f;
            while (!jump.IsGrounded && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(jump.IsGrounded, Is.True);

            Tap(Key.Tab);
            AssertMenuOpen(true);
            Assert.That(ui.SelectedColumn, Is.EqualTo(2), "Reopening must retain the real menu selection.");
            Assert.That(FindText("EquippedCount").text, Is.EqualTo("已装备 2 / 4"));
            Tap(Key.Space);
            Assert.That(reader.HasAbility(AbilityType.Dash), Is.False);
            Assert.That(reader.HasAbility(AbilityType.Jump), Is.True);
            Tap(Key.A);
            Assert.That(ui.SelectedAbility, Is.TypeOf<JumpAbilityData>());
            Tap(Key.Space);
            Assert.That(manager.EquippedCount, Is.Zero);
            Assert.That(reader.HasAbility(AbilityType.Jump), Is.False);
            Assert.That(FindText("EquippedCount").text, Is.EqualTo("已装备 0 / 4"));
            Assert.That(FindText("RemainingCapacity").text, Is.EqualTo("剩余能力格 12 / 12"));
            Tap(Key.Tab);
            AssertMenuOpen(false);

            Tap(Key.Space, Key.LeftShift);
            targetFixedTime = Time.fixedTime + 2f * Time.fixedDeltaTime;
            deadline = Time.realtimeSinceStartup + 5f;
            while (Time.fixedTime < targetFixedTime && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(Time.fixedTime, Is.GreaterThanOrEqualTo(targetFixedTime));
            Assert.That(jump.IsJumping, Is.False, "Unloading the last jump instance must revoke jumping.");
            Assert.That(dash.IsDashing, Is.False, "Unloading the last dash instance must revoke dashing.");
            Assert.That(jump.IsGrounded, Is.True);
            foreach (Key direction in new[] { Key.A, Key.D })
            {
                float beforeX = body.position.x;
                SetKeys(direction);
                yield return null;
                targetFixedTime = Time.fixedTime + 2f * Time.fixedDeltaTime;
                deadline = Time.realtimeSinceStartup + 5f;
                while (Time.fixedTime < targetFixedTime && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(Time.fixedTime, Is.GreaterThanOrEqualTo(targetFixedTime));
                float sign = direction == Key.A ? -1f : 1f;
                Assert.That(body.velocity.x, Is.EqualTo(sign * 5f).Within(0.01f));
                Assert.That((body.position.x - beforeX) * sign, Is.GreaterThan(0f),
                    "Ordinary movement must remain available after unequipping both abilities.");
            }
            SetKeys();

            AsyncOperation load = EditorSceneManager.LoadSceneAsyncInPlayMode(
                MainScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            Assert.That(load, Is.Not.Null);
            deadline = Time.realtimeSinceStartup + 10f;
            while (!load.isDone && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(load.isDone, Is.True, "Reloading the original main scene must finish.");
            yield return null;
            BindSceneObjects();
            AssertMenuOpen(false);
            Assert.That(controller.Manager, Is.Not.SameAs(manager), "A scene reload starts a new equipment session.");
            Assert.That(controller.Manager.EquippedCount, Is.Zero);
            Assert.That(Object.FindObjectsOfType<PlayerAbilityController>(true), Has.Length.EqualTo(1));
            reader = Object.FindObjectOfType<PlayerAbilityController>();
            Assert.That(reader.IsBound, Is.True);
            Assert.That(reader.HasAbility(AbilityType.Jump), Is.False);
            Assert.That(reader.HasAbility(AbilityType.Dash), Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        private void QueueBatch(params KeyboardState[] states)
        {
            foreach (KeyboardState state in states) InputSystem.QueueStateEvent(keyboard, state);
            InputSystem.Update();
        }

        private void BindSceneObjects()
        {
            Assert.That(Object.FindObjectsOfType<AbilityEquipmentController>(true), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsOfType<AbilityUI>(true), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsOfType<EventSystem>(true), Has.Length.EqualTo(1));
            controller = Object.FindObjectOfType<AbilityEquipmentController>();
            ui = Object.FindObjectOfType<AbilityUI>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(ui, Is.Not.Null);
            input = ui.GetComponent<AbilityInputComponent>();
            canvas = ui.GetComponent<Canvas>();
            raycaster = ui.GetComponent<GraphicRaycaster>();
            Assert.That(controller.Manager, Is.Not.Null);
            Assert.That(input, Is.Not.Null);
            Assert.That(canvas, Is.Not.Null);
            Assert.That(raycaster, Is.Not.Null);
        }

        private void AssertMenuOpen(bool expected, string message = null)
        {
            Assert.That(canvas.enabled, Is.EqualTo(expected), message);
            Assert.That(raycaster.enabled, Is.EqualTo(expected), message);
            Assert.That(input.enabled, Is.EqualTo(expected), message);
            Assert.That(ui.isActiveAndEnabled, Is.True, "The view/manager host must stay alive while hidden.");
            Assert.That(controller.isActiveAndEnabled, Is.True);
            Assert.That(controller.gameObject.activeInHierarchy, Is.True);
        }

        private void Tap(params Key[] keys)
        {
            SetKeys(keys);
            SetKeys();
        }

        private void SetKeys(params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
            InputSystem.Update();
        }

        private Text FindText(string name)
        {
            foreach (Text label in ui.GetComponentsInChildren<Text>(true))
                if (label.name == name) return label;
            Assert.Fail($"The scene is missing Text '{name}'.");
            return null;
        }
    }
}
