# SysARchitect takeover — October 30, 2026

Target demonstration: November 2, 2026. No remote pushes are authorized.

**Lesson step-content correction (November 1):** recovered the interrupted edits,
reproduced the unchanged-viewport failure, and moved distinct step headings,
instructions and explanations above static overview text. Navigation restores
scroll position and updates existing component/link focus without simulation
actions. Fresh Unity results: **96 Edit Mode / 47 Play Mode passed**; all five
lessons checked at four phone resolutions. See [root cause and verification](LessonStepContent.md).
Android 0.11.2 packaging is the next verification step; the 0.11.1 APK below is
preserved as the previous delivery.

**Mobile lesson navigation correction (November 1):** the guide now has separate
Previous lesson / Lessons / Next lesson controls and bounded instruction-step
controls. All five topics can be revisited in the picker; completion/quiz storage
and the current graph/simulation are retained. Verified 91 Edit Mode / 44 Play
Mode passes, plus final focused navigation checks and four mobile resolutions.
See [navigation design, regression evidence and Android delivery](LessonNavigation.md).
Feature commit `bca6ef6` has a verified **0.11.1** APK at
`Builds/Android-LessonNavigation/SysARchitect.apk`; packaged navigation controls,
manifest and signing checks passed. Earlier APK/Linux artifacts are preserved.
The correction still needs a physical phone retest; no device was connected here.

**Current approved work:** the October 31 educational/AR request authorizes milestones
A–G and supersedes the earlier AR hold. See [Educational and AR delivery](EducationAndAR.md)
for the current audit, milestone implementation and verification records. Historical
milestone and delivery paths below are retained for provenance. `./RunDesktopDemo.sh`
is the stable current Linux entry point.

**November 1 mobile crash recovery:** recovered committed mobile UI `52ab98c` and
release configuration `5b4daca`; the prior in-flight temporary Android build did
not survive restart. Added optional ARCore and an explicitly non-AR Virtual
Workspace through the existing graph/touch/UI systems. Fresh verification is
91 Edit Mode / 41 Play Mode passed, including actual-scene virtual editing,
database configuration, lessons/quizzes and four phone render resolutions. See
[recovery, verified behavior and deployment limits](MobileRecovery2026-11-01.md).
Feature commit `f79d946` now has verified **0.11.0** Android and Linux builds at
`Builds/Android-RC1/SysARchitect.apk` and `Builds/Linux-RC1/SysARchitect.x86_64`.
APK manifest/signature/ABI checks passed; Linux startup check passed. The existing
Linux-Current launcher/build and old APK were preserved. ADB had no connected
device; physical Android AR/Virtual acceptance and Mac/iOS deployment remain
unverified. Use the RC1 paths for the new builds.

**Crash recovery (October 31):** verified existing A–E commits `ea0f368`,
`be112d4`, `3b8b25a`, `8214677`, `56fbef8` and their test/build records.
Recovered F's uncommitted mobile build helper and input migration without
rebuilding any application systems. Android APK now exists; see
[platform configuration, actual result and M3 MacBook/iPhone handoff](MobileBuilds.md).
No on-device AR or iOS deployment is claimed. The unrelated `dotnet-tools.json`
remains untouched and untracked. G visual/mobile UI polish is the next milestone.

**Usage-limit resume:** A–F are committed through `cceec60`. G's source/assets
were recovered intact and remain separate work in progress. The next small feature,
transparent workload enclosures, was checked in the actual Linux UI and repaired
with a real material asset that retains its player shader variant. See
[recovery, exact reproduction, test results and current build locations](Recovery2026-10-31.md).
Do not mistake the current working-tree demo for a fully committed G milestone.

