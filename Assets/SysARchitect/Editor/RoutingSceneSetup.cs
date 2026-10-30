using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using SysARchitect.Graph;
using SysARchitect.Interaction;
using SysARchitect.Simulation;
using SysARchitect.UI;
using static SysARchitect.Editor.DesktopSceneSetup;

namespace SysARchitect.Editor
{
    public static class RoutingSceneSetup
    {
        public static void Configure()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var canvas = GameObject.Find("Workspace UI");
            if (canvas.GetComponent<RoutingUI>() != null)
            {
                ApplyLayout(canvas.GetComponent<RoutingUI>(), canvas);
                EditorSceneManager.SaveScene(scene);
                return;
            }
            var graph = Object.FindAnyObjectByType<ArchitectureGraph>();
            var selection = Object.FindAnyObjectByType<NodeSelectionController>();
            var interactions = Object.FindAnyObjectByType<InteractionManager>();
            Set(selection, "graph", graph);
            var ui = canvas.AddComponent<RoutingUI>();
            Set(ui, "graph", graph); Set(ui, "selection", selection); Set(ui, "interactions", interactions);
            Set(ui, "inspector", canvas.GetComponent<InspectorUI>());
            Set(ui, "simulation", Object.FindAnyObjectByType<SimulationController>());
            var toolbar = canvas.transform.Find("Toolbox").GetComponent<RectTransform>();
            toolbar.sizeDelta = new Vector2(210, 790);
            toolbar.Find("Clear workspace").GetComponent<RectTransform>().anchoredPosition = new Vector2(12, -588);
            toolbar.Find("Validate architecture").GetComponent<RectTransform>().anchoredPosition = new Vector2(12, -630);
            foreach (Transform child in toolbar)
            {
                var label = child.GetComponent<TMPro.TextMeshProUGUI>();
                if (label != null && label.text.StartsWith("Right drag"))
                    label.rectTransform.anchoredPosition = new Vector2(12, -680);
            }
            Set(ui, "toolbarDeleteConnection", Button(toolbar, "Delete Connection", 12, 546, 186, 34));
            var edgePanel = Panel(canvas.transform, "Connection Inspector", 990, 0, 290, 330);
            edgePanel.anchorMin = edgePanel.anchorMax = edgePanel.pivot = Vector2.one;
            edgePanel.anchoredPosition = Vector2.zero;
            Set(ui, "connectionPanel", edgePanel.gameObject);
            Label(edgePanel, "CONNECTION", 12, 12, 266, 30, 20);
            Set(ui, "connectionText", Label(edgePanel, "Select a directed line", 12, 54, 266, 210, 14));
            Set(ui, "deleteConnectionButton", Button(edgePanel, "Delete Connection", 12, 280, 266, 34));
            var panel = Panel(canvas.transform, "Routing Inspector", 220, 410, 760, 270);
            Set(ui, "routingPanel", panel.gameObject);
            Label(panel, "ROUTING / CACHE", 12, 8, 736, 26, 18);
            Set(ui, "applicationButton", Button(panel, "Application Routing", 12, 42, 230, 34));
            Set(ui, "databaseButton", Button(panel, "Database Router", 254, 42, 230, 34));
            Set(ui, "routingText", Label(panel, "Routing details", 12, 84, 736, 174, 13));
            ApplyLayout(ui, canvas);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
        private static void ApplyLayout(RoutingUI ui, GameObject canvas)
        {
            var panel = canvas.transform.Find("Routing Inspector").GetComponent<RectTransform>();
            panel.anchoredPosition = new Vector2(220, -450);
            panel.sizeDelta = new Vector2(760, 230);
            var text = new SerializedObject(ui).FindProperty("routingText").objectReferenceValue as TMPro.TextMeshProUGUI;
            text.rectTransform.sizeDelta = new Vector2(736, 134);
            var close = panel.Find("Dismiss details");
            Set(ui, "dismissButton", close != null ? close.GetComponent<Button>() : Button(panel, "Dismiss details", 596, 8, 152, 28));
        }
    }
}
