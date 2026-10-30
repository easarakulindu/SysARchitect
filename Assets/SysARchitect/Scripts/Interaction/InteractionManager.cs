namespace SysARchitect.Interaction
{
    using UnityEngine;
    using SysARchitect.Core;
    using SysARchitect.Graph;

    /// <summary>
    /// Coordinates between different interaction modes to prevent conflicts.
    /// Only one mode is active at a time; switching modes cancels any
    /// in-progress operation and enables/disables the appropriate controllers.
    /// </summary>
    public class InteractionManager : MonoBehaviour
    {
        /// <summary>Available interaction modes.</summary>
        public enum Mode
        {
            /// <summary>Click nodes to select them; drag to move.</summary>
            Select,
            /// <summary>Place a new node from the toolbox.</summary>
            Place,
            /// <summary>Two-tap connection creation.</summary>
            Connect,
            /// <summary>Click a node to delete it.</summary>
            Delete
        }

        [SerializeField] private NodePlacementController placementController;
        [SerializeField] private NodeSelectionController selectionController;
        [SerializeField] private ConnectionCreationController connectionController;
        [SerializeField] private NodeMovementController movementController;
        [SerializeField] private ArchitectureGraph graph;

        private Mode currentMode = Mode.Select;

        /// <summary>The currently active interaction mode.</summary>
        public Mode CurrentMode => currentMode;

        void Start()
        {
            ApplyMode(currentMode);
        }

        void LateUpdate()
        {
            if (Input.GetKeyDown(KeyCode.Escape) ||
                (currentMode == Mode.Place && placementController != null && !placementController.IsPlacing) ||
                (currentMode == Mode.Connect && connectionController != null && !connectionController.IsConnecting))
                ReturnToSelectMode();
        }

        /// <summary>
        /// Switch to the specified interaction mode, cancelling any active operation.
        /// </summary>
        public void SetMode(Mode mode)
        {
            if (currentMode == mode) return;

            CancelCurrentOperation();
            currentMode = mode;
            ApplyMode(currentMode);
        }

        /// <summary>
        /// Called by the toolbox UI when a node type button is clicked.
        /// Switches to <see cref="Mode.Place"/> and begins placement.
        /// </summary>
        public void OnToolboxNodeSelected(NodeType type)
        {
            CancelCurrentOperation();
            currentMode = Mode.Place;
            ApplyMode(currentMode);

            if (placementController != null)
            {
                placementController.BeginPlacement(type);
            }
        }

        /// <summary>
        /// Called by the UI Connect button. Switches to <see cref="Mode.Connect"/>.
        /// </summary>
        public void OnConnectButtonPressed()
        {
            SetMode(Mode.Connect);

            if (connectionController != null)
            {
                connectionController.BeginConnectionMode();
            }
        }

        /// <summary>
        /// Called by the UI Delete button. If a node is currently selected,
        /// removes it from the graph and returns to Select mode.
        /// </summary>
        public void OnDeleteButtonPressed()
        {
            if (selectionController == null || graph == null) return;

            var selected = selectionController.SelectedNode;
            if (selected == null) return;

            var graphNode = selected.GetGraphNode();
            if (graphNode == null) return;

            selectionController.Deselect();
            graph.RemoveNode(graphNode);

            ReturnToSelectMode();
        }

        /// <summary>Clear the workspace after cancelling transient interactions.</summary>
        public void OnClearButtonPressed()
        {
            ReturnToSelectMode();
            if (selectionController != null)
                selectionController.Deselect();
            if (graph != null)
                graph.ClearGraph();
        }

        /// <summary>
        /// Cancel any active operation and return to <see cref="Mode.Select"/>.
        /// </summary>
        public void ReturnToSelectMode()
        {
            CancelCurrentOperation();
            currentMode = Mode.Select;
            ApplyMode(currentMode);
        }

        private void CancelCurrentOperation()
        {
            switch (currentMode)
            {
                case Mode.Place:
                    if (placementController != null)
                        placementController.CancelPlacement();
                    break;

                case Mode.Connect:
                    if (connectionController != null)
                        connectionController.CancelConnection();
                    break;
            }
        }

        private void ApplyMode(Mode mode)
        {
            // Enable/disable controllers based on the active mode.
            if (selectionController != null)
                selectionController.enabled = (mode == Mode.Select || mode == Mode.Delete);

            if (movementController != null)
                movementController.enabled = (mode == Mode.Select);

            if (placementController != null)
                placementController.enabled = (mode == Mode.Place);

            if (connectionController != null)
                connectionController.enabled = (mode == Mode.Connect);
        }
    }
}
