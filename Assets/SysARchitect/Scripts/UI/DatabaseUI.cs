using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SysARchitect.Core;
using SysARchitect.Graph;
using SysARchitect.Simulation;
using SysARchitect.Visualization;

namespace SysARchitect.UI
{
    public class DatabaseUI : MonoBehaviour
    {
        [SerializeField] private InspectorUI inspector;
        [SerializeField] private SimulationController simulation;
        [SerializeField] private GameObject panel;
        [SerializeField] private Button expandButton, singleButton, replicationButton, addReplicaButton, shardingButton;
        [SerializeField] private Button fewerShardsButton, moreShardsButton, readButton, writeButton, failMemberButton, recoverMemberButton;
        [SerializeField] private TMP_InputField keyInput;
        [SerializeField] private Slider memberSlider;
        [SerializeField] private TextMeshProUGUI detailsText;
        private readonly StringBuilder text = new StringBuilder();
        private GraphNode Current => inspector.CurrentNode != null && inspector.CurrentNode.Type == NodeType.Database ? inspector.CurrentNode : null;
        private DatabaseModel Model => Current == null ? null : simulation.Engine.FindNode(Current.NodeId)?.Database;
        private int Member => Mathf.Clamp(Mathf.RoundToInt(memberSlider.value), 0, Model.Members.Count - 1);

        private void Start()
        {
            expandButton.onClick.AddListener(() => { if (Current != null) Current.GetComponent<DatabaseClusterVisual>().Expanded ^= true; });
            singleButton.onClick.AddListener(() => Configure(DatabaseMode.Single, 1));
            replicationButton.onClick.AddListener(() => Configure(DatabaseMode.Replication, 2));
            addReplicaButton.onClick.AddListener(() => Configure(DatabaseMode.Replication, Model.Mode == DatabaseMode.Replication ? Mathf.Min(4, Model.Members.Count) : 1));
            shardingButton.onClick.AddListener(() => Configure(DatabaseMode.Sharding, 3));
            fewerShardsButton.onClick.AddListener(() => Configure(DatabaseMode.Sharding, Model.Mode == DatabaseMode.Sharding ? Mathf.Max(1, Model.Members.Count - 1) : 3));
            moreShardsButton.onClick.AddListener(() => Configure(DatabaseMode.Sharding, Model.Mode == DatabaseMode.Sharding ? Mathf.Min(8, Model.Members.Count + 1) : 3));
            readButton.onClick.AddListener(() => Submit(false));
            writeButton.onClick.AddListener(() => Submit(true));
            failMemberButton.onClick.AddListener(() =>
            {
                if (Model == null) return;
                Model.FailMember(Member);
                Current.GetComponent<DatabaseClusterVisual>().Expanded = true;
            });
            recoverMemberButton.onClick.AddListener(() => { if (Model != null) Model.RecoverMember(Member); });
            memberSlider.wholeNumbers = true;
            keyInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            keyInput.SetTextWithoutNotify("1");
        }

        private void Configure(DatabaseMode mode, int count)
        {
            if (Model == null) return;
            Model.Configure(mode, count);
            Current.GetComponent<DatabaseClusterVisual>().Expanded = true;
        }

        private void Submit(bool write)
        {
            if (Model == null || !int.TryParse(keyInput.text, out int key)) return;
            simulation.SubmitDatabaseRequest(Current, write, key);
        }

        private void Update()
        {
            var model = Model;
            panel.SetActive(model != null);
            if (model == null) return;
            memberSlider.maxValue = Mathf.Max(1, model.Members.Count - 1);
            memberSlider.interactable = model.Members.Count > 1;
            addReplicaButton.interactable = model.Mode != DatabaseMode.Replication || model.Members.Count < 5;
            text.Clear();
            text.Append(model.Mode == DatabaseMode.Replication ? "REPLICATION: copies of logical records; primary writes, replica reads. Sync delay 0.5s.\n"
                : model.Mode == DatabaseMode.Sharding ? $"SHARDING: records partition by normalized key % {model.Members.Count}. No extra copies.\n"
                : "SINGLE DATABASE: one copy. Choose replication for copies or sharding for partitions.\n");
            text.Append($"Logical keys {model.LogicalRecordCount} | Reads {model.Reads} | Writes {model.Writes} | Pending copies {model.PendingUpdates}\n");
            foreach (var member in model.Members)
                text.Append($"{member.Index}:{(member.Status == NodeStatus.Healthy ? "up" : member.Status.ToString())} keys {member.Records.Count}   ");
            var selected = model.Members[Member];
            text.Append($"\nSelected {model.MemberName(Member)}: {selected.Status}, CPU {selected.Cpu:P0}, memory {selected.MemoryMB:F1} MB, R/W {selected.Reads}/{selected.Writes}, pending {model.PendingFor(Member)}\n{model.LastOperation}\n");
            text.Append("Capacity applies per member; replicas add reads, not primary write capacity. Run/Step also advances 1s recovery and 0.5s sync.");
            detailsText.text = text.ToString();
        }
    }
}
