using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using SysARchitect.AR;
using SysARchitect.Core;
using SysARchitect.Graph;
using SysARchitect.Interaction;
using SysARchitect.Lessons;
using SysARchitect.Quiz;
using SysARchitect.Simulation;
using SysARchitect.UI;

namespace SysARchitect.Tests
{
    public class VirtualWorkspaceTests
    {
        private sealed class MemoryStore : IProgressStorage
        { public string Read() => ""; public void Write(string json) { } }
        private static Button Named(string name) => Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b => b.name == name);
        private static void Click(Button button) => ExecuteEvents.Execute(button.gameObject,
            new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
        private static void Tap(WorkspaceTouchInput input, Vector3 point)
        {
            var screen = Camera.main.WorldToScreenPoint(point);
            input.HandlePointer(screen, TouchPhase.Began, true); input.HandlePointer(screen, TouchPhase.Ended, true);
        }
        private static IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/ARWorkspace.unity");
            var workspace = Object.FindAnyObjectByType<ARWorkspace>();
            float deadline = Time.realtimeSinceStartup + 10;
            while (workspace.Mode == ARWorkspace.WorkspaceMode.Checking && Time.realtimeSinceStartup < deadline) yield return null;
            // XR Simulation can report support in the editor; explicitly choose non-AR.
            if (workspace.Mode == ARWorkspace.WorkspaceMode.AugmentedReality) Click(Named("Virtual / AR"));
            Assert.That(workspace.Mode, Is.EqualTo(ARWorkspace.WorkspaceMode.Virtual));
            yield return null;
        }
        [UnityTest]
        public IEnumerator VirtualModeUsesActualMobileSceneAndSharedTouchGraphCommands()
        {
            yield return Load();
            var workspace = Object.FindAnyObjectByType<ARWorkspace>();
            var graph = Object.FindAnyObjectByType<ArchitectureGraph>();
            var input = Object.FindAnyObjectByType<WorkspaceTouchInput>();
            var selection = Object.FindAnyObjectByType<NodeSelectionController>();
            var interactions = Object.FindAnyObjectByType<InteractionManager>();
            Assert.That(workspace.CanInteract && workspace.HasWorkspace, Is.True);
            var fields = BindingFlags.Instance | BindingFlags.NonPublic;
            var session = (Behaviour)typeof(ARWorkspace).GetField("session", fields).GetValue(workspace);
            Assert.That(session.enabled, Is.False);
            foreach (var component in (Behaviour[])typeof(ARWorkspace).GetField("arOnlyComponents", fields).GetValue(workspace))
                Assert.That(component.enabled, Is.False, component.GetType().Name);
            Assert.That(Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None).Any(t => t.text.Contains("Virtual Workspace (non-AR)")), Is.True);
            Assert.That(graph.transform.localScale.x, Is.EqualTo(.35f).Within(.001f));
            workspace.TryPlaceWorkspace(Vector2.zero); Assert.That(session.enabled, Is.False);

