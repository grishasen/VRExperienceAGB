using System;
using System.Linq;
using NUnit.Framework;
using VRExperienceAGB.Application;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Tests
{
    public class M4EnsembleTests
    {
        private static void Finish(TreeSession session)
        {
            session.SetPaused(false); session.EnterTree();
            while (!session.State.AtLeaf)
            {
                var state = session.State;
                Assert.That(session.StepProfile(state.Revision, state.NodeId).Accepted, Is.True);
                Assert.That(session.CompleteMove(session.State.PendingDecision.EventId).Accepted, Is.True);
            }
        }
        private static EnsembleSession Prepared(int index = 0)
        {
            var model = Fixtures.Model(); return Fixtures.Require(EnsembleSession.Create(model, Fixtures.Profiles(model).Profiles[index]));
        }
        [TestCase(0, -4.1)] [TestCase(1, -2.6)] [TestCase(2, -2.0)] [TestCase(3, -2.9)]
        public void ShortAndDetailedToursReconcileEveryOrderedContribution(int profile, double expected)
        {
            var e = Prepared(profile); var result = e.Evaluation;
            e.SetDetail(TourDetail.Short); Finish(e.Current);
            Assert.That(e.ExplainRemainingGroup(), Is.True);
            Assert.That(e.Complete, Is.True);
            Assert.That(e.GroupedRows.Select(r => r.TreeId), Is.EqualTo(new[] { "tree-02", "tree-03" }));
            Assert.That(e.RouteTotal, Is.EqualTo(expected).Within(ModelEvaluator.ReferenceTolerance));
            Assert.That(e.Ledger.Sum(r => r.PresentedContribution) + e.Model.BaseScore, Is.EqualTo(result.RawScore).Within(1e-12));
            e.SetDetail(TourDetail.Detailed);
            Assert.That(e.CompletedCount, Is.EqualTo(1));
            for (int i = 1; i < e.Model.Trees.Count; i++) { e.Select(i); Finish(e.Current); }
            Assert.That(e.Evaluation, Is.SameAs(result));
            Assert.That(e.RouteTotal, Is.EqualTo(expected).Within(1e-12));
            e.SetDetail(TourDetail.Short); Finish(e.Current); Assert.That(e.ExplainRemainingGroup(), Is.True);
            Assert.That(e.RouteTotal, Is.EqualTo(expected).Within(1e-12));
        }
        [Test]
        public void LargeWeightedEnsembleHasNoTourOrGeometryCap()
        {
            var source = Fixtures.Model();
            // Independent sum: 128 alternating +0.5/-0.25 weighted leaves => 16, plus baseline 1.25.
            var trees = Enumerable.Range(0, 128).Select(i => new ModelTree("large-" + i, "root", 2,
                new ModelNode[] { new SplitNode("root", "IH.Web.Inbound.Clicked.pyHistoricalOutcomeCount", new SplitCondition(DecisionOperator.LessThan, 5), "yes", "no"),
                    new LeafNode("yes", i % 2 == 0 ? .25 : -.125), new LeafNode("no", 0) }));
            var model = new ModelDefinition(1, source.Id, source.Provenance, source.Objective, source.OutcomeLabel, 1.25, source.Features, trees);
            var e = Fixtures.Require(EnsembleSession.Create(model, Fixtures.Profiles(source).Profiles[0]));
            Assert.That(e.Evaluation.RawScore, Is.EqualTo(17.25));
            e.SetDetail(TourDetail.Short); Finish(e.Current); Assert.That(e.ExplainRemainingGroup(), Is.True);
            Assert.That(e.GroupedRows.Count, Is.EqualTo(127)); Assert.That(e.Ledger.Count, Is.EqualTo(128));
            Assert.That(e.RouteTotal, Is.EqualTo(17.25)); e.Select(127);
            Assert.That(e.RouteTotal, Is.EqualTo(17.25));
            e.SetDetail(TourDetail.Detailed); Assert.That(e.RouteTotal, Is.EqualTo(1.75));
            Assert.That(e.Evaluation.RawScore, Is.EqualTo(17.25));
        }
        [Test]
        public void DraftCancelAndInvalidApplyKeepAcceptedResultAndPendingMove()
        {
            var e = Prepared(); var state = e.Current.State;
            e.Current.StepProfile(state.Revision, state.NodeId); var pending = e.Current.State.PendingDecision;
            var result = e.Evaluation; var accepted = e.Profile; string id = e.EvaluationId;
            e.BeginEdit(); e.StageEdit("IH.Web.Inbound.Clicked.pyHistoricalOutcomeCount", ProfileValue.FromNumber(-1));
            Assert.That(e.ApplyEdit().IsSuccess, Is.False);
            Assert.That(e.Evaluation, Is.SameAs(result)); Assert.That(e.EvaluationId, Is.EqualTo(id));
            Assert.That(e.Profile, Is.SameAs(accepted));
            Assert.That(e.Current.State.PendingDecision, Is.SameAs(pending));
            e.CancelEdit(); Assert.That(e.Draft, Is.Null);
            Assert.That(e.Current.CompleteMove(pending.EventId).Accepted, Is.True);
        }
        [Test]
        public void ExplicitMissingIsDifferentFromAnUnfinishedField()
        {
            var e = Prepared(1); e.BeginEdit(); e.StageEdit("Customer.LoyaltyTier", ProfileValue.NotSupplied);
            Fixtures.Reject(e.ApplyEdit(), "IncompleteEdit");
            e.StageEdit("Customer.LoyaltyTier", ProfileValue.Missing);
            Assert.That(e.ApplyEdit().IsSuccess, Is.True);
            Assert.That(e.Evaluation.RawScore, Is.EqualTo(-2.9).Within(1e-12));
        }
        [Test]
        public void InvalidCategoryAndRangeNeverReplaceAcceptedEvaluation()
        {
            var e = Prepared(); var result = e.Evaluation;
            e.BeginEdit(); e.StageEdit("pyTreatment", ProfileValue.FromCategory("unknown"));
            Fixtures.Reject(e.ApplyEdit(), "InvalidCategoryValue");
            Assert.That(e.Evaluation, Is.SameAs(result));
            e.CancelEdit(); e.BeginEdit(); e.StageEdit("IH.Web.Inbound.Clicked.pyHistoricalOutcomeCount", ProfileValue.FromNumber(-1));
            Fixtures.Reject(e.ApplyEdit(), "ValueOutOfRange");
            Assert.That(e.Evaluation, Is.SameAs(result));
        }
        [Test]
        public void ApplyRetiresOldEventsAndRefreshesIdentityEvenWithAnUnchangedPath()
        {
            var e = Prepared(); var old = e.Current; var s = old.State;
            old.StepProfile(s.Revision, s.NodeId); long token = old.State.PendingDecision.EventId;
            string id = e.EvaluationId; var original = e.OriginalProfile;
            old.SetPaused(true); e.BeginEdit(); e.StageEdit("IH.Web.Inbound.Clicked.pyHistoricalOutcomeCount", ProfileValue.FromNumber(1));
            Assert.That(e.ApplyEdit().IsSuccess, Is.True);
            Assert.That(old.CompleteMove(token).Accepted, Is.False);
            Assert.That(e.EvaluationId, Is.Not.EqualTo(id)); Assert.That(e.Profile.Id, Is.Not.EqualTo(original.Id));
            Assert.That(e.Evaluation.ProfileId, Is.EqualTo(e.Profile.Id));
            Assert.That(e.Evaluation.RawScore, Is.EqualTo(-4.1).Within(1e-12));
            Assert.That(e.Index, Is.Zero); Assert.That(e.CompletedCount, Is.Zero); Assert.That(e.Current.State.Paused, Is.True);
            Assert.That(original.GetValue("IH.Web.Inbound.Clicked.pyHistoricalOutcomeCount").Number, Is.Zero);
            e.Edit("IH.Web.Inbound.Clicked.pyHistoricalOutcomeCount", ProfileValue.FromNumber(5)); Assert.That(e.Evaluation.RawScore, Is.EqualTo(-3.2).Within(1e-12));
            e.RestoreOriginal(); Assert.That(e.Profile, Is.SameAs(original)); Assert.That(e.Evaluation.RawScore, Is.EqualTo(-4.1).Within(1e-12));
        }
        [Test]
        public void BackAcrossBoundaryAndEarlierRevisionClearLaterContributions()
        {
            var e = Prepared(); Finish(e.Current); e.Select(1); Finish(e.Current); e.Select(2);
            Assert.That(e.Back().Accepted, Is.True); Assert.That(e.Index, Is.EqualTo(1));
            Assert.That(e.CompletedCount, Is.EqualTo(2));
            Assert.That(e.Back().Accepted, Is.True); Assert.That(e.CompletedCount, Is.EqualTo(1));
            Assert.That(e.Evaluation.RawScore, Is.EqualTo(-4.1).Within(1e-12));
            e.Select(0); e.Back(); Assert.That(e.CompletedCount, Is.Zero);
            e.Back(); for (int i = 0; i < 5; i++) Assert.That(e.Back().Accepted, Is.False);
            Assert.That(e.Select(-1), Is.False); Assert.That(e.Select(3), Is.False);
        }
        [Test]
        public void FeatureReusedAcrossEightTreesRecomputesVisitedAndHiddenRoutes()
        {
            var source = Fixtures.Model(); var model = EnsembleTeachingExample.Model(source);
            var profiles = EnsembleTeachingExample.Profiles(model, Fixtures.Profiles(source));
            var e = Fixtures.Require(EnsembleSession.Create(model, profiles.Profiles[0]));
            e.SetDetail(TourDetail.Short); Finish(e.Current); e.ExplainRemainingGroup();
            Assert.That(e.RouteTotal, Is.EqualTo(-32.3).Within(1e-12));
            Assert.That(e.Edit("IH.Web.Inbound.Clicked.pyHistoricalOutcomeCount", ProfileValue.FromNumber(5)).IsSuccess, Is.True);
            Assert.That(e.CompletedCount, Is.Zero);
            Assert.That(e.Evaluation.RawScore, Is.EqualTo(-25.1).Within(1e-12));
            Assert.That(e.Evaluation.Trees.Where((t, i) => i % 3 == 0).All(t => t.LeafId == "t1-active"), Is.True);
            Assert.That(e.OriginalProfile.GetValue("IH.Web.Inbound.Clicked.pyHistoricalOutcomeCount").Number, Is.Zero);
        }

        [Test]
        public void SelectingTreePreservesPauseAndShortTourCannotAdvanceWhilePaused()
        {
            var e = Prepared(); Finish(e.Current); e.SetDetail(TourDetail.Short); e.Current.SetPaused(true);
            Assert.That(e.ExplainRemainingGroup(), Is.False);
            e.Select(1); Assert.That(e.Current.State.Paused, Is.True);
            e.Select(0); Assert.That(e.Current.State.Paused, Is.True);
        }
    }
}
