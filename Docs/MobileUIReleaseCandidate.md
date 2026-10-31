# Mobile UI release-candidate verification — October 31, 2026

Continued the preserved mobile draft after `6da8052`; no application regeneration,
package upgrades or replacement simulation systems. The unrelated dotnet-tools.json
and local .agents contents remain outside commits. Existing asset GUIDs and all
207 AR / 197 desktop scene GameObjects were preserved during Unity scene updates.

## Completed mobile interface

Landscape UI uses a 1280×720 reference canvas, safe-area anchors, 96-unit touch
targets and readable wrapped text. Toolbox tabs separate Components from
Edit / Connect, with scroll access to every component/action. The top toolbar
opens Toolbox, Simulation, Reposition, Hide details and Lessons / Progress.
Toolbox and Simulation share the left drawer without overlapping.

Selected nodes/edges use a right drawer with Node / Connection and Service tabs.
Service opens automatically for routing, cache, database, Docker and Kubernetes
configuration; routing-role buttons appear together near the top. General node
configuration, rotation, resource monitoring, failure/restart and individual edge
deletion use the existing controllers. Hidden panel groups do not intercept input.
Tap placement/selection, dragging and two-endpoint connections still use the
shared touch adapter and graph commands; no second graph or simulator was added.

Lessons and quizzes have scrollable bodies with pinned close/navigation controls.
Long validation messages scroll rather than clipping the explanation. Input fields
use their available drawer width. Resource gauges, failure labels, directed links
and packets remain the existing shared presentation. Loss of tracking disables
mutating drawer controls and Run/Step through CanvasGroups, while navigation and
Build remain available. Touch/world editing retains the existing tracking gate.

## Confirmed corrections and tests

Reproduction on the preserved draft produced two failing regression cases:
left drawers could overlap and long architecture validation had no scroll viewport.
Results: `/tmp/sysarchitect-rc-mobile-message-reproduction.xml` (2/4 failed as
expected). Initialization tests also caught inactive-parent lookup and Unity
native-component null handling issues while developing pinned/gated controls;
both were repaired. Failure logs were retained rather than suppressed.

Final source at this checkpoint compiled with Unity **6000.6.5f1**:

- **91/91 Edit Mode passed**, `/tmp/sysarchitect-rc-core.xml` and log. Includes
  five synthetic safe-area cases and actual AR scene/script/reference checks.
- **37/37 Play Mode passed**, `/tmp/sysarchitect-rc-play.xml` and log. Includes
  desktop routing/editing/resources/failures, Docker/Kubernetes, lessons/quizzes,
  shared touch and the six mobile interaction/layout cases.
- Rendered landscape previews at **1280×720, 1920×1080, 2400×1080 and 2796×1290**.
  The latter two use explicitly synthetic notch/system insets, not measurements
  of an identified phone. Assertions check safe bounds, toolbar/text sizes,
  routing-role visibility, tab access and pinned controls after scrolling.
  Preview stages include toolbox, editing, details, lessons, quiz feedback and
  progress: `/tmp/sysarchitect-rc-<width>x<height>-<stage>.png`.

Inspected rendered examples include 1280×720 toolbox/details, 2400×1080 lesson
and 2796×1290 quiz. Tests use the real UI/layout/controllers on a desktop scene
with RenderTextures and injected tracking availability. They do not run a native
ARCore/ARKit camera/session. Quiz previews use in-memory progress and incomplete
practice answers; no learner scores or learning gains were fabricated.

The application supports landscape orientation. These checks do not establish
physical touch ergonomics, camera/permission behavior, tracking/anchor stability,
thermal behavior or performance. Device acceptance remains necessary.

## Release builds

Current Android/Linux release-candidate builds will be recorded here after the
UI checkpoint is committed and build configuration is verified. The prior Linux
demo and APK are preserved until new outputs pass their checks.
