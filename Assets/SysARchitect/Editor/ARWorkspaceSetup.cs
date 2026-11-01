using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.XR.CoreUtils;
using SysARchitect.AR;
using SysARchitect.Graph;
using SysARchitect.Interaction;
using SysARchitect.Simulation;
using SysARchitect.Desktop;
using static SysARchitect.Editor.DesktopSceneSetup;

namespace SysARchitect.Editor
{
    public static class ARWorkspaceSetup
    {
        public const string ScenePath = "Assets/SysARchitect/Scenes/ARWorkspace.unity";
        [MenuItem("SysARchitect/Create Shared AR Workspace")]
        public static void Create()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                throw new InvalidOperationException("AR workspace already exists; open it instead of replacing it.");
            var scene = EditorSceneManager.OpenScene(DesktopSceneSetup.ScenePath);
            EditorSceneManager.SaveScene(scene, ScenePath, true);
            scene = EditorSceneManager.OpenScene(ScenePath);
            var graph = UnityEngine.Object.FindAnyObjectByType<ArchitectureGraph>();
            var simulation = graph.GetComponent<SimulationController>();
            UnityEngine.Object.DestroyImmediate(GameObject.Find("Desktop Placement Surface"));
            var surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
            surface.name = "Anchored Workspace Surface"; surface.layer = 4;
            surface.transform.SetParent(graph.transform, false);
            surface.transform.localPosition = new Vector3(0, -.025f, 0);
            surface.transform.localScale = new Vector3(2.4f, .05f, 2.4f);
            surface.GetComponent<Renderer>().sharedMaterial = MaterialAsset("Tabletop", Color.cyan);
            var originObject = new GameObject("AR Origin", typeof(XROrigin));
            var origin = originObject.GetComponent<XROrigin>();
            var offset = new GameObject("Camera Offset"); offset.transform.SetParent(origin.transform, false);
            var camera = Camera.main;
            UnityEngine.Object.DestroyImmediate(camera.GetComponent<DesktopCameraController>());
            camera.name = "AR Camera"; camera.transform.SetParent(offset.transform, false);
            camera.transform.localPosition = Vector3.zero; camera.transform.localRotation = Quaternion.identity;
            camera.backgroundColor = Color.clear;
            camera.gameObject.AddComponent<ARCameraManager>();
            camera.gameObject.AddComponent<ARCameraBackground>();
            var driver = camera.gameObject.AddComponent<TrackedPoseDriver>();
            driver.positionInput = new InputActionProperty(new InputAction("AR Position", binding: "<HandheldARInputDevice>/devicePosition", expectedControlType: "Vector3"));
            driver.rotationInput = new InputActionProperty(new InputAction("AR Rotation", binding: "<HandheldARInputDevice>/deviceRotation", expectedControlType: "Quaternion"));
            driver.trackingStateInput = new InputActionProperty(new InputAction("AR Tracking", binding: "<HandheldARInputDevice>/trackingState", expectedControlType: "Integer"));
            origin.Camera = camera; origin.CameraFloorOffsetObject = offset; origin.CameraYOffset = 0;
            new GameObject("AR Session", typeof(ARSession), typeof(ARInputManager));
            var planes = originObject.AddComponent<ARPlaneManager>();
            planes.requestedDetectionMode = PlaneDetectionMode.Horizontal;
            var raycasts = originObject.AddComponent<ARRaycastManager>();
            var anchors = originObject.AddComponent<ARAnchorManager>();
            const string prefabFolder = "Assets/SysARchitect/Prefabs/AR";
            if (!AssetDatabase.IsValidFolder(prefabFolder)) AssetDatabase.CreateFolder("Assets/SysARchitect/Prefabs", "AR");
            var planeObject = new GameObject("Detected Horizontal Plane", typeof(ARPlane), typeof(MeshFilter), typeof(MeshRenderer), typeof(ARPlaneMeshVisualizer));
            planeObject.layer = 2;
            var material = MaterialAsset("DetectedPlane", new Color(0, .65f, 1, .2f));
            material.SetFloat("_Surface", 1); material.SetFloat("_ZWrite", 0);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue = 3000;
            EditorUtility.SetDirty(material);
            planeObject.GetComponent<Renderer>().sharedMaterial = material;
            planes.planePrefab = PrefabUtility.SaveAsPrefabAsset(planeObject, prefabFolder + "/HorizontalPlane.prefab");
            UnityEngine.Object.DestroyImmediate(planeObject);
            var workspace = originObject.AddComponent<ARWorkspace>();
            Set(workspace, "graph", graph); Set(workspace, "simulation", simulation);
            Set(workspace, "raycasts", raycasts); Set(workspace, "planes", planes); Set(workspace, "anchors", anchors);
            var input = originObject.AddComponent<WorkspaceTouchInput>();
            Set(input, "workspace", workspace); Set(input, "graph", graph); Set(input, "mainCamera", camera);
            Set(input, "interactions", UnityEngine.Object.FindAnyObjectByType<InteractionManager>());
            Set(input, "placement", UnityEngine.Object.FindAnyObjectByType<NodePlacementController>());
            Set(input, "selection", UnityEngine.Object.FindAnyObjectByType<NodeSelectionController>());
            Set(input, "movement", UnityEngine.Object.FindAnyObjectByType<NodeMovementController>());
            Set(input, "connections", UnityEngine.Object.FindAnyObjectByType<ConnectionCreationController>());
            var canvas = GameObject.Find("Workspace UI");
            var safe = new GameObject("Safe Area", typeof(RectTransform)).GetComponent<RectTransform>();
            safe.SetParent(canvas.transform, false); safe.anchorMin = Vector2.zero; safe.anchorMax = Vector2.one;
            safe.offsetMin = safe.offsetMax = Vector2.zero;
            foreach (Transform child in canvas.transform.Cast<Transform>().ToArray()) if (child != safe) child.SetParent(safe, false);
            var mobile = canvas.AddComponent<MobileWorkspaceUI>();
            Set(mobile, "safeArea", safe); Set(mobile, "workspace", workspace);
            var toolbox = safe.Find("Toolbox"); Set(mobile, "toolbox", toolbox.gameObject);
            var bar = Panel(safe, "AR Controls", 220, 160, 760, 102);
            Set(mobile, "toggleToolbox", Button(bar, "Toolbox", 8, 8, 160, 48));
            Set(mobile, "reposition", Button(bar, "Reposition workspace", 176, 8, 230, 48));
            Set(workspace, "status", Label(bar, "Scan a surface", 8, 60, 744, 38, 14));
            var components = toolbox.Find("Components").GetComponent<RectTransform>();
            components.sizeDelta = new Vector2(186, 312);
            var scroll = components.gameObject.AddComponent<ScrollRect>();
            components.gameObject.AddComponent<RectMask2D>();
            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(components, false); content.anchorMin = content.anchorMax = content.pivot = new Vector2(0, 1); content.sizeDelta = new Vector2(186, 312);
            UnityEngine.Object.DestroyImmediate(components.GetComponent<VerticalLayoutGroup>());
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = 8; layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = components; scroll.content = content; scroll.horizontal = false;
            Set(canvas.GetComponent<SysARchitect.UI.ToolboxUI>(), "buttonContainer", content);
            var template = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SysARchitect/Prefabs/UI/ToolboxButton.prefab"));
            template.GetComponent<LayoutElement>().preferredHeight = 52;
            Set(canvas.GetComponent<SysARchitect.UI.ToolboxUI>(), "toolboxButtonPrefab", PrefabUtility.SaveAsPrefabAsset(template, "Assets/SysARchitect/Prefabs/UI/MobileToolboxButton.prefab"));
            UnityEngine.Object.DestroyImmediate(template);
            components.sizeDelta = new Vector2(186, 250);
            string[] actions = { "Select / Move", "Connect source > target", "Delete selected node", "Delete Connection", "Clear workspace", "Validate architecture" };
            for (int i = 0; i < actions.Length; i++)
            {
                var rect = toolbox.Cast<Transform>().Single(t => t.name == actions[i]).GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(12, -(356 + i * 56)); rect.sizeDelta = new Vector2(186, 48);
            }
            foreach (var label in toolbox.GetComponentsInChildren<TMPro.TextMeshProUGUI>())
                if (label.text.StartsWith("Right drag")) UnityEngine.Object.DestroyImmediate(label.gameObject);
            Label(toolbox.GetComponent<RectTransform>(), "Touch: tap to select / connect\nDrag selected node to move\nScroll toolbox for more components", 12, 706, 190, 68, 12);
            EditorSceneManager.SaveScene(scene);
            var scenes = EditorBuildSettings.scenes.ToList(); scenes.Add(new EditorBuildSettingsScene(ScenePath, true)); EditorBuildSettings.scenes = scenes.ToArray();
            ConfigureProviders(); AssetDatabase.SaveAssets();
            Debug.Log("Shared AR workspace created with AR Foundation 6.6.2 horizontal planes, anchor, and touch adapter.");
        }
        public static void ConfigureProviders()
        {
            foreach (var target in new[] { BuildTargetGroup.Android, BuildTargetGroup.iOS })
            {
                var settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(target);
                if (settings == null || settings.Manager == null || settings.Manager.activeLoaders.Count == 0)
                    throw new InvalidOperationException("Missing configured AR provider for " + target);
                settings.InitManagerOnStart = true; settings.Manager.automaticLoading = true; settings.Manager.automaticRunning = false;
                EditorUtility.SetDirty(settings); EditorUtility.SetDirty(settings.Manager);
            }
        }
        [MenuItem("SysARchitect/Mobile/Configure Availability Gate")]
        public static void ConfigureAvailabilityGate()
        {
            var previous = EditorSceneManager.GetSceneManagerSetup();
            foreach (var item in previous)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneByPath(item.path).isDirty)
                    throw new InvalidOperationException("Save open scenes before configuring AR availability.");
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var workspace = UnityEngine.Object.FindAnyObjectByType<ARWorkspace>();
            var camera = Camera.main;
            var session = UnityEngine.Object.FindAnyObjectByType<ARSession>();
            Behaviour[] components = {
                workspace.GetComponent<ARPlaneManager>(), workspace.GetComponent<ARRaycastManager>(), workspace.GetComponent<ARAnchorManager>(),
                session.GetComponent<ARInputManager>(), camera.GetComponent<ARCameraManager>(), camera.GetComponent<ARCameraBackground>(), camera.GetComponent<TrackedPoseDriver>()
            };
            if (components.Any(c => c == null)) throw new InvalidOperationException("Existing AR scene is missing a required provider component.");
            Set(workspace, "workspaceCamera", camera); Set(workspace, "session", session);
            var serialized = new SerializedObject(workspace);
            var property = serialized.FindProperty("arOnlyComponents"); property.arraySize = components.Length;
            for (int i = 0; i < components.Length; i++) { property.GetArrayElementAtIndex(i).objectReferenceValue = components[i]; components[i].enabled = false; }
            serialized.ApplyModifiedPropertiesWithoutUndo(); session.enabled = false;
            EditorSceneManager.SaveScene(scene);
            ConfigureProviders();
            AssetDatabase.SaveAssets();
            if (previous.Length > 0 && previous.All(item => !string.IsNullOrEmpty(item.path))) EditorSceneManager.RestoreSceneManagerSetup(previous);
        }
    }
}
