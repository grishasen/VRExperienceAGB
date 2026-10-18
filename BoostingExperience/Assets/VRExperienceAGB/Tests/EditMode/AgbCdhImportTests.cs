using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using VRExperienceAGB.Domain;
using VRExperienceAGB.Import;
using VRExperienceAGB.Application;

namespace VRExperienceAGB.Tests
{
    public class AgbCdhImportTests
    {
        private const string Name = "Mobile_Click_Through_Rate_GB_FictionalCustomer";
        private const string FileName = "export_" + Name + "_20261008T183757.json";
        private static JObject Leaf(double score) => new JObject { ["score"] = score, ["gain"] = 0, ["sampleCount"] = 10 };
        private static JObject Split(string condition) => new JObject { ["score"] = 999, ["gain"] = 123, ["sampleCount"] = 20,
            ["split"] = condition, ["left"] = Leaf(-.25), ["right"] = Leaf(.75) };
        private static string Export(params JObject[] trees) => new JObject { ["type"] = "AdaptiveBoostScoringModel", ["algorithm"] = "GRADIENT_BOOST", ["modelVersion"] = "snapshot",
            ["model"] = new JObject { ["booster"] = new JObject { ["trees"] = new JArray(trees.Cast<object>()) } } }.ToString(Formatting.None);
        private static ModelDefinition Model(string condition = "Party.Value < 5") => ModelFileLibrary.ParseModel(Export(Split(condition), Leaf(.125)), FileName).Model;
        private static JObject Cell(object value, string type = "NUMERIC") => new JObject { ["type"] = type, ["value"] = value == null ? JValue.CreateNull() : JToken.FromObject(value) };
        private static JObject Execution(string index = "1") => new JObject { ["modelType"] = "ADM", ["ruleName"] = Name,
            ["component"] = "Container." + Name + "s", ["modelVersion"] = "different-log-version", ["modelExecutionIndex"] = index,
            ["pzCommonInputReferences"] = new JArray(1), ["parameterPredictors"] = new JObject { ["values"] = new JObject { ["pyName"] = Cell("Fictional action", "SYMBOLIC") } },
            ["values"] = new JObject { ["pyPropensity"] = Cell("0.99"), ["pyModelTechnique"] = Cell("GradientBoost", "SYMBOLIC") } };
        private static JObject Record(JToken value, params JObject[] executions) => new JObject {
            ["pxCommonInputs"] = new JArray(new JObject { ["pzCommonInputReference"] = "1", ["pxCommonInput"] = new JObject { ["values"] = new JObject { ["Party.Value"] = value } }.ToString(Formatting.None) }),
            ["pxModelExecutionResults"] = new JArray(executions.Select(e => new JObject { ["pxModelExecutionResults"] = e.ToString(Formatting.None), ["pzCommonInputReferences"] = "1", ["pxUpdateDateTime"] = 1700000000000 }).Cast<object>()) };
        private static ProfileSet Read(ModelDefinition model, params JObject[] records) => Fixtures.Require(CdhProfileJson.Read(string.Join("\n", records.Select(r => r.ToString(Formatting.None))), model));

