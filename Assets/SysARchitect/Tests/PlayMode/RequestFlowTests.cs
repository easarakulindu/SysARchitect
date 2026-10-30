using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using SysARchitect.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using SysARchitect.Core;
using SysARchitect.Graph;
using SysARchitect.Simulation;
using SysARchitect.Visualization;

namespace SysARchitect.Tests
{
    public class RequestFlowTests
    {
        [UnityTest]
        public IEnumerator PlacedButUnconnectedNodesReproduceReportedCounters()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity");
            yield return null;
            var graph = Object.FindFirstObjectByType<ArchitectureGraph>();
            var simulation = Object.FindFirstObjectByType<SimulationController>();
            var frontend = graph.AddNode(NodeType.Frontend, new Vector3(-.6f, .15f, 0));
            graph.AddNode(NodeType.Backend, new Vector3(0, .2f, 0));
            graph.AddNode(NodeType.Database, new Vector3(.6f, .125f, 0));
            for (int i = 0; i < 134; i++) simulation.StepOnce();
            Assert.That(simulation.Engine.Generated, Is.EqualTo(201));
            Assert.That(simulation.Engine.Failed, Is.EqualTo(199));
            Assert.That(simulation.Engine.InFlight, Is.EqualTo(2));
            Assert.That(simulation.Engine.Succeeded, Is.Zero);
            Assert.That(simulation.Engine.FindNode(frontend.NodeId).FailureReason, Is.EqualTo("No service route configured"));
            Assert.That(graph.Connections, Is.Empty);
            Assert.That(Object.FindObjectsByType<RequestPacketVisual>(FindObjectsSortMode.None), Is.Empty);
            yield return null;
            Assert.That(GameObject.Find("Workspace UI/Feedback/Label").GetComponent<TextMeshProUGUI>().text,
                Does.Contain("Connect"));
            Debug.Log("Reproduced unconnected architecture: Sent 201, Failed 199, Active 2; no edges or packet visuals.");
        }

