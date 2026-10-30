namespace SysARchitect.Visualization
{
    using UnityEngine;
    using TMPro;
    using SysARchitect.Core;
    using SysARchitect.Graph;

    /// <summary>
    /// Manages the visual representation of an infrastructure node.
    /// Handles mesh generation, material tinting, status colour changes,
    /// floating label, and selection highlight.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class NodeVisual : MonoBehaviour
    {
        // ── Cached references ──────────────────────────────────────────
        MeshFilter m_MeshFilter;
        MeshRenderer m_MeshRenderer;
        GraphNode m_GraphNode;
        TextMeshPro m_Label;
        MaterialPropertyBlock m_PropBlock;
        BoxCollider m_Collider;

        // ── Visual state ───────────────────────────────────────────────
        bool m_IsSelected;
        NodeStatus m_CurrentStatus = NodeStatus.Healthy;
        bool m_IsDirty;

        // ── Shader property IDs (cached to avoid hashing each frame) ──
        static readonly int k_BaseColorId    = Shader.PropertyToID("_BaseColor");
        static readonly int k_EmissionColorId = Shader.PropertyToID("_EmissionColor");

        // ────────────────────────────────────────────────────────────────
        // Public API
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Sets up the visual for the given <see cref="GraphNode"/>.
        /// Creates procedural mesh, floating label, and box collider.
        /// </summary>
        /// <param name="node">The data node this visual represents.</param>
        /// <param name="baseMaterial">
        /// A URP Lit or Unlit material instance. A per-renderer copy is
        /// created automatically so we can tint without leaking shared state.
        /// </param>
        public void Initialize(GraphNode node, Material baseMaterial)
        {
            m_GraphNode = node;

            // Components
            m_MeshFilter = GetComponent<MeshFilter>();
            m_MeshRenderer = GetComponent<MeshRenderer>();
            m_PropBlock = new MaterialPropertyBlock();

            // Procedural mesh
            Mesh mesh = ProceduralMeshGenerator.GenerateMesh(node.Type);
            m_MeshFilter.sharedMesh = mesh;

            // Material — use a per-instance copy so colour changes are isolated.
            m_MeshRenderer.material = new Material(baseMaterial);
            ApplyAccentColor(node.Type);

            // Box collider for raycasting
            m_Collider = gameObject.AddComponent<BoxCollider>();
            m_Collider.center = mesh.bounds.center;
            m_Collider.size = mesh.bounds.size;

            // Floating label
            CreateLabel(node.DisplayName, mesh.bounds);

            // Initial status
            m_CurrentStatus = NodeStatus.Healthy;
            m_IsDirty = true;
        }

        /// <summary>Toggles the selection highlight.</summary>
        public void SetSelected(bool selected)
        {
            if (m_IsSelected == selected) return;
            m_IsSelected = selected;
            m_IsDirty = true;
        }

        /// <summary>Updates the status colour on the node enclosure.</summary>
        public void UpdateStatus(NodeStatus status)
        {
            if (m_CurrentStatus == status) return;
            m_CurrentStatus = status;
            m_IsDirty = true;
        }

        /// <summary>Changes the text shown on the floating label.</summary>
        public void UpdateLabel(string text)
        {
            if (m_Label != null)
            {
                m_Label.text = text;
            }
        }

        /// <summary>Returns the underlying <see cref="GraphNode"/>.</summary>
        public GraphNode GetGraphNode() => m_GraphNode;

        // ────────────────────────────────────────────────────────────────
        // Unity lifecycle
        // ────────────────────────────────────────────────────────────────

        void LateUpdate()
        {
            if (!m_IsDirty) return;
            m_IsDirty = false;
            ApplyVisualState();
        }

        // ────────────────────────────────────────────────────────────────
        // Internal helpers
        // ────────────────────────────────────────────────────────────────

        void ApplyAccentColor(NodeType type)
        {
            Color accent = NodeColorPalette.GetAccentColor(type);
            m_MeshRenderer.GetPropertyBlock(m_PropBlock);
            m_PropBlock.SetColor(k_BaseColorId, accent);
            m_PropBlock.SetColor(k_EmissionColorId, accent * 0.3f);
            m_MeshRenderer.SetPropertyBlock(m_PropBlock);
        }

        void ApplyVisualState()
        {
            if (m_MeshRenderer == null) return;

            Color statusColor = NodeColorPalette.GetStatusColor(m_CurrentStatus);
            Color emission;

            if (m_IsSelected)
            {
                emission = NodeColorPalette.Selected * 1.2f;
            }
            else
            {
                emission = statusColor * 0.35f;
            }

            m_MeshRenderer.GetPropertyBlock(m_PropBlock);
            m_PropBlock.SetColor(k_BaseColorId, statusColor);
            m_PropBlock.SetColor(k_EmissionColorId, emission);
            m_MeshRenderer.SetPropertyBlock(m_PropBlock);
        }

        void CreateLabel(string text, Bounds meshBounds)
        {
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(transform, false);

            // Position the label 0.1 units above the mesh top.
            float topY = meshBounds.center.y + meshBounds.extents.y;
            labelGo.transform.localPosition = new Vector3(0f, topY + 0.1f, 0f);

            m_Label = labelGo.AddComponent<TextMeshPro>();
            m_Label.text = text;
            m_Label.fontSize = 2f;
            m_Label.alignment = TextAlignmentOptions.Center;
            m_Label.enableAutoSizing = false;

            // World-space rect size — keep compact.
            m_Label.rectTransform.sizeDelta = new Vector2(1f, 0.3f);

            // Sorting — render on top of nodes.
            m_Label.sortingOrder = 10;
        }
    }
}
