using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Import
{
    public sealed class ExportNodeMetadata
    {
        public string SourceAddress { get; }
        public string SplitText { get; }
        public double Score { get; }
        public double Gain { get; }
        public long SampleCount { get; }
        internal ExportNodeMetadata(string address, string split, double score, double gain, long count)
        { SourceAddress = address; SplitText = split; Score = score; Gain = gain; SampleCount = count; }
    }

    public sealed class AgbPreviewResult
    {
        public ModelDefinition Model { get; }
        public IReadOnlyDictionary<string, ExportNodeMetadata> Metadata { get; }
        public string Sha256 { get; }
        internal AgbPreviewResult(ModelDefinition model, Dictionary<string, ExportNodeMetadata> metadata, string sha)
        { Model = model; Metadata = new System.Collections.ObjectModel.ReadOnlyDictionary<string, ExportNodeMetadata>(metadata); Sha256 = sha; }
    }

    /// <summary>Imports complete structure for manual inspection. Observed categories are not a feature dictionary; scoring is disabled.</summary>
    public static class AgbStructurePreview
    {
        private static readonly Regex Numeric = new Regex(@"^(.+?) < ([+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?)$");
        private static readonly Regex Membership = new Regex(@"^(.+?) in \{ (.+) \}$");
        private static readonly Regex Missing = new Regex(@"^(.+?) is Missing$");
        private static readonly Regex CategoryToken = new Regex(@"^[A-Za-z0-9_.:/+\-]+(?: [A-Za-z0-9_.:/+\-]+)*$");
        private sealed class PreviewError : Exception
        {
            internal string Address { get; }
            internal PreviewError(string message, string address) : base(message) { Address = address; }
        }
        private sealed class ObservedFeature
        {
            internal FeatureKind Kind = FeatureKind.Unknown;
            internal readonly HashSet<string> Categories = new HashSet<string>(StringComparer.Ordinal);
        }
        private static JObject Object(JToken token, string address) => token as JObject ?? throw new PreviewError("An object is required.", address);
        private static double Number(JObject obj, string field, string address)
        {
            var token = obj[field];
            if (token == null || (token.Type != JTokenType.Integer && token.Type != JTokenType.Float)) throw new PreviewError("A finite number is required: " + field, address);
            double value = (double)token;
            if (!ModelValidator.IsFinite(value)) throw new PreviewError("A finite number is required: " + field, address);
            return value;
        }
        public static Outcome<AgbPreviewResult> Read(string json, string displayName)
        {
            try
            {
                JObject root;
                using (var input = new StringReader(json ?? ""))
                using (var reader = new JsonTextReader(input) { DateParseHandling = DateParseHandling.None, MaxDepth = 256 })
                {
                    root = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                    if (reader.Read()) throw new PreviewError("Only one JSON document is permitted.", "root");
                }
                if ((string)root["type"] != "AdaptiveBoostScoringModel" || (string)root["algorithm"] != "GRADIENT_BOOST")
                    throw new PreviewError("Only the nested AdaptiveBoostScoringModel GRADIENT_BOOST structure is supported.", "root");
                var exports = Object(Object(root["model"], "model")["booster"], "model.booster")["trees"] as JArray;
                if (exports == null || exports.Count == 0) throw new PreviewError("A complete, nonempty tree array is required.", "model.booster.trees");
                var features = new Dictionary<string, ObservedFeature>(StringComparer.Ordinal);
                var metadata = new Dictionary<string, ExportNodeMetadata>(StringComparer.Ordinal);
                var trees = new List<ModelTree>();
                for (int index = 0; index < exports.Count; index++)
                {
                    string treeId = "tree[" + index + "]";
                    var nodes = new List<ModelNode>();
                    var pending = new Stack<(JToken token, string path)>(); pending.Push((exports[index], "root"));
                    while (pending.Count > 0)
                    {
                        var item = pending.Pop(); string address = treeId + "/" + item.path;
                        var node = Object(item.token, address);
                        var allowed = new HashSet<string>(new[] { "score", "gain", "sampleCount", "split", "left", "right" });
                        if (node.Properties().Any(p => !allowed.Contains(p.Name))) throw new PreviewError("Unsupported node fields; no partial import is permitted.", address);
                        double score = Number(node, "score", address), gain = Number(node, "gain", address), count = Number(node, "sampleCount", address);
                        if (count < 0 || count >= 9223372036854775808d || count != Math.Truncate(count)) throw new PreviewError("sampleCount must be a nonnegative integer.", address);
                        var splitToken = node["split"];
                        if (splitToken == null)
                        {
                            if (node["left"] != null || node["right"] != null || gain != 0) throw new PreviewError("A leaf must have no children and zero gain.", address);
                            nodes.Add(new LeafNode(item.path, score)); metadata.Add(address, new ExportNodeMetadata(address, null, score, gain, (long)count));
                            continue;
                        }
                        if (splitToken.Type != JTokenType.String) throw new PreviewError("A split must be text.", address);
                        string split = (string)splitToken, featureId; FeatureKind kind; SplitCondition condition;
                        var numeric = Numeric.Match(split); var membership = Membership.Match(split); var missing = Missing.Match(split);
                        if (numeric.Success)
                        {
                            featureId = numeric.Groups[1].Value; kind = FeatureKind.Number;
                            double threshold = double.Parse(numeric.Groups[2].Value, CultureInfo.InvariantCulture);
                            if (!ModelValidator.IsFinite(threshold)) throw new PreviewError("The threshold must be finite.", address);
                            condition = new SplitCondition(DecisionOperator.LessThan, threshold);
                        }
                        else if (membership.Success)
                        {
                            featureId = membership.Groups[1].Value; kind = FeatureKind.Category;
                            var categories = membership.Groups[2].Value.Split(',').Select(c => c.Trim()).ToArray();
                            if (categories.Any(c => !CategoryToken.IsMatch(c)) || categories.Distinct(StringComparer.Ordinal).Count() != categories.Length)
                                throw new PreviewError("Ambiguous or unsupported category grammar.", address);
                            condition = new SplitCondition(DecisionOperator.In, categories: categories);
                        }
                        else if (missing.Success)
                        { featureId = missing.Groups[1].Value; kind = FeatureKind.Unknown; condition = new SplitCondition(DecisionOperator.IsMissing); }
                        else throw new PreviewError("Unsupported split syntax; the complete import was rejected.", address);
                        if (!features.TryGetValue(featureId, out var observed)) features.Add(featureId, observed = new ObservedFeature());
                        if (kind != FeatureKind.Unknown)
                        {
                            if (observed.Kind != FeatureKind.Unknown && observed.Kind != kind) throw new PreviewError("A predictor mixes numeric and categorical operands.", address);
                            observed.Kind = kind;
                        }
                        observed.Categories.UnionWith(condition.Categories);
                        string left = item.path + "/left", right = item.path + "/right";
                        nodes.Add(new SplitNode(item.path, featureId, condition, left, right));
                        metadata.Add(address, new ExportNodeMetadata(address, split, score, gain, (long)count));
                        pending.Push((node["right"], right)); pending.Push((node["left"], left));
                    }
                    trees.Add(new ModelTree(treeId, "root", 1, nodes));
                }
                string sha;
                using (var hasher = SHA256.Create()) sha = BitConverter.ToString(hasher.ComputeHash(Encoding.UTF8.GetBytes(json))).Replace("-", "").ToLowerInvariant();
                var dictionary = features.Select(pair => new FeatureDefinition(pair.Key, pair.Value.Kind, true, Label(pair.Key),
                    categories: pair.Value.Kind == FeatureKind.Category ? pair.Value.Categories.OrderBy(c => c, StringComparer.Ordinal) : null));
                var model = new ModelDefinition(1, "agb-structure-" + sha.Substring(0, 12),
                    "Export structure preview: " + displayName + ". Feature domains, missing policies and source scoring are unverified.",
                    "binary_logistic", "Output unavailable in structure preview", 0, dictionary, trees, structureOnlyPreview: true);
                var errors = ModelValidator.Validate(model);
                return errors.Count == 0 ? Outcome<AgbPreviewResult>.Success(new AgbPreviewResult(model, metadata, sha)) : Outcome<AgbPreviewResult>.Failure(errors);
            }
            catch (PreviewError e) { return Outcome<AgbPreviewResult>.Failure(new[] { new Diagnostic("UnsupportedExportStructure", e.Message, e.Address) }); }
            catch (JsonException) { return Failure("MalformedExport", "The export contains malformed JSON or duplicate fields."); }
            catch (Exception e) when (e is OverflowException || e is FormatException || e is ArgumentException)
            { return Failure("MalformedExport", "The export contains an unsupported value or shape."); }
        }
        private static Outcome<AgbPreviewResult> Failure(string code, string message) => Outcome<AgbPreviewResult>.Failure(new[] { new Diagnostic(code, message) });
        private static string Label(string id)
        {
            var words = id.Split('.'); string label = words.Length > 2 && words[0] == "IH" ? string.Join(" ", words.Skip(1)) : words[words.Length - 1];
            return Regex.Replace(label, "([a-z0-9])([A-Z])", "$1 $2");
        }
    }
}
