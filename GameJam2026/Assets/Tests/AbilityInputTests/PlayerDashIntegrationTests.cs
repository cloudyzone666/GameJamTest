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
    // Exercise the shipped scene and its actual Rigidbody2D/input integration.
    // No Assembly-CSharp reference or replacement movement controller is needed.
    public sealed class PlayerDashIntegrationTests
    {
        private Keyboard keyboard;
        private Keyboard previousKeyboard;
        private InputSettings originalInputSettings;
        private InputSettings temporaryInputSettings;
        private AbilityEquipmentController owner;
        private AbilityUI ui;
        private PlayerDash dash;
        private PlayerJump jump;
        private Rigidbody2D body;
        private DashAbilityData definition;
        private JumpAbilityData jumpDefinition;
        private DashAbilityData temporaryDefinition;

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
                "Assets/Scenes/SampleScene.unity", new LoadSceneParameters(LoadSceneMode.Single));
            Assert.That(load, Is.Not.Null);
            while (!load.isDone) yield return null;
            yield return null;

            owner = Object.FindObjectOfType<AbilityEquipmentController>();
            ui = Object.FindObjectOfType<AbilityUI>();
            dash = Object.FindObjectOfType<PlayerDash>();
            jump = Object.FindObjectOfType<PlayerJump>();
            Assert.That(owner, Is.Not.Null);
            Assert.That(owner.Manager, Is.Not.Null);
            Assert.That(ui, Is.Not.Null);
            Assert.That(dash, Is.Not.Null, "The main-scene Player must have its dash module wired.");
            Assert.That(jump, Is.Not.Null);
            body = dash.GetComponent<Rigidbody2D>();
            Assert.That(body, Is.Not.Null);
            definition = AssetDatabase.LoadAssetAtPath<DashAbilityData>("Assets/Data/Abilities/Dash_Test.asset");
            jumpDefinition = AssetDatabase.LoadAssetAtPath<JumpAbilityData>("Assets/Data/Abilities/Jump_Test.asset");
            Assert.That(definition, Is.Not.Null);
            Assert.That(jumpDefinition, Is.Not.Null);
            previousKeyboard = Keyboard.current;
            keyboard = InputSystem.AddDevice<Keyboard>("PlayerDashIntegrationTestKeyboard");
            SetKeys();
            float settleDeadline = Time.realtimeSinceStartup + 5f;
            while (!jump.IsGrounded && Time.realtimeSinceStartup < settleDeadline) yield return null;
            Assert.That(jump.IsGrounded, Is.True, $"Player did not settle: {body.position}, {body.velocity}.");
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
        public IEnumerator DefaultDash_TravelsFourWorldUnits_ThenRestoresOrdinaryMovement()
        {
            Equip(definition);
            MoveBeforeDash(new Vector2(-4f, -2.38f));
            for (int step = 0; step < 8; step++) yield return new WaitForFixedUpdate();
            Assert.That(jump.IsGrounded, Is.True);
            float startX = body.position.x;
            Tap(Key.LeftShift);
            yield return new WaitForFixedUpdate();
            Assert.That(dash.IsDashing, Is.True);
            Assert.That(dash.Speed, Is.EqualTo(definition.Distance / definition.Duration).Within(0.001f));
            Assert.That(body.velocity.x, Is.EqualTo(20f).Within(0.01f));
            for (int step = 0; step < 20 && dash.IsDashing; step++) yield return new WaitForFixedUpdate();
            TestContext.WriteLine($"Default dash measured distance: {body.position.x - startX:F4}");
            Assert.That(dash.IsDashing, Is.False);
            Assert.That(body.position.x - startX, Is.EqualTo(definition.Distance).Within(0.025f));
            Assert.That(body.velocity.x, Is.Zero.Within(0.001f));
            SetKeys(Key.A);
            yield return null;
            for (int step = 0; step < 2; step++) yield return new WaitForFixedUpdate();
            Assert.That(body.velocity.x, Is.EqualTo(-5f).Within(0.01f));
            SetKeys(Key.D);
            yield return null;
            for (int step = 0; step < 2; step++) yield return new WaitForFixedUpdate();
            Assert.That(body.velocity.x, Is.EqualTo(5f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator Direction_UsesLastFacingWithoutInput_AndCurrentInputTakesPriority()
        {
            Equip(CloneDash(0.3f, 0.06f));
            Tap(Key.LeftShift);
            yield return new WaitForFixedUpdate();
            Assert.That(body.velocity.x, Is.GreaterThan(0f), "Initial facing is right.");
            for (int step = 0; step < 8 && dash.IsDashing; step++) yield return new WaitForFixedUpdate();
            SetKeys(Key.A);
            yield return null;
            for (int step = 0; step < 2; step++) yield return new WaitForFixedUpdate();
            SetKeys();
            yield return null;
            for (int step = 0; step < 2; step++) yield return new WaitForFixedUpdate();
            float startX = body.position.x;
            Tap(Key.LeftShift);
            yield return new WaitForFixedUpdate();
            Assert.That(body.velocity.x, Is.LessThan(0f));
            for (int step = 0; step < 8 && dash.IsDashing; step++) yield return new WaitForFixedUpdate();
            Assert.That(body.position.x - startX, Is.EqualTo(-0.3f).Within(0.015f));

            SetKeys(Key.D, Key.LeftShift);
            yield return null;
            yield return new WaitForFixedUpdate();
            Assert.That(dash.IsDashing, Is.True);
            Assert.That(body.velocity.x, Is.GreaterThan(0f), "Current D overrides the prior left facing.");
        }

        [UnityTest]
        public IEnumerator UnsavedConfiguration_ControlsSpeedAndFractionalLastStep_WithoutChangingTheAsset()
        {
            float originalDistance = definition.Distance;
            float originalDuration = definition.Duration;
            Equip(CloneDash(0.23f, 0.07f));
            float startX = body.position.x;
            Tap(Key.LeftShift);
            yield return new WaitForFixedUpdate();
            Assert.That(dash.IsDashing, Is.True);
            Assert.That(dash.Speed, Is.EqualTo(0.23f / 0.07f).Within(0.001f));
            Assert.That(body.velocity.x, Is.EqualTo(0.23f / 0.07f).Within(0.001f));
            for (int step = 0; step < 12 && dash.IsDashing; step++) yield return new WaitForFixedUpdate();
            TestContext.WriteLine($"Fractional 0.23-unit / 0.07s dash distance: {body.position.x - startX:F4}");
            Assert.That(dash.IsDashing, Is.False);
            Assert.That(body.position.x - startX, Is.EqualTo(0.23f).Within(0.005f),
                "The final physics step must use only the remaining duration/distance.");
            Assert.That(body.velocity.x, Is.Zero.Within(0.001f));
            Assert.That(definition.Distance, Is.EqualTo(originalDistance));
            Assert.That(definition.Duration, Is.EqualTo(originalDuration));
        }

        [UnityTest]
        public IEnumerator ConfigurationAbovePhysicsSpeedLimit_IsRejectedWithoutSilentlyShorteningTheDash()
        {
            Equip(CloneDash(4f, 0.02f));
            float startX = body.position.x;
            LogAssert.Expect(LogType.Warning,
                "Dash speed exceeds the current Physics2D maximum translation speed. "
                + "Increase Duration or reduce Distance in the ability asset.");
            Tap(Key.LeftShift);
            yield return new WaitForFixedUpdate();
            Assert.That(dash.IsDashing, Is.False);
            Assert.That(body.velocity.x, Is.Zero.Within(0.001f));
            Assert.That(body.position.x, Is.EqualTo(startX).Within(0.005f));
        }

        [UnityTest]
        public IEnumerator WallCollision_StopsDashBeforeTheColliderCrossesTheWall()
        {
            Equip(definition);
            MoveBeforeDash(new Vector2(5.2f, -2.38f));
            for (int step = 0; step < 8; step++) yield return new WaitForFixedUpdate();
            Assert.That(jump.IsGrounded, Is.True);
            float startX = body.position.x;
            Tap(Key.LeftShift);
            yield return new WaitForFixedUpdate();
            for (int step = 0; step < 20 && dash.IsDashing; step++) yield return new WaitForFixedUpdate();
            Assert.That(dash.IsDashing, Is.False);
            Assert.That(body.position.x, Is.LessThanOrEqualTo(5.61f), "The player's right edge must not cross x=6.");
            Assert.That(body.position.x - startX, Is.GreaterThan(0.2f).And.LessThan(0.45f));
            Assert.That(body.velocity.x, Is.Zero.Within(0.001f));
            for (int step = 0; step < 6; step++) yield return new WaitForFixedUpdate();
            Assert.That(body.position.x, Is.LessThanOrEqualTo(5.61f));
        }

        [UnityTest]
        public IEnumerator AirDash_PreservesGravity_AndDoesNotSpendTheJumpHorizontalBudget()
        {
            Equip(definition);
            Equip(jumpDefinition);
            Tap(Key.Space);
            yield return new WaitForFixedUpdate();
            SetKeys(Key.D);
            yield return null;
            for (int step = 0; step < 4; step++) yield return new WaitForFixedUpdate();
            SetKeys();
            yield return null;
            for (int step = 0; step < 2; step++) yield return new WaitForFixedUpdate();
            float remaining = jump.RemainingHorizontalDistance;
            float startX = body.position.x;
            float verticalSpeed = body.velocity.y;
            Assert.That(remaining, Is.GreaterThan(0f).And.LessThan(jumpDefinition.MaxHorizontalDistance));
            Tap(Key.LeftShift);
            yield return new WaitForFixedUpdate();
            Assert.That(dash.IsDashing, Is.True);
            Assert.That(body.velocity.y, Is.LessThan(verticalSpeed), "A horizontal dash must not suspend gravity.");
            for (int step = 0; step < 20 && dash.IsDashing; step++) yield return new WaitForFixedUpdate();
            for (int step = 0; step < 2; step++) yield return new WaitForFixedUpdate();
            Assert.That(jump.IsJumping, Is.True);
            Assert.That(jump.IsGrounded, Is.False);
            Assert.That(body.position.x - startX, Is.EqualTo(definition.Distance).Within(0.025f));
            Assert.That(jump.RemainingHorizontalDistance, Is.EqualTo(remaining).Within(0.005f),
                "The dash's first, final, and completed physics steps must all be excluded from the ordinary budget.");
        }

        [UnityTest]
        public IEnumerator ExhaustedJumpBudget_StillAllowsAnAirDash_WithoutRefillingTheBudget()
        {
            Equip(definition);
            Equip(jumpDefinition);
            Tap(Key.Space);
            yield return new WaitForFixedUpdate();
            SetKeys(Key.D);
            yield return null;
            for (int step = 0; step < 24; step++) yield return new WaitForFixedUpdate();
            Assert.That(jump.IsJumping, Is.True);
            Assert.That(jump.RemainingHorizontalDistance, Is.Zero.Within(0.005f));
            Assert.That(body.velocity.x, Is.Zero.Within(0.001f));
            SetKeys();
            yield return null;
            yield return new WaitForFixedUpdate();
            float startX = body.position.x;
            SetKeys(Key.A, Key.LeftShift);
            yield return null;
            yield return new WaitForFixedUpdate();
            Assert.That(dash.IsDashing, Is.True);
            Assert.That(body.velocity.x, Is.EqualTo(-20f).Within(0.01f));
            SetKeys();
            for (int step = 0; step < 20 && dash.IsDashing; step++) yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(body.position.x - startX, Is.EqualTo(-definition.Distance).Within(0.025f));
            Assert.That(jump.IsJumping, Is.True);
            Assert.That(jump.RemainingHorizontalDistance, Is.Zero.Within(0.005f));
            Assert.That(body.velocity.x, Is.Zero.Within(0.001f));
        }

        [UnityTest]
        public IEnumerator SameBatchShiftSpace_PerformsGroundDashBeforeTheJump()
        {
            Equip(definition);
            Equip(jumpDefinition);
            MoveBeforeDash(new Vector2(-4f, -2.38f));
            for (int step = 0; step < 8; step++) yield return new WaitForFixedUpdate();
            Assert.That(jump.IsGrounded, Is.True);
            float startX = body.position.x;
            float startY = body.position.y;
            Tap(Key.Space, Key.LeftShift);
            yield return new WaitForFixedUpdate();
            Assert.That(dash.IsDashing, Is.True);
            Assert.That(jump.IsJumping, Is.False);
            for (int step = 0; step < 20 && dash.IsDashing; step++)
            {
                Assert.That(jump.IsJumping, Is.False, "Space must wait while dash owns horizontal motion.");
                yield return new WaitForFixedUpdate();
            }
            Assert.That(dash.IsDashing, Is.False);
            for (int step = 0; step < 2 && !jump.IsJumping; step++) yield return new WaitForFixedUpdate();
            Assert.That(body.position.x - startX, Is.EqualTo(definition.Distance).Within(0.025f));
            Assert.That(jump.IsJumping, Is.True);
            Assert.That(body.velocity.y, Is.GreaterThan(0f));
            Assert.That(body.position.y, Is.GreaterThan(startY));
        }

        [UnityTest]
        public IEnumerator SameBatchAirDashSpace_DropsTheDeferredJump_WhenDashEndsAirborne()
        {
            Equip(definition);
            Equip(jumpDefinition);
            MoveBeforeDash(new Vector2(-4f, 0f));
            for (int step = 0; step < 3; step++) yield return new WaitForFixedUpdate();
            Assert.That(jump.IsGrounded, Is.False);
            Tap(Key.LeftShift, Key.Space);
            yield return new WaitForFixedUpdate();
            Assert.That(dash.IsDashing, Is.True);
            for (int step = 0; step < 20 && dash.IsDashing; step++) yield return new WaitForFixedUpdate();
            Assert.That(jump.IsGrounded, Is.False);
            Assert.That(jump.IsJumping, Is.False);
            Assert.That(body.velocity.y, Is.LessThan(0f));
            for (int step = 0; step < 150 && !jump.IsGrounded; step++) yield return new WaitForFixedUpdate();
            for (int step = 0; step < 6; step++) yield return new WaitForFixedUpdate();
            Assert.That(jump.IsGrounded, Is.True);
            Assert.That(jump.IsJumping, Is.False, "An airborne dash-end must not buffer Space until landing.");
            Assert.That(body.velocity.y, Is.LessThanOrEqualTo(0.01f));
        }

        [UnityTest]
        public IEnumerator SpacePressedDuringDash_WaitsUntilTheGroundedDashEnds()
        {
            Equip(definition);
            Equip(jumpDefinition);
            MoveBeforeDash(new Vector2(-4f, -2.38f));
            for (int step = 0; step < 8; step++) yield return new WaitForFixedUpdate();
            Tap(Key.LeftShift);
            yield return new WaitForFixedUpdate();
            for (int step = 0; step < 2; step++) yield return new WaitForFixedUpdate();
            Assert.That(dash.IsDashing, Is.True);
            Tap(Key.Space);
            yield return new WaitForFixedUpdate();
            Assert.That(jump.IsJumping, Is.False);
            for (int step = 0; step < 20 && dash.IsDashing; step++) yield return new WaitForFixedUpdate();
            for (int step = 0; step < 2 && !jump.IsJumping; step++) yield return new WaitForFixedUpdate();
            Assert.That(dash.IsDashing, Is.False);
            Assert.That(jump.IsJumping, Is.True);
            Assert.That(body.velocity.y, Is.GreaterThan(0f));
        }

        [UnityTest]
        public IEnumerator DuplicateDash_LastRemovalCancelsImmediately_AndClearsDeferredJump()
        {
            Equip(definition);
            Equip(definition);
            Equip(jumpDefinition);
            MoveBeforeDash(new Vector2(-4f, -2.38f));
            for (int step = 0; step < 8; step++) yield return new WaitForFixedUpdate();
            Tap(Key.LeftShift, Key.Space);
            yield return new WaitForFixedUpdate();
            Assert.That(dash.IsDashing, Is.True);
            Assert.That(owner.Manager.TryUnequipLast(definition, out _), Is.True);
            Assert.That(dash.IsDashing, Is.True, "The remaining instance still grants Dash.");
            yield return new WaitForFixedUpdate();
            Assert.That(owner.Manager.TryUnequipLast(definition, out _), Is.True);
            Assert.That(dash.IsDashing, Is.False, "Removing the last instance must cancel immediately.");
            Assert.That(body.velocity.x, Is.Zero.Within(0.001f));
            for (int step = 0; step < 8; step++) yield return new WaitForFixedUpdate();
            Assert.That(jump.IsJumping, Is.False, "Cancelled dash must discard its delayed Space request.");
            Tap(Key.LeftShift);
            yield return new WaitForFixedUpdate();
            Assert.That(dash.IsDashing, Is.False);
            Assert.That(body.velocity.x, Is.Zero.Within(0.001f));
        }

        [UnityTest]
        public IEnumerator MenuOpening_CancelsAirDashAndPendingJump_KeepsGravityAndJumpBudget()
        {
            Equip(definition);
            Equip(jumpDefinition);
            Tap(Key.Space);
            yield return new WaitForFixedUpdate();
            SetKeys(Key.D);
            yield return null;
            for (int step = 0; step < 3; step++) yield return new WaitForFixedUpdate();
            SetKeys();
            yield return null;
            for (int step = 0; step < 2; step++) yield return new WaitForFixedUpdate();
            float remaining = jump.RemainingHorizontalDistance;
            Tap(Key.LeftShift, Key.Space);
            yield return new WaitForFixedUpdate();
            Assert.That(dash.IsDashing, Is.True);
            Tap(Key.Tab);
            Assert.That(ui.GetComponent<Canvas>().enabled, Is.True);
            Assert.That(dash.IsDashing, Is.False);
            Assert.That(body.velocity.x, Is.Zero.Within(0.001f));
            float openX = body.position.x;
            float verticalSpeed = body.velocity.y;
            SetKeys(Key.D, Key.LeftShift);
            yield return null;
            for (int step = 0; step < 5; step++) yield return new WaitForFixedUpdate();
            Assert.That(body.position.x, Is.EqualTo(openX).Within(0.005f));
            Assert.That(body.velocity.y, Is.LessThan(verticalSpeed));
            Assert.That(jump.RemainingHorizontalDistance, Is.EqualTo(remaining).Within(0.005f),
                "Cancelling a partially completed dash must not charge its last displacement as ordinary travel.");
            SetKeys(Key.Escape, Key.LeftShift);
            SetKeys(Key.LeftShift);
            yield return null;
            for (int step = 0; step < 3; step++) yield return new WaitForFixedUpdate();
            Assert.That(ui.GetComponent<Canvas>().enabled, Is.False);
            Assert.That(dash.IsDashing, Is.False, "A held menu key must not replay as a new dash on close.");
            SetKeys();
            for (int step = 0; step < 150 && !jump.IsGrounded; step++) yield return new WaitForFixedUpdate();
            for (int step = 0; step < 6; step++) yield return new WaitForFixedUpdate();
            Assert.That(jump.IsGrounded, Is.True);
            Assert.That(jump.IsJumping, Is.False, "The pre-menu delayed Space request must remain cleared.");
        }

        [UnityTest]
        public IEnumerator MenuTransitionBatchAndHeldShift_DoNotReplayADashOrGameplayJump()
        {
            Equip(definition);
            Equip(jumpDefinition);
            float startX = body.position.x;
            SetKeys(Key.Tab, Key.LeftShift, Key.Space);
            SetKeys(Key.LeftShift, Key.Space);
            yield return null;
            for (int step = 0; step < 2; step++) yield return new WaitForFixedUpdate();
            Assert.That(ui.GetComponent<Canvas>().enabled, Is.True);
            Assert.That(dash.IsDashing, Is.False);
            Assert.That(jump.IsJumping, Is.False);
            SetKeys(Key.Escape, Key.LeftShift, Key.Space);
            SetKeys(Key.LeftShift, Key.Space);
            yield return null;
            for (int step = 0; step < 3; step++) yield return new WaitForFixedUpdate();
            Assert.That(ui.GetComponent<Canvas>().enabled, Is.False);
            Assert.That(dash.IsDashing, Is.False);
            Assert.That(jump.IsJumping, Is.False);
            Assert.That(body.position.x, Is.EqualTo(startX).Within(0.005f));
            SetKeys();
            Tap(Key.LeftShift);
            yield return new WaitForFixedUpdate();
            Assert.That(dash.IsDashing, Is.True, "A fresh gameplay edge must still work after transition suppression.");
        }

        [UnityTest]
        public IEnumerator HeldOrRepeatedShift_DoesNotExtendDash_AndFreshPressNeedsNoCooldown()
        {
            Equip(definition);
            MoveBeforeDash(new Vector2(-4f, -2.38f));
            for (int step = 0; step < 8; step++) yield return new WaitForFixedUpdate();
            float startX = body.position.x;
            SetKeys(Key.LeftShift);
            yield return new WaitForFixedUpdate();
            Assert.That(dash.IsDashing, Is.True);
            for (int step = 0; step < 2; step++) yield return new WaitForFixedUpdate();
            SetKeys();
            SetKeys(Key.LeftShift);
            for (int step = 0; step < 20 && dash.IsDashing; step++) yield return new WaitForFixedUpdate();
            Assert.That(dash.IsDashing, Is.False);
            Assert.That(body.position.x - startX, Is.EqualTo(definition.Distance).Within(0.025f),
                "A new Shift edge during an active dash must not restart its duration.");
            float secondStartX = body.position.x;
            SetKeys(Key.A);
            SetKeys(Key.A, Key.LeftShift);
            yield return new WaitForFixedUpdate();
            Assert.That(dash.IsDashing, Is.True, "A fresh press on the next physics step needs no cooldown.");
            Assert.That(body.velocity.x, Is.EqualTo(-20f).Within(0.01f));
            SetKeys(Key.LeftShift);
            yield return null;
            for (int step = 0; step < 20 && dash.IsDashing; step++) yield return new WaitForFixedUpdate();
            for (int step = 0; step < 12; step++) yield return new WaitForFixedUpdate();
            Assert.That(dash.IsDashing, Is.False, "Holding Shift must not automatically start another dash.");
            Assert.That(body.position.x - secondStartX, Is.EqualTo(-definition.Distance).Within(0.025f));
        }

        private void MoveBeforeDash(Vector2 position)
        {
            SetKeys();
            body.position = position;
            body.velocity = Vector2.zero;
            Physics2D.SyncTransforms();
        }

        private DashAbilityData CloneDash(float distance, float duration)
        {
            temporaryDefinition = Object.Instantiate(definition);
            temporaryDefinition.hideFlags = HideFlags.HideAndDontSave;
            var serialized = new SerializedObject(temporaryDefinition);
            serialized.FindProperty("distance").floatValue = distance;
            serialized.FindProperty("duration").floatValue = duration;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return temporaryDefinition;
        }

        private void Equip(AbilityData ability)
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