        [TestCase(4, -.125)] [TestCase(5, .875)] [TestCase(6, .875)]
        public void FullSumUsesReachedLeavesAndStrictBoundary(double input, double expected)
        {
            var model = Model(); var profile = Read(model, Record(Cell(input.ToString(System.Globalization.CultureInfo.InvariantCulture)), Execution())).Profiles[0];
            var result = Fixtures.Require(ModelEvaluator.Evaluate(model, profile));
            Assert.That(result.RawScore, Is.EqualTo(expected)); Assert.That(result.Trees.Count, Is.EqualTo(2));
            Assert.That(result.Probability, Is.EqualTo(1 / (1 + Math.Exp(-expected))).Within(1e-14));
            Assert.That(result.Probability, Is.Not.EqualTo(profile.Source.RecordedPropensity));
            Assert.That(result.OutputMeaning, Does.Contain("not reconciled"));
            Assert.That(model.ExportInfo.Name, Is.EqualTo(Name)); Assert.That(model.StructureOnlyPreview, Is.False);
            Fixtures.Reject(NormalizedModelJson.WriteModel(model), "PreviewNotInterchange");
        }
        [Test] public void ConstantLeafNeedsNoPredictorsAndRetainsExecutionEvidence()
        {
            var model = ModelFileLibrary.ParseModel(Export(Leaf(-.2)), FileName).Model;
            var profiles = Read(model, Record(Cell(""), Execution("1"), Execution("2")));
            Assert.That(model.Features, Is.Empty); Assert.That(profiles.Profiles.Count, Is.EqualTo(2));
            var pair = Fixtures.Require(ProfileComparisonSession.Create(model, profiles.Profiles[0], profiles.Profiles[1]));
            Assert.That(pair.A.Evaluation.RawScore, Is.EqualTo(-.2)); Assert.That(pair.RawDelta, Is.Zero);
            Assert.That(profiles.Profiles[0].Source.ModelVersion, Is.EqualTo("different-log-version"));
            Assert.That(CdhSourceText.Describe(profiles.Profiles[0]), Does.Contain("0.99"));
        }
        [Test] public void JsonlReferencesAreLocalAndEveryMatchingExecutionIsRetained()
        {
            var unrelated = Execution("9"); unrelated["ruleName"] = "Another"; unrelated["component"] = "Other.Model";
            var plural = Execution("2"); plural["ruleName"] = "CandidateRule";
            var model = Model(); var profiles = Read(model, Record(Cell(4), Execution(), plural, unrelated), Record(Cell(5), Execution()));
            Assert.That(profiles.Profiles.Count, Is.EqualTo(3)); Assert.That(profiles.Profiles.Select(p => p.Id).Distinct().Count(), Is.EqualTo(3));
            Assert.That(profiles.Profiles.Select(p => p.GetValue("Party.Value").Number), Is.EqualTo(new[] { 4d, 4d, 5d }));
            var array = new JArray(Record(Cell(4), Execution()), Record(Cell(5), Execution()));
            Assert.That(Fixtures.Require(CdhProfileJson.Read(array.ToString(), model)).Profiles.Count, Is.EqualTo(2));
        }
        [Test] public void ParametersOverrideCommonInputsAndPartitionFillsAbsentContext()
        {
            var e = Execution(); e["parameterPredictors"]["values"]["Party.Value"] = Cell(6);
            var model = Model(); Assert.That(Read(model, Record(Cell(4), e)).Profiles[0].GetValue("Party.Value").Number, Is.EqualTo(6));
            e["partition"] = new JObject { ["pyTreatment"] = "Fictional treatment" };
            Assert.That(Read(model, Record(Cell(4), e)).Profiles[0].Source.Treatment, Is.EqualTo("Fictional treatment"));
            e["partition"]["Party.Value"] = 2;
            Fixtures.Reject(CdhProfileJson.Read(Record(Cell(4), e).ToString(), model), "ConflictingContext");
        }
        [TestCase("", "AmbiguousCdhEmptyValue")] [TestCase("NaN", "InvalidCdhNumber")] [TestCase("1,5", "InvalidCdhNumber")]
        public void AmbiguousUsedInputFails(string value, string diagnostic) => Fixtures.Reject(CdhProfileJson.Read(Record(Cell(value), Execution()).ToString(), Model()), diagnostic);
        [Test] public void NullNumericOperandCannotSilentlyBecomeZero()
        {
            var model = Model(); var p = Read(model, Record(Cell(null), Execution())).Profiles[0];
            Fixtures.Reject(ModelEvaluator.Evaluate(model, p), "MissingSplitOperand");
        }
        [Test] public void MissingPredicatePreservesExplicitNullAndAbsentKinds()
        {
            var model = Model("Party.Value is Missing"); var p = Read(model, Record(Cell(null), Execution())).Profiles[0];
            Assert.That(p.GetValue("Party.Value").Kind, Is.EqualTo(ValueKind.Missing));
            Assert.That(Fixtures.Require(ModelEvaluator.Evaluate(model, p)).RawScore, Is.EqualTo(-.125));
            var r = Record(Cell(null), Execution()); r["pxCommonInputs"][0]["pxCommonInput"] = "{\"values\":{}}";
            p = Read(model, r).Profiles[0]; Assert.That(p.GetValue("Party.Value").Kind, Is.EqualTo(ValueKind.NotSupplied));
            Assert.That(Fixtures.Require(ModelEvaluator.Evaluate(model, p)).RawScore, Is.EqualTo(-.125));
            p = Read(model, Record(Cell("present", "SYMBOLIC"), Execution())).Profiles[0];
            Assert.That(Fixtures.Require(ModelEvaluator.Evaluate(model, p)).RawScore, Is.EqualTo(.875));
        }
        [Test] public void UnseenCategoryUsesExactMembershipWithoutInventingClosedDomain()
        {
            var model = Model("Party.Value in { Known }");
            var p = Read(model, Record(Cell("known", "SYMBOLIC"), Execution())).Profiles[0];
            Assert.That(Fixtures.Require(ModelEvaluator.Evaluate(model, p)).RawScore, Is.EqualTo(.875));
            Assert.That(ForestExplanation.Family("Party.Value"), Is.EqualTo("Party"));
        }
        [Test] public void BrokenReferencesDuplicateExecutionsAndDuplicateKeysReject()
        {
            var e = Execution(); e["pzCommonInputReferences"] = new JArray(2);
            Fixtures.Reject(CdhProfileJson.Read(Record(Cell(4), e).ToString(), Model()), "UnknownCommonReference");
            Fixtures.Reject(CdhProfileJson.Read(Record(Cell(4), Execution(), Execution()).ToString(), Model()), "DuplicateExecution");
            Fixtures.Reject(CdhProfileJson.Read("{\"pxCommonInputs\":[],\"pxCommonInputs\":[]}", Model()), "MalformedCdhJson");
        }
        [Test] public void LibraryReopenPreservesLongModelNameProfilesAndSource()
        {
            string folder = Path.Combine(Path.GetTempPath(), "agb-cdh-" + Guid.NewGuid().ToString("N"));
            try {
                var library = new ModelFileLibrary(folder); string json = Export(Split("Party.Value < 5"));
                string path = library.Save(false, FileName, json); var model = ModelFileLibrary.ParseModel(ModelFileLibrary.Read(path), path).Model;
                Assert.That(model.ExportInfo.Name, Is.EqualTo(Name));
                library.Save(true, "record.jsonl", Record(Cell(4), Execution()).ToString());
                var profiles = library.CompatibleProfiles(model, out var skipped); Assert.That(skipped, Is.Zero);
                Assert.That(profiles.Profiles.Count, Is.EqualTo(1)); Assert.That(profiles.Profiles[0].Source.RuleName, Is.EqualTo(Name));
                Assert.That(profiles.Profiles[0].WithValue("Party.Value", ProfileValue.FromNumber(5)).Source, Is.Null);
            } finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        }
        [Test] public void PackageMembershipReplacesCdhNameMatchingButKeepsSourceAndValidation()
        {
            string folder=Path.Combine(Path.GetTempPath(),"cdh-package-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
            try {
                var library=new ModelFileLibrary(folder);string zip=Path.Combine(folder,"pair.zip");
                var execution=Execution();execution["ruleName"]="DifferentRule";execution["component"]="Other.Component";
                string cdh=Record(Cell(4),execution).ToString();
                string export=Export(Split("Party.Value < 5"),Leaf(.125));
                Assert.Throws<InvalidDataException>(()=>ModelFileLibrary.ParseProfiles(cdh,ModelFileLibrary.ParseModel(export,"renamed.json").Model));
                using(var archive=ZipFile.Open(zip,ZipArchiveMode.Create)) {
                    using(var writer=new StreamWriter(archive.CreateEntry("renamed.json").Open()))writer.Write(export);
                    using(var writer=new StreamWriter(archive.CreateEntry("records.jsonl").Open()))writer.Write(cdh);
                }
                string path=library.ImportPackage(zip,"pair.zip");
                var model=ModelFileLibrary.ParseModel(ModelFileLibrary.Read(path),path).Model;
                var profile=library.CompatibleProfiles(model,out int skipped,path).Profiles.Single();
                Assert.That(skipped,Is.Zero);Assert.That(profile.Source.RuleName,Is.EqualTo("DifferentRule"));
                Assert.That(profile.Source.ModelVersion,Is.EqualTo("different-log-version"));
                Assert.That(Fixtures.Require(ModelEvaluator.Evaluate(model,profile)).RawScore,Is.EqualTo(-.125));
                Assert.Throws<InvalidDataException>(()=>ModelFileLibrary.ParseProfiles(Record(Cell("NaN"),execution).ToString(),model,true));
            } finally {Directory.Delete(folder,true);}
        }
    }
}
