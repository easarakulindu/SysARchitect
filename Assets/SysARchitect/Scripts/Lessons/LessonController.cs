using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SysARchitect.Components;
using SysARchitect.Core;
using SysARchitect.Graph;
using SysARchitect.Interaction;
using SysARchitect.Simulation;

namespace SysARchitect.Lessons
{
    /// <summary>Guidance and measured challenges over the existing builder, never a parallel simulator.</summary>
    public class LessonController : MonoBehaviour
    {
        [SerializeField] private ArchitectureGraph graph;
        [SerializeField] private SimulationController simulation;
        [SerializeField] private InteractionManager interactions;
        [SerializeField] private DockerWorkspaceController docker;
        [SerializeField] private KubernetesWorkspaceController kubernetes;
        private readonly Dictionary<string, (float cpu, float memory)> limits = new Dictionary<string, (float, float)>();
        private readonly Dictionary<string, long> completedBaseline = new Dictionary<string, long>();
        private long successBaseline, crashCompleted;
        private string observedDatabase, stoppedContainer, crashedContainer;
        private bool replicationObserved, lagObserved, failoverObserved, startedObserved, recoveredObserved, limitsObserved;
        public LessonDefinition Current { get; private set; } = LessonCatalog.All[0];
        public LearningProgress Progress { get; private set; }
        public int ActiveAlgorithmLine { get; private set; }
        public string Feedback { get; private set; } = "Choose a lesson. Load example explicitly replaces the current workspace; otherwise use your existing architecture.";
        private void Awake() => Progress = new LearningProgress(new PlayerPrefsProgressStorage());
        public void UseProgress(IProgressStorage storage) => Progress = new LearningProgress(storage);
        private void OnEnable()
        {
            graph.OnNodeAdded += NodeAdded; graph.OnNodeRemoved += NodeRemoved;
            simulation.Engine.OnTransferPath += Transferred; simulation.Engine.OnRequestFinished += Finished;
            simulation.OnBeforeTick += BeforeTick;
            simulation.OnSimulationReset += ResetSession;
        }
        private void OnDisable()
        {
            graph.OnNodeAdded -= NodeAdded; graph.OnNodeRemoved -= NodeRemoved;
            simulation.Engine.OnTransferPath -= Transferred; simulation.Engine.OnRequestFinished -= Finished;
            simulation.OnBeforeTick -= BeforeTick;
            simulation.OnSimulationReset -= ResetSession;
        }
        private void LateUpdate() => ObserveState();
        private void BeforeTick(double _) => ObserveState();
        private void NodeAdded(GraphNode node)
        { if (Current.Id == LessonId.Docker && node.Type == NodeType.Container) ActiveAlgorithmLine = 1; }
        private void NodeRemoved(GraphNode node)
        {
            limits.Remove(node.NodeId);
            if (node.NodeId == observedDatabase) { observedDatabase = null; replicationObserved = lagObserved = false; }
            if (node.NodeId == stoppedContainer) { stoppedContainer = null; startedObserved = false; }
            if (node.NodeId == crashedContainer) { crashedContainer = null; recoveredObserved = false; }
        }
        public void SelectLesson(LessonId id)
        {
            Current = LessonCatalog.Get(id); ResetObservations();
            CaptureBaseline();
            Feedback = "Read the objectives and steps. Load example replaces the workspace; Check challenge evaluates real simulation state.";
        }
        private void ResetObservations()
        {
            limits.Clear(); observedDatabase = stoppedContainer = crashedContainer = null;
            replicationObserved = lagObserved = failoverObserved = startedObserved = recoveredObserved = limitsObserved = false;
            ActiveAlgorithmLine = 0;
        }
        private void CaptureBaseline()
        {
            successBaseline = simulation.Engine.Succeeded; completedBaseline.Clear();
            foreach (var node in simulation.Engine.Nodes) completedBaseline[node.Id] = node.Completed;
        }
        private void ResetSession() { ResetObservations(); CaptureBaseline(); }
        public void LoadExample()
        {
            if (!graph.isActiveAndEnabled) { Feedback = "Place the AR workspace on a tracked surface before loading an example."; return; }
            simulation.Pause(); interactions.OnClearButtonPressed(); simulation.ResetSimulation(); ResetObservations(); CaptureBaseline();
            simulation.Engine.RequestsPerSecond = 30;
            switch (Current.Id)
            {
                case LessonId.ClientServer:
                    Add(NodeType.Frontend, .8f, .15f); Add(NodeType.Backend, 0, .2f); Add(NodeType.Database, -.8f, .125f); break;
                case LessonId.LoadBalancing:
                {
                    var f = Add(NodeType.Frontend, .9f, .15f); var lb = Add(NodeType.LoadBalancer, .45f, .05f);
                    var b = Add(NodeType.Backend, -.05f, .2f); var db = Add(NodeType.Database, -.75f, .125f); b.ProcessingCapacity = 40;
                    graph.AddConnection(f, lb); graph.AddConnection(lb, b); graph.AddConnection(b, db);
                    simulation.Engine.RequestsPerSecond = 80; break;
                }
                case LessonId.Database:
                {
                    var f = Add(NodeType.Frontend, .9f, .15f); var b = Add(NodeType.Backend, .35f, .2f);
                    var router = Add(NodeType.LoadBalancer, -.2f, .05f); var db = Add(NodeType.Database, -.8f, .125f);
                    graph.TrySetRoutingRole(router, LoadBalancerRole.Database, out _);
                    graph.AddConnection(f, b); graph.AddConnection(b, router); graph.AddConnection(router, db); break;
                }
                case LessonId.Docker:
                {
                    var f = Add(NodeType.Frontend, .9f, .15f); var host = Add(NodeType.DockerHost, -.1f, .2f);
                    host.ProcessingCapacity = 200; graph.AddConnection(f, host); docker.GetHost(host);
                    simulation.Engine.RequestsPerSecond = 60; break;
                }
                case LessonId.Kubernetes:
                {
                    var f = Add(NodeType.Frontend, 1, .15f); var cluster = Add(NodeType.KubernetesCluster, -.15f, .2f);
                    graph.AddConnection(f, cluster); kubernetes.AddWorker(cluster); kubernetes.AddWorker(cluster);
                    simulation.Engine.RequestsPerSecond = 60; break;
                }
            }
            Feedback = Current.Steps[0];
        }
        private GraphNode Add(NodeType type, float x, float height) => graph.AddNode(type, graph.transform.TransformPoint(new Vector3(x, height, 0)));
        public void ObserveState()
        {
            if (Current.Id == LessonId.Database)
            {
                foreach (var node in simulation.Engine.Nodes)
                {
                    var db = node.Database; if (db == null) continue;
                    if (observedDatabase == null && db.Mode == DatabaseMode.Replication && db.Members.Count >= 3 && db.Writes > 0 && db.Members.Skip(1).Any(m => m.Reads > 0))
                    { observedDatabase = node.Id; replicationObserved = true; }
                    if (node.Id == observedDatabase && db.Mode == DatabaseMode.Replication && db.PendingUpdates > 0) { lagObserved = true; ActiveAlgorithmLine = 3; }
                }
            }
            if (Current.Id == LessonId.Docker)
            {
                foreach (var node in graph.Nodes)
                {
                    if (node.Type != NodeType.Container || node.HostNodeId == null) continue;
                    var state = simulation.Engine.FindNode(node.NodeId); if (state == null) continue;
                    if (limits.TryGetValue(node.NodeId, out var prior) && (prior.cpu != node.ProcessingCapacity || prior.memory != node.MemoryLimitMB))
                    { limitsObserved = true; ActiveAlgorithmLine = 2; }
                    limits[node.NodeId] = (node.ProcessingCapacity, node.MemoryLimitMB);
                    if (state.Status == NodeStatus.Offline) { stoppedContainer = node.NodeId; ActiveAlgorithmLine = 3; }
                    if (state.Status == NodeStatus.Crashed && crashedContainer != node.NodeId) { crashedContainer = node.NodeId; crashCompleted = state.Completed; ActiveAlgorithmLine = 3; }
                    if (Available(state) && node.NodeId == stoppedContainer) startedObserved = true;
                    if (Available(state) && node.NodeId == crashedContainer && state.Completed > crashCompleted) recoveredObserved = true;
                }
            }
            if (Current.Id == LessonId.Kubernetes)
            {
                var cluster = graph.Nodes.FirstOrDefault(n => n.Type == NodeType.KubernetesCluster);
                if (cluster != null) ActiveAlgorithmLine = kubernetes.GetCluster(cluster).ActiveAlgorithmLine;
            }
        }
        private void Transferred(long request, string sourceId, string targetId, RequestPathKind path)
        {
            var source = simulation.Engine.FindNode(sourceId); var target = simulation.Engine.FindNode(targetId);
            if (source == null || target == null) return;
            switch (Current.Id)
            {
                case LessonId.ClientServer: ActiveAlgorithmLine = target.Type == NodeType.Database ? 2 : 1; break;
                case LessonId.LoadBalancing:
                    ActiveAlgorithmLine = source.Type == NodeType.LoadBalancer ? 2 : source.Type == NodeType.Frontend ? 1 : 3;
                    if (source.Type == NodeType.LoadBalancer && target.Type == NodeType.Backend && Available(target) &&
                        graph.GetOutgoingConnections(graph.FindNode(sourceId)).Any(e => simulation.Engine.FindNode(e.Target.NodeId).Status == NodeStatus.Crashed)) failoverObserved = true;
                    break;
                case LessonId.Database:
                    ActiveAlgorithmLine = target.Database == null ? 0 : target.Database.Mode == DatabaseMode.Sharding ? 4 : path == RequestPathKind.DatabaseWrite ? 1 : 2;
                    break;
                case LessonId.Docker: ActiveAlgorithmLine = source.Type == NodeType.DockerHost ? 4 : 0; break;
            }
        }
        private void Finished(long request, string nodeId, bool success)
        {
            if (Current.Id == LessonId.ClientServer) ActiveAlgorithmLine = success ? 3 : 4;
            if (Current.Id == LessonId.LoadBalancing) ActiveAlgorithmLine = 4;
            if (Current.Id == LessonId.Database) ActiveAlgorithmLine = 5;
            if (Current.Id == LessonId.Docker) ActiveAlgorithmLine = 5;
        }
        public bool CheckChallenge()
        {
            ObserveState(); string reason = Evaluate();
            if (reason != null) { Feedback = reason; return false; }
            Progress.Complete(Current.Id); Feedback = "Challenge complete: verified from your architecture and simulation state. Saved locally."; return true;
        }
        private string Evaluate()
        {
            if (Current.Id == LessonId.ClientServer)
            {
                bool path = graph.Connections.Any(f => f.Source.Type == NodeType.Frontend && f.Target.Type == NodeType.Backend && graph.GetOutgoingConnections(f.Target).Any(e => e.Target.Type == NodeType.Database));
                if (!path) return "Connect Frontend > Backend > Database using real directed edges.";
                if (simulation.Engine.Succeeded - successBaseline < 20 || !simulation.Engine.Nodes.Any(n => n.Database != null && n.Database.Reads > 0 && n.Database.Writes > 0)) return "Run until 20 requests succeed and Database shows both reads and writes.";
            }
            if (Current.Id == LessonId.LoadBalancing)
            {
                var lb = graph.Nodes.FirstOrDefault(n => n.Type == NodeType.LoadBalancer && n.RoutingRole == LoadBalancerRole.Application);
                var targets = lb == null ? new List<GraphConnection>() : graph.GetOutgoingConnections(lb).Where(e => e.Target.Type == NodeType.Backend).ToList();
                if (targets.Count < 2) return "Add a second Backend and connect the application Load Balancer to both.";
                if (targets.Any(e => !graph.GetOutgoingConnections(e.Target).Any(edge => edge.Target.Type == NodeType.Database))) return "Connect both Backends to Database.";
                if (!failoverObserved) return "Inject failure into one backend while running; observe the balancer send to the healthy backend, then restart the failed one.";
                if (targets.Any(e => !Available(simulation.Engine.FindNode(e.Target.NodeId)) || CompletedSinceStart(e.Target.NodeId) < 20) || simulation.Engine.Succeeded - successBaseline < 100)
                    return "Recover both backends, let each process 20 requests, and reach 100 end-to-end successes. Remaining capacity can still bottleneck during failover.";
            }
            if (Current.Id == LessonId.Database)
            {
                if (!replicationObserved || !lagObserved) return "On one Database, use at least two replicas and Run until it shows primary writes, replica reads, and pending copies.";
                var db = simulation.Engine.FindNode(observedDatabase)?.Database;
                if (db == null || db.Mode != DatabaseMode.Sharding || db.Members.Count < 3 || db.Members.Count(m => m.Records.Count > 0) < 2)
                    return "Switch that same Database to at least three shards and Run until at least two partitions hold records.";
            }
            if (Current.Id == LessonId.Docker)
            {
                bool instances = graph.Nodes.Where(n => n.Type == NodeType.DockerHost).Any(h => docker.GetHost(h).Containers.GroupBy(c => c.Image).Any(g => g.Count(c => Available(simulation.Engine.FindNode(c.NodeId))) >= 2));
                if (!instances) return "Create two available instances of the same image on one Docker Host.";
                if (!limitsObserved) return "Change a container CPU quota or memory limit after creating it.";
                if (!startedObserved) return "Stop one instance, then Start it and Run/Step through recovery.";
                if (!recoveredObserved) return "Crash one instance, Restart that same instance, and let it process requests again.";
                if (simulation.Engine.Succeeded - successBaseline < 20) return "Run frontend traffic through the host until 20 requests succeed.";
            }
            if (Current.Id == LessonId.Kubernetes)
            {
                var cluster = graph.Nodes.FirstOrDefault(n => n.Type == NodeType.KubernetesCluster); var model = cluster == null ? null : kubernetes.GetCluster(cluster);
                if (model == null || model.DesiredReplicas != 3 || model.ScaleChanges == 0) return "Set a Deployment's explicit desired replica count to three.";
                if (model.Replacements < 1) return "Fail a Pod and Run/Step until the controller creates a replacement with a new identity.";
                if (model.ReadyReplicas != 3 || simulation.Engine.Succeeded - successBaseline < 20) return "Wait for three ready replicas and 20 successful real requests.";
            }
            return null;
        }
        private long CompletedSinceStart(string id) => simulation.Engine.FindNode(id).Completed - (completedBaseline.TryGetValue(id, out long count) ? count : 0);
        private static bool Available(SimulationNode node) => node != null && node.Status != NodeStatus.Crashed && node.Status != NodeStatus.Offline && node.Status != NodeStatus.Recovering;
    }
}
