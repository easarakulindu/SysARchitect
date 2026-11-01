using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using SysARchitect.AR;
using SysARchitect.Components;
using SysARchitect.Graph;
using SysARchitect.Interaction;
using SysARchitect.Lessons;
using SysARchitect.Simulation;
using SysARchitect.UI;
using static SysARchitect.Editor.DesktopSceneSetup;

namespace SysARchitect.Editor
{
    public static class LessonSceneSetup
    {
        public static void Configure()
        {
            foreach (var path in new[] { DesktopSceneSetup.ScenePath, ARWorkspaceSetup.ScenePath })
            {
                var scene = EditorSceneManager.OpenScene(path);
                var graph = Object.FindAnyObjectByType<ArchitectureGraph>(); var canvas = GameObject.Find("Workspace UI");
                canvas.GetComponent<Canvas>().sortingOrder = 100; // World labels must remain behind readable UI panels.
                if (canvas.GetComponent<LessonUI>() != null) { EditorSceneManager.SaveScene(scene); continue; }
                var lessons = graph.gameObject.AddComponent<LessonController>();
                Set(lessons, "graph", graph); Set(lessons, "simulation", graph.GetComponent<SimulationController>());
                Set(lessons, "interactions", Object.FindAnyObjectByType<InteractionManager>());
                Set(lessons, "docker", graph.GetComponent<DockerWorkspaceController>()); Set(lessons, "kubernetes", graph.GetComponent<KubernetesWorkspaceController>());
                var ui = canvas.AddComponent<LessonUI>(); Set(ui, "lessons", lessons);
                Set(ui, "workspace", Object.FindAnyObjectByType<ARWorkspace>());
                var root = canvas.transform.Find("Safe Area") ?? canvas.transform;
                var toolbox = root.Find("Toolbox").GetComponent<RectTransform>();
                foreach (var label in toolbox.GetComponentsInChildren<TMPro.TextMeshProUGUI>())
                    if (label.text == "BUILD YOUR ARCHITECTURE") label.gameObject.SetActive(false);
                Set(ui, "toggleButton", Button(toolbox, "Lessons / Progress", 12, 54, 186, 32));
                var panel = Panel(root, "Learning Guide", 220, 160, 760, 520); Set(ui, "panel", panel.gameObject);
                Set(ui, "title", Label(panel, "Guided learning", 12, 10, 570, 30, 18));
                Set(ui, "closeButton", Button(panel, "Close guide", 596, 8, 152, 44));
                Set(ui, "nextLesson", Button(panel, "Next lesson", 12, 56, 180, 44));
                Set(ui, "loadExample", Button(panel, "Load example (clears workspace)", 200, 56, 328, 44));
                Set(ui, "checkChallenge", Button(panel, "Check challenge", 536, 56, 212, 44));
                var viewport = Panel(panel, "Scrollable explanation", 12, 112, 736, 330);
                viewport.gameObject.AddComponent<RectMask2D>(); var scroll = viewport.gameObject.AddComponent<ScrollRect>();
                var body = Label(viewport, "Lesson content", 6, 0, 724, 330, 16);
                body.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                scroll.viewport = viewport; scroll.content = body.rectTransform; scroll.horizontal = false;
                Set(ui, "body", body);
                Set(ui, "previousStep", Button(panel, "Previous step", 12, 456, 180, 48));
                Set(ui, "stepLabel", Label(panel, "Step 1", 204, 464, 340, 34, 15));
                Set(ui, "nextStep", Button(panel, "Next step", 568, 456, 180, 48));
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            ConfigureNavigation();
        }
        [MenuItem("SysARchitect/Lessons/Configure Navigation")]
        public static void ConfigureNavigation()
        {
            var previous = EditorSceneManager.GetSceneManagerSetup();
            foreach (var item in previous)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneByPath(item.path).isDirty)
                    throw new System.InvalidOperationException("Save open scenes before configuring lesson navigation.");
            foreach (var path in new[] { DesktopSceneSetup.ScenePath, ARWorkspaceSetup.ScenePath })
            {
                var scene = EditorSceneManager.OpenScene(path);
                var ui = Object.FindAnyObjectByType<LessonUI>(); var serialized = new SerializedObject(ui);
                if (serialized.FindProperty("previousLesson").objectReferenceValue != null) continue;
                var panel = ((GameObject)serialized.FindProperty("panel").objectReferenceValue).GetComponent<RectTransform>();
                Set(ui, "previousLesson", Button(panel, "Previous lesson", 12, 56, 232, 44));
                Set(ui, "lessonListButton", Button(panel, "Lessons", 252, 56, 232, 44));
                var next = (Button)serialized.FindProperty("nextLesson").objectReferenceValue;
                next.GetComponent<RectTransform>().anchoredPosition = new Vector2(492, -56); next.GetComponent<RectTransform>().sizeDelta = new Vector2(256, 44);
                var load = (Button)serialized.FindProperty("loadExample").objectReferenceValue;
                load.GetComponent<RectTransform>().anchoredPosition = new Vector2(12, -112); load.GetComponent<RectTransform>().sizeDelta = new Vector2(448, 44);
                var check = (Button)serialized.FindProperty("checkChallenge").objectReferenceValue;
                check.GetComponent<RectTransform>().anchoredPosition = new Vector2(472, -112); check.GetComponent<RectTransform>().sizeDelta = new Vector2(276, 44);
                var viewport = panel.Find("Scrollable explanation").GetComponent<RectTransform>();
                viewport.anchoredPosition = new Vector2(12, -168); viewport.sizeDelta = new Vector2(736, 276);
                ((TextMeshProUGUI)serialized.FindProperty("stepLabel").objectReferenceValue).rectTransform.sizeDelta = new Vector2(340, 48);
                var picker = Panel(panel, "Lesson selection", 12, 112, 736, 448);
                picker.GetComponent<Image>().color = new Color(.02f, .035f, .07f, 1);
                picker.gameObject.AddComponent<RectMask2D>();
                var content = new GameObject("Lesson choices", typeof(RectTransform)).GetComponent<RectTransform>();
                content.SetParent(picker, false); content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1); content.sizeDelta = Vector2.zero;
                var flow = content.gameObject.AddComponent<VerticalLayoutGroup>(); flow.padding = new RectOffset(8, 8, 8, 8); flow.spacing = 8;
                flow.childControlHeight = flow.childControlWidth = flow.childForceExpandWidth = true; flow.childForceExpandHeight = false;
                content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                var scroll = picker.gameObject.AddComponent<ScrollRect>(); scroll.viewport = picker; scroll.content = content; scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
                Set(ui, "lessonSelectionPanel", picker.gameObject);
                serialized.Update(); var choices = serialized.FindProperty("lessonChoices"); choices.arraySize = LessonCatalog.All.Count;
                for (int i = 0; i < LessonCatalog.All.Count; i++)
                {
                    var button = Button(content, "Lesson choice " + (i + 1), 0, 0, 720, 80);
                    button.GetComponentInChildren<TextMeshProUGUI>().fontSize = 18;
                    button.gameObject.AddComponent<LayoutElement>().preferredHeight = 80;
                    choices.GetArrayElementAtIndex(i).objectReferenceValue = button;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo(); picker.gameObject.SetActive(false);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            if (previous.Length > 0 && previous.All(item => !string.IsNullOrEmpty(item.path))) EditorSceneManager.RestoreSceneManagerSetup(previous);
        }
    }
}
