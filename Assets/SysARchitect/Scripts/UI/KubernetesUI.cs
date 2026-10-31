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
    public class KubernetesUI : MonoBehaviour
    {
        [SerializeField] private InspectorUI inspector;
        [SerializeField] private ArchitectureGraph graph;
        [SerializeField] private SimulationController simulation;
        [SerializeField] private KubernetesWorkspaceController kubernetes;
        [SerializeField] private GameObject panel;
        [SerializeField] private Button addWorker, nextPod, failPod, expandCluster, expandWorker;
        [SerializeField] private Slider desiredSlider;
        [SerializeField] private TextMeshProUGUI details, replicaText;
        private GraphNode cluster;
        private DeploymentModel model;
        private int selectedPod;
        private readonly StringBuilder text = new StringBuilder();
        private void Start()
        {
            addWorker.onClick.AddListener(() => kubernetes.AddWorker(cluster));
            nextPod.onClick.AddListener(() => selectedPod++);
            failPod.onClick.AddListener(() => { if (model.Pods.Count > 0) simulation.FailNode(graph.FindNode(model.Pods[selectedPod % model.Pods.Count].NodeId)); });
            expandCluster.onClick.AddListener(() => cluster.GetComponent<WorkloadEnclosure>().Toggle());
            expandWorker.onClick.AddListener(() => { if (model.Pods.Count > 0) graph.FindNode(model.Pods[selectedPod % model.Pods.Count].WorkerId).GetComponent<WorkloadEnclosure>().Toggle(); });
            desiredSlider.minValue = 0; desiredSlider.maxValue = 8; desiredSlider.wholeNumbers = true;
            desiredSlider.onValueChanged.AddListener(value => model?.SetDesired((int)value));
        }
        private void Update()
        {
            cluster = inspector.CurrentNode;
            for (int i = 0; i < 2 && cluster != null && cluster.Type != NodeType.KubernetesCluster; i++)
                cluster = cluster.HostNodeId == null ? null : graph.FindNode(cluster.HostNodeId);
            if (cluster != null && cluster.Type != NodeType.KubernetesCluster) cluster = null;
            panel.SetActive(cluster != null); if (cluster == null) return;
            model = kubernetes.GetCluster(cluster);
            desiredSlider.SetValueWithoutNotify(model.DesiredReplicas);
            addWorker.interactable = model.Workers.Count < 4;
            nextPod.interactable = failPod.interactable = expandWorker.interactable = model.Pods.Count > 0;
            replicaText.text = $"Explicit desired replicas: {model.DesiredReplicas} | Actual {model.ActualReplicas} | Ready {model.ReadyReplicas} | Workers {model.Workers.Count}/4";
            text.Clear();
            text.Append("Replica target is explicit. Failure replacement restores it; HPA is not configured.\n");
            text.Append($"Scale changes {model.ScaleChanges} | Replacements {model.Replacements}. Reconcile 0.5s; readiness 1s. Use Run/Step.\n");
            if (model.Pods.Count > 0)
            {
                selectedPod %= model.Pods.Count;
                var pod = model.Pods[selectedPod]; var state = simulation.Engine.FindNode(pod.NodeId);
                text.Append($"Selected UID {pod.NodeId} on {pod.WorkerId}: {state?.Status}, processed {state?.Completed}, CPU {state?.Cpu:P0}.\n");
            }
            text.Append(model.LastAction + "\n");
            string[] algorithm = { "observe(desired, pods)", "removeFailedPods()", "compare(actual, desired)", "scheduleNewPodOnHealthyWorker()", "terminateExcessPods()", "reportPendingCapacity()" };
            for (int i = 0; i < algorithm.Length; i++)
                text.Append(i == model.ActiveAlgorithmLine ? "<color=#40FFFF>" + algorithm[i] + "</color> " : algorithm[i] + " ");
            text.Append("\nService/worker dispatch is abstract; traffic does not traverse the API server/scheduler. Fail Pod is terminal failure (new UID), not a container restart. Four-slot worker scheduler.");
            details.text = text.ToString();
        }
    }
}
