# SysARchitect

An interactive Unity application for learning system architecture by building,
connecting and simulating infrastructure in a desktop, mobile Virtual Workspace,
or augmented reality workspace.

Build an architecture, run requests along its actual connections, and observe
throughput, resource use, overload, failures and recovery. Five guided lessons
combine step-specific explanations, architecture highlights, offline English
narration, practical challenges and locally saved assessments.

## Features

- **Architecture builder:** place, select, move and delete components; create,
  select, delete and reconnect directed edges with routing validation feedback.
- **Request simulation:** directional packets follow connected routes; counters,
  CPU/RAM indicators, queues, failures and recovery reflect simulation state.
- **Load balancing:** application routing across healthy backends, or explicitly
  configured database routing with supported read/write semantics.
- **Databases and caching:** expandable primary/replica and sharded databases,
  replication lag, potentially stale reads, deterministic shard-key routing and
  backend cache-aside behavior.
- **Docker learning model:** reusable image templates, separate container
  instances, shared host limits and explicit lifecycle controls.
- **Kubernetes learning model:** workers, Pods, desired/actual/ready replica
  counts, manual scaling, pending capacity and failed-Pod replacement.
- **Guided learning:** client/server, load balancing, databases, Docker and
  Kubernetes; separate lesson and instruction navigation; 20 bundled English
  narration clips; 20 pre/post quiz questions and local progress/score storage.
- **Mobile presentation:** responsive safe-area panels, touch controls, glass
  infrastructure models, translucent white AR planes and an editing boundary.

Docker, Kubernetes and infrastructure behavior are educational simulations. The
application does not start real containers, deploy clusters or provision servers.

## Open the project

1. Install **Unity 6000.6.5f1** through Unity Hub.
2. Clone this repository and add its root directory as a project in Unity Hub.
3. Open it with that editor and allow package resolution and asset import to finish.
   Keep the committed `Packages` files and Unity `.meta` files.
4. Open one of these scenes and press Play:

| Scene | Purpose |
| --- | --- |
| `Assets/SysARchitect/Scenes/DesktopWorkspace.unity` | Desktop builder and simulation |
| `Assets/SysARchitect/Scenes/ARWorkspace.unity` | Mobile AR, Virtual Workspace and Editor XR Simulation |

Core packages are AR Foundation/ARCore/ARKit **6.6.2**, URP **17.6.0**, XR
Interaction Toolkit **3.6.1**, Input System **1.20.0** and Test Framework **1.8.0**.
The imported `3.5.1` sample folder does not indicate the installed XRI version.

## First architecture

1. Open the toolbox and choose **Frontend**, then place it in the workspace.
   Repeat for **Backend** and **Database**.
2. Choose **Connect source > target**, then select Frontend and Backend. Choose
   Connect again and select Backend and Database.
3. Open **Simulation**, choose a request rate and press **Run**. Inspect arrows,
   packets, success/failure counters and resource indicators; Pause or Step to
   examine the result.
4. Select a component to inspect its configuration. Use the selection/delete
   controls to edit nodes or individual connections.
5. Open **Lessons / Progress**. **Next step / Previous step** change instruction
   content; **Next lesson / Previous lesson / Lessons** navigate topics.
   **Load example** deliberately replaces the current architecture.

Placing nodes alone does not connect them. A disconnected graph can legitimately
fail requests; read the displayed validation explanation and add the required edges.
For `Backend → Load Balancer → Database`, configure the balancer's
**Database Routing** role before connecting it.

## AR and Virtual Workspace

On supported devices, scan a textured, well-lit horizontal surface. The workspace
automatically anchors on a stable visible plane and scales to fit compact surfaces.
White translucent overlays identify detected planes; the **white boundary** marks
the actual editing area. Place components inside that boundary. **Reposition
workspace** retains the architecture and starts surface placement again.

Devices without available AR can use the clearly labelled **Virtual Workspace**.
It provides the shared mobile builder, simulation, lessons and quizzes on a virtual
tabletop. It does not use real-world tracking, camera imagery or physical anchors.
The **Virtual / AR** control switches modes or retries AR availability.

