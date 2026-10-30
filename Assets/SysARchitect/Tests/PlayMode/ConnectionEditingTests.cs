using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SysARchitect.Core;
using SysARchitect.Graph;
using SysARchitect.Interaction;
using SysARchitect.Simulation;
using SysARchitect.Visualization;

namespace SysARchitect.Tests
{
    public class ConnectionEditingTests
    {
        private static void Click(string path) => GameObject.Find(path).GetComponent<Button>().onClick.Invoke();
        private static void Drain(SimulationController simulation)
        {
            for (int i = 0; i < 200 && simulation.Engine.InFlight > 0; i++) simulation.StepOnce();
            Assert.That(simulation.Engine.InFlight, Is.Zero);
        }
        [UnityTest]
        public IEnumerator EdgePickingDeletionReconnectionAndNodeCleanupPreserveTheNodes()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity"); yield return null;
            var graph = Object.FindAnyObjectByType<ArchitectureGraph>();
            var select = Object.FindAnyObjectByType<NodeSelectionController>();
            var sim = Object.FindAnyObjectByType<SimulationController>(); sim.Engine.RequestsPerSecond = 0;
            var f = graph.AddNode(NodeType.Frontend, new Vector3(-.7f, .15f, 0));
            var b = graph.AddNode(NodeType.Backend, new Vector3(.7f, .2f, 0));
            var edge = graph.AddConnection(f, b); Physics.SyncTransforms(); yield return null;
            ConnectionRenderer.GetEndpoints(f.transform, b.transform, out Vector3 a, out Vector3 z);
            select.HandlePointerClick(Camera.main.WorldToScreenPoint((a + z) * .5f));
            Assert.That(select.SelectedConnection, Is.EqualTo(edge)); Assert.That(select.SelectedNode, Is.Null);
            Assert.That(edge.GetComponent<ConnectionRenderer>().IsSelected, Is.True);
            yield return null;
            Assert.That(edge.GetComponent<LineRenderer>().widthMultiplier, Is.GreaterThan(.02f));
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null) DesktopWorkspaceTests.CaptureDemo("sysarchitect-selected-connection.png");
            Click("Workspace UI/Toolbox/Delete Connection"); yield return null;
            Assert.That(graph.Nodes.Count, Is.EqualTo(2)); Assert.That(graph.Connections, Is.Empty);
            Assert.That(select.SelectedConnection, Is.Null);
            sim.Engine.SubmitRequest(f.NodeId, false, 1); Drain(sim);
            Assert.That(sim.Engine.LastFailureReason, Is.EqualTo("No service route configured"));
            Click("Workspace UI/Toolbox/Connect source > target");
            var connect = Object.FindAnyObjectByType<ConnectionCreationController>();
            connect.HandlePointerClick(Camera.main.WorldToScreenPoint(f.transform.position));
            connect.HandlePointerClick(Camera.main.WorldToScreenPoint(b.transform.position));
            yield return null;
            Assert.That(graph.Connections.Count, Is.EqualTo(1));
            sim.Engine.SubmitRequest(f.NodeId, false, 1); Drain(sim);
            Assert.That(sim.Engine.Succeeded, Is.EqualTo(1));
            select.SelectConnection(graph.Connections[0]); graph.RemoveNode(b); yield return null;
            Assert.That(graph.Nodes.Count, Is.EqualTo(1)); Assert.That(graph.Connections, Is.Empty);
            Assert.That(select.SelectedConnection, Is.Null);
        }

        [UnityTest]
        public IEnumerator NodePickingTakesPriorityAndTypingDoesNotDeleteAnEdge()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity"); yield return null;
            var graph = Object.FindAnyObjectByType<ArchitectureGraph>();
            var selection = Object.FindAnyObjectByType<NodeSelectionController>();
            var manager = Object.FindAnyObjectByType<InteractionManager>();
            var f = graph.AddNode(NodeType.Frontend, new Vector3(-.6f, .15f, 0));
            var b = graph.AddNode(NodeType.Backend, new Vector3(.6f, .2f, 0));
            var edge = graph.AddConnection(f, b); Physics.SyncTransforms();
            selection.HandlePointerClick(Camera.main.WorldToScreenPoint(b.transform.position));
            Assert.That(selection.SelectedNode.GetGraphNode(), Is.EqualTo(b)); Assert.That(selection.SelectedConnection, Is.Null);
            selection.SelectConnection(edge);
            var input = new GameObject("Typing", typeof(RectTransform), typeof(InputField));
            input.transform.SetParent(GameObject.Find("Workspace UI").transform, false);
            EventSystem.current.SetSelectedGameObject(input);
            Assert.That(manager.HandleDeleteShortcut(), Is.False); Assert.That(graph.Connections.Count, Is.EqualTo(1));
            EventSystem.current.SetSelectedGameObject(null);
            Assert.That(manager.HandleDeleteShortcut(), Is.True); yield return null;
            Assert.That(graph.Connections, Is.Empty); Assert.That(graph.Nodes.Count, Is.EqualTo(2));
            Object.Destroy(input);
        }

        [UnityTest]
        public IEnumerator ConfigurableRoleSupportsBothLearningArchitecturesAndRejectsMixedLinks()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity"); yield return null;
            var graph = Object.FindAnyObjectByType<ArchitectureGraph>();
            var selection = Object.FindAnyObjectByType<NodeSelectionController>();
            var sim = Object.FindAnyObjectByType<SimulationController>();
            var f = graph.AddNode(NodeType.Frontend, new Vector3(-.8f, .15f, 0));
            var lb = graph.AddNode(NodeType.LoadBalancer, new Vector3(-.35f, .05f, 0));
            var b = graph.AddNode(NodeType.Backend, new Vector3(.1f, .2f, -.3f));
            var db = graph.AddNode(NodeType.Database, new Vector3(.7f, .125f, 0));
            Assert.That(graph.ValidateConnection(b, lb).errorMessage, Does.Contain("Database Router"));
            selection.Select(lb.GetComponent<NodeVisual>()); yield return null;
            Click("Workspace UI/Routing Inspector/Database Router");
            Assert.That(lb.RoutingRole, Is.EqualTo(LoadBalancerRole.Database));
            Assert.That(sim.Engine.FindNode(lb.NodeId).RoutingRole, Is.EqualTo(LoadBalancerRole.Database));
            var inbound = graph.AddConnection(b, lb); var outbound = graph.AddConnection(lb, db);
            Assert.That(inbound, Is.Not.Null); Assert.That(outbound, Is.Not.Null);
            Assert.That(graph.ValidateConnection(f, lb).isValid, Is.False);
            Assert.That(graph.TrySetRoutingRole(lb, LoadBalancerRole.Application, out string message), Is.False);
            Assert.That(message, Does.Contain("Remove incompatible")); Assert.That(graph.Connections.Count, Is.EqualTo(2));
            graph.RemoveConnection(inbound); graph.RemoveConnection(outbound);
            Assert.That(graph.TrySetRoutingRole(lb, LoadBalancerRole.Application, out _), Is.True);
            var b2 = graph.AddNode(NodeType.Backend, new Vector3(.1f, .2f, .3f));
            graph.AddConnection(f, lb); graph.AddConnection(lb, b); graph.AddConnection(lb, b2);
            graph.AddConnection(b, db); graph.AddConnection(b2, db);
            Assert.That(graph.ValidateGraph(), Is.Empty);
            sim.Engine.RequestsPerSecond = 160;
            for (int i = 0; i < 80; i++) sim.StepOnce();
            Assert.That(sim.Engine.Failed, Is.Zero);
            Assert.That(sim.Engine.FindNode(b.NodeId).Completed, Is.EqualTo(sim.Engine.FindNode(b2.NodeId).Completed).Within(1));
            selection.Select(lb.GetComponent<NodeVisual>()); yield return null;
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null) DesktopWorkspaceTests.CaptureDemo("sysarchitect-application-routing.png");
        }

        [UnityTest]
        public IEnumerator CacheAsidePathsAndInspectorReflectActualSimulation()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity"); yield return null;
            var graph = Object.FindAnyObjectByType<ArchitectureGraph>();
            var sim = Object.FindAnyObjectByType<SimulationController>(); sim.Engine.RequestsPerSecond = 0;
            var f = graph.AddNode(NodeType.Frontend, new Vector3(-.8f, .15f, 0));
            var lb = graph.AddNode(NodeType.LoadBalancer, new Vector3(-.4f, .05f, 0));
            var b = graph.AddNode(NodeType.Backend, new Vector3(0, .2f, 0));
            var cache = graph.AddNode(NodeType.Cache, new Vector3(.6f, .06f, -.35f));
            var db = graph.AddNode(NodeType.Database, new Vector3(.6f, .125f, .35f));
            graph.AddConnection(f, lb); graph.AddConnection(lb, b);
            var cacheEdge = graph.AddConnection(b, cache); var storageEdge = graph.AddConnection(b, db);
            sim.Engine.SubmitDatabaseRequest(db.NodeId, true, 1); Drain(sim);
            sim.Run(); sim.Pause();
            sim.Engine.SubmitRequest(f.NodeId, false, 1); Drain(sim);
            Assert.That(storageEdge.GetComponent<ConnectionRenderer>().LastActivity, Is.EqualTo("CacheMiss"));
            Assert.That(cacheEdge.GetComponent<ConnectionRenderer>().LastActivity, Is.EqualTo("CacheFill"));
            sim.Engine.SubmitRequest(f.NodeId, false, 1); Drain(sim);
            Assert.That(cacheEdge.GetComponent<ConnectionRenderer>().LastActivity, Is.EqualTo("Cache hit"));
            Assert.That(sim.Engine.FindNode(db.NodeId).Database.Reads, Is.EqualTo(1));
            Object.FindAnyObjectByType<NodeSelectionController>().Select(cache.GetComponent<NodeVisual>()); yield return null;
            Assert.That(GameObject.Find("Workspace UI/Routing Inspector"), Is.Not.Null);
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null) DesktopWorkspaceTests.CaptureDemo("sysarchitect-cache-aside.png");
            graph.RemoveConnection(cacheEdge);
            sim.Engine.SubmitRequest(f.NodeId, false, 1); Drain(sim); yield return null;
            Assert.That(sim.Engine.FindNode(db.NodeId).Database.Reads, Is.EqualTo(2));
            Assert.That(sim.Engine.Failed, Is.Zero);
        }

        [UnityTest]
        public IEnumerator ApplicationAndDatabaseRoutersShareTheExistingExpandableCluster()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity"); yield return null;
            var graph = Object.FindAnyObjectByType<ArchitectureGraph>();
            var sim = Object.FindAnyObjectByType<SimulationController>(); sim.Engine.RequestsPerSecond = 0;
            var f = graph.AddNode(NodeType.Frontend, new Vector3(-.9f, .15f, 0));
            var app = graph.AddNode(NodeType.LoadBalancer, new Vector3(-.5f, .05f, 0));
            var b1 = graph.AddNode(NodeType.Backend, new Vector3(-.05f, .2f, -.3f));
            var b2 = graph.AddNode(NodeType.Backend, new Vector3(-.05f, .2f, .3f));
            var router = graph.AddNode(NodeType.LoadBalancer, new Vector3(.45f, .05f, 0));
            var db = graph.AddNode(NodeType.Database, new Vector3(.85f, .125f, -.4f));
            db.ProcessingCapacity = 50;
            Assert.That(graph.TrySetRoutingRole(router, LoadBalancerRole.Database, out _), Is.True);
            graph.AddConnection(f, app); graph.AddConnection(app, b1); graph.AddConnection(app, b2);
            graph.AddConnection(b1, router); graph.AddConnection(b2, router); graph.AddConnection(router, db);
            Assert.That(graph.ValidateGraph(), Is.Empty);
            var model = sim.Engine.FindNode(db.NodeId).Database;
            model.Configure(DatabaseMode.Replication, 2);
            sim.Engine.SubmitDatabaseRequest(db.NodeId, true, 1); Drain(sim);
            db.GetComponent<DatabaseClusterVisual>().Expanded = true;
            for (int i = 0; i < 12; i++) sim.StepOnce();
            for (int i = 0; i < 4; i++) sim.Engine.SubmitRequest(f.NodeId, true, 1);
            for (int i = 0; i < 12; i++) sim.Engine.SubmitRequest(f.NodeId, false, 1);
            Drain(sim); yield return null;
            Assert.That(sim.Engine.Succeeded, Is.EqualTo(17)); Assert.That(sim.Engine.Failed, Is.Zero);
            Assert.That(model.Members[0].Writes, Is.EqualTo(5));
            Assert.That(model.Members.Skip(1).All(m => m.Writes == 0 && m.Reads > 0), Is.True);
            Assert.That(model.Members.Sum(m => m.Reads), Is.EqualTo(12));
            var selection = Object.FindAnyObjectByType<NodeSelectionController>();
            selection.Select(router.GetComponent<NodeVisual>()); yield return null;
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null) DesktopWorkspaceTests.CaptureDemo("sysarchitect-database-router.png");
            selection.Select(db.GetComponent<NodeVisual>()); yield return null;
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null) DesktopWorkspaceTests.CaptureDemo("sysarchitect-database-router-members.png");
        }
    }
}
