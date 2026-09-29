using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using VRExperienceAGB.Domain;
using VRExperienceAGB.Import;

namespace VRExperienceAGB.Tests
{
    public class ValidationTests
    {
        // Each case changes only one semantic rule in the checked-in fixture.
        [TestCase("schema", "UnsupportedSchema")]
        [TestCase("id", "InvalidModelId")]
        [TestCase("provenance", "ProvenanceRequired")]
        [TestCase("outcome", "OutcomeRequired")]
        [TestCase("objective", "UnsupportedObjective")]
        [TestCase("baseline", "RequiredField")]
        [TestCase("weight", "RequiredField")]
        [TestCase("score", "RequiredField")]
        [TestCase("threshold", "RequiredField")]
        [TestCase("policy", "RequiredField")]
        [TestCase("unknown-field", "UnsupportedField")]
        [TestCase("empty-features", "FeaturesRequired")]
        [TestCase("feature-type", "InvalidFeatureType")]
        [TestCase("range", "InvalidFeatureRange")]
        [TestCase("categories", "InvalidCategoryDomain")]
        [TestCase("empty-trees", "TreesRequired")]
        [TestCase("tree-id", "DuplicateTreeId")]
        [TestCase("node-id", "DuplicateNodeId")]
        [TestCase("root", "InvalidRoot")]
        [TestCase("child", "MissingChild")]
        [TestCase("cycle", "Cycle")]
        [TestCase("unreachable", "UnreachableNode")]
        [TestCase("multiple-parents", "MultipleParents")]
        [TestCase("no-terminal", "NoTerminalRoute")]
        [TestCase("node-kind", "UnsupportedNode")]
        [TestCase("operator", "UnsupportedOperator")]
        [TestCase("feature", "UnknownFeature")]
        [TestCase("numeric-category", "InvalidNumericSplit")]
        [TestCase("unknown-category", "InvalidMembershipSplit")]
        [TestCase("missing-policy", "InvalidMissingSplit")]
        public void InvalidModelFailsClosed(string mutation, string code)
        {
            var doc = JObject.Parse(Fixtures.Read("demo-model.json"));
            var trees = (JArray)doc["trees"]; var first = (JObject)trees[0]; var nodes = (JArray)first["nodes"]; var root = (JObject)nodes[0];
            switch (mutation)
            {
                case "schema": doc["schemaVersion"] = 2; break;
                case "id": doc["modelId"] = " "; break;
                case "provenance": doc["provenance"] = ""; break;
                case "outcome": doc["outcomeLabel"] = ""; break;
                case "objective": doc["objective"] = "source-calibrated"; break;
                case "baseline": doc.Remove("baseScore"); break;
                case "weight": first.Remove("weight"); break;
                case "score": ((JObject)nodes[3]).Remove("score"); break;
                case "threshold": root.Remove("threshold"); break;
                case "policy": ((JObject)doc["features"]["Customer.DigitalVisits30Days"]).Remove("allowMissing"); break;
                case "unknown-field": doc["learningRate"] = 0.1; break;
                case "empty-features": doc["features"] = new JObject(); break;
                case "feature-type": doc["features"]["Customer.DigitalVisits30Days"]["type"] = "bool"; break;
                case "range": doc["features"]["Customer.DigitalVisits30Days"]["maximum"] = -1; break;
                case "categories": doc["features"]["pyTreatment"]["values"] = new JArray("Welcome Banner", "Welcome Banner"); break;
                case "empty-trees": trees.Clear(); break;
                case "tree-id": trees[1]["id"] = trees[0]["id"].DeepClone(); break;
                case "node-id": nodes[3]["id"] = root["id"].DeepClone(); break;
                case "root": first["rootId"] = "not-a-node"; break;
                case "child": root["trueChild"] = "not-a-node"; break;
                case "cycle": root["trueChild"] = root["id"].DeepClone(); break;
                case "unreachable": nodes.Add(new JObject { ["id"] = "orphan", ["kind"] = "leaf", ["score"] = 0 }); break;
                case "multiple-parents": root["trueChild"] = root["falseChild"].DeepClone(); break;
                case "no-terminal": while (nodes.Count > 3) nodes.RemoveAt(3); break;
                case "node-kind": root["kind"] = "oblique"; break;
                case "operator": root["operator"] = "lte"; break;
                case "feature": root["feature"] = "unknown"; break;
                case "numeric-category": root["feature"] = "pyTreatment"; break;
                case "unknown-category": trees[1]["nodes"][0]["values"] = new JArray("unknown"); break;
                case "missing-policy": doc["features"]["Customer.LoyaltyTier"]["allowMissing"] = false; break;
                default: Assert.Fail("Unimplemented test mutation"); break;
            }
            Fixtures.Reject(NormalizedModelJson.ReadModel(doc.ToString()), code);
        }

