using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SysARchitect.Interaction;

namespace SysARchitect.AR
{
    /// <summary>Reflows the existing panels and commands for landscape touch; owns no simulation state.</summary>
    public sealed class MobilePanelLayout
    {
        private readonly RectTransform root;
        private readonly GameObject toolbox, simulation, details, guide, quiz;
        private readonly List<GameObject> detailPanels = new List<GameObject>();
        private readonly List<TextMeshProUGUI> flowingText = new List<TextMeshProUGUI>();
        private readonly List<Button> buttons = new List<Button>();
        private readonly InteractionManager interactions;
        private readonly NodeSelectionController selection;
        private Button nodeDetails, serviceDetails;
        private bool serviceTab;
        private bool previousHasService;
        private Object previousSelection;
        private float nextRefresh;
        private InteractionManager.Mode previousMode;
        private readonly System.Func<bool> trackingAllowsInput;
        private readonly List<CanvasGroup> trackedControls = new List<CanvasGroup>();
        public const float TouchHeight = 96;
        public MobilePanelLayout(RectTransform root, GameObject toolbox, Button toggle, Button reposition,
            NodeSelectionController selection, InteractionManager interactions, System.Func<bool> trackingAllowsInput = null,
            UnityEngine.Events.UnityAction switchWorkspace = null)
        {
            this.root = root; this.toolbox = toolbox; this.interactions = interactions;
            this.selection = selection;
            this.trackingAllowsInput = trackingAllowsInput;
            var scaler = root.GetComponentInParent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(1280, 720);
            previousMode = interactions.CurrentMode;
            simulation = root.Find("Simulation").gameObject;
            guide = root.Find("Learning Guide").gameObject; quiz = root.Find("Quiz Overlay").gameObject;
            guide.GetComponent<Image>().color = new Color(.02f, .035f, .07f, 1);
            var bar = root.Find("AR Controls").GetComponent<RectTransform>();
            Dock(bar, 0, 1, 1, 1, 0, -184, 0, 0);
            var row = NewRect(bar, "Touch toolbar"); Dock(row, 0, 1, 1, 1, 8, -104, -8, -8);
            var horizontal = row.gameObject.AddComponent<HorizontalLayoutGroup>(); horizontal.spacing = 8;
            horizontal.childControlWidth = horizontal.childControlHeight = true; horizontal.childForceExpandWidth = true;
            toggle.transform.SetParent(row, false); reposition.transform.SetParent(row, false);
            NewButton(row, "Simulation", () => { simulation.SetActive(!simulation.activeSelf); toolbox.SetActive(false); });
            NewButton(row, "Hide details", selection.Deselect);
            NewButton(row, "Virtual / AR", switchWorkspace ?? (() => { }));
            foreach (Transform child in bar)
                if (child.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI text)
                { Dock(text.rectTransform, 0, 0, 1, 0, 12, 8, -12, 72); text.fontSize = 26; text.textWrappingMode = TextWrappingModes.Normal; }

            var toolboxContent = ScrollPanel(toolbox.GetComponent<RectTransform>(), "Mobile toolbox scroll");
            var components = toolboxContent.Find("Components");
            DisableScroll(components);
            Flow(components.GetComponent<RectTransform>());
            var lessonButton = Child(toolboxContent, "Lessons / Progress"); lessonButton.SetParent(row, false);
            var editTools = NewRect(toolboxContent, "Editing tools");
            var originalChildren = new List<Transform>(); foreach (Transform child in toolboxContent) originalChildren.Add(child);
            foreach (var child in originalChildren)
                if (child.GetComponent<Button>() != null) child.SetParent(editTools, false);
            Flow(editTools);
            Flow(toolboxContent);
            foreach (Transform child in toolboxContent)
                if (child.GetComponent<TextMeshProUGUI>() != null) child.gameObject.SetActive(false);
            var toolboxScroll = toolboxContent.parent.GetComponent<ScrollRect>();
            toolboxScroll.viewport.offsetMax = new Vector2(-8, -112);
            var tabs = NewRect(toolbox.transform, "Toolbox tabs"); Dock(tabs, 0, 1, 1, 1, 8, -104, -8, -8);
            var tabLayout = tabs.gameObject.AddComponent<HorizontalLayoutGroup>(); tabLayout.spacing = 8;
            tabLayout.childControlWidth = tabLayout.childControlHeight = true;
            void ShowTools(bool editing)
            {
                components.gameObject.SetActive(!editing); editTools.gameObject.SetActive(editing);
                toolboxScroll.verticalNormalizedPosition = 1;
            }
            NewButton(tabs, "Components", () => ShowTools(false)); NewButton(tabs, "Edit / Connect", () => ShowTools(true));
            ShowTools(false);
            Track(components.gameObject); Track(editTools.gameObject);
            Dock(toolbox.GetComponent<RectTransform>(), 0, 0, 0, 1, 0, 116, 380, -192);
            toolbox.SetActive(false);

            var simulationContent = ScrollPanel(simulation.GetComponent<RectTransform>(), "Mobile simulation scroll");
            Flow(simulationContent);
            Track(Child(simulationContent, "Run").gameObject); Track(Child(simulationContent, "Step 50 ms").gameObject);
            Dock(simulation.GetComponent<RectTransform>(), 0, 0, 0, 1, 0, 116, 460, -192);
            simulation.SetActive(false);

            var drawer = NewRect(root, "Mobile details drawer"); details = drawer.gameObject;
            drawer.gameObject.AddComponent<Image>().color = new Color(.025f, .055f, .1f, .98f);
            Dock(drawer, 1, 0, 1, 1, -460, 116, 0, -192);
            var content = ScrollPanel(drawer, "Details scroll");
            foreach (string name in new[] { "Node Inspector", "Connection Inspector", "Routing Inspector", "Database Explorer", "Docker Explorer", "Kubernetes Explorer" })
            {
                var panel = root.Find(name); if (panel == null) continue;
                detailPanels.Add(panel.gameObject); panel.SetParent(content, false); Flow(panel.GetComponent<RectTransform>());
                if (name == "Routing Inspector")
                    Group(panel, "Routing roles", Child(panel, "Application Routing"), Child(panel, "Database Router")).SetSiblingIndex(1);
            }
            Flow(content);
            Track(content.gameObject);
            content.parent.GetComponent<ScrollRect>().viewport.offsetMax = new Vector2(-8, -112);
            var detailTabs = NewRect(drawer, "Detail tabs"); Dock(detailTabs, 0, 1, 1, 1, 8, -104, -8, -8);
            var detailLayout = detailTabs.gameObject.AddComponent<HorizontalLayoutGroup>(); detailLayout.spacing = 8;
            detailLayout.childControlHeight = detailLayout.childControlWidth = true;
            nodeDetails = NewButton(detailTabs, "Node / Connection", () => { serviceTab = false; content.parent.GetComponent<ScrollRect>().verticalNormalizedPosition = 1; });
            serviceDetails = NewButton(detailTabs, "Service", () => { serviceTab = true; content.parent.GetComponent<ScrollRect>().verticalNormalizedPosition = 1; });

            var explanation = guide.transform.Find("Scrollable explanation");
            DisableScroll(explanation); Flow(explanation.GetComponent<RectTransform>()); explanation.SetAsLastSibling();
            var guideTitle = guide.transform.GetChild(0);
            var lessonHeader = Group(guide.transform, "Lesson header", guideTitle, guide.transform.Find("Close guide")); lessonHeader.SetAsFirstSibling();
            var lessonNavigation = Group(guide.transform, "Lesson navigation", guide.transform.Find("Previous lesson"), guide.transform.Find("Lessons"), guide.transform.Find("Next lesson"));
            Group(guide.transform, "Lesson actions", guide.transform.Find("Load example (clears workspace)"), guide.transform.Find("Check challenge")).SetSiblingIndex(1);
            var lessonPicker = guide.transform.Find("Lesson selection").GetComponent<RectTransform>(); lessonPicker.SetParent(root, false);
            Transform step = null;
            foreach (Transform child in guide.transform) if (child.GetComponent<TextMeshProUGUI>() != null) step = child;
            var stepControls = guide.transform.Find("Replay voice") != null
                ? Group(guide.transform, "Step controls", guide.transform.Find("Previous step"), step, guide.transform.Find("Next step"),
                    guide.transform.Find("Replay voice"), guide.transform.Find("Pause voice"), guide.transform.Find("Mute voice"))
                : Group(guide.transform, "Step controls", guide.transform.Find("Previous step"), step, guide.transform.Find("Next step"));
            stepControls.SetSiblingIndex(2);
            Group(guide.transform, "Quiz choices", guide.transform.Find("Pre-quiz"), guide.transform.Find("Post-quiz"), Child(guide.transform, "Scores / Progress summary")).SetSiblingIndex(3);
            var lessonContent = ScrollPanel(guide.GetComponent<RectTransform>(), "Mobile lesson scroll");
            PinHeaderAndFooter(guide.transform, lessonContent, lessonHeader, stepControls); Flow(lessonContent);
            explanation.SetAsFirstSibling(); // Current instruction must precede action rows and static overview.
            lessonNavigation.SetParent(guide.transform, false); Dock(lessonNavigation, 0, 1, 1, 1, 8, -216, -8, -120);
            lessonContent.parent.GetComponent<ScrollRect>().viewport.offsetMax = new Vector2(-8, -224);
            lessonPicker.SetParent(guide.transform, false); Dock(lessonPicker, 0, 0, 1, 1, 8, 112, -8, -224);
            Flow(lessonPicker.Find("Lesson choices").GetComponent<RectTransform>());
            Dock(guide.GetComponent<RectTransform>(), 0, 0, 1, 1, 8, 8, -8, -8);
            var card = quiz.transform.Find("Quiz Card").GetComponent<RectTransform>();
            var quizHeader = Group(card, "Quiz header", card.GetChild(0), Child(card, "Close / Return to guide")); quizHeader.SetAsFirstSibling();
            Transform counter = null;
            foreach (Transform child in card)
                if (child.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI label && label.text.StartsWith("Answered")) counter = child;
            var quizNavigation = Group(card, "Quiz navigation", counter, card.Find("Next question"));
            var quizContent = ScrollPanel(card, "Mobile quiz scroll");
            PinHeaderAndFooter(card, quizContent, quizHeader, quizNavigation); Flow(quizContent);
            Dock(card, 0, 0, 1, 1, 8, 8, -8, -8);
            var feedback = root.Find("Feedback").GetComponent<RectTransform>();
            Dock(feedback, 0, 0, 1, 0, 0, 0, 0, 108);
            Flow(ScrollPanel(feedback, "Validation messages"));
            foreach (var label in feedback.GetComponentsInChildren<TextMeshProUGUI>()) label.fontSize = 28;
            foreach (Transform child in root)
                if (child.GetComponent<TextMeshProUGUI>() != null) child.gameObject.SetActive(false);
            foreach (var layout in root.GetComponentsInChildren<VerticalLayoutGroup>(true))
            {
                layout.childControlHeight = layout.childControlWidth = true;
                layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            }
            guide.transform.SetAsLastSibling(); quiz.transform.SetAsLastSibling();
            Refresh(true);
        }
        private void Track(GameObject control)
        {
            var group = control.GetComponent<CanvasGroup>();
            if (group == null) group = control.AddComponent<CanvasGroup>();
            trackedControls.Add(group);
        }
        private static void PinHeaderAndFooter(Transform panel, RectTransform content, RectTransform header, RectTransform footer)
        {
            header.SetParent(panel, false); footer.SetParent(panel, false);
            Dock(header, 0, 1, 1, 1, 8, -104, -8, -8); Dock(footer, 0, 0, 1, 0, 8, 8, -8, 104);
            var viewport = content.parent.GetComponent<ScrollRect>().viewport;
            viewport.offsetMin = new Vector2(8, 112); viewport.offsetMax = new Vector2(-8, -112);
        }
        private static Transform Child(Transform parent, string name)
        {
            // Existing UI names can contain '/', so Transform.Find would interpret them as paths.
            foreach (Transform child in parent) if (child.name == name) return child;
            throw new System.InvalidOperationException("Missing mobile control: " + name);
        }
        private static RectTransform Group(Transform parent, string name, params Transform[] children)
        {
            var row = NewRect(parent, name); var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10; layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            row.gameObject.AddComponent<LayoutElement>().minHeight = TouchHeight;
            foreach (var child in children)
            {
                child.SetParent(row, false);
                var element = child.GetComponent<LayoutElement>() ?? child.gameObject.AddComponent<LayoutElement>();
                element.minHeight = element.preferredHeight = TouchHeight; element.flexibleWidth = 1;
                if (child.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI label)
                { label.fontSize = 28; label.enableAutoSizing = false; label.textWrappingMode = TextWrappingModes.Normal; }
            }
            return row;
        }
        private static RectTransform NewRect(Transform parent, string name)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); return rect;
        }
        private static void Dock(RectTransform rect, float ax, float ay, float bx, float by, float left, float bottom, float right, float top)
        {
            rect.anchorMin = new Vector2(ax, ay); rect.anchorMax = new Vector2(bx, by);
            rect.pivot = new Vector2(.5f, .5f); rect.offsetMin = new Vector2(left, bottom); rect.offsetMax = new Vector2(right, top);
        }
        private static Button NewButton(Transform parent, string name, UnityEngine.Events.UnityAction action)
        {
            var rect = NewRect(parent, name); rect.gameObject.AddComponent<Image>().color = new Color(.08f, .22f, .29f);
            var button = rect.gameObject.AddComponent<Button>(); button.onClick.AddListener(action);
            var label = NewRect(rect, "Label").gameObject.AddComponent<TextMeshProUGUI>();
            Dock(label.rectTransform, 0, 0, 1, 1, 8, 4, -8, -4); label.text = name;
            label.fontSize = 28; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
            return button;
        }
        private static void DisableScroll(Transform target)
        {
            var scroll = target.GetComponent<ScrollRect>(); if (scroll != null) scroll.enabled = false;
            var mask = target.GetComponent<RectMask2D>(); if (mask != null) mask.enabled = false;
        }
        private RectTransform ScrollPanel(RectTransform panel, string name)
        {
            var children = new List<Transform>(); foreach (Transform child in panel) children.Add(child);
            var viewport = NewRect(panel, name); Dock(viewport, 0, 0, 1, 1, 8, 8, -8, -8);
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .01f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = NewRect(viewport, "Content"); content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1); content.sizeDelta = Vector2.zero; content.anchoredPosition = Vector2.zero;
            foreach (var child in children) child.SetParent(content, false);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = content;
            scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 32;
            return content;
        }
        private void Flow(RectTransform panel)
        {
            var layout = panel.GetComponent<VerticalLayoutGroup>() ?? panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8); layout.spacing = 10;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            var fitter = panel.GetComponent<ContentSizeFitter>() ?? panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            foreach (Transform child in panel)
            {
                var element = child.GetComponent<LayoutElement>() ?? child.gameObject.AddComponent<LayoutElement>();
                element.minHeight = 64;
                if (child.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI label)
                {
                    label.fontSize = 30; label.enableAutoSizing = false; label.textWrappingMode = TextWrappingModes.Normal;
                    flowingText.Add(label);
                }
                else if (child.GetComponent<Selectable>() != null)
                {
                    element.minHeight = element.preferredHeight = TouchHeight;
                    if (child.GetComponent<Slider>() is Slider slider && slider.handleRect != null)
                        slider.handleRect.sizeDelta = new Vector2(44, TouchHeight);
                    if (child.GetComponent<TMP_InputField>() is TMP_InputField input)
                    {
                        Dock(input.textComponent.rectTransform, 0, 0, 1, 1, 12, 8, -12, -8);
                        input.textComponent.fontSize = 30;
                    }
                }
                else if (child.GetComponent<ScrollRect>() is ScrollRect scroll && scroll.enabled)
                    element.minHeight = element.preferredHeight = 220;
            }
        }
        public void Refresh(bool force = false)
        {
            bool modal = guide.activeSelf || quiz.activeSelf;
            var mode = interactions.CurrentMode;
            if (modal || (mode != previousMode && (mode == InteractionManager.Mode.Place || mode == InteractionManager.Mode.Connect)))
                toolbox.SetActive(false);
            previousMode = mode;
            if (toolbox.activeSelf) simulation.SetActive(false);
            bool tracked = trackingAllowsInput == null || trackingAllowsInput();
            foreach (var group in trackedControls) if (group.interactable != tracked) group.interactable = tracked;
            bool showDetails = false; foreach (var panel in detailPanels) showDetails |= panel.activeSelf;
            details.SetActive(showDetails && !modal);
            bool hasService = detailPanels.Exists(panel => panel.activeSelf && panel.name != "Node Inspector" && panel.name != "Connection Inspector");
            Object selected = selection.SelectedNode;
            if (selected == null) selected = selection.SelectedConnection;
            if (selected != previousSelection || (hasService && !previousHasService)) { serviceTab = hasService; previousSelection = selected; }
            previousHasService = hasService;
            if (!hasService) serviceTab = false;
            serviceDetails.interactable = hasService;
            foreach (var panel in detailPanels)
            {
                bool isService = panel.name != "Node Inspector" && panel.name != "Connection Inspector";
                bool visible = serviceTab == isService;
                var group = panel.GetComponent<CanvasGroup>(); if (group == null) group = panel.AddComponent<CanvasGroup>();
                float alpha = visible ? 1 : 0;
                if (group.alpha != alpha) group.alpha = alpha;
                if (group.blocksRaycasts != visible) group.blocksRaycasts = visible;
                if (group.interactable != visible) group.interactable = visible;
                panel.GetComponent<LayoutElement>().ignoreLayout = !visible;
            }
            nodeDetails.GetComponent<Image>().color = !serviceTab ? new Color(.1f, .4f, .5f) : new Color(.08f, .22f, .29f);
            serviceDetails.GetComponent<Image>().color = serviceTab ? new Color(.1f, .4f, .5f) : new Color(.08f, .22f, .29f);
            if (modal) simulation.SetActive(false);
            if (!force && Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .2f;
            foreach (var label in flowingText)
                label.GetComponent<LayoutElement>().preferredHeight = Mathf.Max(64, label.preferredHeight + 12);
            root.GetComponentsInChildren(true, buttons);
            foreach (var button in buttons)
            {
                var element = button.GetComponent<LayoutElement>() ?? button.gameObject.AddComponent<LayoutElement>();
                element.minHeight = element.preferredHeight = TouchHeight;
                element.flexibleWidth = 1;
                var label = button.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) { label.fontSize = 28; label.enableAutoSizing = false; }
            }
        }
    }
}
