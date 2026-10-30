using System;
using System.Linq;
using NUnit.Framework;
using SysARchitect.Core;
using SysARchitect.Simulation;

namespace SysARchitect.Tests
{
    public class DatabaseModelTests
    {
        [Test]
        public void ReplicationCopiesRecordsAfterExplicitSynchronizationDelay()
        {
            var model = new DatabaseModel();
            model.Configure(DatabaseMode.Replication, 2);
            Assert.That(model.Execute(true, 42, out _), Is.True);
            Assert.That(model.Members[0].Records.ContainsKey(42), Is.True);
            Assert.That(model.Members[1].Records.ContainsKey(42), Is.False);
            Assert.That(model.PendingUpdates, Is.EqualTo(2));
            model.Execute(false, 42, out _);
            Assert.That(model.LastOperation, Does.Contain("lagging"));
            model.AdvanceTo(.49);
            Assert.That(model.Members[1].Records, Is.Empty);
            model.AdvanceTo(.5);
            foreach (var member in model.Members) Assert.That(member.Records[42], Is.EqualTo(1));
            Assert.That(model.PendingUpdates, Is.Zero);
            Assert.That(model.LogicalRecordCount, Is.EqualTo(1));
        }

        [Test]
        public void AddingReplicaAndSwitchingModesPreservesLogicalData()
        {
            var model = new DatabaseModel();
            for (int key = 0; key < 12; key++) model.Execute(true, key, out _);
            model.Configure(DatabaseMode.Replication, 1);
            model.AdvanceTo(.5);
            model.Configure(DatabaseMode.Replication, 3);
            model.AdvanceTo(1);
            Assert.That(model.Members.All(m => m.Records.Count == 12), Is.True);
            model.Configure(DatabaseMode.Sharding, 3);
            Assert.That(model.Members.All(m => m.Records.Count == 4), Is.True);
            Assert.That(model.Members.Sum(m => m.Records.Count), Is.EqualTo(12));
            model.Configure(DatabaseMode.Single, 1);
            Assert.That(model.Members[0].Records.Count, Is.EqualTo(12));
        }

        [TestCase(-7, 3, 2)]
        [TestCase(42, 4, 2)]
        [TestCase(int.MinValue, 3, 1)]
        public void ShardKeysRouteDeterministically(int key, int count, int expected)
        {
            var model = new DatabaseModel();
            model.Configure(DatabaseMode.Sharding, count);
            Assert.That(DatabaseModel.ShardFor(key, count), Is.EqualTo(expected));
            model.Execute(true, key, out _);
            Assert.That(model.LastMemberIndex, Is.EqualTo(expected));
            Assert.That(model.Members[expected].Records.ContainsKey(key), Is.True);
            Assert.That(model.Members.Sum(m => m.Records.Count), Is.EqualTo(1));
        }

        [Test]
        public void AFailedShardOnlyRejectsKeysRoutedToThatShard()
        {
            var model = new DatabaseModel();
            model.Configure(DatabaseMode.Sharding, 3);
            model.FailMember(1);
            Assert.That(model.Execute(true, 4, out string reason), Is.False);
            Assert.That(reason, Does.Contain("Shard 1"));
            Assert.That(model.Execute(true, 5, out _), Is.True);
            model.RecoverMember(1);
            model.AdvanceTo(.5);
            Assert.That(model.Members[1].Status, Is.EqualTo(NodeStatus.Recovering));
            model.AdvanceTo(1);
            Assert.That(model.Execute(true, 4, out _), Is.True);
        }

        [Test]
        public void ReplicaRecoversAndCatchesUpWithoutUnboundedOfflineBacklog()
        {
            var model = new DatabaseModel();
            model.Configure(DatabaseMode.Replication, 2);
            model.FailMember(1);
            for (int i = 0; i < 100; i++) model.Execute(true, 42, out _);
            model.AdvanceTo(.5);
            Assert.That(model.PendingUpdates, Is.Zero);
            Assert.That(model.Members[1].Records, Is.Empty);
            model.RecoverMember(1);
            model.AdvanceTo(1.5);
            Assert.That(model.Members[1].Status, Is.EqualTo(NodeStatus.Healthy));
            Assert.That(model.PendingFor(1), Is.EqualTo(1));
            model.AdvanceTo(2);
            Assert.That(model.Members[1].Records[42], Is.EqualTo(100));
            model.FailMember(0);
            Assert.That(model.Execute(true, 43, out _), Is.False);
            Assert.That(model.Execute(false, 42, out _), Is.True);
            Assert.That(model.LastMemberIndex, Is.Not.Zero);
        }

