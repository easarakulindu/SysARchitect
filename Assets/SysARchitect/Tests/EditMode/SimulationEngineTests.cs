using System;
using System.Linq;
using NUnit.Framework;
using SysARchitect.Core;
using SysARchitect.Simulation;

namespace SysARchitect.Tests
{
    public class SimulationEngineTests
    {
        private static SimulationEngine Architecture(int backends = 1, bool loadBalancer = false)
        {
            var engine = new SimulationEngine { RequestsPerSecond = 100 };
            engine.AddNode(new SimulationNode("frontend", NodeType.Frontend) { Capacity = 1000, QueueCapacity = 100 });
            if (loadBalancer)
                engine.AddNode(new SimulationNode("lb", NodeType.LoadBalancer) { Capacity = 1000, QueueCapacity = 100 });
            for (int i = 0; i < backends; i++)
                engine.AddNode(new SimulationNode("backend" + i, NodeType.Backend) { Capacity = 60, QueueCapacity = 30 });
            engine.SetTargets("frontend", new[] { loadBalancer ? "lb" : "backend0" });
            if (loadBalancer) engine.SetTargets("lb", Enumerable.Range(0, backends).Select(i => "backend" + i));
            return engine;
        }

        private static void Run(SimulationEngine engine, int ticks)
        {
            for (int i = 0; i < ticks; i++)
            {
                engine.Step();
                Assert.That(engine.Generated, Is.EqualTo(engine.Succeeded + engine.Failed + engine.InFlight));
                Assert.That(engine.InFlight, Is.EqualTo(engine.Nodes.Sum(n => (long)n.QueueLength + n.ProcessingCount)));
                Assert.That(engine.Nodes.All(n => n.QueueLength <= n.QueueCapacity), Is.True);
            }
        }

        [Test]
        public void SameTopologyAndTickSequenceReproduceAllCounters()
        {
            var first = Architecture(2, true);
            var second = Architecture(2, true);
            Run(first, 400);
            Run(second, 400);
            Assert.That(first.Succeeded, Is.EqualTo(second.Succeeded));
            Assert.That(first.Failed, Is.EqualTo(second.Failed));
            Assert.That(first.AverageLatencyMs, Is.EqualTo(second.AverageLatencyMs));
            Assert.That(first.Nodes.Select(n => n.Completed), Is.EqualTo(second.Nodes.Select(n => n.Completed)));
            Assert.That(first.Generated, Is.EqualTo(2000));
        }

        [Test]
        public void OverloadQueuesAndRejectsWithoutCpuInducedCrash()
        {
            var engine = Architecture();
            Run(engine, 200);
            var backend = engine.FindNode("backend0");
            Assert.That(backend.QueueLength, Is.GreaterThan(0));
            Assert.That(backend.Rejected, Is.GreaterThan(0));
            Assert.That(backend.Cpu, Is.EqualTo(1).Within(.001));
            Assert.That(backend.Status, Is.EqualTo(NodeStatus.Overloaded));
            Assert.That(engine.FindNode("frontend").Status, Is.Not.EqualTo(NodeStatus.Crashed));
        }

        [Test]
        public void FreshArrivalsBelowCapacityAreNotReportedAsOverload()
        {
            var engine = Architecture(2, true);
            engine.RequestsPerSecond = 60;
            Run(engine, 200);
            Assert.That(engine.Nodes.All(n => n.Status != NodeStatus.Overloaded), Is.True);
            Assert.That(engine.FindNode("backend0").Cpu, Is.InRange(.35, .65));
            Assert.That(engine.Failed, Is.Zero);
        }

        [Test]
        public void QueuedRequestsExpireAndTrafficReductionDrainsTheQueue()
        {
            var engine = Architecture();
            engine.FindNode("backend0").Capacity = 0;
            engine.FindNode("backend0").TimeoutMs = 200;
            Run(engine, 80);
            Assert.That(engine.TimedOut, Is.GreaterThan(0));
            engine.RequestsPerSecond = 0;
            Run(engine, 120);
            Assert.That(engine.InFlight, Is.Zero);
            Assert.That(engine.Generated, Is.EqualTo(engine.Failed));
        }

        [Test]
        public void LongProcessingAlsoObservesRequestDeadline()
        {
            var engine = Architecture();
            engine.FindNode("backend0").LatencyMs = 1000;
            engine.FindNode("backend0").TimeoutMs = 200;
            Run(engine, 80);
            Assert.That(engine.Succeeded, Is.Zero);
            Assert.That(engine.FindNode("backend0").Timeouts, Is.GreaterThan(0));
        }

