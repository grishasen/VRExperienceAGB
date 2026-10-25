using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using UnityEditor.XR.Management;

namespace VRExperienceAGB.Editor
{
    /// <summary>Reproducible Mac build configuration, leaving Android and shared player settings intact.</summary>
    public static class MacDesktopBuild
    {
        private const string Plugin = "Assets/Plugins/macOS/libVRAGBDesktop.dylib";
        [MenuItem("VRExperienceAGB/Desktop/Prepare macOS")]
        public static void Prepare()
        {
            if (!File.Exists(Plugin)) throw new FileNotFoundException("Run scripts/build-macos-native.sh before building.", Plugin);
            AssetDatabase.ImportAsset(Plugin, ImportAssetOptions.ForceSynchronousImport);
            var importer = (PluginImporter)AssetImporter.GetAtPath(Plugin);
            importer.SetCompatibleWithAnyPlatform(false);
            importer.SetCompatibleWithEditor(false);
            importer.SetCompatibleWithPlatform(BuildTarget.StandaloneOSX, true);
            importer.SetPlatformData(BuildTarget.StandaloneOSX, "CPU", "AnyCPU");
            importer.SaveAndReimport();
            var settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            if (settings != null) { settings.InitManagerOnStart = false; EditorUtility.SetDirty(settings); }
            AssetDatabase.SaveAssets();
        }
        [MenuItem("VRExperienceAGB/Desktop/Build macOS")]
        public static void Build()
        {
            Prepare();
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneOSX)
                throw new InvalidOperationException("Switch the active target to macOS before building.");
            EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.Standalone;
            string destination = Path.GetFullPath("Builds/macOS/VRExperienceAGB.app");
            var arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < arguments.Length; i++) if (arguments[i] == "-buildOutput") destination = arguments[i + 1];
            string name = PlayerSettings.productName;
            var preloaded = PlayerSettings.GetPreloadedAssets();
            var mode = PlayerSettings.fullScreenMode;
            int width = PlayerSettings.defaultScreenWidth, height = PlayerSettings.defaultScreenHeight;
            bool background = PlayerSettings.runInBackground, resizable = PlayerSettings.resizableWindow;
            try
            {
                PlayerSettings.productName = "VRExperienceAGB Desktop";
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.vrexperienceagb.desktop");
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
                PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Standalone, ManagedStrippingLevel.Medium);
                PlayerSettings.SetArchitecture(NamedBuildTarget.Standalone, 1);
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneOSX, false);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneOSX, new[] { GraphicsDeviceType.Metal });
                PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
                PlayerSettings.defaultScreenWidth = 1440; PlayerSettings.defaultScreenHeight = 900;
                PlayerSettings.resizableWindow = true; PlayerSettings.runInBackground = false;
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = new[] { "Assets/VRExperienceAGB/Scenes/OneTreeLearning.unity" },
                    locationPathName = destination, target = BuildTarget.StandaloneOSX,
                    options = BuildOptions.Development
                });
                Debug.Log("macOS desktop build: " + report.summary.result + " · " + destination);
                if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("macOS build failed: " + report.summary.result);
            }
            finally
            {
                PlayerSettings.productName = name; PlayerSettings.fullScreenMode = mode;
                PlayerSettings.defaultScreenWidth = width; PlayerSettings.defaultScreenHeight = height;
                PlayerSettings.resizableWindow = resizable; PlayerSettings.runInBackground = background;
                PlayerSettings.SetPreloadedAssets(preloaded);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
