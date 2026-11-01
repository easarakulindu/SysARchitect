using System;
using System.Collections.Generic;
using SysARchitect.Core;

namespace SysARchitect.Lessons
{
    public enum LessonId { ClientServer, LoadBalancing, Database, Docker, Kubernetes }
    /// <summary>Presentation details for the existing indexed instructions; not simulation actions.</summary>
    public sealed class LessonStepDetail
    {
        public string Heading { get; }
        public string Explanation { get; }
        public IReadOnlyList<NodeType> FocusTypes { get; }
        public LessonStepDetail(string heading, string explanation, params NodeType[] focusTypes)
        { Heading = heading; Explanation = explanation; FocusTypes = Array.AsReadOnly(focusTypes); }
    }
    public sealed class LessonDefinition
    {
        public LessonId Id { get; }
        public string Title { get; }
        public string Objectives { get; }
        public string Explanation { get; }
        public IReadOnlyList<string> Steps { get; }
        public IReadOnlyList<LessonStepDetail> StepDetails { get; }
        public IReadOnlyList<string> Algorithm { get; }
        public string Challenge { get; }
        public LessonDefinition(LessonId id, string title, string objectives, string explanation, string[] steps, string[] algorithm, string challenge, LessonStepDetail[] stepDetails)
        {
            if (stepDetails == null || stepDetails.Length != steps.Length) throw new ArgumentException("Each instruction needs matching step details.", nameof(stepDetails));
            Id = id; Title = title; Objectives = objectives; Explanation = explanation; Steps = Array.AsReadOnly(steps);
            Algorithm = Array.AsReadOnly(algorithm); Challenge = challenge; StepDetails = Array.AsReadOnly(stepDetails);
        }
    }
    public static class LessonCatalog
    {
        public static readonly IReadOnlyList<LessonDefinition> All = Array.AsReadOnly(new[] {
            new LessonDefinition(LessonId.ClientServer, "Client-server architecture",
                "Build a directed client/service/storage path; explain the three responsibilities; complete at least 20 real requests.",
                "The frontend presents the application and initiates requests. A backend applies application logic; a database stores records. Separating these responsibilities lets each tier change independently, but every dependency adds latency and can fail. Arrows here show logical requests; replies are implicit. Placing components alone does not connect them. A successful read can return no record without being a transport failure.",
                new[] { "Load the example: three disconnected nodes. Close the guide to use the builder.", "Connect Frontend > Backend, then Backend > Database. Use source-first order.", "Run at 30 requests/s. Select nodes to compare CPU, queues, latency and database reads/writes.", "Reopen this guide and Check challenge after at least 20 successes." },
                new[] { "client.send(request)", "backend.queueAndProcess(request)", "database.execute(readOrWrite, key)", "completeResponse(result)", "monitor(capacity, queue, errors)" },
                "Connect all three tiers and complete 20 requests, including database reads and writes.", new[] {
                    new LessonStepDetail("Inspect the three tiers", "The example deliberately starts disconnected. Frontend, Backend and Database have different jobs; placing them does not create a request path. Loading an example is an explicit action that replaces your workspace.", NodeType.Frontend, NodeType.Backend, NodeType.Database),
                    new LessonStepDetail("Connect the request path", "A directed Frontend-to-Backend link reaches application logic; Backend-to-Database reaches storage. Connect the actual nodes before running. Replies are implicit, so a second reverse arrow is unnecessary.", NodeType.Frontend, NodeType.Backend, NodeType.Database),
                    new LessonStepDetail("Measure work through each tier", "Run or Step the simulation explicitly. CPU, queue length and latency report actual work at the selected node. Database reads and writes demonstrate that traffic reached storage rather than stopping at a disconnected tier.", NodeType.Backend, NodeType.Database),
                    new LessonStepDetail("Verify end-to-end success", "Check challenge evaluates the connected architecture and at least 20 successful requests, including reads and writes. Reading this instruction does not submit requests or complete the challenge.", NodeType.Frontend, NodeType.Backend, NodeType.Database)
                }),
            new LessonDefinition(LessonId.LoadBalancing, "Load balancing and horizontal scaling",
                "Route to two backends; observe work at each; demonstrate healthy-target failover and recovery; complete 100 requests.",
                "An application load balancer selects one healthy backend for each request. More backend instances can increase aggregate processing capacity, provided the frontend, balancer and database have headroom. A balancer alone creates no capacity. Our round-robin policy skips unavailable instances; surviving servers can still overload after a failure. Horizontal scaling adds instances, while vertical scaling increases one instance's capacity.",
                new[] { "The example has one 40 requests/s backend behind an application balancer and 80 requests/s traffic.", "Add a second Backend. Connect Load Balancer > second Backend and second Backend > Database.", "Run until both backends process at least 20 requests. Select one, Inject failure, and observe traffic reach the other.", "Restart the failed backend, advance at least one simulated second, and Check challenge after 100 successes." },
                new[] { "receiveRequest()", "healthy = connectedTargets.whereAvailable()", "target = roundRobin(healthy)", "target.processWithinCapacity()", "completeOrExplainFailure()" },
                "Use two healthy connected backends, observe failover while one was crashed, then restore it; both must process at least 20 requests.", new[] {
                    new LessonStepDetail("Find the capacity bottleneck", "The example offers 80 requests/s to one 40 requests/s backend. The application balancer can choose a target, but cannot create processing capacity. Watch the backend queue and overload under this offered load.", NodeType.LoadBalancer, NodeType.Backend),
                    new LessonStepDetail("Add a second processing instance", "Connect the added backend to both the application balancer and storage. Two 40 requests/s instances provide 80 requests/s of aggregate backend capacity when the other tiers have headroom; each still has its own queue and limits.", NodeType.LoadBalancer, NodeType.Backend, NodeType.Database),
                    new LessonStepDetail("Observe healthy-target failover", "Inject failure explicitly on one backend. New requests skip that unavailable target and reach its healthy sibling. The remaining 40 requests/s instance can still overload; failover does not replace the lost capacity.", NodeType.LoadBalancer, NodeType.Backend),
                    new LessonStepDetail("Recover and verify distribution", "Restart explicitly, then Run or Step through recovery. Check challenge requires real work on both backends, observed failover and recovery, and 100 successes. Navigating here does not restart anything.", NodeType.LoadBalancer, NodeType.Backend)
                }),
            new LessonDefinition(LessonId.Database, "Database replication and sharding",
                "Observe primary writes, replica reads and copy lag; then distribute records across at least three shards of the same cluster.",
                "Replication keeps copies of one logical dataset. In this single-primary model, writes go to the primary and asynchronous copies reach replicas later; reads may be stale during that delay. Replicas can add read capacity but do not multiply primary write capacity. Sharding instead partitions records by a deterministic key rule. Each shard stores only its assigned keys. The same expandable Database models both patterns behind a Database Router. A separate backend-controlled cache-aside path can avoid database reads on repeated cache hits.",
                new[] { "Load the example with Backend > Database Router > Database. Run frontend traffic.", "Select Database, choose Replication and keep at least two replicas. Observe primary writes, replica reads and pending copies.", "Choose Sharding on that same Database and set at least three shards. Run until records reach at least two shards.", "Reopen the guide and Check challenge. Read/Write explorer buttons enter Database directly; frontend traffic demonstrates the router." },
                new[] { "routeDatabaseRequest(key)", "if replication && write: usePrimary()", "if replication && read: chooseHealthyReplica()", "propagateAfterDelayAndExposeLag()", "if sharded: routeByNormalizedKeyModulo()", "completeDatabaseResult()" },
                "Observe replicated reads/writes and pending copy lag, then use three or more shards with records on multiple partitions of that same database.", new[] {
                    new LessonStepDetail("Follow the database router", "The Backend sends database requests through a Load Balancer configured for Database Routing. Frontend traffic exercises that connected path; the Database explorer's direct Read/Write controls bypass the router.", NodeType.Backend, NodeType.LoadBalancer, NodeType.Database),
                    new LessonStepDetail("Compare copies and synchronization lag", "Select Replication explicitly on the Database. Writes use the primary; eligible reads prefer healthy replicas. Copies arrive after the model's 0.5-second delay, so a replica can temporarily return an older result. Replicas do not multiply primary write capacity.", NodeType.LoadBalancer, NodeType.Database),
                    new LessonStepDetail("Partition records with shard keys", "Switch that same Database to Sharding explicitly. The normalized key modulo shard count selects a partition deterministically; shards hold different keys rather than copies. Run traffic until at least two of three or more shards contain records.", NodeType.Database),
                    new LessonStepDetail("Verify both database configurations", "Check challenge observes replication activity and pending-copy lag before checking the later sharded state on the same Database. It does not count a mode change alone. Use frontend requests when demonstrating routing through the Database Router.", NodeType.Backend, NodeType.LoadBalancer, NodeType.Database)
                }),
            new LessonDefinition(LessonId.Docker, "Docker containers and images",
                "Create two instances from one image; change an instance limit; stop/start and crash/restart an instance; keep real traffic flowing.",
                "An image is a reusable read-only package used to create container instances. Each container has its own process state, queues and resource limits while sharing a host kernel. This differs from a VM with a separate guest kernel. One container's failure need not crash its siblings, but a shared host remains a common dependency. Our quotas and memory are an educational model of isolation and contention; no real Docker daemon runs.",
                new[] { "Select the example Docker Host. Create two containers without changing the chosen image.", "Choose a container with the member slider and change its CPU quota or memory limit.", "Stop it, Start it, and Run/Step through one-second recovery. Then Crash and Restart that same instance.", "Run until at least 20 requests succeed and both instances are available; Check challenge." },
                new[] { "image = chooseReadOnlyTemplate()", "instance = createIsolatedInstance(image)", "applyInstanceResourceLimits()", "stopOrRestartThatInstance()", "dispatchToHealthyOwnedInstance()", "monitorSharedHostAndIsolatedQueues()" },
                "Create two instances of one image, adjust a limit, observe stop/start and recovery from a crash, and complete 20 requests with both available.", new[] {
                    new LessonStepDetail("Create instances from one image", "The Docker Host panel chooses an image template and creates separate container instances. Two instances from the same image share that template, but have separate identity, state and resource limits. No real Docker daemon is started.", NodeType.DockerHost, NodeType.Container),
                    new LessonStepDetail("Set limits for one container", "Choose one instance with the member slider, then change its CPU quota or memory limit. Those limits affect that instance's simulated processing and memory behavior; the host's finite resources are still shared.", NodeType.Container),
                    new LessonStepDetail("Compare stop/start and crash/restart", "Use Stop/Start and Crash/Restart explicitly on the chosen instance. Advance simulation time through one-second recovery and observe its state. These controls affect the same instance; changing instruction steps does not execute them.", NodeType.DockerHost, NodeType.Container),
                    new LessonStepDetail("Check work on the shared host", "Run frontend requests through the host to available containers. Check challenge requires both instances, the changed limit, observed stop/start and crash recovery, and real successes after recovery. A host failure remains a shared dependency.", NodeType.Frontend, NodeType.DockerHost, NodeType.Container)
                }),
            new LessonDefinition(LessonId.Kubernetes, "Kubernetes orchestration and recovery",
                "Scale an explicit deployment target to three; fail a Pod and observe a new UID; restore three ready replicas and complete 20 requests.",
                "A cluster contains worker machines. Pods are schedulable groups of one or more containers, and a Deployment describes a desired set of interchangeable application replicas. A controller compares desired and actual state and replaces terminally failed Pods. This replacement maintains the target; it does not increase it. Horizontal Pod Autoscaling requires a separately configured HPA and metrics. This exercise uses manual replica changes, a bounded scheduler and a simulated Service data path, not API-server traffic.",
                new[] { "The example has two workers and a deployment target of two. Run/Step until Pods are ready.", "Set explicit desired replicas to three. Observe actual versus ready counts; new readiness takes one second.", "Fail a Pod. Note its UID and observe a replacement with a new UID while desired remains three.", "Run until three Pods are ready and 20 requests succeed, then Check challenge. HPA remains unconfigured." },
                new[] { "observe(desired, pods)", "removeFailedPods()", "compare(actual, desired)", "scheduleNewPodOnHealthyWorker()", "terminateExcessPods()", "reportPendingCapacity()" },
                "Change the desired target to three, observe a failure replacement, and reach three ready Pods with real successful requests.", new[] {
                    new LessonStepDetail("Inspect desired, actual and ready counts", "The example has two workers and a desired replica target of two. Actual Pods can exist before they become ready. Run or Step explicitly: readiness takes one simulated second, and only eligible healthy instances serve work.", NodeType.KubernetesCluster, NodeType.KubernetesNode, NodeType.KubernetesPod),
                    new LessonStepDetail("Scale the explicit replica target", "Set desired replicas to three in Kubernetes Explorer. The bounded scheduler creates Pods on workers with capacity; actual and ready counts can lag the target. This is manual scaling, not automatic HPA behavior.", NodeType.KubernetesCluster, NodeType.KubernetesNode, NodeType.KubernetesPod),
                    new LessonStepDetail("Observe replacement with a new identity", "Fail a Pod explicitly and advance the simulation. The controller removes the terminally failed Pod and creates a replacement with a new identity while desired remains three. Replacement restores the target; it does not increase it.", NodeType.KubernetesNode, NodeType.KubernetesPod),
                    new LessonStepDetail("Verify readiness and real requests", "Wait for three ready replicas and at least 20 successful requests, then Check challenge. Traffic uses the simulated Service path, not a real API server. No HPA, real cluster deployment or automatic demonstration action is implied.", NodeType.Frontend, NodeType.KubernetesCluster, NodeType.KubernetesNode, NodeType.KubernetesPod)
                }),
        });
        public static LessonDefinition Get(LessonId id) { foreach (var lesson in All) if (lesson.Id == id) return lesson; throw new ArgumentOutOfRangeException(nameof(id)); }
    }
}
