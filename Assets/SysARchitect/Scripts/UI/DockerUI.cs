using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SysARchitect.Components;
using SysARchitect.Core;
using SysARchitect.Graph;
using SysARchitect.Simulation;
using SysARchitect.Visualization;

namespace SysARchitect.UI
{
    public class DockerUI : MonoBehaviour
    {
        [SerializeField] private InspectorUI inspector;
        [SerializeField] private ArchitectureGraph graph;
        [SerializeField] private SimulationController simulation;
        [SerializeField] private DockerWorkspaceController docker;
        [SerializeField] private GameObject panel;
        [SerializeField] private Button imageButton, createButton, expandButton, startButton, stopButton, restartButton, removeButton, failButton;
        [SerializeField] private Slider memberSlider, cpuSlider, memorySlider;
        [SerializeField] private TextMeshProUGUI details, limits;
        private GraphNode host, container;
        private DockerHostModel model;
        private int image;
        private readonly StringBuilder text = new StringBuilder();
        private void Start()
        {
            imageButton.onClick.AddListener(() => image = (image + 1) % DockerHostModel.Images.Count);
            createButton.onClick.AddListener(() => { if (host != null) docker.CreateContainer(host, image); });
            expandButton.onClick.AddListener(() => host.GetComponent<WorkloadEnclosure>().Toggle());
            startButton.onClick.AddListener(() => model.Start(container.NodeId));
            stopButton.onClick.AddListener(() => model.Stop(container.NodeId));
            restartButton.onClick.AddListener(() => model.Restart(container.NodeId));
            removeButton.onClick.AddListener(() => graph.RemoveNode(container));
            failButton.onClick.AddListener(() => simulation.FailNode(container));
            cpuSlider.minValue = 10; cpuSlider.maxValue = 500;
            memorySlider.minValue = 32; memorySlider.maxValue = 512;
            cpuSlider.onValueChanged.AddListener(value => { if (container != null) container.ProcessingCapacity = value; });
            memorySlider.onValueChanged.AddListener(value => { if (container != null) container.MemoryLimitMB = value; });
        }
        private void Update()
        {
            var selected = inspector.CurrentNode;
            host = selected != null && selected.Type == NodeType.DockerHost ? selected : selected?.HostNodeId != null ? graph.FindNode(selected.HostNodeId) : null;
            if (host != null && host.Type != NodeType.DockerHost) host = null;
            panel.SetActive(host != null);
            if (host == null) return;
            model = docker.GetHost(host);
            memberSlider.wholeNumbers = true; memberSlider.minValue = 0; memberSlider.maxValue = Mathf.Max(0, model.Containers.Count - 1);
            int index = Mathf.Clamp((int)memberSlider.value, 0, Mathf.Max(0, model.Containers.Count - 1));
            container = model.Containers.Count == 0 ? null : graph.FindNode(model.Containers[index].NodeId);
            createButton.interactable = model.Containers.Count < 8;
            imageButton.GetComponentInChildren<TextMeshProUGUI>().text = "Image: " + DockerHostModel.Images[image].Name;
            startButton.interactable = stopButton.interactable = restartButton.interactable = removeButton.interactable = failButton.interactable = container != null;
            cpuSlider.interactable = memorySlider.interactable = container != null;
            if (container != null)
            {
                cpuSlider.SetValueWithoutNotify(container.ProcessingCapacity); memorySlider.SetValueWithoutNotify(container.MemoryLimitMB);
                limits.text = $"Container CPU quota {container.ProcessingCapacity:F0} req/s\nMemory limit {container.MemoryLimitMB:F0} MB";
            }
            else limits.text = "Create a container first.";
            text.Clear();
            text.Append("An image is a reusable, read-only template. A container is an isolated running process instance sharing its host kernel.\n");
            text.Append($"Host dispatch/CPU budget {host.ProcessingCapacity:F0} req/s; containers {model.Containers.Count}/8. Start/restart takes 1 simulated second; use Run/Step.\n");
            if (container != null)
            {
                var state = simulation.Engine.FindNode(container.NodeId);
                text.Append($"Selected {index}: {container.DisplayName}, {state.Status}, CPU {state.Cpu:P0}, RAM {state.MemoryMB:F0} MB\nProcessed {state.Completed}, failed {state.Failed}. Independent queues and limits; one container crash does not crash its siblings.\n");
            }
            text.Append("Host RAM includes container memory; throughput is bounded by the host. Move the host to move its owned instances. FE > Host > Container uses real requests. No Docker commands run.");
            details.text = text.ToString();
        }
    }
}