For **Editor XR Simulation**, use the AR scene with the existing Standalone
Simulation loader. Focus the Game view, hold right mouse with WASD / Q / E to scan,
then release navigation and wait for the boundary. Choose a component, close the
toolbox and left-click inside it. Ordinary mouse input works directly; Input
Debugger mouse-to-touch emulation is not required. See the
[repeatable XR demonstration](Docs/XRSimulationStabilization.md#repeatable-editor-demonstration).

## Builds and installation

Install the matching platform modules in Unity Hub before building. Build outputs
are ignored by Git and must be supplied separately with a submission.

| Target | Unity menu | Generated output |
| --- | --- | --- |
| Linux | SysARchitect → Build Linux Desktop Demo | `Builds/Linux-Current/SysARchitect.x86_64` |
| Android | SysARchitect → Mobile → Build Android APK | `Builds/Android/SysARchitect.apk` |
| iOS, on macOS | SysARchitect → Mobile → Export iOS Xcode Project | `Builds/iOS/` |

Android requires **Android Build Support, SDK/NDK and OpenJDK**. The current build
uses ARM64, minimum Android **8 / API 26**, target API **36** and GLES3. ARCore is
optional; genuine AR requires a compatible device and available AR services.

The locally verified **0.12.0** APK is preserved at
`Builds/Android-GlassNarration/SysARchitect.apk`. It includes glass visuals,
automatic surface placement and English narration. It is signed with a development
debug key for direct installation, not a production store release. The subsequent
Editor mouse correction does not change Android behavior.

With USB debugging enabled and the computer authorized:

```sh
adb devices -l
adb install -r Builds/Android-GlassNarration/SysARchitect.apk
adb shell monkey -p com.easara.sysarchitect 1
```

For a fresh build, substitute `Builds/Android/SysARchitect.apk` in the installation
command. See [build recovery and APK verification](Docs/AndroidBuildRecovery2026-11-28.md)
and [Android/iOS setup](Docs/MobileBuilds.md). The latter also contains historical
build records; use the recovery record for the current APK. iOS export requires
the matching Unity iOS module, a compatible Mac/Xcode installation and the
developer's own signing setup. Mac export and physical iOS deployment are unverified.

## Verification and limitations

Use **Window → General → Test Runner** to run Edit Mode and Play Mode tests.
Run Unity test/build processes sequentially; one job/import worker and the Android
helper's bounded Gradle resources reduce pressure on the development host.

Recorded Unity results, not newly rerun for this README:

- Integrated teaching/presentation baseline: **102 Edit Mode / 57 Play Mode passed**.
- Subsequent Editor mouse correction: **58 Play Mode / 17 focused Edit Mode passed**.
- Real-provider Editor XR checks cover tracking, planes, anchoring/repositioning,
  builder input, traffic, resources and lesson navigation. Mobile UI checks cover
  multiple phone resolutions.

Automated XR checks use programmatically driven camera poses and synthetic input.
They do not establish physical ARCore/ARKit compatibility or device performance.
Complete physical-device AR acceptance remains outstanding. Glass is a stylized
mobile shader without optical refraction; Kubernetes HPA and production deployment
are outside the simulation's scope. Learning progress is local, without cloud sync.

## Project guide

- [Current project status and handoff](Docs/ProjectStatus.md)
- [Routing, cache behavior and connection editing](Docs/RoutingAndConnections.md)
- [Database-router correction](Docs/DatabaseRouterFix.md)
- [Educational modules and simulation scope](Docs/EducationAndAR.md)
- [Lesson navigation](Docs/LessonNavigation.md) and [step-content correction](Docs/LessonStepContent.md)
- [Automatic surfaces, glass and narration](Docs/ARSurfaceAndNarration.md)
- [Voice provenance and regeneration](Docs/LessonNarrationVoice.md)
- [XR lifecycle stabilization and mouse-input verification](Docs/XRSimulationStabilization.md)

Application code, scenes and tests live under `Assets/SysARchitect/`; shader/material
assets reuse the project's URP pipeline. `Tools/GenerateLessonNarration.py` supports
offline audio regeneration; bundled recordings require no runtime speech service.
Do not include `.agents`, generated caches/builds, signing credentials or unrelated
local voice models in the source submission. Preserve third-party package and voice
attribution records when redistributing their assets.
