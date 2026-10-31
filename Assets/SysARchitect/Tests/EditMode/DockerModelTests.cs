using NUnit.Framework;
using SysARchitect.Components;
using SysARchitect.Core;
using SysARchitect.Simulation;

namespace SysARchitect.Tests
{
    public class DockerModelTests
    {
        private static (SimulationEngine engine, DockerHostModel model) Host()
        {
            var engine = new SimulationEngine { RequestsPerSecond = 150 };
            engine.AddNode(new SimulationNode("f", NodeType.Frontend) { Capacity = 1000 });
            engine.AddNode(new SimulationNode("h", NodeType.DockerHost) { Capacity = 200 });
            engine.AddNode(new SimulationNode("c1", NodeType.Container));
            engine.AddNode(new SimulationNode("c2", NodeType.Container));
            var model = new DockerHostModel(engine, "h");
            model.Attach("c1", DockerHostModel.Images[0]); model.Attach("c2", DockerHostModel.Images[0]);
            engine.SetTargets("f", new[] { "h" }); engine.SetTargets("h", new[] { "c1", "c2" });
            return (engine, model);
        }
        [Test]
        public void ImagesAreReusableButInstancesHaveIsolatedLimitsAndFailures()
        {
            var (engine, model) = Host(); for (int i = 0; i < 100; i++) engine.Step();
            Assert.That(model.Containers[0].Image, Is.SameAs(model.Containers[1].Image));
            Assert.That(engine.FindNode("c1").Completed, Is.GreaterThan(200));
            Assert.That(engine.FindNode("c2").Completed, Is.GreaterThan(200));
            Assert.That(engine.Failed, Is.Zero);
            Assert.That(engine.FindNode("h").MemoryMB, Is.GreaterThanOrEqualTo(96));
            long first = engine.FindNode("c1").Completed, second = engine.FindNode("c2").Completed;
            engine.InjectFailure("c1"); for (int i = 0; i < 20; i++) engine.Step();
            Assert.That(engine.FindNode("c1").Completed, Is.EqualTo(first));
            Assert.That(engine.FindNode("c2").Completed, Is.GreaterThan(second));
            Assert.That(engine.FindNode("h").Status, Is.Not.EqualTo(NodeStatus.Crashed));
        }
        [Test]
        public void ExplicitStopStartAndRestartUseSimulationTimeAndPreserveOwnership()
        {
            var (engine, model) = Host(); model.Stop("c1");
            Assert.That(engine.FindNode("c1").Status, Is.EqualTo(NodeStatus.Offline));
            model.Start("c1"); Assert.That(engine.FindNode("c1").Status, Is.EqualTo(NodeStatus.Recovering));
            for (int i = 0; i < 20; i++) engine.Step();
            Assert.That(engine.FindNode("c1").Status, Is.Not.EqualTo(NodeStatus.Recovering));
            model.Restart("c2"); Assert.That(engine.FindNode("c2").Status, Is.EqualTo(NodeStatus.Recovering));
            Assert.That(model.Containers.Count, Is.EqualTo(2));
            Assert.Throws<System.ArgumentException>(() => model.Stop("f"));
        }
        [Test]
        public void HostMemoryBudgetIncludesInstancesAndHostFailureConservesRequests()
        {
            var (engine, model) = Host(); engine.FindNode("h").MemoryLimitMB = 70;
            for (int i = 0; i < 40; i++) engine.Step();
            Assert.That(engine.FindNode("h").Status, Is.EqualTo(NodeStatus.Crashed));
            long active = 0; foreach (var node in engine.Nodes) active += node.QueueLength + node.ProcessingCount;
            Assert.That(engine.InFlight, Is.EqualTo(active));
            Assert.That(engine.Failed, Is.GreaterThan(0));
            Assert.That(engine.FindNode("c1").QueueLength + engine.FindNode("c2").QueueLength, Is.Zero);
        }
        [Test]
        public void ExtraInstancesDoNotBypassTheSharedHostThroughputBudget()
        {
            var (engine, model) = Host(); engine.FindNode("h").Capacity = 50;
            for (int i = 0; i < 80; i++) engine.Step();
            Assert.That(engine.Succeeded, Is.LessThanOrEqualTo(200));
            // 50/s alternates 2 and 3 service starts per 50 ms tick; the existing
            // capped utilization average reports that quantized saturated workload.
            Assert.That(engine.FindNode("h").Cpu, Is.GreaterThan(.85));
            Assert.That(engine.FindNode("c1").Status, Is.Not.EqualTo(NodeStatus.Crashed));
        }
        [Test]
        public void AHostCannotRouteToAnotherHostsInstance()
        {
            var (engine, model) = Host();
            engine.AddNode(new SimulationNode("other", NodeType.DockerHost));
            engine.AddNode(new SimulationNode("foreign", NodeType.Container));
            new DockerHostModel(engine, "other").Attach("foreign", DockerHostModel.Images[1]);
            engine.SetTargets("h", new[] { "foreign" });
            for (int i = 0; i < 30; i++) engine.Step();
            Assert.That(engine.FindNode("foreign").Completed, Is.Zero);
            Assert.That(engine.LastFailureReason, Does.Contain("own Containers"));
        }
    }
}
