using System;
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
using Object = UnityEngine.Object;

namespace GameJam2026.Abilities.Input.Tests
{
    // Exercise the wired scene through its real input, controller, manager, and views.
    // The temporary keyboard/settings never modify saved project input configuration.
    public sealed class AbilityEquipmentIntegrationTests
    {
        private const string ScenePath = "Assets/Scenes/AbilityEquipmentTest.unity";
        private Keyboard keyboard;
        private Keyboard previousKeyboard;
        private InputSettings originalInputSettings;
        private InputSettings temporaryInputSettings;
        private AbilityUI ui;
        private AbilityEquipmentController controller;
        private Color emptyPositionColor;
        private Color freeCapacityColor;

        [UnitySetUp]
        public IEnumerator LoadWiredSceneAndCreateKeyboard()
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
                ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            Assert.That(load, Is.Not.Null);
            yield return load;
            yield return null;

            ui = Object.FindObjectOfType<AbilityUI>();
            controller = Object.FindObjectOfType<AbilityEquipmentController>();
            Assert.That(ui, Is.Not.Null);
            Assert.That(controller, Is.Not.Null, "The scene must contain the wired integration component.");
            Assert.That(controller.gameObject, Is.SameAs(ui.gameObject));
            Assert.That(controller.isActiveAndEnabled, Is.True);
            Assert.That(controller.Manager, Is.Not.Null);
            Assert.That(controller.Manager.TotalCapacity, Is.EqualTo(12));
            Assert.That(controller.Manager.MaxEquippedCount, Is.EqualTo(4));

            previousKeyboard = Keyboard.current;
            keyboard = InputSystem.AddDevice<Keyboard>("AbilityEquipmentIntegrationTestKeyboard");
            SetKeys();
            Assert.That(InputState.currentUpdateType, Is.EqualTo(InputUpdateType.Dynamic));

            emptyPositionColor = FindNode("EquipmentPositions").GetChild(0).GetComponent<Image>().color;
            freeCapacityColor = FindNode("CapacityCells").GetChild(0).GetComponent<Image>().color;
            AssertState(0, 0);
        }

        [UnityTearDown]
        public IEnumerator RemoveKeyboardRestoreSettingsAndLeavePlayMode()
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

