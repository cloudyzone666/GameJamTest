using System;
using System.Collections;
using System.Collections.Generic;
using GameJam2026.Abilities.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace GameJam2026.Abilities.UI.Tests
{
    public sealed class AbilityUIPlayModeTests
    {
        private const string ScenePath = "Assets/Scenes/AbilityEquipmentTest.unity";

        [UnitySetUp]
        public IEnumerator LoadAbilityScene()
        {
            yield return new EnterPlayMode();
            AsyncOperation load = EditorSceneManager.LoadSceneAsyncInPlayMode(
                ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            Assert.That(load, Is.Not.Null, "Ability equipment test scene could not be loaded.");
            yield return load;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator ExitAbilityScene()
        {
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator InitialSceneBuild_HasFourColumnsSixteenSlotsAndCapacityDisplays()
        {
            AbilityUI ui = FindUI();
            Assert.That(ui.SlotCount, Is.EqualTo(16));

            AbilitySlotView[] slots = FindSlots(ui);
            Assert.That(slots, Has.Length.EqualTo(16));
            Assert.That(CountConfigured(slots), Is.EqualTo(2));
            Assert.That(CountEmpty(slots), Is.EqualTo(14));

            Transform columns = FindChild(ui.transform, "InventoryColumns");
            Transform positions = FindChild(ui.transform, "EquipmentPositions");
            Transform capacity = FindChild(ui.transform, "CapacityCells");
            Assert.That(columns.childCount, Is.EqualTo(4));
            Assert.That(positions.childCount, Is.EqualTo(4));
            Assert.That(capacity.childCount, Is.EqualTo(12));

            Assert.That(FindText(ui.transform, "EquippedCount").text, Is.EqualTo("已装备 0 / 4"));
            Assert.That(FindText(ui.transform, "RemainingCapacity").text, Is.EqualTo("剩余能力格 12 / 12"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator SelectingConfiguredSlots_UpdatesPublicSelectionAndDetails()
        {
            AbilityUI ui = FindUI();
            Assert.That(ui.SelectSlot(1, 0), Is.True);
            Assert.That(ui.SelectedAbility, Is.Not.Null);
            Assert.That(ui.SelectedAbility.AbilityId, Is.EqualTo("jump.test"));
            Assert.That(ui.SelectedColumn, Is.EqualTo(1));
            Assert.That(ui.SelectedRow, Is.EqualTo(0));
            Assert.That(FindText(ui.transform, "DetailName").text, Is.EqualTo("跳跃"));
            Assert.That(FindText(ui.transform, "DetailParameters").text,
                Does.Contain("竖直跳跃高度：2").And.Contain("水平最大移动距离：2"));
            Assert.That(ParameterLineCount(ui), Is.EqualTo(2));
            Assert.That(FindText(ui.transform, "DetailDescription").text,
                Is.EqualTo(ui.SelectedAbility.Description));
            AssertOnlySelected(ui, FindSlot(ui, "jump.test"));

            Assert.That(ui.SelectSlot(2, 0), Is.True);
            Assert.That(ui.SelectedAbility.AbilityId, Is.EqualTo("dash.test"));
            Assert.That(FindText(ui.transform, "DetailParameters").text, Does.Contain("冲刺距离：4"));
            Assert.That(ParameterLineCount(ui), Is.EqualTo(1));
            Assert.That(FindText(ui.transform, "DetailStatus").text, Does.Contain("未装备"));
            AssertOnlySelected(ui, FindSlot(ui, "dash.test"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator SelectingEmptyAndOutOfRangeSlots_IsSafeAndShowsEmptyState()
        {
            AbilityUI ui = FindUI();
            Assert.That(ui.SelectSlot(3, 3), Is.True);
            Assert.That(ui.SelectedAbility, Is.Null);
            Assert.That(FindText(ui.transform, "DetailName").text, Is.EqualTo("未配置能力"));
            Assert.That(FindText(ui.transform, "DetailParameters").text, Is.EqualTo("暂无能力参数"));
            Assert.That(FindText(ui.transform, "DetailStatus").text, Does.Contain("空置位置"));
            Assert.That(FindText(ui.transform, "DetailDescription").text, Does.Contain("此位置尚未配置能力"));

            Assert.That(ui.SelectSlot(-1, 0), Is.False);
            Assert.That(ui.SelectSlot(4, 0), Is.False);
            Assert.That(ui.SelectSlot(0, 4), Is.False);
            Assert.That(ui.SelectedColumn, Is.EqualTo(3));
            Assert.That(ui.SelectedRow, Is.EqualTo(3));
            yield return null;
        }

        [UnityTest]
        public IEnumerator SlotButtons_InvokeTheConfiguredSelectionCallback()
        {
            AbilityUI ui = FindUI();
            AbilitySlotView dash = FindSlot(ui, "dash.test");
            Assert.That(dash, Is.Not.Null);
            Assert.That(dash.GetComponent<Button>(), Is.Not.Null);

            dash.GetComponent<Button>().onClick.Invoke();
            Assert.That(ui.SelectedAbility, Is.SameAs(dash.Ability));
            Assert.That(ui.SelectedColumn, Is.EqualTo(2));
            Assert.That(ui.SelectedRow, Is.EqualTo(0));
            Assert.That(dash.IsSelected, Is.True);
            AssertOnlySelected(ui, dash);
            AbilitySlotView empty = FindChild(ui.transform, "Slot_3_3").GetComponent<AbilitySlotView>();
            empty.GetComponent<Button>().onClick.Invoke();
            Assert.That(ui.SelectedAbility, Is.Null);
            Assert.That(ui.SelectedColumn, Is.EqualTo(3));
            Assert.That(ui.SelectedRow, Is.EqualTo(3));
            AssertOnlySelected(ui, empty);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EventSystem_UsesInputSystemModuleAndDisablesAutomaticNavigation()
        {
            EventSystem eventSystem = UnityEngine.Object.FindObjectOfType<EventSystem>(true);
            Assert.That(eventSystem, Is.Not.Null);
            Assert.That(eventSystem.sendNavigationEvents, Is.False);
            Assert.That(eventSystem.GetComponent<InputSystemUIInputModule>(), Is.Not.Null);

            AbilityUI ui = FindUI();
            AbilitySlotView[] before = FindSlots(ui);
            for (int count = 0; count < 3; count++)
            {
                ui.gameObject.SetActive(false);
                ui.gameObject.SetActive(true);
            }
            Assert.That(FindSlots(ui), Has.Length.EqualTo(16));
            Assert.That(FindSlots(ui), Is.EqualTo(before));
            Assert.That(FindChild(ui.transform, "InventoryColumns").childCount, Is.EqualTo(4));
            Assert.That(FindChild(ui.transform, "EquipmentPositions").childCount, Is.EqualTo(4));
            Assert.That(FindChild(ui.transform, "CapacityCells").childCount, Is.EqualTo(12));
            FindSlot(ui, "jump.test").GetComponent<Button>().onClick.Invoke();
            Assert.That(ui.SelectedAbility.AbilityId, Is.EqualTo("jump.test"));
            AssertOnlySelected(ui, FindSlot(ui, "jump.test"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator OwnedFont_IsReleasedWhenAbilityUIIsDestroyed()
        {
            AbilityUI ui = FindUI();
            Font ownedFont = FindText(ui.transform, "DetailName").font;
            Assert.That(ownedFont, Is.Not.Null, "The test scene must use AbilityUI's owned Windows fallback font.");

            UnityEngine.Object.Destroy(ui.gameObject);
            yield return null;
            yield return null;
            Assert.That(ownedFont == null, Is.True, "AbilityUI did not release its owned font.");
        }

        [UnityTest]
        public IEnumerator SlotAndDetailLayout_RemainsInsideReferenceCanvasAtOutputScales()
        {
            AbilityUI ui = FindUI();
            Canvas canvas = ui.GetComponentInParent<Canvas>();
            Assert.That(canvas, Is.Not.Null);
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            Assert.That(scaler, Is.Not.Null);
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1600f, 900f)));
            RectTransform detailPanel = FindChild(ui.transform, "DetailPanel") as RectTransform;
            Assert.That(detailPanel, Is.Not.Null);

            Transform canvasTransform = canvas.transform;
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            Vector3 originalScale = canvasTransform.localScale;
            RenderMode originalRenderMode = canvas.renderMode;
            Vector2 originalSize = canvasRect.sizeDelta;
            bool originalScalerEnabled = scaler.enabled;
            try
            {
                // Exercise the fixed reference layout without changing the editor GameView.
                // Output scale only models uniform scaling, not actual screen rendering.
                scaler.enabled = false;
                canvas.renderMode = RenderMode.WorldSpace;
                canvasRect.sizeDelta = scaler.referenceResolution;
                foreach (float outputScale in new[] { 1280f / 1600f, 1920f / 1600f })
                {
                    canvasTransform.localScale = Vector3.one * outputScale;
                    Canvas.ForceUpdateCanvases();
                    AssertLayout(canvas, detailPanel, FindSlots(ui));
                }
            }
            finally
            {
                canvasRect.sizeDelta = originalSize;
                canvas.renderMode = originalRenderMode;
                canvasTransform.localScale = originalScale;
                scaler.enabled = originalScalerEnabled;
                Canvas.ForceUpdateCanvases();
            }
            yield return null;
        }

        private static AbilityUI FindUI()
        {
            AbilityUI ui = UnityEngine.Object.FindObjectOfType<AbilityUI>(true);
            Assert.That(ui, Is.Not.Null, "AbilityEquipmentTest must contain an AbilityUI component.");
            Assert.That(ui.enabled, Is.True, "AbilityUI failed to validate its scene bindings.");
            return ui;
        }

        private static AbilitySlotView[] FindSlots(AbilityUI ui)
        {
            AbilitySlotView[] slots = ui.GetComponentsInChildren<AbilitySlotView>(true);
            Array.Sort(slots, (left, right) => string.CompareOrdinal(left.name, right.name));
            return slots;
        }

        private static AbilitySlotView FindSlot(AbilityUI ui, string abilityId)
        {
            foreach (AbilitySlotView slot in FindSlots(ui))
                if (slot.Ability != null && slot.Ability.AbilityId == abilityId) return slot;
            return null;
        }

        private static int CountConfigured(IEnumerable<AbilitySlotView> slots)
        {
            int count = 0;
            foreach (AbilitySlotView slot in slots) if (slot.Ability != null) count++;
            return count;
        }

        private static int CountEmpty(IEnumerable<AbilitySlotView> slots)
        {
            int count = 0;
            foreach (AbilitySlotView slot in slots) if (slot.Ability == null) count++;
            return count;
        }

        private static int ParameterLineCount(AbilityUI ui)
        {
            return FindText(ui.transform, "DetailParameters").text.Split('\n').Length;
        }

        private static void AssertOnlySelected(AbilityUI ui, AbilitySlotView selected)
        {
            Assert.That(selected, Is.Not.Null);
            foreach (AbilitySlotView slot in FindSlots(ui))
                Assert.That(slot.IsSelected, Is.EqualTo(slot == selected), slot.name);
        }

        private static Transform FindChild(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            Assert.Fail($"Could not find child '{name}' under {root.name}.");
            return null;
        }

        private static Text FindText(Transform root, string name)
        {
            Text text = FindChild(root, name).GetComponent<Text>();
            Assert.That(text, Is.Not.Null, $"{name} must contain a Text component.");
            return text;
        }

        private static void AssertLayout(Canvas canvas, RectTransform detailPanel, AbilitySlotView[] slots)
        {
            List<Bounds> slotBounds = new List<Bounds>(slots.Length);
            foreach (AbilitySlotView slot in slots)
            {
                RectTransform rect = slot.transform as RectTransform;
                Assert.That(rect, Is.Not.Null);
                Bounds bounds = GetWorldBounds(rect);
                Assert.That(bounds.size.x, Is.GreaterThan(0f), $"{slot.name} must have a positive width.");
                Assert.That(bounds.size.y, Is.GreaterThan(0f), $"{slot.name} must have a positive height.");
                slotBounds.Add(bounds);
                Assert.That(IsInside(GetWorldBounds(canvas.GetComponent<RectTransform>()), bounds), Is.True,
                    $"{slot.name} must remain inside the Canvas.");
            }

            for (int i = 0; i < slotBounds.Count; i++)
                for (int j = i + 1; j < slotBounds.Count; j++)
                    Assert.That(Overlaps(slotBounds[i], slotBounds[j]), Is.False,
                        $"Slot {i} overlaps slot {j}.");

            Bounds detailBounds = GetWorldBounds(detailPanel);
            Assert.That(IsInside(GetWorldBounds(canvas.GetComponent<RectTransform>()), detailBounds), Is.True,
                "DetailPanel must remain inside the Canvas.");
            foreach (Bounds slotBoundsValue in slotBounds)
                Assert.That(Overlaps(slotBoundsValue, detailBounds), Is.False,
                    "An inventory slot overlaps DetailPanel.");
        }

        private static Bounds GetWorldBounds(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var bounds = new Bounds(corners[0], Vector3.zero);
            for (int index = 1; index < corners.Length; index++) bounds.Encapsulate(corners[index]);
            return bounds;
        }

        private static bool IsInside(Bounds root, Bounds bounds)
        {
            return bounds.min.x >= root.min.x - 0.01f && bounds.max.x <= root.max.x + 0.01f
                && bounds.min.y >= root.min.y - 0.01f && bounds.max.y <= root.max.y + 0.01f;
        }

        private static bool Overlaps(Bounds left, Bounds right)
        {
            return left.min.x < right.max.x && left.max.x > right.min.x
                && left.min.y < right.max.y && left.max.y > right.min.y;
        }
    }
}