            Click(Named("Toolbox")); yield return null;
            Click(Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b => b.GetComponentInChildren<TextMeshProUGUI>()?.text == "Frontend"));
            yield return null; Tap(input, new Vector3(-.14f, 0, 0)); yield return null;
            Assert.That(graph.Nodes.Count, Is.EqualTo(1)); var frontend = graph.Nodes[0]; selection.Deselect();
            interactions.OnToolboxNodeSelected(NodeType.Backend); yield return null;
            Tap(input, new Vector3(.14f, 0, 0)); yield return null;
            Assert.That(graph.Nodes.Count, Is.EqualTo(2)); var backend = graph.Nodes[1]; selection.Deselect();
            Physics.SyncTransforms();
            Click(Named("Toolbox")); yield return null; Click(Named("Edit / Connect")); yield return null;
            Click(Named("Connect source > target")); yield return null;
            Tap(input, frontend.transform.position); Tap(input, backend.transform.position); yield return null;
            Assert.That(graph.Connections.Count, Is.EqualTo(1));
            interactions.ReturnToSelectMode(); Tap(input, backend.transform.position); yield return null;
            Assert.That(selection.SelectedNode.GetGraphNode(), Is.EqualTo(backend));
            var start = Camera.main.WorldToScreenPoint(backend.transform.position);
            var end = Camera.main.WorldToScreenPoint(new Vector3(.22f, 0, 0));
            input.HandlePointer(start, TouchPhase.Began, true); input.HandlePointer(end, TouchPhase.Moved, true); input.HandlePointer(end, TouchPhase.Ended, true);
            Assert.That(backend.transform.position.x, Is.EqualTo(.22f).Within(.01f)); selection.Deselect();
            yield return null;
            Visualization.ConnectionRenderer.GetEndpoints(frontend.transform, backend.transform, out var a, out var z);
            Tap(input, (a + z) * .5f); Assert.That(selection.SelectedConnection, Is.EqualTo(graph.Connections[0]));
            yield return null; yield return null;
            Click(Named("Delete Connection")); yield return null;
            Assert.That(graph.Nodes.Count, Is.EqualTo(2)); Assert.That(graph.Connections, Is.Empty);
            interactions.OnConnectButtonPressed(); yield return null;
            Tap(input, frontend.transform.position); Tap(input, backend.transform.position); yield return null;
            Assert.That(graph.Connections.Count, Is.EqualTo(1));
            var simulation = Object.FindAnyObjectByType<SimulationController>(); simulation.Engine.RequestsPerSecond = 0;
            simulation.Engine.SubmitRequest(frontend.NodeId, false, 1); for (int i = 0; i < 40; i++) simulation.StepOnce();
            Assert.That(simulation.Engine.Succeeded, Is.EqualTo(1));
            workspace.Reposition(); Assert.That(graph.Nodes.Count, Is.EqualTo(2)); Assert.That(workspace.CanInteract, Is.True);
            workspace.ToggleWorkspaceMode(); yield return null; yield return null; yield return null;
            if (workspace.Mode == ARWorkspace.WorkspaceMode.AugmentedReality) Click(Named("Virtual / AR"));
            Assert.That(workspace.Mode, Is.EqualTo(ARWorkspace.WorkspaceMode.Virtual));
            Assert.That(graph.Connections.Count, Is.EqualTo(1)); Assert.That(simulation.Engine.Succeeded, Is.EqualTo(1));
        }
        [UnityTest]
        public IEnumerator UnavailableOrMissingARServicesLeaveAllARComponentsDisabled()
        {
            yield return Load();
            var workspace = Object.FindAnyObjectByType<ARWorkspace>();
            var fields = BindingFlags.Instance | BindingFlags.NonPublic;
            var resolve = typeof(ARWorkspace).GetMethod("CompleteAvailabilityCheck", fields);
            // Inject availability outcomes only; camera/session behavior uses the saved scene.
            foreach (var state in new[] { ARSessionState.Unsupported, ARSessionState.NeedsInstall, ARSessionState.None })
            {
                resolve.Invoke(workspace, new object[] { state }); yield return null;
                Assert.That(workspace.Mode, Is.EqualTo(ARWorkspace.WorkspaceMode.Virtual));
                Assert.That(workspace.CanInteract, Is.True);
                Assert.That(((Behaviour)typeof(ARWorkspace).GetField("session", fields).GetValue(workspace)).enabled, Is.False);
                foreach (var component in (Behaviour[])typeof(ARWorkspace).GetField("arOnlyComponents", fields).GetValue(workspace))
                    Assert.That(component.enabled, Is.False, component.GetType().Name);
            }
        }
        [UnityTest]
        public IEnumerator VirtualWorkspaceRunsDatabaseLessonConfigurationAndQuiz()
        {
            yield return Load();
            var lessons = Object.FindAnyObjectByType<LessonController>(); lessons.UseProgress(new MemoryStore());
            lessons.SelectLesson(LessonId.Database); lessons.LoadExample(); yield return null;
            var graph = Object.FindAnyObjectByType<ArchitectureGraph>();
            Assert.That(graph.Nodes.Count, Is.EqualTo(4));
            var simulation = Object.FindAnyObjectByType<SimulationController>();
            for (int i = 0; i < 160; i++) simulation.StepOnce();
            Assert.That(simulation.Engine.Succeeded, Is.GreaterThan(0));
            var selection = Object.FindAnyObjectByType<NodeSelectionController>();
            var database = graph.Nodes.Single(n => n.Type == NodeType.Database);
            selection.Select(database.GetComponent<Visualization.NodeVisual>()); yield return null; yield return null;
            Assert.That(GameObject.Find("Mobile details drawer"), Is.Not.Null);
            Assert.That(Named("Replication").IsInteractable(), Is.True);
            Click(Named("Replication")); yield return null;
            var model = simulation.Engine.FindNode(database.NodeId).Database;
            Assert.That(model.Mode, Is.EqualTo(DatabaseMode.Replication)); Assert.That(model.Members.Count, Is.EqualTo(3));
            Click(Named("Add replica")); yield return null; Assert.That(model.Members.Count, Is.EqualTo(4));
            Click(Named("Sharding")); yield return null; Click(Named("+ shard")); yield return null;
            Assert.That(model.Mode, Is.EqualTo(DatabaseMode.Sharding)); Assert.That(model.Members.Count, Is.EqualTo(4));
            var quiz = Object.FindAnyObjectByType<QuizUI>(); quiz.Open(QuizStage.Pre); yield return null;
            quiz.Answer(0); yield return null; Assert.That(Named("Next question").IsInteractable(), Is.True);
            quiz.Close(); yield return null;
            Assert.That(Object.FindAnyObjectByType<ARWorkspace>().CanInteract, Is.True);
        }
        [UnityTest]
        public IEnumerator ActualVirtualSceneRendersSafePhoneLayoutsWithExplicitNonARStatus()
        {
            yield return Load();
            var lessons = Object.FindAnyObjectByType<LessonController>(); lessons.UseProgress(new MemoryStore());
            lessons.SelectLesson(LessonId.Database); lessons.LoadExample(); yield return null;
            var simulation = Object.FindAnyObjectByType<SimulationController>();
            for (int i = 0; i < 80; i++) simulation.StepOnce();
            var canvas = GameObject.Find("Workspace UI").GetComponent<Canvas>();
            canvas.GetComponent<CanvasScaler>().enabled = false;
            var camera = Camera.main; canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = .3f;
            var safeRoot = canvas.transform.Find("Safe Area").GetComponent<RectTransform>();
            var sizes = new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(2400, 1080), new Vector2Int(2796, 1290) };
            var insets = new[] { new Rect(0, 0, 1280, 720), new Rect(0, 0, 1920, 1080), new Rect(90, 30, 2220, 1020), new Rect(144, 63, 2508, 1164) };
            for (int i = 0; i < sizes.Length; i++)
            {
                var size = sizes[i]; var target = new RenderTexture(size.x, size.y, 24);
                camera.targetTexture = target; camera.aspect = size.x / (float)size.y;
                canvas.scaleFactor = Mathf.Min(size.x / 1280f, size.y / 720f);
                MobileWorkspaceUI.ApplySafeArea(safeRoot, insets[i], new Vector2(size.x, size.y));
                yield return null; Canvas.ForceUpdateCanvases(); yield return null;
                var status = safeRoot.Find("AR Controls").GetComponentsInChildren<TextMeshProUGUI>().Single(t => t.text.StartsWith("Virtual Workspace"));
                status.ForceMeshUpdate();
                Assert.That(status.preferredHeight, Is.LessThanOrEqualTo(status.rectTransform.rect.height + 1));
                foreach (var button in safeRoot.Find("AR Controls").GetComponentsInChildren<Button>())
                {
                    MobilePhonePreviewTests.Inside(button.GetComponent<RectTransform>(), insets[i], camera);
                    var text = button.GetComponentInChildren<TextMeshProUGUI>(); text.ForceMeshUpdate();
                    Assert.That(text.preferredHeight, Is.LessThanOrEqualTo(text.rectTransform.rect.height + 1));
                }
                MobilePhonePreviewTests.Capture(camera, target, size.x + "x" + size.y + "-virtual-scene");
                camera.targetTexture = null; Object.Destroy(target); yield return null;
            }
        }
    }
}
