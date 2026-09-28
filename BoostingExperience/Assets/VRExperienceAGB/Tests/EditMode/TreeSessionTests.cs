using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using VRExperienceAGB.Application;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Tests
{
    public class TreeSessionTests
    {
        private static TreeSession Manual() { var m=Fixtures.Model(); return Fixtures.Require(TreeSession.CreateManual(m,m.Trees[0].Id)); }
        private static void Choose(TreeSession session,bool branch)
        {
            var s=session.State; Assert.That(session.ChooseBranch(s.Revision,s.NodeId,branch).Accepted,Is.True);
            Assert.That(session.CompleteMove(session.State.PendingDecision.EventId).Accepted,Is.True);
        }
        private static void Step(TreeSession session)
        {
            var s=session.State; Assert.That(session.StepProfile(s.Revision,s.NodeId).Accepted,Is.True);
            Assert.That(session.CompleteMove(session.State.PendingDecision.EventId).Accepted,Is.True);
        }
        [TestCase(true,true,"t1-new",-3.8)] [TestCase(true,false,"t1-returning",-3.2)]
        [TestCase(false,true,"t1-active",-2.9)] [TestCase(false,false,"t1-engaged",-2.6)]
        public void ManualRoutesCommitExactlyOneLeaf(bool first,bool second,string leaf,double contribution)
        {
            var session=Manual(); Choose(session,first);
            Assert.That(session.State.Contribution,Is.Zero);
            Choose(session,second);
            Assert.That(session.State.NodeId,Is.EqualTo(leaf)); Assert.That(session.State.Contribution,Is.EqualTo(contribution));
            Assert.That(session.State.RouteTotal,Is.EqualTo(contribution)); Assert.That(session.State.ScoreMeaning,Is.EqualTo("Manual route score"));
            var atLeaf=session.State;
            Assert.That(session.ChooseBranch(atLeaf.Revision,atLeaf.NodeId,true).Accepted,Is.False);
            Assert.That(session.State.RouteTotal,Is.EqualTo(atLeaf.RouteTotal));
            Assert.That(atLeaf.Decisions.Select(d=>d.EventId).Distinct().Count(),Is.EqualTo(2));
        }
        [TestCase(0,"t1-new",-3.8)] [TestCase(1,"t1-returning",-3.2)] [TestCase(2,"t1-engaged",-2.6)] [TestCase(3,"t1-engaged",-2.6)]
        public void ProfilesReplayExactPathsWithoutPartialProbability(int profileIndex,string leaf,double contribution)
        {
            var model=Fixtures.Model(); var profile=Fixtures.Profiles(model).Profiles[profileIndex];
            var session=Fixtures.Require(TreeSession.CreatePrepared(model,model.Trees[0].Id,profile));
            var expected=Fixtures.Require(ModelEvaluator.Evaluate(model,profile)).Trees[0];
            for(var replay=0;replay<3;replay++)
            {
                session.Restart();
                while(!session.State.AtLeaf)
                {
                    var decision=session.CurrentProfileDecision;
                    Assert.That(decision,Is.SameAs(session.CurrentProfileDecision));
                    Assert.That(decision.ObservedValue,Is.EqualTo(profile.GetValue(decision.FeatureId))); Step(session);
                }
                Assert.That(session.State.Path,Is.EqualTo(expected.VisitedNodeIds)); Assert.That(session.State.NodeId,Is.EqualTo(leaf));
                Assert.That(session.State.Contribution,Is.EqualTo(contribution)); Assert.That(session.State.ScoreMeaning,Is.EqualTo("One-tree teaching subtotal"));
                session.Back(); Assert.That(session.State.Contribution,Is.Zero); Step(session); Assert.That(session.State.Contribution,Is.EqualTo(contribution));
            }
            Assert.That(typeof(SessionState).GetProperty("Probability"),Is.Null);
        }
        [Test]
        public void DuplicateAndStaleChoicesCannotSkipDecisions()
        {
            var session=Manual(); var old=session.State;
            Assert.That(session.ChooseBranch(old.Revision,old.NodeId,true).Accepted,Is.True);
            Assert.That(session.ChooseBranch(old.Revision,old.NodeId,false).Accepted,Is.False);
            var token=session.State.PendingDecision.EventId;
            Assert.That(session.CompleteMove(token).Accepted,Is.True);
            Assert.That(session.CompleteMove(token).Accepted,Is.False);
            Assert.That(session.ChooseBranch(old.Revision,old.NodeId,true).Accepted,Is.False);
            Assert.That(session.State.Path,Is.EqualTo(new[]{"t1-root","t1-visits"}));
        }
        [Test]
        public void CancelledMoveCompletionCannotCommitAfterRestartOrUndo()
        {
            var session=Manual(); var s=session.State;
            session.ChooseBranch(s.Revision,s.NodeId,true); var token=session.State.PendingDecision.EventId;
            session.Back(); Assert.That(session.CompleteMove(token).Accepted,Is.False); Assert.That(session.State.Path.Count,Is.EqualTo(1));
            s=session.State; session.ChooseBranch(s.Revision,s.NodeId,false); var second=session.State.PendingDecision.EventId;
            Assert.That(second,Is.Not.EqualTo(token)); session.Restart(); Assert.That(session.CompleteMove(second).Accepted,Is.False);
            Choose(session,true); Choose(session,true); session.Back(); Choose(session,false);
            Assert.That(session.State.NodeId,Is.EqualTo("t1-returning")); Assert.That(session.State.RouteTotal,Is.EqualTo(-3.2));
        }
        [Test]
        public void PauseBlocksMovementAndDecisionsWithoutChangingAcceptedState()
        {
            var session=Manual(); var s=session.State; session.ChooseBranch(s.Revision,s.NodeId,true); var token=session.State.PendingDecision.EventId;
            session.SetPaused(true); Assert.That(session.CompleteMove(token).Accepted,Is.False);
            Assert.That(session.State.NodeId,Is.EqualTo("t1-root"));
            session.SetPaused(false); Assert.That(session.CompleteMove(token).Accepted,Is.True);
            session.SetPaused(true); s=session.State; Assert.That(session.ChooseBranch(s.Revision,s.NodeId,true).Accepted,Is.False);
            Assert.That(session.State.Path.Count,Is.EqualTo(2));
        }
        [Test]
        public void OverviewRetainsAcceptedLeafAndCancelsPendingMovement()
        {
            var session=Manual(); Choose(session,false); Choose(session,false); var total=session.State.RouteTotal;
            session.ReturnToOverview(); Assert.That(session.State.RouteTotal,Is.EqualTo(total)); Assert.That(session.State.Overview,Is.True);
            session.EnterTree(); Assert.That(session.State.NodeId,Is.EqualTo("t1-engaged"));
            session.Restart(); var s=session.State; session.ChooseBranch(s.Revision,s.NodeId,true); var token=session.State.PendingDecision.EventId;
            session.ReturnToOverview(); session.EnterTree(); Assert.That(session.CompleteMove(token).Accepted,Is.False);
            Assert.That(session.State.Path.Count,Is.EqualTo(1));
        }
        [Test]
        public void RootBackAndRepeatedRestartAreSafeAndRestoreBaseline()
        {
            var session=Manual(); Assert.That(session.Back().Accepted,Is.False);
            Choose(session,true); Choose(session,false); session.SetPaused(true); session.ReturnToOverview();
            for(var i=0;i<3;i++)
            {
                session.Restart(); Assert.That(session.State.RouteTotal,Is.EqualTo(0)); Assert.That(session.State.Path.Count,Is.EqualTo(1));
                Assert.That(session.State.PendingDecision,Is.Null); Assert.That(session.State.Paused||session.State.Playing||session.State.Overview,Is.False);
            }
        }
        [TestCase(true)] [TestCase(false)]
        public void ArbitraryBranchAlwaysEntersManualAndLeavesProfileUnchanged(bool branch)
        {
            var model=Fixtures.Model(); var profile=Fixtures.Profiles(model).Profiles[0];
            var session=Fixtures.Require(TreeSession.CreatePrepared(model,model.Trees[0].Id,profile));
            session.SetPlaying(true); Choose(session,branch);
            Assert.That(session.State.Mode,Is.EqualTo(ExperienceMode.Manual)); Assert.That(session.State.Playing,Is.False);
            Assert.That(session.CurrentProfileDecision,Is.Null); Assert.That(profile.GetValue("IH.Web.Inbound.Clicked.pyHistoricalOutcomeCount").Number,Is.Zero);
            Assert.That(session.SetPlaying(true).Accepted,Is.False);
        }
        [Test]
        public void PlaybackStopsAtLeafAndBackAndOverview()
        {
            var model=Fixtures.Model(); var profile=Fixtures.Profiles(model).Profiles[1];
            var session=Fixtures.Require(TreeSession.CreatePrepared(model,model.Trees[0].Id,profile));
            Assert.That(session.SetPlaying(true).Accepted,Is.True); Step(session); Assert.That(session.State.Playing,Is.True);
            session.SetPaused(true); Assert.That(session.State.Playing,Is.True); session.SetPaused(false); Step(session);
            Assert.That(session.State.Playing,Is.False); Assert.That(session.SetPlaying(true).Accepted,Is.False);
            session.Back(); session.SetPlaying(true); session.Back(); Assert.That(session.State.Playing,Is.False);
            session.SetPlaying(true); session.ReturnToOverview(); Assert.That(session.State.Playing,Is.False);
        }
        [Test]
        public void SnapshotsRemainImmutableAcrossCommands()
        {
            var session=Manual(); Choose(session,true); var snapshot=session.State; Choose(session,false); session.Restart();
            Assert.That(snapshot.NodeId,Is.EqualTo("t1-visits")); Assert.That(snapshot.Path.Count,Is.EqualTo(2));
            Assert.Throws<NotSupportedException>(()=>((IList<string>)snapshot.Path).Clear());
            Assert.Throws<NotSupportedException>(()=>((IList<RouteDecision>)snapshot.Decisions).Clear());
            Assert.That(typeof(TreeSession).Assembly.GetReferencedAssemblies().Select(a=>a.Name).Where(n=>n.StartsWith("Unity")||n.StartsWith("Oculus")),Is.Empty);
        }
        [Test]
        public void InvalidModelProfileOrTreeNeverCreatesUsableSession()
        {
            var m=Fixtures.Model();
            Fixtures.Reject(TreeSession.CreateManual(null,"x"),"ModelRequired"); Fixtures.Reject(TreeSession.CreateManual(m,"absent"),"UnknownTree");
            Fixtures.Reject(TreeSession.CreatePrepared(m,m.Trees[0].Id,null),"ProfileRequired");
            var p=Fixtures.Profiles(m).Profiles[0].WithValue("Customer.DigitalVisits30Days",ProfileValue.Missing);
            Fixtures.Reject(TreeSession.CreatePrepared(m,m.Trees[0].Id,p),"MissingRequiredValue");
        }
    }
}
