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
    public static class KubernetesSceneSetup
    {
        public static void Configure()
        {
            foreach (var path in new[] { DesktopSceneSetup.ScenePath, ARWorkspaceSetup.ScenePath })
            {
                var scene = EditorSceneManager.OpenScene(path);
                var graph = Object.FindAnyObjectByType<ArchitectureGraph>(); var canvas = GameObject.Find("Workspace UI");
                if (canvas.GetComponent<KubernetesUI>() != null) continue;
                var simulation = graph.GetComponent<SimulationController>();
                var controller = graph.gameObject.AddComponent<KubernetesWorkspaceController>();
                Set(controller, "graph", graph); Set(controller, "simulation", simulation);
                var ui = canvas.AddComponent<KubernetesUI>();
                Set(ui, "graph", graph); Set(ui, "simulation", simulation); Set(ui, "kubernetes", controller); Set(ui, "inspector", canvas.GetComponent<InspectorUI>());
                var root = canvas.transform.Find("Safe Area") ?? canvas.transform;
                var panel = Panel(root, "Kubernetes Explorer", 220, 410, 760, 270); Set(ui, "panel", panel.gameObject);
                Label(panel, "KUBERNETES: DEPLOYMENT DESIRED STATE", 12, 8, 736, 28, 18);
                Set(ui, "addWorker", Button(panel, "Add worker", 12, 42, 132, 44));
                Set(ui, "nextPod", Button(panel, "Next Pod", 152, 42, 120, 44));
                Set(ui, "failPod", Button(panel, "Fail Pod", 280, 42, 120, 44));
                Set(ui, "expandCluster", Button(panel, "Cluster view", 408, 42, 158, 44));
                Set(ui, "expandWorker", Button(panel, "Worker detail", 574, 42, 162, 44));
                Set(ui, "replicaText", Label(panel, "Desired / Actual / Ready", 12, 94, 520, 32, 13));
                Set(ui, "desiredSlider", Slider(panel, 548, 100, 188, 24));
                Set(ui, "details", Label(panel, "Deployment details", 12, 138, 724, 120, 12));
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
