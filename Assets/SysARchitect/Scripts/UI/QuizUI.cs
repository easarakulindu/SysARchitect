using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SysARchitect.AR;
using SysARchitect.Core;
using SysARchitect.Desktop;
using SysARchitect.Interaction;
using SysARchitect.Lessons;
using SysARchitect.Quiz;
using SysARchitect.Simulation;

namespace SysARchitect.UI
{
    public class QuizUI : MonoBehaviour
    {
        [SerializeField] private LessonController lessons;
        [SerializeField] private SimulationController simulation;
        [SerializeField] private InteractionManager interactions;
        [SerializeField] private NodeSelectionController selection;
        [SerializeField] private NodeMovementController movement;
        [SerializeField] private WorkspaceTouchInput touchInput;
        [SerializeField] private DesktopCameraController desktopCamera;
        [SerializeField] private GameObject overlay, guide;
        [SerializeField] private Button preQuiz, postQuiz, progress, next, close;
        [SerializeField] private Button[] answers;
        [SerializeField] private TextMeshProUGUI title, question, feedback, counter;
        private bool suspended, previousSelectionInput, previousMovementInput, previousTouch, previousCamera;
        public QuizSession Session { get; private set; }
        private readonly StringBuilder text = new StringBuilder();
        private void Start()
        {
            overlay.SetActive(false);
            preQuiz.onClick.AddListener(() => Open(QuizStage.Pre)); postQuiz.onClick.AddListener(() => Open(QuizStage.Post));
            progress.onClick.AddListener(ShowSummary); next.onClick.AddListener(Next); close.onClick.AddListener(Close);
            for (int i = 0; i < answers.Length; i++) { int choice = i; answers[i].onClick.AddListener(() => Answer(choice)); }
        }
        private void SuspendInput()
        {
            simulation.Pause(); interactions.ReturnToSelectMode(); selection.Deselect();
            if (!suspended)
            {
                previousSelectionInput = selection.UseExternalPointer; previousMovementInput = movement.UseExternalPointer;
                previousTouch = touchInput != null && touchInput.enabled; previousCamera = desktopCamera != null && desktopCamera.enabled;
                suspended = true;
            }
            selection.UseExternalPointer = movement.UseExternalPointer = true;
            if (touchInput != null) touchInput.enabled = false;
            if (desktopCamera != null) desktopCamera.enabled = false;
            guide.SetActive(false); overlay.SetActive(true);
            AppStateManager.Instance?.SetMode(AppMode.Quiz);
        }
        private void RestoreInput()
        {
            if (!suspended) return;
            if (selection != null) selection.UseExternalPointer = previousSelectionInput;
            if (movement != null) movement.UseExternalPointer = previousMovementInput;
            if (touchInput != null) touchInput.enabled = previousTouch;
            if (desktopCamera != null) desktopCamera.enabled = previousCamera;
            suspended = false;
        }
        private void OnDisable() => RestoreInput();
        private void Update() { if (overlay.activeSelf && Input.GetKeyDown(KeyCode.Escape)) Close(); }
        public void Open(QuizStage stage)
        {
            var session = new QuizSession(stage); SuspendInput(); Session = session;
            feedback.rectTransform.anchoredPosition = new Vector2(32, -510); feedback.rectTransform.sizeDelta = new Vector2(916, 128);
            next.gameObject.SetActive(true); foreach (var button in answers) button.gameObject.SetActive(true);
            RefreshQuestion();
        }
        public void Answer(int choice)
        {
            if (Session == null || Session.Answered) return;
            Session.Answer(choice);
            for (int i = 0; i < answers.Length; i++)
            {
                answers[i].interactable = false;
                answers[i].GetComponent<Image>().color = i == Session.Current.CorrectAnswer ? new Color(.08f, .42f, .3f)
                    : i == choice ? new Color(.45f, .12f, .2f) : new Color(.08f, .22f, .29f);
            }
            feedback.text = Session.Feedback;
            if (Session.Complete) lessons.Progress.RecordQuiz(Session);
            next.interactable = true;
            next.GetComponentInChildren<TextMeshProUGUI>().text = Session.Complete ? "View summary" : "Next question";
            counter.text = $"Answered {Session.Answers.Count}/{Session.Count} | Correct {Session.Correct}";
        }
        public void Next()
        {
            if (Session == null || !Session.Answered) return;
            if (Session.Complete) { ShowSummary(); return; }
            Session.Next(); RefreshQuestion();
        }
        private void RefreshQuestion()
        {
            title.text = $"{Session.Stage}-quiz | {Session.Index + 1}/{Session.Count} | {LessonCatalog.Get(Session.Current.Lesson).Title}";
            question.text = Session.Current.Prompt; feedback.text = "Choose one answer. Feedback explains the correct reasoning. Only completed attempts are saved locally.";
            for (int i = 0; i < answers.Length; i++)
            {
                answers[i].interactable = true; answers[i].GetComponent<Image>().color = new Color(.08f, .22f, .29f);
                answers[i].GetComponentInChildren<TextMeshProUGUI>().text = $"{(char)('A' + i)}. {Session.Current.Choices[i]}";
            }
            next.interactable = false; next.GetComponentInChildren<TextMeshProUGUI>().text = "Next question";
            counter.text = $"Answered {Session.Answers.Count}/{Session.Count} | Correct {Session.Correct}";
        }
        public void ShowSummary()
        {
            SuspendInput(); Session = null; foreach (var button in answers) button.gameObject.SetActive(false); next.gameObject.SetActive(false);
            title.text = "Your local learning progress";
            question.text = "Verified lesson completion and completed practice quizzes are stored on this device. Pre and post use different question sets.";
            feedback.rectTransform.anchoredPosition = new Vector2(32, -180); feedback.rectTransform.sizeDelta = new Vector2(916, 450);
            text.Clear();
            foreach (var lesson in LessonCatalog.All) text.Append($"{(lessons.Progress.IsComplete(lesson.Id) ? "Complete" : "Pending")}: {lesson.Title}\n");
            text.Append($"\nLessons completed: {lessons.Progress.CompletedCount}/5\n");
            foreach (QuizStage stage in new[] { QuizStage.Pre, QuizStage.Post })
            {
                var result = lessons.Progress.LatestQuiz(stage);
                text.Append(result == null ? $"{stage}-quiz: not taken\n" : $"Latest {stage}-quiz: {result.correct}/{result.total} ({100f * result.correct / result.total:F0}%)\n");
            }
            text.Append($"\nSaved completed attempts: {lessons.Progress.QuizAttempts.Count} (latest 20 retained).\nScores reflect your completed practice attempts on this device. Different forms provide practice feedback.");
            feedback.text = text.ToString(); counter.text = "Close to return to the guide. Simulation stays paused.";
        }
        public void Close()
        {
            overlay.SetActive(false); guide.SetActive(true); Session = null; RestoreInput(); AppStateManager.Instance?.SetMode(AppMode.Lesson);
        }
    }
}
