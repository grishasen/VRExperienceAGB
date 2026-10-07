using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace VRExperienceAGB.Editor
{
    public static class HorizonWildlifeBuild
    {
        public static void BuildCandidate()
        {
            string destination = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,
                "../../artifacts/builds/VRExperienceAGB-horizon-wolves.apk"));
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/VRExperienceAGB/Scenes/OneTreeLearning.unity" },
                locationPathName = destination, target = BuildTarget.Android, options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new System.InvalidOperationException("Horizon wildlife build failed: " + report.summary.result);
            Debug.Log("Horizon wildlife APK built: " + destination + " | APK bytes " + new FileInfo(destination).Length +
                " | warnings " + report.summary.totalWarnings + " | errors " + report.summary.totalErrors);
        }
    }
}
