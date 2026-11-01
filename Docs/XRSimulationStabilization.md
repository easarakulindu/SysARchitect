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
playthrough is claimed. Complete demo/regression results follow after execution.

Unrelated materials and dotnet-tools.json are preserved. Saved scenes, GUIDs,
packages, APKs and Linux builds are unchanged. No remote pushes.
