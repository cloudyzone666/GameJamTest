using System.Collections;
using System.Collections.Generic;
using GameJam2026.Abilities.Integration;
using GameJam2026.Abilities.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using AbilityInputComponent = GameJam2026.Abilities.Input.AbilityInput;

namespace GameJam2026.Abilities.Input.Tests
{
    // Editor-only tests enter Play Mode explicitly so no scene is added to Build Settings.
    public sealed class AbilityInputTests
    {
        private const string ScenePath = "Assets/Scenes/AbilityEquipmentTest.unity";
        private Keyboard keyboard;
        private Keyboard previousKeyboard;
        private AbilityUI ui;
        private AbilityInputComponent input;
        private InputSettings originalInputSettings;
        private InputSettings temporaryInputSettings;

        [UnitySetUp]
        public IEnumerator LoadSceneAndCreateKeyboard()
        {
            yield return new EnterPlayMode();
            // Keep batch-mode Editor focus from routing synthetic keys to Editor-only buffers.
            // Mutate an unsaved clone and restore the exact original settings object in teardown.
            originalInputSettings = InputSystem.settings;
            temporaryInputSettings = Object.Instantiate(originalInputSettings);
            temporaryInputSettings.hideFlags = HideFlags.HideAndDontSave;
            temporaryInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            temporaryInputSettings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = temporaryInputSettings;

            AsyncOperation load = EditorSceneManager.LoadSceneAsyncInPlayMode(
                ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            Assert.That(load, Is.Not.Null);
            yield return load;
            yield return null;

            ui = Object.FindObjectOfType<AbilityUI>();
            input = Object.FindObjectOfType<AbilityInputComponent>();
            Assert.That(ui, Is.Not.Null, "The test scene must contain AbilityUI.");
            Assert.That(input, Is.Not.Null, "The test scene must contain the wired AbilityInput component.");
            Assert.That(input.gameObject, Is.SameAs(ui.gameObject));
            Assert.That(input.isActiveAndEnabled, Is.True);

            // This fixture verifies input requests in isolation. End-to-end equipment
            // behavior is covered separately by AbilityEquipmentIntegrationTests.
            AbilityEquipmentController controller = Object.FindObjectOfType<AbilityEquipmentController>();
            Assert.That(controller, Is.Not.Null, "The test scene must contain the equipment controller.");
            controller.enabled = false;

            previousKeyboard = Keyboard.current;
            keyboard = InputSystem.AddDevice<Keyboard>("AbilityInputTestKeyboard");
            SetKeys();
            Assert.That(InputState.currentUpdateType, Is.EqualTo(InputUpdateType.Dynamic),
                "The input fixture must process virtual state in player updates before the first key.");
        }

        [UnityTearDown]
        public IEnumerator RemoveKeyboardAndLeavePlayMode()
        {
            try
            {
                // Release and remove only our device while the temporary player-input routing is active.
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
                // Restore before destroying the clone; the original resource is never edited or saved.
                if (originalInputSettings != null) InputSystem.settings = originalInputSettings;
                if (temporaryInputSettings != null) Object.DestroyImmediate(temporaryInputSettings);
                keyboard = null;
                previousKeyboard = null;
                originalInputSettings = null;
                temporaryInputSettings = null;
            }

            if (Application.isPlaying)
                yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator WasdKeys_MoveOncePerPress_KeepEdgesAndAllowEmptySlots()
        {
            Assert.That(ui.SelectSlot(1, 1), Is.True);
            Tap(Key.D);
            AssertSelection(2, 1);
            Tap(Key.A);
            AssertSelection(1, 1);
            Tap(Key.W);
            AssertSelection(1, 0);
            Tap(Key.S);
            AssertSelection(1, 1);
            Assert.That(ui.SelectedAbility, Is.Null);

            SetKeys(Key.D);
            AssertSelection(2, 1);
            InputSystem.Update();
            yield return null;
            SetKeys(Key.D);
            AssertSelection(2, 1);
            SetKeys();

            ui.SelectSlot(0, 0);
            Tap(Key.A);
            Tap(Key.W);
            AssertSelection(0, 0);
            ui.SelectSlot(3, 3);
            Tap(Key.D);
            Tap(Key.S);
            AssertSelection(3, 3);
        }

        [UnityTest]
        public IEnumerator Space_RequestsOnlyToggle_AndDoesNotRepeatWhileHeld()
        {
            ui.SelectSlot(1, 0);
            var toggles = new List<AbilityData>();
            input.ToggleRequested += toggles.Add;

            SetKeys(Key.Space);
            Assert.That(toggles, Is.EqualTo(new[] { ui.SelectedAbility }));
            InputSystem.Update();
            yield return null;
            yield return null;
            SetKeys(Key.Space);
            Assert.That(toggles, Has.Count.EqualTo(1));

            SetKeys();
            Tap(Key.Space);
            Assert.That(toggles, Has.Count.EqualTo(2));
            AssertEquipmentDisplayUnchanged();
        }

        [UnityTest]
        public IEnumerator EitherShiftPlusSpace_RequestsOneTogglePerPress()
        {
            ui.SelectSlot(2, 0);
            var toggles = new List<AbilityData>();
            input.ToggleRequested += toggles.Add;

            int expectedCount = 0;
            foreach (Key shift in new[] { Key.LeftShift, Key.RightShift })
            {
                SetKeys(shift, Key.Space);
                expectedCount++;
                Assert.That(toggles, Has.Count.EqualTo(expectedCount));
                InputSystem.Update();
                SetKeys(shift, Key.Space);
                Assert.That(toggles, Has.Count.EqualTo(expectedCount));
                SetKeys();
            }

            Assert.That(toggles, Is.EqualTo(new[] { ui.SelectedAbility, ui.SelectedAbility }));
            AssertEquipmentDisplayUnchanged();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ShiftAloneOrPressedAfterSpace_DoesNotCreateAnotherRequest()
        {
            ui.SelectSlot(1, 0);
            int toggles = 0;
            input.ToggleRequested += _ => toggles++;

            Tap(Key.LeftShift);
            Tap(Key.RightShift);
            Assert.That(toggles, Is.Zero);

            SetKeys(Key.Space);
            SetKeys(Key.Space, Key.LeftShift);
            SetKeys(Key.Space, Key.RightShift);
            InputSystem.Update();
            Assert.That(toggles, Is.EqualTo(1));

            SetKeys(Key.RightShift);
            SetKeys(Key.RightShift, Key.Space);
            Assert.That(toggles, Is.EqualTo(2));
            SetKeys();
            yield return null;
        }

        [UnityTest]
        public IEnumerator EmptySlot_IgnoresSpaceWithOrWithoutShift()
        {
            ui.SelectSlot(3, 3);
            int requests = 0;
            input.ToggleRequested += _ => requests++;
            Tap(Key.Space);
            SetKeys(Key.LeftShift, Key.Space);
            SetKeys();
            SetKeys(Key.RightShift, Key.Space);
            SetKeys();

            Assert.That(requests, Is.Zero);
            AssertSelection(3, 3);
            Assert.That(ui.SelectedAbility, Is.Null);
            Assert.That(FindText("DetailName").text, Is.EqualTo("未配置能力"));
            AssertEquipmentDisplayUnchanged();
            yield return null;
        }

        [UnityTest]
        public IEnumerator DisableAndReenable_IgnoresHeldKeysAndKeepsOneActiveHandler()
        {
            ui.SelectSlot(1, 0);
            int toggles = 0;
            input.ToggleRequested += _ => toggles++;

            input.enabled = false;
            SetKeys(Key.D, Key.Space);
            AssertSelection(1, 0);
            Assert.That(toggles, Is.Zero);
            input.enabled = true;
            InputSystem.Update();
            yield return null;
            AssertSelection(1, 0);
            Assert.That(toggles, Is.Zero);
            SetKeys();

            for (int cycle = 0; cycle < 3; cycle++)
            {
                input.enabled = false;
                Tap(Key.D);
                Tap(Key.Space);
                AssertSelection(1, 0);
                Assert.That(toggles, Is.EqualTo(cycle));
                input.enabled = true;
                Tap(Key.D);
                AssertSelection(2, 0);
                Tap(Key.A);
                AssertSelection(1, 0);
                Tap(Key.Space);
                Assert.That(toggles, Is.EqualTo(cycle + 1));
            }

            ui.enabled = false;
            Tap(Key.D);
            Tap(Key.Space);
            AssertSelection(1, 0);
            Assert.That(toggles, Is.EqualTo(3));
            ui.enabled = true;

            input.enabled = false;
            SetKeys(Key.LeftShift);
            input.enabled = true;
            InputSystem.Update();
            Assert.That(toggles, Is.EqualTo(3));
            SetKeys(Key.LeftShift, Key.Space);
            Assert.That(toggles, Is.EqualTo(4));
            InputSystem.Update();
            SetKeys(Key.LeftShift, Key.Space);
            Assert.That(toggles, Is.EqualTo(4));
            SetKeys();
        }

        [UnityTest]
        public IEnumerator UnboundKeys_DoNotMoveSelectionOrRequestEquipment()
        {
            ui.SelectSlot(1, 0);
            int requests = 0;
            input.ToggleRequested += _ => requests++;
            foreach (Key key in new[]
                { Key.UpArrow, Key.LeftArrow, Key.DownArrow, Key.RightArrow, Key.Enter,
                    Key.LeftShift, Key.RightShift })
            {
                Tap(key);
                AssertSelection(1, 0);
                Assert.That(requests, Is.Zero, $"Unbound key {key} must not request equipment.");
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator ButtonSelectionAndKeyboardNavigation_ShareSelectionWithoutChangingEquipment()
        {
            AbilitySlotView dash = null;
            foreach (AbilitySlotView slot in ui.GetComponentsInChildren<AbilitySlotView>())
                if (slot.Ability != null && slot.Ability.AbilityId == "dash.test") dash = slot;
            Assert.That(dash, Is.Not.Null);
            dash.GetComponent<Button>().onClick.Invoke();
            AssertSelection(2, 0);

            Tap(Key.A);
            AssertSelection(1, 0);
            Assert.That(ui.SelectedAbility.AbilityId, Is.EqualTo("jump.test"));
            Tap(Key.S);
            AssertSelection(1, 1);
            Assert.That(ui.SelectedAbility, Is.Null);
            Tap(Key.W);
            AssertSelection(1, 0);

            int requests = 0;
            input.ToggleRequested += _ => requests++;
            Tap(Key.Space);
            SetKeys(Key.RightShift, Key.Space);
            SetKeys();
            Assert.That(requests, Is.EqualTo(2));
            AssertEquipmentDisplayUnchanged();
            Assert.That(FindText("DetailStatus").text, Is.EqualTo("未装备 · ×0"));
            yield return null;
        }

        private void Tap(Key key)
        {
            SetKeys(key);
            SetKeys();
        }

        private void SetKeys(params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
            InputSystem.Update();
        }

        private void AssertSelection(int column, int row)
        {
            Assert.That(ui.SelectedColumn, Is.EqualTo(column));
            Assert.That(ui.SelectedRow, Is.EqualTo(row));
            int selectedCount = 0;
            foreach (AbilitySlotView slot in ui.GetComponentsInChildren<AbilitySlotView>())
                if (slot.IsSelected) selectedCount++;
            Assert.That(selectedCount, Is.EqualTo(1));
        }

        private void AssertEquipmentDisplayUnchanged()
        {
            Assert.That(FindText("EquippedCount").text, Is.EqualTo("已装备 0 / 4"));
            Assert.That(FindText("RemainingCapacity").text, Is.EqualTo("剩余能力格 12 / 12"));
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
