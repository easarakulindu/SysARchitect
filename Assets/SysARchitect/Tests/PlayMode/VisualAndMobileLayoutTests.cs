using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;
using SysARchitect.AR;
using SysARchitect.Core;
using SysARchitect.Graph;
using SysARchitect.Interaction;
using SysARchitect.Lessons;
using SysARchitect.Visualization;

namespace SysARchitect.Tests
{
    public class VisualAndMobileLayoutTests
    {
        internal static MobilePanelLayout Layout(out RectTransform root, out GameObject toolbox, System.Func<bool> tracked = null)
        {
            root = GameObject.Find("Workspace UI").GetComponent<RectTransform>();
            var controls = new GameObject("AR Controls", typeof(RectTransform)).GetComponent<RectTransform>(); controls.SetParent(root, false);
            Button Control(string name)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(controls, false);
                go.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 48);
                go.GetComponent<Image>().color = new Color(.08f, .22f, .29f);
                var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                label.transform.SetParent(go.transform, false); label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = new Vector2(8, 4); label.rectTransform.offsetMax = new Vector2(-8, -4);
                label.text = name; label.raycastTarget = false; label.alignment = TextAlignmentOptions.Center; return go.GetComponent<Button>();
            }
            toolbox = root.Find("Toolbox").gameObject;
            var status = new GameObject("Tracking status", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            status.transform.SetParent(controls, false); status.raycastTarget = false;
            status.text = "Editor preview (synthetic tracking): Toolbox: choose a component, then tap the workspace. Select: tap nodes/lines; drag nodes. Connect: tap two nodes.";
            return new MobilePanelLayout(root, toolbox, Control("Toolbox"), Control("Reposition workspace"),
                Object.FindAnyObjectByType<NodeSelectionController>(), Object.FindAnyObjectByType<InteractionManager>(), tracked);
        }

        [UnityTest]
        public IEnumerator ToolbarControlsHaveTouchSizedHitTargets()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity"); yield return null;
            var layout = Layout(out var root, out _); yield return null; yield return null;
            layout.Refresh(true); Canvas.ForceUpdateCanvases();
            foreach (var button in root.Find("AR Controls").GetComponentsInChildren<Button>())
                Assert.That(button.GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(88), button.name);
        }

        [UnityTest]
        public IEnumerator LeftDrawersNeverObscureEachOther()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity"); yield return null;
            var layout = Layout(out var root, out var toolbox); yield return null;
            root.Find("AR Controls/Touch toolbar/Simulation").GetComponent<Button>().onClick.Invoke();
            toolbox.SetActive(true); layout.Refresh(true);
            Assert.That(root.Find("Simulation").gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator CompleteArchitectureValidationRemainsScrollable()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity"); yield return null;
            var layout = Layout(out var root, out var toolbox);
            var graph = Object.FindAnyObjectByType<ArchitectureGraph>();
            for (int i = 0; i < 20; i++) graph.AddNode(NodeType.Backend, Vector3.zero);
            toolbox.GetComponentsInChildren<Button>(true).Single(b => b.name == "Validate architecture").onClick.Invoke();
            yield return null; layout.Refresh(true); yield return null; Canvas.ForceUpdateCanvases();
            var scroll = root.Find("Feedback").GetComponentInChildren<ScrollRect>();
            Assert.That(scroll, Is.Not.Null, "Long validation explanations require a scrollable viewport.");
            Assert.That(scroll.content.rect.height, Is.GreaterThan(scroll.viewport.rect.height));
        }
        [UnityTest]
        public IEnumerator TouchDrawersScrollAllControlsAndRetainLiveConfigurationCommands()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity"); yield return null;
            var root = GameObject.Find("Workspace UI").GetComponent<RectTransform>();
            var controls = new GameObject("AR Controls", typeof(RectTransform)).GetComponent<RectTransform>(); controls.SetParent(root, false);
            Button Control(string name)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(controls, false);
                return go.GetComponent<Button>();
            }
            var selection = Object.FindAnyObjectByType<NodeSelectionController>();
            var interactions = Object.FindAnyObjectByType<InteractionManager>();
            var toolbox = root.Find("Toolbox").gameObject;
            var layout = new MobilePanelLayout(root, toolbox, Control("Toolbox"), Control("Reposition"), selection, interactions);
            yield return null; yield return null; layout.Refresh(true); Canvas.ForceUpdateCanvases();
            Assert.That(controls.rect.height, Is.EqualTo(184).Within(.1));
            Assert.That(toolbox.activeSelf, Is.False);
            toolbox.SetActive(true); layout.Refresh(true); Canvas.ForceUpdateCanvases();
            yield return null; layout.Refresh(true); Canvas.ForceUpdateCanvases();
            var scroll = toolbox.GetComponentInChildren<ScrollRect>();
            Assert.That(scroll.content.rect.height, Is.GreaterThan(scroll.viewport.rect.height));
            foreach (var button in toolbox.GetComponentsInChildren<Button>())
                Assert.That(button.GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(87.9));
            interactions.OnConnectButtonPressed(); layout.Refresh(true); Assert.That(toolbox.activeSelf, Is.False);
            interactions.ReturnToSelectMode();
            var graph = Object.FindAnyObjectByType<ArchitectureGraph>(); var router = graph.AddNode(NodeType.LoadBalancer, Vector3.zero);
            selection.Select(router.GetComponent<NodeVisual>()); yield return null; layout.Refresh(true); Canvas.ForceUpdateCanvases();
            var drawer = root.Find("Mobile details drawer"); Assert.That(drawer.gameObject.activeSelf, Is.True);
            Button databaseRole = null;
            foreach (var button in drawer.GetComponentsInChildren<Button>())
                if (button.name == "Database Router") databaseRole = button;
            Assert.That(databaseRole, Is.Not.Null); databaseRole.onClick.Invoke();
            Assert.That(router.RoutingRole, Is.EqualTo(LoadBalancerRole.Database));
            var guide = root.Find("Learning Guide"); guide.gameObject.SetActive(true); layout.Refresh(true);
            Assert.That(drawer.gameObject.activeSelf, Is.False);
            var lessonScroll = guide.GetComponentInChildren<ScrollRect>();
            Assert.That(lessonScroll, Is.Not.Null);
            yield return null; layout.Refresh(true); Canvas.ForceUpdateCanvases();
            Assert.That(lessonScroll.content.rect.height, Is.GreaterThan(lessonScroll.viewport.rect.height));
            DesktopWorkspaceTests.CaptureDemo("sysarchitect-mobile-layout.png"); yield return null;
            var quiz = Object.FindAnyObjectByType<SysARchitect.UI.QuizUI>(); quiz.Open(SysARchitect.Quiz.QuizStage.Pre);
            yield return null; layout.Refresh(true); Canvas.ForceUpdateCanvases();
            quiz.Answer(0); yield return null; layout.Refresh(true); Canvas.ForceUpdateCanvases();
            var quizScroll = root.Find("Quiz Overlay").GetComponentInChildren<ScrollRect>();
            Assert.That(quizScroll.content.rect.height, Is.GreaterThan(0));
            foreach (var button in quizScroll.GetComponentsInChildren<Button>())
                Assert.That(button.GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(87.9));
            DesktopWorkspaceTests.CaptureDemo("sysarchitect-mobile-quiz.png"); quiz.Close();
        }
    }
}
