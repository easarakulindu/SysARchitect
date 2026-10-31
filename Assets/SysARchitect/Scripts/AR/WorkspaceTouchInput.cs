using UnityEngine;
using SysARchitect.Graph;
using SysARchitect.Interaction;

namespace SysARchitect.AR
{
    /// <summary>Touch adapter for the existing builder commands; it does not own a graph or simulator.</summary>
    public class WorkspaceTouchInput : MonoBehaviour
    {
        [SerializeField] private ARWorkspace workspace;
        [SerializeField] private ArchitectureGraph graph;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private InteractionManager interactions;
        [SerializeField] private NodePlacementController placement;
        [SerializeField] private NodeSelectionController selection;
        [SerializeField] private NodeMovementController movement;
        [SerializeField] private ConnectionCreationController connections;
        private bool activeGesture;
        private Vector2 start;
        private bool moved;
        private int finger = -1;
        private bool beganOverUI;
        private void Awake()
        {
            placement.UseExternalPointer = selection.UseExternalPointer = movement.UseExternalPointer = connections.UseExternalPointer = true;
        }
        private void Update()
        {
            if (Input.touchCount != 1) { activeGesture = false; finger = -1; return; }
            var touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began) { finger = touch.fingerId; beganOverUI = NodeSelectionController.IsOverUI(touch.position); }
            if (touch.fingerId != finger) return;
            if (touch.phase == TouchPhase.Ended && !beganOverUI && !workspace.HasWorkspace && !NodeSelectionController.IsOverUI(touch.position))
                workspace.TryPlaceWorkspace(touch.position);
            HandlePointer(touch.position, touch.phase, workspace.CanInteract);
        }
        public void HandlePointer(Vector2 position, TouchPhase phase, bool trackingAllowsInput)
        {
            if (!trackingAllowsInput || phase == TouchPhase.Canceled) { activeGesture = false; return; }
            if (phase == TouchPhase.Began)
            {
                activeGesture = !NodeSelectionController.IsOverUI(position);
                start = position; moved = false;
                if (activeGesture && interactions.CurrentMode == InteractionManager.Mode.Select)
                    selection.HandlePointerClick(position, 24);
            }
            if (!activeGesture) return;
            moved |= Vector2.Distance(start, position) > 18;
            if (phase == TouchPhase.Moved && moved && interactions.CurrentMode == InteractionManager.Mode.Select &&
                !NodeSelectionController.IsOverUI(position) && TrySurfacePoint(position, out var point))
                movement.MoveSelectedOnSurface(point, graph.transform.up);
            if (phase != TouchPhase.Ended) return;
            activeGesture = false;
            if (moved || NodeSelectionController.IsOverUI(position)) return;
            if (interactions.CurrentMode == InteractionManager.Mode.Connect) connections.HandlePointerClick(position);
            else if (interactions.CurrentMode == InteractionManager.Mode.Place && TrySurfacePoint(position, out var surface))
            { placement.PlaceOnSurface(surface, graph.transform.up); interactions.ReturnToSelectMode(); }
        }
        public bool TrySurfacePoint(Vector2 position, out Vector3 point)
        {
            var plane = new Plane(graph.transform.up, graph.transform.position);
            var ray = mainCamera.ScreenPointToRay(position);
            if (plane.Raycast(ray, out float distance))
            {
                point = ray.GetPoint(distance);
                var local = graph.transform.InverseTransformPoint(point);
                if (Mathf.Abs(local.x) <= 1.2f && Mathf.Abs(local.z) <= 1.2f) return true;
            }
            point = default; return false;
        }
    }
}
