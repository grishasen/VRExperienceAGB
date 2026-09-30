using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Import
{
    /// <summary>Optional monitoring evidence. Missing or invalid values never become zero or a health verdict.</summary>
    public sealed class ExportMonitoring
    {
        public string Version { get; private set; }
        public string Updated { get; private set; }
        public double? Total { get; private set; }
        public double? Positive { get; private set; }
        public double? Negative { get; private set; }
        public double? Auc { get; private set; }
        public bool CountsReconcile => Total.HasValue && Positive.HasValue && Negative.HasValue && Total == Positive + Negative;
        private static double? Number(JToken token, bool count = false)
        {
            if (token == null || (token.Type != JTokenType.Integer && token.Type != JTokenType.Float)) return null;
            double value = (double)token;
            return ModelValidator.IsFinite(value) && value >= 0 && (!count || value == Math.Truncate(value)) ? value : (double?)null;
        }
        internal static ExportMonitoring Read(JObject root)
        {
            var counts = root["trainingStats"] as JObject;
            double? auc = Number(root["auc"]);
            return new ExportMonitoring {
                Version = root["modelVersion"]?.Type == JTokenType.String ? (string)root["modelVersion"] : null,
                Updated = root["factoryUpdateTime"]?.Type == JTokenType.String ? (string)root["factoryUpdateTime"] : null,
                Total = Number(counts?["totalCount"], true), Positive = Number(counts?["positiveCount"], true),
                Negative = Number(counts?["negativeCount"], true), Auc = auc <= 1 ? auc : null
            };
        }
    }

    public sealed class ExplainedTree
    {
        public ModelTree Tree { get; }
        public IReadOnlyDictionary<string, ModelNode> Nodes { get; }
        public IReadOnlyDictionary<string, double> FamilyGains { get; }
        public double? Gain { get; }
        public double MinimumLeaf { get; }
        public double MaximumLeaf { get; }
        public int Depth { get; }
        public int Leaves { get; }
        internal readonly Dictionary<string, (SplitNode parent, bool matched)> Parents = new Dictionary<string, (SplitNode, bool)>();
        internal ExplainedTree(ModelTree tree, AgbPreviewResult export)
        {
            Tree = tree; Nodes = tree.Nodes.ToDictionary(n => n.Id, StringComparer.Ordinal);
            var families = ForestExplanation.Families.ToDictionary(f => f, f => 0d);
            bool complete = export != null;
            double gain = 0;
            foreach (var split in tree.Nodes.OfType<SplitNode>())
            {
                Parents.Add(split.TrueChild, (split, true)); Parents.Add(split.FalseChild, (split, false));
                if (export != null && export.Metadata.TryGetValue(tree.Id + "/" + split.Id, out var data) && data.Gain >= 0)
                { gain += data.Gain; families[ForestExplanation.Family(split.FeatureId)] += data.Gain; }
                else complete = false;
            }
            Gain = complete && ModelValidator.IsFinite(gain) ? gain : (double?)null;
            FamilyGains = new System.Collections.ObjectModel.ReadOnlyDictionary<string, double>(families);
            var leaves = tree.Nodes.OfType<LeafNode>().ToArray(); Leaves = leaves.Length;
            MinimumLeaf = leaves.Min(l => l.Score * tree.Weight); MaximumLeaf = leaves.Max(l => l.Score * tree.Weight);
            var pending = new Stack<(string id, int depth)>(); pending.Push((tree.RootId, 0));
            while (pending.Count > 0) {
                var item = pending.Pop(); Depth = Math.Max(Depth, item.depth);
                if (Nodes[item.id] is SplitNode split) { pending.Push((split.TrueChild, item.depth + 1)); pending.Push((split.FalseChild, item.depth + 1)); }
            }
        }
    }

    /// <summary>Cached, read-only descriptions over every tree. No Unity objects, scoring, or route mutations.</summary>
    public sealed class ForestExplanation
    {
        public static readonly string[] Families = { "History", "Customer", "Context", "Other" };
        public ModelDefinition Model { get; }
        public IReadOnlyList<ExplainedTree> Trees { get; }
        public double? TotalGain { get; }
        private readonly AgbPreviewResult export;
        private readonly Dictionary<string, FeatureDefinition> features;
        private readonly Dictionary<string, List<(int tree, SplitNode node)>> occurrences;
        private readonly Dictionary<string, double[]> thresholds;
        public static string Number(double value) => value.ToString("G5", CultureInfo.InvariantCulture);
        public static string Family(string id) => id.StartsWith("IH.", StringComparison.Ordinal) ? "History" :
            id.StartsWith("Customer.", StringComparison.Ordinal) ? "Customer" : id.StartsWith("py", StringComparison.Ordinal) || id.StartsWith("Param.", StringComparison.Ordinal) ? "Context" : "Other";
        public ForestExplanation(ModelDefinition model, AgbPreviewResult preview = null)
        {
            Model = model; export = preview?.Model == model ? preview : null;
            features = model.Features.ToDictionary(f => f.Id, StringComparer.Ordinal);
            Trees = Array.AsReadOnly(model.Trees.Select(t => new ExplainedTree(t, export)).ToArray());
            double sum = Trees.Sum(t => t.Gain ?? 0);
            TotalGain = Trees.All(t => t.Gain.HasValue) && ModelValidator.IsFinite(sum) ? sum : (double?)null;
            occurrences = new Dictionary<string, List<(int, SplitNode)>>(StringComparer.Ordinal);
            for (int i = 0; i < Trees.Count; i++) foreach (var node in Trees[i].Tree.Nodes.OfType<SplitNode>()) {
                if (!occurrences.TryGetValue(node.FeatureId, out var list)) occurrences.Add(node.FeatureId, list = new List<(int, SplitNode)>());
                list.Add((i, node));
            }
            thresholds = occurrences.ToDictionary(p => p.Key, p => p.Value.Where(x => x.node.Condition.Operator == DecisionOperator.LessThan)
                .Select(x => x.node.Condition.Threshold.Value).Distinct().OrderBy(v => v).ToArray(), StringComparer.Ordinal);
        }
        public string Label(string feature) => features[feature].DisplayName;
        public double[] Thresholds(string feature) => thresholds.TryGetValue(feature, out var result) ? (double[])result.Clone() : Array.Empty<double>();
        public int Occurrences(string feature) => occurrences.TryGetValue(feature, out var list) ? list.Count : 0;
        public int[] MatchingTrees(string feature) => occurrences.TryGetValue(feature, out var list) ? list.Select(x => x.tree).Distinct().ToArray() : Array.Empty<int>();
        public string Question(SplitNode node) => Branch(node, true) + "?";
        public string Branch(SplitNode node, bool matched, bool full = false)
        {
            string label = Label(node.FeatureId); var c = node.Condition;
            if (c.Operator == DecisionOperator.IsMissing) return label + (matched ? " is missing" : " is present");
            if (c.Operator == DecisionOperator.LessThan) return label + (matched ? " is less than " : " is at least ") + Number(c.Threshold.Value) +
                (string.IsNullOrWhiteSpace(features[node.FeatureId].Unit) ? "" : " " + features[node.FeatureId].Unit);
            return label + (matched ? " is in " : " is outside ") + (full || c.Categories.Count <= 3 ? "{" + string.Join(", ", c.Categories) + "}" : "set of " + c.Categories.Count + " values (point for list)");
        }
        public string Kind(SplitNode node) => features[node.FeatureId].Kind +
            (features[node.FeatureId].Kind == FeatureKind.Number ? " · " + (features[node.FeatureId].Unit ?? "unit not supplied") : "") +
            (Model.StructureOnlyPreview ? " · label inferred from identifier" : " · supplied feature dictionary");
        public string NodeEvidence(int index, string id)
        {
            var tree = Trees[index];
            if (export == null || !export.Metadata.TryGetValue(tree.Tree.Id + "/" + id, out var data)) return "Gain and observation count not supplied";
            string gain = data.Gain < 0 ? "Gain share unavailable: negative source gain" : "Stored gain " + Number(data.Gain) +
                (tree.Gain > 0 ? " · " + Number(100 * data.Gain / tree.Gain.Value) + "% of tree gain" : " · gain share unavailable");
            return gain + "\nExport sampleCount " + data.SampleCount.ToString(CultureInfo.InvariantCulture) +
                (data.SampleCount < 100 ? " · REVIEW: below 100 observations" : "") +
                "\nCount is not a traffic share or observed positives. Node age unverified.";
        }
        public string NodeDetail(int index, string id)
        {
            var tree = Trees[index]; var node = tree.Nodes[id];
            string text = "SOURCE " + tree.Tree.Id + "/" + id + "\n";
            if (node is SplitNode split) {
                text += split.FeatureId + "\n" + Kind(split) + "\nTRUE: " + Branch(split, true, true) + "\nFALSE: " + Branch(split, false, true) +
                    "\nUsed " + tree.Tree.Nodes.OfType<SplitNode>().Count(n => n.FeatureId == split.FeatureId) + " times here; " + Occurrences(split.FeatureId) + " splits in " + MatchingTrees(split.FeatureId).Length + " trees.";
                var cuts = Thresholds(split.FeatureId);
                if (cuts.Length > 0) text += "\nEnsemble thresholds: " + string.Join(" · ", cuts.Select(Number)) + "\nThresholds describe cuts, not a proven memory horizon.";
                if (split.Condition.Operator == DecisionOperator.IsMissing) text += "\n[MISSING] Explicit absence check. Absence does not establish a new customer.";
            } else text += "Leaf contribution " + Number(((LeafNode)node).Score * tree.Tree.Weight) + " (raw score)";
            if (export != null && export.Metadata.TryGetValue(tree.Tree.Id + "/" + id, out var source) && source.SplitText != null)
                text += "\nOriginal condition: " + source.SplitText;
            return text + "\n\n" + NodeEvidence(index, id) + "\nGain is structural evidence; not SHAP, causality or an individual effect.\n\nSEGMENT\n" + Segment(index, id);
        }
        public string Segment(int index, string id)
        {
            var tree = Trees[index]; var path = new List<(SplitNode node, bool matched)>();
            while (tree.Parents.TryGetValue(id, out var entry)) { path.Add((entry.parent, entry.matched)); id = entry.parent.Id; }
            path.Reverse();
            if (path.Count == 0) return "At root · no conditions yet";
            var lines = new List<string>();
            foreach (var group in path.GroupBy(p => p.node.FeatureId)) {
                var numeric = group.Where(p => p.node.Condition.Operator == DecisionOperator.LessThan).ToArray();
                if (numeric.Length > 0) {
                    double lower = numeric.Where(p => !p.matched).Select(p => p.node.Condition.Threshold.Value).DefaultIfEmpty(double.NegativeInfinity).Max();
                    double upper = numeric.Where(p => p.matched).Select(p => p.node.Condition.Threshold.Value).DefaultIfEmpty(double.PositiveInfinity).Min();
                    lines.Add((lower >= upper ? "CONFLICT: " : "") + (double.IsNegativeInfinity(lower) ? "" : Number(lower) + " <= ") + Label(group.Key) +
                        (double.IsPositiveInfinity(upper) ? " (no upper bound)" : " < " + Number(upper)));
                }
                lines.AddRange(group.Where(p => p.node.Condition.Operator != DecisionOperator.LessThan).Select(p => Branch(p.node, p.matched, true)));
            }
            return string.Join("\n", lines);
        }
        public string Passport(int index)
        {
            var t = Trees[index];
            var names = t.Tree.Nodes.OfType<SplitNode>().GroupBy(n => n.FeatureId).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal).Take(3).Select(g => Label(g.Key));
            string root = t.Nodes[t.Tree.RootId] is SplitNode split ? "First: " + Question(split) : "Single leaf; no split";
            return "TREE " + (index + 1) + " · source index " + index + "\nDepth " + t.Depth + " · " + t.Leaves + " leaves · " + t.Tree.Nodes.Count + " nodes\n" +
                (t.Gain.HasValue ? "Stored gain " + Number(t.Gain.Value) + (TotalGain > 0 ? " · " + Number(t.Gain.Value * 100 / TotalGain.Value) + "% of ensemble" : " · share unavailable") : "Gain not supplied") +
                "\nLeaf contributions " + Number(t.MinimumLeaf) + " to " + Number(t.MaximumLeaf) + "\n" + root + "\nMost repeated: " + (names.Any() ? string.Join(", ", names) : "none");
        }
        private string cachedStory;
        public string ModelStory() => cachedStory ?? (cachedStory = BuildModelStory());
        private string BuildModelStory()
        {
            var m = export?.Monitoring;
            string observations = m == null ? "Monitoring not supplied" : "Observations " + (m.Total.HasValue ? Number(m.Total.Value) : "not supplied") +
                " · positive " + (m.Positive.HasValue ? Number(m.Positive.Value) : "not supplied") + "\n" +
                (m.CountsReconcile && m.Total > 0 ? "Positive rate " + Number(100 * m.Positive.Value / m.Total.Value) + "%" : "Monitoring counts unavailable or inconsistent") +
                " · AUC " + (m.Auc.HasValue ? Number(m.Auc.Value) : "not supplied");
            var families = Families.Select(f => f + " " + Model.Trees.SelectMany(t => t.Nodes.OfType<SplitNode>()).Count(n => Family(n.FeatureId) == f));
            return "ABOUT THIS FOREST\n" + Trees.Count + " trees · " + occurrences.Count + " used predictors\nVersion: " + (m?.Version ?? "not supplied") +
                "\nExport update: " + (m?.Updated ?? "not supplied") + "\n" + observations + "\nSplits by identifier family: " + string.Join(" · ", families) +
                "\nThe forest asks " + (occurrences.Count == 0 ? "no split questions." : "most often about " + string.Join(", ", occurrences.OrderByDescending(p => p.Value.Count).ThenBy(p => p.Key, StringComparer.Ordinal).Take(3).Select(p => Label(p.Key))) + ".") +
                "\nHeight = depth · crown width = leaves\nBase strip = family share of stored gain (H / C / X / O)\nHistory / Customer / Context / Other · families inferred from identifiers\nRing + / - = reached leaf contribution\nTree numbers preserve export order; they are not dates.\n" +
                (Model.StructureOnlyPreview ? "Structure preview · scoring unverified" : "Normalized teaching model · supplied scoring contract");
        }
    }
}
