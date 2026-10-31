using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SysARchitect.Components;
using SysARchitect.Core;
using SysARchitect.Graph;
using SysARchitect.Interaction;
using SysARchitect.Simulation;
using SysARchitect.Visualization;

namespace SysARchitect.Tests
{
    public class KubernetesWorkspaceTests
    {
        [UnityTest]
        public IEnumerator DeploymentUIControlsRealPodsRoutesAndFailureReplacement()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity"); yield return null;
            var graph = Object.FindAnyObjectByType<ArchitectureGraph>(); var sim = Object.FindAnyObjectByType<SimulationController>();
            sim.Engine.RequestsPerSecond = 0;
            var cluster = graph.AddNode(NodeType.KubernetesCluster, new Vector3(.2f, .2f, 0));
            Object.FindAnyObjectByType<NodeSelectionController>().Select(cluster.GetComponent<NodeVisual>()); yield return null; yield return null;
            var panel = GameObject.Find("Workspace UI/Kubernetes Explorer"); Assert.That(panel, Is.Not.Null);
            panel.transform.Find("Add worker").GetComponent<Button>().onClick.Invoke(); panel.transform.Find("Add worker").GetComponent<Button>().onClick.Invoke();
            panel.GetComponentInChildren<Slider>().value = 3;
            for (int i = 0; i < 50; i++) sim.StepOnce();
            var model = Object.FindAnyObjectByType<KubernetesWorkspaceController>().GetCluster(cluster);
            Assert.That(model.ActualReplicas, Is.EqualTo(3)); Assert.That(model.ReadyReplicas, Is.EqualTo(3));
            string old = model.Pods[0].NodeId; panel.transform.Find("Fail Pod").GetComponent<Button>().onClick.Invoke();
            for (int i = 0; i < 40; i++) sim.StepOnce();
            Assert.That(graph.FindNode(old), Is.Null); Assert.That(model.Replacements, Is.EqualTo(1));
            Assert.That(model.DesiredReplicas, Is.EqualTo(3)); Assert.That(graph.Nodes.Count, Is.EqualTo(6));
            var f = graph.AddNode(NodeType.Frontend, new Vector3(-.8f, .15f, 0)); graph.AddConnection(f, cluster);
            sim.Engine.RequestsPerSecond = 90; for (int i = 0; i < 80; i++) sim.StepOnce();
            Assert.That(sim.Engine.Succeeded, Is.GreaterThan(100)); Assert.That(sim.Engine.Failed, Is.Zero);
            Assert.That(model.Pods.All(p => sim.Engine.FindNode(p.NodeId).Completed > 0), Is.True);
            yield return null;
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null) DesktopWorkspaceTests.CaptureDemo("sysarchitect-kubernetes.png");
            graph.RemoveNode(cluster); for (int i = 0; i < 40; i++) sim.StepOnce(); yield return null;
            Assert.That(graph.Nodes.Count, Is.EqualTo(1)); Assert.That(graph.Connections, Is.Empty);
        }
    }
}
