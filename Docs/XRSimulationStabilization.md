# XR Simulation stabilization — November 1, 2026

Baseline: d466e96, Unity 6000.6.5f1, locked AR Foundation 6.6.2 / XR Management
4.7.0. Inspected the previous audit under Builds/XRSimulationVerification,
package implementations, scene references and local changes. No applicable
AGENTS.md exists. The local Unity skill applies; .agents remains excluded.

## Anchor ordering defect and fix

ARAnchorManager.TryAddAnchorAsync returns CreateTrackableImmediate: the anchor
is still **pending** until the manager consumes its provider's added changes.
SysARchitect immediately published that anchor as a usable workspace. Reposition
could call TryRemoveAnchor within the same frame. SimulationAnchorImpl.MarkAsRemoved
then deleted the queued addition and emitted a removal. XRAnchorSubsystem's
validation utility had never observed that trackable ID being added, and threw
“Trackable <id> is being removed but is not in the list of trackables.”

A same-frame regression reproduced this on unmodified application code, logging
pending=True before removal. The original baseline case sometimes passed because
nested coroutine completion advanced a frame; the final regression invokes
Reposition inside the placement helper's completion callback instead.

The existing ARWorkspace now waits for pending=False before publishing placement.
Stale results from a cancelled placement are retired through the same manager.
Removal keeps the anchor manager/session alive until GetAnchor confirms that the
provider removal was consumed. Camera/background/input/plane components still
switch off immediately in Virtual mode; only in-flight anchor work delays their
manager/session shutdown. A retry of AR waits for owned changes to finish. No
manual trackable destruction, suppressed exceptions, provider patch, package
upgrade, arbitrary sleep or second anchor system was introduced.

## Combined timeout: test isolation

The initial lifecycle baseline reproduced no planes after the earlier Virtual
Workspace tests. Diagnostics found **zero simulation environment instances**, versus
one in a fresh process. Each test loads a scene with LoadScene(Single), which also
unloads XR Simulation's additive environment scene. Its loader/subsystems survive
scene changes in the test session, so a new ARSession can report tracking while
its scanner still belongs to an unloaded environment.

The Editor-only XR test assembly resets the Simulation provider through public XR
Management APIs between independently loaded test scenes. It does so after the
old AR scene has unloaded, before opening a new AR scene. The explicit combined
regression executes all four existing VirtualWorkspaceTests, recreates the test
provider and verifies real plane detection and anchoring. Same-scene Virtual/AR
cycling is tested separately without provider resets. The application does not
load/reload scenes in normal use; production provider initialization is unchanged.

## Focused verification

All Unity jobs run sequentially in /tmp/SysARchitectValidation, with two job
workers and one import worker; the root editor/project is not commandeered.

- Unfixed same-frame anchor reproduction: one failed with the original exception;
  cancellation-to-Virtual control passed. Evidence: sysarchitect-xr-anchor-reproduction.xml.
- Fixed XR lifecycle: **4 passed, 0 failed**, sysarchitect-xr-anchor-fixed.xml.
  Three placement/immediate-reposition cycles remove every owned anchor; cancellation
  during creation drains its unpublished result; two same-scene Virtual/AR cycles
  plus final placement work; the four-case Virtual test sequence followed by fresh
  real XR plane/raycast/anchor creation works with correct provider isolation.
- Full Edit Mode suite: **96 passed, 0 failed**, sysarchitect-xr-stabilization-edit.xml.

The real provider supplies planes, raycasts and anchors. The test drives the
simulated camera pose and invokes existing commands; it does not fabricate AR
availability or trackable results. No physical AR testing or manual input
playthrough is claimed. Complete demo/regression results follow below.

Unrelated materials and dotnet-tools.json are preserved. Saved scenes, GUIDs,
packages, APKs and Linux builds are unchanged. No remote pushes.

## Complete demonstration and final regression result

Runtime fix commit: **855c1f2**. Final verification used the same runtime source.

- **96/96 Edit Mode passed**, zero failures/skips:
  `Builds/XRStabilizationEvidence/sysarchitect-xr-stabilization-edit.xml`.
- **53/53 Play Mode passed**, zero failures/skips:
  `Builds/XRStabilizationEvidence/sysarchitect-xr-stabilization-full-play.xml`.
  This includes all 47 pre-existing desktop/mobile/model/resource/routing/education
  cases and six Editor-only XR lifecycle/demonstration cases. The combined case
  also executes the four existing Virtual tests before the fresh XR scan.
- Added cancellation by Reposition while creation is in progress: the stale result
  is retired, a replacement is anchored, exactly one anchor remains and the same
  two nodes/connection survive. Same-frame removal, cancellation to Virtual,
  repeated placement and same-scene mode cycling all pass without expected-log
  suppression or errors.