**Model/indicator review:** the hardware models and monitoring presentation were
isolated from mobile layout, reviewed and tested as a focused candidate. Fresh
results: 85 Edit Mode and 31 Play Mode passed, plus a separate Linux player build
and startup check. See [scope, remaining files and evidence-based feature status](ModelIndicatorReview.md).
The current Android APK predates the gauge corrections; on-device AR remains
unverified. The mobile-layout work is preserved outside this focused commit.

**Mobile UI follow-up:** the preserved layout has been completed and checked at
four landscape phone resolutions with synthetic safe-area insets. Current results
are 91 Edit Mode / 37 Play Mode passed; desktop regressions remain green. See
[mobile UI release-candidate record](MobileUIReleaseCandidate.md) for controls,
reproduced corrections, rendering evidence and physical-device limitations.

**October 31 database-router correction:** the older `Linux-DatabaseDemo` was reproduced
rejecting Backend > LB and having no routing-role controls. The newer routing build accepts
the architecture after explicit role selection. Current delivery uses `./RunDesktopDemo.sh`
and `Builds/Linux-RouterFix` with visible version `0.3.1-router-fix`; see
[reproduction, workflow and validation](DatabaseRouterFix.md). AR integration stays on hold.
The following sections retain the project's historical milestone records.

## Inspected baseline

- Unity 6000.6.5f1 (3ff58d469c8a), URP 17.6.0, Input System 1.20.0,
  AR Foundation / ARCore 6.6.2, XR Interaction Toolkit 3.6.1, Test Framework 1.8.0.
  Versions verified in ProjectVersion, manifest, lockfile and installed package metadata.
  Imported XRI samples are in a 3.5.1 directory; that is not the installed package version.
- Two existing commits: initial template and editor settings. All 21 SysARchitect C#
  files and their metadata were untracked. They contained graph ownership/validation,
  procedural visuals, mouse controllers, toolbox and inspector UI. Preserved in a local
  foundation commit with the missing clear-workspace callback repaired.
- Unity reproduced CS1061 at ToolboxUI.cs:103: missing InteractionManager.OnClearButtonPressed.
  A subsequent Unity batch compilation passed after the repair.
- No repository AGENTS.md, README or .editorconfig existed. The local Unity skill was read.
  No Unity MCP tool is connected. The exact installed editor is usable through batch mode.
- The enabled SampleScene is the mobile AR template, with no SysARchitect components.
  SysARchitect's scenes/materials/prefabs directories were empty. Simulation, database,
  Docker/Kubernetes behavior, lessons, quiz and application AR integration were absent.
- Both input backends are enabled. Android minimum SDK is 34, ARM64 enabled.
  ARCore loader exists; its automatic loading/running flags are off. Standalone has an
  XR Simulation loader. This is template configuration, not a tested AR application.
- The current URP renderer contains the template AR background renderer feature.
  GUID references in the original scene resolve (apart from built-in Unity resources).
  EditorBuildSettings contains two pre-existing unresolved config references for Magic
  Leap and OpenXR packages, neither installed; preserved pending AR configuration work.

## Desktop builder

Open `Assets/SysARchitect/Scenes/DesktopWorkspace.unity`, or use
**SysARchitect > Open Desktop Workspace**, then press Play. The workspace starts empty.

1. Choose a toolbox component and click the tabletop. Repeat for the required nodes.
2. Select a node, drag to move, rotate in the inspector, and adjust name/capacity/memory.
3. Choose Connect, click the source, then the target. Direction is source to target.
4. Remove an edge using its inspector button; Delete removes the selected node and its edges.
5. Validate displays disconnected/missing-component warnings. Clear empties the workspace.
6. Right mouse drag orbits, middle drag pans, wheel zooms. Escape cancels an operation.

The dedicated scene is first in build settings; the original template scene is retained.
The builder uses the inherited graph, visuals, UI and interaction controllers. Cycles,
foreign endpoints, self-links, invalid type links and duplicate edges are rejected.
A runtime assembly definition allows the existing code to be referenced by Unity tests.

