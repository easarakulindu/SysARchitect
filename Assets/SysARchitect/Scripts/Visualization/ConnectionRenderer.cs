namespace SysARchitect.Visualization
{
    using UnityEngine;
    using SysARchitect.Graph;

    /// <summary>
    /// Renders a visual connection line between two graph nodes.
    /// Uses a <see cref="LineRenderer"/> with animated UV offset to
    /// simulate flowing data along the connection.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class ConnectionRenderer : MonoBehaviour
    {
        // ── State ──────────────────────────────────────────────────────
        LineRenderer m_LineRenderer;
        GraphConnection m_Connection;
        Transform m_SourceTransform;
        Transform m_TargetTransform;
        Material m_LineMaterialInstance;

        /// <summary>UV scroll speed (units per second).</summary>
        const float k_ScrollSpeed = 0.5f;

        /// <summary>Line width in world units.</summary>
        const float k_LineWidth = 0.014f;

        static readonly int k_MainTexOffsetId = Shader.PropertyToID("_BaseMap_ST");
        static readonly int k_MainTexId       = Shader.PropertyToID("_BaseMap");

        float m_UVOffset;
        bool selected;
        Color activityColor;
        float activityUntil;
        public bool IsSelected => selected;
        public bool IsLessonFocused { get; private set; }
        public string LastActivity { get; private set; } = "No traffic yet";
        public void SetSelected(bool value) { selected = value; ApplyStyle(); }
        public void SetLessonFocus(bool value) { IsLessonFocused = value; ApplyStyle(); }
        public void ShowActivity(Color color, string description)
        { activityColor = color; activityUntil = Time.unscaledTime + .7f; LastActivity = description; ApplyStyle(); }
        private void ApplyStyle()
        {
            if (m_LineRenderer == null) return;
            m_LineRenderer.widthMultiplier = (selected ? .026f : IsLessonFocused ? .021f : k_LineWidth) * m_SourceTransform.lossyScale.x;
            m_LineMaterialInstance.color = selected ? new Color(1, .85f, .1f) : Time.unscaledTime < activityUntil ? activityColor : IsLessonFocused ? new Color(.3f, 1, .65f) : new Color(.15f, .7f, .95f);
        }

        // ────────────────────────────────────────────────────────────────
        // Public API
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Configures the connection line between <paramref name="source"/>
        /// and <paramref name="target"/>.
        /// </summary>
        /// <param name="conn">The logical graph connection.</param>
        /// <param name="source">Source node transform.</param>
        /// <param name="target">Target node transform.</param>
        /// <param name="lineMaterial">Base material for the line.</param>
        public void Initialize(GraphConnection conn, Transform source, Transform target, Material lineMaterial)
        {
            m_Connection = conn;
            m_SourceTransform = source;
            m_TargetTransform = target;

            m_LineRenderer = GetComponent<LineRenderer>();
            m_LineRenderer.useWorldSpace = true;
            m_LineRenderer.positionCount = 5;
            m_LineRenderer.startWidth = 1;
            m_LineRenderer.endWidth = 1;
            m_LineRenderer.widthMultiplier = k_LineWidth;
            m_LineRenderer.numCapVertices = 2;
            m_LineRenderer.numCornerVertices = 2;
            m_LineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            m_LineRenderer.receiveShadows = false;
            m_LineRenderer.sortingOrder = -1; // render behind nodes

            // Per-instance material so UV animation doesn't affect other lines.
            m_LineMaterialInstance = new Material(lineMaterial);
            m_LineRenderer.material = m_LineMaterialInstance;

            // Set initial positions.
            UpdatePositions();
        }

        /// <summary>Returns the underlying <see cref="GraphConnection"/>.</summary>
        public GraphConnection GetConnection() => m_Connection;

        /// <summary>Shared visible ports for both the line and its request packets.</summary>
        public static void GetEndpoints(Transform source, Transform target, out Vector3 start, out Vector3 end)
        {
            Vector3 direction = (target.position - source.position).normalized;
            start = Port(source, direction);
            end = Port(target, -direction);
            float scale = source.lossyScale.x;
            if (Vector3.Dot(end - start, direction) <= .02f * scale)
            {
                // Overlapping enclosures use ports above the objects instead of an inverted line.
                float height = Mathf.Max(source.GetComponent<Collider>().bounds.max.y, target.GetComponent<Collider>().bounds.max.y) + .08f * scale;
                start = new Vector3(source.position.x, height, source.position.z);
                end = new Vector3(target.position.x, height, target.position.z);
            }
        }

        private static Vector3 Port(Transform node, Vector3 direction)
        {
            Bounds bounds = node.GetComponent<Collider>().bounds;
            float distance = float.PositiveInfinity;
            for (int axis = 0; axis < 3; axis++)
                if (Mathf.Abs(direction[axis]) > .00001f)
                    distance = Mathf.Min(distance, bounds.extents[axis] / Mathf.Abs(direction[axis]));
            if (float.IsInfinity(distance)) distance = 0;
            return bounds.center + direction * (distance + .025f * node.lossyScale.x);
        }

        // ────────────────────────────────────────────────────────────────
        // Unity lifecycle
        // ────────────────────────────────────────────────────────────────

        void LateUpdate()
        {
            if (m_SourceTransform == null || m_TargetTransform == null) return;

            UpdatePositions();
            ApplyStyle();
            AnimateUV();
        }

        void OnDestroy()
        {
            // Clean up the per-instance material.
            if (m_LineMaterialInstance != null)
            {
                Destroy(m_LineMaterialInstance);
            }
        }

        // ────────────────────────────────────────────────────────────────
        // Internal helpers
        // ────────────────────────────────────────────────────────────────

        void UpdatePositions()
        {
            GetEndpoints(m_SourceTransform, m_TargetTransform, out Vector3 start, out Vector3 end);
            m_LineRenderer.SetPosition(0, start);
            m_LineRenderer.SetPosition(1, end);
            Vector3 direction = (end - start).normalized;
            Vector3 side = Vector3.Cross(direction, Vector3.up).normalized;
            float size = Mathf.Min((selected ? .12f : .09f) * m_SourceTransform.lossyScale.x, Vector3.Distance(start, end) * .25f);
            m_LineRenderer.SetPosition(2, end - direction * size + side * size * .5f);
            m_LineRenderer.SetPosition(3, end);
            m_LineRenderer.SetPosition(4, end - direction * size - side * size * .5f);
        }

        void AnimateUV()
        {
            m_UVOffset += k_ScrollSpeed * Time.deltaTime;
            if (m_UVOffset > 1f) m_UVOffset -= 1f;

            // Scroll the main texture offset along U to produce a flow effect.
            m_LineMaterialInstance.mainTextureOffset = new Vector2(m_UVOffset, 0f);
        }
    }
}
