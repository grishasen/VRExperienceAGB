using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Tests
{
    public class ScoringTests
    {
        private static ModelDefinition Model(IEnumerable<ModelTree> trees, double baseline = 0, FeatureDefinition feature = null) =>
            new ModelDefinition(1, "test", "Synthetic test", "binary_logistic", "Response", baseline,
                new[] { feature ?? new FeatureDefinition("x", FeatureKind.Number, true) }, trees);
        private static PreparedProfile Profile(ProfileValue value) => new PreparedProfile(1, "test", "profile", "Profile",
            new[] { new KeyValuePair<string, ProfileValue>("x", value) });
        private static ModelTree Constant(string id, double score, double weight = 1) => new ModelTree(id, "leaf", weight, new[] { new LeafNode("leaf", score) });
        private static ModelTree Split(DecisionOperator op, double? threshold = null) => new ModelTree("tree", "root", 1, new ModelNode[] {
            new SplitNode("root", "x", new SplitCondition(op, threshold), "yes", "no"), new LeafNode("yes", 1), new LeafNode("no", -1) });

        [TestCase(4, true)] [TestCase(5, false)] [TestCase(6, false)]
        public void NumericLessThanIsStrict(double value, bool expected)
        {
            var result = Fixtures.Require(ModelEvaluator.Evaluate(Model(new[] { Split(DecisionOperator.LessThan, 5) }), Profile(ProfileValue.FromNumber(value))));
            Assert.That(result.Trees[0].Decisions[0].Matched, Is.EqualTo(expected));
        }

        [TestCase(99.999, "t1-active")] [TestCase(100, "t1-engaged")] [TestCase(100.001, "t1-engaged")]
        public void RelationshipThresholdIsStrict(double value, string leaf)
        {
            var model = Fixtures.Model(); var profile = Fixtures.Profiles(model).Profiles[3].WithValue("Customer.RelationshipMonths", ProfileValue.FromNumber(value));
            Assert.That(Fixtures.Require(ModelEvaluator.Evaluate(model, profile)).Trees[0].LeafId, Is.EqualTo(leaf));
        }

        [TestCase("Savings Card", true)] [TestCase("Service Banner", true)] [TestCase("Welcome Banner", false)] [TestCase("Discovery Card", false)]
        public void CategoriesUseExactDomainMembership(string value, bool expected)
        {
            var model = Fixtures.Model(); var profile = Fixtures.Profiles(model).Profiles[0].WithValue("pyTreatment", ProfileValue.FromCategory(value));
            Assert.That(Fixtures.Require(ModelEvaluator.Evaluate(model, profile)).Trees[1].Decisions[0].Matched, Is.EqualTo(expected));
        }

        [TestCase(true)] [TestCase(false)]
        public void MissingTraceDistinguishesAbsentFromExplicitNull(bool absent)
        {
            var value = absent ? ProfileValue.NotSupplied : ProfileValue.Missing;
            var result = Fixtures.Require(ModelEvaluator.Evaluate(Model(new[] { Split(DecisionOperator.IsMissing) }), Profile(value)));
            Assert.That(result.Trees[0].LeafId, Is.EqualTo("yes"));
            Assert.That(result.Trees[0].Decisions[0].ObservedValue.Kind, Is.EqualTo(value.Kind));
        }

        [Test]
        public void MissingNumericOperandFailsUnlessGuarded()
        {
            Fixtures.Reject(ModelEvaluator.Evaluate(Model(new[] { Split(DecisionOperator.LessThan, 5) }), Profile(ProfileValue.Missing)), "MissingSplitOperand");
            var guarded = new ModelTree("guarded", "guard", 1, new ModelNode[] {
                new SplitNode("guard", "x", new SplitCondition(DecisionOperator.IsMissing), "missing", "number"),
                new SplitNode("number", "x", new SplitCondition(DecisionOperator.LessThan, 5), "yes", "no"),
                new LeafNode("missing", -2), new LeafNode("yes", 1), new LeafNode("no", -1) });
            Assert.That(Fixtures.Require(ModelEvaluator.Evaluate(Model(new[] { guarded }), Profile(ProfileValue.Missing))).RawScore, Is.EqualTo(-2));
            Assert.That(Fixtures.Require(ModelEvaluator.Evaluate(Model(new[] { guarded }), Profile(ProfileValue.FromNumber(0)))).RawScore, Is.EqualTo(1));
        }

        [Test]
        public void NonUnitZeroAndNegativeWeightsRetainSeparateContributions()
        {
            var model = Model(new[] { Constant("a", 2, 0.5), Constant("b", -4, 0), Constant("c", 3, -2) }, -1);
            var result = Fixtures.Require(ModelEvaluator.Evaluate(model, Profile(ProfileValue.Missing)));
            Assert.That(result.Trees.Select(t => t.LeafScore), Is.EqualTo(new[] { 2d, -4, 3 }));
            Assert.That(result.Trees.Select(t => t.Weight), Is.EqualTo(new[] { 0.5, 0, -2 }));
            Assert.That(result.Trees.Select(t => t.Contribution), Is.EqualTo(new[] { 1d, 0, -6 }));
            Assert.That(result.Trees.Select(t => t.RunningRawScore), Is.EqualTo(new[] { 0d, 0, -6 }));
            Assert.That(result.RawScore, Is.EqualTo(-6));
            Assert.That(result.Probability, Is.EqualTo(0.0024726231566347743).Within(ModelEvaluator.ReferenceTolerance));
        }

        [Test]
        public void EveryTreeUsesOriginalProfileRegardlessOfAccumulatedScore()
        {
            var split = Split(DecisionOperator.LessThan, 5);
            var result = Fixtures.Require(ModelEvaluator.Evaluate(Model(new[] { Constant("first", 1000), split }), Profile(ProfileValue.FromNumber(2))));
            Assert.That(result.Trees[1].LeafId, Is.EqualTo("yes")); Assert.That(result.RawScore, Is.EqualTo(1001));
            Assert.That(result.Trees[1].Decisions[0].ObservedValue.Number, Is.EqualTo(2));
        }

        [Test]
        public void LargeEnsembleIsNeverTruncatedToPresentationBudget()
        {
            var model = Model(Enumerable.Range(0, 257).Select(i => Constant("tree-" + i, 0.25)), -64);
            var result = Fixtures.Require(ModelEvaluator.Evaluate(model, Profile(ProfileValue.Missing)));
            Assert.That(result.Trees.Count, Is.EqualTo(257)); Assert.That(result.Trees.Last().TreeId, Is.EqualTo("tree-256"));
            Assert.That(result.RawScore, Is.EqualTo(0.25));
            Assert.That(result.Probability, Is.EqualTo(0.5621765008857981).Within(ModelEvaluator.ReferenceTolerance));
        }

        [TestCase(1000, 1)] [TestCase(-1000, 0)] [TestCase(0, 0.5)]
        public void StableSigmoidDoesNotOverflow(double raw, double probability)
        {
            var result = Fixtures.Require(ModelEvaluator.Evaluate(Model(new[] { Constant("tree", raw) }), Profile(ProfileValue.Missing)));
            Assert.That(result.Probability, Is.EqualTo(probability)); Assert.That(ModelValidator.IsFinite(result.Probability), Is.True);
        }

        [TestCase(true)] [TestCase(false)]
        public void ArithmeticOverflowInvalidatesWholeEnsemble(bool product)
        {
            var model = Model(new[] { Constant("first", 1), Constant("overflow", 1e308, product ? 2 : 1) }, product ? 0 : 1e308);
            Fixtures.Reject(ModelEvaluator.Evaluate(model, Profile(ProfileValue.Missing)), "ScoreOverflow");
        }

        [Test]
        public void DeepTreeValidationAndTraversalDoNotUseRecursiveStack()
        {
            const int depth = 4096; var nodes = new List<ModelNode>();
            for (var i = 0; i < depth; i++)
            {
                nodes.Add(new SplitNode("n" + i, "x", new SplitCondition(DecisionOperator.LessThan, 0), "n" + (i + 1), "f" + i));
                nodes.Add(new LeafNode("f" + i, -1));
            }
            nodes.Add(new LeafNode("n" + depth, 2));
            var result = Fixtures.Require(ModelEvaluator.Evaluate(Model(new[] { new ModelTree("deep", "n0", 1, nodes) }), Profile(ProfileValue.FromNumber(-1))));
            Assert.That(result.Trees[0].VisitedNodeIds.Count, Is.EqualTo(depth + 1)); Assert.That(result.RawScore, Is.EqualTo(2));
        }

        [Test]
        public void DisplayNamesDoNotReplaceStableFeatureIds()
        {
            var model = new ModelDefinition(1, "test", "Synthetic", "binary_logistic", "Response", 0,
                new[] { new FeatureDefinition("x", FeatureKind.Number, false, "Same label"), new FeatureDefinition("X", FeatureKind.Number, false, "Same label") },
                new[] { Split(DecisionOperator.LessThan, 5) });
            var profile = Profile(ProfileValue.FromNumber(2)).WithValue("X", ProfileValue.FromNumber(100));
            Assert.That(Fixtures.Require(ModelEvaluator.Evaluate(model, profile)).Trees[0].LeafId, Is.EqualTo("yes"));
        }
    }
}
