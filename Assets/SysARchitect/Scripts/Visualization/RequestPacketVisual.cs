namespace SysARchitect.Visualization
{
    using UnityEngine;

    /// <summary>
    /// Visual representation of a request packet that travels between nodes.
    /// A small glowing sphere lerps from source to target, then invokes a
    /// completion callback and destroys itself.
    /// </summary>
    public class RequestPacketVisual : MonoBehaviour
    {
        // ── Configuration ──────────────────────────────────────────────
        Transform m_Source;
        Transform m_Target;
        float m_Speed = 1.5f;
        float m_Progress;
        System.Action m_OnComplete;
        bool m_IsActive;
        MeshRenderer m_MeshRenderer;

        /// <summary>Small sphere radius used by the factory.</summary>
        const float k_Radius = 0.02f;

        // ── Preset colours ─────────────────────────────────────────────
        /// <summary>Green — request succeeded.</summary>
        public static readonly Color Success = new Color(0.1f, 1f, 0.3f, 1f);
        /// <summary>Red — request failed.</summary>
        public static readonly Color Failure = new Color(1f, 0.15f, 0.15f, 1f);
        /// <summary>Cyan — request in flight.</summary>
        public static readonly Color Pending = new Color(0f, 0.9f, 0.95f, 1f);

        static readonly int k_BaseColorId    = Shader.PropertyToID("_BaseColor");
        static readonly int k_EmissionColorId = Shader.PropertyToID("_EmissionColor");

        // ────────────────────────────────────────────────────────────────
        // Public API
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Starts the packet moving from <paramref name="from"/> to
        /// <paramref name="to"/>. <paramref name="onArrival"/> fires once
        /// the packet reaches the target, after which the GameObject is
        /// destroyed.
        /// </summary>
        /// <param name="from">Source node transform.</param>
        /// <param name="to">Target node transform.</param>
        /// <param name="color">Packet glow colour.</param>
        /// <param name="travelSpeed">World-units per second.</param>
        /// <param name="onArrival">Optional callback on arrival.</param>
        public void Launch(Transform from, Transform to, Color color, float travelSpeed, System.Action onArrival)
        {
            m_Source = from;
            m_Target = to;
            m_Speed = Mathf.Max(0.01f, travelSpeed);
            m_OnComplete = onArrival;
            m_Progress = 0f;
            m_IsActive = true;

            transform.position = from.position;

            // Tint the packet.
            if (m_MeshRenderer != null)
            {
                var block = new MaterialPropertyBlock();
                m_MeshRenderer.GetPropertyBlock(block);
                block.SetColor(k_BaseColorId, color);
                block.SetColor(k_EmissionColorId, color * 2f);
                m_MeshRenderer.SetPropertyBlock(block);
            }
        }

        /// <summary>
        /// Factory method — creates a small sphere with the given material
        /// and returns the attached <see cref="RequestPacketVisual"/>.
        /// </summary>
        /// <param name="packetMaterial">Material applied to the sphere.</param>
        /// <returns>The newly created component, ready for <see cref="Launch"/>.</returns>
        public static RequestPacketVisual Create(Material packetMaterial)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "RequestPacket";
            go.transform.localScale = Vector3.one * (k_Radius * 2f);

            // Remove the default collider — packets are visual only.
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.material = new Material(packetMaterial);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var packet = go.AddComponent<RequestPacketVisual>();
            packet.m_MeshRenderer = renderer;
            return packet;
        }

        // ────────────────────────────────────────────────────────────────
        // Unity lifecycle
        // ────────────────────────────────────────────────────────────────

        void Update()
        {
            if (!m_IsActive) return;
            if (m_Source == null || m_Target == null)
            {
                // Source or target was destroyed mid-flight — clean up.
                m_IsActive = false;
                Destroy(gameObject);
                return;
            }

            // Compute normalised progress based on distance.
            float distance = Vector3.Distance(m_Source.position, m_Target.position);
            if (distance < 0.0001f)
            {
                m_Progress = 1f;
            }
            else
            {
                m_Progress += (m_Speed / distance) * Time.deltaTime;
            }

            m_Progress = Mathf.Clamp01(m_Progress);
            transform.position = Vector3.Lerp(m_Source.position, m_Target.position, m_Progress);

            if (m_Progress >= 1f)
            {
                m_IsActive = false;
                m_OnComplete?.Invoke();
                Destroy(gameObject);
            }
        }

        void OnDestroy()
        {
            // Clean up the per-instance material.
            if (m_MeshRenderer != null && m_MeshRenderer.material != null)
            {
                Destroy(m_MeshRenderer.material);
            }
        }
    }
}
