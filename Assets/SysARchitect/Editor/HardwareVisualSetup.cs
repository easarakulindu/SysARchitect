using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SysARchitect.Graph;
using SysARchitect.Interaction;

namespace SysARchitect.Editor
{
    public static class HardwareVisualSetup
    {
        [MenuItem("SysARchitect/Apply Reviewed Hardware Material")]
        public static void Configure()
        {
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save open scenes before applying the hardware material.");
            const string path = "Assets/SysARchitect/Materials/HolographicHardware.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) throw new InvalidOperationException("Missing reviewed hardware material: " + path);
            foreach (var scenePath in new[] { DesktopSceneSetup.ScenePath, ARWorkspaceSetup.ScenePath })
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                DesktopSceneSetup.Set(UnityEngine.Object.FindAnyObjectByType<ArchitectureGraph>(), "defaultNodeMaterial", material);
                DesktopSceneSetup.Set(UnityEngine.Object.FindAnyObjectByType<NodePlacementController>(), "defaultNodeMaterial", material);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
