using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using SysARchitect.Graph;
using SysARchitect.Simulation;

namespace SysARchitect.AR
{
    /// <summary>Owns only the real-world placement and tracking of the shared architecture.</summary>
    public class ARWorkspace : MonoBehaviour
    {
        [SerializeField] private ArchitectureGraph graph;
        [SerializeField] private SimulationController simulation;
        [SerializeField] private ARRaycastManager raycasts;
        [SerializeField] private ARPlaneManager planes;
        [SerializeField] private ARAnchorManager anchors;
        [SerializeField] private TextMeshProUGUI status;
        private readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
        private ARAnchor anchor;
        private bool placing;
        private string message;
        public bool HasWorkspace => anchor != null;
        public bool CanInteract => HasWorkspace && TrackingAllowsInput(ARSession.state == ARSessionState.SessionTracking, anchor.trackingState == TrackingState.Tracking);
        public static bool TrackingAllowsInput(bool sessionTracking, bool anchorTracking) => sessionTracking && anchorTracking;

        private void Start()
        {
            graph.gameObject.SetActive(false);
            Application.targetFrameRate = 60;
            Input.simulateMouseWithTouches = false;
        }
        private void Update()
        {
            if (!CanInteract) simulation.Pause();
            status.text = ARSession.state == ARSessionState.Unsupported ? "AR is unavailable on this device. Use the desktop demo on a supported computer."
                : ARSession.state != ARSessionState.SessionTracking ? $"AR: {ARSession.state}. Move slowly and scan a well-lit textured surface. {ARSession.notTrackingReason}"
                : placing ? "Creating surface anchor…"
                : !HasWorkspace ? message ?? "Scan a horizontal surface. Tap a detected plane to anchor the workspace."
                : !CanInteract ? "Workspace tracking lost. Move back toward the surface; editing and simulation are paused."
                : "Workspace tracked. Toolbox: choose a component, then tap the workspace. Select: tap nodes/lines; drag nodes. Connect: tap two nodes.";
        }
        public async void TryPlaceWorkspace(Vector2 screenPosition)
        {
            if (placing || HasWorkspace || ARSession.state != ARSessionState.SessionTracking) return;
            if (!raycasts.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
            { message = "No detected plane at that point. Scan a horizontal surface and tap its highlighted area."; return; }
            var plane = planes.GetPlane(hits[0].trackableId);
            if (plane == null || plane.alignment != PlaneAlignment.HorizontalUp) return;
            placing = true;
            try
            {
                var result = await anchors.TryAddAnchorAsync(hits[0].pose);
                if (this == null) return;
                if (!result.status.IsSuccess()) { message = "The device could not anchor that surface. Scan again and retry."; return; }
                anchor = result.value;
                graph.transform.SetParent(anchor.transform, false);
                graph.transform.localPosition = Vector3.zero;
                graph.transform.localRotation = Quaternion.identity;
                graph.transform.localScale = Vector3.one * .35f;
                graph.gameObject.SetActive(true);
                planes.enabled = false;
                foreach (var trackedPlane in planes.trackables) trackedPlane.gameObject.SetActive(false);
            }
            catch (System.Exception exception) { message = "Anchor creation failed: " + exception.Message; Debug.LogWarning(message); }
            finally { placing = false; }
        }
        public void Reposition()
        {
            simulation.Pause();
            graph.transform.SetParent(null, true);
            graph.gameObject.SetActive(false);
            if (anchor != null) anchors.TryRemoveAnchor(anchor);
            anchor = null;
            planes.enabled = true;
            foreach (var trackedPlane in planes.trackables) trackedPlane.gameObject.SetActive(true);
            message = "Tap another detected horizontal surface. Your nodes and connections are retained.";
        }
    }
}
