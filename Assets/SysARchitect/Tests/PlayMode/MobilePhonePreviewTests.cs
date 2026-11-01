using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SysARchitect.AR;
using SysARchitect.Graph;
using SysARchitect.Interaction;
using SysARchitect.Lessons;
using SysARchitect.Quiz;
using SysARchitect.Simulation;
using SysARchitect.UI;
using SysARchitect.Visualization;

namespace SysARchitect.Tests
{
    public class MobilePhonePreviewTests
    {
        private sealed class MemoryStore : IProgressStorage
        { public string Read() => ""; public void Write(string json) { } }
        private static void PointerClick(Button button) => ExecuteEvents.Execute(button.gameObject,
            new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
        private static Button Named(Transform parent, string name) => parent.GetComponentsInChildren<Button>(true).Single(b => b.name == name);
        internal static void Inside(RectTransform transform, Rect safe, Camera camera)
        {
            var corners = new Vector3[4]; transform.GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                var pixel = camera.WorldToScreenPoint(corner);
                Assert.That(pixel.x, Is.InRange(safe.xMin - 1, safe.xMax + 1), transform.name + " horizontal safe bounds");
                Assert.That(pixel.y, Is.InRange(safe.yMin - 1, safe.yMax + 1), transform.name + " vertical safe bounds");
            }
        }
        internal static void Capture(Camera camera, RenderTexture target, string name)
        {
            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
            var previous = RenderTexture.active; RenderTexture.active = target;
            var pixels = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); pixels.Apply();
            File.WriteAllBytes("/tmp/sysarchitect-rc-" + name + ".png", pixels.EncodeToPNG());
            RenderTexture.active = previous; Object.Destroy(pixels);
        }

