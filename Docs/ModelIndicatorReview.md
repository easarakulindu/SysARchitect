# Model and indicator review — October 31, 2026

Reviewed the preserved working tree starting at `17e705d`. No repository AGENTS.md
was present. Used the local Unity development skill, Unity 6000.6.5f1 and the
existing package lock. No package versions or simulation rules changed.

## Focused implementation

Included the ten detailed procedural meshes, their vertex-region hardware shader
and material, CPU/RAM indicators, status colors and label handling, correct mesh
bottom offsets for placement/movement, and the wider desktop camera framing.
Both existing scenes receive only their graph/preview material references. All
existing scene GameObjects and asset GUIDs remain intact.

The review corrected fixed-width gauges that extended beyond small Pod models.
Gauge widths now fit the model; a zero value hides the fill instead of implying
nonzero utilization. Strengthened tests read actual resources published by
SimulationController after real requests, then verify reset baseline memory,
zero CPU and failure labels. Tests also exercise all ten types at desktop and
0.35 workspace scale, movement after rotation, preview transparency settings,
and owned mesh/material cleanup without destroying another node's material.

Extracted hardware material wiring into `Editor/HardwareVisualSetup.cs`, and
model tests into `Tests/PlayMode/ModelIndicatorTests.cs`. The existing untracked
VisualDesignSetup delegates the hardware portion to that helper and retains its
mobile/guide setup. Its .meta was preserved. The existing mobile test method and
its .meta were preserved; only its model test moved to the focused test class.
The other mobile-layout/build/documentation files were checked byte-for-byte
against their pre-review snapshots.

The hardware itself uses one submesh below 1,500 vertices per type. The four small
gauge renderers add draw work per node. The shader provides flat shading and bright
accents without bloom or real-time lights; this is not a measured mobile
performance result. Deep contained labels remain selected-only to reduce clutter.

## Verification of the exact candidate

The disposable validation project was backed up, restored to tracked `17e705d`
sources and overlaid with only this candidate. Uncommitted mobile runtime/layout,
mixed editor setup and mobile test files were excluded from its compilation.
Every staged code/asset/scene file was compared byte-for-byte with the files used
in that isolated candidate. The root working tree's unrelated edits were retained.

- **85/85 Edit Mode passed:** `/tmp/sysarchitect-model-review-core.xml` and log.
- **31/31 Play Mode passed:** `/tmp/sysarchitect-model-review-play.xml` and log.
  Includes the three new model/indicator cases, existing connection/cache/database/
  resource/failure tests, Kubernetes UI routing/replacement, five lesson challenge
  cases, two quiz UI cases and two shared touch cases.
- Rendered `/tmp/sysarchitect-model-indicator-review.png` was inspected. Its
  counters were 160 sent, 154 succeeded, 0 failed and 6 active after four simulated
  seconds. No learner outcomes are inferred from this test fixture.
- Linux build succeeded: `/tmp/sysarchitect-model-review-linux-build.log`,
  108,879,741 bytes reported by Unity. Separate output:
  `Builds/Linux-ModelReview/SysARchitect.x86_64`. Eight-second background startup
  check had no application errors; timeout exit 124 was intentional. Player log:
  `/tmp/sysarchitect-model-review-linux-player.log`.

The separate build keeps the committed helper's `0.9.0-mobile-ready` version label;
no unrelated version bump was included. Its BUILD-INFO records UTC
2026-10-31T11:25:37Z and scene dependency hash b17e7714087f2afae30083e384593a3b.
The current launcher and Linux-Current demo were not replaced.

## Implementation status versus verification

| Area | Implemented and inspected | Verification and remaining limits |
| --- | --- | --- |
| Kubernetes | One educational Deployment per cluster; worker/Pod graph nodes, explicit desired/actual/ready counts, bounded scheduling, one-second readiness and terminal Pod replacement with a new identity. Scaling changes the explicit target; no HPA is configured. | Fresh run passed 4 DeploymentModel tests, 1 Kubernetes workspace/UI test and its lesson challenge. The UI test routes real requests to Pods and checks replacement/deletion. This is a simulation, with at most four workers/eight desired Pods/four slots per worker, not a real Kubernetes control plane or full container/network/storage model. Physical AR and learner acceptance remain unverified. |
| Guided lessons | Five modules with objectives, explanations, example graphs, four instructional steps, live event/controller algorithm highlighting, measured challenges and local completion storage. | Fresh run passed 5 lesson challenge cases and 3 content/progress tests. Next/Previous changes instructional text; simulation stepping is a separate 50 ms control. Highlighting shows the latest observed action, not a debugger executing each pseudocode line. End-to-end learner usability and teaching effectiveness were not established. |
| Quizzes | 20 explained four-choice questions; fixed disjoint ten-question pre/post forms, balanced across the five modules; answer gating, actual scoring and versioned local PlayerPrefs JSON storage. | Fresh run passed 4 quiz core tests and 2 UI cases, including both full forms, summary and input restoration. Tests inject in-memory progress storage for scoring/serialization. Actual mobile app-restart persistence and assessment validity/learning improvement are unverified. No cloud synchronization is implemented. |
| Android AR | AR Foundation 6.6.2/ARCore provider, horizontal-up planes, asynchronous workspace anchor, scaled shared graph, Input System touch adapter, selection/movement/connections/deletion, configuration, packets, monitoring, tracking gate and repositioning. Android configuration is ARM64/IL2CPP/GLES3, API 26–36. | Fresh run passed 2 shared synthetic-touch cases and 6 touch-phase cases. These do not execute a native ARCore camera/session or real fingers. `adb devices -l` showed no connected device. Permissions, plane detection, anchor stability, tracking loss/recovery, touch ergonomics and performance require a compatible physical Android device. The new mobile drawers are still uncommitted and were excluded here. |

An earlier Android APK exists at `Builds/Android/SysARchitect.apk`, version 0.10.0,
35,856,878 bytes, built UTC 2026-10-31T08:54:45Z. That earlier working-tree build
was built and signed before this review and includes other uncommitted G work.
**It was not rebuilt in this task and does not include the review's gauge-width/
zero-fill changes.** APK build success is not evidence of on-device AR operation.
See MobileBuilds.md for the physical-device/Mac handoff, not a deployment claim.

## Preserved work outside this commit

The remaining files are listed below. Scene files contain only their remaining
mobile/guide edits after the two model material-reference hunks are committed.

- `Assets/SysARchitect/Editor/DesktopBuild.cs`
- `Assets/SysARchitect/Editor/MobileBuild.cs`
- `Assets/SysARchitect/Prefabs/UI/MobileToolboxButton.prefab`
- `Assets/SysARchitect/Scenes/ARWorkspace.unity`
- `Assets/SysARchitect/Scenes/DesktopWorkspace.unity`
- `Assets/SysARchitect/Scripts/AR/MobileWorkspaceUI.cs`
- `Docs/EducationAndAR.md`
- `Assets/SysARchitect/Editor/VisualDesignSetup.cs`
- `Assets/SysARchitect/Editor/VisualDesignSetup.cs.meta`
- `Assets/SysARchitect/Scripts/AR/MobilePanelLayout.cs`
- `Assets/SysARchitect/Scripts/AR/MobilePanelLayout.cs.meta`
- `Assets/SysARchitect/Tests/PlayMode/VisualAndMobileLayoutTests.cs`
- `Assets/SysARchitect/Tests/PlayMode/VisualAndMobileLayoutTests.cs.meta`
- `dotnet-tools.json`
