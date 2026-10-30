using System.Linq;
using NUnit.Framework;
using SysARchitect.Core;
using SysARchitect.Graph;
using SysARchitect.Simulation;

namespace SysARchitect.Tests
{
    public class RoutingAndCacheTests
    {
        private static SimulationNode Node(SimulationEngine engine, string id, NodeType type, double capacity = 1000)
        {
            var node = new SimulationNode(id, type) { Capacity = capacity, QueueCapacity = 1000, MemoryLimitMB = 8192 };
            engine.AddNode(node); return node;
        }
        private static SimulationEngine DatabaseRoute(int replicas = 2)
        {
            var engine = new SimulationEngine { RequestsPerSecond = 0 };
            Node(engine, "f", NodeType.Frontend); Node(engine, "b", NodeType.Backend);
            Node(engine, "router", NodeType.LoadBalancer).RoutingRole = LoadBalancerRole.Database;
            var db = Node(engine, "db", NodeType.Database, 40);
            db.Database.Configure(DatabaseMode.Replication, replicas);
            db.Database.Execute(true, 1, out _);
            engine.SetTargets("f", new[] { "b" }); engine.SetTargets("b", new[] { "router" }); engine.SetTargets("router", new[] { "db" });
            Tick(engine, 20); return engine;
        }
        private static SimulationEngine Cached(bool cache = true)
        {
            var engine = new SimulationEngine { RequestsPerSecond = 0 };
            Node(engine, "f", NodeType.Frontend); Node(engine, "lb", NodeType.LoadBalancer);
            Node(engine, "b", NodeType.Backend); Node(engine, "cache", NodeType.Cache);
            Node(engine, "db", NodeType.Database).Database.Execute(true, 1, out _);
            engine.SetTargets("f", new[] { "lb" }); engine.SetTargets("lb", new[] { "b" });
            engine.SetTargets("b", cache ? new[] { "cache", "db" } : new[] { "db" });
            return engine;
        }
        private static void Tick(SimulationEngine engine, int count)
        {
            for (int i = 0; i < count; i++)
            {
                engine.Step();
                Assert.That(engine.Generated, Is.EqualTo(engine.Succeeded + engine.Failed + engine.Nodes.Sum(n => (long)n.QueueLength + n.ProcessingCount)));
            }
        }
        private static void Drain(SimulationEngine engine)
        {
            for (int i = 0; i < 200 && engine.InFlight > 0; i++) Tick(engine, 1);
            Assert.That(engine.InFlight, Is.Zero);
        }

        [Test]
        public void SupportedRolesAcceptBackendDatabaseRoutingWithoutArbitraryLinks()
        {
            Assert.That(ConnectionRules.Validate(NodeType.Backend, NodeType.LoadBalancer).isValid, Is.False);
            Assert.That(ConnectionRules.Validate(NodeType.Backend, NodeType.LoadBalancer, targetRole: LoadBalancerRole.Database).isValid, Is.True);
            Assert.That(ConnectionRules.Validate(NodeType.LoadBalancer, NodeType.Database, LoadBalancerRole.Database).isValid, Is.True);
            Assert.That(ConnectionRules.Validate(NodeType.LoadBalancer, NodeType.Backend, LoadBalancerRole.Database).isValid, Is.False);
            Assert.That(ConnectionRules.Validate(NodeType.Frontend, NodeType.LoadBalancer, targetRole: LoadBalancerRole.Database).isValid, Is.False);
            Assert.That(ConnectionRules.Validate(NodeType.Cache, NodeType.Database).errorMessage, Does.Contain("Backend"));
        }

        [Test]
        public void ApplicationRoutingSkipsOfflineBackendAndAddedInstancesIncreaseThroughput()
        {
            var single = Cached(false); var pair = Cached(false);
            foreach (var engine in new[] { single, pair }) { engine.FindNode("b").Capacity = 40; engine.RequestsPerSecond = 120; }
            Node(pair, "b2", NodeType.Backend, 40); pair.SetTargets("b2", new[] { "db" }); pair.SetTargets("lb", new[] { "b", "b2" });
            Tick(single, 80); Tick(pair, 80);
            Assert.That(pair.Succeeded, Is.GreaterThan(single.Succeeded * 1.7));
            Assert.That(pair.FindNode("b").Completed, Is.EqualTo(pair.FindNode("b2").Completed).Within(1));
            long before = pair.FindNode("b").Completed; pair.InjectFailure("b"); Tick(pair, 40);
            Assert.That(pair.FindNode("b").Completed, Is.EqualTo(before));
            Assert.That(pair.FindNode("b2").Completed, Is.GreaterThan(before));
        }

