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
            const string path = "Builds/Linux/SysARchitect.x86_64";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var xr = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            bool autoStartXr = xr != null && xr.InitManagerOnStart;
            try
            {
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
            }
            finally
            {
                if (xr != null) xr.InitManagerOnStart = autoStartXr;
            }
        }
    }
}
