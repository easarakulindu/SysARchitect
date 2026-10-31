using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using SysARchitect.AR;
using SysARchitect.Desktop;
using SysARchitect.Graph;
using SysARchitect.Interaction;
using SysARchitect.Lessons;
using SysARchitect.Simulation;
using SysARchitect.UI;
using static SysARchitect.Editor.DesktopSceneSetup;

namespace SysARchitect.Editor
{
    public static class QuizSceneSetup
    {
        public static void Configure()
        {
            foreach (var path in new[] { DesktopSceneSetup.ScenePath, ARWorkspaceSetup.ScenePath })
            {
                var scene = EditorSceneManager.OpenScene(path);
                var graph = Object.FindAnyObjectByType<ArchitectureGraph>(); var canvas = GameObject.Find("Workspace UI");
                if (canvas.GetComponent<QuizUI>() != null) continue;
                var root = canvas.transform.Find("Safe Area") ?? canvas.transform;
                var guide = root.Find("Learning Guide").GetComponent<RectTransform>(); guide.sizeDelta = new Vector2(760, 576);
                var ui = canvas.AddComponent<QuizUI>();
                Set(ui, "lessons", graph.GetComponent<LessonController>()); Set(ui, "simulation", graph.GetComponent<SimulationController>());
                Set(ui, "interactions", Object.FindAnyObjectByType<InteractionManager>());
                Set(ui, "selection", Object.FindAnyObjectByType<NodeSelectionController>()); Set(ui, "movement", Object.FindAnyObjectByType<NodeMovementController>());
                Set(ui, "touchInput", Object.FindAnyObjectByType<WorkspaceTouchInput>()); Set(ui, "desktopCamera", Camera.main.GetComponent<DesktopCameraController>());
                Set(ui, "guide", guide.gameObject);
                Set(ui, "preQuiz", Button(guide, "Pre-quiz", 12, 512, 180, 48));
                Set(ui, "postQuiz", Button(guide, "Post-quiz", 200, 512, 180, 48));
                Set(ui, "progress", Button(guide, "Scores / Progress summary", 388, 512, 360, 48));
                var overlay = Panel(root, "Quiz Overlay", 0, 0, 1280, 800);
                overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one; overlay.offsetMin = overlay.offsetMax = Vector2.zero;
                overlay.GetComponent<Image>().color = new Color(.02f, .03f, .06f, .99f);
                Set(ui, "overlay", overlay.gameObject);
                var card = Panel(overlay, "Quiz Card", 0, 0, 980, 720);
                card.anchorMin = card.anchorMax = card.pivot = new Vector2(.5f, .5f); card.anchoredPosition = Vector2.zero;
                Set(ui, "title", Label(card, "Quiz", 32, 20, 916, 44, 22));
                Set(ui, "question", Label(card, "Question", 32, 80, 916, 90, 20));
                var serialized = new SerializedObject(ui); var answers = serialized.FindProperty("answers"); answers.arraySize = 4;
                for (int i = 0; i < 4; i++) answers.GetArrayElementAtIndex(i).objectReferenceValue = Button(card, "Answer " + i, 32, 180 + i * 80, 916, 64);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Set(ui, "feedback", Label(card, "Answer feedback", 32, 510, 916, 128, 18));
                Set(ui, "close", Button(card, "Close / Return to guide", 32, 650, 220, 50));
                Set(ui, "counter", Label(card, "Answered 0/10", 268, 654, 440, 42, 16));
                Set(ui, "next", Button(card, "Next question", 728, 650, 220, 50));
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
