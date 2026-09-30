using System;
using System.Linq;
using NUnit.Framework;
using VRExperienceAGB.Application;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Tests
{
    public class ManualConsistencyTests
    {
        private static ModelDefinition Model(FeatureDefinition feature) => new ModelDefinition(1, "constraints", "Synthetic", "binary_logistic", "Outcome", 0,
            new[] { feature }, new[] { new ModelTree("t", "leaf", 1, new[] { new LeafNode("leaf", 0) }) });
        private static ManualConstraint Choice(int index, SplitCondition condition, bool branch) => new ManualConstraint("tree-" + index, "root", "x", condition, branch);
        private static SplitCondition Lt(double n) => new SplitCondition(DecisionOperator.LessThan, n);
        private static SplitCondition In(params string[] values) => new SplitCondition(DecisionOperator.In, categories: values);
        [TestCase(5, 5, false)] [TestCase(5, 4.9, true)] [TestCase(5, 6, false)]
        public void StrictUpperAndInclusiveLowerBoundIntersectCorrectly(double upper, double lower, bool possible)
        {
            var m = Model(new FeatureDefinition("x", FeatureKind.Number, false));
            var report = ManualRouteConsistency.Check(m, new[] { Choice(1, Lt(upper), true), Choice(2, Lt(lower), false) });
            Assert.That(report.Status, Is.EqualTo(possible ? RouteConsistency.Consistent : RouteConsistency.Contradictory));
            if (!possible) { Assert.That(report.Conflicts.Count, Is.EqualTo(2)); Assert.That(report.Message, Does.Contain("tree-1/root").And.Contain("tree-2/root").And.Contain("x")); }
        }
        [TestCase(false, true)] [TestCase(true, false)]
        public void IntegerIntervalsRequireAnAvailableInteger(bool integer, bool possible)
        {
            var m = Model(new FeatureDefinition("x", FeatureKind.Number, false, integer: integer));
            var report = ManualRouteConsistency.Check(m, new[] { Choice(1, Lt(.9), true), Choice(2, Lt(.1), false) });
            Assert.That(report.Status, Is.EqualTo(possible ? RouteConsistency.Consistent : RouteConsistency.Contradictory));
        }
        [Test]
        public void LargeIntegerStrictBoundaryDoesNotRoundIntoAnAllowedValue()
        {
            var m = Model(new FeatureDefinition("x", FeatureKind.Number, false, integer: true));
            Assert.That(ManualRouteConsistency.Check(m, new[] { Choice(1, Lt(1e16), true), Choice(2, Lt(1e16), false) }).Status,
                Is.EqualTo(RouteConsistency.Contradictory));
        }
        [Test]
        public void DeclaredMaximumIsInclusiveAndStrictThresholdStillExcludesEquality()
        {
            var m = Model(new FeatureDefinition("x", FeatureKind.Number, false, minimum: 5, maximum: 5));
            Assert.That(ManualRouteConsistency.Check(m, new[] { Choice(1, Lt(5), false) }).Status, Is.EqualTo(RouteConsistency.Consistent));
            Assert.That(ManualRouteConsistency.Check(m, new[] { Choice(1, Lt(5), true) }).Status, Is.EqualTo(RouteConsistency.Contradictory));
        }
        [Test]
        public void CategoricalComplementsUseTheDeclaredDomain()
        {
            var m = Model(new FeatureDefinition("x", FeatureKind.Category, false, categories: new[] { "a", "b", "c" }));
            var choices = new[] { Choice(1, In("a", "b"), false), Choice(2, In("b", "c"), true) };
            Assert.That(ManualRouteConsistency.Check(m, choices).Status, Is.EqualTo(RouteConsistency.Consistent));
            Assert.That(ManualRouteConsistency.Check(m, choices.Concat(new[] { Choice(3, In("c"), false) })).Status, Is.EqualTo(RouteConsistency.Contradictory));
            Assert.That(ManualRouteConsistency.Check(m, new[] { Choice(1, In("A"), true) }).Status, Is.EqualTo(RouteConsistency.Contradictory));
        }
        [TestCase(true)] [TestCase(false)]
        public void MissingCannotAlsoBePresentOrCompared(bool numeric)
        {
            var m = Model(new FeatureDefinition("x", numeric ? FeatureKind.Number : FeatureKind.Category, true, categories: numeric ? null : new[] { "a", "b" }));
            var missing = new SplitCondition(DecisionOperator.IsMissing);
            Assert.That(ManualRouteConsistency.Check(m, new[] { Choice(1, missing, true) }).Status, Is.EqualTo(RouteConsistency.Consistent));
            Assert.That(ManualRouteConsistency.Check(m, new[] { Choice(1, missing, true), Choice(2, missing, false) }).Status, Is.EqualTo(RouteConsistency.Contradictory));
            Assert.That(ManualRouteConsistency.Check(m, new[] { Choice(1, missing, true), Choice(2, numeric ? Lt(5) : In("a"), false) }).Status, Is.EqualTo(RouteConsistency.Contradictory));
        }
        [Test]
        public void UnsupportedConstraintAndUnknownFeatureNeverClaimConsistency()
        {
            var m = Model(new FeatureDefinition("x", FeatureKind.Number, true));
            Assert.That(ManualRouteConsistency.Check(m, new[] { Choice(1, new SplitCondition((DecisionOperator)99), true) }).Status, Is.EqualTo(RouteConsistency.NotVerified));
            Assert.That(ManualRouteConsistency.Check(m, new[] { new ManualConstraint("t", "n", "unknown", Lt(5), true) }).Status, Is.EqualTo(RouteConsistency.NotVerified));
        }
        [Test]
        public void RevisionClearsCrossTreeContradictionWithoutForcingBranches()
        {
            var f = new FeatureDefinition("x", FeatureKind.Number, false);
            var trees = Enumerable.Range(0, 2).Select(i => new ModelTree("t" + i, "root", 1, new ModelNode[] {
                new SplitNode("root", "x", Lt(5), "yes", "no"), new LeafNode("yes", 1), new LeafNode("no", -1) }));
            var model = new ModelDefinition(1, "manual", "Synthetic", "binary_logistic", "Outcome", 0, new[] { f }, trees);
            var e = Fixtures.Require(EnsembleSession.Create(model));
            for (int i = 0; i < 2; i++)
            {
                e.Select(i); var s = e.Current.State;
                e.Current.ChooseBranch(s.Revision, s.NodeId, i == 0); e.Current.CompleteMove(e.Current.State.PendingDecision.EventId);
            }
            Assert.That(e.Consistency.Status, Is.EqualTo(RouteConsistency.Contradictory));
            Assert.That(e.Complete, Is.True); Assert.That(e.Evaluation, Is.Null); Assert.That(e.RouteTotal, Is.Zero);
            Assert.That(e.ReviseConflict(), Is.True);
            Assert.That(e.Index, Is.Zero); Assert.That(e.CompletedCount, Is.Zero);
            Assert.That(e.Current.State.NodeId, Is.EqualTo("root"));
            Assert.That(e.Consistency.Status, Is.EqualTo(RouteConsistency.Consistent));
        }
    }
}
