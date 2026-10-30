using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SysARchitect.Core;
using SysARchitect.Graph;
using SysARchitect.Interaction;
using SysARchitect.Simulation;
using SysARchitect.Visualization;

namespace SysARchitect.UI
{
    public class RoutingUI : MonoBehaviour
    {
        [SerializeField] private ArchitectureGraph graph;
        [SerializeField] private InspectorUI inspector;
        [SerializeField] private SimulationController simulation;
        [SerializeField] private NodeSelectionController selection;
        [SerializeField] private InteractionManager interactions;
        [SerializeField] private GameObject routingPanel, connectionPanel;
        [SerializeField] private Button applicationButton, databaseButton, deleteConnectionButton, toolbarDeleteConnection, dismissButton;
        [SerializeField] private TextMeshProUGUI routingText, connectionText;
        private string feedback = "";
        private GraphNode lastNode;
        private LoadBalancerRole lastRole;
        private readonly StringBuilder text = new StringBuilder();

        private void Start()
        {
            dismissButton.onClick.AddListener(selection.Deselect);
            applicationButton.onClick.AddListener(() => SetRole(LoadBalancerRole.Application));
            databaseButton.onClick.AddListener(() => SetRole(LoadBalancerRole.Database));
            deleteConnectionButton.onClick.AddListener(interactions.OnDeleteConnectionButtonPressed);
            toolbarDeleteConnection.onClick.AddListener(interactions.OnDeleteConnectionButtonPressed);
        }
        private void SetRole(LoadBalancerRole role)
        {
            graph.TrySetRoutingRole(inspector.CurrentNode, role, out feedback);
        }
        private void Update()
        {
            var edge = selection.SelectedConnection;
            connectionPanel.SetActive(edge != null);
            toolbarDeleteConnection.interactable = edge != null;
            if (edge != null)
                connectionText.text = $"{edge.Source.DisplayName}\n  > {edge.Target.DisplayName}\n\n{edge.GetComponent<ConnectionRenderer>().LastActivity}\n\nDelete removes this link only.\nDelete/Backspace works outside text fields.\nReconnect with Connect, source, target.";
            var node = inspector.CurrentNode;
            if (node != lastNode || (node != null && node.RoutingRole != lastRole)) { feedback = ""; lastNode = node; if (node != null) lastRole = node.RoutingRole; }
            bool router = node != null && node.Type == NodeType.LoadBalancer;
            bool cache = node != null && node.Type == NodeType.Cache;
            bool backend = node != null && node.Type == NodeType.Backend;
            routingPanel.SetActive(router || cache || backend);
            applicationButton.gameObject.SetActive(router);
            databaseButton.gameObject.SetActive(router);
            if (!routingPanel.activeSelf) return;
            var state = simulation.Engine.FindNode(node.NodeId);
            text.Clear();
            if (router)
            {
                text.Append(node.RoutingRole == LoadBalancerRole.Application
                    ? "APPLICATION ROUTING: Frontend > Load Balancer > healthy Backends > Database.\n"
                    : "DATABASE ROUTER: Backend > Router > one Database cluster. Configure replicas/shards on that Database.\n");
                text.Append("Database writes go only to primary; reads prefer healthy replicas; shard keys select one partition.\n");
                foreach (var target in graph.GetOutgoingConnections(node))
                {
                    var targetState = simulation.Engine.FindNode(target.Target.NodeId);
                    text.Append($"{target.Target.DisplayName}: {targetState.Status}, CPU {targetState.Cpu:P0}, processed {targetState.Completed}\n");
                }
                text.Append(feedback);
            }
            else if (cache)
                text.Append($"CACHE-ASIDE: Backend reads Cache, misses fall back through Backend to storage, then populate Cache.\nHits {state.Cache.Hits} | Misses {state.Cache.Misses} | Populations {state.Cache.Populations}\nKeys {state.Cache.Count}/128 | Invalidations {state.Cache.Invalidations} | Evictions {state.Cache.Evictions}\nCommitted writes invalidate matching cluster/key entries. Missing/stale records are not cached.");
            else
                text.Append($"Connect this Backend to one Cache and one Database or Database Router for cache-aside.\nReads: Cache hit skips storage; miss queries storage, then fills Cache. Writes bypass Cache and invalidate it.\nCache bypasses while offline: {state.CacheBypasses}\nReplies to Backend are implicit; visible requests follow its actual directed edges.");
            text.Append("\nLines: cyan routing, green hit, orange fallback, purple fill, pink write; selected yellow. Packets green/red = outcome.");
            routingText.text = text.ToString();
        }
    }
}
