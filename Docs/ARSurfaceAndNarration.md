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