            if (Application.isPlaying)
                yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator Space_EquipsThenUnequips_AndRefreshesAllEquipmentDisplays()
        {
            ui.SelectSlot(1, 0);
            Tap(Key.Space);
            AssertState(1, 2);
            Assert.That(FindSlot("jump.test").EquippedCount, Is.EqualTo(1));
            Assert.That(FindText("DetailStatus").text, Is.EqualTo("已装备 · ×1"));
            Assert.That(FindText("Hint").text, Does.Contain("已装备"));

            Tap(Key.Space);
            AssertState(0, 0);
            Assert.That(FindSlot("jump.test").EquippedCount, Is.Zero);
            Assert.That(FindText("DetailStatus").text, Is.EqualTo("未装备 · ×0"));
            Assert.That(FindText("Hint").text, Does.Contain("已卸载"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator SpaceAndEitherShiftSpace_ToggleWithoutAddingCopies_AndKeepOtherAbility()
        {
            ui.SelectSlot(2, 0);
            Tap(Key.Space);
            Guid dash = controller.Manager.EquippedAbilities[0].InstanceId;
            Tap(Key.A);
            foreach (Key shift in new[] { Key.LeftShift, Key.RightShift })
            {
                Tap(Key.Space);
                AssertState(2, 6);
                Assert.That(FindSlot("jump.test").EquippedCount, Is.EqualTo(1));
                Tap(shift, Key.Space);
                AssertState(1, 4);
                Assert.That(FindSlot("jump.test").EquippedCount, Is.Zero);
                Assert.That(FindText("DetailStatus").text, Is.EqualTo("未装备 · ×0"));
                Tap(shift, Key.Space);
                AssertState(2, 6);
                Assert.That(FindSlot("jump.test").EquippedCount, Is.EqualTo(1));
                Assert.That(FindText("DetailStatus").text, Is.EqualTo("已装备 · ×1"));
                Tap(Key.Space);
                AssertState(1, 4);
                Assert.That(FindSlot("jump.test").EquippedCount, Is.Zero);
                Assert.That(FindSlot("dash.test").EquippedCount, Is.EqualTo(1));
                Assert.That(ContainsInstance(dash), Is.True, "Toggling Jump must preserve the equipped Dash instance.");
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator CapacityLimit_RejectsJump_ThenRemovingDashRefundsCapacity()
        {
            SeedEquipped(FindSlot("dash.test").Ability, 3);
            Guid removedId = controller.Manager.EquippedAbilities[2].InstanceId;
            ui.SelectSlot(1, 0);
            int changes = 0;
            controller.Manager.EquipmentChanged += () => changes++;
            AssertState(3, 12);
            Tap(Key.Space);
            AssertState(3, 12);
            Assert.That(changes, Is.Zero, "A rejected request must not emit a state change.");
            Assert.That(FindSlot("jump.test").EquippedCount, Is.Zero);
            Assert.That(FindText("Hint").text, Does.Contain("能力格不足"));

            Tap(Key.D);
            Tap(Key.Space);
            AssertState(2, 8);
            Assert.That(ContainsInstance(removedId), Is.False);
            Tap(Key.A);
            Tap(Key.Space);
            AssertState(3, 10);
            Assert.That(FindSlot("dash.test").EquippedCount, Is.EqualTo(2));
            Assert.That(FindSlot("jump.test").EquippedCount, Is.EqualTo(1));
            Assert.That(changes, Is.EqualTo(2));
            Assert.That(FindText("DetailStatus").text, Is.EqualTo("已装备 · ×1"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator InstanceCountLimit_RejectsDash_DespiteEnoughCapacityForItsCost()
        {
            SeedEquipped(FindSlot("jump.test").Ability, 4);
            ui.SelectSlot(2, 0);
            int changes = 0;
            controller.Manager.EquipmentChanged += () => changes++;
            var before = new List<Guid>();
            foreach (EquippedAbility instance in controller.Manager.EquippedAbilities)
                before.Add(instance.InstanceId);
            AssertState(4, 8);

            Tap(Key.Space);
            AssertState(4, 8);
            Assert.That(controller.Manager.RemainingCapacity, Is.EqualTo(4));
            Assert.That(changes, Is.Zero);
            Assert.That(FindText("Hint").text, Does.Contain("最多装备"));
            for (int index = 0; index < before.Count; index++)
                Assert.That(controller.Manager.EquippedAbilities[index].InstanceId, Is.EqualTo(before[index]));
            Assert.That(FindSlot("jump.test").EquippedCount, Is.EqualTo(4));
            Assert.That(FindSlot("dash.test").EquippedCount, Is.Zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Selection_ShowsPerAbilityCountsAndEquippedVisuals_WhileEmptyInputIsIgnored()
        {
            AbilitySlotView jump = FindSlot("jump.test");
            Color unequippedColor = jump.GetComponent<Button>().targetGraphic.color;
            ui.SelectSlot(1, 0);
            Tap(Key.Space);
            Tap(Key.D);
            Tap(Key.Space);
            AssertState(2, 6);
            Assert.That(jump.IsSelected, Is.False);
            Assert.That(jump.GetComponent<Button>().targetGraphic.color, Is.Not.EqualTo(unequippedColor),
                "An unselected equipped ability must remain visually distinct from an unequipped ability.");
            Assert.That(FindSlot("dash.test").IsSelected, Is.True);
            Assert.That(FindText("DetailStatus").text, Is.EqualTo("已装备 · ×1"));

            Tap(Key.A);
            Assert.That(FindText("DetailStatus").text, Is.EqualTo("已装备 · ×1"));
            Tap(Key.S);
            Assert.That(ui.SelectedAbility, Is.Null);
            Assert.That(FindText("DetailName").text, Is.EqualTo("未配置能力"));
            Assert.That(FindText("DetailStatus").text, Is.EqualTo("空置位置 · 无装备状态"));
            int changes = 0;
            controller.Manager.EquipmentChanged += () => changes++;
            Tap(Key.Space);
            Tap(Key.LeftShift, Key.Space);
            Tap(Key.RightShift, Key.Space);
            AssertState(2, 6);
            Assert.That(changes, Is.Zero);
            Tap(Key.W);
            Assert.That(FindText("DetailStatus").text, Is.EqualTo("已装备 · ×1"));
            Assert.That(jump.EquippedCount, Is.EqualTo(1));
            Assert.That(FindSlot("dash.test").EquippedCount, Is.EqualTo(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ControllerReenable_PreservesManagerRefreshesOfflineChanges_AndDoesNotDuplicateHandlers()
        {
            ui.SelectSlot(1, 0);
            Tap(Key.Space);
            AbilityManager manager = controller.Manager;
            AbilityData dash = FindSlot("dash.test").Ability;
            int changes = 0;
            manager.EquipmentChanged += () => changes++;

            controller.enabled = false;
            Tap(Key.Space);
            AssertState(1, 2);
            Assert.That(manager.TryEquip(dash, out EquippedAbility offlineDash), Is.EqualTo(AbilityEquipResult.Success));
            Assert.That(manager.EquippedCount, Is.EqualTo(2));
            Assert.That(FindText("EquippedCount").text, Is.EqualTo("已装备 1 / 4"));
            Assert.That(FindSlot("jump.test").EquippedCount, Is.EqualTo(1));
            controller.enabled = true;
            Assert.That(controller.Manager, Is.SameAs(manager));
            AssertState(2, 6);
            Assert.That(FindSlot("dash.test").EquippedCount, Is.EqualTo(1));
            Assert.That(FindText("DetailStatus").text, Is.EqualTo("已装备 · ×1"));

            int[] expectedCounts = { 1, 2, 1 };
            int[] expectedCosts = { 4, 6, 4 };
            for (int cycle = 0; cycle < expectedCounts.Length; cycle++)
            {
                controller.enabled = false;
                Tap(Key.Space);
                int before = manager.EquippedCount;
                controller.enabled = true;
                Assert.That(manager.EquippedCount, Is.EqualTo(before));
                Tap(Key.Space);
                AssertState(expectedCounts[cycle], expectedCosts[cycle]);
                Assert.That(changes, Is.EqualTo(cycle + 2),
                    "Each active request must mutate once after repeated controller activation.");
                Assert.That(controller.Manager, Is.SameAs(manager));
                Assert.That(ContainsInstance(offlineDash.InstanceId), Is.True);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator DisabledUI_DoesNotNavigateOrMutateEquipment_AndCanResume()
        {
            ui.SelectSlot(1, 0);
            Tap(Key.Space);
            ui.enabled = false;
            Tap(Key.Space);
            Tap(Key.LeftShift, Key.Space);
            Tap(Key.RightShift, Key.Space);
            Tap(Key.D);
            Assert.That(ui.SelectedColumn, Is.EqualTo(1));
            Assert.That(ui.SelectedRow, Is.Zero);
            AssertState(1, 2);
            ui.enabled = true;
            Tap(Key.Space);
            AssertState(0, 0);
            Assert.That(FindText("DetailStatus").text, Is.EqualTo("未装备 · ×0"));
            yield return null;
        }

        // Fixture preparation only: two configured catalog abilities cannot reach
        // both manager limits through the panel's per-ability toggle interaction.
        private void SeedEquipped(AbilityData ability, int count)
        {
            for (int index = 0; index < count; index++)
                Assert.That(controller.Manager.TryEquip(ability, out _), Is.EqualTo(AbilityEquipResult.Success));
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

        private bool ContainsInstance(Guid instanceId)
        {
            foreach (EquippedAbility instance in controller.Manager.EquippedAbilities)
                if (instance.InstanceId == instanceId) return true;
            return false;
        }

        private void AssertState(int count, int used)
        {
            Assert.That(controller.Manager.EquippedCount, Is.EqualTo(count));
            Assert.That(controller.Manager.UsedCapacity, Is.EqualTo(used));
            Assert.That(controller.Manager.RemainingCapacity, Is.EqualTo(12 - used));
            Assert.That(FindText("EquippedCount").text, Is.EqualTo($"已装备 {count} / 4"));
            Assert.That(FindText("RemainingCapacity").text, Is.EqualTo($"剩余能力格 {12 - used} / 12"));
            Assert.That(CountChangedIndicators("EquipmentPositions", emptyPositionColor), Is.EqualTo(count));
            Assert.That(CountChangedIndicators("CapacityCells", freeCapacityColor), Is.EqualTo(used));
        }

        private int CountChangedIndicators(string nodeName, Color initialColor)
        {
            int changed = 0;
            foreach (Transform indicator in FindNode(nodeName))
                if (indicator.GetComponent<Image>().color != initialColor) changed++;
            return changed;
        }

        private AbilitySlotView FindSlot(string abilityId)
        {
            foreach (AbilitySlotView slot in ui.GetComponentsInChildren<AbilitySlotView>(true))
                if (slot.Ability != null && slot.Ability.AbilityId == abilityId) return slot;
            Assert.Fail($"The scene is missing ability slot '{abilityId}'.");
            return null;
        }

        private Text FindText(string name)
        {
            foreach (Text label in ui.GetComponentsInChildren<Text>(true))
                if (label.name == name) return label;
            Assert.Fail($"The scene is missing Text '{name}'.");
            return null;
        }

        private Transform FindNode(string name)
        {
            foreach (Transform node in ui.GetComponentsInChildren<Transform>(true))
                if (node.name == name) return node;
            Assert.Fail($"The scene is missing node '{name}'.");
            return null;
        }
    }
}
