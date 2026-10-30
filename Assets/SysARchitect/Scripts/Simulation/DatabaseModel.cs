using System;
using System.Collections.Generic;
using SysARchitect.Core;

namespace SysARchitect.Simulation
{
    public enum DatabaseMode { Single, Replication, Sharding }

    public sealed class DatabaseMember
    {
        public int Index { get; internal set; }
        public NodeStatus Status { get; internal set; } = NodeStatus.Healthy;
        public IReadOnlyDictionary<int, long> Records => records;
        public long Reads { get; internal set; }
        public long Writes { get; internal set; }
        internal readonly Dictionary<int, long> records = new Dictionary<int, long>();
        internal double RecoverAt, Credit;
        internal int Budget, Started;
        public double Cpu { get; internal set; }
        public double MemoryMB { get; internal set; } = 32;
        public long Completed { get; internal set; }
        public long Failed { get; internal set; }
    }

    /// <summary>Logical record versions: copies for replication, partitions for sharding.
    /// Uses the simulation clock; render callbacks never apply database state.</summary>
    public sealed class DatabaseModel
    {
        private sealed class Update
        {
            internal DatabaseMember Member;
            internal int Key;
            internal long Version;
            internal double Due;
        }
        private readonly Dictionary<int, long> records = new Dictionary<int, long>();
        private readonly List<DatabaseMember> members = new List<DatabaseMember>();
        private readonly List<Update> pending = new List<Update>();
        private long version;
        private int nextReader;
        public DatabaseMode Mode { get; private set; }
        public IReadOnlyList<DatabaseMember> Members => members;
        public int Revision { get; private set; }
        public double Clock { get; private set; }
        public double SynchronizationDelaySeconds { get; } = .5;
        public int LogicalRecordCount => records.Count;
        public int PendingUpdates => pending.Count;
        public long Reads { get; private set; }
        public long Writes { get; private set; }
        public long FailedOperations { get; private set; }
        public int LastMemberIndex { get; private set; } = -1;
        public long OperationSequence { get; private set; }
        public bool LastOperationSucceeded { get; private set; }
        public string LastOperation { get; private set; } = "Choose a mode, then write and read a record key.";

        public DatabaseModel() => members.Add(new DatabaseMember { Index = 0 });

        public void Configure(DatabaseMode mode, int count)
        {
            int maximum = mode == DatabaseMode.Replication ? 4 : mode == DatabaseMode.Sharding ? 8 : 1;
            if (count < 1 || count > maximum) throw new ArgumentOutOfRangeException(nameof(count));
            int total = mode == DatabaseMode.Replication ? count + 1 : count;
            if (Mode == mode && members.Count == total) return;
            if (Mode == DatabaseMode.Replication && mode == Mode)
            {
                // Scaling replication retains existing copies, counters and failure states.
                while (members.Count < total)
                {
                    var member = new DatabaseMember { Index = members.Count };
                    members.Add(member);
                    ScheduleSnapshot(member);
                }
                if (members.Count > total)
                {
                    pending.RemoveAll(update => update.Member.Index >= total);
                    members.RemoveRange(total, members.Count - total);
                }
                nextReader = 0;
                if (LastMemberIndex >= total) LastMemberIndex = -1;
                Revision++;
                return;
            }
            Mode = mode;
            members.Clear();
            pending.Clear();
            nextReader = 0;
            LastMemberIndex = -1;
            for (int i = 0; i < total; i++) members.Add(new DatabaseMember { Index = i });
            foreach (var record in records)
                members[mode == DatabaseMode.Sharding ? ShardFor(record.Key, count) : 0].records[record.Key] = record.Value;
            if (mode == DatabaseMode.Replication)
                for (int i = 1; i < members.Count; i++) ScheduleSnapshot(members[i]);
            Revision++;
        }

        public static int ShardFor(int key, int count)
        {
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
            return (key % count + count) % count;
        }

        public int SelectMember(bool write, int key, bool requireBudget = false)
        {
            int index = Mode == DatabaseMode.Sharding ? ShardFor(key, members.Count) : 0;
            if (Mode == DatabaseMode.Replication && !write)
            {
                index = -1;
                for (int i = 0; i < members.Count - 1; i++)
                {
                    int candidate = 1 + nextReader;
                    nextReader = (nextReader + 1) % (members.Count - 1);
                    if (members[candidate].Status == NodeStatus.Healthy && (!requireBudget || members[candidate].Budget > 0)) { index = candidate; break; }
                }
                if (index < 0 && requireBudget)
                    for (int i = 1; i < members.Count; i++)
                        if (members[i].Status == NodeStatus.Healthy) return SelectMember(write, key, false);
                if (index < 0 && members[0].Status == NodeStatus.Healthy && (!requireBudget || members[0].Budget > 0)) index = 0;
                if (index < 0 && requireBudget) return SelectMember(write, key, false);
            }
            return index;
        }

        public long LogicalVersion(int key) => records.TryGetValue(key, out long value) ? value : 0;
        public long MemberVersion(int index, int key) => index >= 0 && index < members.Count && members[index].records.TryGetValue(key, out long value) ? value : 0;

