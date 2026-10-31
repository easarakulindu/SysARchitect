using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using SysARchitect.AR;
using SysARchitect.Core;
using SysARchitect.Graph;
using SysARchitect.Interaction;
using SysARchitect.Simulation;

namespace SysARchitect.Tests
{
    public class SharedTouchTests
    {
        private static WorkspaceTouchInput Adapter(ArchitectureGraph graph)
        {
            var go = new GameObject("Synthetic touch adapter"); go.SetActive(false);
            var input = go.AddComponent<WorkspaceTouchInput>();
            void Set(string field, object value) => typeof(WorkspaceTouchInput).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(input, value);
            Set("graph", graph); Set("mainCamera", Camera.main);
            Set("interactions", Object.FindAnyObjectByType<InteractionManager>());
            Set("placement", Object.FindAnyObjectByType<NodePlacementController>());
            Set("selection", Object.FindAnyObjectByType<NodeSelectionController>());
            Set("movement", Object.FindAnyObjectByType<NodeMovementController>());
            Set("connections", Object.FindAnyObjectByType<ConnectionCreationController>());
            go.SetActive(true); input.enabled = false; // Inject pointer phases, not an AR tracking provider.
            return input;
        }
        private static void Tap(WorkspaceTouchInput input, Vector3 world, bool tracking = true)
        {
            var screen = Camera.main.WorldToScreenPoint(world);
            input.HandlePointer(screen, TouchPhase.Began, tracking); input.HandlePointer(screen, TouchPhase.Ended, tracking);
        }
        [UnityTest]
        public IEnumerator TouchSharesPlacementSelectionDragConnectionsDeletionAndSimulation()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity"); yield return null;
            var graph = Object.FindAnyObjectByType<ArchitectureGraph>(); var input = Adapter(graph);
            var manager = Object.FindAnyObjectByType<InteractionManager>();
            var selection = Object.FindAnyObjectByType<NodeSelectionController>();
            var sim = Object.FindAnyObjectByType<SimulationController>(); sim.Engine.RequestsPerSecond = 0;
            manager.OnToolboxNodeSelected(NodeType.Frontend);
            Tap(input, new Vector3(-.5f, 0, 0), false); Assert.That(graph.Nodes, Is.Empty);
            Tap(input, new Vector3(-.5f, 0, 0)); yield return null;
            Assert.That(graph.Nodes.Count, Is.EqualTo(1)); var f = graph.Nodes[0];
            manager.OnToolboxNodeSelected(NodeType.Backend); Tap(input, new Vector3(.5f, 0, 0)); yield return null;
            var b = graph.Nodes[1]; Physics.SyncTransforms();
            manager.OnConnectButtonPressed(); yield return null; yield return null;
            Tap(input, f.transform.position); Tap(input, b.transform.position); yield return null;
            Assert.That(graph.Connections.Count, Is.EqualTo(1));
            Assert.That(sim.Engine.FindNode(b.NodeId), Is.Not.Null);
            sim.Engine.SubmitRequest(f.NodeId, false, 1); for (int i = 0; i < 40; i++) sim.StepOnce();
            Assert.That(sim.Engine.Succeeded, Is.EqualTo(1));
            manager.ReturnToSelectMode(); Tap(input, b.transform.position); yield return null;
            Assert.That(selection.SelectedNode.GetGraphNode(), Is.EqualTo(b));
            Vector3 original = b.transform.position;
            var start = Camera.main.WorldToScreenPoint(original);
            var destination = Camera.main.WorldToScreenPoint(new Vector3(.65f, 0, 0));
            input.HandlePointer(start, TouchPhase.Began, true);
            input.HandlePointer(destination, TouchPhase.Moved, true);
            input.HandlePointer(destination, TouchPhase.Ended, true);
            Assert.That(b.transform.position.x, Is.EqualTo(.65f).Within(.01f));
            manager.ReturnToSelectMode(); selection.Deselect(); yield return null; yield return null;
            Visualization.ConnectionRenderer.GetEndpoints(f.transform, b.transform, out var a, out var z);
            Tap(input, (a + z) * .5f); Assert.That(selection.SelectedConnection, Is.EqualTo(graph.Connections[0]));
            manager.OnDeleteConnectionButtonPressed(); Assert.That(graph.Nodes.Count, Is.EqualTo(2)); Assert.That(graph.Connections, Is.Empty);
        }
        [UnityTest]
        public IEnumerator ScaledWorkspaceKeepsNodesPacketsAndSurfaceBoundsConsistent()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity"); yield return null;
            var graph = Object.FindAnyObjectByType<ArchitectureGraph>(); graph.transform.localScale = Vector3.one * .35f;
            var input = Adapter(graph); var manager = Object.FindAnyObjectByType<InteractionManager>();
            manager.OnToolboxNodeSelected(NodeType.Backend);
            Tap(input, new Vector3(.1f, 0, 0)); yield return null;
            Assert.That(graph.Nodes.Count, Is.EqualTo(1));
            var node = graph.Nodes[0];
            Assert.That(node.transform.lossyScale.x, Is.EqualTo(.35f).Within(.001f));
            Assert.That(node.GetComponent<Collider>().bounds.min.y, Is.EqualTo(0).Within(.001f));
            Assert.That(input.TrySurfacePoint(Camera.main.WorldToScreenPoint(new Vector3(2, 0, 0)), out _), Is.False);
            Assert.That(ARWorkspace.TrackingAllowsInput(true, true), Is.True);
            Assert.That(ARWorkspace.TrackingAllowsInput(false, true), Is.False);
            Assert.That(ARWorkspace.TrackingAllowsInput(true, false), Is.False);
            var frontend = graph.AddNode(NodeType.Frontend, new Vector3(-.15f, .05f, 0));
            graph.AddConnection(frontend, node);
            var sim = Object.FindAnyObjectByType<SimulationController>(); sim.Engine.RequestsPerSecond = 0;
            sim.Engine.SubmitRequest(frontend.NodeId, false, 1);
            Visualization.RequestPacketVisual packet = null;
            for (int i = 0; i < 10 && packet == null; i++)
            { sim.StepOnce(); packet = Object.FindAnyObjectByType<Visualization.RequestPacketVisual>(); }
            Assert.That(packet, Is.Not.Null);
            Assert.That(packet.transform.lossyScale.x, Is.EqualTo(.014f).Within(.001f));
        }
    }
}