        [TestCase("schema", "UnsupportedSchema")]
        [TestCase("model", "ModelMismatch")]
        [TestCase("duplicate", "DuplicateProfileId")]
        [TestCase("empty", "ProfilesRequired")]
        [TestCase("missing", "MissingRequiredValue")]
        [TestCase("null", "MissingRequiredValue")]
        [TestCase("numeric-string", "InvalidNumericValue")]
        [TestCase("boolean", "InvalidProfileValue")]
        [TestCase("object", "InvalidProfileValue")]
        [TestCase("fraction", "IntegerRequired")]
        [TestCase("range", "ValueOutOfRange")]
        [TestCase("category-case", "InvalidCategoryValue")]
        [TestCase("category-number", "InvalidCategoryValue")]
        [TestCase("unknown-feature", "UnknownFeature")]
        public void InvalidProfileInvalidatesEntireSet(string mutation, string code)
        {
            var doc = JObject.Parse(Fixtures.Read("demo-profiles.json")); var profiles = (JArray)doc["profiles"];
            var values = (JObject)profiles[3]["values"];
            switch (mutation)
            {
                case "schema": doc["schemaVersion"] = 2; break;
                case "model": doc["modelId"] = "other-model"; break;
                case "duplicate": profiles[3]["id"] = profiles[0]["id"].DeepClone(); break;
                case "empty": profiles.Clear(); break;
                case "missing": values.Remove("Customer.DigitalVisits30Days"); break;
                case "null": values["Customer.DigitalVisits30Days"] = null; break;
                case "numeric-string": values["Customer.DigitalVisits30Days"] = "3"; break;
                case "boolean": values["Customer.DigitalVisits30Days"] = true; break;
                case "object": values["Customer.DigitalVisits30Days"] = new JObject(); break;
                case "fraction": values["Customer.DigitalVisits30Days"] = 1.5; break;
                case "range": values["Customer.DigitalVisits30Days"] = -1; break;
                case "category-case": values["pyTreatment"] = "savings card"; break;
                case "category-number": values["pyTreatment"] = 1; break;
                case "unknown-feature": values["invented"] = 1; break;
                default: Assert.Fail("Unimplemented test mutation"); break;
            }
            Fixtures.Reject(NormalizedModelJson.ReadProfiles(doc.ToString(), Fixtures.Model()), code);
        }

        [TestCase("{\"schemaVersion\":1,\"schemaVersion\":1}")]
        [TestCase("{broken")]
        [TestCase("[]")]
        [TestCase("{} {}")]
        public void MalformedOrDuplicateJsonDoesNotThrowOrGuess(string json)
        {
            var result = NormalizedModelJson.ReadModel(json);
            Assert.That(result.IsSuccess, Is.False); Assert.That(result.Value, Is.Null); Assert.That(result.Diagnostics, Is.Not.Empty);
        }

        [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] [TestCase(double.NegativeInfinity)]
        public void NonFiniteInputsRejectedAtEveryScoringBoundary(double invalid)
        {
            var model = Fixtures.Model(); var profile = Fixtures.Profiles(model).Profiles[0];
            ModelDefinition With(double baseline, IEnumerable<ModelTree> trees) => new ModelDefinition(1, model.Id, model.Provenance,
                model.Objective, model.OutcomeLabel, baseline, model.Features, trees);
            Fixtures.Reject(ModelEvaluator.Evaluate(With(invalid, model.Trees), profile), "InvalidBaseline");
            Fixtures.Reject(ModelEvaluator.Evaluate(With(0, new[] { new ModelTree("tree", "leaf", invalid, new[] { new LeafNode("leaf", 0) }) }), profile), "InvalidWeight");
            Fixtures.Reject(ModelEvaluator.Evaluate(With(0, new[] { new ModelTree("tree", "leaf", 1, new[] { new LeafNode("leaf", invalid) }) }), profile), "InvalidLeafScore");
            Fixtures.Reject(ModelEvaluator.Evaluate(model, profile.WithValue("Customer.DigitalVisits30Days", ProfileValue.FromNumber(invalid))), "InvalidNumericValue");
            var doc = JObject.Parse(Fixtures.Read("demo-model.json")); doc["baseScore"] = new JValue(invalid);
            Assert.That(NormalizedModelJson.ReadModel(doc.ToString()).IsSuccess, Is.False);
        }

