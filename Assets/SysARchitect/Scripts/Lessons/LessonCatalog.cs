using System;
using System.Collections.Generic;

namespace SysARchitect.Lessons
{
    public enum LessonId { ClientServer, LoadBalancing, Database, Docker, Kubernetes }
    public sealed class LessonDefinition
    {
        public LessonId Id { get; }
        public string Title { get; }
        public string Objectives { get; }
        public string Explanation { get; }
        public IReadOnlyList<string> Steps { get; }
        public IReadOnlyList<string> Algorithm { get; }
        public string Challenge { get; }
        public LessonDefinition(LessonId id, string title, string objectives, string explanation, string[] steps, string[] algorithm, string challenge)
        { Id = id; Title = title; Objectives = objectives; Explanation = explanation; Steps = Array.AsReadOnly(steps); Algorithm = Array.AsReadOnly(algorithm); Challenge = challenge; }
    }
    public static class LessonCatalog
    {
        public static readonly IReadOnlyList<LessonDefinition> All = Array.AsReadOnly(new[] {
            new LessonDefinition(LessonId.ClientServer, "Client-server architecture",
                "Build a directed client/service/storage path; explain the three responsibilities; complete at least 20 real requests.",
                "The frontend presents the application and initiates requests. A backend applies application logic; a database stores records. Separating these responsibilities lets each tier change independently, but every dependency adds latency and can fail. Arrows here show logical requests; replies are implicit. Placing components alone does not connect them. A successful read can return no record without being a transport failure.",
                new[] { "Load the example: three disconnected nodes. Close the guide to use the builder.", "Connect Frontend > Backend, then Backend > Database. Use source-first order.", "Run at 30 requests/s. Select nodes to compare CPU, queues, latency and database reads/writes.", "Reopen this guide and Check challenge after at least 20 successes." },
                new[] { "client.send(request)", "backend.queueAndProcess(request)", "database.execute(readOrWrite, key)", "completeResponse(result)", "monitor(capacity, queue, errors)" },
                "Connect all three tiers and complete 20 requests, including database reads and writes."),
            new LessonDefinition(LessonId.LoadBalancing, "Load balancing and horizontal scaling",
                "Route to two backends; observe work at each; demonstrate healthy-target failover and recovery; complete 100 requests.",
                "An application load balancer selects one healthy backend for each request. More backend instances can increase aggregate processing capacity, provided the frontend, balancer and database have headroom. A balancer alone creates no capacity. Our round-robin policy skips unavailable instances; surviving servers can still overload after a failure. Horizontal scaling adds instances, while vertical scaling increases one instance's capacity.",
                new[] { "The example has one 40 requests/s backend behind an application balancer and 80 requests/s traffic.", "Add a second Backend. Connect Load Balancer > second Backend and second Backend > Database.", "Run until both backends process at least 20 requests. Select one, Inject failure, and observe traffic reach the other.", "Restart the failed backend, advance at least one simulated second, and Check challenge after 100 successes." },
                new[] { "receiveRequest()", "healthy = connectedTargets.whereAvailable()", "target = roundRobin(healthy)", "target.processWithinCapacity()", "completeOrExplainFailure()" },
                "Use two healthy connected backends, observe failover while one was crashed, then restore it; both must process at least 20 requests."),
            new LessonDefinition(LessonId.Database, "Database replication and sharding",
                "Observe primary writes, replica reads and copy lag; then distribute records across at least three shards of the same cluster.",
                "Replication keeps copies of one logical dataset. In this single-primary model, writes go to the primary and asynchronous copies reach replicas later; reads may be stale during that delay. Replicas can add read capacity but do not multiply primary write capacity. Sharding instead partitions records by a deterministic key rule. Each shard stores only its assigned keys. The same expandable Database models both patterns, behind an explicitly configured Database Router.",
                new[] { "Load the example with Backend > Database Router > Database. Run frontend traffic.", "Select Database, choose Replication and keep at least two replicas. Observe primary writes, replica reads and pending copies.", "Choose Sharding on that same Database and set at least three shards. Run until records reach at least two shards.", "Reopen the guide and Check challenge. Read/Write explorer buttons enter Database directly; frontend traffic demonstrates the router." },
                new[] { "routeDatabaseRequest(key)", "if replication && write: usePrimary()", "if replication && read: chooseHealthyReplica()", "propagateAfterDelayAndExposeLag()", "if sharded: routeByNormalizedKeyModulo()", "completeDatabaseResult()" },
                "Observe replicated reads/writes and pending copy lag, then use three or more shards with records on multiple partitions of that same database."),
            new LessonDefinition(LessonId.Docker, "Docker containers and images",
                "Create two instances from one image; change an instance limit; stop/start and crash/restart an instance; keep real traffic flowing.",
                "An image is a reusable read-only package used to create container instances. Each container has its own process state, queues and resource limits while sharing a host kernel. This differs from a VM with a separate guest kernel. One container's failure need not crash its siblings, but a shared host remains a common dependency. Our quotas and memory are an educational model of isolation and contention; no real Docker daemon runs.",
                new[] { "Select the example Docker Host. Create two containers without changing the chosen image.", "Choose a container with the member slider and change its CPU quota or memory limit.", "Stop it, Start it, and Run/Step through one-second recovery. Then Crash and Restart that same instance.", "Run until at least 20 requests succeed and both instances are available; Check challenge." },
                new[] { "image = chooseReadOnlyTemplate()", "instance = createIsolatedInstance(image)", "applyInstanceResourceLimits()", "stopOrRestartThatInstance()", "dispatchToHealthyOwnedInstance()", "monitorSharedHostAndIsolatedQueues()" },
                "Create two instances of one image, adjust a limit, observe stop/start and recovery from a crash, and complete 20 requests with both available."),
            new LessonDefinition(LessonId.Kubernetes, "Kubernetes orchestration and recovery",
                "Scale an explicit deployment target to three; fail a Pod and observe a new UID; restore three ready replicas and complete 20 requests.",
                "A cluster contains worker machines. Pods are schedulable groups of one or more containers, and a Deployment describes a desired set of interchangeable application replicas. A controller compares desired and actual state and replaces terminally failed Pods. This replacement maintains the target; it does not increase it. Horizontal Pod Autoscaling requires a separately configured HPA and metrics. This exercise uses manual replica changes, a bounded scheduler and a simulated Service data path, not API-server traffic.",
                new[] { "The example has two workers and a deployment target of two. Run/Step until Pods are ready.", "Set explicit desired replicas to three. Observe actual versus ready counts; new readiness takes one second.", "Fail a Pod. Note its UID and observe a replacement with a new UID while desired remains three.", "Run until three Pods are ready and 20 requests succeed, then Check challenge. HPA remains unconfigured." },
                new[] { "observe(desired, pods)", "removeFailedPods()", "compare(actual, desired)", "scheduleNewPodOnHealthyWorker()", "terminateExcessPods()", "reportPendingCapacity()" },
                "Change the desired target to three, observe a failure replacement, and reach three ready Pods with real successful requests."),
        });
        public static LessonDefinition Get(LessonId id) { foreach (var lesson in All) if (lesson.Id == id) return lesson; throw new ArgumentOutOfRangeException(nameof(id)); }
    }
}
