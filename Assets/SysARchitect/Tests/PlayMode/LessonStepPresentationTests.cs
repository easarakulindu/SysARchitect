using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;
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
using SysARchitect.Quiz;
using SysARchitect.Simulation;
using SysARchitect.Visualization;
using SysARchitect.Lessons;
using SysARchitect.UI;

namespace SysARchitect.Tests
{
    public class LessonStepPresentationTests
    {
        private static Button Named(string name) => Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b => b.name == name);
        private static void Click(string name) => ExecuteEvents.Execute(Named(name).gameObject,
            new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
        private static TextMeshProUGUI Field(LessonUI ui, string name) =>
            (TextMeshProUGUI)typeof(LessonUI).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui);
        internal static string VisibleText(TextMeshProUGUI label, RectTransform viewport)
        {
            label.ForceMeshUpdate(); var result = new StringBuilder();
            for (int i = 0; i < label.textInfo.characterCount; i++)
            {
                var character = label.textInfo.characterInfo[i]; var line = label.textInfo.lineInfo[character.lineNumber];
                var world = label.transform.TransformPoint(new Vector3(0, (line.ascender + line.descender) * .5f, 0));
                float y = viewport.InverseTransformPoint(world).y;
                if (y >= viewport.rect.yMin && y <= viewport.rect.yMax) result.Append(character.character);
            }
            return result.ToString();
        }
        private static IEnumerator OpenMobileGuide()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/ARWorkspace.unity");
            var workspace = Object.FindAnyObjectByType<ARWorkspace>(); float deadline = Time.realtimeSinceStartup + 10;
            while (workspace.Mode == ARWorkspace.WorkspaceMode.Checking && Time.realtimeSinceStartup < deadline) yield return null;
            if (workspace.Mode == ARWorkspace.WorkspaceMode.AugmentedReality) Click("Virtual / AR");
            Assert.That(workspace.CanInteract, Is.True); yield return null; yield return null;
            Click("Lessons / Progress"); yield return null; yield return null; Canvas.ForceUpdateCanvases();
        }
        [UnityTest]
        public IEnumerator MobileNextStepMustChangeTheVisibleInstructionContent()
        {
            yield return OpenMobileGuide();
            var ui = Object.FindAnyObjectByType<LessonUI>(); var lessons = Object.FindAnyObjectByType<LessonController>();
            var body = Field(ui, "body"); var scroll = body.GetComponentsInParent<ScrollRect>(true).First(s => s.enabled);
            scroll.verticalNormalizedPosition = 1; yield return null; Canvas.ForceUpdateCanvases();
            string before = VisibleText(body, scroll.viewport), wholeBefore = body.text;
            Assert.That(wholeBefore, Does.Contain(lessons.Current.Steps[0]));
            Click("Next step"); yield return null; yield return null; Canvas.ForceUpdateCanvases();
            Assert.That(ui.InstructionStep, Is.EqualTo(1));
            Assert.That(body.text, Does.Contain(lessons.Current.Steps[1]));
            Assert.That(body.text, Is.Not.EqualTo(wholeBefore), "The refresh key already includes the step index");
            Assert.That(VisibleText(body, scroll.viewport), Is.Not.EqualTo(before), "The changing instruction must be visible, not buried below static overview text");
        }
        private sealed class MemoryStore : IProgressStorage
        {
            public string Json = ""; public int Writes;
            public string Read() => Json;
            public void Write(string json) { Json = json; Writes++; }
        }
        private static void AssertStep(LessonUI ui, LessonController lessons, int index, ScrollRect scroll)
        {
            var definition = lessons.Current; var detail = definition.StepDetails[index]; var body = Field(ui, "body");
            Assert.That(ui.InstructionStep, Is.EqualTo(index));
            Assert.That(Field(ui, "title").text, Does.Contain(detail.Heading));
            Assert.That(Field(ui, "stepLabel").text, Does.Contain($"Instruction step {index + 1}/{definition.Steps.Count}"));
            Assert.That(body.text, Does.Contain(definition.Steps[index]).And.Contain(detail.Explanation));
            Assert.That(VisibleText(body, scroll.viewport), Does.Contain(detail.Heading).And.Contain(definition.Steps[index].Substring(0, 25)).And.Contain(detail.Explanation.Substring(0, 25)), "Current content must be inside the viewport");
            Assert.That(Named("Previous step").IsInteractable(), Is.EqualTo(index > 0));
            Assert.That(Named("Next step").IsInteractable(), Is.EqualTo(index < definition.Steps.Count - 1));
        }
        [UnityTest]
        public IEnumerator AllFiveLessonsShowActualForwardBackwardContentAtFourPhoneResolutions()
        {
            yield return OpenMobileGuide();
            var lessons = Object.FindAnyObjectByType<LessonController>(); lessons.UseProgress(new MemoryStore());
            var ui = Object.FindAnyObjectByType<LessonUI>(); var body = Field(ui, "body");
            var scroll = body.GetComponentsInParent<ScrollRect>(true).First(s => s.enabled);
            var canvas = GameObject.Find("Workspace UI").GetComponent<Canvas>(); canvas.GetComponent<CanvasScaler>().enabled = false;
            var camera = Camera.main; canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = .3f;
            var safeRoot = canvas.transform.Find("Safe Area").GetComponent<RectTransform>();
            var sizes = new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(2400, 1080), new Vector2Int(2796, 1290) };
            var areas = new[] { new Rect(0, 0, 1280, 720), new Rect(0, 0, 1920, 1080), new Rect(90, 30, 2220, 1020), new Rect(144, 63, 2508, 1164) };
            for (int sizeIndex = 0; sizeIndex < sizes.Length; sizeIndex++)
            {
                var size = sizes[sizeIndex]; var target = new RenderTexture(size.x, size.y, 24);
                camera.targetTexture = target; camera.aspect = size.x / (float)size.y; canvas.scaleFactor = Mathf.Min(size.x / 1280f, size.y / 720f);
                MobileWorkspaceUI.ApplySafeArea(safeRoot, areas[sizeIndex], new Vector2(size.x, size.y)); yield return null;
                for (int topic = 0; topic < LessonCatalog.All.Count; topic++)
                {
                    Click("Lessons"); yield return null; Click("Lesson choice " + (topic + 1)); yield return null; yield return null; Canvas.ForceUpdateCanvases();
                    Assert.That(lessons.Current.Id, Is.EqualTo(LessonCatalog.All[topic].Id)); AssertStep(ui, lessons, 0, scroll);
                    Click("Previous step"); yield return null; AssertStep(ui, lessons, 0, scroll);
                    string previous = VisibleText(body, scroll.viewport);
                    for (int step = 1; step < lessons.Current.Steps.Count; step++)
                    {
                        scroll.verticalNormalizedPosition = 0; yield return null;
                        Click("Next step"); yield return null; yield return null; Canvas.ForceUpdateCanvases();
                        AssertStep(ui, lessons, step, scroll); Assert.That(VisibleText(body, scroll.viewport), Is.Not.EqualTo(previous)); previous = VisibleText(body, scroll.viewport);
                    }
                    Click("Next step"); yield return null; AssertStep(ui, lessons, lessons.Current.Steps.Count - 1, scroll);
                    for (int step = lessons.Current.Steps.Count - 2; step >= 0; step--)
                    {
                        Click("Previous step"); yield return null; yield return null; Canvas.ForceUpdateCanvases(); AssertStep(ui, lessons, step, scroll);
                    }
                    MobilePhonePreviewTests.Inside(Field(ui, "title").rectTransform, areas[sizeIndex], camera);
                    Field(ui, "title").ForceMeshUpdate(); Assert.That(Field(ui, "title").preferredHeight, Is.LessThanOrEqualTo(Field(ui, "title").rectTransform.rect.height + 1));
                    MobilePhonePreviewTests.Capture(camera, target, size.x + "x" + size.y + "-step-content-lesson-" + (topic + 1));
                }
                Click("Lessons"); yield return null; Click("Lesson choice 1"); yield return null; yield return null;
                AssertStep(ui, lessons, 0, scroll); // Revisiting resets instructions, not completion.
                Click("Next step"); yield return null; yield return null;
                MobilePhonePreviewTests.Capture(camera, target, size.x + "x" + size.y + "-step-content-next");
                camera.targetTexture = null; Object.Destroy(target); yield return null;
            }
        }
        [UnityTest]
        public IEnumerator DesktopStepsPreserveGraphSimulationAndSavedProgressWhileFocusChanges()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity"); yield return null;
            var store = new MemoryStore(); var lessons = Object.FindAnyObjectByType<LessonController>(); lessons.UseProgress(store);
            lessons.Progress.Complete(LessonId.ClientServer); lessons.Progress.Complete(LessonId.Database);
            foreach (var stage in new[] { QuizStage.Pre, QuizStage.Post })
            {
                var quiz = new QuizSession(stage);
                while (!quiz.Complete) { quiz.Answer(quiz.Current.CorrectAnswer); if (!quiz.Complete) quiz.Next(); }
                lessons.Progress.RecordQuiz(quiz);
            }
            string saved = store.Json; int writes = store.Writes;
            var graph = Object.FindAnyObjectByType<ArchitectureGraph>(); var simulation = Object.FindAnyObjectByType<SimulationController>();
            var frontend = graph.AddNode(NodeType.Frontend, Vector3.left); var backend = graph.AddNode(NodeType.Backend, Vector3.zero); var database = graph.AddNode(NodeType.Database, Vector3.right);
            var first = graph.AddConnection(frontend, backend); var second = graph.AddConnection(backend, database);
            var visual = frontend.GetComponent<NodeVisual>(); var edge = first.GetComponent<ConnectionRenderer>(); var line = first.GetComponent<LineRenderer>();
            var selection = Object.FindAnyObjectByType<NodeSelectionController>(); selection.Select(visual);
            simulation.Engine.RequestsPerSecond = 0; simulation.Engine.SubmitRequest(frontend.NodeId, false, 1);
            for (int i = 0; i < 40; i++) simulation.StepOnce();
            double time = simulation.Engine.Time; var succeeded = simulation.Engine.Succeeded; var algorithm = lessons.ActiveAlgorithmLine;
            Click("Lessons / Progress"); yield return null; yield return null;
            Assert.That(visual.IsLessonFocused, Is.True); Assert.That(edge.IsLessonFocused, Is.True); Assert.That(visual.IsSelected, Is.True);
            Click("Next step"); yield return null; Click("Next step"); yield return null;
            Assert.That(visual.IsLessonFocused, Is.False); Assert.That(backend.GetComponent<NodeVisual>().IsLessonFocused, Is.True);
            Assert.That(edge.IsLessonFocused, Is.False); Assert.That(second.GetComponent<ConnectionRenderer>().IsLessonFocused, Is.True);
            Assert.That(lessons.ActiveAlgorithmLine, Is.EqualTo(algorithm), "Instruction navigation must not impersonate actual simulation activity");
            Click("Previous step"); yield return null;
            Assert.That(edge.IsLessonFocused, Is.True);
            edge.ShowActivity(Color.magenta, "actual transfer"); Assert.That(line.sharedMaterial.color, Is.EqualTo(Color.magenta));
            selection.SelectConnection(first); Assert.That(edge.IsSelected, Is.True); Assert.That(Vector4.Distance(line.sharedMaterial.color, new Color(1, .85f, .1f)), Is.LessThan(.0001f));
            var ui = Object.FindAnyObjectByType<LessonUI>(); var body = Field(ui, "body"); var scroll = body.GetComponentInParent<ScrollRect>();
            for (int topic = 0; topic < LessonCatalog.All.Count; topic++)
            {
                Click("Lessons"); yield return null; Assert.That(graph.Nodes.All(n => !n.GetComponent<NodeVisual>().IsLessonFocused), Is.True);
                Click("Lesson choice " + (topic + 1)); yield return null; yield return null;
                AssertStep(ui, lessons, 0, scroll);
                Click("Next step"); yield return null; yield return null; AssertStep(ui, lessons, 1, scroll);
                Click("Previous step"); yield return null; yield return null; AssertStep(ui, lessons, 0, scroll);
            }
            Click("Lessons"); yield return null; Click("Lesson choice 1"); yield return null; yield return null; AssertStep(ui, lessons, 0, scroll);
            Click("Close guide"); yield return null;
            Assert.That(graph.Nodes.All(n => !n.GetComponent<NodeVisual>().IsLessonFocused), Is.True);
            Assert.That(graph.Nodes.Count, Is.EqualTo(3)); Assert.That(graph.Connections, Is.EquivalentTo(new[] { first, second }));
            Assert.That(selection.SelectedConnection, Is.SameAs(first));
            Assert.That(simulation.Engine.Time, Is.EqualTo(time)); Assert.That(simulation.Engine.Succeeded, Is.EqualTo(succeeded));
            Assert.That(store.Json, Is.EqualTo(saved)); Assert.That(store.Writes, Is.EqualTo(writes));
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity"); yield return null;
            lessons = Object.FindAnyObjectByType<LessonController>(); lessons.UseProgress(store);
            Assert.That(lessons.Progress.CompletedCount, Is.EqualTo(2)); Assert.That(lessons.Progress.LatestQuiz(QuizStage.Pre).correct, Is.EqualTo(10));
            Assert.That(lessons.Progress.LatestQuiz(QuizStage.Post).correct, Is.EqualTo(10)); Assert.That(store.Json, Is.EqualTo(saved));
        }
    }
}
