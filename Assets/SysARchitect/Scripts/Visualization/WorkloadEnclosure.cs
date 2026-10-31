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
        private GameObject glass;
        private Material glassMaterial;
        private NodeVisual ownerVisual;
        private Material outlineMaterial;
        public bool Expanded { get; private set; } = true;
        public void Initialize(GraphNode node)
        {
            owner = node; originalSize = node.GetComponent<BoxCollider>().size; originalCenter = node.GetComponent<BoxCollider>().center;
            ownerVisual = node.GetComponent<NodeVisual>();
            var go = new GameObject("Holographic workload enclosure"); go.layer = 2; go.transform.SetParent(transform, false);
            outline = go.AddComponent<LineRenderer>(); outline.useWorldSpace = false;
            outlineMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = Color.white };
            outline.sharedMaterial = outlineMaterial; outline.widthMultiplier = .006f * transform.lossyScale.x;
            outline.startColor = outline.endColor = Color.white;
            outline.positionCount = 16;
            float x = owner.Type == NodeType.KubernetesCluster ? .78f : .62f;
            float z = owner.Type == NodeType.KubernetesCluster ? .5f : .32f;
            var a = new Vector3(-x, -.12f, -z); var b = new Vector3(x, -.12f, -z);
            var c = new Vector3(x, -.12f, z); var d = new Vector3(-x, -.12f, z);
            var e = a + Vector3.up * .4f; var f = b + Vector3.up * .4f; var g = c + Vector3.up * .4f; var h = d + Vector3.up * .4f;
            outline.SetPositions(new[] { a, b, c, d, a, e, f, b, f, g, c, g, h, d, h, e });
            glass = GameObject.CreatePrimitive(PrimitiveType.Cube); glass.name = "Transparent workload shell"; glass.layer = 2;
            Destroy(glass.GetComponent<Collider>()); glass.transform.SetParent(transform, false);
            glass.transform.localPosition = new Vector3(0, .08f, 0); glass.transform.localScale = new Vector3(x * 2, .4f, z * 2);
            // A referenced Resources asset retains the transparent URP variant in players.
            // A runtime-only keyword can be stripped from desktop builds with opaque scenes.
            var template = Resources.Load<Material>("WorkloadGlass");
            if (template == null) throw new System.InvalidOperationException("Missing WorkloadGlass resource material.");
            glassMaterial = new Material(template);
            Color tint = NodeColorPalette.GetAccentColor(owner.Type); tint.a = .065f; glassMaterial.SetColor("_BaseColor", tint);
            glassMaterial.SetFloat("_Surface", 1); glassMaterial.SetFloat("_ZWrite", 0);
            glassMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            glassMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            glassMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); glassMaterial.renderQueue = 3000;
            var renderer = glass.GetComponent<MeshRenderer>(); renderer.sharedMaterial = glassMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
            Refresh();
        }
        public void Add(GraphNode node) { members.Add(node); node.transform.SetParent(transform, true); Refresh(); }
        public void Toggle() { Expanded = !Expanded; Refresh(); }
        private void LateUpdate()
        {
            if (members.RemoveAll(n => n == null) > 0) Refresh();
            outlineMaterial.color = ownerVisual.IsSelected ? NodeColorPalette.Selected
                : owner.Status == NodeStatus.Healthy ? NodeColorPalette.GetAccentColor(owner.Type) : NodeColorPalette.GetStatusColor(owner.Status);
        }
        public void Refresh()
        {
            if (owner == null) return;
            owner.GetComponent<MeshRenderer>().enabled = !Expanded || members.Count == 0;
            outline.enabled = Expanded && members.Count > 0;
            glass.SetActive(outline.enabled);
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
        private void OnDestroy()
        {
            if (glassMaterial != null) Destroy(glassMaterial);
            if (outlineMaterial != null) Destroy(outlineMaterial);
        }
    }
}