        [Test]
        public void ResetPreservesConfigurationButClearsRecordsHealthAndCounters()
        {
            var model = new DatabaseModel();
            model.Configure(DatabaseMode.Sharding, 4);
            model.Execute(true, 7, out _);
            model.FailMember(3);
            model.Reset();
            Assert.That(model.Mode, Is.EqualTo(DatabaseMode.Sharding));
            Assert.That(model.Members.Count, Is.EqualTo(4));
            Assert.That(model.Members.All(m => m.Records.Count == 0 && m.Status == NodeStatus.Healthy), Is.True);
            Assert.That(model.Writes, Is.Zero);
        }

        [Test]
        public void ExplicitDatabaseRequestsUseExistingCapacityQueuesFailureAndAccounting()
        {
            var engine = new SimulationEngine { RequestsPerSecond = 0 };
            var node = new SimulationNode("db", NodeType.Database);
            node.Database.Configure(DatabaseMode.Sharding, 3);
            engine.AddNode(node);
            engine.SubmitDatabaseRequest("db", true, 4);
            engine.Step(); engine.Step();
            Assert.That(engine.Succeeded, Is.EqualTo(1));
            Assert.That(node.Database.Members[1].Records.ContainsKey(4), Is.True);
            node.Database.FailMember(1);
            engine.SubmitDatabaseRequest("db", false, 4);
            engine.Step(); engine.Step();
            Assert.That(engine.Failed, Is.EqualTo(1));
            Assert.That(engine.LastFailureReason, Does.Contain("Shard 1"));
            Assert.That(node.Status, Is.Not.EqualTo(NodeStatus.Crashed));
            node.Capacity = 0; node.TimeoutMs = 100;
            engine.SubmitDatabaseRequest("db", true, 2);
            for (int i = 0; i < 4; i++) engine.Step();
            Assert.That(engine.TimedOut, Is.EqualTo(1));
            Assert.That(engine.Generated, Is.EqualTo(engine.Succeeded + engine.Failed));
            Assert.That(engine.InFlight, Is.Zero);
        }

        [Test]
        public void RoutedTrafficDistributesWritesAcrossShardsAndPreservesAccounting()
        {
            var engine = new SimulationEngine { RequestsPerSecond = 80 };
            engine.AddNode(new SimulationNode("frontend", NodeType.Frontend) { Capacity = 500 });
            engine.AddNode(new SimulationNode("backend", NodeType.Backend) { Capacity = 500 });
            var db = new SimulationNode("database", NodeType.Database) { Capacity = 500 };
            db.Database.Configure(DatabaseMode.Sharding, 4);
            engine.AddNode(db);
            engine.SetTargets("frontend", new[] { "backend" });
            engine.SetTargets("backend", new[] { "database" });
            for (int i = 0; i < 100; i++) engine.Step();
            Assert.That(db.Database.Members.All(m => m.Writes > 0), Is.True);
            Assert.That(engine.Failed, Is.Zero);
            Assert.That(engine.Generated, Is.EqualTo(engine.Succeeded + engine.Nodes.Sum(n => n.QueueLength + n.ProcessingCount)));
        }

        [Test]
        public void WholeDatabaseFailureStopsReplicationUntilRecovery()
        {
            var engine = new SimulationEngine { RequestsPerSecond = 0 };
            var node = new SimulationNode("db", NodeType.Database);
            node.Database.Configure(DatabaseMode.Replication, 1);
            engine.AddNode(node);
            engine.SubmitDatabaseRequest("db", true, 7);
            engine.Step(); engine.Step();
            engine.InjectFailure("db");
            for (int i = 0; i < 20; i++) engine.Step();
            Assert.That(node.Database.Members[1].Records, Is.Empty);
            engine.Restart("db");
            for (int i = 0; i < 21; i++) engine.Step();
            Assert.That(node.Database.Members[1].Records.ContainsKey(7), Is.True);
        }

        [Test]
        public void DatabaseAddedAfterSimulationStartsUsesTheCurrentClockForRecovery()
        {
            var engine = new SimulationEngine { RequestsPerSecond = 0 };
            for (int i = 0; i < 200; i++) engine.Step();
            var node = new SimulationNode("late-db", NodeType.Database);
            engine.AddNode(node);
            Assert.That(node.Database.Clock, Is.EqualTo(engine.Time));
            node.Database.FailMember(0);
            node.Database.RecoverMember(0);
            for (int i = 0; i < 10; i++) engine.Step();
            Assert.That(node.Database.Members[0].Status, Is.EqualTo(NodeStatus.Recovering));
            for (int i = 0; i < 11; i++) engine.Step();
            Assert.That(node.Database.Members[0].Status, Is.EqualTo(NodeStatus.Healthy));
        }

        [Test]
        public void DatabaseClockCannotMoveBackwards()
        {
            var model = new DatabaseModel();
            model.AdvanceTo(1);
            Assert.Throws<ArgumentOutOfRangeException>(() => model.AdvanceTo(.5));
        }
    }
}
