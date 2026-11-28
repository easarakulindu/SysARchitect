using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.Rendering;
using SysARchitect.AR;
using SysARchitect.Lessons;
using SysARchitect.Simulation;
using SysARchitect.UI;

namespace SysARchitect.Tests
{
    public class LessonNarrationTests
    {
        private static void Click(string name) => Object.FindObjectsByType<Button>(FindObjectsInactive.Include)
            .Single(b => b.name == name).onClick.Invoke();
        [UnityTest]
        public IEnumerator LessonClicksAndStepsPlayMatchingAudioAndClosingStopsWithoutChangingProgress()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity"); yield return null;
            var voice = Object.FindAnyObjectByType<LessonNarration>();
            var lessons = Object.FindAnyObjectByType<LessonController>();
            var simulation = Object.FindAnyObjectByType<SimulationController>();
            var ui = Object.FindAnyObjectByType<LessonUI>();
            var store = new MemoryStorage(); lessons.UseProgress(store); lessons.Progress.Complete(LessonId.ClientServer);
            string saved = store.Json; double clock = simulation.Engine.Time;
            Click("Lessons / Progress"); yield return null;
            foreach (var lesson in LessonCatalog.All)
            {
                Click("Lessons"); Assert.That(voice.IsPlaying, Is.False);
                Click("Lesson choice " + (System.Array.IndexOf(LessonCatalog.All.ToArray(), lesson) + 1)); yield return null;
                for (int step = 0; step < lesson.Steps.Count; step++)
                {
                    if (step > 0) { Click("Next step"); yield return null; }
                    Assert.That(voice.CurrentResource, Is.EqualTo(LessonNarration.ResourceFor(lesson.Id, step)));
                    Assert.That(voice.CurrentClip, Is.Not.Null);
                    Assert.That(voice.CurrentClip.length, Is.GreaterThan(2));
                    Assert.That(ui.InstructionStep, Is.EqualTo(step));
                }
                Click("Next step"); Assert.That(ui.InstructionStep, Is.EqualTo(3));
                Click("Previous step"); yield return null;
                Assert.That(voice.CurrentResource, Is.EqualTo(LessonNarration.ResourceFor(lesson.Id, 2)));
            }
            Assert.That(voice.IsPlaying, Is.True);
            Click("Pause voice"); Assert.That(voice.Paused, Is.True); Assert.That(voice.IsPlaying, Is.False);
            Click("Pause voice"); yield return null; Assert.That(voice.Paused, Is.False); Assert.That(voice.IsPlaying, Is.True);
            Click("Mute voice"); Assert.That(voice.Muted, Is.True); Assert.That(voice.IsPlaying, Is.False);
            Click("Previous step"); yield return null; Assert.That(voice.IsPlaying, Is.False);
            Click("Mute voice"); yield return null; Assert.That(voice.IsPlaying, Is.True);
            Click("Replay voice"); yield return null; Assert.That(voice.GetComponent<AudioSource>().time, Is.LessThan(1));
            Click("Pre-quiz"); yield return null; Assert.That(voice.IsPlaying, Is.False);
            Click("Close / Return to guide"); yield return null;
            Click("Replay voice"); yield return null; Click("Close guide"); Assert.That(voice.IsPlaying, Is.False);
            Assert.That(store.Json, Is.EqualTo(saved)); Assert.That(simulation.Engine.Time, Is.EqualTo(clock));
        }
        private sealed class MemoryStorage : IProgressStorage
        {
            public string Json = "";
            public string Read() => Json;
            public void Write(string json) => Json = json;
        }
        [UnityTest]
        public IEnumerator MobileVoiceControlsRemainVisibleAtFourPhoneResolutions()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/ARWorkspace.unity");
            var workspace = Object.FindAnyObjectByType<ARWorkspace>();
            float deadline = Time.realtimeSinceStartup + 10;
            while (workspace.Mode == ARWorkspace.WorkspaceMode.Checking && Time.realtimeSinceStartup < deadline) yield return null;
            if (workspace.Mode != ARWorkspace.WorkspaceMode.Virtual) workspace.ToggleWorkspaceMode();
            yield return null; Click("Lessons / Progress"); yield return null;
            var canvas = GameObject.Find("Workspace UI").GetComponent<Canvas>();
            canvas.GetComponent<CanvasScaler>().enabled = false;
            var camera = Camera.main; canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = .3f;
            var safe = canvas.transform.Find("Safe Area").GetComponent<RectTransform>();
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(2400, 1080), new Vector2Int(2796, 1290) })
            {
                var target = new RenderTexture(size.x, size.y, 24); camera.targetTexture = target; camera.aspect = size.x / (float)size.y;
                canvas.scaleFactor = Mathf.Min(size.x / 1280f, size.y / 720f);
                MobileWorkspaceUI.ApplySafeArea(safe, new Rect(50, 20, size.x - 100, size.y - 40), size);
                yield return null; yield return null; Canvas.ForceUpdateCanvases();
                foreach (string name in new[] { "Replay voice", "Pause voice", "Mute voice", "Next step", "Previous step" })
                {
                    var button = Object.FindObjectsByType<Button>().Single(b => b.name == name);
                    var rect = button.GetComponent<RectTransform>(); var corners = new Vector3[4]; rect.GetWorldCorners(corners);
                    Assert.That(rect.rect.height, Is.GreaterThanOrEqualTo(95));
                    foreach (var corner in corners) Assert.That(safe.rect.Contains((Vector2)safe.InverseTransformPoint(corner)), Is.True, name + " must stay in the safe area");
                    Assert.That(button.transform.parent.name, Is.EqualTo("Step controls"), "Voice controls remain pinned while instruction text scrolls");
                }
                var previous = RenderTexture.active; Texture2D pixels = null;
                try
                {
                    RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                    RenderTexture.active = target; pixels = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
                    pixels.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0); pixels.Apply();
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.temporaryCachePath, $"sysarchitect-lesson-voice-{size.x}.png"), pixels.EncodeToPNG());
                }
                finally { camera.targetTexture = null; RenderTexture.active = previous; if (pixels != null) Object.Destroy(pixels); target.Release(); Object.Destroy(target); }
            }
        }
    }
}
