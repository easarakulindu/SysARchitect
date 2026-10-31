# Educational and AR delivery — October 31, 2026

Target: November 2, 2026. Milestones are implemented incrementally on the existing
builder and simulator. All changes and commits are local; never push. `.agents`
and the unrelated untracked `dotnet-tools.json` remain excluded.

## Audit and execution order

Unity 6000.6.5f1, AR Foundation/ARCore/ARKit 6.6.2, URP 17.6.0, XRI 3.6.1,
Input System 1.20.0 and Test Framework 1.8.0 were rechecked in project/package
metadata. The existing desktop graph, directional rules, caches, database models,
resource queues, failure controls, packets and connection editing are preserved.
The AR/Components/Lessons/Quiz folders initially contained metadata only.
Android Build Support with SDK/NDK/OpenJDK is installed. iOS Build Support and
Xcode are absent on this Linux machine. No hardware AR validation is claimed.

Order: A shared AR, B Docker, C Kubernetes, D five lessons, E quiz/progress,
F platform builds/documentation, G coherent low-poly/mobile polish.

## A — Shared AR foundation

Open `Assets/SysARchitect/Scenes/ARWorkspace.unity`. It was created as a separate
Unity-authored scene derived from the existing desktop workspace, preserving the
original desktop and sample scenes and their GUIDs. AR uses exactly one existing
ArchitectureGraph and SimulationController, the existing inspectors and packet
visualization. Android and iOS providers are separately configured for ARCore and
ARKit; mobile loader initialization/running is enabled. XR Origin, camera offset,
AR camera/background, tracked pose input, session, horizontal plane/raycast and
anchor managers are wired explicitly. No packages were upgraded.

Scan a horizontal textured surface, then tap a highlighted detected plane. A
native AR anchor owns the 0.84 m square workspace (2.4 model units at 0.35 scale).
Choose a component in the scrollable mobile toolbox, then tap within the workspace.
Tap nodes/lines to select; drag nodes to move; Connect takes two taps. Existing
node/connection delete controls, role/configuration panels, run/pause/step,
resources, database modes and failure/restart remain shared. A safe-area container,
toolbox toggle and Reposition workspace control are included. Reposition retains
the architecture. Lost session/anchor tracking pauses editing and simulation;
unsupported devices and failed anchoring get explanations.

`WorkspaceTouchInput` adapts touch phases into existing placement, movement,
selection and connection commands. Its input ownership flags prevent emulated
mouse events from also editing the graph. UI-originated and multi-touch gestures
are rejected. Workspace bounds and tracking gates are explicit. AR owns no copy
of routing or simulation logic. Graph nodes, edge widths/ports and packets inherit
workspace scale, while desktop scale remains one.

Files: `Scripts/AR/{ARWorkspace,WorkspaceTouchInput,MobileWorkspaceUI}.cs`, shared
interaction controllers, graph parenting and scale-aware visual adapters;
`Editor/ARWorkspaceSetup.cs`, the new AR scene/plane/mobile button assets,
runtime assembly references, XR provider flags, and `SharedTouchTests.cs`.

Validation: Unity scene generation/compilation passed; **19/19 Play Mode tests
passed**, including all 17 prior desktop cases and two shared-touch/scale cases.
Results `/tmp/sysarchitect-ar-play.xml`, log `/tmp/sysarchitect-ar-play.log`;
scene setup log `/tmp/sysarchitect-ar-setup.log`. Synthetic touch phases validate
the common adapter and actual Unity raycasts, not camera permission, physical
plane detection, native anchoring or device tracking. Those remain hardware checks.

The stable Linux launcher now uses `Builds/Linux-Current` so historic demo folders
remain intact. Each desktop build includes its visible version and BUILD-INFO.txt.
The A checkpoint built successfully as `0.4.0-ar-foundation` (108,710,737 bytes),
log `/tmp/sysarchitect-ar-linux-build.log`; an eight-second graphical Linux
startup check passed with no application exception (intentional timeout exit 124).
Player log `/tmp/sysarchitect-ar-linux-player.log`. The original desktop scene
hash remains unchanged. Native horizontal-plane/anchor/camera behavior and touch
ergonomics still require compatible Android/iOS hardware. Next priority: B Docker.
