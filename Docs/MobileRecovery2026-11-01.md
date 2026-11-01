# Mobile crash recovery — November 1, 2026

## Recovered state

Inspected `/home/easara/SysARchitect`, applicable instruction locations, the local
Unity skill, Git status/diffs/history and the existing status/model/mobile handoffs.
No applicable AGENTS.md was present. `52ab98c` had already completed the mobile
drawers, safe areas and education navigation; `5b4daca` committed the compiled
0.11.0 build helpers and Mac preparation. Neither feature was recreated.
The only uncommitted file at recovery was unrelated `dotnet-tools.json`, retained
unchanged. `.agents` remains local and excluded from commits.

The restart cleared the temporary validation checkout and in-flight Android build
log. No finished 0.11.0 APK could be verified. The earlier APK and Linux demo were
still present. Prior UI evidence survived in ignored `Builds/ReleaseCandidateEvidence`
(91 Edit Mode / 37 Play Mode passed), and was preserved. A fresh isolated checkout
was created for Unity, without opening or replacing the user's project/editor.

## Completed next feature: optional ARCore and Virtual Workspace

Extended the existing ARWorkspace component, scene and mobile toolbar. There is
one graph, simulator, touch adapter and set of configuration/lesson/quiz systems.
ARCore and depth are optional. Provider initialization remains enabled so AR
availability can be queried, but automatic subsystem running is off. The saved
scene's session, AR input, camera/background/pose driver, plane/raycast/anchor
managers start disabled. The availability result enables the AR path only when
the provider reports Ready/initializing/tracking. Unsupported or missing-service
results open **Virtual Workspace (non-AR)** without starting those components.

**Virtual / AR** explicitly chooses Virtual mode or retries AR, including an
explicit installation request when a supported device needs AR services.
Switching pauses simulation and retains nodes/connections. Returning to AR needs
a new tracked surface anchor; virtual Reposition resets the fixed tabletop view.
The virtual camera excludes the verified XR Simulation environment layer, so
editor previews do not include an unrelated simulated room. Pending anchor
results are checked against the current placement revision after mode changes.

Verified and corrected a mobile service-tab timing defect: a selected component
could be recorded before its service panel became available in Update, leaving
that panel hidden on the Node tab. The layout now also detects service-panel
availability, while retaining subsequent explicit user tab choices.

## Actual verification

Unity **6000.6.5f1**, existing locked packages, Linux isolated validation project.

- **91/91 Edit Mode passed**, `/tmp/sysarchitect-mobile-recovery-core.xml` and log.
  Includes the actual AR scene's saved references, disabled startup components,
  safe-area cases and existing deterministic simulation/model tests.
- **41/41 Play Mode passed**, `/tmp/sysarchitect-mobile-recovery-play.xml` and log.
  Includes all prior desktop tests and four new Virtual Workspace cases.
- New actual-scene cases exercise touch placement, selection, movement, connection
  creation/selection/deletion/reconnection, simulation success, mode-switch graph
  retention, database replication/add-replica/sharding controls, lessons and quiz
  answer/navigation. Availability failures are injected at the result handler;
  the Linux editor's real provider is XR Simulation, which reports support.
- Rendered both the existing UI fixtures and the **actual mobile scene in Virtual
  mode** at 1280×720, 1920×1080, 2400×1080 and 2796×1290. Synthetic safe-area insets
  on the latter sizes; assertions check toolbar bounds and status/button text.
  Inspected the actual virtual tabletop render at 2400×1080 and the earlier
  1280×720 render that exposed the simulated-room clutter.
- Retained failed development runs. The service-tab issue was reproduced in
  `/tmp/sysarchitect-virtual-play4.xml`; the full final run passes that case.

Durable copies of this run's XML/logs and PNGs are in ignored
`Builds/ReleaseCandidateEvidence/2026-11-01`. These are synthetic input/editor
rendering checks, not physical touch, ARCore/ARKit tracking or device performance
results. No learner scores or teaching-effectiveness claims were fabricated.

## Device and deployment limits

The developer identified a **Redmi Note 13 4G**. It has not been connected to this
machine; its installed Android/API level and ABIs must be read using ADB. The APK
still requires ARM64 and API 26+. Virtual mode removes the ARCore requirement,
not Android/GPU/ABI requirements. It uses a fixed camera, with Reset/Reposition;
there is no mobile orbit/pinch camera implementation. Hardware acceptance still
needs installation, touch/keyboard checks, app-restart persistence, visual/profile
checks, and supported-device AR camera/permissions/plane/anchor/tracking tests.

M3 MacBook deployment remains unverified. The preserved handoff in
[MobileBuilds.md](MobileBuilds.md) specifies Apple Silicon Unity 6000.6.5f1 + iOS
Build Support, the locked ARKit 6.6.2 package, iOS 15+ Metal/camera settings, Xcode
export, local Team signing and physical-iPhone installation. On November 1,
[Unity's ARKit 6.6.2 requirements](https://docs.unity3d.com/Packages/com.unity.xr.arkit@6.6/manual/index.html)
still require Xcode 26.0+; [Apple's compatibility table](https://developer.apple.com/xcode/system-requirements)
lists Xcode 26.0/26.0.1 for macOS Sequoia 15.6 through Tahoe 26.x. Select a version
that also supports the actual phone OS. Neither Unity/Xcode installation nor the
Mac's macOS/iPhone version was assumed. Linux cannot sign or run the iOS app.

Release build output, hashes and installation commands will be recorded after
the tested feature checkpoint and actual build verification.
