# Lesson navigation correction — November 1, 2026

## Cause and design

The developer reported the missing return path during physical Android testing.
Source inspection confirmed the existing guide had only a Next lesson callback
(wrapping from lesson five to one), Previous/Next step callbacks, and no lesson
picker. On mobile, Next lesson was also inside the scrolling actions. Previous
step clamped its value but was still enabled at the first instruction.

The existing LessonUI and guide in both scenes now provide:

- **Previous lesson / Lessons / Next lesson**: change topic or open the picker.
  Previous is disabled on topic one; Next is disabled on topic five. No wrapping.
- **Previous step / Next step**: instruction navigation within the current topic,
  with disabled first/last boundaries. These do not tick the simulation.
- **Lessons** lists all five available topics, including completion status and
  direct revisiting. No new unlocking restrictions were introduced.

Changing topic starts its instructions at step one. Navigating never invokes
Load example, Clear, simulation reset or progress deletion. Selecting a different
topic still uses the existing lesson controller's observation/baseline behavior;
saved completion and pre/post quiz attempts remain untouched. The guidance text
now explains the difference between topics, instruction steps and Step 50 ms.

Mobile navigation is pinned in its own 96-unit row below the guide header,
separate from pinned instruction controls at the bottom. Content and the picker
scroll between them. Desktop keeps its existing guide footprint with a separate
topic row. The two saved scenes retain every original GameObject/fileID and asset
GUID; each receives only 16 navigation UI objects. No new lesson/controller or
simulation system was created. Unrelated cached TMP fields were excluded from
the scene patch after Unity serialization; saved references were verified.

## Actual verification

Unity **6000.6.5f1**, unchanged package lock, isolated project
`/tmp/SysARchitectValidation`; the root editor/project was not commandeered.

- C# compilation and Unity scene configuration succeeded.
- **91/91 Edit Mode tests passed**: `/tmp/sysarchitect-lesson-navigation-core.xml`.
  Includes actual mobile scene navigation references and all existing model tests.
- **44/44 Play Mode tests passed**: `/tmp/sysarchitect-lesson-navigation-full-play.xml`.
  Includes existing desktop/touch/AR-adapter/routing/resource/education checks.
- The **three new navigation tests also passed after final scene review**:
  `/tmp/sysarchitect-lesson-navigation-reviewed-play.xml`. They exercise pointer
  click handlers, backward topics/instructions, both ends, nonadjacent picker
  selections, external topic selection, serialized progress/score preservation,
  scene reload, and retention of live graph connections/counters/running state.
- Actual mobile scene rendered at **1280×720, 1920×1080, 2400×1080, 2796×1290**;
  synthetic safe areas on the latter sizes. Assertions verify pinned safe bounds,
  96-unit targets, readable labels and scroll access to topic five. Inspected
  1280×720 guide and 2400×1080 picker PNGs.

Progress fixtures use an in-memory storage implementation and completed quiz
sessions, without modifying the developer's PlayerPrefs. They verify the same
stored JSON can reload with two completions and both quiz forms unchanged; these
are regression fixtures, not learner performance measurements. No physical phone
retest of this correction was performed here. Real AR tracking remains outside
these editor/synthetic-input checks.

Durable copies of XML, logs and navigation/picker PNGs are retained in ignored
`Builds/LessonNavigationEvidence`. Existing APKs and Linux demos remain preserved.
The unrelated dotnet-tools.json, .agents and the separately changed DetectedPlane /
sample InteractableMat materials are excluded from this feature commit.

## Android delivery

Mobile version is **0.11.1**, built successfully from local feature commit
**bca6ef6e9ccf37d0559b07f25f443c6a72b7dcd6**, UTC 2026-11-01T07:27:27Z.

- APK: **`Builds/Android-LessonNavigation/SysARchitect.apk`**, 35,873,394 bytes.
- SHA-256: `e2b5e9ba92927c24e2fd26c61bc7b45e4e6797c039ee3390e9614c301a0ca6e1`.
- BUILD-INFO.txt records source revision, Unity 6000.6.5f1, AR scene and dependency
  hash `3b5422df1a9b8763cdaef2b5624ccea0`.
- aapt verified com.easara.sysarchitect, versionName 0.11.1, min API 26 / target
  36, optional ARCore and no required camera.ar feature. ZIP libraries are ARM64
  only; apksigner verification passed with the local Android Debug certificate.
- Inspection of the packaged level0 data confirms Previous lesson, Lessons,
  Lesson selection and Lesson choice 5 are in the built mobile scene.

Build and package/signature evidence is in `Builds/LessonNavigationEvidence`.
The larger Unity build total includes native backup files and is not the delivered
APK's size. The earlier 0.11.0 APK and all Linux binaries/launchers are unchanged;
Linux binaries were not rebuilt for this navigation correction. Desktop source
and scene behavior were verified in Play Mode. No remote push or publication.

ADB had no connected device. Install on the phone with USB debugging authorized,
from the project root (this updates the existing app without requesting data removal):

```sh
adb_tool=/home/easara/Unity/Hub/Editor/6000.6.5f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb
"$adb_tool" devices -l
"$adb_tool" install -r Builds/Android-LessonNavigation/SysARchitect.apk
"$adb_tool" shell am start -n com.easara.sysarchitect/com.unity3d.player.UnityPlayerGameActivity
```

The prior API 26+/ARM64/GLES 3.1 requirements and Virtual Workspace remain. Verify
0.11.1 in the app, open Lessons / Progress, navigate forward/backward through
topics and instruction steps, choose a nonadjacent topic through Lessons, and
confirm saved completions and quiz scores remain after reopening the app.
This physical acceptance check has not been performed here.
