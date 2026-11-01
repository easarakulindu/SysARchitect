using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using SysARchitect.AR;

namespace SysARchitect.Tests
{
    public class MobileSceneWiringTests
    {
        [Test]
        public void ExistingARSceneRetainsRequiredReferencesAndTouchUI()
        {
            var previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene("Assets/SysARchitect/Scenes/ARWorkspace.unity");
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                        Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject), Is.Zero, transform.name);
                var mobile = Object.FindAnyObjectByType<MobileWorkspaceUI>();
                var serialized = new SerializedObject(mobile);
                foreach (string field in new[] { "safeArea", "toolbox", "toggleToolbox", "reposition", "workspace", "selection", "interactions" })
                    Assert.That(serialized.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
                var workspace = new SerializedObject(Object.FindAnyObjectByType<ARWorkspace>());
                foreach (string field in new[] { "graph", "simulation", "raycasts", "planes", "anchors", "status", "session", "workspaceCamera" })
                    Assert.That(workspace.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
                Assert.That(((Behaviour)workspace.FindProperty("session").objectReferenceValue).enabled, Is.False, "Check availability before starting a session");
                var components = workspace.FindProperty("arOnlyComponents");
                Assert.That(components.arraySize, Is.EqualTo(7));
                for (int i = 0; i < components.arraySize; i++)
                {
                    var component = components.GetArrayElementAtIndex(i).objectReferenceValue as Behaviour;
                    Assert.That(component, Is.Not.Null); Assert.That(component.enabled, Is.False, component.GetType().Name);
                }
                var touch = new SerializedObject(Object.FindAnyObjectByType<WorkspaceTouchInput>());
                foreach (string field in new[] { "workspace", "graph", "mainCamera", "interactions", "placement", "selection", "movement", "connections" })
                    Assert.That(touch.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
                Assert.That(GameObject.Find("EventSystem").GetComponents<MonoBehaviour>().Any(c => c != null && c.GetType().Name == "InputSystemUIInputModule"), Is.True);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SysARchitect/Prefabs/UI/MobileToolboxButton.prefab");
                Assert.That(prefab.GetComponent<LayoutElement>().preferredHeight, Is.EqualTo(MobilePanelLayout.TouchHeight));
                var lessons = new SerializedObject(Object.FindAnyObjectByType<SysARchitect.UI.LessonUI>());
                foreach (string field in new[] { "previousLesson", "nextLesson", "lessonListButton", "previousStep", "nextStep", "lessonSelectionPanel" })
                    Assert.That(lessons.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
                var choices = lessons.FindProperty("lessonChoices"); Assert.That(choices.arraySize, Is.EqualTo(5));
                for (int i = 0; i < choices.arraySize; i++) Assert.That(choices.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null);
            }
            finally
            {
                if (previous.Length > 0 && previous.All(s => !string.IsNullOrEmpty(s.path))) EditorSceneManager.RestoreSceneManagerSetup(previous);
            }
        }
    }
}
