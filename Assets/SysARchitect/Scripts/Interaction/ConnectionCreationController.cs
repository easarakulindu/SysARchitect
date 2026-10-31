namespace SysARchitect.Interaction
{
    using UnityEngine;
    using UnityEngine.EventSystems;
    using SysARchitect.Graph;
    using SysARchitect.Core;
    using SysARchitect.Visualization;
    using System;

    /// <summary>
    /// Handles creating connections between nodes using a two-tap workflow.
    /// Enter connection mode, click a source node, then click a target node to
    /// create a directed connection. A preview line follows the cursor while
    /// the source is selected.
    /// </summary>
    public class ConnectionCreationController : MonoBehaviour
    {
        public bool UseExternalPointer { get; set; }
        [SerializeField] private ArchitectureGraph graph;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private LayerMask nodeLayer;
        [SerializeField] private Material connectionMaterial;

        [Header("Preview Line")]
        [SerializeField] private float lineWidth = 0.01f;
        [SerializeField] private Color previewColor = new Color(0f, 0.9f, 0.9f, 0.6f);

        private bool isConnecting;
        private GraphNode sourceNode;
        private LineRenderer previewLine;

        /// <summary>Fired when a connection fails validation.</summary>
        public event Action<string> OnConnectionError;

        /// <summary>Fired when a connection is successfully created.</summary>
        public event Action OnConnectionCreated;
        public event Action<string> OnConnectionPrompt;
        public GraphNode SourceNode => sourceNode;

        /// <summary>Whether connection mode is currently active.</summary>
        public bool IsConnecting => isConnecting;

        /// <summary>
        /// Enter connection mode. The next node click sets the source.
        /// </summary>
        public void BeginConnectionMode()
        {
            CancelConnection();
            isConnecting = true;
            sourceNode = null;
            CleanupPreviewLine();
            OnConnectionPrompt?.Invoke("Click source, then target. Configured Database Router links accept either click order. Set roles in Select / Move before connecting.");
        }

        /// <summary>
        /// Cancel connection mode and clean up preview visuals.
        /// </summary>
        public void CancelConnection()
        {
            if (sourceNode != null) sourceNode.GetComponent<NodeVisual>().SetSelected(false);
            isConnecting = false;
            sourceNode = null;
            CleanupPreviewLine();
        }

        void Update()
        {
            if (UseExternalPointer) return;
            if (!isConnecting) return;

            // Cancel on right-click or Escape.
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelConnection();
                return;
            }

            // Update preview line to follow mouse.
            if (sourceNode != null && previewLine != null)
            {
                Vector3 mouseWorld = GetMouseWorldOnPlane();
                previewLine.SetPosition(0, sourceNode.transform.position);
                previewLine.SetPosition(1, mouseWorld);
            }

            // Handle left-click.
            if (!Input.GetMouseButtonDown(0)) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            // Check if we hit a node.
            HandlePointerClick(Input.mousePosition);
        }

        /// <summary>The mouse and future touch adapters share the same screen-space picking path.</summary>
        public void HandlePointerClick(Vector2 screenPosition)
        {
            if (!isConnecting) return;
            var pointer = new PointerEventData(EventSystem.current) { position = screenPosition };
            var hits = new System.Collections.Generic.List<RaycastResult>();
            if (EventSystem.current != null)
            {
                EventSystem.current.RaycastAll(pointer, hits);
                if (hits.Count > 0) return;
            }
            Ray ray = mainCamera.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f, nodeLayer)) return;

            var visual = hit.collider.GetComponentInParent<NodeVisual>();
            if (visual == null) return;

            GraphNode clickedNode = visual.GetGraphNode();
            if (clickedNode == null) return;

            if (sourceNode == null)
            {
                // First click — set source.
                SetSource(clickedNode);
            }
            else
            {
                // Second click — attempt to create connection.
                if (clickedNode == sourceNode)
                {
                    // Clicked same node — cancel.
                    OnConnectionError?.Invoke("Choose a different target; a node cannot connect to itself.");
                    return;
                }

                TryCreateConnection(clickedNode);
            }
        }

        private void SetSource(GraphNode node)
        {
            sourceNode = node;
            sourceNode.GetComponent<NodeVisual>().SetSelected(true);
            CreatePreviewLine();
            OnConnectionPrompt?.Invoke($"First endpoint: {node.DisplayName}. Click the other endpoint. Database Router links keep Backend > Router > Database direction; other links use source then target.");
        }

        void OnDisable() => CancelConnection();

        private void TryCreateConnection(GraphNode target)
        {
            GraphNode from = sourceNode, to = target;
            // Only an explicitly configured database router has unambiguous endpoint roles.
            // Never infer database routing from a default application balancer: reversing
            // Backend > Application LB would silently create the wrong learning architecture.
            if ((sourceNode.Type == NodeType.Database && IsDatabaseRouter(target)) ||
                (IsDatabaseRouter(sourceNode) && target.Type == NodeType.Backend))
            {
                from = target;
                to = sourceNode;
            }
            var (isValid, errorMessage) = graph.ValidateConnection(from, to);

            if (!isValid)
            {
                OnConnectionError?.Invoke(errorMessage);
                // Stay in connection mode so the user can pick a different target.
                return;
            }

            var edge = graph.AddConnection(from, to);
            if (edge == null)
            {
                OnConnectionError?.Invoke(graph.ValidateConnection(from, to).errorMessage);
                return;
            }
            OnConnectionPrompt?.Invoke($"Connected {from.DisplayName} > {to.DisplayName}. Choose Connect to add another link.");
            OnConnectionCreated?.Invoke();

            // Return to a clean connection state so the user can chain connections.
            CancelConnection();
        }

        private static bool IsDatabaseRouter(GraphNode node) =>
            node.Type == NodeType.LoadBalancer && node.RoutingRole == LoadBalancerRole.Database;

        private void CreatePreviewLine()
        {
            var lineGO = new GameObject("ConnectionPreview");
            previewLine = lineGO.AddComponent<LineRenderer>();
            previewLine.positionCount = 2;
            previewLine.startWidth = lineWidth;
            previewLine.endWidth = lineWidth;
            previewLine.useWorldSpace = true;

            if (connectionMaterial != null)
            {
                var mat = new Material(connectionMaterial);
                mat.color = previewColor;
                previewLine.material = mat;
            }
            else
            {
                // Fallback: unlit white.
                previewLine.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                previewLine.material.color = previewColor;
            }

            Vector3 start = sourceNode.transform.position;
            previewLine.SetPosition(0, start);
            previewLine.SetPosition(1, start);
        }

        private void CleanupPreviewLine()
        {
            if (previewLine == null) return;

            // Clean up instanced material.
            if (previewLine.material != null)
            {
                Destroy(previewLine.material);
            }

            Destroy(previewLine.gameObject);
            previewLine = null;
        }

        /// <summary>
        /// Project the mouse cursor onto the horizontal plane at y=0
        /// to give a world-space position for the preview line endpoint.
        /// </summary>
        private Vector3 GetMouseWorldOnPlane()
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            var plane = new Plane(Vector3.up, Vector3.zero);

            if (plane.Raycast(ray, out float enter))
            {
                return ray.GetPoint(enter);
            }

            // Fallback: project forward a fixed distance.
            return ray.GetPoint(2f);
        }
    }
}
