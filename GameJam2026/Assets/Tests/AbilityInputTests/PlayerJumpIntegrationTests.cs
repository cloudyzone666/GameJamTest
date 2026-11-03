using System.Collections;
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
    // Exercise the shipped scene, its real input routing and its Rigidbody2D simulation.
    // The test assembly deliberately does not reference the Assembly-CSharp movement class.
    public sealed class PlayerJumpIntegrationTests
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private Keyboard keyboard;
        private Keyboard previousKeyboard;
        private InputSettings originalInputSettings;
        private InputSettings temporaryInputSettings;
        private AbilityEquipmentController owner;
        private AbilityUI ui;
        private PlayerJump jump;
        private Rigidbody2D body;
        private JumpAbilityData definition;
        private JumpAbilityData temporaryDefinition;

        [UnitySetUp]
        public IEnumerator LoadSceneAndWaitForActualGround()
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
            while (!load.isDone) yield return null;
            yield return null;

            owner = Object.FindObjectOfType<AbilityEquipmentController>();
            ui = Object.FindObjectOfType<AbilityUI>();
            jump = Object.FindObjectOfType<PlayerJump>();
            Assert.That(owner, Is.Not.Null);
            Assert.That(owner.Manager, Is.Not.Null);
            Assert.That(ui, Is.Not.Null);
            Assert.That(jump, Is.Not.Null, "The main-scene Player must have its jump module wired.");
            body = jump.GetComponent<Rigidbody2D>();
            Assert.That(body, Is.Not.Null);
            definition = AssetDatabase.LoadAssetAtPath<JumpAbilityData>("Assets/Data/Abilities/Jump_Test.asset");
            Assert.That(definition, Is.Not.Null);
            previousKeyboard = Keyboard.current;
            keyboard = InputSystem.AddDevice<Keyboard>("PlayerJumpIntegrationTestKeyboard");
            SetKeys();
            // The EditMode fixture runner only accepts null here, even after
            // EnterPlayMode. Let real player frames advance until contact exists.
            float settleDeadline = Time.realtimeSinceStartup + 5f;
            while (!jump.IsGrounded && Time.realtimeSinceStartup < settleDeadline)
                yield return null;
            Assert.That(jump.IsGrounded, Is.True,
                $"Player must settle on the existing platform. Position={body.position}, " +
                $"velocity={body.velocity}, isPlaying={Application.isPlaying}, " +
                $"jumpEnabled={jump.isActiveAndEnabled}, simulated={body.simulated}.");
            Assert.That(ui.GetComponent<Canvas>().enabled, Is.False);
            Assert.That(owner.Manager.EquippedCount, Is.Zero);
        }

        [UnityTearDown]
        public IEnumerator RestoreTemporaryInputAndLeavePlayMode()
        {
            try
            {
                if (keyboard != null && keyboard.added)
                {
                    SetKeys();
                    InputSystem.RemoveDevice(keyboard);
                }
                if (previousKeyboard != null && previousKeyboard.added) previousKeyboard.MakeCurrent();
            }
            finally
            {
                if (originalInputSettings != null) InputSystem.settings = originalInputSettings;
                if (temporaryInputSettings != null) Object.DestroyImmediate(temporaryInputSettings);
                if (temporaryDefinition != null) Object.DestroyImmediate(temporaryDefinition);
                keyboard = null;
                previousKeyboard = null;
                originalInputSettings = null;
                temporaryInputSettings = null;
                temporaryDefinition = null;
            }

            if (Application.isPlaying) yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator NoEquipment_PreservesFiveUnitMovement_WithoutJumpOrDash()
        {
            float startY = body.position.y;
            float startX = body.position.x;
            SetKeys(Key.D, Key.Space, Key.LeftShift);
            yield return null;
            for (int step = 0; step < 3; step++) yield return new WaitForFixedUpdate();
            Assert.That(body.velocity.x, Is.EqualTo(5f).Within(0.01f));
            Assert.That(body.position.x, Is.GreaterThan(startX));
            Assert.That(body.position.y, Is.EqualTo(startY).Within(0.02f));
            Assert.That(jump.IsJumping, Is.False);
            SetKeys(Key.A);
            yield return null;
            for (int step = 0; step < 2; step++) yield return new WaitForFixedUpdate();
            Assert.That(body.velocity.x, Is.EqualTo(-5f).Within(0.01f));
            SetKeys();
            yield return null;
            for (int step = 0; step < 2; step++) yield return new WaitForFixedUpdate();
            Assert.That(body.velocity.x, Is.Zero.Within(0.001f));
        }

        [UnityTest]
        public IEnumerator EquippedJump_HasConfiguredHeight_RejectsAirJumps_AndWorksAfterLanding()
        {
            Equip(definition);
            float startY = body.position.y;
            float peak = startY;
            Tap(Key.Space);
            yield return new WaitForFixedUpdate();
            Assert.That(jump.IsJumping, Is.True);
            Assert.That(body.velocity.y, Is.GreaterThan(0f));
            for (int step = 0; step < 100; step++)
            {
                float previousVerticalSpeed = body.velocity.y;
                // Repeated edges during ascent must not add impulse. Stop them well
                // before landing so this test does not request a valid grounded jump.
                if (step < 20) Tap(Key.Space);
                yield return new WaitForFixedUpdate();
                peak = Mathf.Max(peak, body.position.y);
                if (!jump.IsJumping && jump.IsGrounded) break;
                if (step < 20)
                    Assert.That(body.velocity.y, Is.LessThanOrEqualTo(previousVerticalSpeed + 0.01f),
                        "Another Space edge in midair must not apply another jump impulse.");
            }

            TestContext.WriteLine($"Default jump measured height: {peak - startY:F4}, configured: {definition.VerticalHeight:F4}");
            Assert.That(peak - startY, Is.EqualTo(definition.VerticalHeight).Within(0.09f));
            Assert.That(jump.IsGrounded, Is.True);
            Assert.That(jump.IsJumping, Is.False);
            Tap(Key.Space);
            yield return new WaitForFixedUpdate();
            Assert.That(body.velocity.y, Is.GreaterThan(0f), "Actual landing must restore jump eligibility.");
        }

        [UnityTest]
        public IEnumerator UnsavedAbilityClone_ChangesPhysicalJumpHeight_WithoutEditingTheAsset()
        {
            float originalHeight = definition.VerticalHeight;
            temporaryDefinition = Object.Instantiate(definition);
            temporaryDefinition.hideFlags = HideFlags.HideAndDontSave;
            SerializedObject serialized = new SerializedObject(temporaryDefinition);
            serialized.FindProperty("verticalHeight").floatValue = 1f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Equip(temporaryDefinition);
            float startY = body.position.y;
            float peak = startY;
            Tap(Key.Space);
            for (int step = 0; step < 100; step++)
            {
                yield return new WaitForFixedUpdate();
                peak = Mathf.Max(peak, body.position.y);
                if (step > 2 && !jump.IsJumping && jump.IsGrounded) break;
            }

            TestContext.WriteLine($"Unsaved 1-unit jump measured height: {peak - startY:F4}");
            Assert.That(peak - startY, Is.EqualTo(1f).Within(0.07f));
            Assert.That(definition.VerticalHeight, Is.EqualTo(originalHeight), "The source asset must stay untouched.");
        }

        [UnityTest]
        public IEnumerator HorizontalBudget_ChargesBothDirections_StopsAtLimit_AndResetsOnlyAfterLanding()
        {
            Equip(definition);
            Tap(Key.Space);
            yield return new WaitForFixedUpdate();
            float previousX = body.position.x;
            float travelled = 0f;
            SetKeys(Key.D);
            yield return null;
            for (int step = 0; step < 7; step++)
            {
                yield return new WaitForFixedUpdate();
                travelled += Mathf.Abs(body.position.x - previousX);
                previousX = body.position.x;
            }
            float beforeReversal = jump.RemainingHorizontalDistance;
            SetKeys();
            yield return null;
            // Finish the previous direction before reversing so each sampled delta
            // measures one direction, even if this render frame contains physics.
            yield return new WaitForFixedUpdate();
            travelled += Mathf.Abs(body.position.x - previousX);
            previousX = body.position.x;
            SetKeys(Key.A);
            yield return null;
            for (int step = 0; step < 22; step++)
            {
                yield return new WaitForFixedUpdate();
                travelled += Mathf.Abs(body.position.x - previousX);
                previousX = body.position.x;
            }

            TestContext.WriteLine($"Cumulative jump horizontal travel including reversal: {travelled:F4}");
            Assert.That(jump.IsJumping, Is.True);
            Assert.That(beforeReversal, Is.GreaterThan(0f).And.LessThan(definition.MaxHorizontalDistance));
            Assert.That(jump.RemainingHorizontalDistance, Is.Zero.Within(0.005f));
            Assert.That(travelled, Is.EqualTo(definition.MaxHorizontalDistance).Within(0.025f));
            Assert.That(body.velocity.x, Is.Zero.Within(0.001f));
            SetKeys();
            for (int step = 0; step < 100 && jump.IsJumping; step++)
                yield return new WaitForFixedUpdate();
            Assert.That(jump.IsGrounded, Is.True);
            Assert.That(jump.RemainingHorizontalDistance, Is.Zero.Within(0.005f));
            Tap(Key.Space);
            yield return new WaitForFixedUpdate();
            Assert.That(jump.RemainingHorizontalDistance,
                Is.EqualTo(definition.MaxHorizontalDistance).Within(0.005f));
            SetKeys(Key.D);
            yield return null;
            for (int step = 0; step < 2; step++) yield return new WaitForFixedUpdate();
            Assert.That(body.velocity.x, Is.EqualTo(5f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator FractionalHorizontalBudget_ClipsTheFinalPhysicsStepInsteadOfOvershooting()
        {
            float originalDistance = definition.MaxHorizontalDistance;
            temporaryDefinition = Object.Instantiate(definition);
            temporaryDefinition.hideFlags = HideFlags.HideAndDontSave;
            SerializedObject serialized = new SerializedObject(temporaryDefinition);
            serialized.FindProperty("maxHorizontalDistance").floatValue = 0.23f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Equip(temporaryDefinition);
            float startX = body.position.x;
            Tap(Key.Space);
            yield return new WaitForFixedUpdate();
            SetKeys(Key.D);
            yield return null;
            for (int step = 0; step < 12; step++) yield return new WaitForFixedUpdate();

            TestContext.WriteLine($"Fractional 0.23-unit jump budget actual travel: {body.position.x - startX:F4}");
            Assert.That(jump.IsJumping, Is.True);
            Assert.That(body.position.x - startX, Is.EqualTo(0.23f).Within(0.005f),
                "At Move Speed 5 and fixedDeltaTime .02, the last .03-unit step must be clipped.");
            Assert.That(body.velocity.x, Is.Zero.Within(0.001f));
            Assert.That(jump.RemainingHorizontalDistance, Is.Zero.Within(0.005f));
            Assert.That(definition.MaxHorizontalDistance, Is.EqualTo(originalDistance));
        }

        [UnityTest]
        public IEnumerator JumpAgainstWall_ChargesOnlyActualTravel_WithoutRefillingOrBurningTheBudget()
        {
            Equip(definition);
            // Move before takeoff: an artificial teleport during a jump would itself
            // appear as completed physical travel to the normal budget calculation.
            body.position = new Vector2(5.59f, -2.38f);
            body.velocity = Vector2.zero;
            Physics2D.SyncTransforms();
            for (int step = 0; step < 8; step++) yield return new WaitForFixedUpdate();
            Assert.That(jump.IsGrounded, Is.True);
            Tap(Key.Space);
            yield return new WaitForFixedUpdate();
            Assert.That(jump.IsJumping, Is.True);
            SetKeys(Key.D);
            yield return null;
            for (int step = 0; step < 8; step++) yield return new WaitForFixedUpdate();
            float wallX = body.position.x;
            float budgetAtWall = jump.RemainingHorizontalDistance;
            Assert.That(jump.IsGrounded, Is.False, "The adjacent side wall must not reset a jump.");
            Assert.That(budgetAtWall, Is.GreaterThan(definition.MaxHorizontalDistance - 0.03f));
            for (int step = 0; step < 10; step++) yield return new WaitForFixedUpdate();

            TestContext.WriteLine($"Wall-blocked jump budget: {budgetAtWall:F4} -> {jump.RemainingHorizontalDistance:F4}");
            Assert.That(jump.IsJumping, Is.True);
            Assert.That(jump.IsGrounded, Is.False);
            Assert.That(body.position.x, Is.EqualTo(wallX).Within(0.005f));
            Assert.That(body.velocity.x, Is.Zero.Within(0.001f));
            Assert.That(jump.RemainingHorizontalDistance, Is.EqualTo(budgetAtWall).Within(0.005f),
                "Holding into a wall must not spend a requested velocity that produces no actual displacement.");
        }

        [UnityTest]
        public IEnumerator DuplicateEquipment_OneRemovalKeepsJump_LastRemovalDisablesItImmediately()
        {
            Equip(definition);
            Equip(definition);
            Assert.That(owner.Manager.EquippedCount, Is.EqualTo(2));
            Assert.That(owner.Manager.TryUnequipLast(definition, out _), Is.True);
            Tap(Key.Space);
            yield return new WaitForFixedUpdate();
            Assert.That(jump.IsJumping, Is.True);
            Assert.That(body.velocity.y, Is.GreaterThan(0f));
            Assert.That(owner.Manager.TryUnequipLast(definition, out _), Is.True);
            Assert.That(owner.Manager.EquippedCount, Is.Zero);
            Assert.That(body.velocity.y, Is.GreaterThan(0f),
                "Unequipping must not retroactively cancel an already applied physical impulse.");
            SetKeys();
            for (int step = 0; step < 100 && jump.IsJumping; step++)
                yield return new WaitForFixedUpdate();
            Assert.That(jump.IsGrounded, Is.True);
            Tap(Key.Space);
            yield return new WaitForFixedUpdate();
            Assert.That(jump.IsJumping, Is.False);
            Assert.That(body.velocity.y, Is.LessThanOrEqualTo(0.01f));
        }

        [UnityTest]
        public IEnumerator MenuAndEquipmentChanges_DoNotRefillAirBudget_AndWorldKeepsFalling()
        {
            Equip(definition);
            Tap(Key.Space);
            yield return new WaitForFixedUpdate();
            SetKeys(Key.D);
            yield return null;
            for (int step = 0; step < 5; step++) yield return new WaitForFixedUpdate();
            SetKeys();
            yield return null;
            for (int step = 0; step < 2; step++) yield return new WaitForFixedUpdate();
            float remaining = jump.RemainingHorizontalDistance;
            Assert.That(remaining, Is.GreaterThan(0f).And.LessThan(definition.MaxHorizontalDistance));
            Assert.That(owner.Manager.TryUnequipLast(definition, out _), Is.True);
            Equip(definition);
            Tap(Key.Tab);
            Assert.That(ui.GetComponent<Canvas>().enabled, Is.True);
            float openX = body.position.x;
            SetKeys(Key.D, Key.LeftShift);
            yield return null;
            for (int step = 0; step < 30; step++) yield return new WaitForFixedUpdate();
            Assert.That(body.velocity.x, Is.Zero.Within(0.001f));
            Assert.That(body.position.x, Is.EqualTo(openX).Within(0.005f));
            Assert.That(body.velocity.y, Is.LessThan(0f), "Gravity must continue while the menu is open.");
            Assert.That(jump.RemainingHorizontalDistance, Is.EqualTo(remaining).Within(0.005f));
            SetKeys();
            Tap(Key.Escape);
            Assert.That(ui.GetComponent<Canvas>().enabled, Is.False);
            Assert.That(jump.RemainingHorizontalDistance, Is.EqualTo(remaining).Within(0.005f));
            SetKeys(Key.A);
            yield return null;
            for (int step = 0; step < 2; step++) yield return new WaitForFixedUpdate();
            Assert.That(body.velocity.x, Is.EqualTo(-5f).Within(0.01f));
            Assert.That(jump.RemainingHorizontalDistance, Is.LessThan(remaining));
        }

        [UnityTest]
        public IEnumerator MenuTransitionAndHeldSpace_DoNotReplayGameplayJump()
        {
            Equip(definition);
            float startY = body.position.y;
            SetKeys(Key.Tab, Key.Space);
            SetKeys(Key.Space);
            yield return null;
            for (int step = 0; step < 2; step++) yield return new WaitForFixedUpdate();
            Assert.That(ui.GetComponent<Canvas>().enabled, Is.True);
            Assert.That(jump.IsJumping, Is.False);
            SetKeys(Key.Escape, Key.Space);
            SetKeys(Key.Space);
            yield return null;
            for (int step = 0; step < 3; step++) yield return new WaitForFixedUpdate();
            Assert.That(ui.GetComponent<Canvas>().enabled, Is.False);
            Assert.That(jump.IsJumping, Is.False);
            Assert.That(body.position.y, Is.EqualTo(startY).Within(0.02f));
            SetKeys();
            Tap(Key.Space);
            yield return new WaitForFixedUpdate();
            Assert.That(jump.IsJumping, Is.True);
            Assert.That(body.velocity.y, Is.GreaterThan(0f));
            Tap(Key.Tab);
            float ascentSpeed = body.velocity.y;
            Tap(Key.Space, Key.LeftShift);
            yield return new WaitForFixedUpdate();
            Assert.That(body.velocity.y, Is.LessThan(ascentSpeed), "Menu Space must not apply a new impulse.");
        }

        [UnityTest]
        public IEnumerator SideWallContact_DoesNotRestoreGroundedJumpEligibility()
        {
            Equip(definition);
            body.position = new Vector2(5.59f, 1.5f);
            body.velocity = Vector2.zero;
            Physics2D.SyncTransforms();
            SetKeys(Key.D);
            yield return null;
            for (int step = 0; step < 4; step++) yield return new WaitForFixedUpdate();
            Assert.That(jump.IsGrounded, Is.False, "A side-wall normal must not count as a floor.");
            float y = body.position.y;
            Tap(Key.Space);
            yield return new WaitForFixedUpdate();
            Assert.That(body.velocity.y, Is.LessThan(0f));
            Assert.That(body.position.y, Is.LessThan(y));
            Assert.That(jump.IsJumping, Is.False);
        }

        [UnityTest]
        public IEnumerator HoldingSpace_DoesNotRepeatOnLanding_AndAirPressIsNotBuffered()
        {
            Equip(definition);
            SetKeys(Key.Space);
            yield return new WaitForFixedUpdate();
            Assert.That(jump.IsJumping, Is.True);
            for (int step = 0; step < 100 && jump.IsJumping; step++)
                yield return new WaitForFixedUpdate();
            Assert.That(jump.IsGrounded, Is.True);
            float groundedY = body.position.y;
            for (int step = 0; step < 6; step++) yield return new WaitForFixedUpdate();
            Assert.That(jump.IsJumping, Is.False);
            Assert.That(body.position.y, Is.EqualTo(groundedY).Within(0.02f));

            SetKeys();
            Tap(Key.Space);
            yield return new WaitForFixedUpdate();
            Assert.That(jump.IsJumping, Is.True);
            for (int step = 0; step < 100; step++)
            {
                yield return new WaitForFixedUpdate();
                if (body.velocity.y < 0f && body.position.y < groundedY + 0.3f) break;
            }
            Assert.That(jump.IsJumping, Is.True);
            Tap(Key.Space);
            for (int step = 0; step < 100 && jump.IsJumping; step++)
                yield return new WaitForFixedUpdate();
            for (int step = 0; step < 6; step++) yield return new WaitForFixedUpdate();
            Assert.That(jump.IsGrounded, Is.True);
            Assert.That(jump.IsJumping, Is.False, "An airborne Space press must not wait for landing.");
            Assert.That(body.position.y, Is.EqualTo(groundedY).Within(0.02f));
        }

        private void Equip(JumpAbilityData ability)
        {
            Assert.That(owner.Manager.TryEquip(ability, out _), Is.EqualTo(AbilityEquipResult.Success));
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
