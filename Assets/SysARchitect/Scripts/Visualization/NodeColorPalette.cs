namespace SysARchitect.Visualization
{
    using UnityEngine;
    using SysARchitect.Core;

    /// <summary>
    /// Centralized color palette for the holographic visual style.
    /// Provides consistent colors for node types, status indicators, and selection.
    /// </summary>
    public static class NodeColorPalette
    {
        // ── Status colors ──────────────────────────────────────────────

        /// <summary>Cyan glow for healthy nodes.</summary>
        public static readonly Color Healthy = new Color(0f, 0.9f, 0.9f, 1f);

        /// <summary>Orange warning indicator.</summary>
        public static readonly Color Warning = new Color(1f, 0.7f, 0f, 1f);

        /// <summary>Red for overloaded nodes.</summary>
        public static readonly Color Overloaded = new Color(1f, 0.15f, 0.15f, 1f);

        /// <summary>Dark grey for crashed nodes.</summary>
        public static readonly Color Crashed = new Color(0.2f, 0.2f, 0.2f, 1f);

        /// <summary>Slightly lighter grey for offline nodes.</summary>
        public static readonly Color Offline = new Color(0.3f, 0.3f, 0.35f, 1f);

        // ── Node type accent colors ────────────────────────────────────

        /// <summary>Light blue for frontend nodes.</summary>
        public static readonly Color Frontend = new Color(0.2f, 0.7f, 1f, 1f);

        /// <summary>Teal/green for backend nodes.</summary>
        public static readonly Color Backend = new Color(0.1f, 0.9f, 0.6f, 1f);

        /// <summary>Purple for database nodes.</summary>
        public static readonly Color Database = new Color(0.6f, 0.3f, 1f, 1f);

        /// <summary>Cyan for load balancer nodes.</summary>
        public static readonly Color LoadBalancer = new Color(0f, 0.8f, 0.8f, 1f);

        /// <summary>Gold for cache nodes.</summary>
        public static readonly Color Cache = new Color(1f, 0.85f, 0.1f, 1f);

        /// <summary>Docker blue for container nodes.</summary>
        public static readonly Color Container = new Color(0.1f, 0.6f, 0.9f, 1f);

        /// <summary>K8s blue for Kubernetes node.</summary>
        public static readonly Color KubernetesNode = new Color(0.2f, 0.5f, 0.9f, 1f);

        /// <summary>Lighter K8s blue for Kubernetes pod.</summary>
        public static readonly Color KubernetesPod = new Color(0.3f, 0.6f, 1f, 1f);

        // ── Selection ──────────────────────────────────────────────────

        /// <summary>Bright yellow selection highlight.</summary>
        public static readonly Color Selected = new Color(1f, 1f, 0.5f, 1f);

        /// <summary>
        /// Returns the accent color associated with the given <see cref="NodeType"/>.
        /// </summary>
        public static Color GetAccentColor(NodeType type)
        {
            switch (type)
            {
                case NodeType.Frontend:       return Frontend;
                case NodeType.Backend:        return Backend;
                case NodeType.Database:       return Database;
                case NodeType.LoadBalancer:   return LoadBalancer;
                case NodeType.Cache:          return Cache;
                case NodeType.Container:      return Container;
                case NodeType.KubernetesNode: return KubernetesNode;
                case NodeType.KubernetesPod:  return KubernetesPod;
                default:                      return Color.white;
            }
        }

        /// <summary>
        /// Returns the color associated with the given <see cref="NodeStatus"/>.
        /// </summary>
        public static Color GetStatusColor(NodeStatus status)
        {
            switch (status)
            {
                case NodeStatus.Healthy:    return Healthy;
                case NodeStatus.Warning:    return Warning;
                case NodeStatus.Overloaded: return Overloaded;
                case NodeStatus.Crashed:    return Crashed;
                case NodeStatus.Offline:    return Offline;
                case NodeStatus.Recovering: return new Color(.5f, .55f, 1f);
                default:                    return Color.white;
            }
        }
    }
}
