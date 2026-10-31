using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SysARchitect.Interaction;
using SysARchitect.Lessons;
using SysARchitect.Quiz;
using SysARchitect.Simulation;
using SysARchitect.UI;

namespace SysARchitect.Tests
{
    public class QuizWorkspaceTests
    {
        private sealed class MemoryStore : IProgressStorage
        { public string Json = ""; public string Read() => Json; public void Write(string value) => Json = value; }
        private static void Answer(QuizUI ui, bool correct)
        {
            int choice = correct ? ui.Session.Current.CorrectAnswer : (ui.Session.Current.CorrectAnswer + 1) % 4;
            GameObject.Find("Workspace UI/Quiz Overlay/Quiz Card/Answer " + choice).GetComponent<Button>().onClick.Invoke();
        }
        [UnityTest]
        public IEnumerator QuizButtonsExplainAnswersSaveBothFormsAndShowRealSummary()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity"); yield return null;
            var store = new MemoryStore(); var lessons = Object.FindAnyObjectByType<LessonController>(); lessons.UseProgress(store);
            var ui = Object.FindAnyObjectByType<QuizUI>(); var selection = Object.FindAnyObjectByType<NodeSelectionController>();
            ui.Open(QuizStage.Pre); Assert.That(selection.UseExternalPointer, Is.True);
            for (int i = 0; i < 10; i++)
            {
                Answer(ui, i != 0); Assert.That(ui.Session.Feedback, Does.Contain(ui.Session.Current.Explanation));
                if (i == 0)
                {
                    yield return null;
                    if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null) DesktopWorkspaceTests.CaptureDemo("sysarchitect-quiz-feedback.png");
                }
                if (!ui.Session.Complete) ui.Next();
            }
            Assert.That(lessons.Progress.LatestQuiz(QuizStage.Pre).correct, Is.EqualTo(9)); ui.Next();
            Assert.That(ui.Session, Is.Null);
            Assert.That(GameObject.Find("Workspace UI/Quiz Overlay").GetComponentInChildren<TMPro.TextMeshProUGUI>().text, Is.Not.Empty);
            ui.Close(); Assert.That(selection.UseExternalPointer, Is.False);
            GameObject.Find("Workspace UI/Learning Guide/Post-quiz").GetComponent<Button>().onClick.Invoke();
            for (int i = 0; i < 10; i++) { Answer(ui, true); if (!ui.Session.Complete) ui.Next(); }
            ui.Next(); yield return null;
            Assert.That(lessons.Progress.QuizAttempts.Count, Is.EqualTo(2)); Assert.That(lessons.Progress.LatestQuiz(QuizStage.Post).correct, Is.EqualTo(10));
            Assert.That(new LearningProgress(store).LatestQuiz(QuizStage.Pre).correct, Is.EqualTo(9));
            Assert.That(GameObject.Find("Workspace UI/Quiz Overlay").GetComponentsInChildren<TMPro.TextMeshProUGUI>().Any(t => t.text.Contains("Latest Pre-quiz: 9/10") && t.text.Contains("Latest Post-quiz: 10/10")), Is.True);
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null) DesktopWorkspaceTests.CaptureDemo("sysarchitect-progress-summary.png");
            ui.Close(); Assert.That(selection.UseExternalPointer, Is.False);
        }
        [UnityTest]
        public IEnumerator UnfinishedQuizDoesNotCreateAScoreAndClosingRestoresInput()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity"); yield return null;
            var lessons = Object.FindAnyObjectByType<LessonController>(); lessons.UseProgress(new MemoryStore());
            var sim = Object.FindAnyObjectByType<SimulationController>(); var ui = Object.FindAnyObjectByType<QuizUI>();
            sim.Run(); ui.Open(QuizStage.Pre); double time = sim.Engine.Time; yield return null;
            Assert.That(sim.IsRunning, Is.False); Assert.That(sim.Engine.Time, Is.EqualTo(time));
            Answer(ui, true); ui.Close();
            Assert.That(lessons.Progress.QuizAttempts, Is.Empty); Assert.That(lessons.Progress.LatestQuiz(QuizStage.Pre), Is.Null);
            Assert.That(Object.FindAnyObjectByType<NodeSelectionController>().UseExternalPointer, Is.False);
            Assert.That(Object.FindAnyObjectByType<NodeMovementController>().UseExternalPointer, Is.False);
        }
    }
}
