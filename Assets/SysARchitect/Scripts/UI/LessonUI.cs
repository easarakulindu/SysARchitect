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
        [SerializeField] private Button previousLesson, lessonListButton;
        [SerializeField] private GameObject lessonSelectionPanel;
        [SerializeField] private Button[] lessonChoices;
        [SerializeField] private TextMeshProUGUI title, body, stepLabel;
        private int step;
        public int InstructionStep => step;
        public bool ShowingLessonSelection => lessonSelectionPanel.activeSelf;
        private string previousKey;
        private readonly StringBuilder text = new StringBuilder();
        private void Start()
        {
            panel.SetActive(false);
            lessonSelectionPanel.SetActive(false);
            toggleButton.onClick.AddListener(() => panel.SetActive(!panel.activeSelf));
            closeButton.onClick.AddListener(() => panel.SetActive(false));
            previousLesson.onClick.AddListener(() => ChangeLesson(-1));
            nextLesson.onClick.AddListener(() => ChangeLesson(1));
            lessonListButton.onClick.AddListener(() => { lessonSelectionPanel.SetActive(true); previousKey = null; });
            for (int i = 0; i < lessonChoices.Length; i++)
            {
                var id = LessonCatalog.All[i].Id;
                lessonChoices[i].onClick.AddListener(() => ShowLesson(id));
            }
            loadExample.onClick.AddListener(() => { lessons.LoadExample(); panel.SetActive(false); step = 0; });
            checkChallenge.onClick.AddListener(() => lessons.CheckChallenge());
            previousStep.onClick.AddListener(() => step = Mathf.Max(0, step - 1));
            nextStep.onClick.AddListener(() => step = Mathf.Min(lessons.Current.Steps.Count - 1, step + 1));
        }
        private int CurrentLessonIndex()
        {
            for (int i = 0; i < LessonCatalog.All.Count; i++) if (LessonCatalog.All[i].Id == lessons.Current.Id) return i;
            return 0;
        }
        private void ChangeLesson(int direction)
        {
            if (ShowingLessonSelection) return;
            int index = CurrentLessonIndex(), target = Mathf.Clamp(index + direction, 0, LessonCatalog.All.Count - 1);
            if (target != index) ShowLesson(LessonCatalog.All[target].Id);
        }
        private void ShowLesson(LessonId id)
        {
            if (lessons.Current.Id != id) lessons.SelectLesson(id);
            step = 0; lessonSelectionPanel.SetActive(false); previousKey = null;
        }
        private void Update()
        {
            loadExample.interactable = workspace == null || workspace.CanInteract;
            if (!panel.activeSelf) return;
            var lesson = lessons.Current;
            bool choosing = ShowingLessonSelection; int index = CurrentLessonIndex();
            previousLesson.interactable = !choosing && index > 0;
            nextLesson.interactable = !choosing && index < LessonCatalog.All.Count - 1;
            lessonListButton.interactable = !choosing;
            previousStep.interactable = !choosing && step > 0;
            nextStep.interactable = !choosing && step < lesson.Steps.Count - 1;
            string key = lesson.Id + ":" + step + ":" + choosing + ":" + lessons.ActiveAlgorithmLine + ":" + lessons.Progress.CompletedCount + ":" + lessons.Feedback;
            if (key == previousKey) return; previousKey = key;
            title.text = choosing ? $"Lessons | Completed {lessons.Progress.CompletedCount}/{LessonCatalog.All.Count}"
                : $"Lesson {index + 1}/{LessonCatalog.All.Count}: {lesson.Title}";
            stepLabel.text = choosing ? "Choose a lesson" : $"Instruction step {step + 1}/{lesson.Steps.Count}\n{(lessons.Progress.IsComplete(lesson.Id) ? "Challenge completed" : "Challenge pending")}";
            for (int i = 0; i < lessonChoices.Length; i++)
            {
                var item = LessonCatalog.All[i];
                lessonChoices[i].GetComponentInChildren<TextMeshProUGUI>().text = $"{i + 1}. {item.Title}\n{(lessons.Progress.IsComplete(item.Id) ? "Completed — revisit" : "Available")}";
            }
            text.Clear();
            text.Append("<b>NAVIGATION</b>\nPrevious lesson / Next lesson change the topic. Lessons lists all five topics. Previous step / Next step change only these instructions; Simulation's Step 50 ms advances the simulation. Navigating does not load or clear an example.\n\n");
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
