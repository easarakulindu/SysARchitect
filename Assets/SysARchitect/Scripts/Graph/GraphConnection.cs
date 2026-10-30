using UnityEngine;

namespace SysARchitect.Graph
{
    /// <summary>
    /// Represents a directed connection (edge) between two nodes in the architecture graph.
    /// </summary>
    public class GraphConnection : MonoBehaviour
    {
        [SerializeField] private string connectionId;
        [SerializeField] private GraphNode source;
        [SerializeField] private GraphNode target;

        /// <summary>Unique identifier for this connection.</summary>
        public string ConnectionId
        {
            get => connectionId;
            set => connectionId = value;
        }

        /// <summary>Source node of this connection (where requests originate).</summary>
        public GraphNode Source
        {
            get => source;
            set => source = value;
        }

        /// <summary>Target node of this connection (where requests are sent).</summary>
        public GraphNode Target
        {
            get => target;
            set => target = value;
        }
    }
}
