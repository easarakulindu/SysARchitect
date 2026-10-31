using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEngine;

namespace SysARchitect.Editor
{
    public static class DesktopBuild
    {
        [MenuItem("SysARchitect/Build Linux Desktop Demo")]
        public static void BuildLinux()
        {
            string directory = Environment.GetEnvironmentVariable("SYSARCHITECT_BUILD_DIRECTORY") ?? "Builds/Linux-Current";
            string path = Path.Combine(directory, "SysARchitect.x86_64");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var xr = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            bool autoStartXr = xr != null && xr.InitManagerOnStart;
            string previousVersion = PlayerSettings.bundleVersion;
            try
            {
                PlayerSettings.bundleVersion = "0.9.0-mobile-ready";
                // The template uses XR Simulation for AR previews. A desktop-only player
                // must not start it or require simulation camera-background shaders.
                if (xr != null) xr.InitManagerOnStart = false;
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { DesktopSceneSetup.ScenePath },
                    locationPathName = path,
                    target = BuildTarget.StandaloneLinux64,
                    options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("Desktop build failed: " + report.summary.result);
                Debug.Log($"Desktop build succeeded: {Path.GetFullPath(path)} ({report.summary.totalSize} bytes)");
                File.WriteAllText(Path.Combine(directory, "BUILD-INFO.txt"),
                    $"SysARchitect {PlayerSettings.bundleVersion}\nUnity {Application.unityVersion}\nBuilt UTC {DateTime.UtcNow:O}\nScene {DesktopSceneSetup.ScenePath}\nScene dependency hash {AssetDatabase.GetAssetDependencyHash(DesktopSceneSetup.ScenePath)}\n");
            }
            finally
            {
                if (xr != null) xr.InitManagerOnStart = autoStartXr;
                PlayerSettings.bundleVersion = previousVersion;
            }
        }
    }
}
