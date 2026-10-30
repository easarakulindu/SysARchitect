using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using SysARchitect.Core;
using SysARchitect.Graph;
using SysARchitect.Interaction;
using SysARchitect.Desktop;
using SysARchitect.UI;

namespace SysARchitect.Editor
{
    /// <summary>Creates the wired desktop scene through Unity's asset APIs, without replacing template assets.</summary>
    public static class DesktopSceneSetup
    {
        public const string ScenePath = "Assets/SysARchitect/Scenes/DesktopWorkspace.unity";
        private const string ButtonPath = "Assets/SysARchitect/Prefabs/UI/ToolboxButton.prefab";
        private static readonly Color PanelColor = new Color(.035f, .055f, .10f, .97f);

        [MenuItem("SysARchitect/Open Desktop Workspace")]
        public static void OpenWorkspace()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) CreateWorkspace();
            else EditorSceneManager.OpenScene(ScenePath);
        }

        public static void CreateWorkspace()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                throw new InvalidOperationException("Desktop workspace already exists; open it instead of replacing it.");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var nodeMaterial = MaterialAsset("Infrastructure", new Color(.1f, .9f, .8f));
            var connectionMaterial = MaterialAsset("Connection", new Color(.15f, .7f, .95f));
            var tableMaterial = MaterialAsset("Tabletop", new Color(.045f, .08f, .12f));
            var root = new GameObject("SysARchitect");
            root.AddComponent<AppStateManager>();
            var graph = root.AddComponent<ArchitectureGraph>();
            Set(graph, "defaultNodeMaterial", nodeMaterial);
            Set(graph, "connectionMaterial", connectionMaterial);

            var cameraGo = new GameObject("Desktop Camera", typeof(Camera), typeof(AudioListener), typeof(DesktopCameraController));
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.015f, .025f, .05f);
            camera.nearClipPlane = .01f;
            camera.farClipPlane = 50f;
            cameraGo.transform.position = new Vector3(0, 1.4f, 1.4f);
            cameraGo.transform.LookAt(Vector3.zero);

            var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "Desktop Placement Surface";
            table.layer = 4; // Existing Water layer is reserved as a desktop placement mask in this scene only.
            table.transform.position = new Vector3(0, -.04f, 0);
            table.transform.localScale = new Vector3(2.4f, .08f, 2.4f);
            table.GetComponent<Renderer>().sharedMaterial = tableMaterial;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var interactions = new GameObject("Builder Interactions");
            var selection = interactions.AddComponent<NodeSelectionController>();
            Set(selection, "mainCamera", camera);
            SetInt(selection, "nodeLayer", 1);
            var placement = interactions.AddComponent<NodePlacementController>();
            Set(placement, "graph", graph);
            Set(placement, "mainCamera", camera);
            Set(placement, "defaultNodeMaterial", nodeMaterial);
            SetInt(placement, "placementSurface", 1 << 4);
            var movement = interactions.AddComponent<NodeMovementController>();
            Set(movement, "mainCamera", camera);
            Set(movement, "selectionController", selection);
            SetInt(movement, "placementSurface", 1 << 4);
            var connection = interactions.AddComponent<ConnectionCreationController>();
            Set(connection, "mainCamera", camera);
            Set(connection, "graph", graph);
            Set(connection, "connectionMaterial", connectionMaterial);
            SetInt(connection, "nodeLayer", 1);
            var manager = interactions.AddComponent<InteractionManager>();
            Set(manager, "graph", graph);
            Set(manager, "selectionController", selection);
            Set(manager, "placementController", placement);
            Set(manager, "movementController", movement);
            Set(manager, "connectionController", connection);

            var canvasGo = new GameObject("Workspace UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 800);
            scaler.matchWidthOrHeight = .5f;
            var left = Panel(canvasGo.transform, "Toolbox", 0, 0, 210, 680);
            Label(left, "SysARchitect", 12, 12, 190, 36, 24);
            Label(left, "BUILD YOUR ARCHITECTURE", 12, 54, 190, 25, 12);
            var nodeButtons = Rect(left, "Components", 12, 92, 186, 312);
            var layout = nodeButtons.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            var select = Button(left, "Select / Move", 12, 420, 186, 34);
            var connect = Button(left, "Connect source > target", 12, 462, 186, 34);
            var delete = Button(left, "Delete selected node", 12, 504, 186, 34);
            var clear = Button(left, "Clear workspace", 12, 546, 186, 34);
            var validate = Button(left, "Validate architecture", 12, 588, 186, 34);
            Label(left, "Right drag: orbit\nMiddle drag: pan | Wheel: zoom\nEscape: select / cancel", 12, 630, 190, 48, 11);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath);
            if (prefab == null)
            {
                var template = Button(canvasGo.transform, "Toolbox Button", 0, 0, 186, 34);
                template.gameObject.AddComponent<LayoutElement>().preferredHeight = 34;
                prefab = PrefabUtility.SaveAsPrefabAsset(template.gameObject, ButtonPath);
                UnityEngine.Object.DestroyImmediate(template.gameObject);
            }
            var toolbox = canvasGo.AddComponent<ToolboxUI>();
            Set(toolbox, "interactionManager", manager);
            Set(toolbox, "graph", graph);
            Set(toolbox, "connectionController", connection);
            Set(toolbox, "buttonContainer", nodeButtons);
            Set(toolbox, "toolboxButtonPrefab", prefab);
            Set(toolbox, "selectButton", select);
            Set(toolbox, "connectButton", connect);
            Set(toolbox, "deleteButton", delete);
            Set(toolbox, "clearButton", clear);
            Set(toolbox, "validateButton", validate);
            var feedback = Panel(canvasGo.transform, "Feedback", 220, 688, 1050, 102);
            Set(toolbox, "statusText", Label(feedback, "Place a component to begin.", 12, 8, 1026, 86, 14));

            var inspectorPanel = Panel(canvasGo.transform, "Node Inspector", 990, 0, 290, 680);
            var inspector = canvasGo.AddComponent<InspectorUI>();
            Set(inspector, "selectionController", selection);
            Set(inspector, "graph", graph);
            Set(inspector, "inspectorPanel", inspectorPanel.gameObject);
            Set(inspector, "nodeNameText", Label(inspectorPanel, "Node", 12, 12, 266, 34, 23));
            Set(inspector, "nodeTypeText", Label(inspectorPanel, "Type", 12, 50, 266, 24, 16));
            Set(inspector, "nodeStatusText", Label(inspectorPanel, "Healthy", 12, 80, 266, 24, 16));
            Set(inspector, "nodeIdText", Label(inspectorPanel, "ID", 12, 108, 266, 24, 12));
            Set(inspector, "connectionsText", Label(inspectorPanel, "Connections", 12, 136, 266, 24, 14));
            Set(inspector, "nameInputField", Input(inspectorPanel, 12, 174, 266, 34));
            Set(inspector, "capacityValueText", Label(inspectorPanel, "Capacity", 12, 220, 266, 24, 15));
            Set(inspector, "capacitySlider", Slider(inspectorPanel, 12, 252, 266, 22));
            Set(inspector, "memoryValueText", Label(inspectorPanel, "Memory", 12, 292, 266, 24, 15));
            Set(inspector, "memorySlider", Slider(inspectorPanel, 12, 324, 266, 22));
            Set(inspector, "rotateButton", Button(inspectorPanel, "Rotate 45 degrees", 12, 366, 266, 34));
            Label(inspectorPanel, "Click an edge below to remove it", 12, 416, 266, 24, 13);
            var edges = Rect(inspectorPanel, "Connections", 12, 452, 266, 212);
            edges.gameObject.AddComponent<RectMask2D>();
            var scroll = edges.gameObject.AddComponent<ScrollRect>();
            var content = Rect(edges, "Content", 0, 0, 266, 212);
            content.pivot = new Vector2(0, 1);
            var edgeLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            edgeLayout.spacing = 4;
            edgeLayout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = edges;
            scroll.content = content;
            scroll.horizontal = false;
            Set(inspector, "connectionContainer", content);
            Set(inspector, "connectionButtonPrefab", prefab);

            Label(canvasGo.transform, "EMPTY WORKSPACE\nChoose a component from the toolbox, then click the tabletop.", 250, 20, 690, 68, 18);
            EditorSceneManager.SaveScene(scene, ScenePath);
            var existing = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            existing.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = existing.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("SysARchitect desktop workspace created: " + ScenePath);
        }

        public static Material MaterialAsset(string name, Color color)
        {
            string path = $"Assets/SysARchitect/Materials/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = name, color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        public static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(UnityEngine.Object target, string field, int value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        public static RectTransform Panel(Transform parent, string name, float x, float y, float width, float height)
        {
            var rect = Rect(parent, name, x, y, width, height);
            rect.gameObject.AddComponent<Image>().color = PanelColor;
            return rect;
        }

        public static TextMeshProUGUI Label(Transform parent, string text, float x, float y, float width, float height, float size)
        {
            var label = Rect(parent, "Label", x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = new Color(.75f, .9f, 1);
            label.raycastTarget = false;
            return label;
        }

        public static Button Button(Transform parent, string text, float x, float y, float width, float height)
        {
            var rect = Rect(parent, text, x, y, width, height);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(.08f, .22f, .29f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var label = Label(rect, text, 6, 3, width - 12, height - 6, 14);
            label.alignment = TextAlignmentOptions.Center;
            // Stretch labels so the same prefab also fits narrower connection lists.
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(6, 3);
            label.rectTransform.offsetMax = new Vector2(-6, -3);
            return button;
        }

        private static TMP_InputField Input(Transform parent, float x, float y, float width, float height)
        {
            var rect = Panel(parent, "Node name", x, y, width, height);
            var input = rect.gameObject.AddComponent<TMP_InputField>();
            input.targetGraphic = rect.GetComponent<Image>();
            var label = Label(rect, "", 6, 3, width - 12, height - 6, 16);
            input.textViewport = rect;
            input.textComponent = label;
            input.characterLimit = 40;
            return input;
        }

        public static Slider Slider(Transform parent, float x, float y, float width, float height)
        {
            var rect = Rect(parent, "Slider", x, y, width, height);
            rect.gameObject.AddComponent<Image>().color = new Color(.08f, .2f, .28f);
            var slider = rect.gameObject.AddComponent<Slider>();
            var handle = Rect(rect, "Handle", 0, 0, 16, height);
            handle.gameObject.AddComponent<Image>().color = Color.cyan;
            slider.handleRect = handle;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            return slider;
        }
    }
}
