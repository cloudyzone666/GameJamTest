using System;
using System.Collections;
using System.Collections.Generic;
using GameJam2026.Abilities.Integration;
using GameJam2026.Abilities.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace GameJam2026.Abilities.Input.Tests
{
    public sealed class PlayerAbilityConnectionTests
    {
        private readonly List<Object> temporaryObjects = new List<Object>();
        private GameObject host;
        private PlayerAbilityController player;
        private AbilityManager manager;
        private AbilitySystemConfig config;
        private JumpAbilityData jump;
        private DashAbilityData dash;

        [SetUp]
        public void CreateIsolatedReaderAndEquipment()
        {
            config = CloneAsset<AbilitySystemConfig>("Assets/Data/Abilities/DefaultAbilitySystemConfig.asset");
            jump = CloneAsset<JumpAbilityData>("Assets/Data/Abilities/Jump_Test.asset");
            dash = CloneAsset<DashAbilityData>("Assets/Data/Abilities/Dash_Test.asset");
            manager = new AbilityManager(config);
            host = new GameObject("PlayerAbilityConnectionTestHost");
            player = host.AddComponent<PlayerAbilityController>();
        }

        [TearDown]
        public void DestroyTemporaryObjects()
        {
            if (host != null) Object.DestroyImmediate(host);
            foreach (Object temporary in temporaryObjects)
                if (temporary != null) Object.DestroyImmediate(temporary);
            temporaryObjects.Clear();
        }

        [Test]
        public void DuplicateInstances_RemainAvailableUntilTheLastActualInstanceIsRemoved()
        {
            Assert.That(player.IsBound, Is.False);
            Assert.That(player.HasAbility(AbilityType.Jump), Is.False);
            Assert.That(player.GetEquippedCount((AbilityData)null), Is.Zero);
            player.BindManager(manager);
            Assert.That(player.IsBound, Is.True);
            Assert.That(manager.TryEquip(jump, out EquippedAbility first), Is.EqualTo(AbilityEquipResult.Success));
            Assert.That(manager.TryEquip(jump, out EquippedAbility second), Is.EqualTo(AbilityEquipResult.Success));
            Assert.That(first.InstanceId, Is.Not.EqualTo(second.InstanceId));
            Assert.That(player.GetEquippedCount(AbilityType.Jump), Is.EqualTo(2));
            Assert.That(player.GetEquippedCount(jump), Is.EqualTo(2));

            Assert.That(manager.TryUnequip(first.InstanceId, out _), Is.True);
            Assert.That(player.HasAbility(AbilityType.Jump), Is.True);
            Assert.That(player.HasAbility(jump), Is.True);
            Assert.That(player.GetEquippedCount(jump), Is.EqualTo(1));
            Assert.That(player.TryGetEquippedAbility(AbilityType.Jump, out AbilityData remaining), Is.True);
            Assert.That(remaining, Is.SameAs(jump));

            Assert.That(manager.TryUnequip(second.InstanceId, out _), Is.True);
            Assert.That(player.HasAbility(AbilityType.Jump), Is.False);
            Assert.That(player.HasAbility(jump), Is.False);
            Assert.That(player.GetEquippedCount(AbilityType.Jump), Is.Zero);
            Assert.That(player.TryGetEquippedAbility(AbilityType.Jump, out remaining), Is.False);
            Assert.That(remaining, Is.Null);
        }

        [Test]
        public void SameTypeDifferentDefinitions_QueryActualEquipmentOrderAndFallBackAfterRemoval()
        {
            JumpAbilityData alternateJump = Object.Instantiate(jump);
            temporaryObjects.Add(alternateJump);
            string originalId = jump.AbilityId;
            SerializedObject serialized = new SerializedObject(alternateJump);
            serialized.FindProperty("abilityId").stringValue = "jump.connection.alternate";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(alternateJump.TryValidate(out string error), Is.True, error);
            player.BindManager(manager);
            Assert.That(manager.TryEquip(jump, out EquippedAbility first), Is.EqualTo(AbilityEquipResult.Success));
            Assert.That(manager.TryEquip(alternateJump, out _), Is.EqualTo(AbilityEquipResult.Success));

            Assert.That(player.GetEquippedCount(AbilityType.Jump), Is.EqualTo(2));
            Assert.That(player.GetEquippedCount(jump), Is.EqualTo(1));
            Assert.That(player.GetEquippedCount(alternateJump), Is.EqualTo(1));
            Assert.That(player.TryGetEquippedAbility(AbilityType.Jump, out AbilityData chosen), Is.True);
            Assert.That(chosen, Is.SameAs(jump));
            Assert.That(manager.TryUnequip(first.InstanceId, out _), Is.True);
            Assert.That(player.TryGetEquippedAbility(AbilityType.Jump, out chosen), Is.True);
            Assert.That(chosen, Is.SameAs(alternateJump));
            Assert.That(player.HasAbility(jump), Is.False);
            Assert.That(player.HasAbility(alternateJump), Is.True);
            Assert.That(jump.AbilityId, Is.EqualTo(originalId), "Only the unsaved clone may be edited.");
        }

        [Test]
        public void RepeatedBindingAndSourceChanges_ForwardOnlyOneCurrentSourceNotification()
        {
            var otherManager = new AbilityManager(config);
            player.BindManager(manager);
            int changes = 0;
            player.AbilitiesChanged += () => changes++;
            player.BindManager(manager);
            player.BindManager(manager);
            Assert.That(changes, Is.Zero, "Binding the same source must be idempotent.");
            Assert.That(manager.TryEquip(jump, out _), Is.EqualTo(AbilityEquipResult.Success));
            Assert.That(changes, Is.EqualTo(1));

            player.BindManager(otherManager);
            Assert.That(changes, Is.EqualTo(2), "Changing source must publish its current state once.");
            Assert.That(player.HasAbility(jump), Is.False);
            Assert.That(manager.TryEquip(dash, out _), Is.EqualTo(AbilityEquipResult.Success));
            Assert.That(changes, Is.EqualTo(2), "The old source must no longer forward events.");
            Assert.That(otherManager.TryEquip(dash, out _), Is.EqualTo(AbilityEquipResult.Success));
            Assert.That(changes, Is.EqualTo(3));
            Assert.That(player.HasAbility(dash), Is.True);

            player.BindManager(null);
            Assert.That(player.IsBound, Is.False);
            Assert.That(player.HasAbility(dash), Is.False);
            Assert.That(changes, Is.EqualTo(4));
            player.BindManager(null);
            Assert.That(changes, Is.EqualTo(4));
            Assert.That(otherManager.TryEquip(jump, out _), Is.EqualTo(AbilityEquipResult.Success));
            Assert.That(changes, Is.EqualTo(4));
        }

        private T CloneAsset<T>(string path) where T : Object
        {
            T original = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(original, Is.Not.Null, path);
            T clone = Object.Instantiate(original);
            clone.hideFlags = HideFlags.HideAndDontSave;
            temporaryObjects.Add(clone);
            return clone;
        }
    }

    public sealed class PlayerAbilitySceneConnectionTests
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private Keyboard keyboard;
        private Keyboard previousKeyboard;
        private InputSettings originalInputSettings;
        private InputSettings temporaryInputSettings;
        private PlayerAbilityController player;
        private AbilityEquipmentController owner;
        private AbilityUI ui;

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
                ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            Assert.That(load, Is.Not.Null);
            yield return load;
            yield return null;
            CaptureSceneObjects();

            previousKeyboard = Keyboard.current;
            keyboard = InputSystem.AddDevice<Keyboard>("PlayerAbilityConnectionTestKeyboard");
            SetKeys();
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
        public IEnumerator EquipmentUiAndClosedMenu_QueryTheOwnersActualEquipment()
        {
            Assert.That(player.HasAbility(AbilityType.Jump), Is.False);
            Assert.That(player.HasAbility(AbilityType.Dash), Is.False);
            Tap(Key.Tab);
            ui.SelectSlot(1, 0);
            AbilityData jump = ui.SelectedAbility;
            Tap(Key.Space);
            Assert.That(player.HasAbility(jump), Is.True);
            Assert.That(player.GetEquippedCount(AbilityType.Jump), Is.EqualTo(1));
            Assert.That(player.TryGetEquippedAbility(AbilityType.Jump, out AbilityData queried), Is.True);
            Assert.That(queried, Is.SameAs(owner.Manager.EquippedAbilities[0].Ability));

            Tap(Key.Escape);
            Assert.That(ui.GetComponent<Canvas>().enabled, Is.False);
            Assert.That(player.HasAbility(jump), Is.True);
            Assert.That(owner.Manager.TryUnequipLast(jump, out _), Is.True);
            Assert.That(player.HasAbility(jump), Is.False,
                "The query must follow real data even while the menu is closed.");
            Tap(Key.Tab);
            ui.SelectSlot(2, 0);
            AbilityData dash = ui.SelectedAbility;
            Tap(Key.Space);
            Assert.That(player.HasAbility(AbilityType.Dash), Is.True);
            Assert.That(player.GetEquippedCount(dash), Is.EqualTo(owner.Manager.GetEquippedCount(dash)));
            Tap(Key.Space);
            Assert.That(player.HasAbility(dash), Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DisabledReader_QueriesRemainLive_AndReenableRefreshesWithoutDuplicateSubscriptions()
        {
            ui.SelectSlot(1, 0);
            AbilityData jump = ui.SelectedAbility;
            ui.SelectSlot(2, 0);
            AbilityData dash = ui.SelectedAbility;
            int changes = 0;
            player.AbilitiesChanged += () => changes++;
            player.enabled = false;
            Assert.That(owner.Manager.TryEquip(jump, out _), Is.EqualTo(AbilityEquipResult.Success));
            Assert.That(player.IsBound, Is.True);
            Assert.That(player.HasAbility(jump), Is.True);
            Assert.That(changes, Is.Zero);

            player.enabled = true;
            Assert.That(changes, Is.EqualTo(1), "Reactivation must refresh its current state once.");
            Assert.That(owner.Manager.TryEquip(dash, out _), Is.EqualTo(AbilityEquipResult.Success));
            Assert.That(changes, Is.EqualTo(2));
            player.BindManager(owner.Manager);
            Assert.That(changes, Is.EqualTo(2));
            player.enabled = false;
            player.enabled = true;
            Assert.That(changes, Is.EqualTo(3));
            Assert.That(owner.Manager.TryUnequipLast(jump, out _), Is.True);
            Assert.That(changes, Is.EqualTo(4));
            Assert.That(player.HasAbility(jump), Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MenuHostDisablePreservesBinding_ButSessionOwnerDestructionClearsIt()
        {
            AbilityManager manager = owner.Manager;
            Tap(Key.Tab);
            ui.SelectSlot(1, 0);
            AbilityData jump = ui.SelectedAbility;
            Tap(Key.Space);
            Guid equippedId = manager.EquippedAbilities[0].InstanceId;
            ui.gameObject.SetActive(false);
            InputSystem.Update();
            Assert.That(player.IsBound, Is.True);
            Assert.That(player.HasAbility(jump), Is.True);
            ui.gameObject.SetActive(true);
            InputSystem.Update();
            Assert.That(owner.Manager, Is.SameAs(manager));
            Assert.That(manager.EquippedAbilities[0].InstanceId, Is.EqualTo(equippedId));
            Assert.That(player.HasAbility(jump), Is.True);

            int changes = 0;
            player.AbilitiesChanged += () => changes++;
            Object.Destroy(owner.gameObject);
            yield return null;
            Assert.That(player.IsBound, Is.False);
            Assert.That(player.HasAbility(jump), Is.False);
            Assert.That(changes, Is.EqualTo(1), "Session end must clear the source exactly once.");
            InputSystem.Update();
            Assert.That(changes, Is.EqualTo(1));
            Assert.That(manager.TryUnequipLast(jump, out _), Is.True);
            Assert.That(changes, Is.EqualTo(1), "A discarded session must no longer reach the player.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SingleSceneReload_CreatesOneNewSessionAndBindsOnlyTheNewPlayer()
        {
            AbilityManager previousManager = owner.Manager;
            PlayerAbilityController previousPlayer = player;
            ui.SelectSlot(1, 0);
            AbilityData jump = ui.SelectedAbility;
            Assert.That(previousManager.TryEquip(jump, out _), Is.EqualTo(AbilityEquipResult.Success));
            Assert.That(player.HasAbility(jump), Is.True);

            AsyncOperation load = EditorSceneManager.LoadSceneAsyncInPlayMode(
                ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            Assert.That(load, Is.Not.Null);
            yield return load;
            yield return null;
            CaptureSceneObjects();
            Assert.That(previousPlayer == null, Is.True);
            Assert.That(owner.Manager, Is.Not.SameAs(previousManager));
            Assert.That(player.HasAbility(AbilityType.Jump), Is.False,
                "This stage does not add cross-scene equipment persistence.");
            int changes = 0;
            player.AbilitiesChanged += () => changes++;
            Assert.That(previousManager.TryUnequipLast(jump, out _), Is.True);
            Assert.That(changes, Is.Zero);
            Assert.That(owner.Manager.TryEquip(jump, out _), Is.EqualTo(AbilityEquipResult.Success));
            Assert.That(changes, Is.EqualTo(1));
            Assert.That(player.HasAbility(jump), Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        private void CaptureSceneObjects()
        {
            Assert.That(Object.FindObjectsOfType<AbilityEquipmentController>(true), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsOfType<PlayerAbilityController>(true), Has.Length.EqualTo(1));
            owner = Object.FindObjectOfType<AbilityEquipmentController>();
            player = Object.FindObjectOfType<PlayerAbilityController>();
            ui = Object.FindObjectOfType<AbilityUI>();
            Assert.That(owner, Is.Not.Null);
            Assert.That(owner.Manager, Is.Not.Null);
            Assert.That(player, Is.Not.Null);
            Assert.That(player.gameObject.name, Is.EqualTo("Player"));
            Assert.That(player.IsBound, Is.True);
            Assert.That(ui, Is.Not.Null);
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
    }
}
