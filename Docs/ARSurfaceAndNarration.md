# Automatic AR surfaces, glass and English narration — November 28, 2026

## Automatic AR surface

The existing ARWorkspace now automatically anchors the shared graph after the camera
sees a tracked horizontal-up plane with both dimensions at least 0.15 m. The same
plane must remain within 5 cm of its candidate centre for 0.8 seconds. The plane's
centre must lie inside the camera view. A valid visible candidate is retained to
avoid flicker; otherwise the largest eligible visible plane is chosen. An exact
centre-screen raycast is unnecessary; optional manual placement retains its polygon
raycasts. Off-screen, subsumed, vertical and untracked planes are excluded.
The existing graph scales to 90% of the smaller detected dimension, capped at its
original 0.84 m width. A thin white boundary identifies its unchanged logical editing
bounds. Scaling changes presentation, not processing capacity or request state.
No separate graph, simulation or boundary GameObject is created. Existing manual placement remains an
optional API, and lifecycle fixtures explicitly disable automatic placement while
testing same-frame manual cancellation.

Detected planes use a white, 24%-opacity, double-sided overlay. Their detection and
visuals remain enabled after anchoring. The black AR platform's renderer is hidden;
its existing collider and editing bounds are preserved. Virtual Workspace retains
its own visible tabletop. Reposition pauses simulation, preserves architecture,
releases the acknowledged anchor through the existing lifecycle, then automatically
places again when a suitable surface is aimed at. Plane extents are a bounding-box
check, not a guarantee that every point of an irregular polygon fits the workspace.

Unity 6000.6.5f1 compiled and saved the AR scene in an isolated validation copy.
Seven real-provider XR Simulation Play Mode tests passed, including no-tap
placement/reposition, the previous anchor/cancellation regressions, mode switching,
combined Virtual/AR sequence, and the full builder/traffic/lesson demonstration.
Evidence: `/tmp/sysarchitect-auto-surface-play.xml` and corresponding `.log`.
This is automated Editor simulation, not physical Android AR verification.

Registry connectivity delayed initial validation; the copy resolved installed
package sources locally. No production package manifests/settings were changed.

## Polished infrastructure glass

The existing hardware shader/material now uses transparent blending, view-dependent
Fresnel rims, sharp studio highlights and URP environment reflections. Box bodies
have chamfered edges, and database drums have 40 sides. Hardware detail strips,
selection/failure accents, labels, arrows and resource gauges remain legible; gauge
fills stay opaque. Expanded database members reuse the detailed glass mesh, and
Docker/Kubernetes enclosures retain their existing bounded ownership and selection.
There are no additional per-node GameObjects, draw passes or screen/depth copies
for this treatment. This is stylized mobile glass, without optical refraction.

Final glass validation: **96 Edit Mode tests passed**, including outward-facing
beveled mesh geometry and real transparent enclosure assets; **10 focused Play
Mode tests passed**, covering all ten infrastructure types, indicators, database
exploration and existing mobile layouts. A separate synchronous-shader capture
test also passed, and its 1280x720 render was inspected.
Evidence: `/tmp/sysarchitect-glass-final-edit.xml`,
`/tmp/sysarchitect-glass-reflection-play.xml`, `/tmp/sysarchitect-glass-capture-play.xml`;
image `/tmp/DefaultCompany/SysARchitect/sysarchitect-glass-models.png`.
The old enclosure regression's URP-specific keyword assertion was replaced with
the dedicated shader's transparent render-tag contract; its blend/depth/asset-alpha
checks remain. No physical-device performance claim is made.

## English lesson narration

All five lessons have bundled synthetic English teaching audio: **20 distinct
recordings, 700.4 seconds in total, 4,400,410 bytes of mono Vorbis audio**. Selecting
or opening a lesson plays its overview and first instruction; Next/Previous step
replaces that audio with the matching heading, instruction and explanation. Replay,
pause/resume and mute controls remain pinned beside mobile instruction navigation.
Closing the guide, loading an example, opening the lesson picker or starting a quiz
stops narration. App suspension pauses it for explicit resumption. Muting is scoped
to the current scene; existing saved lesson completion and quiz results are retained.

Playback uses the existing lesson text and Unity AudioSource, without advancing
instruction steps, creating architecture, driving simulation or using a runtime
network service. Existing highlights, challenges, examples and quiz scoring remain
separate. Both saved workspace scenes have one narrator and one source; repeated
configuration reuses them and their three buttons. Clips are compressed in memory.
See [voice provenance and regeneration](LessonNarrationVoice.md).

Focused verification: **8 Play Mode tests passed** for actual clip changes across
all five lessons, forward/backward navigation, pause/replay/mute, closure/quiz stop,
progress and simulation-clock preservation, and rendered mobile controls at
1280x720, 1920x1080, 2400x1080 and 2796x1290 with safe-area insets. Captures at
1280 and 2400 were visually inspected. The complete **97 Edit Mode tests passed**,
including exact transcript synchronization, distinct audio hashes, durations and
all earlier data/model tests. Phone speakers, headset output and physical AR have
not been verified on a connected device.

## Final integrated verification

Current source passed **102 Edit Mode tests and 57 Play Mode tests**, with zero
failures/skips and editor exit 0. Results: `/tmp/sysarchitect-teaching-final-edit2.xml`
and `/tmp/sysarchitect-teaching-final-play5.xml`, with matching `.log` files. This
includes the real-provider XR demonstration and seven XR lifecycle/surface tests,
all desktop/Virtual editing and request/resource regressions, lessons, quizzes,
narration and phone layouts. Camera motion and touches are driven by tests.

Intermediate runs are retained. They exposed obsolete URP keyword assertions and
the initial automatic-placement gate waiting forever on otherwise valid compact
planes (20 cm and 40x60 cm were observed). Visible-plane selection and adaptive
scale address that behavior; exact size/bounds checks cover compact and large planes.
A boundary initialization error during development was repaired using Unity's
null comparison. Its cancelled isolated editor crashed during shutdown; subsequent
clean focused/full runs exited 0. That editor shutdown is separate from the later host OOM described below. No physical device test is claimed.

Unity processes ran sequentially with two job workers and one import worker.
Registry-driven toolchain migration was disabled for cached validation. Two
commands timed out in automatic approval review before execution and succeeded on
retry. Production package manifests, XR providers and working Linux/APK artifacts
were preserved. Unrelated untracked `dotnet-tools.json` and
`Assets/StreamingAssets/VoiceModels/` were preserved and excluded from commits.
Recovery subsequently found copies of those models in the validation/exported inputs;
these copies were moved aside before packaging, and APK inspection confirmed no
model files were shipped. The original repository files remain untouched. `.agents` remains ignored.
