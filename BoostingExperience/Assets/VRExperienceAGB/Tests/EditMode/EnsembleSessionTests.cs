using System.Linq;
using NUnit.Framework;
using VRExperienceAGB.Application;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Tests
{
    public class EnsembleSessionTests
    {
        [Test] public void ConsistencyIsCachedUntilAnyTreeRevisionChanges()
        {
            var ensemble=Fixtures.Require(EnsembleSession.Create(Fixtures.Model()));
            var first=ensemble.Consistency;Assert.That(ensemble.Consistency,Is.SameAs(first));
            var tree=ensemble.Trees[1];tree.EnterTree();
            var state=tree.State;tree.ChooseBranch(state.Revision,state.NodeId,true);tree.CompleteMove(tree.State.PendingDecision.EventId);
            var changed=ensemble.Consistency;Assert.That(changed,Is.Not.SameAs(first));
            Assert.That(ensemble.Consistency,Is.SameAs(changed));tree.Back();Assert.That(ensemble.Consistency,Is.Not.SameAs(changed));
        }
        private static void Finish(TreeSession session)
        {
            while (!session.State.AtLeaf)
            { var state = session.State; session.StepProfile(state.Revision, state.NodeId); session.CompleteMove(session.State.PendingDecision.EventId); }
        }
        [TestCase(0, -4.1)] [TestCase(1, -2.6)] [TestCase(2, -2)] [TestCase(3, -2.9)]
        public void FullTourAndDirectEvaluationAgreeAndUndoRemovesOneContribution(int profile, double expected)
        {
            var model = Fixtures.Model();
            var ensemble = Fixtures.Require(EnsembleSession.Create(model, Fixtures.Profiles(model).Profiles[profile]));
            for (int i = 0; i < model.Trees.Count; i++) { if(i>0) ensemble.Select(i); Finish(ensemble.Current); }
            Assert.That(ensemble.Complete, Is.True);
            Assert.That(ensemble.RouteTotal, Is.EqualTo(expected).Within(1e-12));
            Assert.That(ensemble.Evaluation.RawScore, Is.EqualTo(expected).Within(1e-12));
            double contribution = ensemble.Current.State.Contribution;
            ensemble.Current.Back();
            Assert.That(ensemble.RouteTotal, Is.EqualTo(expected - contribution).Within(1e-12));
            Assert.That(ensemble.Complete, Is.False);
            Finish(ensemble.Current); Assert.That(ensemble.RouteTotal, Is.EqualTo(expected).Within(1e-12));
        }
        [Test]
        public void ProfileEditResetsEveryRouteAndLeavesOriginalImmutable()
        {
            var model = DeepTreeExample.Model(); var original = DeepTreeExample.Profiles(model).Profiles[0];
            var ensemble = Fixtures.Require(EnsembleSession.Create(model, original)); Finish(ensemble.Current);
            Assert.That(ensemble.Edit("position", ProfileValue.FromNumber(255)).IsSuccess, Is.True);
            Assert.That(ensemble.CompletedCount, Is.Zero);
            Assert.That(ensemble.Evaluation.RawScore, Is.EqualTo(127/64.0));
            Assert.That(original.GetValue("position").Number, Is.Zero);
            var valid = ensemble.Current;
            Assert.That(ensemble.Edit("position", ProfileValue.FromNumber(256)).IsSuccess, Is.False);
            Assert.That(ensemble.Current, Is.SameAs(valid));
            ensemble.RestoreOriginal(); Assert.That(ensemble.Evaluation.RawScore, Is.EqualTo(-2));
        }
        [Test]
        public void TreeSelectionPreservesProgressAndInvalidatesOldPendingMovement()
        {
            var model = Fixtures.Model(); var ensemble = Fixtures.Require(EnsembleSession.Create(model));
            var session = ensemble.Current; var s = session.State;
            session.ChooseBranch(s.Revision, s.NodeId, true); long token = session.State.PendingDecision.EventId;
            ensemble.Select(1); ensemble.Select(0);
            Assert.That(session.CompleteMove(token).Accepted, Is.False);
            Assert.That(session.State.NodeId, Is.EqualTo(model.Trees[0].RootId));
        }
        [Test]
        public void ArbitraryChoiceSwitchesAllTreesToManualWithoutLosingThatMove()
        {
            var model = Fixtures.Model(); var ensemble = Fixtures.Require(EnsembleSession.Create(model, Fixtures.Profiles(model).Profiles[0]));
            var s = ensemble.Current.State; ensemble.Current.ChooseBranch(s.Revision, s.NodeId, false); ensemble.TakeOver();
            Assert.That(ensemble.Current.State.PendingDecision, Is.Not.Null);
            Assert.That(ensemble.Evaluation, Is.Null);
            Assert.That(ensemble.Trees.All(t => t.State.Mode == ExperienceMode.Manual), Is.True);
        }
    }
}
