using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using SysARchitect.AR;
using SysARchitect.Components;
using SysARchitect.Graph;
using SysARchitect.Interaction;
using SysARchitect.Lessons;
using SysARchitect.Simulation;
using SysARchitect.UI;
using static SysARchitect.Editor.DesktopSceneSetup;

namespace SysARchitect.Editor
{
    public static class LessonSceneSetup
    {
        public static void Configure()
        {
            foreach (var path in new[] { DesktopSceneSetup.ScenePath, ARWorkspaceSetup.ScenePath })
            {
                var scene = EditorSceneManager.OpenScene(path);
                var graph = Object.FindAnyObjectByType<ArchitectureGraph>(); var canvas = GameObject.Find("Workspace UI");
                canvas.GetComponent<Canvas>().sortingOrder = 100; // World labels must remain behind readable UI panels.
                if (canvas.GetComponent<LessonUI>() != null) { EditorSceneManager.SaveScene(scene); continue; }
                var lessons = graph.gameObject.AddComponent<LessonController>();
                Set(lessons, "graph", graph); Set(lessons, "simulation", graph.GetComponent<SimulationController>());
                Set(lessons, "interactions", Object.FindAnyObjectByType<InteractionManager>());
                Set(lessons, "docker", graph.GetComponent<DockerWorkspaceController>()); Set(lessons, "kubernetes", graph.GetComponent<KubernetesWorkspaceController>());
                var ui = canvas.AddComponent<LessonUI>(); Set(ui, "lessons", lessons);
                Set(ui, "workspace", Object.FindAnyObjectByType<ARWorkspace>());
                var root = canvas.transform.Find("Safe Area") ?? canvas.transform;
                var toolbox = root.Find("Toolbox").GetComponent<RectTransform>();
                foreach (var label in toolbox.GetComponentsInChildren<TMPro.TextMeshProUGUI>())
                    if (label.text == "BUILD YOUR ARCHITECTURE") label.gameObject.SetActive(false);
                Set(ui, "toggleButton", Button(toolbox, "Lessons / Progress", 12, 54, 186, 32));
                var panel = Panel(root, "Learning Guide", 220, 160, 760, 520); Set(ui, "panel", panel.gameObject);
                Set(ui, "title", Label(panel, "Guided learning", 12, 10, 570, 30, 18));
                Set(ui, "closeButton", Button(panel, "Close guide", 596, 8, 152, 44));
                Set(ui, "nextLesson", Button(panel, "Next lesson", 12, 56, 180, 44));
                Set(ui, "loadExample", Button(panel, "Load example (clears workspace)", 200, 56, 328, 44));
                Set(ui, "checkChallenge", Button(panel, "Check challenge", 536, 56, 212, 44));
                var viewport = Panel(panel, "Scrollable explanation", 12, 112, 736, 330);
                viewport.gameObject.AddComponent<RectMask2D>(); var scroll = viewport.gameObject.AddComponent<ScrollRect>();
                var body = Label(viewport, "Lesson content", 6, 0, 724, 330, 16);
                body.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                scroll.viewport = viewport; scroll.content = body.rectTransform; scroll.horizontal = false;
                Set(ui, "body", body);
                Set(ui, "previousStep", Button(panel, "Previous step", 12, 456, 180, 48));
                Set(ui, "stepLabel", Label(panel, "Step 1", 204, 464, 340, 34, 15));
                Set(ui, "nextStep", Button(panel, "Next step", 568, 456, 180, 48));
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
