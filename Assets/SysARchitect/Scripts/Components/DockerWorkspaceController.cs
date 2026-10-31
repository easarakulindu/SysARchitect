using System.Collections.Generic;
using UnityEngine;
using SysARchitect.Core;
using SysARchitect.Graph;
using SysARchitect.Simulation;
using SysARchitect.Visualization;

namespace SysARchitect.Components
{
    public class DockerWorkspaceController : MonoBehaviour
    {
        [SerializeField] private ArchitectureGraph graph;
        [SerializeField] private SimulationController simulation;
        private readonly Dictionary<string, DockerHostModel> hosts = new Dictionary<string, DockerHostModel>();
        private void OnEnable() => graph.OnNodeRemoved += Removed;
        private void OnDisable() => graph.OnNodeRemoved -= Removed;
        private void Removed(GraphNode node)
        {
            hosts.Remove(node.NodeId);
            foreach (var model in hosts.Values) model.Forget(node.NodeId);
        }
        public DockerHostModel GetHost(GraphNode host)
        {
            if (host == null || host.Type != NodeType.DockerHost) return null;
            if (!hosts.TryGetValue(host.NodeId, out var model))
            {
                model = new DockerHostModel(simulation.Engine, host.NodeId); hosts.Add(host.NodeId, model);
                var enclosure = host.gameObject.AddComponent<WorkloadEnclosure>(); enclosure.Initialize(host);
            }
            return model;
        }
        public GraphNode CreateContainer(GraphNode host, int imageIndex)
        {
            var model = GetHost(host);
            if (model.Containers.Count >= 8) return null;
            var node = graph.AddNode(NodeType.Container, host.transform.position);
            node.HostNodeId = host.NodeId;
            model.Attach(node.NodeId, DockerHostModel.Images[imageIndex]);
            node.DisplayName = DockerHostModel.Images[imageIndex].Name + " #" + node.NodeId.Replace("Container_", "");
            node.GetComponent<NodeVisual>().UpdateLabel(node.DisplayName);
            node.MemoryLimitMB = 128;
            graph.AddConnection(host, node);
            host.GetComponent<WorkloadEnclosure>().Add(node);
            return node;
        }
    }
}
