namespace SysARchitect.Interaction
{
    using UnityEngine;
    using UnityEngine.EventSystems;
    using SysARchitect.Core;
    using SysARchitect.Graph;
    using SysARchitect.Visualization;

    /// <summary>
    /// Handles placing new infrastructure nodes onto the workspace.
    /// In desktop mode, places on a virtual tabletop plane via raycast.
    /// <para>
    /// Call <see cref="BeginPlacement"/> with a <see cref="NodeType"/> to enter
    /// placement mode. A semi-transparent preview mesh follows the mouse cursor
    /// over the placement surface. Left-click finalises; right-click or Escape
    /// cancels.
    /// </para>
    /// </summary>
    public class NodePlacementController : MonoBehaviour
    {
        public bool UseExternalPointer { get; set; }
        public bool PlaceOnSurface(Vector3 point, Vector3 normal)
        {
            if (!pendingNodeType.HasValue || previewObject == null) return false;
            float height = previewObject.GetComponent<MeshFilter>().sharedMesh.bounds.extents.y * graph.transform.lossyScale.y;
            FinalizePlacement(point + normal * height);
            return true;
        }
        [SerializeField] private ArchitectureGraph graph;
        [SerializeField] private Material defaultNodeMaterial;
        [SerializeField] private LayerMask placementSurface;
        [SerializeField] private Camera mainCamera;

        private NodeType? pendingNodeType;
        private GameObject previewObject;

        /// <summary>Whether a placement operation is currently in progress.</summary>
        public bool IsPlacing => pendingNodeType.HasValue;

        /// <summary>
        /// Begin placing a node of the given <paramref name="type"/>.
        /// Creates a semi-transparent preview that follows the mouse cursor
        /// on the placement surface.
        /// </summary>
        public void BeginPlacement(NodeType type)
        {
            // Cancel any existing placement first.
            if (pendingNodeType.HasValue)
            {
                CancelPlacement();
            }

            pendingNodeType = type;

            // Build preview object with the correct procedural mesh.
            previewObject = new GameObject($"Preview_{type}");
            var meshFilter = previewObject.AddComponent<MeshFilter>();
            meshFilter.mesh = ProceduralMeshGenerator.GenerateMesh(type);

            var meshRenderer = previewObject.AddComponent<MeshRenderer>();

            // Create a semi-transparent instance of the default material.
            var previewMat = new Material(defaultNodeMaterial);
            Color col = NodeColorPalette.GetAccentColor(type);
            col.a = 0.5f;
            previewMat.color = col;

            // Enable transparency on URP/Lit materials.
            if (previewMat.HasProperty("_Surface"))
            {
                previewMat.SetFloat("_Surface", 1f); // 1 = Transparent
                previewMat.SetFloat("_Blend", 0f);
                previewMat.SetOverrideTag("RenderType", "Transparent");
                previewMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                previewMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                previewMat.SetInt("_ZWrite", 0);
                previewMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                previewMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            meshRenderer.material = previewMat;
            previewObject.SetActive(false); // hidden until first valid raycast
        }

        /// <summary>
        /// Cancel the current placement and destroy the preview object.
        /// </summary>
        public void CancelPlacement()
        {
            if (previewObject != null)
            {
                // Clean up instanced preview material.
                var renderer = previewObject.GetComponent<MeshRenderer>();
                if (renderer != null && renderer.material != null)
                {
                    Destroy(renderer.material);
                }
                Destroy(previewObject.GetComponent<MeshFilter>().sharedMesh);
                Destroy(previewObject);
                previewObject = null;
            }

            pendingNodeType = null;
        }

        void OnDisable() => CancelPlacement();

        void Update()
        {
            if (UseExternalPointer) return;
            if (!pendingNodeType.HasValue) return;

            // Cancel on right-click or Escape.
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelPlacement();
                return;
            }

            // Raycast from mouse position onto the placement surface.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                previewObject.SetActive(false);
                return;
            }
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, placementSurface))
            {
                if (!previewObject.activeSelf)
                {
                    previewObject.SetActive(true);
                }

                previewObject.transform.position = hit.point + Vector3.up *
                    previewObject.GetComponent<MeshFilter>().sharedMesh.bounds.extents.y;

                // Finalise on left-click.
                if (Input.GetMouseButtonDown(0))
                {
                    FinalizePlacement(previewObject.transform.position);
                }
            }
            else
            {
                // Hide preview when not hovering over a valid surface.
                if (previewObject.activeSelf)
                {
                    previewObject.SetActive(false);
                }
            }
        }

        private void FinalizePlacement(Vector3 position)
        {
            NodeType type = pendingNodeType.Value;

            // Create the actual node via the graph.
            graph.AddNode(type, position);

            // Clean up preview.
            CancelPlacement();
        }
    }
}
