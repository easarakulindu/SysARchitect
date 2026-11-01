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

Mobile version is now **0.11.1** so this correction can be distinguished from
0.11.0 during installation. Build outcome and the verified APK path/hash will be
recorded after the tested feature is committed and Android packaging succeeds.
