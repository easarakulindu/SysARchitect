using System;
using System.Collections.Generic;
using SysARchitect.Core;
using SysARchitect.Simulation;

namespace SysARchitect.Components
{
    public sealed class ContainerImage
    {
        public string Name { get; }
        public string Purpose { get; }
        public ContainerImage(string name, string purpose) { Name = name; Purpose = purpose; }
    }
    public sealed class ContainerInstance
    {
        public string NodeId { get; }
        public ContainerImage Image { get; }
        public ContainerInstance(string id, ContainerImage image) { NodeId = id; Image = image; }
    }
    /// <summary>Image/instance ownership over the existing queue/resource/failure engine.</summary>
    public sealed class DockerHostModel
    {
        public static readonly IReadOnlyList<ContainerImage> Images = Array.AsReadOnly(new[] {
            new ContainerImage("web:v1", "Static web service"), new ContainerImage("api:v1", "Application API"),
            new ContainerImage("worker:v1", "Background task worker") });
        private readonly SimulationEngine engine;
        private readonly List<ContainerInstance> containers = new List<ContainerInstance>();
        public IReadOnlyList<ContainerInstance> Containers => containers;
        public string HostId { get; }
        public DockerHostModel(SimulationEngine engine, string hostId)
        {
            if (engine.FindNode(hostId)?.Type != NodeType.DockerHost) throw new ArgumentException("Choose a Docker Host.");
            this.engine = engine; HostId = hostId;
        }
        public void Attach(string containerId, ContainerImage image)
        {
            var node = engine.FindNode(containerId);
            if (node?.Type != NodeType.Container || node.HostingNodeId != null) throw new ArgumentException("Choose an unowned Container.");
            if (containers.Count >= 8) throw new InvalidOperationException("This educational host supports eight container instances.");
            if (image == null) throw new ArgumentNullException(nameof(image));
            node.HostingNodeId = HostId;
            containers.Add(new ContainerInstance(containerId, image));
        }
        public void Forget(string id) => containers.RemoveAll(c => c.NodeId == id);
        public void Start(string id) { Require(id); if (engine.FindNode(id).Status == NodeStatus.Offline) engine.Restart(id); }
        public void Stop(string id) { Require(id); engine.Stop(id); }
        public void Restart(string id) { Require(id); engine.Stop(id); engine.Restart(id); }
        private void Require(string id)
        {
            if (!containers.Exists(c => c.NodeId == id) || engine.FindNode(id) == null) throw new ArgumentException("Select a container owned by this host.");
        }
    }
}
