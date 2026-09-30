using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Import
{
    /// <summary>Strict normalized-v1 synthetic interchange. This is not an AGB export adapter.</summary>
    public static class NormalizedModelJson
    {
        private sealed class InvalidInput : Exception
        {
            public Diagnostic Diagnostic { get; }
            public InvalidInput(string code, string message, JToken token)
            { Diagnostic = new Diagnostic(code, message, token?.Path); }
        }

        private static T Fail<T>(string message, JToken token, string code = "InvalidJsonShape") =>
            throw new InvalidInput(code, message, token);

        private static JObject Object(JToken token) => token as JObject ?? Fail<JObject>("An object is required.", token);
        private static JArray Array(JToken token) => token as JArray ?? Fail<JArray>("An array is required.", token);
        private static JToken Required(JObject obj, string name) => obj[name] ?? Fail<JToken>("A mandatory field is absent: " + name, obj, "RequiredField");
        private static string Text(JToken token) => token?.Type == JTokenType.String ? (string)token : Fail<string>("A string is required.", token);
        private static string Text(JObject obj, string name) => Text(Required(obj, name));
        private static double Number(JToken token)
        {
            if (token == null || (token.Type != JTokenType.Integer && token.Type != JTokenType.Float))
                return Fail<double>("A finite JSON number is required.", token);
            var value = (double)token;
            return ModelValidator.IsFinite(value) ? value : Fail<double>("Numbers must be finite.", token, "NonFiniteNumber");
        }
        private static double? OptionalNumber(JObject obj, string name) => obj[name] == null ? (double?)null : Number(obj[name]);
        private static bool Boolean(JToken token) => token?.Type == JTokenType.Boolean ? (bool)token : Fail<bool>("A boolean is required.", token);
        private static string[] Strings(JToken token) => Array(token).Select(Text).ToArray();
        private static int Version(JObject obj)
        {
            var token = Required(obj, "schemaVersion");
            if (token.Type != JTokenType.Integer || Number(token) != 1)
                return Fail<int>("Only normalized schema version 1 is supported.", token, "UnsupportedSchema");
            return 1;
        }
        private static void Fields(JObject obj, params string[] allowed)
        {
            var names = new HashSet<string>(allowed, StringComparer.Ordinal);
            foreach (var property in obj.Properties())
                if (!names.Contains(property.Name)) Fail<object>("A field is not supported by this schema.", property, "UnsupportedField");
        }
        private static JObject Document(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return Fail<JObject>("A JSON document is required.", null);
            using (var input = new StringReader(json))
            using (var reader = new JsonTextReader(input) { DateParseHandling = DateParseHandling.None, MaxDepth = 128 })
            {
                var root = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                if (reader.Read()) return Fail<JObject>("Only one JSON document is allowed.", root);
                return root;
            }
        }
        private static Outcome<T> Read<T>(Func<Outcome<T>> action) where T : class
        {
            try { return action(); }
            catch (InvalidInput error) { return Outcome<T>.Failure(new[] { error.Diagnostic }); }
            // Parser exception text can contain profile values. Do not expose it in diagnostics.
            catch (JsonException) { return Outcome<T>.Failure(new[] { new Diagnostic("MalformedJson", "The JSON document is malformed or contains duplicate fields.") }); }
            catch (OverflowException) { return Outcome<T>.Failure(new[] { new Diagnostic("NonFiniteNumber", "A number cannot be represented as a finite value.") }); }
        }

        public static Outcome<ModelDefinition> ReadModel(string json) => Read(() =>
        {
            var root = Document(json);
            Fields(root, "schemaVersion", "modelId", "provenance", "objective", "outcomeLabel", "baseScore", "features", "trees");
            var version = Version(root);
            var features = Object(Required(root, "features")).Properties().Select(property =>
            {
                var f = Object(property.Value);
                var type = Text(f, "type");
                if (type != "number" && type != "category") return Fail<FeatureDefinition>("The feature type is unsupported.", f, "InvalidFeatureType");
                if (type == "number") Fields(f, "type", "displayName", "allowMissing", "integer", "minimum", "maximum");
                else Fields(f, "type", "displayName", "allowMissing", "values");
                return new FeatureDefinition(property.Name, type == "number" ? FeatureKind.Number : FeatureKind.Category,
                    Boolean(Required(f, "allowMissing")), f["displayName"] == null ? null : Text(f["displayName"]),
                    f["integer"] != null && Boolean(f["integer"]), OptionalNumber(f, "minimum"), OptionalNumber(f, "maximum"),
                    type == "category" ? Strings(Required(f, "values")) : null);
            }).ToArray();
            var trees = Array(Required(root, "trees")).Select(token =>
            {
                var tree = Object(token); Fields(tree, "id", "rootId", "weight", "nodes");
                var nodes = Array(Required(tree, "nodes")).Select(ReadNode).ToArray();
                return new ModelTree(Text(tree, "id"), Text(tree, "rootId"), Number(Required(tree, "weight")), nodes);
            }).ToArray();
            var model = new ModelDefinition(version, Text(root, "modelId"), Text(root, "provenance"), Text(root, "objective"),
                Text(root, "outcomeLabel"), Number(Required(root, "baseScore")), features, trees);
            var errors = ModelValidator.Validate(model);
            return errors.Count == 0 ? Outcome<ModelDefinition>.Success(model) : Outcome<ModelDefinition>.Failure(errors);
        });

        private static ModelNode ReadNode(JToken token)
        {
            var node = Object(token); var kind = Text(node, "kind"); var id = Text(node, "id");
            if (kind == "leaf")
            {
                Fields(node, "id", "kind", "score"); return new LeafNode(id, Number(Required(node, "score")));
            }
            if (kind != "split") return Fail<ModelNode>("Only split and leaf nodes are supported.", node, "UnsupportedNode");
            var op = Text(node, "operator");
            var allowed = new List<string> { "id", "kind", "feature", "operator", "trueChild", "falseChild" };
            SplitCondition condition;
            switch (op)
            {
                case "lt": allowed.Add("threshold"); condition = new SplitCondition(DecisionOperator.LessThan, Number(Required(node, "threshold"))); break;
                case "in": allowed.Add("values"); condition = new SplitCondition(DecisionOperator.In, categories: Strings(Required(node, "values"))); break;
                case "is_missing": condition = new SplitCondition(DecisionOperator.IsMissing); break;
                default: return Fail<ModelNode>("The split operator is unsupported.", node, "UnsupportedOperator");
            }
            Fields(node, allowed.ToArray());
            return new SplitNode(id, Text(node, "feature"), condition, Text(node, "trueChild"), Text(node, "falseChild"));
        }

        public static Outcome<ProfileSet> ReadProfiles(string json, ModelDefinition model) => Read(() =>
        {
            var modelErrors = ModelValidator.Validate(model);
            if (modelErrors.Count != 0) return Outcome<ProfileSet>.Failure(modelErrors);
            var root = Document(json); Fields(root, "schemaVersion", "modelId", "profiles");
            var version = Version(root); var modelId = Text(root, "modelId");
            if (modelId != model.Id) return Fail<Outcome<ProfileSet>>("Profiles belong to a different model.", root, "ModelMismatch");
            var profiles = new List<PreparedProfile>(); var errors = new List<Diagnostic>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var token in Array(Required(root, "profiles")))
            {
                var obj = Object(token); Fields(obj, "id", "displayName", "values");
                var values = Object(Required(obj, "values")).Properties().Select(p => new KeyValuePair<string, ProfileValue>(p.Name, ReadValue(p.Value)));
                var profile = new PreparedProfile(version, modelId, Text(obj, "id"), Text(obj, "displayName"), values);
                if (!ids.Add(profile.Id)) errors.Add(new Diagnostic("DuplicateProfileId", "Profile IDs must be unique.", obj.Path, modelId));
                errors.AddRange(ModelValidator.ValidateProfile(model, profile)); profiles.Add(profile);
            }
            if (profiles.Count == 0) errors.Add(new Diagnostic("ProfilesRequired", "At least one prepared profile is required.", root.Path, modelId));
            return errors.Count == 0 ? Outcome<ProfileSet>.Success(new ProfileSet(version, modelId, profiles)) : Outcome<ProfileSet>.Failure(errors);
        });

        private static ProfileValue ReadValue(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Null: return ProfileValue.Missing;
                case JTokenType.Integer: case JTokenType.Float: return ProfileValue.FromNumber(Number(token));
                case JTokenType.String: return ProfileValue.FromCategory(Text(token));
                default: return Fail<ProfileValue>("A profile value must be a number, category string, or explicit null.", token, "InvalidProfileValue");
            }
        }

        public static Outcome<string> WriteModel(ModelDefinition model)
        {
            if (model != null && model.StructureOnlyPreview) return Outcome<string>.Failure(new[] { new Diagnostic("PreviewNotInterchange", "An export structure preview cannot be serialized as a verified normalized feature contract.") });
            var errors = ModelValidator.Validate(model);
            if (errors.Count != 0) return Outcome<string>.Failure(errors);
            var features = new JObject();
            foreach (var f in model.Features)
            {
                var obj = new JObject { ["type"] = f.Kind == FeatureKind.Number ? "number" : "category", ["displayName"] = f.DisplayName, ["allowMissing"] = f.AllowMissing };
                if (f.Kind == FeatureKind.Category) obj["values"] = new JArray(f.Categories);
                else
                {
                    obj["integer"] = f.Integer;
                    if (f.Minimum.HasValue) obj["minimum"] = f.Minimum.Value;
                    if (f.Maximum.HasValue) obj["maximum"] = f.Maximum.Value;
                }
                features[f.Id] = obj;
            }
            var trees = new JArray(model.Trees.Select(t => new JObject { ["id"] = t.Id, ["rootId"] = t.RootId,
                ["weight"] = t.Weight, ["nodes"] = new JArray(t.Nodes.Select(WriteNode)) }));
            return Outcome<string>.Success(new JObject { ["schemaVersion"] = model.SchemaVersion, ["modelId"] = model.Id,
                ["provenance"] = model.Provenance, ["objective"] = model.Objective, ["outcomeLabel"] = model.OutcomeLabel,
                ["baseScore"] = model.BaseScore, ["features"] = features, ["trees"] = trees }.ToString(Formatting.Indented));
        }

        private static JObject WriteNode(ModelNode node)
        {
            if (node is LeafNode leaf) return new JObject { ["id"] = leaf.Id, ["kind"] = "leaf", ["score"] = leaf.Score };
            var split = (SplitNode)node; var c = split.Condition;
            var obj = new JObject { ["id"] = split.Id, ["kind"] = "split", ["feature"] = split.FeatureId,
                ["operator"] = c.Operator == DecisionOperator.LessThan ? "lt" : c.Operator == DecisionOperator.In ? "in" : "is_missing",
                ["trueChild"] = split.TrueChild, ["falseChild"] = split.FalseChild };
            if (c.Threshold.HasValue) obj["threshold"] = c.Threshold.Value;
            if (c.Operator == DecisionOperator.In) obj["values"] = new JArray(c.Categories);
            return obj;
        }

        public static Outcome<string> WriteProfiles(ProfileSet set, ModelDefinition model)
        {
            if (set == null) return Outcome<string>.Failure(new[] { new Diagnostic("ProfilesRequired", "A profile set is required.") });
            var errors = ModelValidator.Validate(model);
            if (errors.Count != 0) return Outcome<string>.Failure(errors);
            if (set.Profiles.Any(p => p == null || p.SchemaVersion != set.SchemaVersion || p.ModelId != set.ModelId))
                return Outcome<string>.Failure(new[] { new Diagnostic("ModelMismatch", "All profiles must share the set schema and model ID.") });
            var profiles = new JArray();
            foreach (var profile in set.Profiles)
            {
                errors = ModelValidator.ValidateProfile(model, profile);
                if (errors.Count != 0) return Outcome<string>.Failure(errors);
                var values = new JObject();
                foreach (var pair in profile.Values)
                {
                    var v = pair.Value;
                    if (v.Kind == ValueKind.NotSupplied) continue;
                    values[pair.Key] = v.Kind == ValueKind.Missing ? JValue.CreateNull() :
                        v.Kind == ValueKind.Number ? new JValue(v.Number) : new JValue(v.Category);
                }
                profiles.Add(new JObject { ["id"] = profile.Id, ["displayName"] = profile.DisplayName, ["values"] = values });
            }
            var json = new JObject { ["schemaVersion"] = set.SchemaVersion, ["modelId"] = set.ModelId, ["profiles"] = profiles }.ToString(Formatting.Indented);
            var validation = ReadProfiles(json, model);
            return validation.IsSuccess ? Outcome<string>.Success(json) : Outcome<string>.Failure(validation.Diagnostics);
        }
    }
}
