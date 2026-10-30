using System.Collections;
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
            Assert.That(toolbox.GetComponentsInChildren<Button>().Length, Is.EqualTo(8));
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
