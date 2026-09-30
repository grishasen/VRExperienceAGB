using System;
using System.Linq;
using NUnit.Framework;
using VRExperienceAGB.Domain;
using VRExperienceAGB.Import;

namespace VRExperienceAGB.Tests
{
    public sealed class ForestExplanationTests
    {
        private const string Leaf = "{\"score\":0.25,\"gain\":0,\"sampleCount\":12}";
        private static string Split(string feature, double threshold, double gain, string left = Leaf, string right = Leaf) =>
            "{\"score\":0,\"gain\":"+gain.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"sampleCount\":100,\"split\":\""+feature+" < "+threshold.ToString(System.Globalization.CultureInfo.InvariantCulture)+"\",\"left\":"+left+",\"right\":"+right+"}";
        private static AgbPreviewResult Read(string trees, string monitoring = "") => AgbStructurePreview.Read(
            "{\"type\":\"AdaptiveBoostScoringModel\",\"algorithm\":\"GRADIENT_BOOST\","+monitoring+"\"model\":{\"booster\":{\"trees\":["+trees+"]}}}","Synthetic explanation checks").Value;
        [Test]
        public void AggregatesEveryTreeAndUsesExactIdentifiersAndStoredGain()
        {
            var export=Read(Split("IH.Click",10,3)+","+Split("IH.Click",30,1)+","+Split("IH.ClickOther",20,6));
            var analysis=new ForestExplanation(export.Model,export);
            Assert.That(analysis.TotalGain,Is.EqualTo(10));
            Assert.That(analysis.Occurrences("IH.Click"),Is.EqualTo(2));
            Assert.That(analysis.MatchingTrees("IH.Click"),Is.EqualTo(new[]{0,1}));
            Assert.That(analysis.Thresholds("IH.Click"),Is.EqualTo(new[]{10d,30d}));
            Assert.That(analysis.Trees.Sum(t=>t.FamilyGains.Values.Sum()),Is.EqualTo(10));
            Assert.That(analysis.Passport(2),Does.Contain("60% of ensemble"));
            Assert.That(analysis.Passport(2),Does.Contain("source index 2"));
        }
        [Test]
        public void ZeroNegativeAndMissingGainNeverInventImportance()
        {
            var zero=Read(Split("x",1,0));var negative=Read(Split("x",1,-1));
            Assert.That(new ForestExplanation(zero.Model,zero).Passport(0),Does.Contain("share unavailable"));
            Assert.That(new ForestExplanation(negative.Model,negative).TotalGain,Is.Null);
            Assert.That(new ForestExplanation(zero.Model).NodeEvidence(0,"root"),Does.Contain("not supplied"));
            Assert.That(new ForestExplanation(negative.Model,negative).NodeEvidence(0,"root"),Does.Contain("negative source gain"));
        }
        [Test]
        public void PortraitMergesStrictNumericBoundsAndRetainsMissingConditions()
        {
            var export=Read(Split("x",10,1, right:Split("x",20,2)));
            var analysis=new ForestExplanation(export.Model,export);
            Assert.That(analysis.Segment(0,"root/right/left"),Is.EqualTo("10 <= x < 20"));
            Assert.That(analysis.Segment(0,"root"),Does.Contain("no conditions"));
            var contradiction=Read(Split("x",20,1,right:Split("x",10,2)));
            Assert.That(new ForestExplanation(contradiction.Model,contradiction).Segment(0,"root/right/left"),Does.StartWith("CONFLICT:"));
        }
        [Test]
        public void FullMembershipAndAbsenceRemainExplicitAndNeverMeanNewCustomer()
        {
            string missing="{\"score\":0,\"gain\":1,\"sampleCount\":10,\"split\":\"IH.Click is Missing\",\"left\":"+Leaf+",\"right\":"+Leaf+"}";
            string member="{\"score\":0,\"gain\":1,\"sampleCount\":10,\"split\":\"pyTreatment in { A, B, C, Long Name / D }\",\"left\":"+missing+",\"right\":"+Leaf+"}";
            var export=Read(member);var analysis=new ForestExplanation(export.Model,export);
            Assert.That(analysis.NodeDetail(0,"root"),Does.Contain("Long Name / D"));
            Assert.That(analysis.NodeDetail(0,"root/left"),Does.Contain("does not establish a new customer"));
            Assert.That(analysis.Segment(0,"root/left/left"),Does.Contain("is missing"));
            Assert.That(analysis.NodeEvidence(0,"root/left"),Does.Contain("below 100 observations"));
        }
        [Test]
        public void MonitoringIsOptionalAndInvalidValuesDoNotBecomeZeroOrGreen()
        {
            var absent=Read(Leaf);Assert.That(absent.Monitoring.Total,Is.Null);
            var export=Read(Leaf,"\"modelVersion\":\"fixture-v1\",\"factoryUpdateTime\":\"2026-01-01\",\"auc\":1.1,\"trainingStats\":{\"totalCount\":100,\"positiveCount\":10,\"negativeCount\":80},");
            Assert.That(export.Monitoring.CountsReconcile,Is.False);Assert.That(export.Monitoring.Auc,Is.Null);
            Assert.That(new ForestExplanation(export.Model,export).ModelStory(),Does.Contain("counts unavailable or inconsistent"));
            var valid=Read(Leaf,"\"trainingStats\":{\"totalCount\":100,\"positiveCount\":10,\"negativeCount\":90},");
            Assert.That(valid.Monitoring.CountsReconcile,Is.True);
            Assert.That(new ForestExplanation(valid.Model,valid).ModelStory(),Does.Contain("Positive rate 10%"));
        }
        [Test]
        public void SuppliedUnitsAreUsedWithoutGuessingFromNames()
        {
            var feature=new FeatureDefinition("duration",FeatureKind.Number,false,"Time since response",unit:"days");
            var split=new SplitNode("root","duration",new SplitCondition(DecisionOperator.LessThan,7),"a","b");
            var model=new ModelDefinition(1,"units","synthetic","binary_logistic","response",0,new[]{feature},new[]{new ModelTree("t","root",1,new ModelNode[]{split,new LeafNode("a",-1),new LeafNode("b",1)})});
            Assert.That(new ForestExplanation(model).Question(split),Is.EqualTo("Time since response is less than 7 days?"));
            var export=Read(Split("IH.DaysSince",7,1));
            Assert.That(new ForestExplanation(export.Model,export).Kind((SplitNode)export.Model.Trees[0].Nodes[0]),Does.Contain("unit not supplied"));
        }
        [Test]
        public void LeafBoundsAreWeightedAndUnrelatedMetadataCannotLeakAcrossModels()
        {
            var first=Read(Split("x",1,3));var second=Read(Split("y",2,7));
            var analysis=new ForestExplanation(second.Model,first);
            Assert.That(analysis.TotalGain,Is.Null);Assert.That(analysis.Trees[0].MinimumLeaf,Is.EqualTo(.25));
            Assert.That(analysis.Passport(0),Does.Not.Contain("probability"));
            Assert.That(analysis.ModelStory(),Does.Contain("Monitoring not supplied"));
        }
    }
}
