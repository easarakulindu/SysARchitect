using System;
using System.Collections.Generic;
using SysARchitect.Core;
using SysARchitect.Simulation;

namespace SysARchitect.Components
{
    public sealed class DeploymentPod
    {
        public string NodeId { get; }
        public string WorkerId { get; }
        public int Generation { get; }
        public DeploymentPod(string id, string worker, int generation) { NodeId = id; WorkerId = worker; Generation = generation; }
    }
    /// <summary>Explicit desired-state reconciliation; no HPA or animation-driven scaling.</summary>
    public sealed class DeploymentModel
    {
        private readonly SimulationEngine engine;
        private readonly Func<string, int, string> createPod;
        private readonly Action<string> removePod;
        private readonly List<string> workers = new List<string>();
        private readonly List<DeploymentPod> pods = new List<DeploymentPod>();
        private double nextReconcile, previousTime;
        private int desired = 2, sequence, workerCursor, replacementDebt;
        public string ClusterId { get; }
        public IReadOnlyList<string> Workers => workers;
        public IReadOnlyList<DeploymentPod> Pods => pods;
        public int DesiredReplicas => desired;
        public int ActualReplicas { get { int n = 0; foreach (var pod in pods) if (engine.FindNode(pod.NodeId) is SimulationNode node && node.Status != NodeStatus.Crashed) n++; return n; } }
        public int ReadyReplicas { get { int n = 0; foreach (var pod in pods) if (Available(engine.FindNode(pod.NodeId)) && Available(engine.FindNode(pod.WorkerId))) n++; return n; } }
        public int Replacements { get; private set; }
        public int ScaleChanges { get; private set; }
        public int ActiveAlgorithmLine { get; private set; }
        public string LastAction { get; private set; } = "Add workers, set the explicit replica target, then Run/Step. HPA is not configured.";
        public DeploymentModel(SimulationEngine engine, string cluster, Func<string, int, string> create, Action<string> remove)
        {
            if (engine.FindNode(cluster)?.Type != NodeType.KubernetesCluster) throw new ArgumentException("Choose a Kubernetes Cluster.");
            this.engine = engine; ClusterId = cluster; createPod = create; removePod = remove;
        }
        public void AddWorker(string id)
        {
            var node = engine.FindNode(id);
            if (node?.Type != NodeType.KubernetesNode || node.HostingNodeId != null) throw new ArgumentException("Choose an unowned Worker Node.");
            if (workers.Count >= 4) throw new InvalidOperationException("This learning cluster supports four workers.");
            node.HostingNodeId = ClusterId; workers.Add(id); nextReconcile = 0;
        }
        public void ForgetWorker(string id) => workers.Remove(id);
        public void ResetObservations()
        { nextReconcile = previousTime = 0; Replacements = ScaleChanges = replacementDebt = 0; }
        public void SetDesired(int count)
        {
            if (count < 0 || count > 8) throw new ArgumentOutOfRangeException(nameof(count), "Replica target must be 0–8.");
            if (count == desired) return;
            desired = count; ScaleChanges++; nextReconcile = 0;
            Trace(2, "Learner changed desired replicas to " + count + ". This is explicit scaling, not failure replacement or HPA.");
        }
        public void Reconcile(double time)
        {
            if (time < previousTime) nextReconcile = 0;
            previousTime = time;
            if (time + 1e-9 < nextReconcile || !Available(engine.FindNode(ClusterId))) return;
            nextReconcile = time + .5;
            workers.RemoveAll(id => engine.FindNode(id) == null);
            Trace(0, "Observe desired target and current Pod identities.");
            for (int i = pods.Count - 1; i >= 0; i--)
            {
                var state = engine.FindNode(pods[i].NodeId); var worker = engine.FindNode(pods[i].WorkerId);
                if (state != null && state.Status != NodeStatus.Crashed && worker != null && worker.Status != NodeStatus.Crashed && worker.Status != NodeStatus.Offline) continue;
                string id = pods[i].NodeId; pods.RemoveAt(i); removePod(id); replacementDebt++;
                Trace(1, "Removed failed/lost Pod " + id + "; its replacement will have a new identity.");
            }
            while (pods.Count > desired)
            {
                var pod = pods[pods.Count - 1]; pods.RemoveAt(pods.Count - 1); removePod(pod.NodeId);
                Trace(4, "Terminated excess Pod after explicit scale-down.");
            }
            replacementDebt = Math.Min(replacementDebt, Math.Max(0, desired - pods.Count));
            while (pods.Count < desired)
            {
                string worker = ChooseWorker();
                if (worker == null) { Trace(5, "Pending: no healthy worker slot. Add/recover a worker; desired target is unchanged."); return; }
                string id = createPod(worker, ++sequence);
                var node = engine.FindNode(id) ?? throw new InvalidOperationException("Pod factory must create a real simulator node.");
                node.HostingNodeId = worker; engine.Stop(id); engine.Restart(id);
                pods.Add(new DeploymentPod(id, worker, sequence));
                if (replacementDebt > 0) { replacementDebt--; Replacements++; }
                Trace(3, "Scheduled " + id + " on " + worker + ". New Pod needs 1 simulated second to become ready.");
            }
            if (ActiveAlgorithmLine == 0) Trace(2, "Actual matches desired; no scaling action. HPA is not configured.");
        }
        private string ChooseWorker()
        {
            for (int i = 0; i < workers.Count; i++)
            {
                string id = workers[workerCursor++ % workers.Count]; if (workerCursor == int.MaxValue) workerCursor = 0;
                if (!Available(engine.FindNode(id))) continue;
                int count = 0; foreach (var pod in pods) if (pod.WorkerId == id) count++;
                // Bounded educational scheduler: four slots per worker, not a complete Kubernetes scheduler.
                if (count < 4) return id;
            }
            return null;
        }
        private static bool Available(SimulationNode node) => node != null && node.Status != NodeStatus.Crashed && node.Status != NodeStatus.Offline && node.Status != NodeStatus.Recovering;
        private void Trace(int line, string action) { ActiveAlgorithmLine = line; LastAction = action; }
    }
}
