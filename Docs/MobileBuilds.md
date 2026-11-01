# Mobile builds and device handoff

**Current verified delivery (November 1):** version **0.11.0**,
`Builds/Android-RC1/SysARchitect.apk`, with optional ARCore and labelled non-AR
Virtual Workspace. The earlier default-path APK was preserved. Use the RC1 APK
for testing; see [build evidence, exact ADB commands and limits](MobileRecovery2026-11-01.md).
The packaged APK requires GLES 3.1 in addition to API 26+ and ARM64. No phone was
connected during verification.

Use Unity **6000.6.5f1** (revision 3ff58d469c8a), including its matching platform
modules. Keep Packages/manifest.json and packages-lock.json: AR Foundation,
ARCore and ARKit are **6.6.2**, URP **17.6.0**, XRI **3.6.1**. The 3.5.1 samples
folder is not the installed XRI version. Do not upgrade packages for this handoff.

`SysARchitect/Mobile/Configure AR Platforms` applies the checked settings below.
The build commands select only `Assets/SysARchitect/Scenes/ARWorkspace.unity`;
desktop continues to use DesktopWorkspace and disables standalone XR startup.
Bundle identifier for both mobile platforms: **com.easara.sysarchitect**.
Build outputs are ignored by Git. No release keystore, team or private signing
credentials are included in the project.

## Android on Linux

Install Android Build Support **with Android SDK & NDK Tools and OpenJDK** in
Unity Hub for this exact editor. This machine has Hub's SDK (platform 36,
build-tools 36.0.0), NDK **27.2.12479018 / r27c**, and OpenJDK **17.0.18**.
The command configures IL2CPP, ARM64 only, GLES3, minimum API **26** and target
API **36**, ARCore optional (depth optional). The minimum is checked against the installed
ARCore editor validation rules for Unity 6000.6, rather than older AR samples.
Internet access may be necessary for Gradle/Maven dependencies on the first build.

In Unity choose **SysARchitect > Mobile > Build Android APK**, or, with the
project closed (use a separate checkout if it is open):

```sh
"/home/easara/Unity/Hub/Editor/6000.6.5f1/Editor/Unity" \
  -batchmode -quit -projectPath "$PWD" -buildTarget Android \
  -executeMethod SysARchitect.Editor.MobileBuild.BuildAndroid \
  -logFile /tmp/sysarchitect-android-build.log
```

Output: `Builds/Android/SysARchitect.apk` and BUILD-INFO.txt. This is a local
debug-key signed APK, not a Play Store release. The helper restores the existing
custom-keystore/App Bundle choices afterwards. For local installation, enable
USB debugging, authorize this computer and use the Hub SDK's `platform-tools/adb`:

```sh
adb devices -l
adb install -r Builds/Android/SysARchitect.apk
adb shell monkey -p com.easara.sysarchitect 1
adb logcat -s Unity AndroidRuntime
```

Virtual Workspace supports an **ARM64 Android 8/API 26 or newer device** without
ARCore. It is explicitly non-AR and uses the existing shared builder/simulator.
The developer's Redmi Note 13 4G has not been connected or installed here; check
its actual Android API and ABI before installation with `adb shell getprop
ro.build.version.sdk` and `adb shell getprop ro.product.cpu.abilist`.

