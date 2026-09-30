using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using VRExperienceAGB.Application;
using VRExperienceAGB.Domain;
using VRExperienceAGB.Import;

namespace VRExperienceAGB.Tests
{
    public class AgbStructurePreviewTests
    {
        private static JObject Leaf(double score) => new JObject { ["score"] = score, ["gain"] = 0, ["sampleCount"] = 5 };
        private static JObject Split(string condition) => new JObject { ["score"] = 999, ["gain"] = 4, ["sampleCount"] = 10,
            ["split"] = condition, ["left"] = Leaf(-.25), ["right"] = Leaf(.75) };
        private static JObject Export(params JObject[] trees) => new JObject { ["type"] = "AdaptiveBoostScoringModel", ["algorithm"] = "GRADIENT_BOOST",
            ["model"] = new JObject { ["booster"] = new JObject { ["trees"] = new JArray(trees.Cast<object>()) } } };
        [Test]
        public void CompleteStructureRetainsOrderAddressesConditionsAndAuditMetadata()
        {
            var json = Export(Split("Audience.Score < 5.017708e-4"), Split("Audience.Bank in { Cedar Community Bank, Riverbend Credit }"), Split("Audience.Flag is Missing"), Leaf(.125));
            var imported = Fixtures.Require(AgbStructurePreview.Read(json.ToString(), "Fictional export"));
            Assert.That(imported.Model.Trees.Select(t => t.Id), Is.EqualTo(new[] { "tree[0]", "tree[1]", "tree[2]", "tree[3]" }));
            Assert.That(imported.Model.Trees.Sum(t => t.Nodes.Count), Is.EqualTo(10));
            Assert.That(imported.Metadata.Count, Is.EqualTo(10));
            Assert.That(imported.Metadata["tree[0]/root"].SplitText, Is.EqualTo("Audience.Score < 5.017708e-4"));
            Assert.That(imported.Metadata["tree[0]/root"].Score, Is.EqualTo(999));
            Assert.That(imported.Metadata["tree[0]/root"].SampleCount, Is.EqualTo(10));
            Assert.That(((SplitNode)imported.Model.Trees[0].Nodes[0]).Condition.Threshold, Is.EqualTo(5.017708e-4));
            Assert.That(imported.Model.Features.Single(f => f.Id == "Audience.Flag").Kind, Is.EqualTo(FeatureKind.Unknown));
            Assert.That(imported.Sha256.Length, Is.EqualTo(64));
            var ensemble = Fixtures.Require(EnsembleSession.Create(imported.Model));
            foreach (var tree in ensemble.Trees.Where(t => !t.State.AtLeaf))
            {
                tree.EnterTree(); var state = tree.State;
                Assert.That(tree.ChooseBranch(state.Revision, state.NodeId, true).Accepted, Is.True);
                Assert.That(tree.CompleteMove(tree.State.PendingDecision.EventId).Accepted, Is.True);
            }
            Assert.That(ensemble.CompletedCount, Is.EqualTo(4));
            Assert.That(ensemble.RouteTotal, Is.EqualTo(-.625), "Only reached leaves contribute; internal scores and gain never do.");
            Assert.That(ensemble.Consistency.Status, Is.EqualTo(RouteConsistency.NotVerified));
            Assert.That(ensemble.Evaluation, Is.Null);
            var profile = new PreparedProfile(1, imported.Model.Id, "fixture", "Fictional", new Dictionary<string, ProfileValue>());
            Fixtures.Reject(ModelEvaluator.Evaluate(imported.Model, profile), "ProfileEvaluationUnavailable");
            Fixtures.Reject(NormalizedModelJson.WriteModel(imported.Model), "PreviewNotInterchange");
        }
        [Test]
        public void AnUnsplitExportIsAValidManualLeafWithoutInventedPredictors()
        {
            var imported = Fixtures.Require(AgbStructurePreview.Read(Export(Leaf(.125)).ToString(), "Unsplit fictional export"));
            Assert.That(imported.Model.Features.Count, Is.Zero);
            var ensemble = Fixtures.Require(EnsembleSession.Create(imported.Model));
            Assert.That(ensemble.CompletedCount, Is.EqualTo(1));
            Assert.That(ensemble.RouteTotal, Is.EqualTo(.125));
            Assert.That(ensemble.Evaluation, Is.Null);
        }
        [TestCase("Audience.Score <= 1")]
        [TestCase("Audience.Bank in { 'Cedar, Bank' }")]
        [TestCase("Audience.Bank in { A, A }")]
        public void UnsupportedConditionsRejectWholeExport(string condition)
        { Assert.That(AgbStructurePreview.Read(Export(Split("Audience.Score < 1"), Split(condition)).ToString(), "Fixture").IsSuccess, Is.False); }
        [Test]
        public void MixedFeatureTypesRejectWholeExport()
        { Assert.That(AgbStructurePreview.Read(Export(Split("Audience.X < 1"), Split("Audience.X in { A }")).ToString(), "Fixture").IsSuccess, Is.False); }
        [Test]
        public void MissingChildRejectsWholeExport()
        { var node = Split("Audience.Score < 1"); node.Remove("right"); Assert.That(AgbStructurePreview.Read(Export(node).ToString(), "Fixture").IsSuccess, Is.False); }
        [Test]
        public void LeafCannotHideChildrenOrNonzeroGain()
        { var node = Leaf(1); node["left"] = Leaf(2); Assert.That(AgbStructurePreview.Read(Export(node).ToString(), "Fixture").IsSuccess, Is.False); node.Remove("left"); node["gain"] = 1; Assert.That(AgbStructurePreview.Read(Export(node).ToString(), "Fixture").IsSuccess, Is.False); }
        [TestCase("null")]
        [TestCase("{\"model\":3,\"type\":\"AdaptiveBoostScoringModel\",\"algorithm\":\"GRADIENT_BOOST\"}")]
        [TestCase("{\"type\":\"A\",\"type\":\"B\"}")]
        public void MalformedShapesAndDuplicateFieldsReturnDiagnostics(string json)
        { Assert.That(AgbStructurePreview.Read(json, "Fixture").IsSuccess, Is.False); }
        [Test]
        public void TrailingDocumentsAndNonfiniteThresholdsAreRejected()
        { Assert.That(AgbStructurePreview.Read(Export(Leaf(1)) + " {}", "Fixture").IsSuccess, Is.False); Assert.That(AgbStructurePreview.Read(Export(Split("Audience.Score < 1e999")).ToString(), "Fixture").IsSuccess, Is.False); }
    }
}