Validation: exact-editor compilation and 2 Play Mode tests passed. Tests cover scene
wiring, empty startup, toolbox placement preview/cancel, selection, edge deletion,
rotation, node deletion/cleanup, clear, invalid links, graph membership and cycles.
Headless tests do not establish visual quality or real mouse usability.

## Remaining milestones

1. Builder foundation: implemented and automated lifecycle checks pass; manual visual/input QA pending.
2. Simulation engine: implemented and wired to the desktop scene; see validation below.
3. Database replication/sharding: implemented with an expandable explorer; see current verification below.
4. Five lessons, Docker/Kubernetes controllers, 20-question quiz and progress: absent.
5. Shared AR placement and Android deployment: template only; no device test performed.
6. Visual/mobile polish, performance measurements and full demo validation: pending.

`.agents/` is ignored and must never be staged. All commits are local; never push.

## Deterministic simulation core

`SimulationEngine` uses explicit 50 ms steps and plain C# state. It models bounded FIFO
queues, rate-limited parallel service, end-to-end deadlines, round-robin routing,
load-balancer health filtering, memory exhaustion, injected failure and a one-second
restart state. CPU saturation alone never crashes a node. The default educational
memory model is 32 MB baseline plus 1 MB per queued/processing request. Terminal service
nodes succeed; frontends, load balancers and worker nodes require an outgoing route.
Multiple outgoing service edges choose one target by round-robin (not a fan-out transaction).

The same topology, configuration and tick sequence reproduce results within the same
runtime. No cross-platform floating-point identity is claimed. Packet animation is a
presentation of transfers and does not drive processing. 17 simulation Edit Mode test cases passed,
including request conservation against actual queue/processing counts on every tick.
Scene controls and resource/packet presentation are now integrated.

## Integrated desktop demonstration

The scene has Run, Pause, Step (50 ms), Reset, Build and a 0–500 requests/s traffic slider.
Build pauses simulation so editing can continue. Adding/removing nodes or links updates
simulation topology without rebuilding the scene. Reset preserves topology/configuration,
clears counters/packets and restores node health. The inspector reports CPU, memory,
incoming rate, queue, local processing/failure/rejection/timeout totals and mean node
latency; global counters report end-to-end success/failure and successful latency.
CPU uses a fixed-tick exponential average; fresh transfers do not count as sustained
backlog. Animation is sampled (maximum 64 packets, two new visuals per tick), freezes
on Pause, and never decides outcomes. Overload pulses red; crash/recovery are labelled.

Suggested demonstration:

1. Place Frontend, Load Balancer, two Backends and one Database.
2. Connect Frontend > Load Balancer > each Backend, then each Backend > Database.
3. Set Frontend, Load Balancer and Database capacity to 500 req/s; set each Backend to 40.
4. Run at 60 req/s, inspect balanced backend utilization, then raise traffic to 160 req/s.
   Observe queues, latency and rejected requests. A load balancer alone adds no capacity.
5. Select a Backend and Inject failure. The load balancer skips it; the frontend remains
   running. Restart shows one second of recovery in simulation time. Reduce traffic to
   drain queues, or add/connect backend capacity. Pause, step, then reset.

The first graphical test exposed oversized world labels, incorrectly anchored button
text, and inward-facing cylinder caps. These were repaired and visually rechecked.
Initial automated coverage included 25 Edit Mode cases (17 simulation, 8 mesh winding) and
3 Play Mode cases (builder lifecycle, graph validation, integrated controls and packets).
Test artifacts are in `/tmp/sysarchitect-simulation-tests.xml` and
`/tmp/sysarchitect-integrated-tests.xml`; the rendered demo capture is
`/tmp/sysarchitect-desktop-demo.png`. Full mouse-driven usability is still a manual check.
Container/Pod/Worker nodes currently have generic service behavior and distinct shapes;
Docker images/hosts, deployments and reconciliation are not implemented.

