using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.XR.Simulation;
using SysARchitect.AR;
using SysARchitect.Core;
using SysARchitect.Graph;
using SysARchitect.Interaction;
using SysARchitect.Lessons;
using SysARchitect.Simulation;
using SysARchitect.UI;

namespace SysARchitect.Tests
{
    public class XRSimulationDemonstrationTests
    {
        private sealed class MemoryStore : IProgressStorage
        { public string Read() => ""; public void Write(string json) { } }
        private static Button Named(string name) => Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b => b.name == name);
        private static void Click(Button button) => ExecuteEvents.Execute(button.gameObject,
            new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
        private static void Click(string name) => Click(Named(name));
        private static IEnumerator Tap(WorkspaceTouchInput adapter, Touchscreen screen, Vector3 point)
        {
            Assert.That(screen.enabled, Is.True, "Editor focus must not disable the synthetic touchscreen");
            Vector2 position = Camera.main.WorldToScreenPoint(point);
            Assert.That(NodeSelectionController.IsOverUI(position), Is.False, "Demonstration gesture must reach the real workspace");
            InputSystem.QueueStateEvent(screen, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Began, position = position });
            yield return null; // Natural dynamic input/update, including the production adapter.Update.
            InputSystem.QueueStateEvent(screen, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = position });
            yield return null;

        }
        private static void Capture(string name)
        {
            var camera = Camera.main; var canvas = GameObject.Find("Workspace UI").GetComponent<Canvas>();
            var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera; float oldDistance = canvas.planeDistance;
            var target = new RenderTexture(Screen.width, Screen.height, 24); var previous = RenderTexture.active;
            Texture2D pixels = null;
            try
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = .3f;
                Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target; pixels = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); pixels.Apply();
                File.WriteAllBytes(Path.Combine(Application.temporaryCachePath, "sysarchitect-xr-demo-" + name + ".png"), pixels.EncodeToPNG());
                Debug.Log("XR demo capture: " + Path.Combine(Application.temporaryCachePath, "sysarchitect-xr-demo-" + name + ".png"));
            }
            finally
            {
                RenderTexture.active = previous; if (pixels != null) Object.Destroy(pixels); target.Release(); Object.Destroy(target);
                canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; canvas.planeDistance = oldDistance;
            }
        }
        [UnityTest]
        public IEnumerator RealXRWorkspaceRepositionsBuildsThreeTiersRunsTrafficAndShowsLessonSteps()
        {
            yield return XRSimulationLifecycleTests.ReloadProvider();
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/ARWorkspace.unity");
            yield return XRSimulationLifecycleTests.Scan(); yield return XRSimulationLifecycleTests.Place();
            var workspace = Object.FindAnyObjectByType<ARWorkspace>();
            Click("Reposition workspace"); yield return XRSimulationLifecycleTests.Scan(); yield return XRSimulationLifecycleTests.Place();
            var graph = Object.FindAnyObjectByType<ArchitectureGraph>(); var adapter = Object.FindAnyObjectByType<WorkspaceTouchInput>();
            var simulation = Object.FindAnyObjectByType<SimulationController>();
            var pose = Object.FindAnyObjectByType<SimulationCameraPoseProvider>();
            pose.transform.SetPositionAndRotation(graph.transform.position + new Vector3(0, .75f, -.65f), Quaternion.LookRotation(new Vector3(0, -.75f, .65f)));
            float deadline = Time.realtimeSinceStartup + 5;
            while ((Vector3.Distance(Camera.main.transform.position, pose.transform.position) > .01f || Quaternion.Angle(Camera.main.transform.rotation, pose.transform.rotation) > .5f) && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(workspace.CanInteract, Is.True);
            var oldUpdate = InputSystem.settings.updateMode; var oldBackground = InputSystem.settings.backgroundBehavior;
            var oldEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            Touchscreen screen = null; bool oldEnabled = adapter.enabled;
            try
            {
                InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                screen = InputSystem.AddDevice<Touchscreen>("XR demonstration touch");
                adapter.enabled = true; // Normal Update reads synthetic device events through the production reader.
                var types = new[] { NodeType.Frontend, NodeType.Backend, NodeType.Database };
                var locations = new[] { new Vector3(-.65f, 0, .35f), Vector3.zero, new Vector3(.65f, 0, .35f) };
                for (int i = 0; i < types.Length; i++)
                {
                    Click("Toolbox"); yield return null; Click("Components"); yield return null;
                    Click(Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b => b.GetComponentInChildren<TextMeshProUGUI>()?.text == types[i].ToString()));
                    Click("Toolbox"); yield return null;
                    yield return Tap(adapter, screen, graph.transform.TransformPoint(locations[i]));
                    Assert.That(graph.Nodes.Count, Is.EqualTo(i + 1)); Click("Hide details"); yield return null;
                }
                var nodes = types.Select(t => graph.Nodes.Single(n => n.Type == t)).ToArray(); Physics.SyncTransforms();
                for (int i = 0; i < 2; i++)
                {
                    Click("Toolbox"); yield return null; Click("Edit / Connect"); yield return null;
                    Click("Connect source > target"); Click("Toolbox"); yield return null;
                    yield return Tap(adapter, screen, nodes[i].transform.position);
                    yield return Tap(adapter, screen, nodes[i + 1].transform.position);
                    Assert.That(graph.Connections.Count, Is.EqualTo(i + 1));
                    Assert.That(graph.Connections[i].Source, Is.SameAs(nodes[i])); Assert.That(graph.Connections[i].Target, Is.SameAs(nodes[i + 1]));
                }
                var paths = new HashSet<string>(); simulation.Engine.OnTransfer += (_, from, to) => paths.Add(from + ">" + to);
                simulation.Engine.RequestsPerSecond = 30;
                Click("Simulation"); yield return null; Click("Run");
                deadline = Time.realtimeSinceStartup + 10;
                while (simulation.Engine.Succeeded < 20 && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(simulation.IsRunning, Is.True); Assert.That(simulation.Engine.Succeeded, Is.GreaterThanOrEqualTo(20));
                Assert.That(paths, Does.Contain(nodes[0].NodeId + ">" + nodes[1].NodeId).And.Contain(nodes[1].NodeId + ">" + nodes[2].NodeId));
                Click("Pause"); yield return null;
                Assert.That(simulation.Engine.FindNode(nodes[2].NodeId).Database.Reads, Is.GreaterThan(0));
                Assert.That(simulation.Engine.FindNode(nodes[2].NodeId).Database.Writes, Is.GreaterThan(0));
                Assert.That(simulation.Engine.Generated, Is.EqualTo(simulation.Engine.Succeeded + simulation.Engine.Failed + simulation.Engine.InFlight));
                deadline = Time.realtimeSinceStartup + 2;
                bool published = false;
                while (!published && Time.realtimeSinceStartup < deadline)
                {
                    published = nodes.All(node =>
                    {
                        var gauges = node.transform.Find("CPU and RAM indicators (top to bottom)");
                        var state = simulation.Engine.FindNode(node.NodeId); float width = gauges.Find("CPU track").localScale.x;
                        return System.Math.Abs(gauges.Find("CPU utilization").localScale.x / width - state.Cpu) < .001 &&
                            System.Math.Abs(gauges.Find("RAM utilization").localScale.x / width - state.MemoryMB / node.MemoryLimitMB) < .001;
                    });
                    if (!published) yield return null;
                }
                Assert.That(published, Is.True, "Wait for the existing resource presentation refresh, without advancing paused simulation time");
                foreach (var node in nodes)
                {
                    var state = simulation.Engine.FindNode(node.NodeId); Assert.That(state.Cpu, Is.GreaterThan(0));
                    Assert.That(node.CpuUtilization, Is.EqualTo(state.Cpu).Within(.0001));
                    var gauges = node.transform.Find("CPU and RAM indicators (top to bottom)"); float width = gauges.Find("CPU track").localScale.x;
                    Assert.That(gauges.Find("CPU utilization").localScale.x / width, Is.EqualTo(state.Cpu).Within(.001));
                    Assert.That(gauges.Find("RAM utilization").localScale.x / width, Is.EqualTo(state.MemoryMB / node.MemoryLimitMB).Within(.001));
                }
                Capture("traffic"); Click("Simulation"); yield return null; Capture("architecture");
                var lessons = Object.FindAnyObjectByType<LessonController>(); lessons.UseProgress(new MemoryStore());
                Click("Lessons / Progress"); yield return null; yield return null;
                var ui = Object.FindAnyObjectByType<LessonUI>();
                var body = (TextMeshProUGUI)typeof(LessonUI).GetField("body", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui);
                string original = body.text; Click("Next step"); yield return null; yield return null;
                Assert.That(body.text, Is.Not.EqualTo(original)); Assert.That(body.text, Does.Contain(lessons.Current.StepDetails[1].Heading));
                Assert.That(ui.InstructionStep, Is.EqualTo(1)); Capture("lesson");
                Click("Previous step"); yield return null; yield return null; Assert.That(body.text, Is.EqualTo(original));
                Assert.That(graph.Nodes.Count, Is.EqualTo(3)); Assert.That(graph.Connections.Count, Is.EqualTo(2)); Assert.That(workspace.CanInteract, Is.True);
                Debug.Log("XR demonstration: reposition, three touch-placed nodes, two picked edges, " + simulation.Engine.Succeeded + " successes, real CPU/RAM gauges and forward/backward instruction content verified.");
            }
            finally
            {
                simulation.Pause(); adapter.enabled = oldEnabled; if (screen != null) InputSystem.RemoveDevice(screen);
                InputSystem.settings.updateMode = oldUpdate; InputSystem.settings.backgroundBehavior = oldBackground;
                InputSystem.settings.editorInputBehaviorInPlayMode = oldEditorInput;
            }
        }
    }
}
