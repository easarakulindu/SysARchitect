using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SysARchitect.Core;
using SysARchitect.Visualization;

namespace SysARchitect.Graph
{
    /// <summary>
    /// Central manager for the architecture graph. Owns all nodes and connections.
    /// Provides methods to add, remove, query, and validate graph elements.
    /// </summary>
    public class ArchitectureGraph : MonoBehaviour
    {
        [SerializeField] private Material defaultNodeMaterial;
        [SerializeField] private Material connectionMaterial;

        private readonly List<GraphNode> nodes = new List<GraphNode>();
        private readonly List<GraphConnection> connections = new List<GraphConnection>();
        private readonly Dictionary<string, int> typeCounters = new Dictionary<string, int>();

        /// <summary>Read-only list of all nodes in the graph.</summary>
        public IReadOnlyList<GraphNode> Nodes => nodes;

        /// <summary>Read-only list of all connections in the graph.</summary>
        public IReadOnlyList<GraphConnection> Connections => connections;

        /// <summary>Fired when a node is added.</summary>
        public event Action<GraphNode> OnNodeAdded;

        /// <summary>Fired when a node is removed.</summary>
        public event Action<GraphNode> OnNodeRemoved;

        /// <summary>Fired when a connection is added.</summary>
        public event Action<GraphConnection> OnConnectionAdded;

        /// <summary>Fired when a connection is removed.</summary>
        public event Action<GraphConnection> OnConnectionRemoved;
        public event Action<GraphNode> OnNodeConfigured;

        /// <summary>
        /// Add a new infrastructure node at the given world position.
        /// </summary>
        /// <param name="type">The infrastructure component type.</param>
        /// <param name="position">World position to place the node.</param>
        /// <returns>The created GraphNode.</returns>
        public GraphNode AddNode(NodeType type, Vector3 position)
        {
            string typeKey = type.ToString();
            if (!typeCounters.ContainsKey(typeKey))
                typeCounters[typeKey] = 0;
            typeCounters[typeKey]++;

            int index = typeCounters[typeKey];
            string nodeId = $"{typeKey}_{index}";
            string displayName = GraphNode.DefaultDisplayName(type, index);

            // Create the GameObject
            var go = new GameObject(displayName);
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            go.layer = gameObject.layer;

            // Add GraphNode component
            var graphNode = go.AddComponent<GraphNode>();
            graphNode.NodeId = nodeId;
            graphNode.Type = type;
            // Default cheap ingress/router/cache services leave room to demonstrate backend scaling.
            graphNode.ProcessingCapacity = type == NodeType.Frontend || type == NodeType.LoadBalancer || type == NodeType.Cache ? 1000
                : type == NodeType.Database ? 500 : 100;
            graphNode.DisplayName = displayName;

            // Add visual representation
            var visual = go.AddComponent<NodeVisual>();
            visual.Initialize(graphNode, defaultNodeMaterial);

            nodes.Add(graphNode);
            OnNodeAdded?.Invoke(graphNode);

            return graphNode;
        }

        /// <summary>
        /// Validate whether a connection between two nodes is allowed.
        /// </summary>
        /// <returns>Tuple of (isValid, errorMessage).</returns>
        public (bool isValid, string errorMessage) ValidateConnection(GraphNode source, GraphNode target)
        {
            if (source == null || target == null)
                return (false, "Source or target node is null.");

            if (source == target)
                return (false, "Cannot connect a node to itself.");

            if (!nodes.Contains(source) || !nodes.Contains(target))
                return (false, "Both nodes must belong to this workspace.");
            if (source.Type == NodeType.DockerHost && target.HostNodeId != source.NodeId)
                return (false, "Create this host's Containers from an image in its Docker panel; a Container belongs to one host.");

            // Check for duplicate connection
            if (connections.Any(c => c.Source == source && c.Target == target))
                return (false, "This connection already exists.");

            if (CanReach(target, source, new HashSet<GraphNode>()))
                return (false, "This connection creates a request-routing cycle.");

            // Check type-level rules
            if (source.Type == NodeType.LoadBalancer && source.RoutingRole == LoadBalancerRole.Database &&
                connections.Any(c => c.Source == source))
                return (false, "Connect a Database Router to one expandable Database cluster; configure its internal replicas or shards.");
            if (source.Type == NodeType.Backend)
            {
                bool storage = target.Type == NodeType.Database || target.Type == NodeType.LoadBalancer;
                if (target.Type == NodeType.Cache && connections.Any(c => c.Source == source && c.Target.Type == NodeType.Cache))
                    return (false, "This cache-aside scenario supports one Cache per Backend.");
                if (storage && connections.Any(c => c.Source == source && (c.Target.Type == NodeType.Database || c.Target.Type == NodeType.LoadBalancer)))
                    return (false, "Choose one Database or Database Router per Backend; a router targets one expandable cluster.");
            }
            return ConnectionRules.Validate(source.Type, target.Type, source.RoutingRole, target.RoutingRole);
        }

