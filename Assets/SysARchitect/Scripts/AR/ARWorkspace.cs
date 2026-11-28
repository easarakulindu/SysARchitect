using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using SysARchitect.Graph;
using SysARchitect.Simulation;

namespace SysARchitect.AR
{
    /// <summary>Places the shared architecture in AR, or in a clearly labelled non-AR view.</summary>
    public class ARWorkspace : MonoBehaviour
    {
        [SerializeField] private ArchitectureGraph graph;
        [SerializeField] private SimulationController simulation;
        [SerializeField] private ARRaycastManager raycasts;
        [SerializeField] private ARPlaneManager planes;
        [SerializeField] private ARAnchorManager anchors;
        [SerializeField] private TextMeshProUGUI status;
        [SerializeField] private Camera workspaceCamera;
        [SerializeField] private ARSession session;
        [SerializeField] private Behaviour[] arOnlyComponents;
        [SerializeField] private Renderer workspaceSurface;
        [SerializeField] private bool automaticPlacement = true;
        [SerializeField, Min(0)] private float surfaceStabilitySeconds = .8f;
        private readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
        private ARPlane candidatePlane;
        private Vector3 candidateCenter;
        private float candidateSince;
        private ARAnchor anchor;
        private bool placing;
        private int placementRevision;
        private int anchorOperations;
        private string message;
        private Coroutine availabilityCheck;
        private int arCullingMask;
        public enum WorkspaceMode { Checking, AugmentedReality, Virtual }
        public WorkspaceMode Mode { get; private set; }
        public bool AutomaticPlacement { get => automaticPlacement; set { automaticPlacement = value; candidatePlane = null; } }
        public bool HasWorkspace => Mode == WorkspaceMode.Virtual || anchor != null;
        public bool CanInteract => Mode == WorkspaceMode.Virtual || (anchor != null && TrackingAllowsInput(ARSession.state == ARSessionState.SessionTracking, anchor.trackingState == TrackingState.Tracking));
        public static bool TrackingAllowsInput(bool sessionTracking, bool anchorTracking) => sessionTracking && anchorTracking;

