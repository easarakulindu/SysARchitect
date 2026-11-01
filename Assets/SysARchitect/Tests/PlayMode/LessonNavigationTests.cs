using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
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
    public class LessonNavigationTests
    {
        private sealed class MemoryStore : IProgressStorage
        {
            public string Json = ""; public int Writes;
            public string Read() => Json;
            public void Write(string json) { Json = json; Writes++; }
        }
        private static Button Named(string name) => Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b => b.name == name);
        private static void Click(string name) => ExecuteEvents.Execute(Named(name).gameObject,
            new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
        [UnityTest]
        public IEnumerator LessonAndInstructionNavigationHaveIndependentBoundariesAndPickerAccess()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity"); yield return null;
            var lessons = Object.FindAnyObjectByType<LessonController>(); lessons.UseProgress(new MemoryStore());
            var ui = Object.FindAnyObjectByType<LessonUI>();
            Click("Lessons / Progress"); yield return null; yield return null;
            Assert.That(Named("Previous lesson").IsInteractable(), Is.False);
            Assert.That(Named("Previous step").IsInteractable(), Is.False);
            Click("Previous lesson"); Click("Previous step"); Assert.That(ui.InstructionStep, Is.Zero);
            Assert.That(lessons.Current.Id, Is.EqualTo(LessonId.ClientServer));
            for (int i = 1; i < lessons.Current.Steps.Count; i++) { Click("Next step"); yield return null; }
            Assert.That(Named("Next step").IsInteractable(), Is.False); Click("Next step");
            Assert.That(ui.InstructionStep, Is.EqualTo(lessons.Current.Steps.Count - 1));
            Click("Previous step"); yield return null; Assert.That(ui.InstructionStep, Is.EqualTo(lessons.Current.Steps.Count - 2));
            Click("Next lesson"); yield return null;
            Assert.That(lessons.Current.Id, Is.EqualTo(LessonId.LoadBalancing)); Assert.That(ui.InstructionStep, Is.Zero);
            Click("Previous lesson"); yield return null;
            Assert.That(lessons.Current.Id, Is.EqualTo(LessonId.ClientServer));
            Click("Lessons"); yield return null; Assert.That(ui.ShowingLessonSelection, Is.True);
            Assert.That(Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Count(b => b.name.StartsWith("Lesson choice ")), Is.EqualTo(5));
            Click("Lesson choice 5"); yield return null;
            Assert.That(lessons.Current.Id, Is.EqualTo(LessonId.Kubernetes)); Assert.That(ui.ShowingLessonSelection, Is.False);
            Assert.That(Named("Next lesson").IsInteractable(), Is.False); Click("Next lesson");
            Assert.That(lessons.Current.Id, Is.EqualTo(LessonId.Kubernetes));
            Click("Lessons"); yield return null; Click("Lesson choice 1"); yield return null;
            Assert.That(Named("Previous lesson").IsInteractable(), Is.False);
            // An externally selected lesson must not leave a stale UI lesson index.
            lessons.SelectLesson(LessonId.Database); yield return null; Click("Next lesson"); yield return null;
            Assert.That(lessons.Current.Id, Is.EqualTo(LessonId.Docker));
        }
        [UnityTest]
        public IEnumerator BackwardAndPickerNavigationPreserveSerializedProgressScoresAndLiveGraph()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity"); yield return null;
            var store = new MemoryStore(); var lessons = Object.FindAnyObjectByType<LessonController>(); lessons.UseProgress(store);
            lessons.Progress.Complete(LessonId.ClientServer); lessons.Progress.Complete(LessonId.Database);
            foreach (var stage in new[] { QuizStage.Pre, QuizStage.Post })
            {
                var session = new QuizSession(stage);
                while (!session.Complete) { session.Answer(session.Current.CorrectAnswer); if (!session.Complete) session.Next(); }
                lessons.Progress.RecordQuiz(session);
            }
            string saved = store.Json; int writes = store.Writes;
            var graph = Object.FindAnyObjectByType<ArchitectureGraph>(); var simulation = Object.FindAnyObjectByType<SimulationController>();
            var f = graph.AddNode(NodeType.Frontend, Vector3.left * .3f); var b = graph.AddNode(NodeType.Backend, Vector3.right * .3f); var edge = graph.AddConnection(f, b);
            simulation.Engine.RequestsPerSecond = 0; simulation.Engine.SubmitRequest(f.NodeId, false, 1);
            for (int i = 0; i < 40; i++) simulation.StepOnce(); Assert.That(simulation.Engine.Succeeded, Is.EqualTo(1));
            simulation.Run(); var mode = Object.FindAnyObjectByType<InteractionManager>().CurrentMode;
            Click("Lessons / Progress"); yield return null;
            Click("Next lesson"); yield return null; Click("Previous lesson"); yield return null;
            Click("Lessons"); yield return null; Click("Lesson choice 5"); yield return null;
            Click("Previous lesson"); yield return null; Click("Lessons"); yield return null; Click("Lesson choice 1"); yield return null;
            Assert.That(store.Json, Is.EqualTo(saved)); Assert.That(store.Writes, Is.EqualTo(writes));
            Assert.That(simulation.IsRunning, Is.True); Assert.That(simulation.Engine.Succeeded, Is.EqualTo(1));
            Assert.That(graph.Nodes.Count, Is.EqualTo(2)); Assert.That(graph.Connections.Single(), Is.SameAs(edge));
            Assert.That(Object.FindAnyObjectByType<InteractionManager>().CurrentMode, Is.EqualTo(mode)); simulation.Pause();
            // Reload the scene and stored JSON, without touching a real learner's PlayerPrefs.
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity"); yield return null;
            lessons = Object.FindAnyObjectByType<LessonController>(); lessons.UseProgress(store);
            Assert.That(lessons.Progress.CompletedCount, Is.EqualTo(2)); Assert.That(lessons.Progress.IsComplete(LessonId.Database), Is.True);
            Assert.That(lessons.Progress.QuizAttempts.Count, Is.EqualTo(2));
            Assert.That(lessons.Progress.LatestQuiz(QuizStage.Pre).correct, Is.EqualTo(10));
            Assert.That(lessons.Progress.LatestQuiz(QuizStage.Post).correct, Is.EqualTo(10));
            Assert.That(store.Json, Is.EqualTo(saved)); Assert.That(store.Writes, Is.EqualTo(writes));
        }
        [UnityTest]
        public IEnumerator ActualMobileGuidePinsLessonControlsAndScrollsAllChoicesAtPhoneResolutions()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/ARWorkspace.unity");
            var workspace = Object.FindAnyObjectByType<ARWorkspace>(); float deadline = Time.realtimeSinceStartup + 10;
            while (workspace.Mode == ARWorkspace.WorkspaceMode.Checking && Time.realtimeSinceStartup < deadline) yield return null;
            if (workspace.Mode == ARWorkspace.WorkspaceMode.AugmentedReality) Click("Virtual / AR");
            Assert.That(workspace.Mode, Is.EqualTo(ARWorkspace.WorkspaceMode.Virtual)); yield return null; yield return null;
            var lessons = Object.FindAnyObjectByType<LessonController>(); lessons.UseProgress(new MemoryStore());
            Click("Lessons / Progress"); yield return null; yield return null;
            var ui = Object.FindAnyObjectByType<LessonUI>();
            var canvas = GameObject.Find("Workspace UI").GetComponent<Canvas>(); canvas.GetComponent<CanvasScaler>().enabled = false;
            var camera = Camera.main; canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = .3f;
            var root = canvas.transform.Find("Safe Area").GetComponent<RectTransform>();
            var sizes = new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(2400, 1080), new Vector2Int(2796, 1290) };
            var areas = new[] { new Rect(0, 0, 1280, 720), new Rect(0, 0, 1920, 1080), new Rect(90, 30, 2220, 1020), new Rect(144, 63, 2508, 1164) };
            for (int i = 0; i < sizes.Length; i++)
            {
                var size = sizes[i]; var target = new RenderTexture(size.x, size.y, 24); camera.targetTexture = target; camera.aspect = size.x / (float)size.y;
                canvas.scaleFactor = Mathf.Min(size.x / 1280f, size.y / 720f); MobileWorkspaceUI.ApplySafeArea(root, areas[i], new Vector2(size.x, size.y));
                yield return null; Canvas.ForceUpdateCanvases(); yield return null;
                var guide = root.Find("Learning Guide"); var lessonScroll = guide.Find("Mobile lesson scroll").GetComponent<ScrollRect>();
                lessonScroll.verticalNormalizedPosition = 0; yield return null;
                foreach (string name in new[] { "Previous lesson", "Lessons", "Next lesson", "Previous step", "Next step" })
                {
                    var button = Named(name); MobilePhonePreviewTests.Inside(button.GetComponent<RectTransform>(), areas[i], camera);
                    Assert.That(button.transform.IsChildOf(lessonScroll.viewport), Is.False, "Navigation must remain pinned after scrolling");
                    Assert.That(button.GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(95.9));
                    var label = button.GetComponentInChildren<TextMeshProUGUI>(); label.ForceMeshUpdate();
                    Assert.That(label.preferredHeight, Is.LessThanOrEqualTo(label.rectTransform.rect.height + 1));
                }
                MobilePhonePreviewTests.Capture(camera, target, size.x + "x" + size.y + "-lesson-navigation");
                Click("Lessons"); yield return null; Canvas.ForceUpdateCanvases(); yield return null;
                var picker = guide.Find("Lesson selection").GetComponent<ScrollRect>();
                Assert.That(picker.content.rect.height, Is.GreaterThan(picker.viewport.rect.height));
                picker.verticalNormalizedPosition = 0; yield return null;
                MobilePhonePreviewTests.Inside(Named("Lesson choice 5").GetComponent<RectTransform>(), areas[i], camera);
                MobilePhonePreviewTests.Capture(camera, target, size.x + "x" + size.y + "-lesson-picker");
                Click("Lesson choice 5"); yield return null; Assert.That(ui.ShowingLessonSelection, Is.False);
                Assert.That(Named("Next lesson").IsInteractable(), Is.False);
                Click("Previous lesson"); yield return null; Assert.That(lessons.Current.Id, Is.EqualTo(LessonId.Docker));
                Assert.That(workspace.CanInteract, Is.True);
                camera.targetTexture = null; Object.Destroy(target); yield return null;
            }
        }
    }
}