- The complete XR demonstration passed both focused and full runs. It starts the
  saved ARWorkspace, obtains real simulated planes and anchors, uses the Reposition
  button, reanchors, places Frontend/Backend/Database using synthetic device events
  through the normal WorkspaceTouchInput.Update reader, picks both directed links,
  runs 30 requests/s through both actual architecture hops, and pauses after **21
  successes** in the recorded full run. Database reads and writes and request-count
  conservation are checked. Each CPU/RAM gauge is compared with the published
  engine state after its normal presentation refresh.
- The same session opens the guide, advances to instruction two, checks its real
  heading/body and returns to the original content, preserving three nodes/two
  edges. The lesson fixture uses memory storage, preserving learner PlayerPrefs.
- Inspected architecture, traffic/control and lesson renders captured from the
  actual simulated AR camera/URP scene at the test Game view's **640×480**. They are
  not physical-camera photos. PNGs and all XML/logs are retained in ignored
  `Builds/XRStabilizationEvidence`. The traffic/control screenshot shows the top of
  the scroll panel; scroll lower to inspect global counters during a manual demo.

Intermediate demo harness failures are retained. Manual InputSystem.Update calls
and an unfocused Game view initially prevented the synthetic touchscreen release
from being read. Device diagnostics then showed the normal-frame touchscreen was
disabled by Editor focus policy. The final harness uses normal dynamic updates,
temporarily routes devices to the Game view, and restores input/background/focus
settings and removes its device in finally. It waits for gauge values to match
published state after Pause rather than asserting stale presentation. These were
harness corrections; production touch and resource logic were unchanged.

## Repeatable Editor demonstration

1. Use Unity **6000.6.5f1** with the existing packages. Open
   `Assets/SysARchitect/Scenes/ARWorkspace.unity`. In XR Plug-in Management's
   Standalone tab, retain the existing Simulation loader. Window > XR > AR Foundation
   > XR Environment shows the existing DefaultSimulationEnvironment fallback.
2. For mouse taps, enable Window > Analysis > Input Debugger > Options > **Simulate
   Touch Input From Mouse or Pen**. Press Play and focus the Game view. Hold right
   mouse and use WASD / Q / E to scan; Shift moves faster. Release navigation input
   and left-click a highlighted upward horizontal plane. Wait for Workspace tracked.
3. Press **Reposition workspace** and click a detected surface again. Existing
   architecture is retained. Cancellation during creation is now covered as well.
4. **Toolbox > Components > Frontend**; tap an empty point inside the tabletop.
   Repeat for Backend and Database, leaving space between them. **Hide details**
   clears selection if the inspector covers a point you want to tap.
5. **Toolbox > Edit / Connect > Connect source > target**; tap Frontend then Backend.
   Choose Connect again and tap Backend then Database. Confirm two directed arrows.
6. **Simulation > Run**, using 30 requests/s. Scroll the simulation panel to see
   Sent / Success / Failed / In flight and latency. After at least 20 successes,
   Pause, close the Simulation panel and inspect the nodes' CPU/RAM bars; tap a node
   for its inspector. Counters satisfy sent = success + failed + in flight.
7. **Lessons / Progress** opens the current guided lesson. Use **Next step** and
   **Previous step** to see changing headings, instructions and explanations.
   **Previous lesson / Next lesson / Lessons** change topics. **Load example**
   explicitly replaces the workspace, so it is a separate demonstration action.
8. Optionally switch **Virtual / AR** to Virtual and back; wait for availability,
   scan and anchor again. Stop Play before opening/reloading a different scene.

To repeat the automated complete sequence in a separate validation checkout:

```sh
BEE_BUILD_THREADS=2 /home/easara/Unity/Hub/Editor/6000.6.5f1/Editor/Unity \
  -batchmode -job-worker-count 2 -import-worker-count 1 \
  -projectPath /tmp/SysARchitectValidation -runTests -testPlatform PlayMode \
  -testFilter SysARchitect.Tests.XRSimulationDemonstrationTests \
  -testResults /tmp/sysarchitect-xr-demo-repeat.xml \
  -logFile /tmp/sysarchitect-xr-demo-repeat.log
```

The Editor-only fixtures reset providers for **test scene replacement**, not for
normal same-scene mode switches. Simulation environments should not be assumed to
survive arbitrary LoadScene(Single) calls. Adding application scene navigation in
the future would need an explicit XR teardown/reinitialization contract.

## Limits and preservation

The complete sequence is automated in an actual Unity Editor Play Mode XR
Simulation session, with real simulation subsystems, synthetic touches and UI click
handlers. Camera poses are driven programmatically. A fully manual right-mouse /
mouse-to-touch playthrough and physical ARCore/ARKit validation remain outstanding.
No Android/iOS player build or performance profile was run for this XR correction.
Existing **0.11.2 APK and Linux demos remain unchanged and predate the XR fix**.
The prior APK hash still matches its delivery record. Scenes, packages, .meta GUIDs,
unrelated materials, dotnet-tools.json and .agents were preserved; nothing pushed.
