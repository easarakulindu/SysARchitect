using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.ARCore;
using UnityEditor.XR.ARKit;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace SysARchitect.Editor
{
    /// <summary>Explicit mobile scene and toolchain settings; never signs with a private release key.</summary>
    public static class MobileBuild
    {
        [MenuItem("SysARchitect/Mobile/Configure AR Platforms")]
        public static void Configure()
        {
            var android = NamedBuildTarget.Android;
            PlayerSettings.SetApplicationIdentifier(android, "com.easara.sysarchitect");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.easara.sysarchitect");
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            // Installed ARCore 6.6 rules for Unity 6000.6 require API 26 with GLES3.
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.cameraUsageDescription = "SysARchitect uses the camera to place your architecture on a real tabletop.";
            // Linux has no iOS renderer module; retain its existing serialized Metal API.
            if (BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
            {
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.iOS, false);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS, new[] { GraphicsDeviceType.Metal });
            }
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            ARCoreSettings.GetOrCreateSettings().requirement = ARCoreSettings.Requirement.Optional;
            ARCoreSettings.GetOrCreateSettings().depth = ARCoreSettings.Requirement.Optional;
            ARKitSettings.GetOrCreateSettings().requirement = ARKitSettings.Requirement.Required;
            ARWorkspaceSetup.ConfigureProviders();
            ConfigureTouchUI();
            ARWorkspaceSetup.ConfigureAvailabilityGate();
            AssetDatabase.SaveAssets();
            Debug.Log("Mobile configuration: ARWorkspace, ARM64 IL2CPP, Android 26–36 GLES3, optional ARCore; iOS 15+ Metal, required ARKit.");
        }

        private static void ConfigureTouchUI()
        {
            var previous = EditorSceneManager.GetSceneManagerSetup();
            foreach (var item in previous)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneByPath(item.path).isDirty)
                    throw new InvalidOperationException("Save the open scene before configuring the mobile UI.");
            var scene = EditorSceneManager.OpenScene(ARWorkspaceSetup.ScenePath);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var events in root.GetComponentsInChildren<EventSystem>(true))
                {
                    var legacy = events.GetComponent<StandaloneInputModule>();
                    if (legacy != null) UnityEngine.Object.DestroyImmediate(legacy);
                    var input = events.GetComponent<InputSystemUIInputModule>();
                    if (input == null) input = events.gameObject.AddComponent<InputSystemUIInputModule>();
                    if (input.actionsAsset == null) input.AssignDefaultActions();
                }
            EditorSceneManager.SaveScene(scene);
            // Batch mode can start with an untitled scene, which cannot be restored by path.
            if (previous.Length > 0 && previous.All(item => !string.IsNullOrEmpty(item.path)))
                EditorSceneManager.RestoreSceneManagerSetup(previous);
        }

        [MenuItem("SysARchitect/Mobile/Build Android APK")]
        public static void BuildAndroid()
        {
            Configure();
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new InvalidOperationException("Install Android Build Support, SDK/NDK and OpenJDK for Unity " + Application.unityVersion);
            bool appBundle = EditorUserBuildSettings.buildAppBundle;
            bool customSigning = PlayerSettings.Android.useCustomKeystore;
            try
            {
                EditorUserBuildSettings.buildAppBundle = false;
                PlayerSettings.Android.useCustomKeystore = false;
                Build(BuildTarget.Android, "Builds/Android/SysARchitect.apk");
            }
            finally
            {
                EditorUserBuildSettings.buildAppBundle = appBundle;
                PlayerSettings.Android.useCustomKeystore = customSigning;
            }
        }

        [MenuItem("SysARchitect/Mobile/Export iOS Xcode Project")]
        public static void ExportIOS()
        {
            if (Application.platform != RuntimePlatform.OSXEditor ||
                !BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
                throw new InvalidOperationException("Export on a Mac with Unity " + Application.unityVersion + " iOS Build Support and Xcode 26+. See Docs/MobileBuilds.md.");
            Configure();
            Build(BuildTarget.iOS, "Builds/iOS");
        }

        private static void Build(BuildTarget target, string path)
        {
            string previousVersion = PlayerSettings.bundleVersion;
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var inputHandler = settings.FindProperty("activeInputHandler");
            int previousInput = inputHandler.intValue;
            try
            {
                // Android does not support Both. Desktop retains its legacy mouse input.
                inputHandler.intValue = 1; settings.ApplyModifiedPropertiesWithoutUndo();
                PlayerSettings.bundleVersion = "0.11.2";
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ARWorkspaceSetup.ScenePath },
                    locationPathName = path, target = target, options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException(target + " build failed: " + report.summary.result);
                string directory = target == BuildTarget.iOS ? path : Path.GetDirectoryName(path);
                File.WriteAllText(Path.Combine(directory, "BUILD-INFO.txt"),
                    $"SysARchitect {PlayerSettings.bundleVersion}\nUnity {Application.unityVersion}\nTarget {target}\nSource revision {Environment.GetEnvironmentVariable("SYSARCHITECT_BUILD_REVISION") ?? "not recorded"}\nBuilt UTC {DateTime.UtcNow:O}\nScene {ARWorkspaceSetup.ScenePath}\nScene dependency hash {AssetDatabase.GetAssetDependencyHash(ARWorkspaceSetup.ScenePath)}\n");
                Debug.Log($"Mobile build succeeded: {Path.GetFullPath(path)} ({report.summary.totalSize} bytes)");
            }
            finally
            {
                PlayerSettings.bundleVersion = previousVersion;
                settings.Update(); inputHandler = settings.FindProperty("activeInputHandler");
                inputHandler.intValue = previousInput; settings.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
