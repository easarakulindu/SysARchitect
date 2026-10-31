using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using SysARchitect.Core;
using SysARchitect.Graph;
using SysARchitect.Interaction;
using SysARchitect.Simulation;
using SysARchitect.Visualization;

namespace SysARchitect.Tests
{
    public class ModelIndicatorTests
    {
        [UnityTest]
        public IEnumerator DetailedModelsPlaceAndMoveOnDesktopAndScaledWorkspaceSurfaces()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity"); yield return null;
            var graph = Object.FindAnyObjectByType<ArchitectureGraph>();
            var placement = Object.FindAnyObjectByType<NodePlacementController>(); placement.UseExternalPointer = true;
            var movement = Object.FindAnyObjectByType<NodeMovementController>(); movement.UseExternalPointer = true;
            var selection = Object.FindAnyObjectByType<NodeSelectionController>();
            foreach (float scale in new[] { 1f, .35f })
            {
                graph.transform.localScale = Vector3.one * scale;
                foreach (NodeType type in System.Enum.GetValues(typeof(NodeType)))
                {
                    placement.BeginPlacement(type);
                    var preview = Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                        .Single(filter => filter.gameObject.name == "Preview_" + type);
                    var previewMaterial = preview.GetComponent<Renderer>().sharedMaterial;
                    Assert.That(previewMaterial.color.a, Is.EqualTo(.5f).Within(.001));
                    Assert.That(previewMaterial.renderQueue, Is.EqualTo(3000));
                    Assert.That(placement.PlaceOnSurface(Vector3.zero, Vector3.up), Is.True);
                    var node = graph.Nodes[graph.Nodes.Count - 1]; Physics.SyncTransforms();
                    Assert.That(node.GetComponent<BoxCollider>().bounds.min.y, Is.EqualTo(0).Within(.001), type + " placement");
                    Assert.That(node.GetComponent<MeshFilter>().sharedMesh.name, Does.Contain("Holographic Hardware"));
                    Assert.That(node.GetComponent<Renderer>().sharedMaterial.shader.isSupported, Is.True);
                    var gauges = node.transform.Find("CPU and RAM indicators (top to bottom)");
                    Assert.That(gauges.Find("CPU track").localScale.x, Is.LessThanOrEqualTo(node.GetComponent<BoxCollider>().size.x));
                    selection.Select(node.GetComponent<NodeVisual>());
                    node.transform.Rotate(Vector3.up, 45);
                    movement.MoveSelectedOnSurface(new Vector3(.1f, 0, .1f), Vector3.up); Physics.SyncTransforms();
                    Assert.That(node.GetComponent<BoxCollider>().bounds.min.y, Is.EqualTo(0).Within(.001), type + " movement");
                    graph.RemoveNode(node); yield return null;
                }
            }
        }

        [UnityTest]
        public IEnumerator GaugesFollowPublishedSimulationResourcesResetAndFailure()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity"); yield return null;
            var graph = Object.FindAnyObjectByType<ArchitectureGraph>();
            var sim = Object.FindAnyObjectByType<SimulationController>();
            var frontend = graph.AddNode(NodeType.Frontend, new Vector3(-.4f, .22f, 0));
            var backend = graph.AddNode(NodeType.Backend, new Vector3(.4f, .21f, 0));
            backend.ProcessingCapacity = 40; backend.MemoryLimitMB = 256;
            graph.AddConnection(frontend, backend); sim.Engine.RequestsPerSecond = 40;
            for (int i = 0; i < 80; i++) sim.StepOnce();
            var state = sim.Engine.FindNode(backend.NodeId);
            Assert.That(sim.Engine.Succeeded, Is.GreaterThan(0)); Assert.That(state.Cpu, Is.GreaterThan(.1));
            Assert.That(backend.CpuUtilization, Is.EqualTo(state.Cpu).Within(.0001));
            yield return new WaitForSecondsRealtime(.25f);
            var gauges = backend.transform.Find("CPU and RAM indicators (top to bottom)");
            float width = gauges.Find("CPU track").localScale.x;
            Assert.That(gauges.Find("CPU utilization").localScale.x / width, Is.EqualTo(state.Cpu).Within(.001));
            Assert.That(gauges.Find("RAM utilization").localScale.x / width,
                Is.EqualTo(state.MemoryMB / backend.MemoryLimitMB).Within(.001));
            DesktopWorkspaceTests.CaptureDemo("sysarchitect-model-indicator-review.png");
            sim.ResetSimulation(); yield return new WaitForSecondsRealtime(.25f);
            Assert.That(gauges.Find("CPU utilization").gameObject.activeSelf, Is.False, "Zero CPU must show an empty gauge.");
            Assert.That(gauges.Find("RAM utilization").localScale.x / width,
                Is.EqualTo(state.MemoryMB / backend.MemoryLimitMB).Within(.001), "Reset retains actual baseline memory.");
            sim.FailNode(backend); yield return null;
            var label = backend.GetComponentInChildren<TextMeshPro>();
            Assert.That(label.text, Does.Contain("Crashed")); Assert.That(label.color, Is.EqualTo(NodeColorPalette.Crashed));
        }

        [UnityTest]
        public IEnumerator RemovingAModelReleasesItsOwnedMeshAndMaterialOnly()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity"); yield return null;
            var graph = Object.FindAnyObjectByType<ArchitectureGraph>();
            var removed = graph.AddNode(NodeType.Frontend, Vector3.zero);
            var retained = graph.AddNode(NodeType.Frontend, Vector3.right);
            var mesh = removed.GetComponent<MeshFilter>().sharedMesh;
            var material = removed.GetComponent<Renderer>().sharedMaterial;
            var retainedMaterial = retained.GetComponent<Renderer>().sharedMaterial;
            Assert.That(material, Is.Not.SameAs(retainedMaterial));
            graph.RemoveNode(removed); yield return null; yield return null;
            Assert.That(mesh == null, Is.True); Assert.That(material == null, Is.True);
            Assert.That(retainedMaterial != null, Is.True); Assert.That(retained.GetComponent<Renderer>().sharedMaterial.shader.isSupported, Is.True);
        }
    }
}
