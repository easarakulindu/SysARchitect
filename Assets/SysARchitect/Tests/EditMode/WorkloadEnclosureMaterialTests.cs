using NUnit.Framework;
using UnityEngine;

namespace SysARchitect.Tests
{
    public class WorkloadEnclosureMaterialTests
    {
        [Test]
        public void PlayerEnclosureIncludesARealTransparentMaterialAsset()
        {
            var material = Resources.Load<Material>("WorkloadGlass");
            Assert.That(material, Is.Not.Null);
            Assert.That(material.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT"), Is.True);
            Assert.That(material.renderQueue, Is.EqualTo(3000));
            Assert.That(material.GetFloat("_ZWrite"), Is.EqualTo(0));
            Assert.That(material.GetFloat("_SrcBlend"), Is.EqualTo((float)UnityEngine.Rendering.BlendMode.SrcAlpha));
            Assert.That(material.GetFloat("_DstBlend"), Is.EqualTo((float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            Assert.That(material.GetColor("_BaseColor").a, Is.InRange(.01f, .1f));
        }
    }
}