        private void Start()
        {
            graph.gameObject.SetActive(false);
            if (workspaceSurface != null) workspaceSurface.enabled = false;
            arCullingMask = workspaceCamera.cullingMask;
            Application.targetFrameRate = 60;
#if ENABLE_LEGACY_INPUT_MANAGER
            Input.simulateMouseWithTouches = false;
#endif
            SetARActive(false);
            availabilityCheck = StartCoroutine(CheckAvailability(false));
        }
        private IEnumerator CheckAvailability(bool installIfNeeded)
        {
            // Finish owned anchor changes before restarting the provider on a mode switch.
            while (anchorOperations > 0) yield return null;
            if (Mode != WorkspaceMode.Checking) { availabilityCheck = null; yield break; }
            yield return ARSession.CheckAvailability();
            if (Mode != WorkspaceMode.Checking) { availabilityCheck = null; yield break; }
            if (installIfNeeded && ARSession.state == ARSessionState.NeedsInstall)
                yield return ARSession.Install();
            availabilityCheck = null;
            if (Mode != WorkspaceMode.Checking) yield break;
            CompleteAvailabilityCheck(ARSession.state);
        }
        private void CompleteAvailabilityCheck(ARSessionState state)
        {
            if (state == ARSessionState.Ready || state == ARSessionState.SessionTracking || state == ARSessionState.SessionInitializing)
            {
                Mode = WorkspaceMode.AugmentedReality;
                workspaceCamera.transform.localPosition = Vector3.zero;
                workspaceCamera.transform.localRotation = Quaternion.identity;
                workspaceCamera.cullingMask = arCullingMask;
                if (workspaceSurface != null) workspaceSurface.enabled = false;
                SetARActive(true);
                foreach (var plane in planes.trackables) plane.gameObject.SetActive(true);
            }
            else EnterVirtualWorkspace(state == ARSessionState.NeedsInstall
                ? "AR services need installation (Virtual / AR to install)."
                : "AR unavailable.");
        }
        private void SetARActive(bool active)
        {
            // The scene starts with these disabled: no camera/session starts before availability.
            bool drainingAnchors = anchorOperations > 0;
            if (!active && !drainingAnchors) session.enabled = false;
            foreach (var component in arOnlyComponents)
                component.enabled = active || (drainingAnchors && component == anchors);
            if (active || drainingAnchors) session.enabled = true;
        }
        public void ToggleWorkspaceMode()
        {
            if (Mode != WorkspaceMode.Virtual) { EnterVirtualWorkspace("Virtual mode selected."); return; }
            if (availabilityCheck != null) return;
            simulation.Pause(); graph.gameObject.SetActive(false);
            Mode = WorkspaceMode.Checking;
            availabilityCheck = StartCoroutine(CheckAvailability(true));
        }
        private void EnterVirtualWorkspace(string reason)
        {
            simulation.Pause(); placing = false; placementRevision++; candidatePlane = null;
            graph.transform.SetParent(null, true);
            if (anchor != null) ReleaseAnchor(anchor);
            anchor = null;
            SetARActive(false);
            foreach (var plane in planes.trackables) plane.gameObject.SetActive(false);
            Mode = WorkspaceMode.Virtual; message = reason;
            graph.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            graph.transform.localScale = Vector3.one * .35f;
            graph.gameObject.SetActive(true);
            if (workspaceSurface != null) workspaceSurface.enabled = true;
            ResetVirtualView();
        }
        private void ResetVirtualView()
        {
            workspaceCamera.ResetProjectionMatrix();
            // The editor's XR Simulation environment is separate from the non-AR tabletop.
            workspaceCamera.cullingMask = arCullingMask & ~LayerMask.GetMask("XR Simulation");
            workspaceCamera.clearFlags = CameraClearFlags.SolidColor;
            workspaceCamera.backgroundColor = new Color(.015f, .025f, .05f, 1);
            workspaceCamera.transform.SetPositionAndRotation(new Vector3(0, .85f, -1.15f), Quaternion.Euler(36.5f, 0, 0));
        }
        private void Update()
        {
            if (!CanInteract) simulation.Pause();
            if (automaticPlacement) TryAutomaticPlacement();
            status.text = Mode == WorkspaceMode.Virtual ? $"Virtual Workspace (non-AR). {message} Toolbox: build; tap/drag nodes; Connect: tap two nodes."
                : Mode == WorkspaceMode.Checking ? "Checking AR availability… Virtual / AR opens the non-AR workspace now."
                : ARSession.state != ARSessionState.SessionTracking ? $"AR: {ARSession.state}. Move slowly and scan a well-lit textured surface. {ARSession.notTrackingReason}"
                : placing ? "Creating surface anchor…"
                : !HasWorkspace ? message ?? "Scan a horizontal surface. Hold the camera toward a white detected plane; the workspace anchors automatically."
                : !CanInteract ? "Workspace tracking lost. Move back toward the surface; editing and simulation are paused."
                : "Workspace tracked. Toolbox: choose a component, then tap the workspace. Select: tap nodes/lines; drag nodes. Connect: tap two nodes.";
        }
        public void TryPlaceWorkspace(Vector2 screenPosition)
        {
            if (Mode != WorkspaceMode.AugmentedReality || placing || HasWorkspace || ARSession.state != ARSessionState.SessionTracking) return;
            if (!raycasts.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
            { message = "No detected plane at that point. Scan a horizontal surface and tap its highlighted area."; return; }
            var plane = planes.GetPlane(hits[0].trackableId);
            if (plane == null || plane.alignment != PlaneAlignment.HorizontalUp) return;
            BeginAnchor(hits[0].pose);
        }
        private void TryAutomaticPlacement()
        {
            if (Mode != WorkspaceMode.AugmentedReality || HasWorkspace || placing || anchorOperations > 0 ||
                ARSession.state != ARSessionState.SessionTracking) { candidatePlane = null; return; }
            // Use the real polygon under the camera's aim, rather than anchoring an off-screen floor.
            var aim = workspaceCamera.ViewportToScreenPoint(new Vector3(.5f, .5f));
            ARPlane target = null;
            if (raycasts.Raycast(aim, hits, TrackableType.PlaneWithinPolygon))
            {
                var plane = planes.GetPlane(hits[0].trackableId);
                // The shared editing bounds are 2.4 model units at 0.35 metres per unit.
                if (plane != null && plane.isActiveAndEnabled && plane.subsumedBy == null &&
                    plane.alignment == PlaneAlignment.HorizontalUp && plane.trackingState == TrackingState.Tracking &&
                    plane.size.x >= .84f && plane.size.y >= .84f) target = plane;
            }
            if (target == null)
            {
                candidatePlane = null;
                message = "Scan a horizontal surface at least 84 cm wide. Aim at its white overlay for automatic placement.";
                return;
            }
            if (candidatePlane != target || Vector3.Distance(candidateCenter, target.center) > .05f)
            {
                candidatePlane = target; candidateCenter = target.center; candidateSince = Time.unscaledTime;
                message = "Surface detected. Hold the camera steady while the workspace anchors automatically.";
                return;
            }
            if (Time.unscaledTime - candidateSince >= surfaceStabilitySeconds)
            {
                candidatePlane = null;
                BeginAnchor(new Pose(target.center, target.transform.rotation));
            }
        }
        private async void BeginAnchor(Pose pose)
        {
            placing = true;
            int revision = placementRevision;
            anchorOperations++;
            try
            {
                var result = await anchors.TryAddAnchorAsync(pose);
                if (this == null) return;
                if (!result.status.IsSuccess()) { message = "The device could not anchor that surface. Scan again and retry."; return; }
                // Async creation can finish before GetChanges announces the anchor. Removing a
                // pending anchor makes XR Simulation emit a removal without a preceding addition.
                var created = result.value;
                while (this != null && created != null && created.pending) await Awaitable.NextFrameAsync();
                if (this == null || created == null) return;
                if (Mode != WorkspaceMode.AugmentedReality || revision != placementRevision)
                { ReleaseAnchor(created); return; }
                anchor = created;
                graph.transform.SetParent(anchor.transform, false);
                graph.transform.localPosition = Vector3.zero;
                graph.transform.localRotation = Quaternion.identity;
                graph.transform.localScale = Vector3.one * .35f;
                graph.gameObject.SetActive(true);
                // Keep detected white surfaces visible; the AR workspace uses their overlay.
            }
            catch (System.Exception exception) { message = "Anchor creation failed: " + exception.Message; Debug.LogWarning(message); }
            finally
            {
                if (this != null)
                {
                    if (revision == placementRevision) placing = false;
                    FinishAnchorOperation();
                }
            }
        }
        private async void ReleaseAnchor(ARAnchor retired)
        {
            anchorOperations++;
            try
            {
                while (this != null && retired != null && retired.pending) await Awaitable.NextFrameAsync();
                if (this == null || retired == null) return;
                var id = retired.trackableId;
                if (!anchors.TryRemoveAnchor(retired))
                { message = "The device did not confirm removal of the previous anchor."; return; }
                // Keep the manager/session alive until the removal has actually been consumed.
                while (this != null && anchors != null && anchors.GetAnchor(id) != null) await Awaitable.NextFrameAsync();
            }
            catch (System.Exception exception)
            { if (this != null) { message = "Anchor removal failed: " + exception.Message; Debug.LogException(exception, this); } }
            finally { if (this != null) FinishAnchorOperation(); }
        }
        private void FinishAnchorOperation()
        {
            anchorOperations--;
            if (Mode == WorkspaceMode.AugmentedReality) return;
            SetARActive(false);
            if (Mode == WorkspaceMode.Virtual) ResetVirtualView();
        }
        public void Reposition()
        {
            simulation.Pause();
            if (Mode == WorkspaceMode.Virtual) { ResetVirtualView(); return; }
            if (Mode != WorkspaceMode.AugmentedReality) return;
            placementRevision++; placing = false; candidatePlane = null;
            graph.transform.SetParent(null, true);
            graph.gameObject.SetActive(false);
            if (anchor != null) ReleaseAnchor(anchor);
            anchor = null;
            planes.enabled = true;
            foreach (var trackedPlane in planes.trackables) trackedPlane.gameObject.SetActive(true);
            message = "Aim at a white horizontal surface to re-anchor automatically. Your nodes and connections are retained.";
        }
    }
}
