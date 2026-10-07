using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace VRExperienceAGB.Editor
{
    public static class ExplorationExtensionsBuild
    {
        public static void BuildCandidate()
        {
            string directory = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "../../artifacts/builds"));
            Directory.CreateDirectory(directory);
            string resultPath = Path.Combine(directory, "extensions-build-result.txt");
            File.WriteAllText(resultPath, "Building");
            try {
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = new[] { "Assets/VRExperienceAGB/Scenes/OneTreeLearning.unity" },
                    locationPathName = Path.Combine(directory, "VRExperienceAGB-extensions.apk"),
                    target = BuildTarget.Android, options = BuildOptions.Development
                });
                File.WriteAllText(resultPath, report.summary.result + "\nErrors: " + report.summary.totalErrors +
                    "\nWarnings: " + report.summary.totalWarnings + "\nBytes: " + report.summary.totalSize);
                if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Extensions build failed: " + report.summary.result);
            } catch (Exception error) { File.AppendAllText(resultPath, "\n" + error.Message); throw; }
        }
    }
}
