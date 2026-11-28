using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using SysARchitect.Core;
using SysARchitect.Graph;
using SysARchitect.Visualization;
using SysARchitect.Desktop;

namespace SysARchitect.Tests
{
    public class GlassPresentationTests
    {
        [UnityTest]
        public IEnumerator EveryInfrastructureTypeUsesGlassWhileStatusAndSelectionRemainDistinct()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity"); yield return null;
            var graph = Object.FindAnyObjectByType<ArchitectureGraph>(); int i = 0;
            foreach (NodeType type in System.Enum.GetValues(typeof(NodeType)))
            {
                var node = graph.AddNode(type, new Vector3((i % 5 - 2) * .5f, 0, (i / 5 - .5f) * .65f)); i++;
                var renderer = node.GetComponent<Renderer>(); var material = renderer.sharedMaterial;
                Assert.That(material.shader.name, Is.EqualTo("SysARchitect/HolographicHardware"));
                Assert.That(material.renderQueue, Is.EqualTo(3000)); Assert.That(material.GetFloat("_ZWrite"), Is.Zero);
                Assert.That(material.GetFloat("_Opacity"), Is.InRange(.15f, .5f));
                var mesh = node.GetComponent<MeshFilter>().sharedMesh;
                Assert.That(mesh.colors.Length, Is.EqualTo(mesh.vertexCount));
                var visual = node.GetComponent<NodeVisual>(); visual.SetSelected(true); yield return null;
                var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block); Color selected = block.GetColor("_BaseColor");
                visual.SetSelected(false); visual.UpdateStatus(NodeStatus.Crashed);
                node.Status = NodeStatus.Crashed; yield return null;
                renderer.GetPropertyBlock(block); Assert.That(block.GetColor("_BaseColor"), Is.Not.EqualTo(selected));
                node.Status = NodeStatus.Healthy;
            }
            yield return null;
            var camera = Camera.main;
            var controller = camera.GetComponent<DesktopCameraController>(); if (controller != null) controller.enabled = false;
            camera.transform.SetPositionAndRotation(new Vector3(1.25f, 1.8f, -2.4f), Quaternion.LookRotation(new Vector3(-1.25f, -1.7f, 2.4f)));
            var canvas = GameObject.Find("Workspace UI").GetComponent<Canvas>(); canvas.enabled = false;
            var target = new RenderTexture(1280, 720, 24); var previous = RenderTexture.active;
            Texture2D pixels = null; float aspect = camera.aspect;
#if UNITY_EDITOR
            bool asyncShaders = UnityEditor.ShaderUtil.allowAsyncCompilation;
            UnityEditor.ShaderUtil.allowAsyncCompilation = false;
#endif
            try
            {
                camera.aspect = 1280f / 720;
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target; pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                File.WriteAllBytes(Path.Combine(Application.temporaryCachePath, "sysarchitect-glass-models.png"), pixels.EncodeToPNG());
                Debug.Log("Glass capture: " + Path.Combine(Application.temporaryCachePath, "sysarchitect-glass-models.png"));
            }
            finally
            {
                camera.aspect = aspect; canvas.enabled = true; RenderTexture.active = previous;
                if (pixels != null) Object.Destroy(pixels); target.Release(); Object.Destroy(target);
#if UNITY_EDITOR
                UnityEditor.ShaderUtil.allowAsyncCompilation = asyncShaders;
#endif
            }
        }
    }
}