        [Test]
        public void ReadReplicasIncreaseReadThroughputButDoNotIncreasePrimaryWriteCapacity()
        {
            var one = DatabaseRoute(1); var three = DatabaseRoute(3);
            foreach (var engine in new[] { one, three }) for (int i = 0; i < 500; i++) engine.SubmitRequest("f", false, 1);
            Tick(one, 80); Tick(three, 80);
            Assert.That(three.Succeeded, Is.GreaterThan(one.Succeeded * 2.5));
            Assert.That(three.FindNode("db").Database.Members[0].Reads, Is.Zero);
            Assert.That(three.FindNode("db").Database.Members.Skip(1).All(m => m.Reads > 0 && m.Cpu > .5), Is.True);
            one = DatabaseRoute(1); three = DatabaseRoute(3);
            foreach (var engine in new[] { one, three }) for (int i = 0; i < 500; i++) engine.SubmitRequest("f", true, 1);
            Tick(one, 80); Tick(three, 80);
            Assert.That(three.Succeeded, Is.EqualTo(one.Succeeded));
            Assert.That(three.Succeeded, Is.LessThanOrEqualTo(160));
            Assert.That(three.FindNode("db").Database.Members.Skip(1).All(m => m.Writes == 0), Is.True);
            Assert.That(three.FindNode("db").Database.Members[0].Cpu, Is.GreaterThan(.99));
        }

        [Test]
        public void DatabaseRouterPreservesStaleReadAndShardKeySemantics()
        {
            var engine = DatabaseRoute(); var db = engine.FindNode("db").Database;
            engine.SubmitRequest("f", true, 7); Drain(engine);
            engine.SubmitRequest("f", false, 7); Drain(engine);
            Assert.That(db.LastOperation, Does.Contain("lagging"));
            db.FailMember(1);
            engine.SubmitRequest("f", false, 1); Drain(engine);
            Assert.That(db.LastMemberIndex, Is.EqualTo(2));
            db.Configure(DatabaseMode.Sharding, 3);
            foreach (int key in new[] { -7, 0, 1, 8 }) { engine.SubmitRequest("f", true, key); Drain(engine); Assert.That(db.LastMemberIndex, Is.EqualTo(DatabaseModel.ShardFor(key, 3))); }
            db.FailMember(1); engine.SubmitRequest("f", true, 1); Drain(engine);
            Assert.That(engine.LastFailureReason, Does.Contain("Shard 1"));
        }

        [Test]
        public void CacheMissFallsBackPopulatesAndRepeatedHitsReduceDatabaseWork()
        {
            var engine = Cached(); var cache = engine.FindNode("cache").Cache; var db = engine.FindNode("db").Database;
            engine.SubmitRequest("f", false, 1); Drain(engine); double firstLatency = engine.AverageLatencyMs;
            Assert.That(cache.Misses, Is.EqualTo(1)); Assert.That(cache.Populations, Is.EqualTo(1)); Assert.That(db.Reads, Is.EqualTo(1));
            for (int i = 0; i < 10; i++) { engine.SubmitRequest("f", false, 1); Drain(engine); }
            Assert.That(cache.Hits, Is.EqualTo(10)); Assert.That(db.Reads, Is.EqualTo(1));
            Assert.That(engine.AverageLatencyMs, Is.LessThan(firstLatency));
            Assert.That(engine.Succeeded, Is.EqualTo(11)); Assert.That(engine.Failed, Is.Zero);
            var without = Cached(false);
            for (int i = 0; i < 11; i++) { without.SubmitRequest("f", false, 1); Drain(without); }
            Assert.That(without.FindNode("db").Database.Reads, Is.EqualTo(11));
        }

