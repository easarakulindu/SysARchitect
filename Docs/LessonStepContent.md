# Guided instruction content correction — November 1, 2026

## Recovered state and root cause

Recovered from `d7ce945` with seven partially edited implementation files and an
untracked viewport regression. The interrupted focused Unity run had no result
XML; it was not counted as a pass. No new scene or lesson system was generated.

The 0.11.1 implementation already had four distinct instruction strings for each
of five lessons, and its refresh key included the instruction index. Next/Previous
updated the body string correctly. However, the displayed title was lesson-level;
static navigation, objectives and the long lesson overview preceded the changing
instruction, and mobile action rows preceded the explanation panel. Scrolling was
not reset on step navigation. The first viewport could therefore remain identical
while the step counter changed. Dedicated step headings/explanations were missing.

The preserved baseline regression (`sysarchitect-step-content-reproduction.xml`)
failed because actual TMP lines within the viewport were identical before and
after Next step, even though the full body string and step index changed. This
reproduces the reported presentation failure in the actual mobile scene in Unity;
it is separate from the user's physical Redmi Note 13 4G / APK 0.11.1 observation.

## Corrected presentation

- All twenty existing instructions now have matching headings, explanations and
  component focus metadata, aligned with their original lesson goals. Existing
  instruction strings and explicit demonstration actions are retained.
- The pinned title includes the current step heading. The scroll body starts with
  that heading, the instruction and **Why this step**, followed by component focus,
  navigation help, objectives, overview, the existing live algorithm and challenge.
  Mobile action/quiz rows remain reachable below the explanation by scrolling.
- Forward/backward instruction navigation and topic selection reopen content at
  the top. Live algorithm events do not keep resetting the learner's scroll.
  The step counter remains bounded; Previous/Next lesson and Lessons are separate.
- Mint focus applies only to existing matching node types and links whose two
  endpoints match the current step. Closing the guide or opening the picker clears
  it. This does not select nodes, create links, load examples, advance time, inject
  failures or change database/container/cluster configuration. Selection and actual
  traffic colors retain priority; failure and overload indicators are preserved.
- Completion and quiz storage are unchanged. Revisiting a topic starts instruction
  one without erasing progress. The existing live conceptual algorithm remains
  tied to actual engine/controller observations, independent of instruction index.

## Verification

Exact editor: Unity **6000.6.5f1**, unchanged packages, isolated project
`/tmp/SysARchitectValidation`. Unity jobs run serially with two job workers and one
import worker to reduce load after the host crash. No root editor was commandeered.

- Edit Mode: **96 passed, 0 failed** (`sysarchitect-step-content-editmode.xml`).
  Includes five new data cases for matching, distinct content and valid focus types.
- Play Mode: **47 passed, 0 failed** (`sysarchitect-step-content-playmode.xml`),
  including all existing desktop, graph/routing, resource, education and AR-adapter
  regressions. The final test run also checks visible explanation text, not only
  headings and instruction indices. Android delivery below was built from this verified source.
- The new mobile fixture exercises all five topics, all four instructions forward
  and backward, both boundaries, switching/revisiting and explicit bottom-to-top
  scroll restoration at 1280×720, 1920×1080, 2400×1080 and 2796×1290. The latter
  sizes use synthetic safe-area insets. Assertions inspect actual rendered TMP
  viewport lines, full instruction/explanation, title, counter and button bounds.
- The desktop fixture checks actual content, focus/selection/traffic priority and
  unchanged graph identities, connection selection, simulation time and successes.
  Two completed lessons and completed pre/post quiz fixtures survive navigation
  and JSON reload after a scene reload. It uses memory storage rather than changing
  the developer's PlayerPrefs; this is not an Android storage/device test.
- Render captures were inspected at 1280×720 and 2400×1080. The current instruction
  and explanation are visible immediately, with separate pinned topic/step controls.

The prior APK and both saved scenes were hash-checked against the pre-interruption
snapshot. Package files, unrelated material edits and `dotnet-tools.json` are
unchanged. `.agents` remains ignored and excluded. Linux binaries are preserved;
desktop source behavior is tested in Unity rather than claiming a new Linux build.

No physical Android retest, AR tracking verification or device performance profile
is claimed. After installation, verify all five topics and steps on the Redmi,
close/reopen the application and confirm prior completion/quiz results remain.

