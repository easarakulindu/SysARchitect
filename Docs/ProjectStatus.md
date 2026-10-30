# SysARchitect takeover — October 30, 2026

Target demonstration: November 2, 2026. No remote pushes are authorized.

## Inspected baseline

- Unity 6000.6.5f1 (3ff58d469c8a), URP 17.6.0, Input System 1.20.0,
  AR Foundation / ARCore 6.6.2, XR Interaction Toolkit 3.6.1, Test Framework 1.8.0.
  Versions verified in ProjectVersion, manifest, lockfile and installed package metadata.
  Imported XRI samples are in a 3.5.1 directory; that is not the installed package version.
- Two existing commits: initial template and editor settings. All 23 SysARchitect C#
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
2. Simulation engine: next priority (traffic, fixed ticks, queues, timeouts, load balancing, failure/restart).
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
presentation of transfers and does not drive processing. 16 Edit Mode test cases passed,
including request conservation against actual queue/processing counts on every tick.
Scene simulation controls and presentation are the next integration step.
