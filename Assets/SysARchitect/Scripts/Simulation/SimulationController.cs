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
        private int visualsThisTick;
        private readonly List<string> targets = new List<string>();
        public SimulationEngine Engine { get; } = new SimulationEngine();
        public bool IsRunning { get; private set; }

        private void OnEnable()
        {
            graph.OnNodeAdded += AddNode;
            graph.OnNodeRemoved += RemoveNode;
            graph.OnConnectionAdded += ConnectionChanged;
            graph.OnConnectionRemoved += ConnectionChanged;
            Engine.OnTransfer += ShowTransfer;
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
            graph.OnConnectionRemoved -= ConnectionChanged;
            Engine.OnTransfer -= ShowTransfer;
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
            Tick();
        }

        public void ResetSimulation()
        {
            Pause();
            Engine.Reset();
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
            visualsThisTick = 0;
            Engine.Step();
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

        private void ShowTransfer(string sourceId, string targetId)
        {
            // Sample visual traffic: never create one GameObject for every simulated request.
            if (packetMaterial == null || packetRoot.childCount >= 64 || visualsThisTick++ >= 2) return;
            var source = graph.FindNode(sourceId);
            var target = graph.FindNode(targetId);
            if (source == null || target == null) return;
            var packet = RequestPacketVisual.Create(packetMaterial);
            packet.transform.SetParent(packetRoot, true);
            packet.IsPaused = !IsRunning;
            var state = Engine.FindNode(targetId);
            Color color = state.Status == NodeStatus.Crashed ? RequestPacketVisual.Failure : RequestPacketVisual.Pending;
            packet.Launch(source.transform, target.transform, color, 1.5f, null);
        }

        private void PausePackets(bool paused)
        {
            if (packetRoot == null) return;
            foreach (Transform child in packetRoot)
                child.GetComponent<RequestPacketVisual>().IsPaused = paused;
        }
    }
}
