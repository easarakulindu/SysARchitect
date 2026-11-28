# Automatic AR surfaces, glass and English narration — November 28, 2026

## Automatic AR surface

The existing ARWorkspace now automatically anchors the shared graph after the camera
aims at a tracked horizontal-up plane with both dimensions at least 0.84 m. The same
plane must remain within 5 cm of its candidate centre for 0.8 seconds. Actual AR
polygon raycasts select the visible surface; an off-screen plane is not selected.
No separate graph or simulation is created. Existing manual placement remains an
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
