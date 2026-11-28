using UnityEngine;
using UnityEngine.InputSystem;
using TouchPhase = UnityEngine.TouchPhase;
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
#if UNITY_EDITOR
        private bool editorMousePressed;
#endif
        private void Awake()
        {
            placement.UseExternalPointer = selection.UseExternalPointer = movement.UseExternalPointer = connections.UseExternalPointer = true;
        }
        private void Update()
        {
            var screen = Touchscreen.current;
#if UNITY_EDITOR
            // XR Simulation still uses the mobile editing commands. Accept an ordinary
            // Editor mouse when no enabled touch device is supplying that input.
            if (!EditorTouchOwnsInput(screen))
            {
                finger = -1;
                ReadEditorMouse(Mouse.current);
                return;
            }
            if (editorMousePressed) { editorMousePressed = false; activeGesture = false; }
#endif
            ReadTouchscreen(screen);
        }
#if UNITY_EDITOR
        private static bool EditorTouchOwnsInput(Touchscreen screen)
        {
            if (screen == null || !screen.enabled) return false;
            var simulation = UnityEngine.InputSystem.EnhancedTouch.TouchSimulation.instance;
            if (simulation != null && simulation.enabled) return true;
            foreach (var touch in screen.touches)
                if (touch.press.isPressed || touch.press.wasReleasedThisFrame) return true;
            return false;
        }
        private void ReadEditorMouse(Mouse mouse)
        {
            if (mouse == null || !mouse.enabled || mouse.rightButton.isPressed || mouse.middleButton.isPressed)
            { editorMousePressed = false; activeGesture = false; return; }
            Vector2 position = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame)
            {
                editorMousePressed = true;
                beganOverUI = NodeSelectionController.IsOverUI(position);
                HandlePointer(position, TouchPhase.Began, workspace.CanInteract);
            }
            else if (mouse.leftButton.wasReleasedThisFrame && editorMousePressed)
            {
                editorMousePressed = false;
                if (!beganOverUI && !workspace.HasWorkspace && !NodeSelectionController.IsOverUI(position))
                    workspace.TryPlaceWorkspace(position);
                HandlePointer(position, TouchPhase.Ended, workspace.CanInteract);
            }
            else if (editorMousePressed && mouse.leftButton.isPressed)
                HandlePointer(position, TouchPhase.Moved, workspace.CanInteract);
        }
        private void OnDisable() { editorMousePressed = false; activeGesture = false; finger = -1; }
#endif
        public void ReadTouchscreen(Touchscreen screen)
        {
            if (screen == null) { activeGesture = false; finger = -1; return; }
            UnityEngine.InputSystem.Controls.TouchControl touch = null;
            int count = 0;
            foreach (var candidate in screen.touches)
                if (candidate.press.isPressed || candidate.press.wasReleasedThisFrame) { touch = candidate; count++; }
            if (count != 1) { activeGesture = false; finger = -1; return; }
            int id = touch.touchId.ReadValue(); Vector2 position = touch.position.ReadValue();
            var phase = ConvertPhase(touch.phase.ReadValue());
            if (phase == TouchPhase.Began) { finger = id; beganOverUI = NodeSelectionController.IsOverUI(position); }
            if (id != finger) return;
            if (phase == TouchPhase.Ended && !beganOverUI && !workspace.HasWorkspace && !NodeSelectionController.IsOverUI(position))
                workspace.TryPlaceWorkspace(position);
            HandlePointer(position, phase, workspace.CanInteract);
        }
        public static TouchPhase ConvertPhase(UnityEngine.InputSystem.TouchPhase phase)
        {
            switch (phase)
            {
                case UnityEngine.InputSystem.TouchPhase.Began: return TouchPhase.Began;
                case UnityEngine.InputSystem.TouchPhase.Moved: return TouchPhase.Moved;
                case UnityEngine.InputSystem.TouchPhase.Stationary: return TouchPhase.Stationary;
                case UnityEngine.InputSystem.TouchPhase.Ended: return TouchPhase.Ended;
                default: return TouchPhase.Canceled;
            }
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