Use **SysARchitect > Build Linux Desktop Demo** for a local build containing only the
desktop workspace. Output: `Builds/Linux/SysARchitect.x86_64` (ignored by Git).

## Next priority before November 2

The next original milestone is the five guided lessons, Docker/Kubernetes behavior and
quiz/progress. Desktop mouse usability QA and architecture save/load remain usability
work. Shared AR placement and Android validation follow. The complete product is not finished; no physical AR, Android build, or
mobile performance verification has been claimed.

## Build verification and local commits

Unity 6000.6.5f1 successfully built the Linux desktop demo (108,617,475 bytes reported;
about 104 MiB on disk). A 12-second graphical player startup smoke test completed with
no application exceptions. The test runner intentionally terminated that instance after
12 seconds (`timeout` exit 124). The initial build tried to start template XR Simulation
and logged a missing camera-background shader error. The desktop build helper now
turns off automatic XR initialization during the build and restores the editor setting
in `finally`; the rebuilt player's log has no camera-background error. Original XR
settings and the AR background renderer feature remain intact.

Remaining startup warnings: the desktop display cannot report DPI; stripped/unused URP
postprocessing shaders report that those passes will not execute. The desktop camera
uses no postprocessing. These warnings, manual input QA, and device AR remain unverified
work rather than claimed fixes. Player/build logs are `/tmp/sysarchitect-linux-player.log`
and `/tmp/sysarchitect-linux-build.log`.

Local implementation commits:

- `b155a09` — ignore `.agents`.
- `be6c71d` — preserve/repair the inherited 21-script builder foundation.
- `0c46547` — wire desktop builder scene and lifecycle tests.
- `36ab056` — deterministic request/resource engine and tests.
- `b185970` — distinguish fresh arrivals from sustained overload.
- `c6d712e` — repair inherited cylinder-cap winding.
- `89ecb4b` — connect simulation controls, live inspector and visual presentation.
- The following build-helper commit contains the desktop-only build entry point and this
  validation record; use `git log` for its hash.

No pushes were made. Validation-generated edits to the original sample material and
URP/volume assets were reversed to their recorded clean baseline, with a review patch
retained at `/tmp/sysarchitect-generated-asset-changes.patch`. The untracked
`dotnet-tools.json` appeared during the session; its origin is not established, so it was
left untouched and excluded from commits. `.agents` is ignored and has no tracked files.

## Connection and request-flow investigation

The user's manual run had placed nodes without using Connect. Reproduced exactly at
134 fixed ticks (6.7 simulated seconds): **201 sent, 199 failed, 2 in flight, 0 succeeded**.
There were zero edges and zero packet visuals. The failure reason was "No service route
configured"; this is expected unrouted-frontend behavior, not overload or a frontend crash.
Run/Step now explain missing routes, Connect prompts for source/target, highlights the
source, explains invalid targets and confirms successful directed connections.

Two independent connected-case regressions failed before repair:

- The arrow tip was at the target's center, occluded by its mesh. Line/arrow ports now
  sit outside enclosures; wider lines and larger arrowheads improve direction visibility.
- A global first-two-transfers limit repeatedly sampled only Frontend > Load Balancer.
  Round-robin sampling now covers actual active edges, with one pending sample per edge
  and the existing 64-packet bound. Packets use the same visible ports as their line,
  follow node movement, and disappear when their connection is removed.

Packets receive logical request IDs and become green/red from engine completion events.
Animation does not change counters or outcomes. Explicit Step advances presentation by
one tick; Pause freezes it. Counters retain the invariant sent = succeeded + failed +
queued/processing. Counts include earlier configurations until Reset.

