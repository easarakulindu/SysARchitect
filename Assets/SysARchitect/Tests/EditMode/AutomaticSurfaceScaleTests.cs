using NUnit.Framework;
using UnityEngine;
using SysARchitect.AR;

namespace SysARchitect.Tests
{
    public class AutomaticSurfaceScaleTests
    {
        [TestCase(.2f, .2f, .075f)]
        [TestCase(.4f, .6f, .15f)]
        [TestCase(1f, 2f, .35f)]
        [TestCase(.1f, .2f, 0f)]
        [TestCase(float.NaN, 1f, 0f)]
        public void AutomaticWorkspaceFitsCompactSurfacesAndCapsLargeOnes(float width, float depth, float expected)
        {
            float scale = ARWorkspace.WorkspaceScaleForSurface(new Vector2(width, depth));
            Assert.That(scale, Is.EqualTo(expected).Within(.0001f));
            if (expected > 0) Assert.That(scale * 2.4f, Is.LessThanOrEqualTo(Mathf.Min(width, depth) * .9f + .001f));
        }
    }
}
