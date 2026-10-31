using System.Collections;
using System.IO;
using UnityEngine.Rendering;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SysARchitect.Core;
using SysARchitect.Graph;
using SysARchitect.Interaction;
using SysARchitect.Visualization;
using SysARchitect.Simulation;

namespace SysARchitect.Tests
{
    public class DesktopWorkspaceTests
    {
        [UnityTest]
        public IEnumerator DesktopSceneStartsEmptyAndSupportsBuilderLifecycle()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity");
            yield return null;
            var graphs = Object.FindObjectsByType<ArchitectureGraph>(FindObjectsSortMode.None);
            Assert.That(graphs.Length, Is.EqualTo(1));
            var graph = graphs[0];
            Assert.That(graph.Nodes, Is.Empty);
            Assert.That(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            var manager = Object.FindFirstObjectByType<InteractionManager>();
            var selection = Object.FindFirstObjectByType<NodeSelectionController>();
            var toolbox = GameObject.Find("Workspace UI/Toolbox/Components");
            Assert.That(toolbox.GetComponentsInChildren<Button>().Length, Is.EqualTo(10));
            toolbox.GetComponentsInChildren<Button>()[0].onClick.Invoke();
            Assert.That(Object.FindFirstObjectByType<NodePlacementController>().IsPlacing, Is.True);
            manager.ReturnToSelectMode();
            yield return null;
            Assert.That(GameObject.Find("Preview_Frontend"), Is.Null);

            var frontend = graph.AddNode(NodeType.Frontend, new Vector3(-.4f, .1f, 0));
            var backend = graph.AddNode(NodeType.Backend, new Vector3(0, .2f, 0));
            var database = graph.AddNode(NodeType.Database, new Vector3(.4f, .125f, 0));
            Assert.That(frontend.GetComponent<Collider>(), Is.Not.Null);
            Assert.That(frontend.GetComponent<Renderer>().sharedMaterial.shader.name, Does.Contain("Universal Render Pipeline"));
            Assert.That(graph.ValidateConnection(frontend, database).isValid, Is.False);
            var connection = graph.AddConnection(frontend, backend);
            Assert.That(connection, Is.Not.Null);
            Assert.That(graph.ValidateConnection(frontend, backend).isValid, Is.False);
            graph.AddConnection(backend, database);
            Assert.That(graph.ValidateGraph(), Is.Empty);
            selection.Select(backend.GetComponent<NodeVisual>());
            yield return null;
            Assert.That(GameObject.Find("Workspace UI/Node Inspector"), Is.Not.Null);
            var edgeButtons = GameObject.Find("Workspace UI/Node Inspector/Connections/Content").GetComponentsInChildren<Button>();
            Assert.That(edgeButtons.Length, Is.EqualTo(2));
            edgeButtons[0].onClick.Invoke();
            Assert.That(graph.Connections.Count, Is.EqualTo(1));
            GameObject.Find("Workspace UI/Node Inspector/Rotate 45 degrees").GetComponent<Button>().onClick.Invoke();
            Assert.That(backend.transform.eulerAngles.y, Is.EqualTo(45).Within(.01));
            manager.OnDeleteButtonPressed();
            yield return null;
            Assert.That(graph.Nodes.Count, Is.EqualTo(2));
            Assert.That(graph.Connections, Is.Empty);
            Assert.That(selection.SelectedNode, Is.Null);
            manager.OnClearButtonPressed();
            yield return null;
            Assert.That(graph.Nodes, Is.Empty);
            Assert.That(Object.FindObjectsByType<NodeVisual>(FindObjectsSortMode.None), Is.Empty);
        }

