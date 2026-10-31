using NUnit.Framework;
using UnityEngine;
using SysARchitect.AR;

namespace SysARchitect.Tests
{
    public class MobileSafeAreaTests
    {
        [TestCase(1280, 720, 0, 0, 1280, 720)]
        [TestCase(1920, 1080, 0, 0, 1920, 1080)]
        [TestCase(2400, 1080, 90, 30, 2220, 1020)]
        [TestCase(2796, 1290, 144, 63, 2508, 1164)]
        public void SyntheticPhoneInsetsRoundTripToPixelBounds(int width, int height, int x, int y, int safeWidth, int safeHeight)
        {
            var target = new GameObject("Safe area", typeof(RectTransform)).GetComponent<RectTransform>();
            try
            {
                target.offsetMin = Vector2.one * 30;
                MobileWorkspaceUI.ApplySafeArea(target, new Rect(x, y, safeWidth, safeHeight), new Vector2(width, height));
                Assert.That(target.anchorMin.x * width, Is.EqualTo(x).Within(.001));
                Assert.That(target.anchorMin.y * height, Is.EqualTo(y).Within(.001));
                Assert.That(target.anchorMax.x * width, Is.EqualTo(x + safeWidth).Within(.001));
                Assert.That(target.anchorMax.y * height, Is.EqualTo(y + safeHeight).Within(.001));
                Assert.That(target.offsetMin, Is.EqualTo(Vector2.zero)); Assert.That(target.offsetMax, Is.EqualTo(Vector2.zero));
            }
            finally { Object.DestroyImmediate(target.gameObject); }
        }
        [Test]
        public void InvalidStartupSizesDoNotWriteNaNAnchorsAndOversizedInsetsClamp()
        {
            var target = new GameObject("Safe area", typeof(RectTransform)).GetComponent<RectTransform>();
            try
            {
                var before = target.anchorMin;
                MobileWorkspaceUI.ApplySafeArea(target, new Rect(0, 0, 100, 100), Vector2.zero);
                Assert.That(target.anchorMin, Is.EqualTo(before));
                MobileWorkspaceUI.ApplySafeArea(target, new Rect(-10, -20, 2000, 2000), new Vector2(1280, 720));
                Assert.That(target.anchorMin, Is.EqualTo(Vector2.zero)); Assert.That(target.anchorMax, Is.EqualTo(Vector2.one));
            }
            finally { Object.DestroyImmediate(target.gameObject); }
        }
    }
}