        public void BeginTick(double capacity, double seconds)
        {
            foreach (var member in members)
            {
                member.Started = 0;
                member.Credit += capacity * seconds;
                member.Budget = (int)System.Math.Floor(member.Credit + 1e-9);
                member.Credit -= member.Budget;
            }
        }

        public bool TryStart(int index)
        {
            if (index < 0 || index >= members.Count || members[index].Status != NodeStatus.Healthy || members[index].Budget <= 0) return false;
            members[index].Budget--; members[index].Started++;
            return true;
        }

        public void EndTick(double capacity, double seconds)
        {
            foreach (var member in members)
            {
                double usage = member.Status == NodeStatus.Healthy && capacity > 0 ? System.Math.Min(1, member.Started / (capacity * seconds)) : 0;
                member.Cpu += .2 * (usage - member.Cpu);
                member.MemoryMB = 32 + member.records.Count * .125;
            }
        }

        public bool Execute(bool write, int key, out string error, int? routedMember = null)
        {
            OperationSequence++;
            LastOperationSucceeded = false;
            int index = routedMember ?? SelectMember(write, key);
            LastMemberIndex = index;
            if (index < 0 || index >= members.Count || members[index].Status != NodeStatus.Healthy)
            {
                FailedOperations++;
                if (index >= 0 && index < members.Count) members[index].Failed++;
                error = index < 0 || index >= members.Count ? "No database member is available" : $"{MemberName(index)} unavailable";
                LastOperation = $"{(write ? "Write" : "Read")} key {key}: {error}.";
                return false;
            }
            var member = members[index];
            member.Completed++;
            if (write)
            {
                records[key] = ++version;
                member.records[key] = version;
                member.Writes++;
                Writes++;
                LastOperation = $"Write key {key} v{version} > {MemberName(index)}.";
                if (Mode == DatabaseMode.Replication)
                    for (int i = 1; i < members.Count; i++)
                        if (members[i].Status == NodeStatus.Healthy) Schedule(members[i], key, version);
            }
            else
            {
                Reads++;
                member.Reads++;
                bool found = member.records.TryGetValue(key, out long localVersion);
                bool stale = records.TryGetValue(key, out long latest) && (!found || latest != localVersion);
                LastOperation = $"Read key {key} < {MemberName(index)}: " +
                    (found ? $"v{localVersion}" : "not found") + (stale ? " (replica is lagging)." : ".");
            }
            error = null;
            LastOperationSucceeded = true;
            return true;
        }

        public string MemberName(int index) => Mode == DatabaseMode.Replication
            ? index == 0 ? "Primary" : $"Replica {index}"
            : Mode == DatabaseMode.Sharding ? $"Shard {index}" : "Database";

        public int PendingFor(int index)
        {
            int count = 0;
            foreach (var update in pending) if (update.Member == members[index]) count++;
            return count;
        }

        public void FailMember(int index)
        {
            var member = members[index];
            member.Status = NodeStatus.Crashed;
            pending.RemoveAll(update => update.Member == member);
        }

        public void RecoverMember(int index)
        {
            if (members[index].Status != NodeStatus.Crashed) return;
            members[index].Status = NodeStatus.Recovering;
            members[index].RecoverAt = Clock + 1;
        }

        public void AdvanceTo(double time, bool available = true)
        {
            if (double.IsNaN(time) || double.IsInfinity(time) || time < Clock - 1e-9)
                throw new ArgumentOutOfRangeException(nameof(time));
            Clock = time;
            if (!available) return;
            foreach (var member in members)
                if (member.Status == NodeStatus.Recovering && Clock + 1e-9 >= member.RecoverAt)
                {
                    member.Status = NodeStatus.Healthy;
                    if (Mode == DatabaseMode.Replication && member.Index > 0) ScheduleSnapshot(member);
                }
            for (int i = 0; i < pending.Count;)
            {
                var update = pending[i];
                if (Clock + 1e-9 < update.Due) { i++; continue; }
                if (Mode == DatabaseMode.Replication && members[0].Status != NodeStatus.Healthy) { i++; continue; }
                if (update.Member.Status == NodeStatus.Healthy)
                {
                    if (!update.Member.records.TryGetValue(update.Key, out long old) || old < update.Version)
                        update.Member.records[update.Key] = update.Version;
                }
                pending.RemoveAt(i);
            }
        }

        public void Reset()
        {
            records.Clear(); pending.Clear(); version = Reads = Writes = FailedOperations = 0;
            Clock = 0; nextReader = 0; LastMemberIndex = -1;
            OperationSequence = 0; LastOperationSucceeded = false;
            LastOperation = "Database reset; mode and member count preserved.";
            foreach (var member in members)
            {
                member.records.Clear(); member.Status = NodeStatus.Healthy;
                member.Reads = member.Writes = member.Completed = member.Failed = 0; member.RecoverAt = member.Credit = member.Cpu = 0; member.Budget = member.Started = 0; member.MemoryMB = 32;
            }
        }

        private void ScheduleSnapshot(DatabaseMember member)
        {
            foreach (var record in records) Schedule(member, record.Key, record.Value);
        }

        private void Schedule(DatabaseMember member, int key, long value)
        {
            pending.Add(new Update { Member = member, Key = key, Version = value, Due = Clock + SynchronizationDelaySeconds });
        }
    }
}
