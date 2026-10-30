using UnityEngine;
using SysARchitect.Core;

namespace SysARchitect.Graph
{
    /// <summary>
    /// Runtime representation of an infrastructure node in the architecture graph.
    /// Attached to the GameObject that represents this node in the scene.
    /// </summary>
    public class GraphNode : MonoBehaviour
    {
        [SerializeField] private string nodeId;
        [SerializeField] private NodeType type;
        [SerializeField] private string displayName;
        [SerializeField] private NodeStatus status = NodeStatus.Healthy;

        // --- Simulation properties (used by Milestone 2+) ---
        [Header("Resource Model")]
        [SerializeField] private float processingCapacity = 100f;
        [SerializeField] private float cpuUtilization;
        [SerializeField] private float memoryUsageMB;
        [SerializeField] private float memoryLimitMB = 512f;
        [SerializeField] private int queueCapacity = 50;
        [SerializeField] private float processingLatencyMs = 10f;
        [SerializeField] private float timeoutThresholdMs = 5000f;

        /// <summary>Unique identifier for this node within the graph.</summary>
        public string NodeId
        {
            get => nodeId;
            set => nodeId = value;
        }

        /// <summary>Infrastructure component type.</summary>
        public NodeType Type
        {
            get => type;
            set => type = value;
        }

        /// <summary>Human-readable display name shown in the UI and label.</summary>
        public string DisplayName
        {
            get => displayName;
            set => displayName = value;
        }

        /// <summary>Current operational status.</summary>
        public NodeStatus Status
        {
            get => status;
            set => status = value;
        }

        /// <summary>Maximum requests per second this node can handle.</summary>
        public float ProcessingCapacity
        {
            get => processingCapacity;
            set => processingCapacity = value;
        }

        /// <summary>Current CPU utilization as a fraction (0–1).</summary>
        public float CpuUtilization
        {
            get => cpuUtilization;
            set => cpuUtilization = value;
        }

        /// <summary>Current memory usage in MB.</summary>
        public float MemoryUsageMB
        {
            get => memoryUsageMB;
            set => memoryUsageMB = value;
        }

        /// <summary>Maximum memory in MB before potential OOM crash.</summary>
        public float MemoryLimitMB
        {
            get => memoryLimitMB;
            set => memoryLimitMB = value;
        }

        /// <summary>Maximum number of pending requests in the queue.</summary>
        public int QueueCapacity
        {
            get => queueCapacity;
            set => queueCapacity = value;
        }

        /// <summary>Base processing latency per request in milliseconds.</summary>
        public float ProcessingLatencyMs
        {
            get => processingLatencyMs;
            set => processingLatencyMs = value;
        }

        /// <summary>Request timeout threshold in milliseconds.</summary>
        public float TimeoutThresholdMs
        {
            get => timeoutThresholdMs;
            set => timeoutThresholdMs = value;
        }

        /// <summary>
        /// Generate a default display name based on type and id.
        /// </summary>
        public static string DefaultDisplayName(NodeType nodeType, int index)
        {
            return nodeType switch
            {
                NodeType.Frontend => $"Frontend {index}",
                NodeType.Backend => $"Backend {index}",
                NodeType.Database => $"Database {index}",
                NodeType.LoadBalancer => $"Load Balancer {index}",
                NodeType.Cache => $"Cache {index}",
                NodeType.Container => $"Container {index}",
                NodeType.KubernetesNode => $"K8s Node {index}",
                NodeType.KubernetesPod => $"K8s Pod {index}",
                _ => $"Node {index}"
            };
        }
    }
}
