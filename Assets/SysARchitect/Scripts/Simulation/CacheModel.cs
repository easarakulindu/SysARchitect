using System.Collections.Generic;

namespace SysARchitect.Simulation
{
    /// <summary>Bounded, deterministic cache-aside record versions, scoped to the database cluster.</summary>
    public sealed class CacheModel
    {
        private readonly Dictionary<(string database, int key), long> records = new Dictionary<(string, int), long>();
        private readonly Queue<(string database, int key)> insertionOrder = new Queue<(string, int)>();
        public int Count => records.Count;
        public long Hits { get; private set; }
        public long Misses { get; private set; }
        public long Populations { get; private set; }
        public long Invalidations { get; private set; }
        public long Evictions { get; private set; }
        public bool Read(string database, int key, out long version)
        {
            bool hit = records.TryGetValue((database, key), out version);
            if (hit) Hits++; else Misses++;
            return hit;
        }
        public void Populate(string database, int key, long version)
        {
            var id = (database, key);
            if (records.ContainsKey(id)) { records[id] = version; return; }
            while (records.Count >= 128 && insertionOrder.Count > 0)
                if (records.Remove(insertionOrder.Dequeue())) Evictions++;
            records[id] = version; insertionOrder.Enqueue(id); Populations++;
        }
        public void Invalidate(string database, int key)
        {
            if (records.Remove((database, key))) Invalidations++;
            // Compact tombstones so repeated write/invalidate cycles cannot grow storage unboundedly.
            int count = insertionOrder.Count;
            for (int i = 0; i < count; i++)
            {
                var entry = insertionOrder.Dequeue();
                if (records.ContainsKey(entry)) insertionOrder.Enqueue(entry);
            }
        }
        public void Clear() { records.Clear(); insertionOrder.Clear(); }
        public void Reset() { Clear(); Hits = Misses = Populations = Invalidations = Evictions = 0; }
    }
    public enum RequestPathKind { Request, ApplicationRoute, DatabaseRead, DatabaseWrite, CacheLookup, CacheHit, CacheMiss, CacheFill }
}
