using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using VRExperienceAGB.Domain;
using VRExperienceAGB.Import;
using VRExperienceAGB.Application;

namespace VRExperienceAGB.Tests
{
    public sealed class ReviewRegressionTests
    {
        private static AgbPreviewResult Export(string feature = "Party.Age") => AgbStructurePreview.Read(
            "{\"type\":\"AdaptiveBoostScoringModel\",\"algorithm\":\"GRADIENT_BOOST\",\"model\":{\"booster\":{\"trees\":[{\"score\":0,\"gain\":3,\"sampleCount\":20,\"split\":\""+feature+" < 10\",\"left\":{\"score\":-1,\"gain\":0,\"sampleCount\":10},\"right\":{\"score\":2,\"gain\":0,\"sampleCount\":10}}]}}}", "Synthetic").Value;
        [Test] public void FingerprintAndCompleteMappingAreBothRequiredForMetadata()
        {
            var source = Export(); var m = source.Model;
            var copy = new ModelDefinition(1,"copy","synthetic","binary_logistic","response",0,m.Features,m.Trees);
            Assert.That(new ForestExplanation(copy,source).TotalGain,Is.Null);
            Assert.That(new ForestExplanation(copy,source,source.Sha256).TotalGain,Is.EqualTo(3));
            var changed = new ModelDefinition(1,"changed","synthetic","binary_logistic","response",0,m.Features,
                new[]{new ModelTree(m.Trees[0].Id,"root",1,new ModelNode[]{m.Trees[0].Nodes[0],new LeafNode("root/left",-99),new LeafNode("root/right",2)})});
            Assert.That(new ForestExplanation(changed,source,source.Sha256).TotalGain,Is.Null);
            Assert.That(new ForestExplanation(copy,Export("Party.Other"),source.Sha256).TotalGain,Is.Null);
        }
        [Test] public void NamespacesAreDynamicAndCustomerScoresIsNotASpecialFamily()
        {
            Assert.That(ForestExplanation.Family("Party.Age"),Is.EqualTo("Party"));
            Assert.That(ForestExplanation.Family("Customer.Scores.External"),Is.EqualTo("Customer"));
            Assert.That(ForestExplanation.Family("pyAction"),Is.EqualTo("Unqualified"));
            var e=Export();var a=new ForestExplanation(e.Model,e);
            Assert.That(a.Families,Is.EqualTo(new[]{"Party"}));
            Assert.That(a.Trees[0].FamilyGains["Party"],Is.EqualTo(3));
        }
        [Test] public void CollidingDisplayNamesRetainExactIdentity()
        {
            var e=Export();var m=e.Model;
            var features=new[]{new FeatureDefinition("Party.Age",FeatureKind.Number,false,"Age"),new FeatureDefinition("Household.Age",FeatureKind.Number,false,"Age")};
            var copy=new ModelDefinition(1,"labels","synthetic","binary_logistic","response",0,features,m.Trees);
            var a=new ForestExplanation(copy);
            Assert.That(a.Label("Party.Age"),Does.Contain("Party.Age"));
            Assert.That(a.Label("Party.Age"),Is.Not.EqualTo(a.Label("Household.Age")));
        }
        [Test] public void ActivationRejectsFiniteFieldsWhoseWeightedLeavesOverflow()
        {
            var model=new ModelDefinition(1,"overflow","synthetic","binary_logistic","response",0,new[]{new FeatureDefinition("x",FeatureKind.Number,true)},
                new[]{new ModelTree("t","leaf",1e308,new[]{new LeafNode("leaf",2)})});
            var result=ModelActivation.Prepare(model,new ProfileSet(1,model.Id,Array.Empty<PreparedProfile>()));
            Assert.That(result.IsSuccess,Is.False);Assert.That(result.Diagnostics.Any(d=>d.Code=="ScoreOverflow"),Is.True);
        }
        [Test] public void ActivationValidatesCompleteProfileSumBeforeCommit()
        {
            var model=new ModelDefinition(1,"sum-overflow","synthetic","binary_logistic","response",0,new[]{new FeatureDefinition("x",FeatureKind.Number,true)},
                new[]{new ModelTree("a","leaf",1,new[]{new LeafNode("leaf",1e308)}),new ModelTree("b","leaf",1,new[]{new LeafNode("leaf",1e308)})});
            var p=new PreparedProfile(1,model.Id,"p","Synthetic",new Dictionary<string,ProfileValue>{{"x",ProfileValue.Missing}});
            var result=ModelActivation.Prepare(model,new ProfileSet(1,model.Id,new[]{p}));
            Assert.That(result.IsSuccess,Is.False);Assert.That(result.Diagnostics.Any(d=>d.Code=="ScoreOverflow"),Is.True);
        }
    }
}
