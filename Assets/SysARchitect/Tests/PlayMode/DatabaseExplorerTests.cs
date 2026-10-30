using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
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
    public class DatabaseExplorerTests
    {
        private static void Click(string label) => GameObject.Find("Workspace UI/Database Explorer")
            .GetComponentsInChildren<Button>().Single(b => b.GetComponentInChildren<TextMeshProUGUI>().text == label).onClick.Invoke();

        [UnityTest]
        public IEnumerator OneToolboxDatabaseExpandsReplicasAndSyncsQueuedWrites()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity");
            yield return null;
            var graph = Object.FindFirstObjectByType<ArchitectureGraph>();
            var simulation = Object.FindFirstObjectByType<SimulationController>();
            var selection = Object.FindFirstObjectByType<NodeSelectionController>();
            var node = graph.AddNode(NodeType.Database, new Vector3(0, .125f, -.5f));
            selection.Select(node.GetComponent<NodeVisual>());
            yield return null;
            Click("Replication");
            yield return null;
            var model = simulation.Engine.FindNode(node.NodeId).Database;
            var visual = node.GetComponent<DatabaseClusterVisual>();
            Assert.That(graph.Nodes.Count, Is.EqualTo(1));
            Assert.That(visual.VisibleMemberCount, Is.EqualTo(3));
            Assert.That(node.GetComponentsInChildren<GraphNode>().Length, Is.EqualTo(1));
            Click("Write");
            simulation.StepOnce(); simulation.StepOnce();
            yield return null;
            Assert.That(model.Members[0].Records.ContainsKey(1), Is.True);
            Assert.That(model.Members[1].Records.ContainsKey(1), Is.False);
            Assert.That(model.PendingUpdates, Is.GreaterThan(0));
            Assert.That(node.GetComponentsInChildren<RequestPacketVisual>().Length, Is.GreaterThan(0), "Writes must animate on internal primary-replica links.");
            for (int i = 0; i < 12; i++) simulation.StepOnce();
            yield return null;
            Assert.That(model.Members.All(m => m.Records.ContainsKey(1)), Is.True);
            Assert.That(simulation.Engine.Succeeded, Is.EqualTo(1));
            Click("Add replica");
            yield return null;
            Assert.That(visual.VisibleMemberCount, Is.EqualTo(4));
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                DesktopWorkspaceTests.CaptureDemo("sysarchitect-database-replication.png");
            simulation.ResetSimulation();
            yield return null;
            yield return null;
            Assert.That(node.GetComponentsInChildren<RequestPacketVisual>(), Is.Empty, "Reset must clear internal database animations too.");
            Click("Expand / Collapse");
            yield return null;
            Assert.That(visual.VisibleMemberCount, Is.Zero);
            Assert.That(node.GetComponent<MeshRenderer>().enabled, Is.True);
            Assert.That(node.GetComponent<BoxCollider>().size.x, Is.EqualTo(.3f).Within(.001));
            Assert.That(GameObject.Find("Workspace UI/Toolbox/Components").GetComponentsInChildren<Button>()
                .Count(b => b.GetComponentInChildren<TextMeshProUGUI>().text == "Database"), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ShardFailureAndRecoveryRetainSharedResourceAndRequestCounters()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity");
            yield return null;
            var graph = Object.FindFirstObjectByType<ArchitectureGraph>();
            var simulation = Object.FindFirstObjectByType<SimulationController>();
            var node = graph.AddNode(NodeType.Database, new Vector3(0, .125f, -.5f));
            Object.FindFirstObjectByType<NodeSelectionController>().Select(node.GetComponent<NodeVisual>());
            yield return null;
            Click("Sharding");
            yield return null;
            var model = simulation.Engine.FindNode(node.NodeId).Database;
            Assert.That(model.Members.Count, Is.EqualTo(3));
            GameObject.Find("Workspace UI/Database Explorer/Slider").GetComponent<Slider>().value = 1;
            Click("Fail member");
            Click("Write"); // Default key 1 routes to shard 1.
            simulation.StepOnce(); simulation.StepOnce();
            Assert.That(simulation.Engine.Failed, Is.EqualTo(1));
            Assert.That(simulation.Engine.LastFailureReason, Does.Contain("Shard 1"));
            Assert.That(node.Status, Is.Not.EqualTo(NodeStatus.Crashed));
            Click("Recover member");
            Assert.That(model.Members[1].Status, Is.EqualTo(NodeStatus.Recovering));
            for (int i = 0; i < 21; i++) simulation.StepOnce();
            Click("Write"); simulation.StepOnce(); simulation.StepOnce();
            yield return null;
            Assert.That(model.Members[1].Records.ContainsKey(1), Is.True);
            Assert.That(simulation.Engine.Succeeded, Is.EqualTo(1));
            Assert.That(model.Members.Where(m => m.Index != 1).All(m => m.Records.Count == 0), Is.True);
            Click("+ shard");
            yield return null;
            Assert.That(model.Members.Count, Is.EqualTo(4));
            Assert.That(node.GetComponent<DatabaseClusterVisual>().VisibleMemberCount, Is.EqualTo(4));
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                DesktopWorkspaceTests.CaptureDemo("sysarchitect-database-sharding.png");
            simulation.ResetSimulation();
            yield return null;
            Assert.That(model.Mode, Is.EqualTo(DatabaseMode.Sharding));
            Assert.That(model.LogicalRecordCount, Is.Zero);
            Assert.That(simulation.Engine.Generated, Is.Zero);
        }
    }
}