        [Test]
        public void LaterTreeFailureDoesNotExposeEarlierOrPreviousResult()
        {
            var model = Fixtures.Model(); var profile = Fixtures.Profiles(model).Profiles[0];
            var previous = Fixtures.Require(ModelEvaluator.Evaluate(model, profile));
            var trees = model.Trees.ToList(); trees.Add(new ModelTree("invalid-final", "absent", 1, new[] { new LeafNode("leaf", 1) }));
            var invalid = new ModelDefinition(1, model.Id, model.Provenance, model.Objective, model.OutcomeLabel, model.BaseScore, model.Features, trees);
            Fixtures.Reject(ModelEvaluator.Evaluate(invalid, profile), "InvalidRoot");
            Fixtures.Reject(ModelEvaluator.Evaluate(model, profile.WithValue("Customer.DigitalVisits30Days", ProfileValue.Missing)), "MissingRequiredValue");
            Assert.That(previous.RawScore, Is.EqualTo(-4.1).Within(ModelEvaluator.ReferenceTolerance));
            Assert.That(Fixtures.Require(ModelEvaluator.Evaluate(model, profile)).RawScore, Is.EqualTo(previous.RawScore));
        }

        [Test]
        public void DiagnosticsIdentifyContextWithoutIncludingProfileValues()
        {
            const string privateValue = "PRIVATE_PROFILE_VALUE";
            var model = Fixtures.Model(); var profile = Fixtures.Profiles(model).Profiles[0].WithValue("pyTreatment", ProfileValue.FromCategory(privateValue));
            var failure = ModelEvaluator.Evaluate(model, profile);
            Fixtures.Reject(failure, "InvalidCategoryValue");
            Assert.That(failure.Diagnostics[0].ModelId, Is.EqualTo(model.Id)); Assert.That(failure.Diagnostics[0].FeatureId, Is.EqualTo("pyTreatment"));
            Assert.That(string.Join(" ", failure.Diagnostics.Select(d => d.Message + d.Location)), Does.Not.Contain(privateValue));
            var json = "{\"schemaVersion\":1,\"modelId\":\"" + model.Id + "\",\"profiles\":[\"" + privateValue + "\"]}";
            var importFailure = NormalizedModelJson.ReadProfiles(json, model);
            Assert.That(importFailure.IsSuccess, Is.False);
            Assert.That(string.Join(" ", importFailure.Diagnostics.Select(d => d.Message + d.Location)), Does.Not.Contain(privateValue));
            var bad = new ModelDefinition(1, model.Id, model.Provenance, model.Objective, model.OutcomeLabel, 0, model.Features,
                new[] { new ModelTree("broken-tree", "node", 1, new ModelNode[] { new SplitNode("node", "Customer.DigitalVisits30Days", new SplitCondition((DecisionOperator)999), "a", "b"), new LeafNode("a", 0), new LeafNode("b", 0) }) });
            var diagnostic = ModelValidator.Validate(bad).Single(d => d.Code == "UnsupportedOperator");
            Assert.That(diagnostic.TreeId, Is.EqualTo("broken-tree")); Assert.That(diagnostic.NodeId, Is.EqualTo("node")); Assert.That(diagnostic.FeatureId, Is.EqualTo("Customer.DigitalVisits30Days"));
        }

        [Test]
        public void DuplicateFeatureIdsAndNullInputsFailValidation()
        {
            var model = Fixtures.Model();
            var invalid = new ModelDefinition(1, model.Id, model.Provenance, model.Objective, model.OutcomeLabel, 0,
                model.Features.Concat(new[] { model.Features[0] }), model.Trees);
            Fixtures.Reject(ModelEvaluator.Evaluate(invalid, Fixtures.Profiles(model).Profiles[0]), "DuplicateFeatureId");
            Fixtures.Reject(ModelEvaluator.Evaluate(null, null), "ModelRequired");
            Fixtures.Reject(ModelEvaluator.Evaluate(model, null), "ProfileRequired");
            Assert.That(ModelValidator.ValidateProfile(null, null).Select(d => d.Code), Does.Contain("ModelRequired"));
            Assert.That(NormalizedModelJson.ReadModel(null).IsSuccess, Is.False);
        }
    }
}
