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

## Verified release-candidate artifacts

Both builds succeeded with Unity 6000.6.5f1 from feature commit
**f79d9466f999920259df750ca14d0f54b8a08f06**. BUILD-INFO.txt records that source
revision, Unity version, UTC build time, explicit scene and dependency hash.
The APK and Linux player identify themselves as **0.11.0**. The later handoff
documentation commit does not change player code/assets.

| Output | Actual result |
| --- | --- |
| `Builds/Android-RC1/SysARchitect.apk` | **35,865,306 bytes**, built UTC 2026-11-01T06:25:10Z |
| APK SHA-256 | `c5014701702dc8c010444a1164909bf1246e351a93d8454ca2db06a03b33b798` |
| `Builds/Linux-RC1/SysARchitect.x86_64` | Build succeeded; Unity reported 108,895,869 bytes, built UTC 2026-11-01T06:26:44Z |
| Linux startup smoke check | Eight seconds, no application exceptions/errors observed; timeout exit 124 intentionally stopped the process |

APK verification used Hub SDK build-tools **36.0.0 aapt**, JDK **17.0.18** and
apksigner.jar. Manifest confirms com.easara.sysarchitect, min API 26 / target 36,
**ARCore metadata `optional`**, no required camera.ar/depth feature, camera
permission and GLES **3.1** (0x30001). ZIP native libraries are **arm64-v8a only**.
Signature verification passed with the local Android Debug certificate. This is
a locally signed test APK, not a store publication or release-key artifact.
The build used NDK **27.2.12479018 / r27c** and Gradle **9.3.1**. Unity's larger
reported Android build total includes native backup output; the APK size above
is measured from the delivered file itself.

The helper restored Both input backends and the prior project bundle version
after Android building. The validation checkout's Unity material import normalized
WorkloadGlass shader serialization; the source material was not overwritten.
The earlier `Builds/Android/SysARchitect.apk`, `Builds/Linux-Current` and
`RunDesktopDemo.sh` remain preserved. Use the explicit RC1 paths for these new
builds. Nothing was pushed or published.

Build/player logs, APK badging/manifest/signature output, BUILD-INFO copies, Linux
file hashes, final tests and renders are retained under ignored
`Builds/ReleaseCandidateEvidence/2026-11-01`. `adb devices -l` was empty; no APK
installation, physical AR or on-device persistence/performance test was performed.
The physical-device/Mac checks above remain outstanding.

## Install on the Redmi Note 13 4G

Enable USB debugging, connect via USB and authorize this computer. From the
project root, use the actual installed SDK tool (no system Java changes needed):

```sh
adb_tool=/home/easara/Unity/Hub/Editor/6000.6.5f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb
"$adb_tool" devices -l
"$adb_tool" shell getprop ro.build.version.sdk
"$adb_tool" shell getprop ro.product.cpu.abilist
"$adb_tool" shell getprop ro.opengles.version
"$adb_tool" install -r Builds/Android-RC1/SysARchitect.apk
"$adb_tool" shell am start -n com.easara.sysarchitect/com.unity3d.player.UnityPlayerGameActivity
"$adb_tool" logcat -s Unity AndroidRuntime
```

Confirm API >= 26, arm64-v8a and GLES >= 3.1 (decimal 196609) on the actual phone.
If AR is unavailable, the app opens **Virtual Workspace (non-AR)**. Virtual / AR
also allows choosing that mode when the device reports AR support. Use Toolbox
to build, Edit / Connect for edge operations, Simulation for Run/Build/Step,
Service for database/routing/container configuration, and Lessons / Progress
for guidance and quizzes. These are installation instructions, not an executed
phone test. Run the new Linux player directly from its RC1 path above.
