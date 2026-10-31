using System.Linq;
using NUnit.Framework;
using SysARchitect.Components;
using SysARchitect.Core;
using SysARchitect.Simulation;

namespace SysARchitect.Tests
{
    public class DeploymentModelTests
    {
        private static (SimulationEngine engine, DeploymentModel model) Cluster(int workers = 2)
        {
            var engine = new SimulationEngine { RequestsPerSecond = 0 };
            engine.AddNode(new SimulationNode("cluster", NodeType.KubernetesCluster) { Capacity = 1000 });
            var model = new DeploymentModel(engine, "cluster", (worker, generation) =>
            {
                string id = "pod-" + generation;
                engine.AddNode(new SimulationNode(id, NodeType.KubernetesPod) { Capacity = 60, MemoryLimitMB = 128 });
                engine.SetTargets(worker, engine.Nodes.Where(n => n.HostingNodeId == worker).Select(n => n.Id).Append(id).ToArray());
                return id;
            }, engine.RemoveNode);
            for (int i = 0; i < workers; i++)
            {
                string id = "worker-" + i; engine.AddNode(new SimulationNode(id, NodeType.KubernetesNode) { Capacity = 200 }); model.AddWorker(id);
            }
            engine.SetTargets("cluster", model.Workers); return (engine, model);
        }
        private static void Tick(SimulationEngine engine, DeploymentModel model, int count)
        { for (int i = 0; i < count; i++) { model.Reconcile(engine.Time); engine.Step(); } }
        [Test]
        public void FailedPodIsReplacedWithANewIdentityWithoutScalingTheTarget()
        {
            var (engine, model) = Cluster(); Tick(engine, model, 40);
            Assert.That(model.ReadyReplicas, Is.EqualTo(2)); string old = model.Pods[0].NodeId;
            engine.InjectFailure(old); Assert.That(model.ActualReplicas, Is.EqualTo(1));
            Tick(engine, model, 40);
            Assert.That(engine.FindNode(old), Is.Null); Assert.That(model.Pods.Any(p => p.NodeId == old), Is.False);
            Assert.That(model.DesiredReplicas, Is.EqualTo(2)); Assert.That(model.ReadyReplicas, Is.EqualTo(2));
            Assert.That(model.Replacements, Is.EqualTo(1)); Assert.That(model.ScaleChanges, Is.Zero);
        }
        [Test]
        public void ExplicitScalingChangesCountAndHighTrafficDoesNotEnableHpa()
        {
            var (engine, model) = Cluster(); model.SetDesired(4); Tick(engine, model, 40);
            Assert.That(model.ActualReplicas, Is.EqualTo(4)); Assert.That(model.ReadyReplicas, Is.EqualTo(4));
            Assert.That(model.ScaleChanges, Is.EqualTo(1)); Assert.That(model.Replacements, Is.Zero);
            engine.AddNode(new SimulationNode("f", NodeType.Frontend) { Capacity = 1000 }); engine.SetTargets("f", new[] { "cluster" });
            engine.RequestsPerSecond = 1000; Tick(engine, model, 100);
            Assert.That(model.DesiredReplicas, Is.EqualTo(4)); Assert.That(model.Pods.Count, Is.EqualTo(4));
            model.SetDesired(1); Tick(engine, model, 30);
            Assert.That(model.ActualReplicas, Is.EqualTo(1)); Assert.That(model.Replacements, Is.Zero);
        }
        [Test]
        public void MissingOrFailedWorkersLeavePendingPodsRatherThanCreatingFakeCapacity()
        {
            var (engine, model) = Cluster(0); model.SetDesired(8); Tick(engine, model, 20);
            Assert.That(model.ActualReplicas, Is.Zero); Assert.That(model.LastAction, Does.Contain("Pending"));
            engine.AddNode(new SimulationNode("w", NodeType.KubernetesNode)); model.AddWorker("w"); Tick(engine, model, 40);
            Assert.That(model.ActualReplicas, Is.EqualTo(4)); Assert.That(model.DesiredReplicas, Is.EqualTo(8));
            engine.InjectFailure("w"); Tick(engine, model, 20);
            Assert.That(model.ActualReplicas, Is.Zero); Assert.That(model.Workers.Count, Is.EqualTo(1));
            Assert.That(model.DesiredReplicas, Is.EqualTo(8)); Assert.That(model.LastAction, Does.Contain("Pending"));
        }
        [Test]
        public void NewPodsHaveReadinessDelayAndResetClockDoesNotFreezeTheController()
        {
            var (engine, model) = Cluster(); model.Reconcile(0);
            Assert.That(model.ActualReplicas, Is.EqualTo(2)); Assert.That(model.ReadyReplicas, Is.Zero);
            Tick(engine, model, 60); engine.Reset(); model.ResetObservations(); model.SetDesired(3); Tick(engine, model, 40);
            Assert.That(model.ActualReplicas, Is.EqualTo(3)); Assert.That(model.ReadyReplicas, Is.EqualTo(3));
            Assert.That(model.ScaleChanges, Is.EqualTo(1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => model.SetDesired(9));
        }
    }
}