## Local Android build resource controls

The first post-test Android attempt was deliberately cancelled during native
compilation: Unity's job/import limits and CPU affinity did not limit Bee's
separate compiler pool, which launched 24 jobs. This was an interrupted build,
not a passing build or a code/test failure. Its log is retained. The same cached
build was restarted with the installed Bee backend's `BEE_BUILD_THREADS=2` control;
process inspection confirmed two native compiler jobs and one Unity process.
Unity still passes its own `-Xmx4096m` maximum heap argument to Gradle; the
`GRADLE_OPTS` heap setting below did not override that argument. The JVM processor
count and CPU affinity controls remained in effect.
No cache or project work was deleted. The build helper restored its temporary
version/input settings when the cancelled attempt exited.

Build environment for this handoff (only in the isolated validation project):

```sh
SYSARCHITECT_BUILD_REVISION=e556a6f4cc52977f55a247ba889adf1718254b57 \
BEE_BUILD_THREADS=2 JAVA_TOOL_OPTIONS=-XX:ActiveProcessorCount=2 \
GRADLE_OPTS='-Dorg.gradle.workers.max=2 -Dorg.gradle.parallel=false -Dorg.gradle.jvmargs=-Xmx2048m' \
taskset -c 0,1 /home/easara/Unity/Hub/Editor/6000.6.5f1/Editor/Unity \
  -batchmode -quit -job-worker-count 2 -import-worker-count 1 \
  -projectPath /tmp/SysARchitectValidation -buildTarget Android \
  -executeMethod SysARchitect.Editor.MobileBuild.BuildAndroid \
  -logFile /tmp/sysarchitect-step-content-android-limited.log
```

## Verified Android delivery

- **0.11.2**, package `com.easara.sysarchitect`, versionCode 1.
- **`Builds/Android-StepContent/SysARchitect.apk`**, **35,878,130 bytes**.
- SHA-256: `2c65e89ec4ffff013b3cc35f4c30224987391f92f44cb18f03a7f8883abbdf0a`.
- Source feature commit **`e556a6f4cc52977f55a247ba889adf1718254b57`**.
- Unity exited **0**; BUILD-INFO records UTC **2026-11-01T08:54:15Z** and
  AR scene dependency hash `3b5422df1a9b8763cdaef2b5624ccea0`.
- `apksigner verify --print-certs` succeeded: local **Android Debug** certificate,
  SHA-256 `f8b23b34451fa9109fef5831bea70a74e6a6e148c33c96e4270805f91e2214f1`,
  matching 0.11.1. This is a signed local testing APK, not store-release signing.
- aapt verified API 26 minimum / 36 target, GLES 3.1, optional ARCore and no required
  `android.hardware.camera.ar`. Native libraries are ARM64 only.
- ZIP inspection verified **all 20 headings and 20 explanations** in the packaged
  IL2CPP metadata, preventing accidental delivery of the old lesson code.
- Actual build log: `Builds/LessonStepContentEvidence/sysarchitect-step-content-android-limited.log`.
  XML, screenshots, signing/manifest output, verification script and BUILD-INFO are
  retained beside it. Unity's 843,772,100-byte build total includes native backup
  files; it is not the delivered APK size.

The prior `Builds/Android-LessonNavigation/SysARchitect.apk` was hash-verified
unchanged. All older APKs, Linux builds and launchers were left in place.
ADB reported no connected device; no physical install or device retest was run.

Install with USB debugging enabled and the computer authorized, from the project
root, using **replace/update** rather than uninstalling so existing app data stays:

```sh
adb_tool=/home/easara/Unity/Hub/Editor/6000.6.5f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb
"$adb_tool" devices -l
"$adb_tool" install -r Builds/Android-StepContent/SysARchitect.apk
"$adb_tool" shell am start -n com.easara.sysarchitect/com.unity3d.player.UnityPlayerGameActivity
```

On the Redmi Note 13 4G, confirm version 0.11.2, enter labelled **Virtual Workspace**,
open Lessons / Progress and check changing headings/instructions/explanations with
Next step and Previous step in all five topics. Scroll down then change a step:
the new instruction should return to the top. Switch/revisit topics through Lessons,
then restart the app and check prior quiz scores/completions. True AR still requires
a supported device; these editor checks and non-AR acceptance are not physical AR
verification. Load example remains an explicit workspace-replacing action.