        [Test]
        public void WritesInvalidateCacheAndUnavailableCacheFallsBackToStorage()
        {
            var engine = Cached(); var cache = engine.FindNode("cache").Cache;
            engine.SubmitRequest("f", false, 1); Drain(engine);
            engine.SubmitRequest("f", true, 1); Drain(engine);
            Assert.That(cache.Count, Is.Zero); Assert.That(cache.Invalidations, Is.EqualTo(1));
            engine.SubmitRequest("f", false, 1); Drain(engine); Assert.That(cache.Populations, Is.EqualTo(2));
            engine.InjectFailure("cache"); engine.SubmitRequest("f", false, 1); Drain(engine);
            Assert.That(engine.Failed, Is.Zero); Assert.That(engine.FindNode("b").CacheBypasses, Is.EqualTo(1));
            Assert.That(cache.Count, Is.Zero);
        }

        [Test]
        public void CacheAsideUsesDatabaseRouterAndRecalculatesAfterDependencyDeletion()
        {
            var engine = Cached(); Node(engine, "router", NodeType.LoadBalancer).RoutingRole = LoadBalancerRole.Database;
            engine.SetTargets("router", new[] { "db" }); engine.SetTargets("b", new[] { "cache", "router" });
            engine.SubmitRequest("f", false, 1); Drain(engine); Assert.That(engine.FindNode("cache").Cache.Count, Is.EqualTo(1));
            engine.SetTargets("b", new[] { "router" }); engine.SubmitRequest("f", false, 1); Drain(engine);
            Assert.That(engine.FindNode("db").Database.Reads, Is.EqualTo(2));
            engine.SetTargets("b", new[] { "cache", "router" }); engine.SubmitRequest("f", false, 1); Drain(engine);
            Assert.That(engine.FindNode("cache").Cache.Hits, Is.EqualTo(1));
            engine.SetTargets("router", new string[0]); engine.SubmitRequest("f", false, 2); Drain(engine);
            Assert.That(engine.LastFailureReason, Does.Contain("Cache-aside"));
        }

        [Test]
        public void AReadRacingACommittedWriteCannotRepopulateAnOldCacheVersion()
        {
            var engine = Cached(); var cache = engine.FindNode("cache").Cache; var db = engine.FindNode("db").Database;
            engine.SubmitRequest("f", false, 1);
            for (int i = 0; i < 40 && db.Reads == 0; i++) Tick(engine, 1);
            Assert.That(db.Reads, Is.EqualTo(1));
            engine.FindNode("cache").Capacity = 0;
            engine.SubmitDatabaseRequest("db", true, 1); Tick(engine, 4);
            engine.FindNode("cache").Capacity = 1000; Drain(engine);
            Assert.That(cache.Count, Is.Zero, "The completed read held an older version than the committed write.");
            engine.SubmitRequest("f", false, 1); Drain(engine);
            Assert.That(cache.Read("db", 1, out long value), Is.True);
            Assert.That(value, Is.EqualTo(db.LogicalVersion(1)));
        }

        [Test]
        public void SharedCacheScopesKeysToTheirDatabaseAndOptionalFillFailureDoesNotFailTheRead()
        {
            var engine = Cached();
            Node(engine, "b2", NodeType.Backend); var db2 = Node(engine, "db2", NodeType.Database);
            db2.Database.Execute(true, 1, out _); db2.Database.Execute(true, 1, out _);
            engine.SetTargets("b2", new[] { "cache", "db2" }); engine.SetTargets("lb", new[] { "b", "b2" });
            for (int i = 0; i < 8; i++) { engine.SubmitRequest("f", false, 1); Drain(engine); }
            Assert.That(engine.FindNode("cache").Cache.Count, Is.EqualTo(2));
            Assert.That(engine.FindNode("db").Database.Reads, Is.EqualTo(1));
            Assert.That(db2.Database.Reads, Is.EqualTo(1));
            engine = Cached(); var db = engine.FindNode("db").Database;
            engine.SubmitRequest("f", false, 1);
            for (int i = 0; i < 40 && db.Reads == 0; i++) Tick(engine, 1);
            engine.InjectFailure("cache"); Drain(engine);
            Assert.That(engine.Succeeded, Is.EqualTo(1)); Assert.That(engine.Failed, Is.Zero);
        }
    }
}
