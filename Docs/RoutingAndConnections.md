# Routing, cache-aside and connection editing

These corrections extend the existing graph, simulator, Database model and input controllers.
The toolbox still has one configurable Load Balancer and one expandable Database entry.

## Supported routing

- **Application Routing** (default): Frontend > Load Balancer > healthy Backends > storage.
  Connected operational backends receive round-robin traffic. Crashed/offline/recovering
  targets are skipped; saturation alone does not crash a backend.
- **Database Router**: Backend > Load Balancer configured as Database Router > one Database
  cluster. Select the router, then choose Database Router before making these links.
  Configure replication/sharding on the Database itself. Both routing roles can be used
  together: application LB > multiple backends > database router > Database cluster.

Switching roles with incompatible existing links displays an explanation and preserves
those links. Remove them first, change role, then reconnect. Backend > an application-role
LB and chains of database routers are outside these supported learning scenarios; the
messages describe these limits rather than claiming those designs are universally invalid.
A database router targets one expandable cluster, not separate independent database nodes.

Newly placed Frontend/LB/Cache nodes default to 1,000 requests/s, Backends to 100 and Database
members to 500. This leaves headroom for backend-scaling demonstrations. Capacity sliders
still apply: frontend, router or storage bottlenecks can limit end-to-end throughput even
when backend capacity increases. A load balancer does not add backend processing capacity.

## Database routing and resources

Replication uses a single primary for **every write**. Reads prefer operational replicas
round-robin and use the primary only when no replica is healthy. Busy replicas queue reads;
they do not turn read-only replicas into write targets. More replicas add read service
budgets, never primary write budgets. Sharding uses normalized integer key modulo shard
count and services the chosen partition. Unavailable members reject affected requests.

The existing DatabaseModel owns all copy/version/lag and shard logic; the router forwards
to it. The capacity slider now means **capacity per member**, with separate service credits
and CPU/read/write/completion counters. Cluster CPU is the mean member CPU; the explorer
shows the selected member's utilization and throughput. Queue/deadline/memory limits remain
cluster-wide. Record storage contributes to simulated memory. Replication delay stays
0.5 simulated seconds and recovery stays one second. Reads may return a stale replica
version, explicitly labelled by the explorer. A successful query returning no record is
still a successful query. Automatic leader promotion is not implemented. In-flight requests whose member was replaced
by live reconfiguration obtain a new reservation; old indices cannot write to a replica or
wrong shard.

## Cache-aside

Connect the Backend to **both one Cache and one Database or Database Router**. Frontends
and other generic component types do not call Cache directly in this learning model.
Cache does not initiate database requests. On a read:

1. Backend requests its Cache over the actual Backend > Cache edge.
2. A hit completes without a database query.
3. A miss returns control to Backend (responses are implicit), which requests storage over
   its Backend > Database/Router edge.
4. A successful storage result fills Cache over Backend > Cache, consuming cache service
   capacity. Missing or stale versions are not inserted. A concurrent committed write
   cannot cause an old in-flight read to repopulate an invalidated version.

Writes bypass Cache and invalidate matching cluster/key entries after commit. Keys include
cluster identity, so sharing a Cache does not mix identical keys from different databases.
This educational model applies invalidation immediately, without modeling a separate
invalidation network or replica synchronization CPU cost. Cache is bounded to 128 entries
with deterministic FIFO eviction; TTL and negative caching are not implemented.

An already-offline Cache is bypassed. Failure of an in-flight lookup can fail that request;
future reads bypass it. Failure of optional population after a successful database read
can still return success. Reset clears cache records/counters. Selecting Cache shows hits,
misses, populations, invalidations and evictions. Automatic traffic cycles 32 keys with
25% writes and repeated reads, producing actual warm-cache hits rather than random hit
probabilities. Database Explorer Read/Write exercises intentionally enter the database
queue directly; use frontend traffic to exercise the routers/cache path.

A repeatable 50-second, 60 requests/s check completed all 3,000 requests without failures.
Database reads were 2,250 without caching and 1,712 with caching, with 538 hits (~24% fewer
reads in that specific workload). Gains depend on repetition, writes and bottlenecks;
caching does not automatically increase backend processing capacity.

## Editing and visuals

- In Select mode, click a line to select it. Node hits take priority. The selected line and
  arrow become wider and yellow. The Connection Inspector displays endpoints/activity.
- Use either **Delete Connection** button, or Delete/Backspace outside a text field.
  Only the edge is removed. Reconnect using Connect > source > target.
- Node deletion still removes all attached edges. Graph edits immediately update simulator
  target lists and remove associated packet visuals. Requests already delivered may finish;
  later hops use the current graph and report missing dependencies.
- Right drag/orbit, middle drag/pan and wheel zoom retain their controls. The screen-space
  picker has a configurable pixel tolerance for a future touch adapter; AR touch is untested.
- Routing/cache details can be dismissed to uncover the workspace. Labels are smaller and
  long unselected names are abbreviated; dense layouts still require spacing and pan/zoom.
- Lines: cyan routing, green cache hit, orange miss/fallback, purple fill, pink write.
  Packets become green/red for their actual logical outcome. Arrowheads/packets share ports
  outside node enclosures, and sampled traffic rotates across active edges. Animation never
  changes counters or simulation outcomes.

## Verification and delivery

Unity 6000.6.5f1, existing packages unchanged. Validation uses the isolated
`/tmp/SysARchitectValidation` because the user's editor is open; no editor was stopped.
**50 Edit Mode and 16 Play Mode cases passed.**
Results: `/tmp/sysarchitect-routing-core.xml` and `/tmp/sysarchitect-routing-play.xml`.
Regression coverage includes scaling, health filtering, role/type feedback, primary-only
writes, read capacity, stale reads, shard routing, cache-aside/coherence/key isolation,
edge picking/deletion/reconnection, typing guards, node cleanup and route recalculation.
Rendered application/database routing, member stats, cache paths and selection were inspected.

The updated Linux demo is delivered separately in `Builds/Linux-RoutingDemo/`, preserving
previous builds. Build log: `/tmp/sysarchitect-routing-linux-build.log`; startup log:
`/tmp/sysarchitect-routing-linux-player.log`. Reload DesktopWorkspace from disk if it is
already open in Unity. Manual physical mouse walkthrough, AR device testing and mobile
profiling remain separate checks. No unrelated milestones or package upgrades were done.
All commits are local. Never push; `.agents` stays ignored and untracked.

The final Linux build and 12-second graphical startup smoke check passed. The test
instance was intentionally stopped by timeout (exit 124); no application exception or
AR camera-background error appeared. Existing host DPI/unused URP postprocessing warnings
remain. Build output reports 108,700,945 bytes. The new build folder also contains the
inspected routing/cache/edge-selection captures. Original builds remain unchanged.