Validation uses `/tmp/SysARchitectValidation`, an isolated copy because the user has Unity
open. Baseline results: `/tmp/sysarchitect-flow-baseline.xml` (one reproduction passes,
two visual regressions fail). Repaired Play Mode suite: `/tmp/sysarchitect-flow-fixed.xml`
(all 9 pass), including actual camera/raycast source-target picking, invalid-target retry,
line geometry, every-hop sampling, packet position/removal/colors and existing lifecycle.
Rendered output was inspected. Core/resource regressions use `/tmp/sysarchitect-flow-core.xml`.
The next original incomplete milestone is database replication/sharding.

## Database milestone: expandable explorer

One Database toolbox entry now owns a `DatabaseModel` and expandable enclosure. Select a
Database to open **Database Explorer**, then choose Replication (primary + two replicas),
Add replica (up to four replicas), Sharding (one to eight partitions), or Single. Members
are visual children of one selectable graph node; they do not create duplicate graph
nodes or connection systems. The collider/enclosure expands and collapses with the view.

- Replication writes to the primary and copies logical record versions after 0.5 simulated
  seconds. Animated internal write packets, pending-copy counts and member status show
  synchronization. Reads use healthy replicas, falling back to the primary when needed;
  a lagging replica can return a stale version or not found, and the UI explains that.
- Sharding routes keys by normalized `key % shardCount`, including negative integer keys.
  Records live in exactly one shard. Changing shard count redistributes the retained
  logical records; switching modes preserves them. The routed member is highlighted and
  internal access packets show the chosen destination.
- Read/Write queue explicit requests at the selected database. Run or Step processes them
  through existing capacity, queues and deadlines, with the same global success/failure
  counters and node resource monitoring. A successful query with no record is not a
  transport failure. Automatic frontend traffic uses deterministic 25% writes over 32 keys.
- The member slider selects the primary, replica or shard for Fail member / Recover member.
  Recovery takes one simulated second. Recovered replicas catch up after the sync delay.
  Offline replicas do not accumulate an unbounded update backlog. A failed primary rejects
  writes while healthy replicas can still serve reads; automatic primary promotion is not
  modeled. A failed shard affects its keys. Whole-node failure/restart remains available
  in the regular inspector and stops internal replication while offline.
- Reset clears records, counters, health and internal animations while retaining mode and
  member count. Database capacity was aggregate at this checkpoint; the routing correction below now
  uses per-member service budgets with shared cluster memory/queue limits. Record versions and mode changes are an educational model, not a real DB engine.

Quick exercise: place/select a Database, choose Replication, Write key 1, and Step twice.
The primary has the key while replicas are pending. Step another ten ticks and Read to
observe the synchronized copy. Choose Sharding and write keys 1, 2 and 3 to see separate
partitions. Select member 1, fail it, and read key 1; observe a failed request without
crashing unrelated nodes. Recover it and advance at least twenty ticks.

Database-checkpoint validation: **38 Edit Mode cases and 11 Play Mode cases passed** in the isolated
Unity 6000.6.5f1 project. This includes the prior connection/resource tests plus database
copy delay, mode migration, key distribution, outages, recovery, counters, explorer wiring,
member/graph ownership, animations and collapse. Rendered replication and sharding views
were inspected: `/tmp/sysarchitect-database-replication.png` and
`/tmp/sysarchitect-database-sharding.png`. Results: `/tmp/sysarchitect-database-core.xml`
and `/tmp/sysarchitect-database-play.xml`. Physical mouse interaction and device AR are
still separate manual checks; no Android/device success is claimed.

The Unity-authored scene update preserved all pre-existing scene object IDs and the scene
GUID. The source scene's hash was checked before applying it, with no concurrent disk
changes found. The user's running editor was not stopped. New local commits include
`960a0d6` (connection/flow repair), `751bc3e` (database model), `dbd867e` (late-node
clock initialization), `a1c1421` (preserve copies and health when adding replicas), and the
explorer integration commit recorded by `git log`. No pushes; `.agents` remains ignored and untracked, and
unrelated `dotnet-tools.json` remains untouched.


