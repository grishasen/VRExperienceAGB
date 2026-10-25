using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.Rendering;

namespace VRExperienceAGB.Editor
{
    /// <summary>Windows x64 Mono build, also supported by the Mac Editor with Windows Build Support.</summary>
    public static class WindowsDesktopBuild
    {
        [MenuItem("VRExperienceAGB/Desktop/Build Windows x64")]
        public static void Build()
        {
            const BuildTarget target = BuildTarget.StandaloneWindows64;
            if (EditorUserBuildSettings.activeBuildTarget != target)
                throw new InvalidOperationException("Switch the active target to Windows x64 before building.");
            EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.Standalone;
            string destination = Path.GetFullPath("Builds/Windows/VRExperienceAGB/VRExperienceAGB.exe");
            var arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < arguments.Length; i++) if (arguments[i] == "-buildOutput") destination = arguments[i + 1];
            string name = PlayerSettings.productName;
            var preloaded = PlayerSettings.GetPreloadedAssets();
            var mode = PlayerSettings.fullScreenMode;
            int width = PlayerSettings.defaultScreenWidth, height = PlayerSettings.defaultScreenHeight;
            bool background = PlayerSettings.runInBackground, resizable = PlayerSettings.resizableWindow;
            var backend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone);
            var stripping = PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.Standalone);
            int architecture = PlayerSettings.GetArchitecture(NamedBuildTarget.Standalone);
            string identifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Standalone);
            bool automaticGraphics = PlayerSettings.GetUseDefaultGraphicsAPIs(target);
            var graphics = PlayerSettings.GetGraphicsAPIs(target);
            var xr = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            bool initializeXr = xr != null && xr.InitManagerOnStart;
            try
            {
                if (xr != null) { xr.InitManagerOnStart = false; EditorUtility.SetDirty(xr); }
                PlayerSettings.productName = "VRExperienceAGB Desktop";
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.vrexperienceagb.desktop");
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
                PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Standalone, ManagedStrippingLevel.Medium);
                PlayerSettings.SetArchitecture(NamedBuildTarget.Standalone, 1);
                PlayerSettings.SetUseDefaultGraphicsAPIs(target, false);
                PlayerSettings.SetGraphicsAPIs(target, new[] { GraphicsDeviceType.Direct3D11 });
                PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
                PlayerSettings.defaultScreenWidth = 1440; PlayerSettings.defaultScreenHeight = 900;
                PlayerSettings.resizableWindow = true; PlayerSettings.runInBackground = false;
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = new[] { "Assets/VRExperienceAGB/Scenes/OneTreeLearning.unity" },
                    locationPathName = destination, target = target, options = BuildOptions.Development
                });
                Debug.Log("Windows desktop build: " + report.summary.result + " · " + destination);
                if (report.summary.result != BuildResult.Succeeded || report.summary.totalErrors != 0 ||
                    !File.Exists(destination) || !File.Exists(Path.Combine(Path.GetDirectoryName(destination), "UnityPlayer.dll")))
                    throw new InvalidOperationException("Windows build is incomplete: " + report.summary.result +
                        ", errors: " + report.summary.totalErrors + ". Check output files and restart Unity after installing build modules.");
            }
            finally
            {
                PlayerSettings.productName = name; PlayerSettings.fullScreenMode = mode;
                PlayerSettings.defaultScreenWidth = width; PlayerSettings.defaultScreenHeight = height;
                PlayerSettings.resizableWindow = resizable; PlayerSettings.runInBackground = background;
                PlayerSettings.SetPreloadedAssets(preloaded);
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, backend);
                PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Standalone, stripping);
                PlayerSettings.SetArchitecture(NamedBuildTarget.Standalone, architecture);
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, identifier);
                PlayerSettings.SetGraphicsAPIs(target, graphics);
                PlayerSettings.SetUseDefaultGraphicsAPIs(target, automaticGraphics);
                if (xr != null) { xr.InitManagerOnStart = initializeXr; EditorUtility.SetDirty(xr); }
                AssetDatabase.SaveAssets();
            }
        }
    }
}
