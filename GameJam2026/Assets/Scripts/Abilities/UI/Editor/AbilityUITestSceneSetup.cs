using System;
using System.IO;
using GameJam2026.Abilities.Input;
using GameJam2026.Abilities.Integration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GameJam2026.Abilities.UI.Editor
{
    // One-time authoring helper. The saved scene and prefab work without running this again.
    public static class AbilityUITestSceneSetup
    {
        private const string ScenePath = "Assets/Scenes/AbilityEquipmentTest.unity";
        private const string PrefabPath = "Assets/Prefabs/Abilities/AbilitySlot.prefab";
        private static readonly Color Ink = new Color32(225, 233, 242, 255);
        private static readonly Color Muted = new Color32(154, 174, 192, 255);

        public static void CreateScene()
        {
            // Never overwrite an existing scene or prefab, including edits by the user.
            if (File.Exists(ScenePath) || File.Exists(PrefabPath))
                throw new InvalidOperationException("Ability UI scene/prefab already exists; creation was cancelled.");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EnsureFolder("Assets", "Prefabs");
            EnsureFolder("Assets/Prefabs", "Abilities");
            AbilitySlotView prefab = CreateSlotPrefab();

            var canvasObject = new GameObject("AbilityCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(AbilityUI), typeof(AbilityInput),
                typeof(AbilityEquipmentController));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            RectTransform backdrop = Rect("Backdrop", canvasObject.transform, 0, 0, 0, 0);
            Stretch(backdrop);
            Panel(backdrop, new Color32(10, 16, 24, 255));
            RectTransform main = Rect("EquipmentPanel", canvasObject.transform, 0, 0, 1520, 820);
            main.anchorMin = main.anchorMax = main.pivot = new Vector2(0.5f, 0.5f);
            main.anchoredPosition = Vector2.zero;
            Panel(main, new Color32(17, 24, 34, 255));
            Label("Title", main, 32, 20, 950, 48, "能力装备", 32, Ink);
            Label("Stage", main, 1170, 32, 320, 28, "能力配置", 20, Muted);
            Text feedback = Label("Hint", main, 32, 74, 1456, 30, "能力列表", 20, Muted);
            Text equipped = Label("EquippedCount", main, 32, 124, 220, 34, "", 23, Ink);
            RectTransform positions = Rect("EquipmentPositions", main, 270, 126, 280, 28);
            Horizontal(positions, 12);
            Label("PositionsLegend", main, 580, 124, 650, 32, "暗色位置：未装备的实例位置", 19, Muted);
            Text remaining = Label("RemainingCapacity", main, 32, 175, 310, 34, "", 23, Ink);
            RectTransform cells = Rect("CapacityCells", main, 370, 180, 700, 22);
            Horizontal(cells, 8);
            Label("CapacityLegend", main, 1100, 173, 380, 38, "浅蓝：剩余  /  暗色：已用", 19, Muted);
            Panel(Rect("Divider", main, 32, 226, 1456, 2), new Color32(57, 74, 92, 255));
            RectTransform inventory = Rect("InventoryColumns", main, 32, 246, 880, 492);
            Horizontal(inventory, 14);
            RectTransform details = Rect("DetailPanel", main, 946, 246, 542, 500);
            Panel(details, new Color32(24, 33, 45, 255));
            RectTransform iconBox = Rect("DetailIconBox", details, 22, 22, 74, 74);
            Panel(iconBox, new Color32(45, 57, 71, 255));
            Image icon = Panel(Rect("DetailIcon", iconBox, 7, 7, 60, 60), Color.white);
            icon.preserveAspect = true;
            Text fallback = Label("DetailIconFallback", iconBox, 0, 0, 74, 74, "", 20, Muted);
            fallback.alignment = TextAnchor.MiddleCenter;
            Text name = Label("DetailName", details, 116, 20, 404, 40, "", 29, Ink);
            Text type = Label("DetailType", details, 116, 65, 404, 28, "", 19, Muted);
            Text cost = Label("DetailCost", details, 22, 117, 498, 32, "", 22, Ink);
            Text description = Label("DetailDescription", details, 22, 165, 498, 94, "", 21, Ink);
            Label("ParameterHeading", details, 22, 272, 498, 30, "能力参数", 20, Muted);
            Text parameters = Label("DetailParameters", details, 22, 313, 498, 90, "", 22, Ink);
            Text status = Label("DetailStatus", details, 22, 419, 498, 65, "", 19, Muted);
            var ui = new SerializedObject(canvasObject.GetComponent<AbilityUI>());
            Bind(ui, "catalog", AssetDatabase.LoadAssetAtPath<AbilityCatalog>("Assets/Data/Abilities/TestAbilityCatalog.asset"));
            Bind(ui, "systemConfig", AssetDatabase.LoadAssetAtPath<AbilitySystemConfig>("Assets/Data/Abilities/DefaultAbilitySystemConfig.asset"));
            Bind(ui, "slotPrefab", prefab);
            Bind(ui, "inventoryColumns", inventory);
            Bind(ui, "equipmentPositions", positions);
            Bind(ui, "capacityCells", cells);
            Bind(ui, "equippedCount", equipped);
            Bind(ui, "remainingCapacity", remaining);
            Bind(ui, "detailIcon", icon);
            Bind(ui, "detailIconFallback", fallback);
            Bind(ui, "detailName", name);
            Bind(ui, "detailType", type);
            Bind(ui, "detailCost", cost);
            Bind(ui, "detailDescription", description);
            Bind(ui, "detailParameters", parameters);
            Bind(ui, "detailStatus", status);
            Bind(ui, "operationFeedback", feedback);
            ui.ApplyModifiedPropertiesWithoutUndo();
            var input = new SerializedObject(canvasObject.GetComponent<AbilityInput>());
            Bind(input, "ui", canvasObject.GetComponent<AbilityUI>());
            input.FindProperty("logRequests").boolValue = false;
            input.ApplyModifiedPropertiesWithoutUndo();
            var equipment = new SerializedObject(canvasObject.GetComponent<AbilityEquipmentController>());
            Bind(equipment, "systemConfig", AssetDatabase.LoadAssetAtPath<AbilitySystemConfig>("Assets/Data/Abilities/DefaultAbilitySystemConfig.asset"));
            Bind(equipment, "ui", canvasObject.GetComponent<AbilityUI>());
            Bind(equipment, "input", canvasObject.GetComponent<AbilityInput>());
            equipment.ApplyModifiedPropertiesWithoutUndo();
            var eventObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventObject.GetComponent<EventSystem>().sendNavigationEvents = false;
            // The module assigns its default pointer actions when enabled; no project input asset is changed.
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("ABILITY_UI_SCENE_CREATED: " + ScenePath);
        }

        private static AbilitySlotView CreateSlotPrefab()
        {
            RectTransform root = Rect("AbilitySlot", null, 0, 0, 200, 102);
            Image background = Panel(root, new Color32(30, 41, 53, 255));
            background.raycastTarget = true;
            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = layout.preferredHeight = 102;
            AbilitySlotView view = root.gameObject.AddComponent<AbilitySlotView>();
            RectTransform frame = Rect("SelectionFrame", root, 0, 0, 0, 0);
            Stretch(frame);
            Color accent = new Color32(148, 210, 239, 255);
            Border(frame, "Top", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 3), accent);
            Border(frame, "Bottom", Vector2.zero, new Vector2(1, 0), new Vector2(0, 3), accent);
            Border(frame, "Left", Vector2.zero, new Vector2(0, 1), new Vector2(3, 0), accent);
            Border(frame, "Right", new Vector2(1, 0), Vector2.one, new Vector2(3, 0), accent);
            frame.gameObject.SetActive(false);
            Image icon = Panel(Rect("Icon", root, 12, 12, 38, 38), Color.white);
            icon.preserveAspect = true;
            Text fallback = Label("IconFallback", root, 10, 12, 42, 38, "", 16, Muted);
            fallback.alignment = TextAnchor.MiddleCenter;
            Text name = Label("AbilityName", root, 62, 12, 132, 36, "", 22, Ink);
            Text cost = Label("Cost", root, 12, 58, 180, 25, "", 17, Muted);
            Text count = Label("EquippedQuantity", root, 110, 81, 83, 18, "", 14, Muted);
            var serialized = new SerializedObject(view);
            Bind(serialized, "background", background);
            Bind(serialized, "selectionFrame", frame.gameObject);
            Bind(serialized, "icon", icon);
            Bind(serialized, "iconFallback", fallback);
            Bind(serialized, "abilityName", name);
            Bind(serialized, "cost", cost);
            Bind(serialized, "equippedCount", count);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
            Object.DestroyImmediate(root.gameObject);
            return saved.GetComponent<AbilitySlotView>();
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
        }
        private static void Bind(SerializedObject target, string field, Object value)
        {
            if (value == null) throw new InvalidOperationException("Missing binding: " + field);
            target.FindProperty(field).objectReferenceValue = value;
        }
        private static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        private static Image Panel(RectTransform rect, Color color)
        {
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }
        private static Text Label(string name, Transform parent, float x, float y, float width, float height,
            string value, int size, Color color)
        {
            Text text = Rect(name, parent, x, y, width, height).gameObject.AddComponent<Text>();
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }
        private static void Horizontal(RectTransform rect, float spacing)
        {
            var group = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.spacing = spacing;
            group.childControlWidth = group.childControlHeight = true;
            group.childForceExpandWidth = group.childForceExpandHeight = true;
        }
        private static void Border(RectTransform parent, string name, Vector2 min, Vector2 max, Vector2 size, Color color)
        {
            RectTransform rect = Rect(name, parent, 0, 0, 0, 0);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            Panel(rect, color);
        }
    }
}
