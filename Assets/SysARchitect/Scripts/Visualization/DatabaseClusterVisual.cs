using System.Collections.Generic;
using TMPro;
using UnityEngine;
using SysARchitect.Core;
using SysARchitect.Graph;
using SysARchitect.Simulation;

namespace SysARchitect.Visualization
{
    /// <summary>One selectable graph node owns its visual members; members are not extra infrastructure nodes.</summary>
    public class DatabaseClusterVisual : MonoBehaviour
    {
        private GraphNode node;
        private DatabaseModel model;
        private GameObject enclosure;
        private readonly List<Transform> members = new List<Transform>();
        private readonly List<TextMeshPro> labels = new List<TextMeshPro>();
        private readonly List<MeshRenderer> renderers = new List<MeshRenderer>();
        private readonly List<RequestPacketVisual> syncPackets = new List<RequestPacketVisual>();
        private Mesh memberMesh;
        private MeshRenderer rootRenderer;
        private BoxCollider rootCollider;
        private Bounds originalBounds;
        private int revision = -1;
        private bool builtExpanded;
        private double previousClock;
        private Transform accessPort;
        private RequestPacketVisual accessPacket;
        private long lastOperation;
        public bool Expanded { get; set; }
        public int VisibleMemberCount => members.Count;

        public void Initialize(GraphNode owner, DatabaseModel database)
        {
            node = owner; model = database;
            rootRenderer = GetComponent<MeshRenderer>();
            rootCollider = GetComponent<BoxCollider>();
            originalBounds = new Bounds(rootCollider.center, rootCollider.size);
        }

        private void LateUpdate()
        {
            if (model == null) return;
            if (revision != model.Revision || builtExpanded != Expanded) Rebuild();
            float delta = (float)System.Math.Max(0, model.Clock - previousClock);
            if (model.Clock < previousClock)
            {
                for (int i = 0; i < syncPackets.Count; i++)
                {
                    if (syncPackets[i] != null)
                    {
                        syncPackets[i].gameObject.SetActive(false);
                        Destroy(syncPackets[i].gameObject);
                    }
                    syncPackets[i] = null;
                }
                if (accessPacket != null) { accessPacket.gameObject.SetActive(false); Destroy(accessPacket.gameObject); }
                accessPacket = null;
                lastOperation = model.OperationSequence;
            }
            previousClock = model.Clock;
            if (Expanded && model.OperationSequence != lastOperation && model.LastMemberIndex >= 0 && accessPacket == null)
            {
                accessPacket = RequestPacketVisual.Create(rootRenderer.sharedMaterial);
                accessPacket.gameObject.name = "Database key route";
                accessPacket.transform.SetParent(enclosure.transform, false);
                accessPacket.IsPaused = true;
                var target = members[model.LastMemberIndex];
                accessPacket.Launch(accessPort, target,
                    model.LastOperationSucceeded ? RequestPacketVisual.Success : RequestPacketVisual.Failure,
                    Vector3.Distance(accessPort.position, target.position) / .3f, null);
                lastOperation = model.OperationSequence;
            }
            if (accessPacket != null && delta > 0) accessPacket.Advance(delta);
            for (int i = 0; i < members.Count; i++)
            {
                var state = model.Members[i];
                bool wholeDatabaseDown = node.Status == NodeStatus.Crashed || node.Status == NodeStatus.Recovering;
                NodeStatus health = wholeDatabaseDown ? node.Status : state.Status;
                Color color = health == NodeStatus.Healthy ? NodeColorPalette.Database : NodeColorPalette.GetStatusColor(health);
                if (i == model.LastMemberIndex && health == NodeStatus.Healthy) color = Color.Lerp(color, Color.cyan, .4f);
                var block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", color);
                renderers[i].SetPropertyBlock(block);
                int waiting = model.PendingFor(i);
                labels[i].text = $"{model.MemberName(i)}\n{state.Records.Count} keys\n" +
                    (health != NodeStatus.Healthy ? health.ToString() : waiting > 0 ? $"Syncing {waiting}" : "Ready");
                if (Camera.main != null) labels[i].transform.rotation = Camera.main.transform.rotation;
                if (model.Mode != DatabaseMode.Replication || i == 0) continue;
                bool primaryDown = model.Members[0].Status != NodeStatus.Healthy;
                if ((wholeDatabaseDown || primaryDown || health != NodeStatus.Healthy) && syncPackets[i] != null)
                {
                    Destroy(syncPackets[i].gameObject);
                    syncPackets[i] = null;
                }
                if (syncPackets[i] == null && waiting > 0 && !wholeDatabaseDown && !primaryDown)
                {
                    var packet = RequestPacketVisual.Create(rootRenderer.sharedMaterial);
                    packet.gameObject.name = "Replication write propagation";
                    packet.transform.SetParent(enclosure.transform, false);
                    packet.IsPaused = true;
                    float speed = Vector3.Distance(members[0].position, members[i].position) / (float)model.SynchronizationDelaySeconds;
                    packet.Launch(members[0], members[i], new Color(.8f, .45f, 1), speed, null);
                    syncPackets[i] = packet;
                }
                if (syncPackets[i] != null && delta > 0) syncPackets[i].Advance(delta);
            }
        }

