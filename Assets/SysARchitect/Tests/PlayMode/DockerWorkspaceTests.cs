using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SysARchitect.Components;
using SysARchitect.Core;
using SysARchitect.Graph;
using SysARchitect.Interaction;
using SysARchitect.Visualization;

namespace SysARchitect.Tests
{
    public class DockerWorkspaceTests
    {
        [UnityTest]
        public IEnumerator HostUIShowsActualContainersAndRemovalCleansGraphEdges()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity"); yield return null;
            var graph = Object.FindAnyObjectByType<ArchitectureGraph>();
            var host = graph.AddNode(NodeType.DockerHost, new Vector3(0, .2f, 0));
            Object.FindAnyObjectByType<NodeSelectionController>().Select(host.GetComponent<NodeVisual>()); yield return null; yield return null;
            var panel = GameObject.Find("Workspace UI/Docker Explorer"); Assert.That(panel, Is.Not.Null);
            panel.transform.Find("Create container").GetComponent<Button>().onClick.Invoke();
            panel.transform.Find("Create container").GetComponent<Button>().onClick.Invoke(); yield return null;
            Assert.That(graph.Nodes.Count, Is.EqualTo(3)); Assert.That(graph.Connections.Count, Is.EqualTo(2));
            Assert.That(graph.Nodes[1].HostNodeId, Is.EqualTo(host.NodeId));
            var docker = Object.FindAnyObjectByType<DockerWorkspaceController>();
            Assert.That(docker.GetHost(host).Containers.Count, Is.EqualTo(2));
            Assert.That(host.GetComponent<WorkloadEnclosure>().Expanded, Is.True);
            // The enclosure's base collider must not hide the actual container hit targets.
            Physics.SyncTransforms();
            var selection = Object.FindAnyObjectByType<NodeSelectionController>();
            selection.HandlePointerClick(Camera.main.WorldToScreenPoint(graph.Nodes[1].transform.position));
            Assert.That(selection.SelectedNode.GetGraphNode(), Is.EqualTo(graph.Nodes[1]));
            yield return null;
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null) DesktopWorkspaceTests.CaptureDemo("sysarchitect-docker.png");
            graph.RemoveNode(host); yield return null;
            Assert.That(graph.Nodes, Is.Empty); Assert.That(graph.Connections, Is.Empty);
        }
    }
}
