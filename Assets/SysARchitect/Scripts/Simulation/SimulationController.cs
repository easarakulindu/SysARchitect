using System;
using System.Collections.Generic;
using UnityEngine;
using SysARchitect.Core;
using SysARchitect.Graph;
using SysARchitect.Interaction;
using SysARchitect.Visualization;

namespace SysARchitect.Simulation
{
    /// <summary>Adapts the existing workspace graph to the scene-independent simulation.</summary>
    public class SimulationController : MonoBehaviour
    {
        [SerializeField] private ArchitectureGraph graph;
        [SerializeField] private InteractionManager interactionManager;
        [SerializeField] private AppStateManager appState;
        [SerializeField] private Material packetMaterial;
        private Transform packetRoot;
        private double accumulator;
        private sealed class VisualTransfer
        {
            public long RequestId;
            public Color Color = RequestPacketVisual.Pending;
        }
        private readonly Dictionary<GraphConnection, VisualTransfer> pendingVisuals = new Dictionary<GraphConnection, VisualTransfer>();
        private int visualCursor;
        private string shownFailure;
        public event Action<string> OnMessage;
        private readonly List<string> targets = new List<string>();
        public SimulationEngine Engine { get; } = new SimulationEngine();
        public bool IsRunning { get; private set; }

        private void OnEnable()
        {
            graph.OnNodeAdded += AddNode;
            graph.OnNodeRemoved += RemoveNode;
            graph.OnConnectionAdded += ConnectionChanged;
            graph.OnConnectionRemoved += ConnectionRemoved;
            Engine.OnTransfer += QueueTransfer;
            Engine.OnRequestFinished += ShowOutcome;
            foreach (var node in graph.Nodes)
                if (Engine.FindNode(node.NodeId) == null) AddNode(node);
            SyncConnections();
            if (packetRoot == null)
            {
                packetRoot = new GameObject("Request Packets").transform;
                packetRoot.SetParent(transform, false);
            }
        }

        private void OnDisable()
        {
            graph.OnNodeAdded -= AddNode;
            graph.OnNodeRemoved -= RemoveNode;
            graph.OnConnectionAdded -= ConnectionChanged;
            graph.OnConnectionRemoved -= ConnectionRemoved;
            Engine.OnTransfer -= QueueTransfer;
            Engine.OnRequestFinished -= ShowOutcome;
            Pause();
        }

        private void Update()
        {
            if (!IsRunning) return;
            // Bound catch-up work after a stall; explicit ticks still define simulation time.
            accumulator = Math.Min(accumulator + Time.unscaledDeltaTime, SimulationEngine.TickSeconds * 8);
            while (accumulator >= SimulationEngine.TickSeconds)
            {
                Tick();
                accumulator -= SimulationEngine.TickSeconds;
            }
        }

        public void Run()
        {
            interactionManager.ReturnToSelectMode();
            ExplainArchitecture();
            IsRunning = true;
            appState.SetMode(AppMode.Simulate);
            PausePackets(false);
        }

        public void Pause()
        {
            IsRunning = false;
            accumulator = 0;
            PausePackets(true);
        }

        public void Build()
        {
            Pause();
            interactionManager.ReturnToSelectMode();
            appState.SetMode(AppMode.Build);
        }

        public void StepOnce()
        {
            Pause();
            appState.SetMode(AppMode.Simulate);
            if (Engine.Time == 0) ExplainArchitecture();
            Tick();
            if (packetRoot != null)
                foreach (Transform child in packetRoot) child.GetComponent<RequestPacketVisual>().Advance((float)SimulationEngine.TickSeconds);
        }

        public void ResetSimulation()
        {
            Pause();
            Engine.Reset();
            pendingVisuals.Clear();
            visualCursor = 0;
            shownFailure = null;
            if (packetRoot != null)
                foreach (Transform packet in packetRoot) Destroy(packet.gameObject);
            PresentState();
        }

        public void FailNode(GraphNode node)
        {
            if (node == null) return;
            Engine.InjectFailure(node.NodeId);
            PresentState();
        }

        public void RestartNode(GraphNode node)
        {
            if (node == null) return;
            Engine.Restart(node.NodeId);
            PresentState();
        }

        private void Tick()
        {
            foreach (var node in graph.Nodes) CopyConfiguration(node, Engine.FindNode(node.NodeId));
            Engine.Step();
            FlushVisuals();
            string failure = Engine.LastFailureNodeId + ": " + Engine.LastFailureReason;
            if (Engine.LastFailureReason != null && failure != shownFailure)
            {
                shownFailure = failure;
                var failedNode = graph.FindNode(Engine.LastFailureNodeId);
                OnMessage?.Invoke($"Last failure at {failedNode?.DisplayName}: {Engine.LastFailureReason}. " +
                    (Engine.LastFailureReason == "No service route configured" ? "Choose Connect, then click a source and target node." : "Inspect the node, its routing, capacity and health."));
            }
            PresentState();
        }

