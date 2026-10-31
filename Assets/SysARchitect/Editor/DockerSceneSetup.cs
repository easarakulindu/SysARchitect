using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SysARchitect.Components;
using SysARchitect.Graph;
using SysARchitect.Simulation;
using SysARchitect.UI;
using static SysARchitect.Editor.DesktopSceneSetup;

namespace SysARchitect.Editor
{
    public static class DockerSceneSetup
    {
        public static void Configure()
        {
            foreach (var path in new[] { DesktopSceneSetup.ScenePath, ARWorkspaceSetup.ScenePath })
            {
                var scene = EditorSceneManager.OpenScene(path);
                var graph = Object.FindAnyObjectByType<ArchitectureGraph>();
                var simulation = graph.GetComponent<SimulationController>();
                var canvas = GameObject.Find("Workspace UI");
                var root = canvas.transform.Find("Safe Area") ?? canvas.transform;
                var components = root.Find("Toolbox/Components").GetComponent<RectTransform>();
                if (components.GetComponent<UnityEngine.UI.ScrollRect>() == null)
                {
                    UnityEngine.Object.DestroyImmediate(components.GetComponent<UnityEngine.UI.VerticalLayoutGroup>());
                    components.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
                    var scroll = components.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
                    var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
                    content.SetParent(components, false); content.anchorMin = content.anchorMax = content.pivot = new Vector2(0, 1); content.sizeDelta = new Vector2(186, 312);
                    var layout = content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
                    layout.spacing = 4; layout.childForceExpandHeight = false;
                    content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                    scroll.viewport = components; scroll.content = content; scroll.horizontal = false;
                    Set(canvas.GetComponent<ToolboxUI>(), "buttonContainer", content);
                }
                if (canvas.GetComponent<DockerUI>() != null) { EditorSceneManager.SaveScene(scene); continue; }
                var controller = graph.gameObject.AddComponent<DockerWorkspaceController>();
                Set(controller, "graph", graph); Set(controller, "simulation", simulation);
                var ui = canvas.AddComponent<DockerUI>();
                Set(ui, "graph", graph); Set(ui, "simulation", simulation); Set(ui, "docker", controller); Set(ui, "inspector", canvas.GetComponent<InspectorUI>());
                var panel = Panel(root, "Docker Explorer", 220, 410, 760, 270); Set(ui, "panel", panel.gameObject);
                Label(panel, "DOCKER: IMAGE TO ISOLATED INSTANCES", 12, 8, 736, 28, 18);
                Set(ui, "imageButton", Button(panel, "Next image", 12, 42, 190, 44));
                Set(ui, "createButton", Button(panel, "Create container", 210, 42, 150, 44));
                Set(ui, "expandButton", Button(panel, "Expand / Collapse", 368, 42, 160, 44));
                Label(panel, "Choose container", 540, 30, 190, 18, 12);
                Set(ui, "memberSlider", Slider(panel, 540, 58, 190, 22));
                Set(ui, "stopButton", Button(panel, "Stop", 12, 94, 90, 44));
                Set(ui, "startButton", Button(panel, "Start", 110, 94, 90, 44));
                Set(ui, "restartButton", Button(panel, "Restart", 208, 94, 100, 44));
                Set(ui, "removeButton", Button(panel, "Remove", 316, 94, 100, 44));
                Set(ui, "failButton", Button(panel, "Crash", 424, 94, 104, 44));
                Set(ui, "cpuSlider", Slider(panel, 548, 154, 188, 22));
                Set(ui, "memorySlider", Slider(panel, 548, 208, 188, 22));
                Set(ui, "limits", Label(panel, "Limits", 548, 92, 188, 55, 12));
                Set(ui, "details", Label(panel, "Docker details", 12, 146, 520, 112, 12));
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
