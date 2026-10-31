namespace SysARchitect.Interaction
{
    using UnityEngine;
    using UnityEngine.EventSystems;
    using SysARchitect.Visualization;
    using SysARchitect.Graph;
    using System.Collections.Generic;
    using System.Linq;
    using System;

    /// <summary>
    /// Handles node selection via mouse click in desktop mode.
    /// Left-click a <see cref="NodeVisual"/> to select it; click empty
    /// space to deselect. Fires events for the UI layer to react.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class NodeSelectionController : MonoBehaviour
    {
        public bool UseExternalPointer { get; set; }
        [SerializeField] private Camera mainCamera;
        [SerializeField] private ArchitectureGraph graph;
        [SerializeField] private LayerMask nodeLayer;

        private NodeVisual selectedNode;

        /// <summary>Fired when a node is selected, with the newly selected visual.</summary>
        public event Action<NodeVisual> OnNodeSelected;

        /// <summary>Fired when the current node is deselected.</summary>
        public event Action OnNodeDeselected;

        /// <summary>The currently selected node, or null if nothing is selected.</summary>
        public NodeVisual SelectedNode => selectedNode;
        public GraphConnection SelectedConnection { get; private set; }
        public event Action<GraphConnection> OnConnectionSelected;

        private void OnEnable()
        {
            if (graph == null) return;
            graph.OnConnectionRemoved += ConnectionRemoved;
            graph.OnNodeRemoved += NodeRemoved;
        }
        private void OnDisable()
        {
            if (graph == null) return;
            graph.OnConnectionRemoved -= ConnectionRemoved;
            graph.OnNodeRemoved -= NodeRemoved;
        }
        private void ConnectionRemoved(GraphConnection connection) { if (SelectedConnection == connection) Deselect(); }
        private void NodeRemoved(GraphNode node) { if (selectedNode != null && selectedNode.GetGraphNode() == node) Deselect(); }
        public static bool IsOverUI(Vector2 position)
        {
            if (EventSystem.current == null) return false;
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, results);
            return results.Count > 0;
        }

        void Update()
        {
            if (UseExternalPointer) return;
            if (!Input.GetMouseButtonDown(0)) return;

            HandlePointerClick(Input.mousePosition);
        }

        /// <summary>Shared screen-space picker; nodes take priority, edges use a pixel tolerance for future touch adapters.</summary>
        public void HandlePointerClick(Vector2 position, float tolerancePixels = 12)
        {
            if (IsOverUI(position)) return;
            Ray ray = mainCamera.ScreenPointToRay(position);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, nodeLayer))
            {
                var visual = hit.collider.GetComponentInParent<NodeVisual>();
                if (visual != null)
                {
                    Select(visual);
                    return;
                }
            }

            GraphConnection nearest = null;
            float best = tolerancePixels;
            if (graph != null)
                foreach (var edge in graph.Connections)
                {
                    ConnectionRenderer.GetEndpoints(edge.Source.transform, edge.Target.transform, out Vector3 from, out Vector3 to);
                    Vector3 a = mainCamera.WorldToScreenPoint(from), b = mainCamera.WorldToScreenPoint(to);
                    if (a.z <= mainCamera.nearClipPlane || b.z <= mainCamera.nearClipPlane) continue;
                    Vector2 delta = (Vector2)(b - a);
                    float t = delta.sqrMagnitude < .001f ? 0 : Mathf.Clamp01(Vector2.Dot(position - (Vector2)a, delta) / delta.sqrMagnitude);
                    float distance = Vector2.Distance(position, (Vector2)a + t * delta);
                    if (distance < best) { best = distance; nearest = edge; }
                }
            if (nearest != null) SelectConnection(nearest);
            else Deselect();
        }

        /// <summary>
        /// Programmatically select the given node, deselecting any previous selection.
        /// </summary>
        public void Select(NodeVisual node)
        {
            if (node == null) return;

            Deselect();
            selectedNode = node;
            selectedNode.SetSelected(true);
            OnNodeSelected?.Invoke(selectedNode);
        }

        /// <summary>
        /// Deselect the current node if one is selected.
        /// </summary>
        public void SelectConnection(GraphConnection connection)
        {
            if (connection == null || graph == null || !graph.Connections.Contains(connection)) return;
            Deselect();
            SelectedConnection = connection;
            connection.GetComponent<ConnectionRenderer>().SetSelected(true);
            OnConnectionSelected?.Invoke(connection);
        }

        public void Deselect()
        {
            if (selectedNode == null && SelectedConnection == null) return;
            if (selectedNode != null) selectedNode.SetSelected(false);
            if (SelectedConnection != null) SelectedConnection.GetComponent<ConnectionRenderer>().SetSelected(false);
            selectedNode = null;
            SelectedConnection = null;
            OnNodeDeselected?.Invoke();
        }
    }
}
