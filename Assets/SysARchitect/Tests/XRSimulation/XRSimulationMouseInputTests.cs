using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.XR.Simulation;
using UnityEngine.UI;
using SysARchitect.AR;
using SysARchitect.Core;
using SysARchitect.Graph;
using SysARchitect.Interaction;
using SysARchitect.Visualization;

namespace SysARchitect.Tests
{
    public class XRSimulationMouseInputTests
    {
        private static IEnumerator Click(Mouse mouse, Vector3 world, bool navigating = false)
        {
            Vector2 point = Camera.main.WorldToScreenPoint(world);
            Assert.That(mouse.enabled, Is.True);
            Assert.That(NodeSelectionController.IsOverUI(point), Is.False);
            var state = new MouseState { position = point }.WithButton(MouseButton.Left);
            if (navigating) state = state.WithButton(MouseButton.Right);
            InputSystem.QueueStateEvent(mouse, state); yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }); yield return null;
        }
        [UnityTest]
        public IEnumerator EditorMouseBuildsAndEditsWithoutTouchEmulationAndNavigationDoesNotPlace()
        {
            yield return XRSimulationLifecycleTests.ReloadProvider();
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/ARWorkspace.unity");
            yield return XRSimulationLifecycleTests.Scan(); yield return XRSimulationLifecycleTests.Place();
            var graph = Object.FindAnyObjectByType<ArchitectureGraph>();
            var adapter = Object.FindAnyObjectByType<WorkspaceTouchInput>();
            var actions = Object.FindAnyObjectByType<InteractionManager>();
            var selection = Object.FindAnyObjectByType<NodeSelectionController>();
            var pose = Object.FindAnyObjectByType<SimulationCameraPoseProvider>();
            pose.transform.SetPositionAndRotation(graph.transform.position + new Vector3(0, .75f, -.65f),
                Quaternion.LookRotation(new Vector3(0, -.75f, .65f)));
            float deadline = Time.realtimeSinceStartup + 5;
            while ((Vector3.Distance(Camera.main.transform.position, pose.transform.position) > .01f ||
                Quaternion.Angle(Camera.main.transform.rotation, pose.transform.rotation) > .5f) && Time.realtimeSinceStartup < deadline) yield return null;
            var update = InputSystem.settings.updateMode; var background = InputSystem.settings.backgroundBehavior;
            var editor = InputSystem.settings.editorInputBehaviorInPlayMode;
            bool emulation = TouchSimulation.instance != null && TouchSimulation.instance.enabled;
            if (emulation) TouchSimulation.Disable();
            var screens = InputSystem.devices.OfType<Touchscreen>().Where(s => s.enabled).ToArray();
            Mouse mouse = null; Touchscreen idleScreen = null; bool enabled = adapter.enabled;
            try
            {
                InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                foreach (var screen in screens) InputSystem.DisableDevice(screen);
                mouse = InputSystem.AddDevice<Mouse>("XR mouse regression"); adapter.enabled = true;
                var firstPoint = graph.transform.TransformPoint(new Vector3(-.5f, 0, .35f));
                var secondPoint = graph.transform.TransformPoint(new Vector3(.5f, 0, .35f));
                actions.OnToolboxNodeSelected(NodeType.Frontend); yield return null;
                yield return Click(mouse, firstPoint);
                Assert.That(graph.Nodes.Count, Is.EqualTo(1), "A normal Editor mouse click must reach the shared placement command without a touchscreen");
                selection.Deselect();
                idleScreen = InputSystem.AddDevice<Touchscreen>("Idle touchscreen alongside Editor mouse");
                actions.OnToolboxNodeSelected(NodeType.Backend); yield return null;
                var toolbar = Object.FindObjectsByType<Button>().Single(b => b.name == "Toolbox");
                Vector2 overUI = toolbar.transform.position;
                Assert.That(NodeSelectionController.IsOverUI(overUI), Is.True);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = overUI }.WithButton(MouseButton.Left)); yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = Camera.main.WorldToScreenPoint(secondPoint) }); yield return null;
                Assert.That(graph.Nodes.Count, Is.EqualTo(1), "Dragging out of a toolbar must not place a component");
                yield return Click(mouse, secondPoint, navigating: true);
                Assert.That(graph.Nodes.Count, Is.EqualTo(1), "Right-button camera navigation must cancel world editing");
                yield return Click(mouse, secondPoint);
                Assert.That(graph.Nodes.Count, Is.EqualTo(2)); selection.Deselect(); Physics.SyncTransforms();
                actions.OnConnectButtonPressed(); yield return null;
                yield return Click(mouse, graph.Nodes[0].transform.position);
                yield return Click(mouse, graph.Nodes[1].transform.position);
                Assert.That(graph.Connections.Count, Is.EqualTo(1));
                actions.ReturnToSelectMode();
                yield return Click(mouse, graph.Nodes[0].transform.position);
                Assert.That(selection.SelectedNode.GetGraphNode(), Is.SameAs(graph.Nodes[0]));
                Vector2 dragStart = Camera.main.WorldToScreenPoint(graph.Nodes[0].transform.position);
                Vector2 dragEnd = Camera.main.WorldToScreenPoint(graph.transform.TransformPoint(new Vector3(-.5f, 0, -.25f)));
                Assert.That(NodeSelectionController.IsOverUI(dragEnd), Is.False);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = dragStart }.WithButton(MouseButton.Left)); yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = dragEnd }.WithButton(MouseButton.Left)); yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = dragEnd }); yield return null;
                Assert.That(graph.Nodes[0].transform.localPosition.z, Is.EqualTo(-.25f).Within(.02));
                actions.OnDeleteButtonPressed(); yield return null;
                Assert.That(graph.Nodes.Count, Is.EqualTo(1)); Assert.That(graph.Connections, Is.Empty);
                actions.OnToolboxNodeSelected(NodeType.Database); yield return null;
                Vector2 simultaneous = Camera.main.WorldToScreenPoint(firstPoint);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = simultaneous }.WithButton(MouseButton.Left));
                InputSystem.QueueStateEvent(idleScreen, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Began, position = simultaneous });
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = simultaneous });
                InputSystem.QueueStateEvent(idleScreen, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = simultaneous });
                yield return null; yield return null;
                Assert.That(graph.Nodes.Count, Is.EqualTo(2), "Touch and mouse events in one gesture must create exactly one node");
            }
            finally
            {
                adapter.enabled = enabled;
                if (mouse != null) InputSystem.RemoveDevice(mouse);
                if (idleScreen != null) InputSystem.RemoveDevice(idleScreen);
                foreach (var screen in screens) if (screen.added) InputSystem.EnableDevice(screen);
                if (emulation) TouchSimulation.Enable();
                InputSystem.settings.updateMode = update; InputSystem.settings.backgroundBehavior = background;
                InputSystem.settings.editorInputBehaviorInPlayMode = editor;
            }
        }
    }
}
