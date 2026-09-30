using NUnit.Framework;
using VRExperienceAGB.Application;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Tests
{
    public class GardenTreeMetricsTests
    {
        [Test]
        public void ReportsStructuralDepthAndLeavesBeforeAnyExploration()
        {
            var model=Fixtures.Model(); var metrics=new GardenTreeMetrics(model.Trees[0]);
            Assert.That(metrics.MaximumDepth,Is.EqualTo(2));Assert.That(metrics.LeafCount,Is.EqualTo(4));Assert.That(metrics.NodeCount,Is.EqualTo(7));
            var session=Fixtures.Require(EnsembleSession.Create(model));Assert.That(session.Current.State.Decisions.Count,Is.Zero);
            Assert.That(metrics.MaximumDepth,Is.EqualTo(2),"Route progress must not masquerade as structural depth.");
        }
        [Test]
        public void AStumpHasDepthZeroOneLeafAndANonzeroPine()
        {
            var tree=new ModelTree("stump","root",1,new[]{new LeafNode("root",.2)});var metrics=new GardenTreeMetrics(tree);
            Assert.That(metrics.MaximumDepth,Is.Zero);Assert.That(metrics.LeafCount,Is.EqualTo(1));Assert.That(metrics.PineHeight,Is.GreaterThan(0));
        }
        [Test]
        public void UnbalancedTreesUseTheLongestPathAndEveryLeaf()
        {
            var tree=new ModelTree("tree","a",1,new ModelNode[]{new SplitNode("a","x",new SplitCondition(DecisionOperator.LessThan,1),"b","c"),
                new LeafNode("b",0),new SplitNode("c","x",new SplitCondition(DecisionOperator.LessThan,2),"d","e"),new LeafNode("d",0),new LeafNode("e",0)});
            var metrics=new GardenTreeMetrics(tree);Assert.That(metrics.MaximumDepth,Is.EqualTo(2));Assert.That(metrics.LeafCount,Is.EqualTo(3));
        }
    }
}
