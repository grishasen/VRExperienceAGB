using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRExperienceAGB.Presentation;
using Object = UnityEngine.Object;

namespace VRExperienceAGB.Tests
{
    public class AgbCdhPlayModeTests
    {
        private string folder;
        private OneTreeExperience view;
        private const string ModelName = "Mobile_Click_Through_Rate_GB_FictionalCustomer";
        private const string Constant = "{\"score\":0.125,\"gain\":0,\"sampleCount\":10}";
        private const string Split = "{\"score\":999,\"gain\":2,\"sampleCount\":20,\"split\":\"Party.Value < 5\",\"left\":{\"score\":-0.25,\"gain\":0,\"sampleCount\":10},\"right\":{\"score\":0.75,\"gain\":0,\"sampleCount\":10}}";
        private static string Export(bool constant) => "{\"type\":\"AdaptiveBoostScoringModel\",\"algorithm\":\"GRADIENT_BOOST\",\"model\":{\"booster\":{\"trees\":[" + (constant ? Constant : Split + "," + Constant) + "]}}}";
        private static string Record(string value) => "{\"pxCommonInputs\":[{\"pzCommonInputReference\":\"1\",\"pxCommonInput\":{\"values\":{\"Party.Value\":{\"type\":\"NUMERIC\",\"value\":\"" + value + "\"}}}}],\"pxModelExecutionResults\":[{\"pxModelExecutionResults\":{\"modelType\":\"ADM\",\"ruleName\":\"" + ModelName + "\",\"modelExecutionIndex\":\"1\",\"pzCommonInputReferences\":[1],\"values\":{\"pyPropensity\":{\"type\":\"NUMERIC\",\"value\":\"0.99\"}}}}]}";
        [UnitySetUp] public IEnumerator Open()
        {
            yield return SceneManager.LoadSceneAsync("OneTreeLearning"); yield return null;
            view = Object.FindAnyObjectByType<OneTreeExperience>(); view.enabled = false; view.Director.enabled = false;
            Object.FindAnyObjectByType<DesktopTreePreview>().enabled = false;
            folder = Path.Combine(Path.GetTempPath(), "agb-cdh-scene-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
            view.Director.Library.Configure(view.Director, Path.Combine(folder, "Library"));
            view.SendMessage("OnApplicationFocus", true); view.SendMessage("OnApplicationPause", false);
        }
        [TearDown] public void Close() { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        private IEnumerator Import(string name, string json, bool profiles)
        {
            string path = Path.Combine(folder, name); File.WriteAllText(path, json);
            view.Director.Library.ImportPath(path, profiles, true);
            yield return Wait();
        }
        private IEnumerator Wait()
        {
            float until = Time.realtimeSinceStartup + 15;
            while (view.Director.Library.Busy && Time.realtimeSinceStartup < until) yield return null;
            Assert.That(view.Director.Library.Busy, Is.False, view.Director.Library.Status);
        }
        [UnityTest] public IEnumerator CdhPairCalculatesEveryTreeAndReopensFromSavedFiles()
        {
            yield return Import("export_" + ModelName + "_20261008T183757.json", Export(false), false);
            yield return Import("records.jsonl", Record("4") + "\n" + Record("5"), true);
            var d = view.Director; Assert.That(view.AvailableProfiles.Profiles.Count, Is.EqualTo(2), d.Library.Status);
            d.StartPlayback(true); d.Activate("calculate-all");
            Assert.That(d.Playback.CompletedTrees, Is.EqualTo(2));
            Assert.That(d.Playback.A.RawScore, Is.EqualTo(-.125)); Assert.That(d.Playback.B.RawScore, Is.EqualTo(.875));
            Assert.That(d.FinalText, Does.Contain("Score -0.125").And.Contain("Score 0.875").And.Contain("Propensity comes from calibration"));
            Assert.That(d.FinalText, Does.Not.Contain("Probability =").And.Not.Contain("percentage points"));
            d.Activate("result-details");
            d.OpenMenu("compare"); d.Activate("values"); d.Activate("profile-source");
            Assert.That(d.MenuPage, Is.EqualTo("profile-source"));
            Assert.That(view.GetComponentsInChildren<TMPro.TMP_Text>().Any(t => t.text.Contains("0.99") && t.text.Contains("CDH")), Is.True);
            d.Library.ImportPath(d.Library.Storage.Files(false).Single(), false, false); yield return Wait();
            Assert.That(view.Model.ExportInfo.Name, Is.EqualTo(ModelName)); Assert.That(view.AvailableProfiles.Profiles.Count, Is.EqualTo(2));
            d.StartPlayback(true); d.Activate("calculate-all"); Assert.That(d.Playback.B.RawScore, Is.EqualTo(.875));
        }
        [UnityTest] public IEnumerator ConstantExportSupportsPairAndDisablesEmptyPredictorEditor()
        {
            yield return Import("export_" + ModelName + "_20261008T183757.json", Export(true), false);
            yield return Import("records.jsonl", Record("") + "\n" + Record("5"), true);
            var d = view.Director; Assert.That(view.AvailableProfiles.Profiles.Count, Is.EqualTo(2), d.Library.Status);
            d.OpenMenu("compare"); d.Activate("values");
            Assert.That(view.GetComponentsInChildren<TMPro.TMP_Text>().Any(t => t.text.Contains("constant leaves")), Is.True);
            d.StartPlayback(true); d.Activate("calculate-all");
            Assert.That(d.Playback.A.RawScore, Is.EqualTo(.125)); Assert.That(d.Playback.B.RawScore, Is.EqualTo(.125));
            d.CloseMenu(); view.Garden.M5.Comparison.Open(); view.Garden.M5.Comparison.Activate(M5Action.CompareEdit);
            Assert.That(view.Comparison.Draft, Is.Null); yield return null;
        }
        [UnityTest] public IEnumerator InvalidUsedCdhValuePreservesCompletedPairAndSavedProfiles()
        {
            yield return Import("export_" + ModelName + "_20261008T183757.json", Export(false), false);
            yield return Import("records.jsonl", Record("4") + "\n" + Record("5"), true);
            var d = view.Director; d.StartPlayback(true); d.Activate("calculate-all"); var accepted = d.Playback;
            yield return Import("bad.json", Record(""), true);
            Assert.That(d.Library.Status, Does.Contain("AmbiguousCdhEmptyValue"));
            Assert.That(d.Playback, Is.SameAs(accepted)); Assert.That(d.Library.Storage.Files(true).Length, Is.EqualTo(1));
            Assert.That(d.Playback.B.RawScore, Is.EqualTo(.875));
        }
    }
}
