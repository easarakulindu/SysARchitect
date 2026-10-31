# Database-router desktop investigation — October 31, 2026

## Reproduced cause

The three existing Linux folders contain different runtime assemblies. Executable
timestamps alone are misleading because Unity's native launcher is reused.

| Existing folder | Runtime assembly SHA-256 before this fix | Observed behavior |
| --- | --- | --- |
| `Builds/Linux-DatabaseDemo` | `d1e1997ff4e7f200d1c5834121f630d4e4342c879cd099b0afecffdcefb8a60b` | No routing-role controls; Backend to LB rejected |
| `Builds/Linux-RoutingDemo` | `9478f89ef586e2f00cada10063317b55e3bac9ce1790bede30083c3db342908b` | Explicit Database Router role accepts both required edges |
| `Builds/Linux` | `270740b567995100bf76d1622ff88e4ccc7833097cdc375b6b9cbe92bad28bda` | Earlier simulation build; not the current routing delivery |

The first two behaviors were reproduced in standalone Linux players with actual
toolbox, tabletop, selection and Connect mouse events, with desktop screenshots.
The reported rejection is reproducible in the older DatabaseDemo. This does not
prove which executable the user launched in every earlier manual test.

Legacy reproduction:

1. Launch `Builds/Linux-DatabaseDemo/SysARchitect.x86_64`.
2. Place Backend, Load Balancer and Database using the toolbox and tabletop.
3. Select / Move, then click Load Balancer: there is no routing-role panel.
4. Connect, click Backend, then Load Balancer. Feedback reads:
   “Backend typically does not connect to a Load Balancer. The Load Balancer
   distributes requests to Backends.” No edge is created.

In the pre-fix RoutingDemo, selecting Database Router before making the two links
works. Leaving the default Application role rejects database-oriented links as
intended. Reverse selection was unsupported, and reversing an existing link
reported a cycle. The old role regression bypassed desktop edge creation with
`graph.AddConnection`, so it did not cover this workflow.

## Corrected workflow

Launch `./RunDesktopDemo.sh` from the project root. The current player displays
`v0.3.1-router-fix` in the feedback area and logs its version and data directory.
The build includes `BUILD-INFO.txt` with UTC build time, Unity version and scene
dependency hash. The Unity build menu now targets `Builds/Linux-RouterFix` by
default. Previous demo folders are preserved as historical artifacts.

1. Place Backend, Load Balancer, Database, and a Frontend to generate traffic.
2. Choose **Select / Move**, click the Load Balancer, then **Database Routing**
   in **ROUTING / CACHE**. The node subtitle becomes **DB Router**. The active
   role button is disabled; role-change feedback stays visible.
3. Choose **Connect source > target** for each link. Create Frontend > Backend,
   Backend > Router and Router > Database.
4. For the two database-router links, either endpoint can be clicked first.
   The resulting directed edges always remain Backend > Router > Database.
   Other links retain source-first order. An application LB is never silently
   reconfigured or reversed into a database router.
5. Select Database to configure its existing replication/sharding model. Run
   frontend traffic to exercise the entire path. Explorer Read/Write buttons
   deliberately submit directly to Database and do not demonstrate router traffic.

Every attempted edge still passes `ArchitectureGraph.ValidateConnection`.
Duplicate links, cycles, mixed roles, multiple storage targets and unsupported
node combinations stay invalid. Rejections display the validator's exact reason;
role mismatch messages now spell out the required UI steps. Incompatible links
must be removed before switching role. Deletion/reconnection uses the existing
edge editor, with no replacement graph or routing simulation.

## Verification

The new Play Mode regression raycasts the visible UI buttons, changes the role,
and creates both links through `ConnectionCreationController.HandlePointerClick`,
the same screen-space picker called by desktop mouse input. It covers both click
orders, duplicate feedback, default-role rejection, incompatible-role feedback,
and read/write transfers across both actual router edges. Existing resource,
cache, replication, sharding, failure and connection-editing tests are retained.

Validation runs in `/tmp/SysARchitectValidation` using Unity 6000.6.5f1 to preserve
the user's open editor. No scene, prefab, metadata or package changes are needed.
AR integration remains on hold; no AR device claims are made.

All **50 Edit Mode and 17 Play Mode tests passed**. Result files are
`/tmp/router-fix-core.xml` and `/tmp/router-fix-play.xml`; accompanying logs use
the same basename with `.log`. The focused UI regression initially exposed
test-harness issues (a slash in a GameObject name and panel activation/render
timing); its final version uses visible button hit targets and frame boundaries.
The final full-suite run includes that corrected test. No simulator behavior
changes were required for this fix.

The Linux build succeeded (108,701,985 bytes), with build log
`/tmp/router-fix-linux-build.log`. The delivered runtime assembly SHA-256 is
`bde0e3225d3a9675dd22c1cd88d6bfbc1d1e317e7fabaf717c5d41daa51acb29`.
The player was launched through `RunDesktopDemo.sh` and checked with compositor
mouse input, rather than graph API calls. Verified:

- Default-role rejection displays the full validation reason and role-switch steps.
- Database Routing can be selected with a visible, persistent confirmation.
- Backend-first and router-first selections create Backend > Router; Database-first
  selection creates Router > Database. Arrows point in the intended direction.
- Clicking the Backend > Router edge highlights it yellow. Delete Connection
  removes only that edge; router-first reconnection restores the original direction.
- Frontend traffic visibly traverses both router links. At the single-database
  checkpoint: 550 sent = 540 successful + 10 in flight, zero failures; Database
  reported 405 reads and 135 writes. Router processed 543, consistent with its
  downstream requests still being in flight.
- Switching the same connected Database to replication and adding a replica
  preserved the path. The selected Replica 3 reported 85 reads and zero writes.
- Switching that cluster to sharding preserved all three graph edges and routed
  requests to internal shards. The final checkpoint showed 1,419 sent = 1,408
  successful + 11 in flight, zero failures/timeouts; Database reported 1,056 reads
  and 352 writes. Selected Shard 2 reported 121 reads and 41 writes.

Screenshots are in `Builds/Linux-RouterFix/Verification/`, including legacy
comparison, role controls, edge deletion/reconnection, packets and member counters.
Player log: `/tmp/router-fix-linux-player.log`; no application exception or missing
reference appeared. The test player and temporary virtual mouse service were
stopped after verification. The user's Unity editor was left open. Existing
host DPI/unused shader messages and a Wayland shutdown warning remain.

Remaining limits: automatic reversal applies only to explicitly configured
Database Router links; other links still require source-first selection. Dense
layouts can need moving nodes or dismissing details. Physical-hand input and AR
device integration are not claimed by the compositor-input walkthrough.