        private void Rebuild()
        {
            if (enclosure != null) { enclosure.SetActive(false); Destroy(enclosure); }
            members.Clear(); labels.Clear(); renderers.Clear(); syncPackets.Clear();
            revision = model.Revision; builtExpanded = Expanded;
            accessPacket = null;
            lastOperation = model.OperationSequence;
            rootRenderer.enabled = !Expanded;
            rootCollider.center = originalBounds.center; rootCollider.size = originalBounds.size;
            if (!Expanded) return;
            if (memberMesh == null) memberMesh = ProceduralMeshGenerator.GenerateHolographicMesh(NodeType.Database);
            enclosure = new GameObject("Database enclosure");
            enclosure.transform.SetParent(transform, false);
            accessPort = new GameObject("Database request port").transform;
            accessPort.SetParent(enclosure.transform, false);
            accessPort.localPosition = new Vector3(0, 0, -.125f);
            int count = model.Members.Count;
            rootCollider.size = new Vector3(Mathf.Max(.3f, count * .23f), .22f, .25f);
            for (int i = 0; i < count; i++)
            {
                var member = new GameObject(model.MemberName(i), typeof(MeshFilter), typeof(MeshRenderer));
                member.transform.SetParent(enclosure.transform, false);
                member.transform.localPosition = new Vector3((i - (count - 1) * .5f) * .22f, 0, 0);
                member.transform.localScale = Vector3.one * .55f;
                member.GetComponent<MeshFilter>().sharedMesh = memberMesh;
                var renderer = member.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = rootRenderer.sharedMaterial;
                renderers.Add(renderer); members.Add(member.transform); syncPackets.Add(null);
                var labelObject = new GameObject("Member label");
                labelObject.transform.SetParent(enclosure.transform, false);
                labelObject.transform.localPosition = member.transform.localPosition + Vector3.up * .15f;
                var label = labelObject.AddComponent<TextMeshPro>();
                label.fontSize = .22f; label.alignment = TextAlignmentOptions.Center;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.rectTransform.sizeDelta = new Vector2(.35f, .15f);
                label.raycastTarget = false;
                labels.Add(label);
                if (model.Mode == DatabaseMode.Replication && i > 0)
                {
                    var link = new GameObject("Primary write propagation link", typeof(LineRenderer)).GetComponent<LineRenderer>();
                    link.transform.SetParent(enclosure.transform, false);
                    link.useWorldSpace = false; link.positionCount = 2; link.widthMultiplier = .004f;
                    link.sharedMaterial = rootRenderer.sharedMaterial;
                    link.SetPosition(0, members[0].localPosition); link.SetPosition(1, member.transform.localPosition);
                }
            }
            var outline = enclosure.AddComponent<LineRenderer>();
            outline.useWorldSpace = false; outline.widthMultiplier = .005f;
            outline.sharedMaterial = rootRenderer.sharedMaterial;
            float x = rootCollider.size.x * .5f, y = .1f, z = .125f;
            var a = new Vector3(-x, -y, -z); var b = new Vector3(x, -y, -z);
            var c = new Vector3(x, -y, z); var d = new Vector3(-x, -y, z);
            var e = new Vector3(-x, y, -z); var f = new Vector3(x, y, -z);
            var g = new Vector3(x, y, z); var h = new Vector3(-x, y, z);
            outline.positionCount = 16;
            outline.SetPositions(new[] { a, b, c, d, a, e, f, b, f, g, c, g, h, d, h, e });
        }

        private void OnDestroy()
        {
            if (memberMesh != null) Destroy(memberMesh);
        }
    }
}