        [UnityTest]
        public IEnumerator TrackingLossBlocksMutationsButPreservesNavigationAndBuildControls()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity"); yield return null;
            bool tracked = false;
            var layout = VisualAndMobileLayoutTests.Layout(out var root, out var toolbox, () => tracked);
            toolbox.SetActive(true); layout.Refresh(true); yield return null;
            var frontend = toolbox.GetComponentsInChildren<Button>(true).Single(b => b.GetComponentInChildren<TextMeshProUGUI>()?.text == "Frontend");
            PointerClick(frontend);
            var interactions = Object.FindAnyObjectByType<InteractionManager>();
            Assert.That(interactions.CurrentMode, Is.EqualTo(InteractionManager.Mode.Select));
            tracked = true; layout.Refresh(true); yield return null; PointerClick(frontend);
            Assert.That(interactions.CurrentMode, Is.EqualTo(InteractionManager.Mode.Place)); interactions.ReturnToSelectMode();
            var simulation = Object.FindAnyObjectByType<SimulationController>();
            toolbox.SetActive(false); root.Find("Simulation").gameObject.SetActive(true);
            tracked = false; layout.Refresh(true); yield return null;
            PointerClick(Named(root.Find("Simulation"), "Run")); PointerClick(Named(root.Find("Simulation"), "Step 50 ms"));
            Assert.That(simulation.IsRunning, Is.False); Assert.That(simulation.Engine.Time, Is.Zero);
            PointerClick(Named(root.Find("Simulation"), "Build"));
            Assert.That(interactions.CurrentMode, Is.EqualTo(InteractionManager.Mode.Select));
            tracked = true; layout.Refresh(true); yield return null; PointerClick(Named(root.Find("Simulation"), "Run"));
            Assert.That(simulation.IsRunning, Is.True); simulation.Pause();
        }

        [UnityTest]
        public IEnumerator PhoneResolutionsKeepPinnedControlsReadableAndAllPanelsScrollable()
        {
            var sizes = new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(2400, 1080), new Vector2Int(2796, 1290) };
            var safeAreas = new[] { new Rect(0, 0, 1280, 720), new Rect(0, 0, 1920, 1080), new Rect(90, 30, 2220, 1020), new Rect(144, 63, 2508, 1164) };
            for (int index = 0; index < sizes.Length; index++)
            {
                yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity"); yield return null;
                var lessons = Object.FindAnyObjectByType<LessonController>(); lessons.UseProgress(new MemoryStore());
                lessons.SelectLesson(LessonId.Database); lessons.LoadExample();
                var simulation = Object.FindAnyObjectByType<SimulationController>(); for (int i = 0; i < 40; i++) simulation.StepOnce();
                var layout = VisualAndMobileLayoutTests.Layout(out var canvasRoot, out var toolbox);
                var canvas = canvasRoot.GetComponent<Canvas>(); canvasRoot.GetComponent<CanvasScaler>().enabled = false;
                var camera = Camera.main; var size = sizes[index]; var safe = safeAreas[index];
                var target = new RenderTexture(size.x, size.y, 24); camera.targetTexture = target; camera.aspect = size.x / (float)size.y;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = .3f;
                canvas.scaleFactor = Mathf.Min(size.x / 1280f, size.y / 720f);
                var children = canvasRoot.Cast<Transform>().ToArray();
                var root = new GameObject("Synthetic safe area", typeof(RectTransform)).GetComponent<RectTransform>(); root.SetParent(canvasRoot, false);
                foreach (var child in children) child.SetParent(root, false);
                MobileWorkspaceUI.ApplySafeArea(root, safe, new Vector2(size.x, size.y));
                toolbox.SetActive(true); layout.Refresh(true); yield return null; layout.Refresh(true); Canvas.ForceUpdateCanvases(); yield return null;
                foreach (var button in root.Find("AR Controls").GetComponentsInChildren<Button>())
                {
                    Inside(button.GetComponent<RectTransform>(), safe, camera);
                    Assert.That(button.GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(95.9));
                    var text = button.GetComponentInChildren<TextMeshProUGUI>(); text.ForceMeshUpdate();
                    Assert.That(text.preferredHeight, Is.LessThanOrEqualTo(text.rectTransform.rect.height + 1), text.text);
                }
                var status = root.Find("AR Controls/Tracking status").GetComponent<TextMeshProUGUI>(); status.ForceMeshUpdate();
                Assert.That(status.preferredHeight, Is.LessThanOrEqualTo(status.rectTransform.rect.height + 1));
                Inside(toolbox.GetComponent<RectTransform>(), safe, camera);
                Capture(camera, target, size.x + "x" + size.y + "-toolbox");
                Named(toolbox.transform, "Edit / Connect").onClick.Invoke(); layout.Refresh(true); yield return null; Canvas.ForceUpdateCanvases();
                Assert.That(Named(toolbox.transform, "Connect source > target").gameObject.activeInHierarchy, Is.True);
                Capture(camera, target, size.x + "x" + size.y + "-editing");
                toolbox.SetActive(false);
                var graph = Object.FindAnyObjectByType<ArchitectureGraph>();
                Object.FindAnyObjectByType<NodeSelectionController>().Select(graph.Nodes.Single(n => n.Type == SysARchitect.Core.NodeType.LoadBalancer).GetComponent<NodeVisual>());
                yield return null; layout.Refresh(true); Canvas.ForceUpdateCanvases();
                var details = root.Find("Mobile details drawer"); Inside(details.GetComponent<RectTransform>(), safe, camera);
                Assert.That(Named(details, "Database Router").gameObject.activeInHierarchy, Is.True);
                Inside(Named(details, "Database Router").GetComponent<RectTransform>(), safe, camera);
                Capture(camera, target, size.x + "x" + size.y + "-details");
                Named(details, "Node / Connection").onClick.Invoke(); layout.Refresh(true); yield return null; Canvas.ForceUpdateCanvases();
                Assert.That(details.GetComponentsInChildren<CanvasGroup>().Single(g => g.name == "Node Inspector").alpha, Is.EqualTo(1));
                root.Find("Learning Guide").gameObject.SetActive(true); layout.Refresh(true); yield return null; layout.Refresh(true); Canvas.ForceUpdateCanvases();
                var guide = root.Find("Learning Guide"); var lessonScroll = guide.GetComponentInChildren<ScrollRect>(); lessonScroll.verticalNormalizedPosition = 0;
                yield return null; Inside(Named(guide, "Close guide").GetComponent<RectTransform>(), safe, camera);
                Inside(Named(guide, "Next step").GetComponent<RectTransform>(), safe, camera);
                Capture(camera, target, size.x + "x" + size.y + "-lesson");
                var quiz = Object.FindAnyObjectByType<QuizUI>(); quiz.Open(QuizStage.Pre); yield return null;
                quiz.Answer(0); layout.Refresh(true); yield return null; layout.Refresh(true); Canvas.ForceUpdateCanvases();
                var overlay = root.Find("Quiz Overlay"); var quizScroll = overlay.GetComponentInChildren<ScrollRect>(); quizScroll.verticalNormalizedPosition = 0;
                yield return null; Inside(Named(overlay, "Close / Return to guide").GetComponent<RectTransform>(), safe, camera);
                Inside(Named(overlay, "Next question").GetComponent<RectTransform>(), safe, camera);
                Assert.That(Named(overlay, "Next question").transform.IsChildOf(quizScroll.viewport), Is.False);
                Capture(camera, target, size.x + "x" + size.y + "-quiz");
                quiz.ShowSummary(); layout.Refresh(true); yield return null; layout.Refresh(true); Canvas.ForceUpdateCanvases();
                Capture(camera, target, size.x + "x" + size.y + "-progress"); quiz.Close();
                camera.targetTexture = null; Object.Destroy(target); yield return null;
            }
        }
    }
}
