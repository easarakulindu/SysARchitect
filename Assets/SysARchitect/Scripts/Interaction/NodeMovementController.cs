namespace SysARchitect.Interaction
{
    using UnityEngine;
    using SysARchitect.Visualization;

    /// <summary>
    /// Allows dragging selected nodes to reposition them on the workspace.
    /// The node follows the mouse cursor along the tabletop surface plane
    /// while maintaining a grab offset so it does not snap to the cursor centre.
    /// </summary>
    public class NodeMovementController : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private LayerMask placementSurface;
        [SerializeField] private NodeSelectionController selectionController;

        [Tooltip("Smooth movement interpolation speed (higher = snappier).")]
        [SerializeField] private float moveSmoothing = 20f;

        private bool isDragging;
        private Vector3 dragOffset;
        private NodeVisual dragTarget;

        /// <summary>Whether a drag operation is currently in progress.</summary>
        public bool IsDragging => isDragging;

        void Update()
        {
            if (isDragging)
            {
                HandleDrag();
                return;
            }

            TryBeginDrag();
        }

        private void TryBeginDrag()
        {
            if (!Input.GetMouseButtonDown(0)) return;

            // Only drag the currently selected node.
            NodeVisual selected = selectionController != null ? selectionController.SelectedNode : null;
            if (selected == null) return;

            // Verify the click is actually on the selected node.
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f, placementSurface | LayerToMask(selected.gameObject.layer)))
            {
                return;
            }

            var hitVisual = hit.collider.GetComponentInParent<NodeVisual>();
            if (hitVisual != selected) return;

            // Calculate offset between node position and surface hit point.
            Vector3 surfacePoint = GetSurfacePoint();
            if (surfacePoint == Vector3.zero && !IsOnSurface(surfacePoint))
            {
                // Fallback: use the node's current position.
                surfacePoint = selected.transform.position;
            }

            dragOffset = selected.transform.position - surfacePoint;
            dragTarget = selected;
            isDragging = true;
        }

        private void HandleDrag()
        {
            // End drag on mouse release.
            if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
                dragTarget = null;
                return;
            }

            if (dragTarget == null)
            {
                isDragging = false;
                return;
            }

            Vector3 surfacePoint = GetSurfacePoint();
            if (IsOnSurface(surfacePoint))
            {
                Vector3 targetPos = surfacePoint + dragOffset;
                // Smooth movement to avoid jitter.
                dragTarget.transform.position = Vector3.Lerp(
                    dragTarget.transform.position,
                    targetPos,
                    Time.deltaTime * moveSmoothing
                );
            }
        }

        /// <summary>
        /// Raycast from the mouse cursor onto the placement surface to get a
        /// world-space position.
        /// </summary>
        private Vector3 GetSurfacePoint()
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, placementSurface))
            {
                return hit.point;
            }

            // Fallback: project onto y=0 plane.
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float enter))
            {
                return ray.GetPoint(enter);
            }

            return Vector3.zero;
        }

        private static bool IsOnSurface(Vector3 point)
        {
            // Simple validity check — a zero vector is our sentinel for "no hit".
            return point.sqrMagnitude > 0.0001f || Mathf.Abs(point.y) > 0.0001f;
        }

        private static int LayerToMask(int layer)
        {
            return 1 << layer;
        }
    }
}
