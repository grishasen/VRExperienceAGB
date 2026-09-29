using System.Linq;
using NUnit.Framework;
using VRExperienceAGB.Application;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Tests
{
    public class TreeNeighborhoodTests
    {
        [Test]
        public void EightLevelExampleRetainsAllNodesWhileShowingOnlySeven()
        {
            var model = DeepTreeExample.Model(); var tree = model.Trees[0];
            Assert.That(ModelValidator.Validate(model), Is.Empty);
            Assert.That(tree.Nodes.Count, Is.EqualTo(511));
            var view = new TreeNeighborhood(tree);
            Assert.That(view.Visible(tree.RootId).Count, Is.EqualTo(7));
            Assert.That(view.Descendants(tree.RootId), Is.EqualTo(510));
            foreach (var profile in DeepTreeExample.Profiles(model).Profiles)
            {
                var result = ModelEvaluator.Evaluate(model, profile);
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.Value.Trees[0].Decisions.Count, Is.EqualTo(8));
                Assert.That(result.Value.RawScore, Is.EqualTo((profile.GetValue("position").Number - 128) / 64));
                Assert.That(view.Visible(result.Value.Trees[0].LeafId).Count, Is.EqualTo(1));
            }
        }
        [Test]
        public void FocusAndOverviewDoNotChangeAcceptedRouteOrContribution()
        {
            var model = DeepTreeExample.Model(); var profile = DeepTreeExample.Profiles(model).Profiles[1];
            var session = TreeSession.CreatePrepared(model, model.Trees[0].Id, profile).Value;
            for (int i = 0; i < 8; i++)
            {
                var before = session.State;
                Assert.That(session.StepProfile(before.Revision, before.NodeId).Accepted, Is.True);
                session.CompleteMove(session.State.PendingDecision.EventId);
                var node = session.State.NodeId;
                session.ReturnToOverview(); session.EnterTree();
                Assert.That(session.State.NodeId, Is.EqualTo(node));
            }
            Assert.That(session.State.Contribution, Is.EqualTo(-43 / 64.0));
            session.Back(); Assert.That(session.State.Contribution, Is.Zero);
            Assert.That(session.State.Decisions.Count, Is.EqualTo(7));
        }
    }
}
