# SysARchitect takeover — October 30, 2026

Target demonstration: November 2, 2026. No remote pushes are authorized.

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
3. Database replication/sharding: absent.
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
Automated coverage now includes 25 Edit Mode cases (17 simulation, 8 mesh winding) and
3 Play Mode cases (builder lifecycle, graph validation, integrated controls and packets).
Test artifacts are in `/tmp/sysarchitect-simulation-tests.xml` and
`/tmp/sysarchitect-integrated-tests.xml`; the rendered demo capture is
`/tmp/sysarchitect-desktop-demo.png`. Full mouse-driven usability is still a manual check.
Container/Pod/Worker nodes currently have generic service behavior and distinct shapes;
Docker images/hosts, deployments and reconciliation are not implemented.

Use **SysARchitect > Build Linux Desktop Demo** for a local build containing only the
desktop workspace. Output: `Builds/Linux/SysARchitect.x86_64` (ignored by Git).

## Next priority before November 2

Finish desktop mouse usability QA and save/load of learner architectures, then implement
database replication/sharding against this same engine. Follow with the five guided
lessons, Docker/Kubernetes behavior, quiz/progress, shared AR placement and Android
validation. The complete product is not finished; no physical AR, Android build, or
mobile performance verification has been claimed.
