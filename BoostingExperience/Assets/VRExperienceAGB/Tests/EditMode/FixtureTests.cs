using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using VRExperienceAGB.Domain;
using VRExperienceAGB.Import;

namespace VRExperienceAGB.Tests
{
    internal static class Fixtures
    {
        public static string Read(string name) => File.ReadAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "../../data/examples", name)));
        public static ModelDefinition Model() => Require(NormalizedModelJson.ReadModel(Read("demo-model.json")));
        public static ProfileSet Profiles(ModelDefinition model) => Require(NormalizedModelJson.ReadProfiles(Read("demo-profiles.json"), model));
        public static T Require<T>(Outcome<T> outcome) where T : class
        {
            Assert.That(outcome.IsSuccess, Is.True, string.Join("; ", outcome.Diagnostics.Select(d => d.Code + ": " + d.Message + " at " + d.Location)));
            return outcome.Value;
        }
        public static void Reject<T>(Outcome<T> outcome, string code) where T : class
        {
            Assert.That(outcome.IsSuccess, Is.False);
            Assert.That(outcome.Value, Is.Null, "A failed evaluation must not expose a partial or previous result.");
            Assert.That(outcome.Diagnostics.Select(d => d.Code), Does.Contain(code));
        }
    }

    public class FixtureTests
    {
        [TestCase("new-visitor")]
        [TestCase("returning-visitor")]
        [TestCase("engaged-visitor")]
        [TestCase("boundary-profile")]
        public void PreparedProfileMatchesIndependentReference(string profileId)
        {
            var model = Fixtures.Model(); var profile = Fixtures.Profiles(model).Profiles.Single(p => p.Id == profileId);
            var expected = JObject.Parse(Fixtures.Read("expected-predictions.json"))["cases"].Single(c => (string)c["profileId"] == profileId);
            var result = Fixtures.Require(ModelEvaluator.Evaluate(model, profile));
            Assert.That(result.Trees.Select(t => t.TreeId), Is.EqualTo(model.Trees.Select(t => t.Id)));
            Assert.That(result.Trees.Select(t => t.LeafId), Is.EqualTo(expected["leaves"].Values<string>()));
            Assert.That(result.BaseScore, Is.EqualTo(-1.5));
            Assert.That(result.RawScore, Is.EqualTo((double)expected["rawScore"]).Within(ModelEvaluator.ReferenceTolerance));
            Assert.That(result.Probability, Is.EqualTo((double)expected["probability"]).Within(ModelEvaluator.ReferenceTolerance));
            var running = -1.5;
            for (var i = 0; i < result.Trees.Count; i++)
            {
                var tree = result.Trees[i]; var contribution = (double)expected["contributions"][i];
                Assert.That(tree.VisitedNodeIds, Is.EqualTo(expected["paths"][i].Values<string>()));
                Assert.That(tree.Contribution, Is.EqualTo(contribution).Within(ModelEvaluator.ReferenceTolerance));
                Assert.That(tree.LeafScore, Is.EqualTo(contribution)); Assert.That(tree.Weight, Is.EqualTo(1));
                running += contribution;
                Assert.That(tree.RunningRawScore, Is.EqualTo(running).Within(ModelEvaluator.ReferenceTolerance));
                Assert.That(tree.Decisions.Select(d => d.NodeId), Is.EqualTo(tree.VisitedNodeIds.Take(tree.VisitedNodeIds.Count - 1)));
                Assert.That(tree.Decisions.Select(d => d.ChosenChildId), Is.EqualTo(tree.VisitedNodeIds.Skip(1)));
                foreach (var decision in tree.Decisions)
                {
                    Assert.That(decision.ObservedValue, Is.EqualTo(profile.GetValue(decision.FeatureId)));
                    var split = model.Trees[i].Nodes.OfType<SplitNode>().Single(n => n.Id == decision.NodeId);
                    Assert.That(decision.Condition, Is.SameAs(split.Condition));
                    Assert.That(decision.ChosenChildId, Is.EqualTo(decision.Matched ? split.TrueChild : split.FalseChild));
                }
            }
            Assert.That(result.IsSourceModelVerified, Is.False);
            Assert.That(result.OutputMeaning, Does.Contain("Synthetic"));
        }

        [Test]
        public void ModelAndProfileRoundTripPreservesSemanticsAndMissingKinds()
        {
            var model = Fixtures.Model(); var profiles = Fixtures.Profiles(model);
            var modelJson = Fixtures.Require(NormalizedModelJson.WriteModel(model));
            var restored = Fixtures.Require(NormalizedModelJson.ReadModel(modelJson));
            Assert.That(Fixtures.Require(NormalizedModelJson.WriteModel(restored)), Is.EqualTo(modelJson));
            var absent = profiles.Profiles[0].WithValue("loyaltyTier", ProfileValue.NotSupplied);
            var set = new ProfileSet(1, model.Id, new[] { absent, profiles.Profiles[1] });
            var json = Fixtures.Require(NormalizedModelJson.WriteProfiles(set, model));
            var reread = Fixtures.Require(NormalizedModelJson.ReadProfiles(json, restored));
            Assert.That(reread.Profiles[0].GetValue("loyaltyTier").Kind, Is.EqualTo(ValueKind.NotSupplied));
            foreach (var p in reread.Profiles)
            {
                var original = set.Profiles.Single(x => x.Id == p.Id);
                Assert.That(Fixtures.Require(ModelEvaluator.Evaluate(restored, p)).RawScore,
                    Is.EqualTo(Fixtures.Require(ModelEvaluator.Evaluate(model, original)).RawScore));
            }
            var explicitNull = Fixtures.Require(NormalizedModelJson.ReadProfiles(Fixtures.Read("demo-profiles.json"), restored));
            Assert.That(explicitNull.Profiles[0].GetValue("loyaltyTier").Kind, Is.EqualTo(ValueKind.Missing));
        }

        [Test]
        public void InputsCopiesAndResultsCannotMutateEarlierState()
        {
            var categories = new List<string> { "one", "two" };
            var feature = new FeatureDefinition("x", FeatureKind.Category, false, categories: categories);
            var condition = new SplitCondition(DecisionOperator.In, categories: categories);
            categories.Clear(); Assert.That(feature.Categories.Count, Is.EqualTo(2)); Assert.That(condition.Categories.Count, Is.EqualTo(2));
            var features = new List<FeatureDefinition> { feature };
            var nodes = new List<ModelNode> { new LeafNode("leaf", 2) };
            var tree = new ModelTree("tree", "leaf", 0.5, nodes); nodes.Clear();
            var trees = new List<ModelTree> { tree };
            var model = new ModelDefinition(1, "id", "Synthetic", "binary_logistic", "Response", -1, features, trees);
            features.Clear(); trees.Clear();
            var values = new Dictionary<string, ProfileValue> { ["x"] = ProfileValue.FromCategory("one") };
            var profile = new PreparedProfile(1, "id", "p", "Profile", values); values.Clear();
            var changed = profile.WithValue("x", ProfileValue.FromCategory("two"));
            Assert.That(profile.GetValue("x").Category, Is.EqualTo("one")); Assert.That(changed.GetValue("x").Category, Is.EqualTo("two"));
            var result = Fixtures.Require(ModelEvaluator.Evaluate(model, profile));
            Assert.Throws<NotSupportedException>(() => ((IList<ModelTree>)model.Trees).Clear());
            Assert.Throws<NotSupportedException>(() => ((IDictionary<string, ProfileValue>)profile.Values).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<TreeEvaluation>)result.Trees).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<string>)result.Trees[0].VisitedNodeIds).Clear());
            Assert.That(result.RawScore, Is.Zero);
        }

        [Test]
        public void RuntimeAssembliesDoNotReferenceUnityOrXr()
        {
            foreach (var assembly in new[] { typeof(ModelEvaluator).Assembly, typeof(NormalizedModelJson).Assembly })
                Assert.That(assembly.GetReferencedAssemblies().Select(a => a.Name).Where(n => n.StartsWith("Unity") || n.StartsWith("Oculus") || n.StartsWith("Meta")), Is.Empty);
            Assert.That(typeof(ModelEvaluator).Assembly.GetReferencedAssemblies().Select(a => a.Name), Does.Not.Contain("Newtonsoft.Json"));
        }
    }
}
