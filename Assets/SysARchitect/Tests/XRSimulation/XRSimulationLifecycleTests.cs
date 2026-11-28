using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;
using UnityEngine.XR.Simulation;
using SysARchitect.AR;
using SysARchitect.Graph;

namespace SysARchitect.Tests
{
    public class XRSimulationLifecycleTests
    {
        [UnitySetUp]
        public IEnumerator FreshProvider()
        {
            // LoadScene(Single) unloads XR Simulation's additive environment. The loader
            // outlives scenes, so reset that provider between independently loaded test scenes.
            yield return ReloadProvider();
        }
        internal static IEnumerator ReloadProvider()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity");
            var manager = XRGeneralSettings.Instance.Manager;
            manager.DeinitializeLoader(); yield return null;
            manager.InitializeLoaderSync();
            Assert.That(manager.activeLoader, Is.InstanceOf<SimulationLoader>());
        }
        private static ARPlane Up(ARPlaneManager manager)
        { foreach (var plane in manager.trackables) if (plane.isActiveAndEnabled && plane.alignment == PlaneAlignment.HorizontalUp) return plane; return null; }
        private static IEnumerator Tracking()
        {
            float deadline = Time.realtimeSinceStartup + 10;
            var workspace = Object.FindAnyObjectByType<ARWorkspace>();
            // The legacy lifecycle cases deliberately request/cancel placement in the same frame.
            // Automatic placement has its own real-provider regression below.
            workspace.AutomaticPlacement = false;
            while ((workspace.Mode == ARWorkspace.WorkspaceMode.Checking || ARSession.state != ARSessionState.SessionTracking) && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(XRGeneralSettings.Instance.Manager.activeLoader, Is.InstanceOf<SimulationLoader>());
            Assert.That(workspace.Mode, Is.EqualTo(ARWorkspace.WorkspaceMode.AugmentedReality));
            Assert.That(ARSession.state, Is.EqualTo(ARSessionState.SessionTracking));
        }
        internal static IEnumerator Scan()
        {
            yield return Tracking();
            var planes = Object.FindAnyObjectByType<ARPlaneManager>();
            var pose = Object.FindAnyObjectByType<SimulationCameraPoseProvider>();
            Assert.That(pose, Is.Not.Null);
            Vector3 origin = pose.transform.position;
            float deadline = Time.realtimeSinceStartup + 20;
            while (Up(planes) == null && Time.realtimeSinceStartup < deadline)
            {
                pose.transform.SetPositionAndRotation(origin + new Vector3(Mathf.Sin(Time.realtimeSinceStartup) * .25f, 0, 0), Quaternion.Euler(65, 0, 0));
                yield return null;
            }
            // Diagnostics only: no mutation of package internals or injected tracking results.
            var environmentType = typeof(SimulationCameraPoseProvider).Assembly.GetType("UnityEngine.XR.Simulation.SimulationEnvironment");
            Debug.Log("XR lifecycle: environment instances=" + Resources.FindObjectsOfTypeAll(environmentType).Cast<Component>().Count(e => e.gameObject.scene.IsValid()) + "; planes=" + planes.trackables.count);
            Assert.That(Up(planes), Is.Not.Null, "Real simulated environment must rediscover horizontal planes");
        }
        internal static IEnumerator Place(System.Action onPlaced = null, System.Action onRequested = null, bool expectWorkspace = true)
        {
            var workspace = Object.FindAnyObjectByType<ARWorkspace>();
            var planes = Object.FindAnyObjectByType<ARPlaneManager>(); var rays = Object.FindAnyObjectByType<ARRaycastManager>();
            var pose = Object.FindAnyObjectByType<SimulationCameraPoseProvider>();
            var hits = new System.Collections.Generic.List<ARRaycastHit>();
            pose.transform.rotation = Quaternion.LookRotation(Up(planes).center - pose.transform.position);
            float deadline = Time.realtimeSinceStartup + 10; bool requested = false;
            while (!workspace.HasWorkspace && Time.realtimeSinceStartup < deadline)
            {
                if (!requested) foreach (var plane in planes.trackables)
                {
                    if (plane.alignment != PlaneAlignment.HorizontalUp) continue;
                    Vector3 screen = Camera.main.WorldToScreenPoint(plane.center);
                    if (screen.z > 0 && rays.Raycast((Vector2)screen, hits, TrackableType.PlaneWithinPolygon))
                    { workspace.TryPlaceWorkspace(screen); requested = true; onRequested?.Invoke(); break; }
                }
                if (requested && !expectWorkspace) yield break;
                if (!workspace.HasWorkspace) yield return null;
            }
            Assert.That(workspace.HasWorkspace && workspace.CanInteract, Is.True);
            var graph = Object.FindAnyObjectByType<ArchitectureGraph>();
            Assert.That(graph.transform.parent.GetComponent<ARAnchor>(), Is.Not.Null);
            onPlaced?.Invoke();
        }
        [UnityTest]
        public IEnumerator AutomaticPlacementAndRepositionUseWhitePlanesWithoutATap()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/ARWorkspace.unity");
            yield return Scan();
            var workspace = Object.FindAnyObjectByType<ARWorkspace>();
            var planes = Object.FindAnyObjectByType<ARPlaneManager>();
            var pose = Object.FindAnyObjectByType<SimulationCameraPoseProvider>();
            var graph = Object.FindAnyObjectByType<ArchitectureGraph>(FindObjectsInactive.Include);
            var surface = graph.transform.Find("Anchored Workspace Surface");
            Assert.That(surface.GetComponent<Renderer>().enabled, Is.False);
            Assert.That(graph.GetComponent<LineRenderer>().enabled, Is.True);
            Assert.That(surface.GetComponent<Collider>().enabled, Is.True, "Editing bounds remain available");
            for (int cycle = 0; cycle < 2; cycle++)
            {
                workspace.AutomaticPlacement = true;
                float deadline = Time.realtimeSinceStartup + 20;
                while (!workspace.HasWorkspace && Time.realtimeSinceStartup < deadline)
                {
                    ARPlane plane = null;
                    foreach (var tracked in planes.trackables)
                        if (tracked.alignment == PlaneAlignment.HorizontalUp && tracked.size.x >= .15f && tracked.size.y >= .15f &&
                            tracked.subsumedBy == null && tracked.trackingState == TrackingState.Tracking &&
                            (plane == null || tracked.size.sqrMagnitude > plane.size.sqrMagnitude)) plane = tracked;
                    if (plane != null) pose.transform.rotation = Quaternion.LookRotation(plane.center - pose.transform.position);
                    else pose.transform.rotation = Quaternion.Euler(65, 0, 0);
                    yield return null;
                }
                string surfaces = "";
                foreach (var tracked in planes.trackables) surfaces += $" [{tracked.alignment}, {tracked.size}, {tracked.trackingState}]";
                Debug.Log("Automatic placement cycle " + cycle + ": " + workspace.HasWorkspace + surfaces);
                Assert.That(workspace.HasWorkspace && workspace.CanInteract, Is.True, "Automatic anchor requires no pointer event. Detected:" + surfaces);
                Assert.That(graph.transform.parent.GetComponent<ARAnchor>().pending, Is.False);
                float largestShortSide = 0;
                foreach (var tracked in planes.trackables)
                    if (tracked.alignment == PlaneAlignment.HorizontalUp && tracked.trackingState == TrackingState.Tracking)
                        largestShortSide = Mathf.Max(largestShortSide, Mathf.Min(tracked.size.x, tracked.size.y));
                Assert.That(graph.transform.lossyScale.x * 2.4f, Is.LessThanOrEqualTo(Mathf.Min(.84f, largestShortSide * .9f) + .01f),
                    "The visible workspace must fit compact surfaces without changing its logical editing bounds");
                Assert.That(planes.enabled, Is.True);
                var overlay = Up(planes).GetComponent<Renderer>();
                Assert.That(overlay.enabled, Is.True);
                var color = overlay.sharedMaterial.GetColor("_BaseColor");
                Assert.That(color.r, Is.EqualTo(1)); Assert.That(color.a, Is.InRange(.1f, .4f));
                workspace.AutomaticPlacement = false;
                workspace.Reposition();
                yield return null;
            }
            workspace.ToggleWorkspaceMode(); yield return null;
            Assert.That(surface.GetComponent<Renderer>().enabled, Is.True, "Virtual Workspace retains its own visible tabletop");
        }
        [UnityTest]
        public IEnumerator ImmediateRepositionRemovesConfirmedAnchorsAndCanRepeat()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/ARWorkspace.unity");
            var workspace = Object.FindAnyObjectByType<ARWorkspace>(); var anchors = Object.FindAnyObjectByType<ARAnchorManager>();
            for (int cycle = 0; cycle < 3; cycle++)
            {
                yield return Scan();
                yield return Place(() =>
                {
                    var created = Object.FindAnyObjectByType<ArchitectureGraph>().transform.parent.GetComponent<ARAnchor>();
                    Debug.Log("XR lifecycle: immediately repositioning anchor " + created.trackableId + "; pending=" + created.pending);
                    workspace.Reposition();
                });
                var graph = Object.FindAnyObjectByType<ArchitectureGraph>(FindObjectsInactive.Include);
                Assert.That(workspace.HasWorkspace, Is.False);
                float deadline = Time.realtimeSinceStartup + 10;
                while (anchors.trackables.count > 0 && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(anchors.trackables.count, Is.Zero);
                Assert.That(graph.transform.parent, Is.Null);
            }
        }
        [UnityTest]
        public IEnumerator SwitchingToVirtualDuringCreationDrainsTheUnpublishedAnchor()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/ARWorkspace.unity");
            yield return Scan();
            var workspace = Object.FindAnyObjectByType<ARWorkspace>();
            yield return Place(onRequested: () => workspace.ToggleWorkspaceMode(), expectWorkspace: false);
            var session = Object.FindAnyObjectByType<ARSession>();
            var anchors = Object.FindAnyObjectByType<ARAnchorManager>();
            float deadline = Time.realtimeSinceStartup + 10;
            while (session.enabled && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(workspace.Mode, Is.EqualTo(ARWorkspace.WorkspaceMode.Virtual));
            Assert.That(workspace.CanInteract, Is.True); Assert.That(session.enabled, Is.False);
            Assert.That(anchors.trackables.count, Is.Zero);
            Assert.That(Object.FindAnyObjectByType<ArchitectureGraph>().transform.parent, Is.Null);
        }
        [UnityTest]
        public IEnumerator RepositionDuringCreationRetiresStaleResultAndPreservesArchitecture()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/ARWorkspace.unity");
            yield return Scan(); yield return Place();
            var workspace = Object.FindAnyObjectByType<ARWorkspace>();
            var graph = Object.FindAnyObjectByType<ArchitectureGraph>();
            var a = graph.AddNode(SysARchitect.Core.NodeType.Frontend, graph.transform.position);
            var b = graph.AddNode(SysARchitect.Core.NodeType.Backend, graph.transform.position + Vector3.right * .2f);
            var edge = graph.AddConnection(a, b);
            workspace.Reposition(); yield return Scan();
            yield return Place(onRequested: workspace.Reposition, expectWorkspace: false);
            var anchors = Object.FindAnyObjectByType<ARAnchorManager>(); float deadline = Time.realtimeSinceStartup + 10;
            while (anchors.trackables.count > 0 && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(anchors.trackables.count, Is.Zero); Assert.That(workspace.HasWorkspace, Is.False);
            yield return Scan(); yield return Place();
            Assert.That(graph.Nodes, Is.EquivalentTo(new[] { a, b })); Assert.That(graph.Connections.Single(), Is.SameAs(edge));
            Assert.That(anchors.trackables.count, Is.EqualTo(1)); Assert.That(graph.transform.parent.GetComponent<ARAnchor>().pending, Is.False);
        }
        [UnityTest]
        public IEnumerator VirtualARCyclesRediscoverPlanesAndCreateNewAnchors()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/ARWorkspace.unity");
            var workspace = Object.FindAnyObjectByType<ARWorkspace>();
            for (int cycle = 0; cycle < 2; cycle++)
            {
                yield return Scan(); yield return Place(); workspace.ToggleWorkspaceMode();
                Assert.That(workspace.Mode, Is.EqualTo(ARWorkspace.WorkspaceMode.Virtual));
                yield return null;
                workspace.ToggleWorkspaceMode(); yield return Tracking();
            }
            yield return Scan(); yield return Place();
        }
        [UnityTest]
        public IEnumerator VirtualTestSequenceThenARWorkspaceDiscoversRealPlanes()
        {
            var previous = new VirtualWorkspaceTests();
            yield return previous.ActualVirtualSceneRendersSafePhoneLayoutsWithExplicitNonARStatus();
            yield return previous.UnavailableOrMissingARServicesLeaveAllARComponentsDisabled();
            yield return previous.VirtualModeUsesActualMobileSceneAndSharedTouchGraphCommands();
            yield return previous.VirtualWorkspaceRunsDatabaseLessonConfigurationAndQuiz();
            yield return ReloadProvider();
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/ARWorkspace.unity");
            yield return Scan(); yield return Place();
        }
    }
}
