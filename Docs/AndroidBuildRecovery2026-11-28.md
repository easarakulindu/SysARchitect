# Android build recovery — November 28, 2026

## Recovered state

Application changes are preserved in local commits `2154a68`, `a04d9ba`,
`c3fd07a`, `f858215` and `369fadd`. The retained NUnit XML files confirm
**102 Edit Mode / 57 Play Mode passed**, with no failures/skips. These are the
executed pre-crash results, not tests rerun after the interruption. All 20 source
narration recordings still match their recorded hashes. No runtime, scene or
lesson changes were needed for recovery. The validation AR scene differs only
by Unity's serialization of empty `m_Name` fields.

The kernel journal records a global out-of-memory event at **18:07:25**, killing
ChatGPT. Gradle then logged client disconnection and cancellation; D8's subsequent
`InterruptedException` was a cancelled build, not evidence of a Java source defect.
Native Unity compilation had completed, including the ARM64 IL2CPP library, but
Unity had not returned a successful build result or copied an APK to its output.

## Packaging recovery and safer defaults

No Unity process was running. The idle daemon owned by the interrupted build was
stopped, and the existing generated Gradle project was resumed **offline**, without
recompiling Unity native code: one worker, no project parallelism, one active Java
processor and a **1 GB heap**. Gradle completed successfully in **19 seconds**, exit 0.
The recovered signed 0.12.0 APK is preserved separately at
`Builds/Android-GlassNarration-Recovered/SysARchitect.apk` (38,802,812 bytes;
SHA-256 `a2a22127c128aab83af4ea83d5218510754eded2f589ff99f2ba74b9d4ba3edc`).
This does not retroactively establish exit 0 for the interrupted Unity invocation.

The existing mobile build helper now bounds native workers to one, caps Java at
1 GB/one processor, and writes one-worker/non-parallel/no-daemon Gradle settings
through the installed SDK's Android post-generation callback. The callback is
guarded for the Android editor target so an iOS-only installation has no new
Android editor-assembly requirement. Temporary environment/preferences are restored
in `finally` on normal completion. Final helper compilation passed in Unity
6000.6.5f1, exit 0 (`/tmp/sysarchitect-resource-limits-final-compile.log`).
These limits reduce build pressure; other applications still share host memory.

## Preserved unrelated files and verification

Contrary to the earlier handoff's exclusion statement, copies of the untracked
`Assets/StreamingAssets/VoiceModels/` files were present in the temporary validation
and exported Gradle inputs. They have no references in the application code and
were moved out of those inputs before packaging. Those temporary copies and metadata
are retained in `/tmp/SysARchitectRecoveryPreserved/20261128-181054/`.
The repository originals and `dotnet-tools.json` were left untouched/uncommitted.
The recovered APK contains neither `.pth`/`.index` models nor a VoiceModels folder.

`apksigner verify --print-certs` passed, using the same Android Debug certificate
as 0.11.2 (SHA-256 `f8b23b34451fa9109fef5831bea70a74e6a6e148c33c96e4270805f91e2214f1`).
Manifest inspection confirms `com.easara.sysarchitect`, **0.12.0**, versionCode 1,
minimum API 26 / target 36 and ARM64. Package inspection found all 20 narration clip
names, all 20 transcript hashes and 20 FSB5 sound-bank headers. This establishes
packaged audio data, not listening through physical phone speakers.

Existing APKs and the Linux demo remain preserved. No physical AR, phone audio,
iOS deployment or device profiling is claimed. No remote pushes were made.

## Verified final Unity build

After committing the resource limits as **`f32e0d3`**, the normal Unity Android build
was rerun using the existing native cache. **Unity exited 0**, and its generated
Gradle project confirms `org.gradle.workers.max=1`, `org.gradle.parallel=false`,
`org.gradle.daemon=false`; the actual Gradle invocation used `-Xmx1024m`.
The post-generation callback was invoked by Unity, not only compiled.

- Final APK: **`Builds/Android-GlassNarration/SysARchitect.apk`**.
- Version **0.12.0**, **38,802,820 bytes**.
- SHA-256: `413f6f16e3e2e4b6a156c5bc12860a26007831724410ca3169c0da723fed70e1`.
- Source revision: `f32e0d3031a24cfb358edea95e4f07dd88d7dfe1`.
- Built UTC: `2026-11-28T12:48:38.6369940Z`.
- Unity scene dependency hash: `8d6d891c166db195b89b0e4e397839d9`.
- Signature verified again; same testing certificate as the earlier APK.
- Manifest: optional ARCore, no required `android.hardware.camera.ar`, API 26–36,
  ARM64 only. ZIP CRC passed; all 20 clip names/transcript hashes and 20 FSB5
  sound-bank headers were found; no unrelated voice-model files were packaged.

The Unity summary's 850,240,977 bytes includes intermediate/back-up build data;
it is not the actual APK size. Build info, signature, manifest and package checks
are saved beside the APK. Recovery logs are in `Builds/TeachingPresentationEvidence/Recovery/`.
The earlier 102/57 test results were recovered and checked, not rerun: runtime and
educational content are unchanged; the new build-helper code was freshly compiled
and exercised by this successful full build. The recovered APK is retained separately.

Install/update using the installed SDK, preserving app data:

```bash
/home/easara/Unity/Hub/Editor/6000.6.5f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb install -r /home/easara/SysARchitect/Builds/Android-GlassNarration/SysARchitect.apk
```

On a supported AR device, scan a textured horizontal surface and keep its white
overlay in view until the workspace anchors. Place components inside the white
boundary. Reposition retains the architecture and resumes automatic placement.
Unsupported devices use the labelled Virtual Workspace; its tests do not establish
physical AR behavior. Open Lessons / Progress or choose a topic to hear English
narration; use Next/Previous step, Replay, Pause/Resume and Mute.
