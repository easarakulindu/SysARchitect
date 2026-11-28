using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SysARchitect.Graph;
using SysARchitect.Interaction;
using SysARchitect.AR;
using UnityEngine.UI;
using TMPro;
using static SysARchitect.Editor.DesktopSceneSetup;

namespace SysARchitect.Editor
{
    public static class VisualDesignSetup
    {
        [MenuItem("SysARchitect/Visuals/Configure Glass and Lesson Audio")]
        public static void ConfigureTeachingPresentation()
        {
            GlassVisualSetup.Configure();
            LessonNarrationSetup.Configure();
        }
        public static void Configure()
        {
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new System.InvalidOperationException("Save open scenes before configuring mobile UI.");
            foreach (var scenePath in new[] { DesktopSceneSetup.ScenePath, ARWorkspaceSetup.ScenePath })
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                var uiRoot = GameObject.Find("Workspace UI").transform;
                uiRoot = uiRoot.Find("Safe Area") ?? uiRoot;
                if (scenePath == DesktopSceneSetup.ScenePath)
                    uiRoot.Find("Learning Guide").GetComponent<Image>().color = new Color(.02f, .035f, .07f, 1);
                if (scenePath == ARWorkspaceSetup.ScenePath)
                {
                    GameObject.Find("Workspace UI").GetComponent<CanvasScaler>().referenceResolution = new Vector2(1280, 720);
                    var mobile = Object.FindAnyObjectByType<MobileWorkspaceUI>();
                    Set(mobile, "selection", Object.FindAnyObjectByType<NodeSelectionController>());
                    Set(mobile, "interactions", Object.FindAnyObjectByType<InteractionManager>());
                    const string buttonPath = "Assets/SysARchitect/Prefabs/UI/MobileToolboxButton.prefab";
                    var button = PrefabUtility.LoadPrefabContents(buttonPath);
                    button.GetComponent<LayoutElement>().minHeight = MobilePanelLayout.TouchHeight;
                    button.GetComponent<LayoutElement>().preferredHeight = MobilePanelLayout.TouchHeight;
                    button.GetComponentInChildren<TextMeshProUGUI>().enableAutoSizing = false;
                    button.GetComponentInChildren<TextMeshProUGUI>().fontSize = 28;
                    PrefabUtility.SaveAsPrefabAsset(button, buttonPath); PrefabUtility.UnloadPrefabContents(button);
                    Set(GameObject.Find("Workspace UI").GetComponent<SysARchitect.UI.InspectorUI>(), "connectionButtonPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(buttonPath));
                }
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
