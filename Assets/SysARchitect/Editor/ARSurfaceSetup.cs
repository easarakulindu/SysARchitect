using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using SysARchitect.AR;

namespace SysARchitect.Editor
{
    public static class ARSurfaceSetup
    {
        [MenuItem("SysARchitect/Mobile/Configure Automatic White Surface")]
        public static void Configure()
        {
            var previous = EditorSceneManager.GetSceneManagerSetup();
            foreach (var item in previous)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneByPath(item.path).isDirty)
                    throw new InvalidOperationException("Save open scenes before configuring the AR surface.");
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/SysARchitect/Materials/DetectedPlane.mat");
            material.SetColor("_BaseColor", new Color(1, 1, 1, .24f));
            material.SetColor("_Color", new Color(1, 1, 1, .24f));
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetFloat("_Surface", 1); material.SetFloat("_ZWrite", 0);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue = 3000;
            EditorUtility.SetDirty(material);
            var scene = EditorSceneManager.OpenScene(ARWorkspaceSetup.ScenePath);
            var workspace = UnityEngine.Object.FindAnyObjectByType<ARWorkspace>();
            DesktopSceneSetup.Set(workspace, "workspaceSurface", GameObject.Find("Anchored Workspace Surface").GetComponent<Renderer>());
            var serialized = new SerializedObject(workspace);
            serialized.FindProperty("automaticPlacement").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            if (previous.Length > 0 && previous.All(item => !string.IsNullOrEmpty(item.path))) EditorSceneManager.RestoreSceneManagerSetup(previous);
        }
    }
}
