using System;
using System.Collections.Generic;
using System.Linq;
using SysARchitect.Lessons;

namespace SysARchitect.Quiz
{
    public enum QuizStage { Pre, Post }
    public sealed class QuizQuestion
    {
        public string Id { get; }
        public LessonId Lesson { get; }
        public QuizStage Stage { get; }
        public string Prompt { get; }
        public IReadOnlyList<string> Choices { get; }
        public int CorrectAnswer { get; }
        public string Explanation { get; }
        public QuizQuestion(string id, LessonId lesson, QuizStage stage, string prompt, string[] choices, int correct, string explanation)
        {
            if (choices == null || choices.Length != 4 || correct < 0 || correct >= choices.Length) throw new ArgumentException("A question needs four choices and one valid correct index.");
            Id = id; Lesson = lesson; Stage = stage; Prompt = prompt; Choices = Array.AsReadOnly(choices); CorrectAnswer = correct; Explanation = explanation;
        }
    }
    public static class QuizCatalog
    {
        public static readonly IReadOnlyList<QuizQuestion> All = Array.AsReadOnly(new[] {
            new QuizQuestion("cs-01", LessonId.ClientServer, QuizStage.Pre, "Which tier normally applies application/business logic in a three-tier design?",
                new[] { "The frontend only", "The backend service", "The network cable", "The database replica router only" }, 1,
                "The backend applies application logic. The frontend handles presentation and interaction; the database stores and queries data."),
            new QuizQuestion("cs-02", LessonId.ClientServer, QuizStage.Pre, "Nodes are placed, no arrows exist, and requests fail with 'No service route configured'. What should you do first?",
                new[] { "Increase every memory limit", "Add replicas to the frontend", "Create valid directed connections", "Hide failed-request counters" }, 2,
                "Placement does not create edges. Configure the request path before diagnosing capacity or failure handling."),
            new QuizQuestion("cs-03", LessonId.ClientServer, QuizStage.Post, "A backend can process 40 requests/s while 100 arrive. Increasing only frontend capacity usually does what?",
                new[] { "Leaves the backend bottleneck in place", "Doubles backend capacity", "Removes the need for a backend", "Guarantees zero latency" }, 0,
                "End-to-end capacity is bounded by constrained dependencies. Increasing an upstream limit does not increase the backend's processing budget."),
            new QuizQuestion("cs-04", LessonId.ClientServer, QuizStage.Post, "At one checkpoint, 120 requests were sent, 110 succeeded and 5 failed. How many remain in flight?",
                new[] { "0", "10", "115", "5" }, 3,
                "Request conservation gives in flight = sent - succeeded - failed, so 120 - 110 - 5 = 5. Animated packet counts are sampled visuals, not this total."),
            new QuizQuestion("lb-01", LessonId.LoadBalancing, QuizStage.Pre, "Two healthy backends each sustain 40 requests/s. With balanced work and headroom elsewhere, their ideal combined capacity is approximately…",
                new[] { "40 requests/s", "80 requests/s", "Unlimited", "20 requests/s" }, 1,
                "Independent backend capacity can add to about 80 requests/s. Real throughput still depends on balanced work and other bottlenecks."),
            new QuizQuestion("lb-02", LessonId.LoadBalancing, QuizStage.Pre, "One of two connected backend instances crashes. What should a health-aware load balancer do?",
                new[] { "Skip it and use operational targets, within their capacity", "Keep sending half the traffic to the crashed target", "Increase the surviving backend's physical resources automatically", "Turn database replicas into backend servers" }, 0,
                "Failover changes routing, not resource capacity. The surviving backend can still saturate under the increased share of traffic."),
            new QuizQuestion("lb-03", LessonId.LoadBalancing, QuizStage.Post, "You need Backend > Router > Database, but the Load Balancer is in Application Routing mode. Which correction is appropriate?",
                new[] { "Disable all graph validation", "Reverse every database write", "Select Database Routing, then create the supported edges", "Connect the frontend directly to a read-only replica" }, 2,
                "This builder keeps one configurable component with explicit application and database roles. Database Routing supports backend requests to one expandable database cluster."),
            new QuizQuestion("lb-04", LessonId.LoadBalancing, QuizStage.Post, "A single backend is saturated. The balancer already has spare capacity. Raising only the balancer's limit from 500 to 1000 requests/s will…",
                new[] { "Create another backend", "Double primary write capacity", "Make all queues disappear", "Leave backend capacity unchanged" }, 3,
                "The balancer distributes work. It does not manufacture processing capacity in an unchanged backend."),
            new QuizQuestion("db-01", LessonId.Database, QuizStage.Pre, "In single-primary replication, adding three read replicas changes primary write capacity in which way?",
                new[] { "It does not multiply the primary's write budget", "It makes writes four times faster automatically", "It permits random writes to read-only replicas", "It eliminates synchronization lag" }, 0,
                "All writes still use the primary. Replicas can help eligible read workloads; replication overhead and lag are separate concerns."),
            new QuizQuestion("db-02", LessonId.Database, QuizStage.Pre, "A write just committed on the primary. An asynchronous replica read may…",
                new[] { "Always return the new value immediately", "Write the value into a random shard", "Return an older value or not find it yet", "Prove the network request failed" }, 2,
                "Asynchronous replication has a propagation delay. A successful query can return stale data or no record while the replica is catching up."),
            new QuizQuestion("db-03", LessonId.Database, QuizStage.Post, "The educational sharding rule is normalized key modulo shard count. With three shards numbered 0–2, key 7 routes to…",
                new[] { "Shard 0", "Shard 1", "Shard 2", "All shards as a write broadcast" }, 1,
                "7 modulo 3 is 1. Sharding partitions keys; this model routes the operation to one deterministic partition rather than broadcasting writes."),
            new QuizQuestion("db-04", LessonId.Database, QuizStage.Post, "Repeated backend reads hit a cache-aside cache. What happens to database read load for those hits?",
                new[] { "Every hit must query Database again", "Hits increase primary write capacity", "The Cache calls the frontend to find a shard", "Those reads can complete without a database query" }, 3,
                "The backend consults Cache first. A hit avoids a database read; a miss falls back to storage and can populate Cache. Writes and invalidation still matter."),
            new QuizQuestion("docker-01", LessonId.Docker, QuizStage.Pre, "What is the relationship between a container image and a container instance?",
                new[] { "Every instance is a different physical machine", "An image is a running Kubernetes worker", "An instance is the shared host kernel", "An image is a reusable template used to create instances" }, 3,
                "Images package application files and configuration. Multiple containers can be created from one read-only image while maintaining separate runtime state."),
            new QuizQuestion("docker-02", LessonId.Docker, QuizStage.Pre, "Several Linux containers on one Linux host normally share…",
                new[] { "The host kernel", "One mandatory application queue", "A separate guest kernel per container", "One immutable request counter across every process" }, 0,
                "Containers isolate processes and resources while sharing the host kernel. They are not each a separate virtual machine with its own kernel."),
            new QuizQuestion("docker-03", LessonId.Docker, QuizStage.Post, "One container exhausts its own memory limit while the host has headroom. What is a reasonable expectation?",
                new[] { "Every sibling must crash", "That instance can fail while siblings continue", "The image is automatically rewritten", "The host gains more RAM" }, 1,
                "Per-instance resource limits provide isolation. Shared host exhaustion or dependencies can still affect multiple containers, so isolation is not an absolute availability guarantee."),
            new QuizQuestion("docker-04", LessonId.Docker, QuizStage.Post, "An unmanaged simulated Docker container crashes. Which action restores that same instance in this exercise?",
                new[] { "Assume an unconfigured HPA creates it", "Add a database replica", "Use Restart and advance simulation recovery time", "Remove all host resource limits" }, 2,
                "The Docker exercise provides explicit lifecycle controls and a one-second simulated restart. It does not imply every unmanaged container has an automatic restart policy."),
            new QuizQuestion("k8s-01", LessonId.Kubernetes, QuizStage.Pre, "A Kubernetes Pod is best described as…",
                new[] { "The entire cluster", "A mandatory physical server", "A schedulable group of one or more containers", "A database's primary write log" }, 2,
                "Pods group closely related containers with shared networking and storage context. Worker nodes host Pods; a cluster includes control-plane and worker components."),
            new QuizQuestion("k8s-02", LessonId.Kubernetes, QuizStage.Pre, "A Deployment desires three replicas and one Pod terminally fails. The controller normally aims to…",
                new[] { "Raise desired replicas to four automatically", "Create a replacement Pod to restore three", "Reuse the failed Pod's identity indefinitely", "Delete all healthy Pods" }, 1,
                "A Deployment, through its ReplicaSet, maintains the specified replica target. A replacement Pod has a new identity. Restoring the target is distinct from increasing it."),
            new QuizQuestion("k8s-03", LessonId.Kubernetes, QuizStage.Post, "CPU rises but no HPA or other autoscaler is configured. Desired replicas are three. What should this simulation do?",
                new[] { "Keep the explicit target until the learner changes it", "Automatically grow to eight in every workload", "Delete the Deployment", "Treat a failed Pod replacement as scaling to four" }, 0,
                "Autoscaling requires separate configuration and metrics. This exercise supports explicit scaling and failure replacement, without silently enabling HPA."),
            new QuizQuestion("k8s-04", LessonId.Kubernetes, QuizStage.Post, "Desired Pods exceed all healthy worker slots. The deployment should report…",
                new[] { "Guaranteed ready replicas without resources", "That every request succeeded", "Unlimited workers created by a Deployment alone", "Pending capacity while preserving the desired target" }, 3,
                "A controller cannot schedule ready Pods without suitable worker capacity. Add or recover workers, or change the target. Deployments do not inherently provision worker machines."),
        });
        public static IReadOnlyList<QuizQuestion> Form(QuizStage stage)
        {
            if (stage != QuizStage.Pre && stage != QuizStage.Post) throw new ArgumentOutOfRangeException(nameof(stage));
            return Array.AsReadOnly(All.Where(q => q.Stage == stage).ToArray());
        }
        public static QuizQuestion Find(string id) => All.FirstOrDefault(q => q.Id == id);
    }
}
