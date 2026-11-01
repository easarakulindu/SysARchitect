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
        private LessonId stepLesson;
        private bool scrollToStep = true;
        public int InstructionStep => step;
        public bool ShowingLessonSelection => lessonSelectionPanel.activeSelf;
        private string previousKey;
        private readonly StringBuilder text = new StringBuilder();
        private void Start()
        {
            panel.SetActive(false);
            lessonSelectionPanel.SetActive(false);
            stepLesson = lessons.Current.Id;
            if (title.GetComponent<LayoutElement>() == null) title.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 44);
            toggleButton.onClick.AddListener(() => { panel.SetActive(!panel.activeSelf); scrollToStep = true; previousKey = null; });
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
            previousStep.onClick.AddListener(() => ChangeStep(-1));
            nextStep.onClick.AddListener(() => ChangeStep(1));
        }
        private void ChangeStep(int direction)
        {
            int target = Mathf.Clamp(step + direction, 0, lessons.Current.Steps.Count - 1);
            if (target == step) return;
            step = target; scrollToStep = true;
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
            step = 0; stepLesson = id; scrollToStep = true; lessonSelectionPanel.SetActive(false); previousKey = null;
        }
        private void OnDisable() { if (lessons != null) lessons.SetInstructionFocus(-1); }
        private void Update()
        {
            loadExample.interactable = workspace == null || workspace.CanInteract;
            if (stepLesson != lessons.Current.Id) { step = 0; stepLesson = lessons.Current.Id; scrollToStep = true; }
            lessons.SetInstructionFocus(panel.activeSelf && !ShowingLessonSelection ? step : -1);
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
            var current = lesson.StepDetails[step];
            title.text = choosing ? $"Lessons | Completed {lessons.Progress.CompletedCount}/{LessonCatalog.All.Count}"
                : $"{lesson.Title}\n{current.Heading}";
            stepLabel.text = choosing ? "Choose a lesson" : $"Instruction step {step + 1}/{lesson.Steps.Count}\n{(lessons.Progress.IsComplete(lesson.Id) ? "Challenge completed" : "Challenge pending")}";
            for (int i = 0; i < lessonChoices.Length; i++)
            {
                var item = LessonCatalog.All[i];
                lessonChoices[i].GetComponentInChildren<TextMeshProUGUI>().text = $"{i + 1}. {item.Title}\n{(lessons.Progress.IsComplete(item.Id) ? "Completed — revisit" : "Available")}";
            }
            text.Clear();
            text.Append($"<b>{current.Heading}</b>\n{lesson.Steps[step]}\n\n<b>WHY THIS STEP</b>\n{current.Explanation}\n\n");
            text.Append("<b>ARCHITECTURE FOCUS</b>\n" + string.Join(", ", current.FocusTypes) + ". Mint highlights identify existing components and links; selection and traffic colors take priority. No nodes or links are created by navigation.\n\n");
            text.Append("<b>NAVIGATION</b>\nPrevious lesson / Next lesson change the topic. Lessons lists all five topics. Previous step / Next step change only these instructions; Simulation's Step 50 ms advances the simulation. Navigating does not load or clear an example.\n\n");
            text.Append("<b>MEASURABLE OBJECTIVES</b>\n" + lesson.Objectives + "\n\n");
            text.Append("<b>LESSON OVERVIEW</b>\n" + lesson.Explanation + "\n\n");
            text.Append("<b>LIVE CONCEPTUAL ALGORITHM</b> (highlight = most recent actual simulation/controller action)\n");
            for (int i = 0; i < lesson.Algorithm.Count; i++)
                text.Append(i == lessons.ActiveAlgorithmLine ? "<color=#40FFFF>" + lesson.Algorithm[i] + "</color>\n" : lesson.Algorithm[i] + "\n");
            text.Append("\n<b>PRACTICAL CHALLENGE</b>\n" + lesson.Challenge + "\n\n<b>FEEDBACK</b>\n" + lessons.Feedback);
            body.text = text.ToString();
            if (scrollToStep && !choosing)
            {
                scrollToStep = false;
                var element = body.GetComponent<LayoutElement>();
                if (element != null) element.preferredHeight = Mathf.Max(64, body.preferredHeight + 12);
                Canvas.ForceUpdateCanvases();
                foreach (var scroll in body.GetComponentsInParent<ScrollRect>(true))
                    if (scroll.enabled) { LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content); scroll.StopMovement(); scroll.verticalNormalizedPosition = 1; }
            }
        }
    }
}