For genuine AR, use an **ARCore-certified physical device**, with a working camera
and current Google Play Services for AR. Minimum Android version alone does not
establish ARCore compatibility; check the exact model
against [Google's supported device list](https://developers.google.com/ar/devices).
No depth sensor is required by this horizontal-plane application.

The scene checks AR availability before enabling the session, camera, tracking,
planes, anchors or raycasts. Unsupported/missing-service outcomes open Virtual
Workspace. **Virtual / AR** switches to non-AR immediately, or retries AR from
Virtual mode; supported devices missing services can explicitly request their
installation. A supported, installed provider starts the existing AR path.
Virtual mode uses a fixed tabletop camera; Reposition resets that view and keeps
nodes/connections. Switching modes pauses simulation and retains the architecture;
returning to AR requires scanning and anchoring the workspace again. Virtual mode
has no plane, camera-feed or real-world anchor behavior. Camera permission or
tracking problems on a supported device can be bypassed using Virtual / AR.
XR initializes the provider for availability queries, but automatic subsystem
running is disabled; only the checked AR path enables the AR components.

## MacBook and iPhone

On the friend's **M3 MacBook Pro**, install the Apple Silicon build of this same
Unity version with **iOS Build Support**. Installation of Unity and Xcode on that
Mac has not been verified; do not assume either is present. The exact macOS and
iPhone model/iOS still need checking (`sw_vers` and `xcodebuild -version` on the
Mac, and Settings > General > About on the phone). No Mac is connected here.
ARKit 6.6.2 specifically requires **Xcode 26.0 or newer**; Unity's generic Xcode
minimum is insufficient for this package. Xcode 26/26.0.1 requires macOS
Sequoia **15.6** or supported Tahoe 26 versions. Newer Xcode releases have their
own macOS and connected-device OS requirements. Check the exact Mac model,
macOS, iPhone model/iOS and selected Xcode combination against
[Apple's Xcode compatibility table](https://developer.apple.com/xcode/system-requirements)
before downloading. An older Mac unable to run that macOS is a blocking dependency.

Use a physical **ARKit-compatible iPhone** running **iOS 15 or newer**, with
ARM64/Metal and camera access. The app uses basic world tracking/planes, not
LiDAR or face tracking. Verify ARKit world-tracking support on the exact device;
a simulator is not evidence of working camera-based tabletop AR. Unity's iOS
15 minimum is retained even though basic ARKit historically supported older iOS.

1. Transfer the repository including Assets **and .meta files**, Packages and
   ProjectSettings. Exclude Library, Temp, Builds, .agents and local credentials.
   Open with Unity 6000.6.5f1 and let it import for the Mac.
2. Install/open Xcode, accept its license and install the iOS platform components.
   Select that Xcode in command line tools. Connect/unlock the iPhone, trust the
   Mac, and enable Developer Mode if the phone requests it.
3. In Unity switch active target to iOS, then choose **SysARchitect > Mobile >
   Export iOS Xcode Project**. CLI equivalent uses `-buildTarget iOS` and
   `-executeMethod SysARchitect.Editor.MobileBuild.ExportIOS`. The helper refuses
   to export from Linux or without the installed iOS module.
4. Open the generated `Builds/iOS/Unity-iPhone.xcodeproj` (or generated workspace
   if a later integration adds one). Select the **Unity-iPhone application target**,
   Signing & Capabilities, automatic signing, and the developer's own Team.
   Use com.easara.sysarchitect, or a unique permitted suffix if that identifier
   cannot be provisioned. Set the same identifier in Unity before re-exporting.
   Do not put signing credentials into Git. A personal development team can be
   used subject to Apple's current provisioning/device/expiry limits.
5. Select the connected iPhone as run destination, build and Run. Resolve any
   provisioning/trust prompts, allow camera access and inspect Xcode's console.
   This is device development installation, not an App Store submission.

iOS settings use iOS 15 minimum, Metal, ARKit required and a meaningful camera
permission string. Runtime ARKit chooses/validates world-tracking support.
Linux has no iOS Build Support/Xcode and cannot build, sign or install this app
on iPhone. Export and deployment remain **unverified until performed on the Mac**.

## Release-candidate build configuration

The current helpers stamp **0.11.0** for both platforms and restore the previous
project bundle version after building. Set `SYSARCHITECT_BUILD_REVISION` to the
verified source commit before launching Unity to include provenance in
BUILD-INFO.txt; an unset value is explicitly recorded as `not recorded`. The
mobile configurator retains the AR scene's existing Input System actions asset
instead of assigning a new default asset on every build. The release-candidate
outputs and actual checks are recorded in [MobileUIReleaseCandidate.md](MobileUIReleaseCandidate.md).

## Physical-device acceptance checks

- Allow camera permission; scan a textured, well-lit horizontal surface. Verify
  detected plane, tap-to-anchor, and a stable approximately 84 cm workspace.
- Open the toolbox, place/move/select/remove nodes; connect two endpoints,
  select/delete/reconnect an edge, and inspect routing/configuration messages.
- Run a connected lesson; observe directed packets, actual counters/resources,
  database routing/lag, Docker restart and Kubernetes replacement.
- Interrupt tracking/cover the camera: edits and simulation pause with an
  explanation. Restore tracking, reposition without losing the architecture.
- Complete a quiz, restart the app and verify local score/lesson persistence.
- Verify safe-area layout and button usability on the exact screen; profile a
  representative expanded architecture before making performance claims.

Build success alone does not verify these checks. Camera permission denial,
unsupported-device and tracking recovery need device testing too.

## Historical Android build checkpoint — October 31, 2026

The recovered Android build succeeded in the isolated validation checkout using
the installed toolchain. Delivered APK: `Builds/Android/SysARchitect.apk`, version
0.9.0, **35,840,852 bytes** (check current file size after subsequent builds).
SHA-256 `44f73e630e226a0c3205a148263517747cda44baa928498b6dceed0123bed942`.
`aapt dump badging` verified com.easara.sysarchitect, min 26 / target 36, camera
permission, required camera.ar feature and **arm64-v8a only**. Hub JDK +
`apksigner.jar verify --print-certs` verified the Android Debug signing certificate.
The ordinary apksigner shell launcher could not find java on PATH; invoking its
jar with the installed JDK succeeded. No system Java installation was changed.

The first process was interrupted during native compilation. A retry then
exhausted the 7.7 GB `/tmp` filesystem during packaging. Generated native caches
were moved, preserved, to ignored `Builds/ValidationCache`, with a validation-only
symlink; the subsequent full build succeeded. Log:
`/tmp/sysarchitect-mobile-android-final.log`. Unity's reported total build size
includes native debug backup output and is larger than the compressed APK.
No connected Android device was available (`adb devices -l` empty).

The Android build confirmed Unity's unsupported **Both** input-backend warning.
The mobile build now selects Input System only while building and restores the
desktop's Both setting afterwards. AR touch uses Touchscreen and the AR scene
uses InputSystemUIInputModule; desktop mouse controllers remain intact. Shared
keyboard shortcuts use Keyboard and tolerate no physical keyboard. Existing
shared gesture/graph tests and device-to-pointer phase tests cover the adapter.

Version references: [Unity 6000.6 requirements](https://docs.unity3d.com/6000.6/Documentation/Manual/system-requirements.html),
[ARKit 6.6 requirements](https://docs.unity3d.com/Packages/com.unity.xr.arkit@6.6/manual/index.html),
and installed `com.unity.xr.arcore/Editor/ProjectValidation/ARCoreProjectValidationRules.cs`.
