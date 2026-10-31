using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SysARchitect.Components;
using SysARchitect.Core;
using SysARchitect.Graph;
using SysARchitect.Lessons;
using SysARchitect.Simulation;

namespace SysARchitect.Tests
{
    public class LessonWorkspaceTests
    {
        private sealed class MemoryStore : IProgressStorage
        { private string json = ""; public string Read() => json; public void Write(string value) => json = value; }
        private static LessonController Load(LessonId id)
        {
            var lesson = Object.FindAnyObjectByType<LessonController>(); lesson.UseProgress(new MemoryStore()); lesson.SelectLesson(id); lesson.LoadExample(); return lesson;
        }
        private static void Tick(SimulationController simulation, int count) { for (int i = 0; i < count; i++) simulation.StepOnce(); }
        [UnityTest]
        public IEnumerator ClientServerChallengeRequiresConnectedRealRequestsAndShowsGuide()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity"); yield return null;
            var lesson = Load(LessonId.ClientServer); var graph = Object.FindAnyObjectByType<ArchitectureGraph>();
            Assert.That(lesson.CheckChallenge(), Is.False); Assert.That(lesson.Progress.CompletedCount, Is.Zero);
            graph.AddConnection(graph.Nodes[0], graph.Nodes[1]); graph.AddConnection(graph.Nodes[1], graph.Nodes[2]);
            Tick(Object.FindAnyObjectByType<SimulationController>(), 60);
            Assert.That(lesson.CheckChallenge(), Is.True); Assert.That(lesson.Progress.IsComplete(LessonId.ClientServer), Is.True);
            lesson.CheckChallenge(); Assert.That(lesson.Progress.CompletedCount, Is.EqualTo(1));
            GameObject.Find("Workspace UI/Toolbox").GetComponentsInChildren<Button>().Single(b => b.name == "Lessons / Progress").onClick.Invoke();
            yield return null; yield return null;
            var guide = GameObject.Find("Workspace UI/Learning Guide"); Assert.That(guide, Is.Not.Null);
            Assert.That(guide.GetComponentsInChildren<TMPro.TextMeshProUGUI>().Any(t => t.text.Contains("MEASURABLE OBJECTIVES") && t.text.Contains("<color=#40FFFF>")), Is.True);
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null) DesktopWorkspaceTests.CaptureDemo("sysarchitect-learning-guide.png");
        }
        [UnityTest]
        public IEnumerator LoadBalancingChallengeMeasuresBothTargetsFailoverAndRecovery()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity"); yield return null;
            var lesson = Load(LessonId.LoadBalancing); var graph = Object.FindAnyObjectByType<ArchitectureGraph>(); var sim = Object.FindAnyObjectByType<SimulationController>();
            Assert.That(lesson.CheckChallenge(), Is.False);
            var lb = graph.Nodes.Single(n => n.Type == NodeType.LoadBalancer); var first = graph.Nodes.Single(n => n.Type == NodeType.Backend); var db = graph.Nodes.Single(n => n.Type == NodeType.Database);
            var second = graph.AddNode(NodeType.Backend, new Vector3(-.05f, .2f, .4f)); second.ProcessingCapacity = 40;
            graph.AddConnection(lb, second); graph.AddConnection(second, db); Tick(sim, 60);
            Assert.That(lesson.CheckChallenge(), Is.False, "Traffic alone does not prove failover");
            sim.FailNode(first); Tick(sim, 5); sim.RestartNode(first); Tick(sim, 60);
            Assert.That(lesson.CheckChallenge(), Is.True); Assert.That(lesson.Progress.IsComplete(LessonId.LoadBalancing), Is.True);
        }
        [UnityTest]
        public IEnumerator DatabaseChallengeObservesActualLagThenShardingAndExplicitWriteHighlight()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity"); yield return null;
            var lesson = Load(LessonId.Database); var graph = Object.FindAnyObjectByType<ArchitectureGraph>(); var sim = Object.FindAnyObjectByType<SimulationController>();
            Assert.That(graph.Nodes.Single(n => n.Type == NodeType.LoadBalancer).RoutingRole, Is.EqualTo(LoadBalancerRole.Database));
            var state = sim.Engine.FindNode(graph.Nodes.Single(n => n.Type == NodeType.Database).NodeId); state.Database.Configure(DatabaseMode.Replication, 3);
            sim.Engine.RequestsPerSecond = 0;
            sim.Engine.SubmitRequest(graph.Nodes.Single(n => n.Type == NodeType.Frontend).NodeId, true, 7);
            var lines = new HashSet<int>(); for (int i = 0; i < 20; i++) { sim.StepOnce(); lines.Add(lesson.ActiveAlgorithmLine); }
            Assert.That(lines, Does.Contain(1), "Explicit write ID 1 must use actual transfer type, not an ID-based write guess");
            sim.Engine.RequestsPerSecond = 30; Tick(sim, 80);
            Assert.That(lesson.CheckChallenge(), Is.False); Assert.That(lesson.Feedback, Does.Contain("Switch"));
            state.Database.Configure(DatabaseMode.Sharding, 3); Tick(sim, 20);
            Assert.That(lesson.CheckChallenge(), Is.True); Assert.That(lesson.Progress.IsComplete(LessonId.Database), Is.True);
        }
        [UnityTest]
        public IEnumerator DockerChallengeUsesInstanceStateLimitsAndActualPostRestartWork()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity"); yield return null;
            var lesson = Load(LessonId.Docker); var graph = Object.FindAnyObjectByType<ArchitectureGraph>(); var sim = Object.FindAnyObjectByType<SimulationController>();
            var docker = Object.FindAnyObjectByType<DockerWorkspaceController>(); var host = graph.Nodes.Single(n => n.Type == NodeType.DockerHost);
            Assert.That(lesson.CheckChallenge(), Is.False);
            var first = docker.CreateContainer(host, 0); docker.CreateContainer(host, 0); lesson.ObserveState();
            first.ProcessingCapacity = 80; lesson.ObserveState(); var model = docker.GetHost(host);
            model.Stop(first.NodeId); sim.StepOnce(); model.Start(first.NodeId); Tick(sim, 30);
            sim.FailNode(first); sim.StepOnce(); model.Restart(first.NodeId); Tick(sim, 80);
            Assert.That(lesson.CheckChallenge(), Is.True); Assert.That(lesson.Progress.IsComplete(LessonId.Docker), Is.True);
        }
        [UnityTest]
        public IEnumerator KubernetesChallengeDistinguishesScalingFromNewIdentityReplacement()
        {
            yield return SceneManager.LoadSceneAsync("Assets/SysARchitect/Scenes/DesktopWorkspace.unity"); yield return null;
            var lesson = Load(LessonId.Kubernetes); var graph = Object.FindAnyObjectByType<ArchitectureGraph>(); var sim = Object.FindAnyObjectByType<SimulationController>();
            var model = Object.FindAnyObjectByType<KubernetesWorkspaceController>().GetCluster(graph.Nodes.Single(n => n.Type == NodeType.KubernetesCluster));
            Assert.That(lesson.CheckChallenge(), Is.False);
            model.SetDesired(3); Tick(sim, 60); Assert.That(lesson.CheckChallenge(), Is.False, "Scaling alone does not prove recovery");
            string old = model.Pods[0].NodeId; sim.FailNode(graph.FindNode(old)); Tick(sim, 60);
            Assert.That(graph.FindNode(old), Is.Null); Assert.That(lesson.CheckChallenge(), Is.True);
            Assert.That(lesson.Progress.IsComplete(LessonId.Kubernetes), Is.True);
        }
    }
}