        [Test]
        public void RoundRobinBalancesAndAdditionalBackendAddsCapacity()
        {
            var single = Architecture(1, true);
            var pair = Architecture(2, true);
            Run(single, 400);
            Run(pair, 400);
            Assert.That(pair.Succeeded, Is.GreaterThan(single.Succeeded * 1.4));
            Assert.That(Math.Abs(pair.FindNode("backend0").Completed - pair.FindNode("backend1").Completed), Is.LessThanOrEqualTo(1));
            Assert.That(pair.Failed, Is.Zero);
            var direct = Architecture();
            Run(direct, 400);
            Assert.That(single.Succeeded, Is.LessThanOrEqualTo(direct.Succeeded));
        }

        [Test]
        public void LoadBalancerSkipsFailedBackendAndRestartHasRecoveryDelay()
        {
            var engine = Architecture(2, true);
            Run(engine, 50);
            engine.InjectFailure("backend0");
            long failedNodeCompletions = engine.FindNode("backend0").Completed;
            long healthyCompletions = engine.FindNode("backend1").Completed;
            Run(engine, 40);
            Assert.That(engine.FindNode("backend0").Completed, Is.EqualTo(failedNodeCompletions));
            Assert.That(engine.FindNode("backend1").Completed, Is.GreaterThan(healthyCompletions));
            engine.Restart("backend0");
            Assert.That(engine.FindNode("backend0").Status, Is.EqualTo(NodeStatus.Recovering));
            Run(engine, 10);
            Assert.That(engine.FindNode("backend0").Status, Is.EqualTo(NodeStatus.Recovering));
            Run(engine, 40);
            Assert.That(engine.FindNode("backend0").Completed, Is.GreaterThan(failedNodeCompletions));
        }

        [Test]
        public void DependencyFailureDoesNotCrashHealthyFrontend()
        {
            var engine = Architecture();
            engine.InjectFailure("backend0");
            Run(engine, 40);
            Assert.That(engine.Failed, Is.GreaterThan(0));
            Assert.That(engine.FindNode("frontend").Status, Is.EqualTo(NodeStatus.Healthy));
        }

        [Test]
        public void MemoryExhaustionCrashesAndFailsPendingRequests()
        {
            var engine = Architecture();
            engine.FindNode("backend0").Capacity = 0;
            engine.FindNode("backend0").MemoryLimitMB = 35;
            Run(engine, 40);
            Assert.That(engine.FindNode("backend0").Status, Is.EqualTo(NodeStatus.Crashed));
            Assert.That(engine.FindNode("backend0").FailureReason, Is.Not.Null);
            Assert.That(engine.FindNode("frontend").Status, Is.Not.EqualTo(NodeStatus.Crashed));
        }

        [Test]
        public void ResetReplaysIdenticallyAndPreservesTopologyAndConfiguration()
        {
            var engine = Architecture();
            Run(engine, 200);
            long completed = engine.Succeeded;
            long failed = engine.Failed;
            engine.Reset();
            Assert.That(engine.Generated, Is.Zero);
            Assert.That(engine.InFlight, Is.Zero);
            Assert.That(engine.Time, Is.Zero);
            Run(engine, 200);
            Assert.That(engine.Succeeded, Is.EqualTo(completed));
            Assert.That(engine.Failed, Is.EqualTo(failed));
        }

        [Test]
        public void RemovingBusyNodeAccountsForItsRequestsAndAllowsNewCapacity()
        {
            var engine = Architecture(2, true);
            Run(engine, 80);
            engine.RemoveNode("backend0");
            Run(engine, 40);
            engine.AddNode(new SimulationNode("backend2", NodeType.Backend));
            engine.SetTargets("lb", new[] { "backend1", "backend2" });
            Run(engine, 80);
            Assert.That(engine.FindNode("backend2").Completed, Is.GreaterThan(0));
        }

        [Test]
        public void NoFrontendProducesNoTrafficAndMissingRouteFailsExplicitly()
        {
            var engine = new SimulationEngine();
            Run(engine, 20);
            Assert.That(engine.Generated, Is.Zero);
            engine.AddNode(new SimulationNode("frontend", NodeType.Frontend));
            Run(engine, 40);
            Assert.That(engine.Failed, Is.GreaterThan(0));
            Assert.That(engine.Nodes[0].Status, Is.Not.EqualTo(NodeStatus.Crashed));
        }

        [Test]
        public void RoutingCycleTerminatesAtHopLimit()
        {
            var engine = Architecture();
            engine.AddNode(new SimulationNode("backend1", NodeType.Backend) { Capacity = 1000 });
            engine.FindNode("backend0").Capacity = 1000;
            engine.SetTargets("backend0", new[] { "backend1" });
            engine.SetTargets("backend1", new[] { "backend0" });
            Run(engine, 200);
            Assert.That(engine.Failed, Is.GreaterThan(0));
            engine.RequestsPerSecond = 0;
            Run(engine, 300);
            Assert.That(engine.InFlight, Is.Zero);
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(-1)]
        [TestCase(1001)]
        public void InvalidTrafficIsRejected(double traffic)
        {
            var engine = Architecture();
            engine.RequestsPerSecond = traffic;
            Assert.Throws<InvalidOperationException>(() => engine.Step());
        }
    }
}
