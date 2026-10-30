using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SysARchitect.Core;
using SysARchitect.Interaction;
using SysARchitect.Graph;

namespace SysARchitect.UI
{
    /// <summary>
    /// Toolbox panel that displays buttons for each infrastructure component type.
    /// Clicking a button enters placement mode for that component.
    /// </summary>
    public class ToolboxUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InteractionManager interactionManager;
        [SerializeField] private Transform buttonContainer;
        [SerializeField] private GameObject toolboxButtonPrefab;

        [Header("Toolbox Buttons")]
        [SerializeField] private Button connectButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private Button selectButton;
        [SerializeField] private Button validateButton;
        [SerializeField] private ArchitectureGraph graph;
        [SerializeField] private ConnectionCreationController connectionController;
        [SerializeField] private TextMeshProUGUI statusText;
        private string feedback = "Place a Frontend and Backend, then connect them.";

        private Button[] nodeButtons;

        private void OnEnable()
        {
            if (connectionController != null) connectionController.OnConnectionError += ShowFeedback;
        }

        private void OnDisable()
        {
            if (connectionController != null) connectionController.OnConnectionError -= ShowFeedback;
        }

        private void Update()
        {
            if (statusText != null)
                statusText.text = $"{interactionManager.CurrentMode} | {feedback}";
        }

        private void ShowFeedback(string message) => feedback = message;

        private void Start()
        {
            CreateNodeButtons();
            SetupActionButtons();
        }

        private void CreateNodeButtons()
        {
            // If using a prefab-based approach, instantiate buttons for each NodeType.
            // For now, find existing buttons or create them programmatically.
            var nodeTypes = new[]
            {
                NodeType.Frontend,
                NodeType.Backend,
                NodeType.Database,
                NodeType.LoadBalancer,
                NodeType.Cache,
                NodeType.Container,
                NodeType.KubernetesNode,
                NodeType.KubernetesPod
            };

            if (toolboxButtonPrefab != null && buttonContainer != null)
            {
                nodeButtons = new Button[nodeTypes.Length];
                for (int i = 0; i < nodeTypes.Length; i++)
                {
                    var nodeType = nodeTypes[i];
                    var buttonGo = Instantiate(toolboxButtonPrefab, buttonContainer);
                    var button = buttonGo.GetComponent<Button>();
                    var label = buttonGo.GetComponentInChildren<TextMeshProUGUI>();

                    if (label != null)
                    {
                        label.text = FormatNodeTypeName(nodeType);
                    }

                    button.onClick.AddListener(() => OnNodeTypeClicked(nodeType));
                    nodeButtons[i] = button;
                }
            }
        }

        private void SetupActionButtons()
        {
            if (selectButton != null) selectButton.onClick.AddListener(interactionManager.ReturnToSelectMode);
            if (validateButton != null) validateButton.onClick.AddListener(() =>
            {
                var warnings = graph.ValidateGraph();
                feedback = warnings.Count == 0 ? "Architecture validation passed." : string.Join("\n", warnings);
            });
            if (connectButton != null)
                connectButton.onClick.AddListener(OnConnectClicked);

            if (deleteButton != null)
                deleteButton.onClick.AddListener(OnDeleteClicked);

            if (clearButton != null)
                clearButton.onClick.AddListener(OnClearClicked);
        }

        private void OnNodeTypeClicked(NodeType type)
        {
            if (interactionManager != null)
                interactionManager.OnToolboxNodeSelected(type);
        }

        private void OnConnectClicked()
        {
            if (interactionManager != null)
                interactionManager.OnConnectButtonPressed();
        }

        private void OnDeleteClicked()
        {
            if (interactionManager != null)
                interactionManager.OnDeleteButtonPressed();
        }

        private void OnClearClicked()
        {
            if (interactionManager != null)
                interactionManager.OnClearButtonPressed();
        }

        private static string FormatNodeTypeName(NodeType type)
        {
            return type switch
            {
                NodeType.LoadBalancer => "Load\nBalancer",
                NodeType.KubernetesNode => "K8s\nNode",
                NodeType.KubernetesPod => "K8s\nPod",
                _ => type.ToString()
            };
        }
    }
}
