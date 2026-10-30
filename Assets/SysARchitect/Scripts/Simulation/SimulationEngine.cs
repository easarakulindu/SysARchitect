using System;
using System.Collections.Generic;
using SysARchitect.Core;

namespace SysARchitect.Simulation
{
    /// <summary>Plain C# configuration/state. No scene, input, animation or wall-clock dependencies.</summary>
    public sealed class SimulationNode
    {
        public string Id { get; }
        public NodeType Type { get; }
        public double Capacity = 100;
        public int QueueCapacity = 50;
        public double MemoryLimitMB = 512;
        public double LatencyMs = 10;
        public double TimeoutMs = 5000;
        public NodeStatus Status { get; internal set; } = NodeStatus.Healthy;
        public double Cpu { get; internal set; }
        public double MemoryMB { get; internal set; }
        public double IncomingRate { get; internal set; }
        public int QueueLength => Waiting.Count;
        public int ProcessingCount => Processing.Count;
        public long Completed { get; internal set; }
        public long Failed { get; internal set; }
        public long Rejected { get; internal set; }
        public long Timeouts { get; internal set; }
        public double AverageLatencyMs => Completed == 0 ? 0 : TotalLatencyMs / Completed;
        public string FailureReason { get; internal set; }
        public DatabaseModel Database { get; }
        internal double TotalLatencyMs, Credit, RecoveryAt;
        internal int RouteIndex, Arrivals, Processed, RejectedThisTick;
        internal readonly Queue<SimRequest> Waiting = new Queue<SimRequest>();
        internal readonly List<SimRequest> Processing = new List<SimRequest>();
        internal readonly List<string> Targets = new List<string>();

