using System.Collections.Generic;
using UnityEngine;
using SysARchitect.Core;
using SysARchitect.Graph;
using SysARchitect.Simulation;
using SysARchitect.Visualization;

namespace SysARchitect.Components
{
    public class KubernetesWorkspaceController : MonoBehaviour
    {
        [SerializeField] private ArchitectureGraph graph;
        [SerializeField] private SimulationController simulation;
        private readonly Dictionary<string, DeploymentModel> clusters = new Dictionary<string, DeploymentModel>();
        private void OnEnable() { graph.OnNodeRemoved += Removed; simulation.OnBeforeTick += Reconcile; simulation.OnSimulationReset += ResetModels; }
        private void OnDisable() { graph.OnNodeRemoved -= Removed; simulation.OnBeforeTick -= Reconcile; simulation.OnSimulationReset -= ResetModels; }
        private void ResetModels() { foreach (var model in clusters.Values) model.ResetObservations(); }
        private void Removed(GraphNode node)
        {
            clusters.Remove(node.NodeId);
            foreach (var model in clusters.Values) model.ForgetWorker(node.NodeId);
        }
        private void Reconcile(double time) { foreach (var model in clusters.Values) model.Reconcile(time); }
        public DeploymentModel GetCluster(GraphNode cluster)
        {
            if (cluster == null || cluster.Type != NodeType.KubernetesCluster) return null;
            if (!clusters.TryGetValue(cluster.NodeId, out var model))
            {
                model = new DeploymentModel(simulation.Engine, cluster.NodeId,
                    (worker, generation) => CreatePod(worker, generation), id => graph.RemoveNode(graph.FindNode(id)));
                clusters.Add(cluster.NodeId, model);
                cluster.gameObject.AddComponent<WorkloadEnclosure>().Initialize(cluster);
            }
            return model;
        }
        public GraphNode AddWorker(GraphNode cluster)
        {
            var model = GetCluster(cluster); if (model.Workers.Count >= 4) return null;
            var worker = graph.AddNode(NodeType.KubernetesNode, cluster.transform.position);
            worker.HostNodeId = cluster.NodeId; worker.ProcessingCapacity = 200;
            model.AddWorker(worker.NodeId);
            graph.AddConnection(cluster, worker);
            cluster.GetComponent<WorkloadEnclosure>().Add(worker);
            worker.gameObject.AddComponent<WorkloadEnclosure>().Initialize(worker);
            return worker;
        }
        private string CreatePod(string workerId, int generation)
        {
            var worker = graph.FindNode(workerId);
            var pod = graph.AddNode(NodeType.KubernetesPod, worker.transform.position);
            pod.HostNodeId = workerId; pod.ProcessingCapacity = 60; pod.MemoryLimitMB = 128;
            pod.DisplayName = "api Pod " + generation;
            pod.GetComponent<NodeVisual>().UpdateLabel(pod.DisplayName);
            simulation.Engine.FindNode(pod.NodeId).HostingNodeId = workerId;
            graph.AddConnection(worker, pod); worker.GetComponent<WorkloadEnclosure>().Add(pod);
            return pod.NodeId;
        }
    }
}
