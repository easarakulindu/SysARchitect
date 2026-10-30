namespace SysARchitect.Interaction
{
    using UnityEngine;
    using UnityEngine.EventSystems;
    using SysARchitect.Visualization;
    using System;

    /// <summary>
    /// Handles node selection via mouse click in desktop mode.
    /// Left-click a <see cref="NodeVisual"/> to select it; click empty
    /// space to deselect. Fires events for the UI layer to react.
    /// </summary>
    public class NodeSelectionController : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private LayerMask nodeLayer;

        private NodeVisual selectedNode;

        /// <summary>Fired when a node is selected, with the newly selected visual.</summary>
        public event Action<NodeVisual> OnNodeSelected;

        /// <summary>Fired when the current node is deselected.</summary>
        public event Action OnNodeDeselected;

        /// <summary>The currently selected node, or null if nothing is selected.</summary>
        public NodeVisual SelectedNode => selectedNode;

        void Update()
        {
            if (!Input.GetMouseButtonDown(0)) return;

            // Don't handle clicks that fall on UI elements.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, nodeLayer))
            {
                var visual = hit.collider.GetComponentInParent<NodeVisual>();
                if (visual != null)
                {
                    Select(visual);
                    return;
                }
            }

            // Clicked empty space — deselect.
            Deselect();
        }

        /// <summary>
        /// Programmatically select the given node, deselecting any previous selection.
        /// </summary>
        public void Select(NodeVisual node)
        {
            if (node == null) return;

            // Deselect previous if different.
            if (selectedNode != null && selectedNode != node)
            {
                selectedNode.SetSelected(false);
            }

            selectedNode = node;
            selectedNode.SetSelected(true);
            OnNodeSelected?.Invoke(selectedNode);
        }

        /// <summary>
        /// Deselect the current node if one is selected.
        /// </summary>
        public void Deselect()
        {
            if (selectedNode == null) return;

            selectedNode.SetSelected(false);
            selectedNode = null;
            OnNodeDeselected?.Invoke();
        }
    }
}
