using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SysARchitect.Core;
using SysARchitect.Graph;
using SysARchitect.Interaction;
using SysARchitect.Simulation;
using SysARchitect.UI;
using static SysARchitect.Editor.DesktopSceneSetup;

namespace SysARchitect.Editor
{
    public static class DesktopSimulationSetup
    {
        public static void AddSimulation()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            if (UnityEngine.Object.FindFirstObjectByType<SimulationController>() != null)
                throw new InvalidOperationException("Simulation is already wired; refusing to add duplicate components.");
            var graph = UnityEngine.Object.FindFirstObjectByType<ArchitectureGraph>();
            var manager = UnityEngine.Object.FindFirstObjectByType<InteractionManager>();
            var canvas = GameObject.Find("Workspace UI");
            var inspector = canvas.GetComponent<InspectorUI>();
            var simulation = graph.gameObject.AddComponent<SimulationController>();
            Set(simulation, "graph", graph);
            Set(simulation, "interactionManager", manager);
            Set(simulation, "appState", graph.GetComponent<AppStateManager>());
            Set(simulation, "packetMaterial", MaterialAsset("RequestPacket", Color.cyan));
            var panel = Panel(canvas.transform, "Simulation", 220, 0, 760, 155);
            var ui = canvas.AddComponent<SimulationUI>();
            Set(ui, "simulation", simulation);
            Set(ui, "inspector", inspector);
            Set(ui, "runButton", Button(panel, "Run", 12, 12, 136, 34));
            Set(ui, "pauseButton", Button(panel, "Pause", 160, 12, 136, 34));
            Set(ui, "stepButton", Button(panel, "Step 50 ms", 308, 12, 136, 34));
            Set(ui, "resetButton", Button(panel, "Reset simulation", 456, 12, 136, 34));
            Set(ui, "buildButton", Button(panel, "Build", 604, 12, 144, 34));
            Set(ui, "trafficText", Label(panel, "Incoming traffic", 12, 62, 310, 24, 15));
            Set(ui, "trafficSlider", Slider(panel, 12, 100, 300, 24));
            Set(ui, "countersText", Label(panel, "Paused", 334, 62, 414, 85, 14));
            var inspectorPanel = canvas.transform.Find("Node Inspector").GetComponent<RectTransform>();
            inspectorPanel.sizeDelta = new Vector2(290, 790);
            foreach (var label in inspectorPanel.GetComponentsInChildren<TextMeshProUGUI>())
                if (label.text.StartsWith("Click an edge")) label.rectTransform.anchoredPosition = new Vector2(12, -604);
            var edges = inspectorPanel.Find("Connections").GetComponent<RectTransform>();
            edges.anchoredPosition = new Vector2(12, -636);
            edges.sizeDelta = new Vector2(266, 138);
            var resources = Label(inspectorPanel, "Resource monitor", 12, 414, 266, 128, 13);
            Set(inspector, "resourceText", resources);
            Set(inspector, "simulation", simulation);
            Set(ui, "failButton", Button(inspectorPanel, "Inject failure", 12, 554, 128, 34));
            Set(ui, "restartButton", Button(inspectorPanel, "Restart", 150, 554, 128, 34));
            canvas.transform.Find("Feedback").GetComponent<RectTransform>().sizeDelta = new Vector2(760, 102);
            canvas.transform.Find("Feedback/Label").GetComponent<RectTransform>().sizeDelta = new Vector2(736, 86);
            foreach (Transform child in canvas.transform)
            {
                var label = child.GetComponent<TextMeshProUGUI>();
                if (label == null || !label.text.StartsWith("EMPTY WORKSPACE")) continue;
                label.text = "Build from the toolbox. Connect Frontend > Backend > Database, then Run.";
                label.fontSize = 14;
                label.rectTransform.anchoredPosition = new Vector2(250, -167);
                label.rectTransform.sizeDelta = new Vector2(690, 36);
            }
            ApplyLayout(canvas.GetComponent<Canvas>());
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        public static void UpdateLayout()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var canvas = GameObject.Find("Workspace UI").GetComponent<Canvas>();
            ApplyLayout(canvas);
            // Fix the existing reusable button prefab through Unity's prefab API.
            string path = "Assets/SysARchitect/Prefabs/UI/ToolboxButton.prefab";
            var prefab = PrefabUtility.LoadPrefabContents(path);
            FitButton(prefab.GetComponent<Button>());
            PrefabUtility.SaveAsPrefabAsset(prefab, path);
            PrefabUtility.UnloadPrefabContents(prefab);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        private static void ApplyLayout(Canvas canvas)
        {
            canvas.GetComponent<CanvasScaler>().screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            foreach (var button in canvas.GetComponentsInChildren<Button>(true)) FitButton(button);
            var inspector = canvas.transform.Find("Node Inspector").GetComponent<RectTransform>();
            inspector.anchorMin = inspector.anchorMax = inspector.pivot = Vector2.one;
            inspector.anchoredPosition = Vector2.zero;
            var feedback = canvas.transform.Find("Feedback").GetComponent<RectTransform>();
            feedback.anchorMin = Vector2.zero;
            feedback.anchorMax = Vector2.right;
            feedback.pivot = Vector2.zero;
            feedback.anchoredPosition = new Vector2(220, 10);
            feedback.sizeDelta = new Vector2(-520, 102);
        }

        private static void FitButton(Button button)
        {
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label == null) return;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(6, 3);
            label.rectTransform.offsetMax = new Vector2(-6, -3);
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 10;
            label.fontSizeMax = 14;
        }
    }
}
