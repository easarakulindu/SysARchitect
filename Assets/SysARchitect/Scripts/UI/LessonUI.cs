using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SysARchitect.AR;
using SysARchitect.Lessons;

namespace SysARchitect.UI
{
    public class LessonUI : MonoBehaviour
    {
        [SerializeField] private LessonController lessons;
        [SerializeField] private ARWorkspace workspace;
        [SerializeField] private GameObject panel;
        [SerializeField] private Button toggleButton, closeButton, nextLesson, loadExample, checkChallenge, previousStep, nextStep;
        [SerializeField] private TextMeshProUGUI title, body, stepLabel;
        private int lessonIndex, step;
        private string previousKey;
        private readonly StringBuilder text = new StringBuilder();
        private void Start()
        {
            panel.SetActive(false);
            toggleButton.onClick.AddListener(() => panel.SetActive(!panel.activeSelf));
            closeButton.onClick.AddListener(() => panel.SetActive(false));
            nextLesson.onClick.AddListener(() => { lessonIndex = (lessonIndex + 1) % LessonCatalog.All.Count; step = 0; lessons.SelectLesson(LessonCatalog.All[lessonIndex].Id); });
            loadExample.onClick.AddListener(() => { lessons.LoadExample(); panel.SetActive(false); step = 0; });
            checkChallenge.onClick.AddListener(() => lessons.CheckChallenge());
            previousStep.onClick.AddListener(() => step = Mathf.Max(0, step - 1));
            nextStep.onClick.AddListener(() => step = Mathf.Min(lessons.Current.Steps.Count - 1, step + 1));
        }
        private void Update()
        {
            loadExample.interactable = workspace == null || workspace.CanInteract;
            if (!panel.activeSelf) return;
            var lesson = lessons.Current;
            string key = lesson.Id + ":" + step + ":" + lessons.ActiveAlgorithmLine + ":" + lessons.Progress.CompletedCount + ":" + lessons.Feedback;
            if (key == previousKey) return; previousKey = key;
            title.text = $"{lesson.Title} | Completed {lessons.Progress.CompletedCount}/5";
            stepLabel.text = $"Step {step + 1}/{lesson.Steps.Count} | {(lessons.Progress.IsComplete(lesson.Id) ? "Challenge completed" : "Challenge pending")}";
            text.Clear();
            text.Append("<b>MEASURABLE OBJECTIVES</b>\n" + lesson.Objectives + "\n\n");
            text.Append(lesson.Explanation + "\n\n<b>CURRENT STEP</b>\n" + lesson.Steps[step] + "\n\n");
            text.Append("<b>LIVE CONCEPTUAL ALGORITHM</b> (highlight = most recent actual simulation/controller action)\n");
            for (int i = 0; i < lesson.Algorithm.Count; i++)
                text.Append(i == lessons.ActiveAlgorithmLine ? "<color=#40FFFF>" + lesson.Algorithm[i] + "</color>\n" : lesson.Algorithm[i] + "\n");
            text.Append("\n<b>PRACTICAL CHALLENGE</b>\n" + lesson.Challenge + "\n\n<b>FEEDBACK</b>\n" + lessons.Feedback);
            body.text = text.ToString();
        }
    }
}