        [UnityTest]
        public IEnumerator SimulationControlsShareTheGraphAndSupportPauseStepFailureAndReset()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity");
            yield return null;
            var graph = Object.FindFirstObjectByType<ArchitectureGraph>();
            var simulation = Object.FindFirstObjectByType<SimulationController>();
            Assert.That(Object.FindObjectsByType<SimulationController>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            var frontend = graph.AddNode(NodeType.Frontend, new Vector3(-.5f, .1f, 0));
            var balancer = graph.AddNode(NodeType.LoadBalancer, new Vector3(-.2f, .05f, 0));
            var backend = graph.AddNode(NodeType.Backend, new Vector3(.1f, .2f, -.25f));
            var backend2 = graph.AddNode(NodeType.Backend, new Vector3(.1f, .2f, .25f));
            var database = graph.AddNode(NodeType.Database, new Vector3(.5f, .125f, 0));
            frontend.ProcessingCapacity = balancer.ProcessingCapacity = database.ProcessingCapacity = 500;
            backend.ProcessingCapacity = backend2.ProcessingCapacity = 40;
            graph.AddConnection(frontend, balancer);
            graph.AddConnection(balancer, backend);
            graph.AddConnection(balancer, backend2);
            graph.AddConnection(backend, database);
            graph.AddConnection(backend2, database);
            simulation.Engine.RequestsPerSecond = 60;
            var selection = Object.FindFirstObjectByType<NodeSelectionController>();
            selection.Select(backend.GetComponent<NodeVisual>());
            for (int i = 0; i < 200; i++) simulation.StepOnce();
            Assert.That(simulation.Engine.Succeeded, Is.GreaterThan(0));
            Assert.That(simulation.Engine.Failed, Is.Zero);
            Assert.That(simulation.Engine.Nodes.Count, Is.EqualTo(graph.Nodes.Count));
            Assert.That(Object.FindObjectsByType<RequestPacketVisual>(FindObjectsSortMode.None).Length, Is.LessThanOrEqualTo(64));
            double pausedTime = simulation.Engine.Time;
            yield return new WaitForSeconds(.1f);
            Assert.That(simulation.Engine.Time, Is.EqualTo(pausedTime));
            GameObject.Find("Workspace UI/Simulation/Step 50 ms").GetComponent<Button>().onClick.Invoke();
            Assert.That(simulation.Engine.Time, Is.EqualTo(pausedTime + SimulationEngine.TickSeconds).Within(.00001));
            GameObject.Find("Workspace UI/Node Inspector/Inject failure").GetComponent<Button>().onClick.Invoke();
            Assert.That(backend.Status, Is.EqualTo(NodeStatus.Crashed));
            Assert.That(frontend.Status, Is.Not.EqualTo(NodeStatus.Crashed));
            GameObject.Find("Workspace UI/Node Inspector/Restart").GetComponent<Button>().onClick.Invoke();
            Assert.That(backend.Status, Is.EqualTo(NodeStatus.Recovering));
            for (int i = 0; i < 30; i++) simulation.StepOnce();
            Assert.That(backend.Status, Is.Not.EqualTo(NodeStatus.Recovering));
            GameObject.Find("Workspace UI/Simulation/Run").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSeconds(.2f);
            Assert.That(simulation.IsRunning, Is.True);
            Assert.That(simulation.Engine.Time, Is.GreaterThan(pausedTime + 1));
            GameObject.Find("Workspace UI/Simulation/Pause").GetComponent<Button>().onClick.Invoke();
            Assert.That(simulation.IsRunning, Is.False);
            // Optional rendered evidence when the test is run with a graphics device.
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                yield return null;
                CaptureDemo();
            }
            GameObject.Find("Workspace UI/Simulation/Reset simulation").GetComponent<Button>().onClick.Invoke();
            Assert.That(simulation.Engine.Generated, Is.Zero);
            Assert.That(simulation.Engine.Time, Is.Zero);
            Assert.That(graph.Nodes.Count, Is.EqualTo(5));
            yield return null;
            Assert.That(Object.FindObjectsByType<RequestPacketVisual>(FindObjectsSortMode.None), Is.Empty);
            Object.FindFirstObjectByType<InteractionManager>().OnClearButtonPressed();
            yield return null;
            Assert.That(simulation.Engine.Nodes, Is.Empty);
        }

        internal static void CaptureDemo(string fileName = "sysarchitect-desktop-demo.png")
        {
            var camera = Camera.main;
            var canvas = GameObject.Find("Workspace UI").GetComponent<Canvas>();
            var target = new RenderTexture(1280, 800, 24);
            var previous = RenderTexture.active;
            camera.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = .3f;
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
            RenderTexture.active = target;
            var texture = new Texture2D(1280, 800, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1280, 800), 0, 0);
            texture.Apply();
            File.WriteAllBytes(Path.Combine(Path.GetTempPath(), fileName), texture.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Object.Destroy(texture);
            Object.Destroy(target);
        }

        [UnityTest]
        public IEnumerator ConnectionsRejectForeignNodesAndCycles()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity");
            var graph = Object.FindFirstObjectByType<ArchitectureGraph>();
            var a = graph.AddNode(NodeType.Backend, Vector3.zero);
            var b = graph.AddNode(NodeType.Backend, Vector3.right * .3f);
            var c = graph.AddNode(NodeType.Backend, Vector3.left * .3f);
            graph.AddConnection(a, b);
            graph.AddConnection(b, c);
            Assert.That(graph.ValidateConnection(c, a).isValid, Is.False);
            var foreign = new GameObject("Foreign node").AddComponent<GraphNode>();
            Assert.That(graph.ValidateConnection(a, foreign).isValid, Is.False);
            Object.Destroy(foreign.gameObject);
            graph.ClearGraph();
            yield return null;
        }
    }
}