        public bool TrySetRoutingRole(GraphNode node, LoadBalancerRole role, out string message)
        {
            if (node == null || !nodes.Contains(node) || node.Type != NodeType.LoadBalancer)
            { message = "Select a Load Balancer to configure its role."; return false; }
            foreach (var edge in connections)
            {
                if (edge.Source != node && edge.Target != node) continue;
                var result = ConnectionRules.Validate(edge.Source.Type, edge.Target.Type,
                    edge.Source == node ? role : edge.Source.RoutingRole,
                    edge.Target == node ? role : edge.Target.RoutingRole);
                if (!result.isValid) { message = "Remove incompatible links before changing role. " + result.errorMessage; return false; }
            }
            if (role == LoadBalancerRole.Database && connections.Count(c => c.Source == node) > 1)
            { message = "A Database Router targets one expandable cluster. Remove extra outgoing links first."; return false; }
            node.RoutingRole = role;
            node.GetComponent<NodeVisual>().UpdateLabel(node.DisplayName);
            OnNodeConfigured?.Invoke(node);
            message = role == LoadBalancerRole.Database ? "Database Router: Backend > Router > Database cluster." : "Application routing: Frontend > Load Balancer > Backends.";
            return true;
        }

        private bool CanReach(GraphNode start, GraphNode destination, HashSet<GraphNode> visited)
        {
            if (start == destination) return true;
            if (!visited.Add(start)) return false;
            foreach (var edge in connections)
                if (edge.Source == start && CanReach(edge.Target, destination, visited)) return true;
            return false;
        }

        /// <summary>
        /// Add a connection between two nodes after validation.
        /// </summary>
        /// <returns>The created connection, or null if validation fails.</returns>
        public GraphConnection AddConnection(GraphNode source, GraphNode target)
        {
            var (isValid, errorMessage) = ValidateConnection(source, target);
            if (!isValid)
            {
                Debug.LogWarning($"[ArchitectureGraph] Connection rejected: {errorMessage}");
                return null;
            }

            // Create connection GameObject
            string connId = $"conn_{source.NodeId}_to_{target.NodeId}";
            var go = new GameObject(connId);
            go.transform.SetParent(transform, false);

            // Add GraphConnection component
            var connection = go.AddComponent<GraphConnection>();
            connection.ConnectionId = connId;
            connection.Source = source;
            connection.Target = target;

            // Add visual renderer
            var renderer = go.AddComponent<ConnectionRenderer>();
            renderer.Initialize(connection, source.transform, target.transform, connectionMaterial);

            connections.Add(connection);
            OnConnectionAdded?.Invoke(connection);

            return connection;
        }

        /// <summary>
        /// Remove a node and all its connections from the graph.
        /// </summary>
        public void RemoveNode(GraphNode node)
        {
            if (node == null || !nodes.Contains(node))
                return;

            // Remove all connections involving this node
            var connectedEdges = connections
                .Where(c => c.Source == node || c.Target == node)
                .ToList();

            foreach (var conn in connectedEdges)
            {
                RemoveConnection(conn);
            }

            foreach (var child in nodes.Where(n => n.HostNodeId == node.NodeId).ToList()) RemoveNode(child);

            nodes.Remove(node);
            OnNodeRemoved?.Invoke(node);

            if (node != null && node.gameObject != null)
                Destroy(node.gameObject);
        }

        /// <summary>
        /// Remove a specific connection from the graph.
        /// </summary>
        public void RemoveConnection(GraphConnection connection)
        {
            if (connection == null || !connections.Contains(connection))
                return;

            connections.Remove(connection);
            OnConnectionRemoved?.Invoke(connection);

            if (connection != null && connection.gameObject != null)
                Destroy(connection.gameObject);
        }