The updated Linux build is in **`Builds/Linux-DatabaseDemo/SysARchitect.x86_64`**, with
its required sibling data/shared-library files. The previous `Builds/Linux` demo is
preserved for comparison. Unity reports 108,666,673 build bytes. Both database screenshots
are also copied into the new build folder. Build log:
`/tmp/sysarchitect-database-linux-build.log`. If DesktopWorkspace is already open in
Unity, reload the scene from disk to pick up the new explorer UI.

The new Linux player also passed a 12-second graphical startup smoke test with no
application exceptions or camera-background initialization error. The test instance was
closed intentionally by `timeout` (exit 124). Existing display-DPI and unused/stripped URP
postprocessing warnings remain, as in the prior build. Player log:
`/tmp/sysarchitect-database-linux-player.log`. Final source verification found no removed
scene object IDs, no changed scene GUID, and no source scene changes during isolation.


## Current architecture/UX corrections

See `Docs/RoutingAndConnections.md` for supported patterns and the manual walkthrough.
One configurable Load Balancer now supports application routing and database routing;
Backend > Database Router > expandable cluster is supported without arbitrary role mixing.
Database execution/versioning still lives in DatabaseModel. Replica/shard member service
budgets and utilization now reflect routed read/write work; primary writes remain limited
to one primary budget. Backend-controlled cache-aside has actual hit/miss/fallback/fill
behavior, scoped keys, write invalidation and coherence tests.

The existing selection controller now picks directed edges as well as nodes, with node
priority, clear selected-edge styling, dedicated deletion controls and guarded keyboard
shortcuts. Current graph changes update simulator routes immediately. Existing node deletion,
packet cleanup, desktop camera controls and expandable database functionality are preserved.
Default ingress/router/cache headroom makes backend scaling visible without silently
increasing backend capacity. Label size/name abbreviation and dismissible inspectors improve
clarity; full automatic label layout is not implemented.

Validation artifacts: `/tmp/sysarchitect-routing-core.xml`, `/tmp/sysarchitect-routing-play.xml`,
and rendered captures under `/tmp/sysarchitect-*routing*.png`,
`/tmp/sysarchitect-selected-connection.png`, `/tmp/sysarchitect-cache-aside.png`.
The simulator/validation foundation is committed as `3e8212d`; following commits record
additional regression coverage and the integrated UI/scene. The latest Linux demo will be
in `Builds/Linux-RoutingDemo/`, leaving previous demo folders intact. No unrelated milestone
was advanced. `.agents` remains excluded; untracked `dotnet-tools.json` was untouched.

Final correction suites: **50 Edit Mode and 16 Play Mode cases passed**. Both combined
routing roles were exercised through the actual scene, including primary-only writes,
replica reads, stale-read display, selected-edge controls and cache hit/miss/fill activity.
All pre-existing scene object IDs and asset GUIDs were preserved; no unresolved scene GUIDs
were found. Validation/build stayed in the isolated project while the user's editor remained
open. Source and validated scene hashes match. Core follow-up commit: `a99c43b`.

The final Linux build succeeded (108,700,945 bytes) and was copied to
`Builds/Linux-RoutingDemo/SysARchitect.x86_64`; previous demo folders are preserved.
A 12-second graphical startup smoke test found no application exceptions or AR background
initialization error. The test instance was deliberately closed by timeout (exit 124).
Existing host DPI and unused URP postprocessing shader warnings remain. Build/player logs
are `/tmp/sysarchitect-routing-linux-build.log` and `/tmp/sysarchitect-routing-linux-player.log`.
The live database reconfiguration regression is fixed in `bb1bb8b`; its before/after
reproducer confirmed a write now lands on the correct new shard, and centralized policy
checks reject forced replica writes or mismatched shard indices. Final suites: 50 Edit
Mode + 16 Play Mode, all passing. The following UI/scene commit completes these corrections;
no unrelated milestone was advanced and no remote push was made.