        private void AddNode(GraphNode node)
        {
            var state = new SimulationNode(node.NodeId, node.Type);
            CopyConfiguration(node, state);
            Engine.AddNode(state);
        }

        private void RemoveNode(GraphNode node) => Engine.RemoveNode(node.NodeId);
        private void ConnectionChanged(GraphConnection connection) => SyncConnections();

        private void SyncConnections()
        {
            foreach (var node in graph.Nodes)
            {
                targets.Clear();
                foreach (var edge in graph.Connections)
                    if (edge.Source == node) targets.Add(edge.Target.NodeId);
                Engine.SetTargets(node.NodeId, targets);
            }
        }

        private static void CopyConfiguration(GraphNode node, SimulationNode state)
        {
            state.Capacity = node.ProcessingCapacity;
            state.QueueCapacity = node.QueueCapacity;
            state.MemoryLimitMB = node.MemoryLimitMB;
            state.LatencyMs = node.ProcessingLatencyMs;
            state.TimeoutMs = node.TimeoutThresholdMs;
        }

        private void PresentState()
        {
            foreach (var node in graph.Nodes)
            {
                var state = Engine.FindNode(node.NodeId);
                node.Status = state.Status;
                node.CpuUtilization = (float)state.Cpu;
                node.MemoryUsageMB = (float)state.MemoryMB;
            }
        }

        private void QueueTransfer(long requestId, string sourceId, string targetId)
        {
            foreach (var edge in graph.Connections)
                if (edge.Source.NodeId == sourceId && edge.Target.NodeId == targetId)
                {
                    pendingVisuals[edge] = new VisualTransfer { RequestId = requestId };
                    return;
                }
        }

        private void ShowOutcome(long requestId, string nodeId, bool succeeded)
        {
            foreach (var transfer in pendingVisuals.Values)
                if (transfer.RequestId == requestId)
                    transfer.Color = succeeded ? RequestPacketVisual.Success : RequestPacketVisual.Failure;
            if (packetRoot == null) return;
            foreach (Transform child in packetRoot)
            {
                var packet = child.GetComponent<RequestPacketVisual>();
                if (packet.RequestId == requestId) packet.SetOutcome(succeeded);
            }
        }

        private void FlushVisuals()
        {
            if (packetMaterial == null || graph.Connections.Count == 0) return;
            // Rotate across actual edges; one pending sample per edge bounds storage and
            // prevents a busy frontend from starving downstream paths of packet visuals.
            int inspected = 0, spawned = 0;
            while (inspected++ < graph.Connections.Count && spawned < 2 && packetRoot.childCount < 64)
            {
                visualCursor %= graph.Connections.Count;
                var edge = graph.Connections[visualCursor++];
                if (!pendingVisuals.TryGetValue(edge, out var transfer)) continue;
                pendingVisuals.Remove(edge);
                var packet = RequestPacketVisual.Create(packetMaterial);
                packet.transform.SetParent(packetRoot, true);
                packet.IsPaused = !IsRunning;
                packet.Launch(edge, transfer.RequestId, transfer.Color, 1.5f);
                spawned++;
            }
        }

        private void ConnectionRemoved(GraphConnection connection)
        {
            pendingVisuals.Remove(connection);
            if (packetRoot != null)
                foreach (Transform child in packetRoot)
                    if (child.GetComponent<RequestPacketVisual>().Connection == connection)
                    {
                        child.gameObject.SetActive(false);
                        Destroy(child.gameObject);
                    }
            SyncConnections();
        }

        private void ExplainArchitecture()
        {
            var warnings = graph.ValidateGraph();
            string message = graph.Connections.Count == 0
                ? "No connections: placing nodes alone does not create a request path. Choose Connect, click source, then target. Unrouted requests fail."
                : $"{graph.Connections.Count} directed links. Packets: cyan pending, green succeeded, red failed. Counters persist until Reset.";
            if (warnings.Count > 0) message += "\n" + warnings[0];
            OnMessage?.Invoke(message);
        }

        private void PausePackets(bool paused)
        {
            if (packetRoot == null) return;
            foreach (Transform child in packetRoot)
                child.GetComponent<RequestPacketVisual>().IsPaused = paused;
        }
    }
}