        /// <summary>
        /// Get all connections originating from a specific node.
        /// </summary>
        public List<GraphConnection> GetOutgoingConnections(GraphNode node)
        {
            return connections.Where(c => c.Source == node).ToList();
        }

        /// <summary>
        /// Get all connections targeting a specific node.
        /// </summary>
        public List<GraphConnection> GetIncomingConnections(GraphNode node)
        {
            return connections.Where(c => c.Target == node).ToList();
        }

        /// <summary>
        /// Get all nodes of a specific type.
        /// </summary>
        public List<GraphNode> GetNodesByType(NodeType type)
        {
            return nodes.Where(n => n.Type == type).ToList();
        }

        /// <summary>
        /// Find a node by its unique ID.
        /// </summary>
        public GraphNode FindNode(string nodeId)
        {
            return nodes.FirstOrDefault(n => n.NodeId == nodeId);
        }

        /// <summary>
        /// Clear the entire graph, destroying all nodes and connections.
        /// </summary>
        public void ClearGraph()
        {
            // Destroy connections first
            for (int i = connections.Count - 1; i >= 0; i--)
            {
                var conn = connections[i];
                connections.RemoveAt(i);
                OnConnectionRemoved?.Invoke(conn);
                if (conn != null && conn.gameObject != null)
                    Destroy(conn.gameObject);
            }

            // Then destroy nodes
            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                var node = nodes[i];
                nodes.RemoveAt(i);
                OnNodeRemoved?.Invoke(node);
                if (node != null && node.gameObject != null)
                    Destroy(node.gameObject);
            }

            typeCounters.Clear();
        }

        /// <summary>
        /// Validate the entire graph and return a list of warnings.
        /// </summary>
        public List<string> ValidateGraph()
        {
            var warnings = new List<string>();

            // Check for disconnected nodes
            foreach (var node in nodes)
            {
                bool hasIncoming = connections.Any(c => c.Target == node);
                bool hasOutgoing = connections.Any(c => c.Source == node);

                if (!hasIncoming && !hasOutgoing)
                {
                    warnings.Add($"{node.DisplayName} is not connected to any other component.");
                }
            }

            // Check for missing frontends
            if (!nodes.Any(n => n.Type == NodeType.Frontend))
            {
                warnings.Add("No Frontend found. Users need a Frontend to send requests.");
            }

            // Check for backends without databases
            var backends = nodes.Where(n => n.Type == NodeType.Backend).ToList();
            foreach (var backend in backends)
            {
                bool connectsToDb = connections.Any(c =>
                    c.Source == backend && (c.Target.Type == NodeType.Database || (c.Target.Type == NodeType.LoadBalancer && c.Target.RoutingRole == LoadBalancerRole.Database)));
                bool connectsToCache = connections.Any(c =>
                    c.Source == backend && c.Target.Type == NodeType.Cache);
                if (connectsToCache && !connectsToDb)
                    warnings.Add($"{backend.DisplayName}: cache-aside needs a Database or Database Router fallback as well as Cache.");
                if (!connectsToDb && !connectsToCache)
                {
                    warnings.Add($"{backend.DisplayName} is not connected to any Database or Cache.");
                }
            }

            // Check for load balancer with no targets
            var loadBalancers = nodes.Where(n => n.Type == NodeType.LoadBalancer).ToList();
            foreach (var lb in loadBalancers)
            {
                var targets = connections.Where(c => c.Source == lb).ToList();
                if (targets.Count == 0)
                {
                    warnings.Add($"{lb.DisplayName} has no backend targets. Connect it to the targets supported by its configured role.");
                }
                else if (targets.Count == 1 && lb.RoutingRole == LoadBalancerRole.Application)
                {
                    warnings.Add($"{lb.DisplayName} has only one target. Add more backends for load distribution.");
                }
            }
            foreach (var host in nodes.Where(n => n.Type == NodeType.DockerHost))
                if (!connections.Any(c => c.Source == host))
                    warnings.Add($"{host.DisplayName} has no Container route. Select the host and create a Container from an image in Docker Explorer.");

            return warnings;
        }
    }
}
