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
        Transform cpuFill, memoryFill;
        float nextResourceUpdate;
        Transform resourceRoot;
        Transform workspaceRoot;
        float resourceGaugeWidth;

        // ── Visual state ───────────────────────────────────────────────
        bool m_IsSelected;
        public bool IsSelected => m_IsSelected;
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
            workspaceRoot = node.transform.parent;

            // Components
            m_MeshFilter = GetComponent<MeshFilter>();
            m_MeshRenderer = GetComponent<MeshRenderer>();
            m_PropBlock = new MaterialPropertyBlock();

            // Procedural mesh
            Mesh mesh = ProceduralMeshGenerator.GenerateHolographicMesh(node.Type);
            m_MeshFilter.sharedMesh = mesh;

            // Material — use a per-instance copy so colour changes are isolated.
            m_MeshRenderer.material = new Material(baseMaterial);
            m_MeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            m_MeshRenderer.receiveShadows = false;
            ApplyAccentColor(node.Type);

            // Box collider for raycasting
            m_Collider = gameObject.AddComponent<BoxCollider>();
            m_Collider.center = mesh.bounds.center;
            m_Collider.size = mesh.bounds.size;

            // Floating label
            CreateLabel(node.DisplayName, mesh.bounds);
            CreateResourceIndicators(mesh.bounds);

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
            m_IsDirty = true;
            if (m_Label != null) m_Label.text = text;
        }

        /// <summary>Returns the underlying <see cref="GraphNode"/>.</summary>
        public GraphNode GetGraphNode() => m_GraphNode;

        // ────────────────────────────────────────────────────────────────
        // Unity lifecycle
        // ────────────────────────────────────────────────────────────────

        void LateUpdate()
        {
            if (m_Label != null && Camera.main != null)
            {
                m_Label.transform.rotation = Camera.main.transform.rotation;
                // Keep contained workload labels legible without magnifying the actual model.
                float parentScale = workspaceRoot != null ? workspaceRoot.lossyScale.x : 1;
                float labelScale = Mathf.Min(3.5f, parentScale / Mathf.Max(.001f, transform.lossyScale.x));
                m_Label.transform.localScale = Vector3.one * labelScale;
                m_Label.fontSize = m_GraphNode.HostNodeId != null && !m_IsSelected ? .32f : .5f;
                bool deeplyNested = transform.parent != null && transform.parent.GetComponent<GraphNode>()?.HostNodeId != null;
                m_Label.enabled = !deeplyNested || m_IsSelected;
            }
            if (m_GraphNode != null) UpdateStatus(m_GraphNode.Status);
            if (Time.unscaledTime >= nextResourceUpdate && m_GraphNode != null)
            {
                nextResourceUpdate = Time.unscaledTime + .2f;
                SetGauge(cpuFill, m_GraphNode.CpuUtilization);
                SetGauge(memoryFill, m_GraphNode.MemoryUsageMB / Mathf.Max(1, m_GraphNode.MemoryLimitMB));
            }
            if (m_CurrentStatus == NodeStatus.Overloaded) m_IsDirty = true;
            if (!m_IsDirty) return;
            m_IsDirty = false;
            ApplyVisualState();
        }

        void CreateResourceIndicators(Bounds bounds)
        {
            resourceGaugeWidth = Mathf.Clamp(bounds.size.x * .9f, .08f, .22f);
            resourceRoot = new GameObject("CPU and RAM indicators (top to bottom)").transform;
            resourceRoot.SetParent(transform, false);
            resourceRoot.localPosition = new Vector3(0, bounds.min.y + .04f, bounds.max.z + .015f);
            Transform Bar(string name, float y, Color color)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.layer = 2;
                Destroy(go.GetComponent<Collider>()); go.transform.SetParent(resourceRoot, false);
                go.transform.localPosition = new Vector3(0, y, 0); go.transform.localScale = new Vector3(resourceGaugeWidth, .009f, .009f);
                var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = m_MeshRenderer.sharedMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
                var block = new MaterialPropertyBlock(); block.SetColor(k_BaseColorId, color); block.SetColor(k_EmissionColorId, color * .15f);
                renderer.SetPropertyBlock(block); return go.transform;
            }
            Bar("CPU track", 0, new Color(.03f, .06f, .1f));
            Bar("RAM track", -.018f, new Color(.03f, .06f, .1f));
            cpuFill = Bar("CPU utilization", 0, new Color(0, .95f, 1));
            memoryFill = Bar("RAM utilization", -.018f, new Color(.65f, .4f, 1));
            cpuFill.localPosition += Vector3.forward * .006f; memoryFill.localPosition += Vector3.forward * .006f;
            SetGauge(cpuFill, 0); SetGauge(memoryFill, 0);
        }
        void SetGauge(Transform bar, float value)
        {
            float amount = Mathf.Clamp01(value);
            bar.gameObject.SetActive(amount > 0);
            bar.localScale = new Vector3(Mathf.Max(.001f, resourceGaugeWidth * amount), .009f, .009f);
            var position = bar.localPosition; position.x = resourceGaugeWidth * (amount - 1) * .5f; bar.localPosition = position;
        }

        void OnDestroy()
        {
            if (m_MeshFilter != null && m_MeshFilter.sharedMesh != null) Destroy(m_MeshFilter.sharedMesh);
            if (m_MeshRenderer != null && m_MeshRenderer.sharedMaterial != null) Destroy(m_MeshRenderer.sharedMaterial);
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

            Color statusColor = m_CurrentStatus == NodeStatus.Healthy
                ? NodeColorPalette.GetAccentColor(m_GraphNode.Type)
                : NodeColorPalette.GetStatusColor(m_CurrentStatus);
            Color emission;
            if (m_CurrentStatus == NodeStatus.Overloaded)
                statusColor *= .7f + .3f * Mathf.Sin(Time.unscaledTime * 2f);
            if (m_IsSelected) statusColor = Color.Lerp(statusColor, NodeColorPalette.Selected, .5f);
            if (m_Label != null)
            {
                string name = m_GraphNode.DisplayName;
                if (!m_IsSelected && name.Length > 18) name = name.Substring(0, 16) + "…";
                if (m_GraphNode.Type == NodeType.LoadBalancer)
                    name += m_GraphNode.RoutingRole == LoadBalancerRole.Database ? "\nDB Router" : "\nApp LB";
                m_Label.text = m_CurrentStatus == NodeStatus.Healthy ? name : $"{name}\n{m_CurrentStatus}";
                m_Label.color = m_IsSelected ? NodeColorPalette.Selected : m_CurrentStatus == NodeStatus.Healthy ? Color.white : statusColor;
            }

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
            m_Label.fontSize = .5f;
            m_Label.textWrappingMode = TextWrappingModes.NoWrap;
            m_Label.raycastTarget = false;
            m_Label.alignment = TextAlignmentOptions.Center;
            m_Label.enableAutoSizing = false;

            // World-space rect size — keep compact.
            m_Label.rectTransform.sizeDelta = new Vector2(1f, 0.3f);

            // Sorting — render on top of nodes.
            m_Label.sortingOrder = 10;
        }
    }
}
