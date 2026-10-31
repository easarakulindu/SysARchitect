using NUnit.Framework;
using SysARchitect.AR;
using InputPhase = UnityEngine.InputSystem.TouchPhase;
using PointerPhase = UnityEngine.TouchPhase;

namespace SysARchitect.Tests
{
    public class MobileInputTests
    {
        [TestCase(InputPhase.None, PointerPhase.Canceled)]
        [TestCase(InputPhase.Began, PointerPhase.Began)]
        [TestCase(InputPhase.Moved, PointerPhase.Moved)]
        [TestCase(InputPhase.Stationary, PointerPhase.Stationary)]
        [TestCase(InputPhase.Ended, PointerPhase.Ended)]
        [TestCase(InputPhase.Canceled, PointerPhase.Canceled)]
        public void DeviceTouchPhasesPreserveSharedGestureLifecycle(InputPhase device, PointerPhase shared)
        {
            Assert.That(WorkspaceTouchInput.ConvertPhase(device), Is.EqualTo(shared));
        }
    }
}
