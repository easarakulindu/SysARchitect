namespace SysARchitect.Interaction
{
    using UnityEngine;
    using UnityEngine.EventSystems;
    using SysARchitect.Visualization;

    /// <summary>
    /// Allows dragging selected nodes to reposition them on the workspace.
    /// The node follows the mouse cursor along the tabletop surface plane
    /// while maintaining a grab offset so it does not snap to the cursor centre.
    /// </summary>
    public class NodeMovementController : MonoBehaviour
    {
        public bool UseExternalPointer { get; set; }
        public void MoveSelectedOnSurface(Vector3 point, Vector3 normal)
        {
            var visual = selectionController.SelectedNode;
            if (visual == null) return;
            float height = visual.GetComponent<MeshFilter>().sharedMesh.bounds.extents.y * visual.transform.lossyScale.y;
            visual.transform.position = point + normal * height;
        }
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
            if (UseExternalPointer) return;
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
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

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
            if (!TryGetSurfacePoint(out Vector3 surfacePoint)) return;

            dragOffset = selected.transform.position - surfacePoint;
            dragTarget = selected;
            isDragging = true;
        }

        private void HandleDrag()
        {
            // End drag on mouse release.
            if (!Input.GetMouseButton(0))
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

            if (TryGetSurfacePoint(out Vector3 surfacePoint))
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
        private bool TryGetSurfacePoint(out Vector3 point)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, placementSurface))
            {
                point = hit.point;
                return true;
            }
            point = default;
            return false;
        }

        private void OnDisable()
        {
            isDragging = false;
            dragTarget = null;
        }

        private static int LayerToMask(int layer)
        {
            return 1 << layer;
        }
    }
}