        [UnityTest]
        public IEnumerator DirectionArrowIsOutsideTargetInfrastructure()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity");
            yield return null;
            var graph = Object.FindFirstObjectByType<ArchitectureGraph>();
            var a = graph.AddNode(NodeType.Frontend, new Vector3(-.6f, .15f, 0));
            var b = graph.AddNode(NodeType.Backend, new Vector3(.6f, .2f, 0));
            var edge = graph.AddConnection(a, b);
            yield return null;
            var line = edge.GetComponent<LineRenderer>();
            Assert.That(b.GetComponent<Collider>().bounds.Contains(line.GetPosition(1)), Is.False,
                "Direction arrow tip is hidden inside the target mesh.");
        }

        [UnityTest]
        public IEnumerator TrafficVisualizationSamplesEveryActiveConnection()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity");
            yield return null;
            var graph = Object.FindFirstObjectByType<ArchitectureGraph>();
            var simulation = Object.FindFirstObjectByType<SimulationController>();
            var f = graph.AddNode(NodeType.Frontend, new Vector3(-.8f, .15f, 0));
            var l = graph.AddNode(NodeType.LoadBalancer, new Vector3(-.3f, .05f, 0));
            var b = graph.AddNode(NodeType.Backend, new Vector3(.2f, .2f, 0));
            var d = graph.AddNode(NodeType.Database, new Vector3(.8f, .125f, 0));
            foreach (var node in graph.Nodes) node.ProcessingCapacity = 500;
            graph.AddConnection(f, l); graph.AddConnection(l, b); graph.AddConnection(b, d);
            simulation.Engine.RequestsPerSecond = 100;
            simulation.Run();
            var sampled = new HashSet<string>();
            for (int i = 0; i < 60; i++)
            {
                yield return new WaitForSeconds(.025f);
                foreach (var packet in Object.FindObjectsByType<RequestPacketVisual>(FindObjectsSortMode.None))
                {
                    Assert.That(packet.Connection, Is.Not.Null);
                    Assert.That(graph.Connections, Does.Contain(packet.Connection));
                    sampled.Add(packet.Connection.Source.NodeId + ">" + packet.Connection.Target.NodeId);
                }
            }
            simulation.Pause();
            Assert.That(simulation.Engine.Succeeded, Is.GreaterThan(0));
            Assert.That(simulation.Engine.Failed, Is.Zero);
            Assert.That(sampled, Does.Contain(b.NodeId + ">" + d.NodeId), "Downstream edge never receives visual packets.");
        }

        [UnityTest]
        public IEnumerator ScreenSpaceSourceAndTargetPickingCreatesOnlyValidDirectedEdges()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity");
            yield return null;
            var graph = Object.FindFirstObjectByType<ArchitectureGraph>();
            var connection = Object.FindFirstObjectByType<ConnectionCreationController>(FindObjectsInactive.Include);
            var frontend = graph.AddNode(NodeType.Frontend, new Vector3(-.5f, .15f, 0));
            var backend = graph.AddNode(NodeType.Backend, new Vector3(.5f, .2f, 0));
            var database = graph.AddNode(NodeType.Database, new Vector3(0, .125f, -.5f));
            Physics.SyncTransforms();
            string error = null;
            connection.OnConnectionError += message => error = message;
            GameObject.Find("Workspace UI/Toolbox/Connect source > target").GetComponent<Button>().onClick.Invoke();
            connection.HandlePointerClick(Camera.main.WorldToScreenPoint(frontend.transform.position));
            Assert.That(connection.SourceNode, Is.EqualTo(frontend));
            connection.HandlePointerClick(Camera.main.WorldToScreenPoint(database.transform.position));
            Assert.That(graph.Connections, Is.Empty);
            Assert.That(error, Does.Contain("Backend"));
            Assert.That(connection.SourceNode, Is.EqualTo(frontend), "Invalid target must leave the source available for retry.");
            connection.HandlePointerClick(Camera.main.WorldToScreenPoint(backend.transform.position));
            Assert.That(graph.Connections.Count, Is.EqualTo(1));
            Assert.That(graph.Connections[0].Source, Is.EqualTo(frontend));
            Assert.That(graph.Connections[0].Target, Is.EqualTo(backend));
            yield return null;
            Assert.That(Object.FindFirstObjectByType<InteractionManager>().CurrentMode, Is.EqualTo(InteractionManager.Mode.Select));
        }

        [UnityTest]
        public IEnumerator PacketsFollowVisibleEdgePortsAndDisappearWhenTheEdgeIsRemoved()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity");
            yield return null;
            var graph = Object.FindFirstObjectByType<ArchitectureGraph>();
            var simulation = Object.FindFirstObjectByType<SimulationController>();
            var source = graph.AddNode(NodeType.Frontend, new Vector3(-.7f, .15f, 0));
            var target = graph.AddNode(NodeType.Backend, new Vector3(.7f, .2f, 0));
            var edge = graph.AddConnection(source, target);
            for (int i = 0; i < 5; i++) simulation.StepOnce();
            var packet = Object.FindFirstObjectByType<RequestPacketVisual>();
            Assert.That(packet, Is.Not.Null);
            target.transform.position += Vector3.forward * .2f;
            target.transform.Rotate(Vector3.up, 45);
            Physics.SyncTransforms();
            yield return null;
            ConnectionRenderer.GetEndpoints(source.transform, target.transform, out Vector3 a, out Vector3 b);
            double time = simulation.Engine.Time;
            packet.Advance(.05f);
            Vector3 projection = a + Vector3.Project(packet.transform.position - a, b - a);
            Assert.That(Vector3.Distance(projection, packet.transform.position), Is.LessThan(.001f));
            Assert.That(Vector3.Dot(packet.transform.position - a, b - a), Is.GreaterThan(0));
            Assert.That(simulation.Engine.Time, Is.EqualTo(time), "Presentation cannot drive simulation state.");
            graph.RemoveConnection(edge);
            yield return null;
            Assert.That(Object.FindObjectsByType<RequestPacketVisual>(FindObjectsSortMode.None), Is.Empty);
        }

        [UnityTest]
        public IEnumerator PacketColorsReflectLogicalSuccessAndFailure()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity");
            yield return null;
            var graph = Object.FindFirstObjectByType<ArchitectureGraph>();
            var simulation = Object.FindFirstObjectByType<SimulationController>();
            var source = graph.AddNode(NodeType.Frontend, new Vector3(-.8f, .15f, 0));
            var target = graph.AddNode(NodeType.Backend, new Vector3(.8f, .2f, 0));
            graph.AddConnection(source, target);
            bool successColor = false, failureColor = false;
            simulation.Run();
            for (int i = 0; i < 40; i++)
            {
                if (i == 20) simulation.FailNode(target);
                yield return new WaitForSeconds(.025f);
                foreach (var packet in Object.FindObjectsByType<RequestPacketVisual>(FindObjectsSortMode.None))
                {
                    successColor |= packet.CurrentColor == RequestPacketVisual.Success;
                    failureColor |= packet.CurrentColor == RequestPacketVisual.Failure;
                }
            }
            simulation.Pause();
            Assert.That(simulation.Engine.Succeeded, Is.GreaterThan(0));
            Assert.That(simulation.Engine.Failed, Is.GreaterThan(0));
            Assert.That(successColor, Is.True);
            Assert.That(failureColor, Is.True);
            Assert.That(source.Status, Is.Not.EqualTo(NodeStatus.Crashed));
        }
    }
}
