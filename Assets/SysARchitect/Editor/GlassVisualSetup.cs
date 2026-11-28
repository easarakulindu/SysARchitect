using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SysARchitect.Editor
{
    public static class GlassVisualSetup
    {
        [MenuItem("SysARchitect/Visuals/Apply Polished Glass")]
        public static void Configure()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/SysARchitect/Materials/HolographicHardware.shader");
            foreach (var path in new[] { "Assets/SysARchitect/Materials/HolographicHardware.mat", "Assets/SysARchitect/Resources/WorkloadGlass.mat" })
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                material.shader = shader;
                bool enclosure = path.Contains("WorkloadGlass");
                material.SetFloat("_HardwareDetails", enclosure ? 0 : 1);
                material.SetFloat("_Opacity", enclosure ? 1 : .22f);
                material.SetFloat("_Surface", 1); material.SetFloat("_ZWrite", 0);
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.SetOverrideTag("RenderType", "Transparent");
                material.shaderKeywords = System.Array.Empty<string>(); material.renderQueue = 3000;
                EditorUtility.SetDirty(material);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
