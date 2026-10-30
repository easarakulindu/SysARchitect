using SysARchitect.Core;

namespace SysARchitect.Graph
{
    /// <summary>
    /// Defines and validates which connections are allowed between node types.
    /// Provides clear error messages for invalid connections.
    /// </summary>
    public static class ConnectionRules
    {
        /// <summary>
        /// Validate whether a connection from source to target is allowed.
        /// </summary>
        /// <param name="sourceType">Type of the source node.</param>
        /// <param name="targetType">Type of the target node.</param>
        /// <returns>A tuple of (isValid, errorMessage). errorMessage is null when valid.</returns>
        public static (bool isValid, string errorMessage) Validate(NodeType sourceType, NodeType targetType)
        {
            // Self-connection is checked at a higher level (same instance check).
            // Here we check type-level rules.

            // Frontend can connect to: Backend, LoadBalancer, Cache
            // Backend can connect to: Database, Cache, Backend (microservice), Container, KubernetesPod
            // LoadBalancer can connect to: Backend, Container, KubernetesPod
            // Database can connect to: (nothing outgoing in basic model — it responds)
            // Cache can connect to: Database, Backend
            // Container can connect to: Database, Cache, Backend, Container
            // KubernetesNode can connect to: KubernetesPod
            // KubernetesPod can connect to: Database, Cache, Backend, KubernetesPod

            switch (sourceType)
            {
                case NodeType.Frontend:
                    if (targetType == NodeType.Backend ||
                        targetType == NodeType.LoadBalancer ||
                        targetType == NodeType.Cache)
                        return (true, null);
                    if (targetType == NodeType.Frontend)
                        return (false, "A Frontend cannot connect to another Frontend. Connect it to a Backend or Load Balancer.");
                    if (targetType == NodeType.Database)
                        return (false, "Frontends should not connect directly to databases. Route requests through a Backend server.");
                    return (false, $"Frontend cannot connect to {FormatTypeName(targetType)}. Try connecting to a Backend or Load Balancer.");

                case NodeType.Backend:
                    if (targetType == NodeType.Database ||
                        targetType == NodeType.Cache ||
                        targetType == NodeType.Backend ||
                        targetType == NodeType.Container ||
                        targetType == NodeType.KubernetesPod)
                        return (true, null);
                    if (targetType == NodeType.Frontend)
                        return (false, "Backend should not connect back to Frontend. The Frontend already sends requests to the Backend.");
                    if (targetType == NodeType.LoadBalancer)
                        return (false, "Backend typically does not connect to a Load Balancer. The Load Balancer distributes requests to Backends.");
                    return (false, $"Backend cannot connect to {FormatTypeName(targetType)}.");

                case NodeType.LoadBalancer:
                    if (targetType == NodeType.Backend ||
                        targetType == NodeType.Container ||
                        targetType == NodeType.KubernetesPod)
                        return (true, null);
                    if (targetType == NodeType.Frontend)
                        return (false, "Load Balancer distributes requests to backend services, not to frontends.");
                    if (targetType == NodeType.Database)
                        return (false, "Load Balancer should distribute to application servers, not directly to databases.");
                    if (targetType == NodeType.LoadBalancer)
                        return (false, "Chaining Load Balancers is not supported in this simulation.");
                    return (false, $"Load Balancer cannot connect to {FormatTypeName(targetType)}. Connect it to Backend servers.");

                case NodeType.Database:
                    return (false, "Databases receive requests but do not initiate outgoing connections in this model. Connect a Backend to the Database instead.");

                case NodeType.Cache:
                    if (targetType == NodeType.Database ||
                        targetType == NodeType.Backend)
                        return (true, null);
                    return (false, $"Cache cannot connect to {FormatTypeName(targetType)}. A Cache can connect to a Database (cache-aside) or a Backend.");

                case NodeType.Container:
                    if (targetType == NodeType.Database ||
                        targetType == NodeType.Cache ||
                        targetType == NodeType.Backend ||
                        targetType == NodeType.Container)
                        return (true, null);
                    if (targetType == NodeType.Frontend)
                        return (false, "Containers serve backend workloads. The Frontend connects to them, not the other way around.");
                    return (false, $"Container cannot connect to {FormatTypeName(targetType)}.");

                case NodeType.KubernetesNode:
                    if (targetType == NodeType.KubernetesPod)
                        return (true, null);
                    return (false, $"A Kubernetes Worker Node hosts Pods. It can only connect to Kubernetes Pods.");

                case NodeType.KubernetesPod:
                    if (targetType == NodeType.Database ||
                        targetType == NodeType.Cache ||
                        targetType == NodeType.Backend ||
                        targetType == NodeType.KubernetesPod)
                        return (true, null);
                    if (targetType == NodeType.KubernetesNode)
                        return (false, "Pods run on Nodes — the Node connects to the Pod, not the reverse.");
                    return (false, $"Kubernetes Pod cannot connect to {FormatTypeName(targetType)}.");

                default:
                    return (false, "Unknown node type.");
            }
        }

        private static string FormatTypeName(NodeType type)
        {
            return type switch
            {
                NodeType.LoadBalancer => "Load Balancer",
                NodeType.KubernetesNode => "Kubernetes Node",
                NodeType.KubernetesPod => "Kubernetes Pod",
                _ => type.ToString()
            };
        }
    }
}
