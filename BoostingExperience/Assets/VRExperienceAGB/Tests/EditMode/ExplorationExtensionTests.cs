using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VRExperienceAGB.Application;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Tests
{
    public class ExplorationExtensionTests
    {
        [Test]
        public void ExactPredictorSearchRejectsPartialAndCaseChangedIdentifiers()
        {
            var m = Fixtures.Model(); var search = new PredictorSearch(m);
            const string id = "Customer.LoyaltyTier";
            Assert.That(search.Query(id, PredictorScope.EntireModel, null).Select(o => o.Node.Id), Is.EqualTo(new[] { "t3-root" }));
            Assert.That(search.Query("LoyaltyTier", PredictorScope.EntireModel, null), Is.Empty);
            Assert.That(search.Query(id.ToLowerInvariant(), PredictorScope.EntireModel, null), Is.Empty);
        }
        [Test]
        public void PathScopeUsesCompleteEvaluatedPathIncludingUnvisitedTrees()
        {
            var m = Fixtures.Model(); var s = Fixtures.Require(EnsembleSession.Create(m, Fixtures.Profiles(m).Profiles[0]));
            var search = new PredictorSearch(m);
            Assert.That(search.Query("Customer.LoyaltyTier", PredictorScope.CurrentProfilePath, s).Single().TreeIndex, Is.EqualTo(2));
            Assert.That(s.CompletedCount, Is.Zero);
            var offPath = m.Trees[0].Nodes.OfType<SplitNode>().Single(n => n.Id == "t1-value").FeatureId;
            Assert.That(search.Query(offPath, PredictorScope.EntireModel, s).Count, Is.GreaterThan(0));
            Assert.That(search.Query(offPath, PredictorScope.CurrentProfilePath, s), Is.Empty);
            s.TakeOver(); Assert.That(search.Query("Customer.LoyaltyTier", PredictorScope.CurrentProfilePath, s), Is.Empty);
        }
        [Test]
        public void ProgressionReconcilesIndependentFixturePrefixesWithoutChangingRoute()
        {
            var m = Fixtures.Model(); var s = Fixtures.Require(EnsembleSession.Create(m, Fixtures.Profiles(m).Profiles[0]));
            var points = BoostingProgression.Points(s);
            Assert.That(points.Select(p => p.RawScore), Is.EqualTo(new[] { 0, -3.8, -3.9, -4.1 }).Within(1e-12));
            Assert.That(points[0].Probability, Is.EqualTo(.5));
            Assert.That(points[3].Probability, Is.EqualTo(.016302499371440946).Within(1e-12));
            Assert.That(s.CompletedCount, Is.Zero); Assert.That(s.RouteTotal, Is.Zero);
            Assert.That(BoostingProgression.Points(Fixtures.Require(EnsembleSession.Create(m))), Is.Empty);
        }
        [Test]
        public void ReviewRoundTripPreservesOrderIdsAndEditedProfileSnapshot()
        {
            var m = Fixtures.Model(); var p = Fixtures.Profiles(m).Profiles[0].WithValue("Customer.LoyaltyTier", ProfileValue.FromCategory("gold"));
            var review = GuidedReview.Create(m);
            review.stops.Add(new ReviewStop { kind = ReviewStopKind.Forest, explanation = "First" });
            review.stops.Add(new ReviewStop { kind = ReviewStopKind.Comparison, treeId = m.Trees[0].Id, nodeId = "t1-root", explanation = "Second",
                profileA = ReviewProfile.Capture(Fixtures.Profiles(m).Profiles[0]), profileB = ReviewProfile.Capture(p), showingB = true });
            var copy = JsonUtility.FromJson<GuidedReview>(JsonUtility.ToJson(review));
            Assert.That(copy.stops.Select(s => s.explanation), Is.EqualTo(new[] { "First", "Second" }));
            Assert.That(copy.stops[1].profileB.Restore(m.Id).GetValue("Customer.LoyaltyTier").Category, Is.EqualTo("gold"));
            Assert.That(copy.stops[1].showingB, Is.True);
            Assert.That(copy.Validate(m), Is.Null);
        }
        [Test]
        public void ReviewRejectsChangedSemanticsEvenWhenTreeAndNodeIdsMatch()
        {
            var m = Fixtures.Model(); var review = GuidedReview.Create(m);
            review.stops.Add(new ReviewStop { kind = ReviewStopKind.Node, treeId = m.Trees[0].Id, nodeId = "t1-root" });
            Assert.That(review.Validate(m), Is.Null);
            var changed = new ModelDefinition(m.SchemaVersion, m.Id, m.Provenance, m.Objective, m.OutcomeLabel, 1, m.Features, m.Trees);
            Assert.That(review.Validate(changed), Does.Contain("model changed"));
            review.stops[0].nodeId = "missing-node";
            Assert.That(review.Validate(m), Does.Contain("Missing node"));
        }
        [Test]
        public void ReviewRejectsMissingInvalidAndDuplicateProfileData()
        {
            var m = Fixtures.Model(); var review = GuidedReview.Create(m);
            review.stops.Add(new ReviewStop { kind = ReviewStopKind.Profile, treeId = m.Trees[0].Id });
            Assert.That(review.Validate(m), Does.Contain("Missing saved profile"));
            var p = ReviewProfile.Capture(Fixtures.Profiles(m).Profiles[0]);
            p.values = p.values.Concat(new[] { p.values[0] }).ToArray(); review.stops[0].profileA = p;
            Assert.That(review.Validate(m), Does.Contain("duplicate"));
        }
    }
}
