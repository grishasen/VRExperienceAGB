using System;
using System.Linq;
using NUnit.Framework;
using VRExperienceAGB.Application;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Tests
{
    public class ProfileComparisonTests
    {
        private const string Clicks = "IH.Web.Inbound.Clicked.pyHistoricalOutcomeCount";
        private static ProfileComparisonSession Pair()
        {
            var m = Fixtures.Model(); return Fixtures.Require(ProfileComparisonSession.Create(m, Fixtures.Profiles(m).Profiles[0]));
        }
        [Test]
        public void IdenticalCopyHasIndependentIdentityAndZeroDifferences()
        {
            var pair = Pair();
            Assert.That(pair.A.Profile.Id, Is.Not.EqualTo(pair.B.Profile.Id));
            Assert.That(pair.A.Current, Is.Not.SameAs(pair.B.Current));
            Assert.That(pair.ChangedFeatures, Is.Empty);
            Assert.That(pair.Trees.All(t => !t.PathsDiffer && t.ContributionDelta == 0), Is.True);
            Assert.That(pair.RawDelta, Is.Zero); Assert.That(pair.ProbabilityPointDelta, Is.Zero);
        }
        [Test]
        public void ThresholdEditHasKnownDeltaAndFirstDivergenceWithoutChangingA()
        {
            var pair = Pair(); var a = pair.A.Evaluation;
            pair.BeginEdit(); pair.StageEdit(Clicks, ProfileValue.FromNumber(5));
            Fixtures.Require(pair.ApplyEdit());
            Assert.That(pair.A.Evaluation, Is.SameAs(a));
            Assert.That(pair.A.Profile.GetValue(Clicks).Number, Is.Zero);
            Assert.That(pair.B.Evaluation.RawScore, Is.EqualTo(-3.2).Within(1e-12));
            Assert.That(pair.RawDelta, Is.EqualTo(.9).Within(1e-12));
            Assert.That(pair.ProbabilityPointDelta, Is.EqualTo((1/(1+Math.Exp(3.2))-1/(1+Math.Exp(4.1)))*100).Within(1e-12));
            Assert.That(pair.Trees[0].DivergenceA.FeatureId, Is.EqualTo(Clicks));
            Assert.That(pair.Trees[0].DivergenceA.Matched, Is.True);
            Assert.That(pair.Trees[0].DivergenceB.Matched, Is.False);
            Assert.That(pair.Trees.Count(t => t.PathsDiffer), Is.EqualTo(1));
            Assert.That(pair.Trees.Sum(t => t.ContributionDelta), Is.EqualTo(pair.RawDelta).Within(1e-12));
        }
        [Test]
        public void CancelAndInvalidEditsPreserveAcceptedPairAndPendingMovement()
        {
            var pair = Pair(); var before = pair.B.Evaluation;
            var s = pair.B.Current; s.StepProfile(s.State.Revision, s.State.NodeId);
            var pending = s.State.PendingDecision;
            pair.BeginEdit(); pair.StageEdit(Clicks, ProfileValue.FromNumber(-1));
            Fixtures.Reject(pair.ApplyEdit(), "ValueOutOfRange");
            Assert.That(pair.B.Evaluation, Is.SameAs(before));
            Assert.That(pair.B.Current.State.PendingDecision, Is.SameAs(pending));
            pair.CancelEdit(); Assert.That(pair.Draft, Is.Null);
            Assert.That(s.CompleteMove(pending.EventId).Accepted, Is.True);
        }
        [Test]
        public void MissingAndNotSuppliedRemainDistinctAndNoIncompleteEditIsAccepted()
        {
            var pair = Pair(); pair.BeginEdit(); pair.StageEdit("Customer.LoyaltyTier", ProfileValue.NotSupplied);
            Fixtures.Reject(pair.ApplyEdit(), "IncompleteEdit");
            pair.StageEdit("Customer.LoyaltyTier", ProfileValue.Missing); Fixtures.Require(pair.ApplyEdit());
            Assert.That(pair.ChangedFeatures, Is.Empty);
        }
        [Test]
        public void SwitchingPreservesAcceptedRoutesAndRetiresUnfinishedTokens()
        {
            var pair = Pair(); var a = pair.A.Current;
            a.StepProfile(a.State.Revision, a.State.NodeId); a.CompleteMove(a.State.PendingDecision.EventId);
            string node = a.State.NodeId;
            a.StepProfile(a.State.Revision, a.State.NodeId); long unfinished = a.State.PendingDecision.EventId;
            pair.Show(true, 0); Assert.That(a.CompleteMove(unfinished).Accepted, Is.False);
            Assert.That(pair.B.Current.State.NodeId, Is.EqualTo(pair.B.Current.Tree.RootId));
            pair.Show(false, 0); Assert.That(pair.Active, Is.SameAs(pair.A));
            Assert.That(pair.A.Current.State.NodeId, Is.EqualTo(node));
        }
        [Test]
        public void FullLargeEnsembleAndEqualScoreDifferentPathsAreBothReported()
        {
            var source = Fixtures.Model(); var profile = Fixtures.Profiles(source).Profiles[0];
            var trees = Enumerable.Range(0, 128).Select(i => new ModelTree("tree-" + i, "root", 2,
                new ModelNode[] { new SplitNode("root", Clicks, new SplitCondition(DecisionOperator.LessThan, 5), "yes", "no"),
                    new LeafNode("yes", .25), new LeafNode("no", i == 127 ? .25 : .75) }));
            var model = new ModelDefinition(1, source.Id, source.Provenance, source.Objective, source.OutcomeLabel, 1.25, source.Features, trees);
            var pair = Fixtures.Require(ProfileComparisonSession.Create(model, profile, profile.WithValue(Clicks, ProfileValue.FromNumber(5))));
            Assert.That(pair.Trees.Count, Is.EqualTo(128)); Assert.That(pair.Trees.Count(t => t.PathsDiffer), Is.EqualTo(128));
            Assert.That(pair.Trees[127].ContributionDelta, Is.Zero);
            Assert.That(pair.RawDelta, Is.EqualTo(127)); Assert.That(pair.A.Evaluation.RawScore, Is.EqualTo(65.25));
        }
        [Test]
        public void UnverifiedStructureAndForeignProfilesCannotStartComparison()
        {
            var source = Fixtures.Model(); var p = Fixtures.Profiles(source).Profiles[0];
            var preview = new ModelDefinition(1, source.Id, source.Provenance, source.Objective, source.OutcomeLabel, 0, source.Features, source.Trees, true);
            Assert.That(ProfileComparisonSession.Create(preview, p).IsSuccess, Is.False);
            var foreign = new PreparedProfile(1, "another-model", "foreign", "Foreign", p.Values);
            Assert.That(ProfileComparisonSession.Create(source, p, foreign).IsSuccess, Is.False);
        }
        [Test]
        public void ChangingBRetiresItsOldAnimationAndLeavesAProgressIntact()
        {
            var pair = Pair(); var a = pair.A.Current;
            a.StepProfile(a.State.Revision, a.State.NodeId); a.CompleteMove(a.State.PendingDecision.EventId);
            string node = a.State.NodeId; var oldB = pair.B.Current;
            oldB.StepProfile(oldB.State.Revision, oldB.State.NodeId); long token = oldB.State.PendingDecision.EventId;
            pair.BeginEdit(); pair.StageEdit(Clicks, ProfileValue.FromNumber(5)); Fixtures.Require(pair.ApplyEdit());
            Assert.That(oldB.CompleteMove(token).Accepted, Is.False);
            Assert.That(pair.A.Current.State.NodeId, Is.EqualTo(node)); Assert.That(pair.B.CompletedCount, Is.Zero);
        }
    }
}
