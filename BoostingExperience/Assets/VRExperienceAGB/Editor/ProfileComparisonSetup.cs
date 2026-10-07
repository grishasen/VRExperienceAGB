using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRExperienceAGB.Presentation;

namespace VRExperienceAGB.Editor
{
    public static class ProfileComparisonSetup
    {
        public static void BuildCandidate()
        {
            var destination = System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath,
                "../../artifacts/builds/VRExperienceAGB-profile-comparison.apk"));
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destination));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/VRExperienceAGB/Scenes/OneTreeLearning.unity" },
                locationPathName = destination, target = BuildTarget.Android, options = BuildOptions.Development
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.InvalidOperationException("Comparison candidate build failed: " + report.summary.result);
            Debug.Log("Comparison APK built: " + destination + " | bytes " + report.summary.totalSize +
                " | warnings " + report.summary.totalWarnings + " | errors " + report.summary.totalErrors);
        }

        [MenuItem("VRExperienceAGB/Configure Profile Comparison")]
        public static void Configure()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play mode before configuring comparison assets.");
            var scene = EditorSceneManager.OpenScene("Assets/VRExperienceAGB/Scenes/OneTreeLearning.unity");
            var view = Object.FindAnyObjectByType<OneTreeExperience>();
            if (view == null) throw new System.InvalidOperationException("The teaching experience is missing.");
            view.comparisonModelFile = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/VRExperienceAGB/Data/demo-model.json");
            view.comparisonProfilesFile = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/VRExperienceAGB/Data/demo-profiles.json");
            if (view.comparisonModelFile == null || view.comparisonProfilesFile == null)
                throw new System.InvalidOperationException("The synthetic comparison fixtures are missing.");
            EditorUtility.SetDirty(view); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("Synthetic profile comparison assets configured. Default model preserved.");
        }
    }
}
