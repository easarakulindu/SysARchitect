using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SysARchitect.Core;
using SysARchitect.Graph;
using SysARchitect.Interaction;
using SysARchitect.Visualization;

namespace SysARchitect.UI
{
    /// <summary>
    /// Inspector panel that shows properties of the currently selected node.
    /// Updates when selection changes.
    /// </summary>
    public class InspectorUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NodeSelectionController selectionController;
        [SerializeField] private ArchitectureGraph graph;

        [Header("UI Elements")]
        [SerializeField] private GameObject inspectorPanel;
        [SerializeField] private TextMeshProUGUI nodeNameText;
        [SerializeField] private TextMeshProUGUI nodeTypeText;
        [SerializeField] private TextMeshProUGUI nodeStatusText;
        [SerializeField] private TextMeshProUGUI nodeIdText;
        [SerializeField] private TextMeshProUGUI connectionsText;

        [Header("Configuration")]
        [SerializeField] private TMP_InputField nameInputField;
        [SerializeField] private Slider capacitySlider;
        [SerializeField] private TextMeshProUGUI capacityValueText;
        [SerializeField] private Slider memorySlider;
        [SerializeField] private TextMeshProUGUI memoryValueText;
        [SerializeField] private Button rotateButton;
        [SerializeField] private Transform connectionContainer;
        [SerializeField] private GameObject connectionButtonPrefab;

        private GraphNode currentNode;

        private void OnEnable()
        {
            if (graph != null)
            {
                graph.OnConnectionAdded += OnConnectionsChanged;
                graph.OnConnectionRemoved += OnConnectionsChanged;
                graph.OnNodeRemoved += OnNodeRemoved;
            }
            if (selectionController != null)
            {
                selectionController.OnNodeSelected += OnNodeSelected;
                selectionController.OnNodeDeselected += OnNodeDeselected;
            }
        }

        private void OnDisable()
        {
            if (graph != null)
            {
                graph.OnConnectionAdded -= OnConnectionsChanged;
                graph.OnConnectionRemoved -= OnConnectionsChanged;
                graph.OnNodeRemoved -= OnNodeRemoved;
            }
            if (selectionController != null)
            {
                selectionController.OnNodeSelected -= OnNodeSelected;
                selectionController.OnNodeDeselected -= OnNodeDeselected;
            }
        }

        private void Start()
        {
            if (inspectorPanel != null)
                inspectorPanel.SetActive(false);

            SetupInputListeners();
        }

        private void SetupInputListeners()
        {
            if (rotateButton != null) rotateButton.onClick.AddListener(() =>
            {
                if (currentNode != null) currentNode.transform.Rotate(Vector3.up, 45f, Space.World);
            });
            if (nameInputField != null)
            {
                nameInputField.onEndEdit.AddListener(OnNameChanged);
            }

            if (capacitySlider != null)
            {
                capacitySlider.minValue = 10f;
                capacitySlider.maxValue = 1000f;
                capacitySlider.onValueChanged.AddListener(OnCapacityChanged);
            }

            if (memorySlider != null)
            {
                memorySlider.minValue = 64f;
                memorySlider.maxValue = 4096f;
                memorySlider.onValueChanged.AddListener(OnMemoryChanged);
            }
        }

        private void OnNodeSelected(NodeVisual visual)
        {
            if (visual == null) return;

            currentNode = visual.GetGraphNode();
            if (currentNode == null) return;

            if (inspectorPanel != null)
                inspectorPanel.SetActive(true);

            RefreshDisplay();
            RefreshConnections();
        }

        private void OnNodeRemoved(GraphNode node)
        {
            if (node == currentNode) OnNodeDeselected();
        }

        private void OnConnectionsChanged(GraphConnection connection) => RefreshConnections();

        private void RefreshConnections()
        {
            if (connectionContainer == null || connectionButtonPrefab == null) return;
            foreach (Transform child in connectionContainer)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            if (currentNode == null || graph == null) return;
            foreach (var edge in graph.Connections)
            {
                if (edge.Source != currentNode && edge.Target != currentNode) continue;
                var button = Instantiate(connectionButtonPrefab, connectionContainer).GetComponent<Button>();
                button.GetComponentInChildren<TextMeshProUGUI>().text =
                    $"Remove: {edge.Source.DisplayName} > {edge.Target.DisplayName}";
                button.onClick.AddListener(() => graph.RemoveConnection(edge));
            }
            RefreshDisplay();
        }

        private void OnNodeDeselected()
        {
            currentNode = null;

            if (inspectorPanel != null)
                inspectorPanel.SetActive(false);
        }

        private void RefreshDisplay()
        {
            if (currentNode == null) return;

            if (nodeNameText != null)
                nodeNameText.text = currentNode.DisplayName;

            if (nodeTypeText != null)
                nodeTypeText.text = FormatNodeType(currentNode.Type);

            if (nodeStatusText != null)
            {
                nodeStatusText.text = currentNode.Status.ToString();
                nodeStatusText.color = NodeColorPalette.GetStatusColor(currentNode.Status);
            }

            if (nodeIdText != null)
                nodeIdText.text = $"ID: {currentNode.NodeId}";

            if (connectionsText != null && graph != null)
            {
                var incoming = graph.GetIncomingConnections(currentNode);
                var outgoing = graph.GetOutgoingConnections(currentNode);
                connectionsText.text = $"In: {incoming.Count}  Out: {outgoing.Count}";
            }

            if (nameInputField != null)
            {
                nameInputField.SetTextWithoutNotify(currentNode.DisplayName);
            }

            if (capacitySlider != null)
            {
                capacitySlider.SetValueWithoutNotify(currentNode.ProcessingCapacity);
                UpdateCapacityText(currentNode.ProcessingCapacity);
            }

            if (memorySlider != null)
            {
                memorySlider.SetValueWithoutNotify(currentNode.MemoryLimitMB);
                UpdateMemoryText(currentNode.MemoryLimitMB);
            }
        }

        private void OnNameChanged(string newName)
        {
            if (currentNode == null || string.IsNullOrWhiteSpace(newName)) return;

            currentNode.DisplayName = newName;
            currentNode.gameObject.name = newName;

            var visual = currentNode.GetComponent<NodeVisual>();
            if (visual != null)
                visual.UpdateLabel(newName);

            RefreshDisplay();
        }

        private void OnCapacityChanged(float value)
        {
            if (currentNode == null) return;
            currentNode.ProcessingCapacity = value;
            UpdateCapacityText(value);
        }

        private void OnMemoryChanged(float value)
        {
            if (currentNode == null) return;
            currentNode.MemoryLimitMB = value;
            UpdateMemoryText(value);
        }

        private void UpdateCapacityText(float value)
        {
            if (capacityValueText != null)
                capacityValueText.text = $"{value:F0} req/s";
        }

        private void UpdateMemoryText(float value)
        {
            if (memoryValueText != null)
                memoryValueText.text = $"{value:F0} MB";
        }

        private static string FormatNodeType(NodeType type)
        {
            return type switch
            {
                NodeType.LoadBalancer => "Load Balancer",
                NodeType.KubernetesNode => "Kubernetes Node",
                NodeType.KubernetesPod => "Kubernetes Pod",
                _ => type.ToString()
            };
        }
    }
}