        public SimulationNode(string id, NodeType type)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Node id is required.", nameof(id));
            Id = id;
            Type = type;
            if (type == NodeType.Database) Database = new DatabaseModel();
        }
    }

    internal sealed class SimRequest
    {
        internal long Id;
        internal bool IsWrite;
        internal int RecordKey;
        internal double CreatedAt, ArrivedAt, ReadyAt, Deadline;
        internal int Hops;
    }

    /// <summary>
    /// Fixed-tick, deterministic throughput model. Each node has a bounded FIFO and a
    /// rate-limited parallel service stage. Routing transfers are delivered after all
    /// nodes finish the tick, so node insertion order cannot give downstream capacity.
    /// Memory = 32 MB baseline + 1 MB per queued or processing request.
    /// </summary>
    public sealed class SimulationEngine
    {
        public const double TickSeconds = .05;
        private const double Epsilon = 1e-9;
        private readonly List<SimulationNode> nodes = new List<SimulationNode>();
        private readonly Dictionary<string, SimulationNode> byId = new Dictionary<string, SimulationNode>();
        private readonly List<(SimulationNode source, SimulationNode target, SimRequest request)> transfers =
            new List<(SimulationNode, SimulationNode, SimRequest)>();
        private double arrivalCredit;
        private int entryIndex;
        public IReadOnlyList<SimulationNode> Nodes => nodes;
        public double Time { get; private set; }
        public double RequestsPerSecond { get; set; } = 30;
        public long Generated { get; private set; }
        public long Succeeded { get; private set; }
        public long Failed { get; private set; }
        public long TimedOut { get; private set; }
        public long InFlight => Generated - Succeeded - Failed;
        public double AverageLatencyMs => Succeeded == 0 ? 0 : totalLatencyMs / Succeeded;
        private double totalLatencyMs;
        public event Action<long, string, string> OnTransfer;
        public event Action<long, string, bool> OnRequestFinished;
        public string LastFailureReason { get; private set; }
        public string LastFailureNodeId { get; private set; }

        public SimulationNode FindNode(string id) => byId.TryGetValue(id, out var node) ? node : null;

        /// <summary>Explicit database exercises use the same queue, capacity, deadline and counters.</summary>
        public long SubmitDatabaseRequest(string id, bool write, int key)
        {
            var node = FindNode(id);
            if (node?.Database == null) throw new ArgumentException("Choose a Database node.");
            ValidateConfig(node);
            Generated++;
            Arrive(node, new SimRequest { Id = Generated, IsWrite = write, RecordKey = key,
                CreatedAt = Time, Deadline = Time + node.TimeoutMs / 1000 });
            return Generated;
        }

        public void AddNode(SimulationNode node)
        {
            if (node == null || byId.ContainsKey(node.Id)) throw new ArgumentException("Node must have a unique id.");
            byId.Add(node.Id, node);
            nodes.Add(node);
        }

        public void RemoveNode(string id)
        {
            var node = FindNode(id);
            if (node == null) return;
            FailPending(node, "Component removed");
            byId.Remove(id);
            nodes.Remove(node);
            foreach (var other in nodes) other.Targets.Remove(id);
        }

        public void SetTargets(string source, IEnumerable<string> targets)
        {
            var node = FindNode(source) ?? throw new ArgumentException("Unknown source node.");
            node.Targets.Clear();
            foreach (string target in targets)
            {
                if (target == source || !byId.ContainsKey(target)) throw new ArgumentException("Invalid target node.");
                if (!node.Targets.Contains(target)) node.Targets.Add(target);
            }
        }

        public void InjectFailure(string id)
        {
            var node = FindNode(id);
            if (node == null) return;
            node.Status = NodeStatus.Crashed;
            FailPending(node, "Injected failure");
        }

        public void Restart(string id)
        {
            var node = FindNode(id);
            if (node == null || IsAvailable(node)) return;
            node.Status = NodeStatus.Recovering;
            node.RecoveryAt = Time + 1;
            node.FailureReason = null;
            node.Credit = 0;
        }

        public void Reset()
        {
            Time = arrivalCredit = totalLatencyMs = 0;
            Generated = Succeeded = Failed = TimedOut = 0;
            LastFailureReason = LastFailureNodeId = null;
            entryIndex = 0;
            transfers.Clear();
            foreach (var node in nodes)
            {
                node.Waiting.Clear();
                node.Processing.Clear();
                node.Status = NodeStatus.Healthy;
                node.Cpu = node.IncomingRate = node.TotalLatencyMs = node.Credit = node.RecoveryAt = 0;
                node.MemoryMB = 32;
                node.Completed = node.Failed = node.Rejected = node.Timeouts = 0;
                node.RouteIndex = node.Arrivals = node.Processed = node.RejectedThisTick = 0;
                node.FailureReason = null;
                node.Database?.Reset();
            }
        }

        public void Step()
        {
            if (double.IsNaN(RequestsPerSecond) || double.IsInfinity(RequestsPerSecond) || RequestsPerSecond < 0 || RequestsPerSecond > 1000)
                throw new InvalidOperationException("Traffic must be between 0 and 1000 requests per second.");
            foreach (var node in nodes) ValidateConfig(node);
            Time += TickSeconds;
            foreach (var node in nodes)
            {
                node.Arrivals = node.Processed = node.RejectedThisTick = 0;
                if (node.Status == NodeStatus.Recovering && Time + Epsilon >= node.RecoveryAt)
                    node.Status = NodeStatus.Healthy;
                node.Database?.AdvanceTo(Time, IsAvailable(node));
            }
            GenerateTraffic();
            foreach (var node in nodes)
            {
                if (!IsAvailable(node)) continue;
                ExpireAndComplete(node);
                // Idle capacity is not banked for a later burst.
                node.Credit += node.Capacity * TickSeconds;
                int budget = (int)Math.Floor(node.Credit + Epsilon);
                node.Credit -= budget;
                while (budget-- > 0 && node.Waiting.Count > 0)
                {
                    var request = node.Waiting.Dequeue();
                    request.ReadyAt = Time + node.LatencyMs / 1000;
                    node.Processing.Add(request);
                    node.Processed++;
                }
            }
            foreach (var transfer in transfers)
            {
                OnTransfer?.Invoke(transfer.request.Id, transfer.source.Id, transfer.target.Id);
                Arrive(transfer.target, transfer.request);
            }
            transfers.Clear();
            foreach (var node in nodes) UpdateResources(node);
        }

        private void GenerateTraffic()
        {
            int count = 0;
            foreach (var node in nodes) if (node.Type == NodeType.Frontend) count++;
            if (count == 0) { arrivalCredit = 0; return; }
            arrivalCredit += RequestsPerSecond * TickSeconds;
            int arrivals = (int)Math.Floor(arrivalCredit + Epsilon);
            arrivalCredit -= arrivals;
            for (int i = 0; i < arrivals; i++)
            {
                int selected = entryIndex++ % count;
                if (entryIndex == int.MaxValue) entryIndex = 0;
                foreach (var node in nodes)
                {
                    if (node.Type != NodeType.Frontend || selected-- != 0) continue;
                    Generated++;
                    Arrive(node, new SimRequest { Id = Generated, IsWrite = Generated % 4 == 0, RecordKey = (int)((Generated / 4) % 32),
                        CreatedAt = Time, Deadline = Time + node.TimeoutMs / 1000 });
                    break;
                }
            }
        }

        private void Arrive(SimulationNode node, SimRequest request)
        {
            node.Arrivals++;
            request.ArrivedAt = Time;
            request.Deadline = Math.Min(request.Deadline, Time + node.TimeoutMs / 1000);
            if (!IsAvailable(node)) { Fail(node, request, false, "Dependency unavailable"); return; }
            if (node.Waiting.Count >= node.QueueCapacity)
            {
                node.Rejected++;
                node.RejectedThisTick++;
                Fail(node, request, false, "Queue full");
                return;
            }
            node.Waiting.Enqueue(request);
        }

        private void ExpireAndComplete(SimulationNode node)
        {
            int waiting = node.Waiting.Count;
            for (int i = 0; i < waiting; i++)
            {
                var request = node.Waiting.Dequeue();
                if (Time + Epsilon >= request.Deadline) Fail(node, request, true, "Request deadline expired");
                else node.Waiting.Enqueue(request);
            }
            // Preserve request order; completion order determines round-robin routing.
            for (int i = 0; i < node.Processing.Count;)
            {
                var request = node.Processing[i];
                if (Time + Epsilon >= request.Deadline)
                {
                    node.Processing.RemoveAt(i);
                    Fail(node, request, true, "Request deadline expired");
                }
                else if (Time + Epsilon >= request.ReadyAt)
                {
                    node.Processing.RemoveAt(i);
                    node.Completed++;
                    node.TotalLatencyMs += (Time - request.ArrivedAt) * 1000;
                    Route(node, request);
                }
                else i++;
            }
        }

        private void Route(SimulationNode source, SimRequest request)
        {
            if (source.Database != null && !source.Database.Execute(request.IsWrite, request.RecordKey, out string error))
            {
                Fail(source, request, false, error);
                return;
            }
            if (++request.Hops > 32) { Fail(source, request, false, "Routing hop limit exceeded"); return; }
            if (source.Targets.Count == 0)
            {
                if (source.Type == NodeType.Frontend || source.Type == NodeType.LoadBalancer || source.Type == NodeType.KubernetesNode)
                    Fail(source, request, false, "No service route configured");
                else
                {
                    Succeeded++;
                    totalLatencyMs += (Time - request.CreatedAt) * 1000;
                    OnRequestFinished?.Invoke(request.Id, source.Id, true);
                }
                return;
            }
            // All multi-target service routes use round-robin. LBs skip unavailable targets.
            for (int i = 0; i < source.Targets.Count; i++)
            {
                int index = source.RouteIndex % source.Targets.Count;
                source.RouteIndex = (index + 1) % source.Targets.Count;
                var target = FindNode(source.Targets[index]);
                if (target == null || (source.Type == NodeType.LoadBalancer && !IsAvailable(target))) continue;
                transfers.Add((source, target, request));
                return;
            }
            Fail(source, request, false, "No healthy routing target");
        }

        private void UpdateResources(SimulationNode node)
        {
            node.IncomingRate = node.Arrivals / TickSeconds;
            double utilization = node.Capacity <= 0 ? 0 : Math.Min(1, node.Processed / (node.Capacity * TickSeconds));
            node.Cpu += .2 * (utilization - node.Cpu);
            node.MemoryMB = 32 + node.Waiting.Count + node.Processing.Count;
            if (!IsAvailable(node)) { node.Cpu = 0; return; }
            if (node.MemoryMB > node.MemoryLimitMB)
            {
                node.Status = NodeStatus.Crashed;
                FailPending(node, "Memory exhaustion");
                return;
            }
            bool backlog = node.QueueLength > 0 && Time - node.Waiting.Peek().ArrivedAt + Epsilon >= TickSeconds;
            node.Status = backlog || node.RejectedThisTick > 0 ? NodeStatus.Overloaded
                : node.Cpu >= .8 ? NodeStatus.Warning : NodeStatus.Healthy;
        }

        private void Fail(SimulationNode node, SimRequest request, bool timeout, string reason)
        {
            Failed++;
            node.Failed++;
            LastFailureReason = reason;
            LastFailureNodeId = node.Id;
            if (IsAvailable(node)) node.FailureReason = reason;
            if (timeout) { TimedOut++; node.Timeouts++; }
            OnRequestFinished?.Invoke(request.Id, node.Id, false);
        }

        private void FailPending(SimulationNode node, string reason)
        {
            while (node.Waiting.Count > 0) Fail(node, node.Waiting.Dequeue(), false, reason);
            foreach (var request in node.Processing) Fail(node, request, false, reason);
            node.Processing.Clear();
            node.FailureReason = reason;
            node.Cpu = 0;
        }

        private static bool IsAvailable(SimulationNode node) => node.Status != NodeStatus.Crashed &&
            node.Status != NodeStatus.Offline && node.Status != NodeStatus.Recovering;

        private static void ValidateConfig(SimulationNode node)
        {
            if (!Finite(node.Capacity) || node.Capacity < 0 || node.Capacity > 10000 || node.QueueCapacity < 1 ||
                !Finite(node.MemoryLimitMB) || node.MemoryLimitMB <= 0 || !Finite(node.LatencyMs) || node.LatencyMs < 0 ||
                !Finite(node.TimeoutMs) || node.TimeoutMs <= 0)
                throw new InvalidOperationException("Invalid resource configuration for " + node.Id);
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
