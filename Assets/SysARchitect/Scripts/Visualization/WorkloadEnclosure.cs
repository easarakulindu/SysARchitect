using System.Collections.Generic;
using UnityEngine;
using SysARchitect.Graph;
using SysARchitect.Core;

namespace SysARchitect.Visualization
{
    /// <summary>Shows actual contained graph nodes; no duplicate simulation members.</summary>
    public class WorkloadEnclosure : MonoBehaviour
    {
        private GraphNode owner;
        private readonly List<GraphNode> members = new List<GraphNode>();
        private LineRenderer outline;
        private Vector3 originalSize;
        private Vector3 originalCenter;
        public bool Expanded { get; private set; } = true;
        public void Initialize(GraphNode node)
        {
            owner = node; originalSize = node.GetComponent<BoxCollider>().size; originalCenter = node.GetComponent<BoxCollider>().center;
            var go = new GameObject("Holographic workload enclosure"); go.layer = 2; go.transform.SetParent(transform, false);
            outline = go.AddComponent<LineRenderer>(); outline.useWorldSpace = false;
            outline.sharedMaterial = node.GetComponent<Renderer>().sharedMaterial; outline.widthMultiplier = .006f * transform.lossyScale.x;
            outline.positionCount = 16;
            float x = owner.Type == NodeType.KubernetesCluster ? .78f : .62f;
            float z = owner.Type == NodeType.KubernetesCluster ? .5f : .32f;
            var a = new Vector3(-x, -.12f, -z); var b = new Vector3(x, -.12f, -z);
            var c = new Vector3(x, -.12f, z); var d = new Vector3(-x, -.12f, z);
            var e = a + Vector3.up * .4f; var f = b + Vector3.up * .4f; var g = c + Vector3.up * .4f; var h = d + Vector3.up * .4f;
            outline.SetPositions(new[] { a, b, c, d, a, e, f, b, f, g, c, g, h, d, h, e });
            Refresh();
        }
        public void Add(GraphNode node) { members.Add(node); node.transform.SetParent(transform, true); Refresh(); }
        public void Toggle() { Expanded = !Expanded; Refresh(); }
        private void LateUpdate() { if (members.RemoveAll(n => n == null) > 0) Refresh(); }
        public void Refresh()
        {
            if (owner == null) return;
            owner.GetComponent<MeshRenderer>().enabled = !Expanded || members.Count == 0;
            outline.enabled = Expanded && members.Count > 0;
            bool cluster = owner.Type == NodeType.KubernetesCluster;
            owner.GetComponent<BoxCollider>().size = Expanded && members.Count > 0 ? new Vector3(cluster ? 1.56f : 1.24f, .025f, cluster ? 1 : .64f) : originalSize;
            owner.GetComponent<BoxCollider>().center = Expanded && members.Count > 0 ? new Vector3(0, -.14f, 0) : originalCenter;
            for (int i = 0; i < members.Count; i++)
            {
                members[i].gameObject.SetActive(Expanded);
                members[i].transform.localPosition = cluster ? new Vector3((i % 2 - .5f) * .75f, .02f, (i / 2 - .5f) * .48f)
                    : new Vector3((i % 4 - 1.5f) * .29f, .02f, (i / 4 - .5f) * .3f);
                members[i].transform.localScale = Vector3.one * (cluster ? .5f : .55f);
            }
        }
    }
}
