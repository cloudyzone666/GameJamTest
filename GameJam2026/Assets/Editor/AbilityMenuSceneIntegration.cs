using System;
using System.Collections.Generic;
using System.IO;
using GameJam2026.Abilities;
using GameJam2026.Abilities.Input;
using GameJam2026.Abilities.Integration;
using GameJam2026.Abilities.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GameJam2026.EditorTools
{
    // One-time migration of the saved UI, not a replacement for either existing scene.
    public static class AbilityMenuSceneIntegration
    {
        private const string TestScenePath = "Assets/Scenes/AbilityEquipmentTest.unity";
        private const string MainScenePath = "Assets/Scenes/SampleScene.unity";
        private const string CanvasPrefabPath = "Assets/Prefabs/Abilities/AbilityEquipmentCanvas.prefab";

        public static void Integrate()
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode,
                "Scene integration is an edit-mode operation.");
            for (int index = 0; index < SceneManager.sceneCount; index++)
                Require(!SceneManager.GetSceneAt(index).isDirty,
                    "An open scene has unsaved changes. Integration was cancelled.");

            string prefabFile = Path.Combine(Application.dataPath,
                CanvasPrefabPath.Substring("Assets/".Length));
            Require(!File.Exists(prefabFile) && !File.Exists(prefabFile + ".meta")
                && AssetDatabase.LoadMainAssetAtPath(CanvasPrefabPath) == null,
                "The shared Canvas prefab already exists. Integration never overwrites it.");
            Require(AssetDatabase.IsValidFolder("Assets/Prefabs/Abilities"),
                "The existing abilities prefab directory is missing.");
            Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(TestScenePath) != null
                && AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath) != null,
                "Both original scenes are required.");

            Scene testScene = EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);
            Scene mainScene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Additive);
            Require(FindAll<GameplayAbilityCoordinator>(testScene).Count == 0
                && FindAll<GameplayAbilityCoordinator>(mainScene).Count == 0,
                "A scene already contains a gameplay ability coordinator.");
            Require(!HasObjectNamed(testScene, "GameSystems") && !HasObjectNamed(mainScene, "GameSystems"),
                "An existing GameSystems object must be reviewed instead of replaced.");
            Require(FindAll<AbilityEquipmentController>(mainScene).Count == 0
                && FindAll<AbilityUI>(mainScene).Count == 0
                && FindAll<AbilityInput>(mainScene).Count == 0,
                "The main scene already contains ability equipment components.");
            Require(FindAll<EventSystem>(mainScene).Count == 0
                && FindAll<BaseInputModule>(mainScene).Count == 0,
                "The main scene already contains an EventSystem or input module.");

            Canvas testCanvas = ExactlyOne<Canvas>(testScene);
            AbilityUI testUI = ExactlyOne<AbilityUI>(testScene);
            AbilityInput testInput = ExactlyOne<AbilityInput>(testScene);
            AbilityEquipmentController testController = ExactlyOne<AbilityEquipmentController>(testScene);
            EventSystem testEventSystem = ExactlyOne<EventSystem>(testScene);
            InputSystemUIInputModule testModule = ExactlyOne<InputSystemUIInputModule>(testScene);
            PlayerController2D mainPlayer = ExactlyOne<PlayerController2D>(mainScene);
            Require(testCanvas.transform.parent == null
                && testCanvas.gameObject == testUI.gameObject
                && testUI.gameObject == testInput.gameObject
                && testInput.gameObject == testController.gameObject,
                "The original Canvas must own the existing UI, input and equipment controller.");
            Require(testCanvas.gameObject.activeSelf && testCanvas.enabled && testUI.enabled
                && testInput.enabled && testController.enabled,
                "The saved equipment host and its components must remain enabled.");
            Require(testCanvas.GetComponent<GraphicRaycaster>() != null,
                "The original Canvas is missing its GraphicRaycaster.");
            Require(testCanvas.renderMode == RenderMode.ScreenSpaceOverlay,
                "The shared UI must retain its existing screen-space overlay Canvas.");
            Require(testEventSystem.gameObject == testModule.gameObject
                && FindAll<BaseInputModule>(testScene).Count == 1
                && !testEventSystem.sendNavigationEvents,
                "The original EventSystem must have one Input System module and no automatic navigation.");
            Require(testModule.actionsAsset != null && AssetDatabase.Contains(testModule.actionsAsset),
                "The original UI input module must reference a saved action asset.");
            ValidateOriginalBindings(testCanvas.gameObject);

            List<GameObject> originalMainObjects = AllObjects(mainScene);
            Component[] originalEventComponents = testEventSystem.GetComponents<Component>();
            var originalEventState = new string[originalEventComponents.Length];
            for (int index = 0; index < originalEventComponents.Length; index++)
                originalEventState[index] = EditorJsonUtility.ToJson(originalEventComponents[index]);

            GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(testCanvas.gameObject,
                CanvasPrefabPath, InteractionMode.AutomatedAction, out bool prefabSaved);
            Require(prefabSaved && prefab != null, "The existing Canvas could not be saved as a shared prefab.");
            Require(prefab.GetComponentsInChildren<AbilityEquipmentController>(true).Length == 1
                && prefab.GetComponentsInChildren<EventSystem>(true).Length == 0
                && prefab.GetComponentsInChildren<GameplayAbilityCoordinator>(true).Length == 0,
                "The shared prefab must contain one equipment owner and no scene-level systems.");

            GameObject mainCanvasObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab, mainScene);
            Require(mainCanvasObject != null, "The shared Canvas could not be instantiated in the main scene.");
            Canvas mainCanvas = mainCanvasObject.GetComponent<Canvas>();
            CreateCoordinator(testScene, testCanvas, null, true);
            CreateCoordinator(mainScene, mainCanvas, mainPlayer, false);
            CreateEventSystem(mainScene, testModule);

            ValidateIntegratedScene(testScene, testCanvas, null, true);
            ValidateIntegratedScene(mainScene, mainCanvas, mainPlayer, false);
            foreach (GameObject original in originalMainObjects)
                Require(original != null && original.scene == mainScene,
                    "An original main-scene object was unexpectedly removed or moved.");
            for (int index = 0; index < originalEventComponents.Length; index++)
                Require(originalEventComponents[index] != null
                    && EditorJsonUtility.ToJson(originalEventComponents[index]) == originalEventState[index],
                    "The original test-scene EventSystem was unexpectedly changed.");

            EditorSceneManager.MarkSceneDirty(testScene);
            EditorSceneManager.MarkSceneDirty(mainScene);
            Require(EditorSceneManager.SaveScene(testScene), "The integrated test scene could not be saved.");
            Require(EditorSceneManager.SaveScene(mainScene), "The integrated main scene could not be saved.");
            Debug.Log("ABILITY_MENU_SCENES_INTEGRATED: shared Canvas, one equipment owner and one EventSystem per scene.");
        }

        private static void CreateCoordinator(Scene scene, Canvas canvas, PlayerController2D player, bool menuOpen)
        {
            PlayerAbilityController playerAbilities = player != null
                ? player.GetComponent<PlayerAbilityController>() : null;
            if (player != null && playerAbilities == null)
                playerAbilities = player.gameObject.AddComponent<PlayerAbilityController>();
            if (player != null && player.GetComponent<PlayerJump>() == null)
                player.gameObject.AddComponent<PlayerJump>();
            if (player != null && player.GetComponent<PlayerDash>() == null)
                player.gameObject.AddComponent<PlayerDash>();
            var systems = new GameObject("GameSystems");
            SceneManager.MoveGameObjectToScene(systems, scene);
            var coordinator = systems.AddComponent<GameplayAbilityCoordinator>();
            var serialized = new SerializedObject(coordinator);
            Bind(serialized, "abilityCanvas", canvas);
            Bind(serialized, "abilityUI", canvas.GetComponent<AbilityUI>());
            Bind(serialized, "abilityInput", canvas.GetComponent<AbilityInput>());
            Bind(serialized, "equipmentController", canvas.GetComponent<AbilityEquipmentController>());
            Bind(serialized, "player", player, true);
            Bind(serialized, "playerAbilities", playerAbilities, true);
            SerializedProperty initialState = serialized.FindProperty("startWithMenuOpen");
            Require(initialState != null, "Coordinator is missing startWithMenuOpen.");
            initialState.boolValue = menuOpen;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateEventSystem(Scene scene, InputSystemUIInputModule originalModule)
        {
            var eventObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            SceneManager.MoveGameObjectToScene(eventObject, scene);
            eventObject.GetComponent<EventSystem>().sendNavigationEvents = false;
            InputSystemUIInputModule module = eventObject.GetComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
            // Keep the existing saved package references rather than serialize transient default actions.
            module.actionsAsset = originalModule.actionsAsset;
            module.point = originalModule.point;
            module.move = originalModule.move;
            module.submit = originalModule.submit;
            module.cancel = originalModule.cancel;
            module.leftClick = originalModule.leftClick;
            module.middleClick = originalModule.middleClick;
            module.rightClick = originalModule.rightClick;
            module.scrollWheel = originalModule.scrollWheel;
            module.trackedDevicePosition = originalModule.trackedDevicePosition;
            module.trackedDeviceOrientation = originalModule.trackedDeviceOrientation;
        }

        private static void ValidateIntegratedScene(Scene scene, Canvas canvas,
            PlayerController2D player, bool menuOpen)
        {
            AbilityUI ui = ExactlyOne<AbilityUI>(scene);
            AbilityInput input = ExactlyOne<AbilityInput>(scene);
            AbilityEquipmentController controller = ExactlyOne<AbilityEquipmentController>(scene);
            GameplayAbilityCoordinator coordinator = ExactlyOne<GameplayAbilityCoordinator>(scene);
            EventSystem eventSystem = ExactlyOne<EventSystem>(scene);
            InputSystemUIInputModule module = ExactlyOne<InputSystemUIInputModule>(scene);
            Require(ui.gameObject == canvas.gameObject && input.gameObject == canvas.gameObject
                && controller.gameObject == canvas.gameObject,
                "All equipment components must remain on the shared Canvas host.");
            Require(canvas.gameObject.activeSelf && canvas.enabled && ui.enabled && input.enabled
                && controller.enabled && coordinator.enabled,
                "The saved Canvas host and components must not be disabled to represent runtime menu state.");
            Require(PrefabUtility.IsPartOfPrefabInstance(canvas.gameObject)
                && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(canvas.gameObject) == CanvasPrefabPath,
                "Both scenes must use the same shared Canvas prefab.");
            Require(FindAll<BaseInputModule>(scene).Count == 1
                && eventSystem.gameObject == module.gameObject && !eventSystem.sendNavigationEvents
                && module.actionsAsset != null && AssetDatabase.Contains(module.actionsAsset),
                "Each scene must retain one correctly configured EventSystem.");
            var bindings = new SerializedObject(coordinator);
            Require(Reference(bindings, "abilityCanvas") == canvas
                && Reference(bindings, "abilityUI") == ui
                && Reference(bindings, "abilityInput") == input
                && Reference(bindings, "equipmentController") == controller
                && Reference(bindings, "player") == player
                && Reference(bindings, "playerAbilities") == (player != null
                    ? player.GetComponent<PlayerAbilityController>() : null)
                && bindings.FindProperty("startWithMenuOpen").boolValue == menuOpen,
                "Coordinator scene bindings do not match the intended equipment session.");
            ValidateOriginalBindings(canvas.gameObject);
        }

        private static void ValidateOriginalBindings(GameObject canvas)
        {
            var ui = new SerializedObject(canvas.GetComponent<AbilityUI>());
            string[] required = { "catalog", "systemConfig", "inventoryColumns", "slotPrefab",
                "equipmentPositions", "capacityCells", "equippedCount", "remainingCapacity",
                "operationFeedback", "detailIcon", "detailIconFallback", "detailName", "detailType",
                "detailCost", "detailDescription", "detailParameters", "detailStatus" };
            foreach (string field in required)
                Require(Reference(ui, field) != null, "The original UI binding is missing: " + field);
            var input = new SerializedObject(canvas.GetComponent<AbilityInput>());
            var controller = new SerializedObject(canvas.GetComponent<AbilityEquipmentController>());
            Require(Reference(input, "ui") == canvas.GetComponent<AbilityUI>()
                && Reference(controller, "ui") == canvas.GetComponent<AbilityUI>()
                && Reference(controller, "input") == canvas.GetComponent<AbilityInput>()
                && Reference(controller, "systemConfig") == Reference(ui, "systemConfig"),
                "Equipment input, UI and controller must retain their existing shared references.");
        }

        private static void Bind(SerializedObject target, string field, Object value, bool allowNull = false)
        {
            Require(allowNull || value != null, "Missing integration binding: " + field);
            SerializedProperty property = target.FindProperty(field);
            Require(property != null && property.propertyType == SerializedPropertyType.ObjectReference,
                "Missing object-reference field: " + field);
            property.objectReferenceValue = value;
        }

        private static Object Reference(SerializedObject target, string field)
        {
            SerializedProperty property = target.FindProperty(field);
            Require(property != null && property.propertyType == SerializedPropertyType.ObjectReference,
                "Missing object-reference field: " + field);
            return property.objectReferenceValue;
        }

        private static T ExactlyOne<T>(Scene scene) where T : Component
        {
            List<T> found = FindAll<T>(scene);
            Require(found.Count == 1, scene.path + " must contain exactly one " + typeof(T).Name + ".");
            return found[0];
        }

        private static List<T> FindAll<T>(Scene scene) where T : Component
        {
            var result = new List<T>();
            foreach (GameObject root in scene.GetRootGameObjects())
                result.AddRange(root.GetComponentsInChildren<T>(true));
            return result;
        }

        private static List<GameObject> AllObjects(Scene scene)
        {
            var result = new List<GameObject>();
            foreach (Transform transform in FindAll<Transform>(scene)) result.Add(transform.gameObject);
            return result;
        }

        private static bool HasObjectNamed(Scene scene, string name)
        {
            foreach (GameObject item in AllObjects(scene))
                if (item.name == name) return true;
            return false;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Ability menu integration: " + message);
        }
    }
}
