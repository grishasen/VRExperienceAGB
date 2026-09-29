using System;
using System.Collections.Generic;
using System.Linq;

namespace VRExperienceAGB.Domain
{
    public static class ModelValidator
    {
        public static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static bool HasId(string value) => !string.IsNullOrWhiteSpace(value);

        public static IReadOnlyList<Diagnostic> Validate(ModelDefinition model)
        {
            var errors = new List<Diagnostic>();
            if (model == null) return Snapshot.List(new[] { new Diagnostic("ModelRequired", "A model is required.") });
            void Error(string code, string message, string tree = null, string node = null, string feature = null) =>
                errors.Add(new Diagnostic(code, message, modelId: model.Id, treeId: tree, nodeId: node, featureId: feature));
            if (model.SchemaVersion != 1) Error("UnsupportedSchema", "Only normalized schema version 1 is supported.");
            if (!HasId(model.Id)) Error("InvalidModelId", "A nonempty model ID is required.");
            if (!HasId(model.Provenance)) Error("ProvenanceRequired", "Explicit provenance is required.");
            if (!HasId(model.OutcomeLabel)) Error("OutcomeRequired", "A named outcome is required.");
            if (model.Objective != "binary_logistic") Error("UnsupportedObjective", "Only the synthetic binary logistic contract is supported.");
            if (!IsFinite(model.BaseScore)) Error("InvalidBaseline", "The raw-score baseline must be finite.");
            if (model.Features.Count == 0) Error("FeaturesRequired", "At least one feature definition is required.");
            var features = new Dictionary<string, FeatureDefinition>(StringComparer.Ordinal);
            foreach (var f in model.Features)
            {
                if (f == null || !HasId(f.Id)) { Error("InvalidFeatureId", "Every feature requires a nonempty ID."); continue; }
                if (features.ContainsKey(f.Id)) { Error("DuplicateFeatureId", "Feature IDs must be unique.", feature: f.Id); continue; }
                features.Add(f.Id, f);
                if (!Enum.IsDefined(typeof(FeatureKind), f.Kind)) Error("InvalidFeatureType", "Unsupported feature type.", feature: f.Id);
                if (f.Kind == FeatureKind.Number)
                {
                    if ((f.Minimum.HasValue && !IsFinite(f.Minimum.Value)) || (f.Maximum.HasValue && !IsFinite(f.Maximum.Value)) ||
                        (f.Minimum.HasValue && f.Maximum.HasValue && f.Minimum.Value > f.Maximum.Value))
                        Error("InvalidFeatureRange", "Numeric bounds must be finite and ordered.", feature: f.Id);
                    if (f.Categories.Count != 0) Error("InvalidFeatureType", "Numeric features cannot declare categories.", feature: f.Id);
                }
                else if (f.Kind == FeatureKind.Category)
                {
                    if (f.Categories.Count == 0 || f.Categories.Any(x => x == null) || f.Categories.Distinct(StringComparer.Ordinal).Count() != f.Categories.Count)
                        Error("InvalidCategoryDomain", "Category domains must be nonempty, unique, and contain strings.", feature: f.Id);
                    if (f.Integer || f.Minimum.HasValue || f.Maximum.HasValue)
                        Error("InvalidFeatureType", "Categorical features cannot declare numeric constraints.", feature: f.Id);
                }
            }
            if (model.Trees.Count == 0) Error("TreesRequired", "The model must contain trees.");
            var treeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var tree in model.Trees)
            {
                if (tree == null || !HasId(tree.Id)) { Error("InvalidTreeId", "Every tree requires a nonempty ID."); continue; }
                if (!treeIds.Add(tree.Id)) Error("DuplicateTreeId", "Tree IDs must be unique.", tree.Id);
                if (!IsFinite(tree.Weight)) Error("InvalidWeight", "Tree weight must be finite.", tree.Id);
                var nodes = new Dictionary<string, ModelNode>(StringComparer.Ordinal);
                foreach (var node in tree.Nodes)
                {
                    if (node == null || !HasId(node.Id)) { Error("InvalidNodeId", "Every node requires a nonempty ID.", tree.Id); continue; }
                    if (nodes.ContainsKey(node.Id)) { Error("DuplicateNodeId", "Node IDs must be unique within a tree.", tree.Id, node.Id); continue; }
                    nodes.Add(node.Id, node);
                    if (node is LeafNode leaf)
                    {
                        if (!IsFinite(leaf.Score)) Error("InvalidLeafScore", "Leaf scores must be finite.", tree.Id, node.Id);
                    }
                    else if (node is SplitNode split)
                    {
                        if (split.FeatureId == null || !features.TryGetValue(split.FeatureId, out var feature))
                        { Error("UnknownFeature", "The split refers to an undeclared feature.", tree.Id, node.Id, split.FeatureId); continue; }
                        var c = split.Condition;
                        if (c == null || !Enum.IsDefined(typeof(DecisionOperator), c.Operator))
                        { Error("UnsupportedOperator", "The split operator is unsupported.", tree.Id, node.Id, feature.Id); continue; }
                        if (c.Operator == DecisionOperator.LessThan &&
                            (feature.Kind != FeatureKind.Number || !c.Threshold.HasValue || !IsFinite(c.Threshold.Value) || c.Categories.Count != 0))
                            Error("InvalidNumericSplit", "Less-than requires a numeric feature and finite threshold.", tree.Id, node.Id, feature.Id);
                        if (c.Operator == DecisionOperator.In &&
                            (feature.Kind != FeatureKind.Category || c.Threshold.HasValue || c.Categories.Count == 0 ||
                             c.Categories.Any(x => x == null || !feature.Categories.Contains(x, StringComparer.Ordinal)) ||
                             c.Categories.Distinct(StringComparer.Ordinal).Count() != c.Categories.Count))
                            Error("InvalidMembershipSplit", "Membership requires unique categories from the declared domain.", tree.Id, node.Id, feature.Id);
                        if (c.Operator == DecisionOperator.IsMissing && (!feature.AllowMissing || c.Threshold.HasValue || c.Categories.Count != 0))
                            Error("InvalidMissingSplit", "A missing predicate requires an explicit missing-value policy and no operands.", tree.Id, node.Id, feature.Id);
                    }
                    else Error("UnsupportedNode", "Only split and leaf nodes are supported.", tree.Id, node.Id);
                }
                if (!HasId(tree.RootId) || !nodes.ContainsKey(tree.RootId)) Error("InvalidRoot", "Root must identify a node in this tree.", tree.Id);
                var parents = nodes.Keys.ToDictionary(id => id, _ => 0, StringComparer.Ordinal);
                foreach (var split in nodes.Values.OfType<SplitNode>())
                foreach (var child in new[] { split.TrueChild, split.FalseChild })
                {
                    if (child == null || !nodes.ContainsKey(child)) Error("MissingChild", "Split child does not exist in this tree.", tree.Id, split.Id);
                    else if (++parents[child] > 1) Error("MultipleParents", "A tree node cannot have multiple incoming branches.", tree.Id, child);
                }
                if (tree.RootId != null && parents.TryGetValue(tree.RootId, out var incoming) && incoming != 0)
                    Error("RootHasParent", "The root cannot have a parent.", tree.Id, tree.RootId);
                if (!nodes.Values.Any(n => n is LeafNode)) Error("NoTerminalRoute", "A tree must contain a terminal leaf.", tree.Id);

                // Iterative traversal handles deep exports without consuming the call stack.
                var colors = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var start in nodes.Keys)
                {
                    if (colors.ContainsKey(start)) continue;
                    var stack = new Stack<(string id, bool exiting)>(); stack.Push((start, false));
                    while (stack.Count != 0)
                    {
                        var item = stack.Pop();
                        if (item.exiting) { colors[item.id] = 2; continue; }
                        if (colors.TryGetValue(item.id, out var color))
                        {
                            if (color == 1) Error("Cycle", "The tree contains a cycle.", tree.Id, item.id);
                            continue;
                        }
                        colors[item.id] = 1; stack.Push((item.id, true));
                        if (nodes[item.id] is SplitNode split)
                        foreach (var child in new[] { split.FalseChild, split.TrueChild })
                            if (child != null && nodes.ContainsKey(child)) stack.Push((child, false));
                    }
                }
                if (tree.RootId != null && nodes.ContainsKey(tree.RootId))
                {
                    var reachable = new HashSet<string>(StringComparer.Ordinal);
                    var pending = new Stack<string>(); pending.Push(tree.RootId);
                    while (pending.Count != 0)
                    {
                        var id = pending.Pop(); if (!reachable.Add(id)) continue;
                        if (nodes[id] is SplitNode split)
                        foreach (var child in new[] { split.TrueChild, split.FalseChild })
                            if (child != null && nodes.ContainsKey(child)) pending.Push(child);
                    }
                    foreach (var id in nodes.Keys.Where(id => !reachable.Contains(id)))
                        Error("UnreachableNode", "Every declared node must be reachable from the root.", tree.Id, id);
                }
            }
            return Snapshot.List(errors);
        }

        /// <summary>Validates both inputs without returning or exposing profile values.</summary>
        public static IReadOnlyList<Diagnostic> ValidateProfile(ModelDefinition model, PreparedProfile profile)
        {
            var modelErrors = Validate(model);
            return modelErrors.Count != 0 ? modelErrors : ValidateProfileValues(model, profile);
        }

        /// <summary>Call only after model validation.</summary>
        internal static IReadOnlyList<Diagnostic> ValidateProfileValues(ModelDefinition model, PreparedProfile profile)
        {
            var errors = new List<Diagnostic>();
            void Error(string code, string message, string feature = null) => errors.Add(new Diagnostic(code, message,
                modelId: model.Id, featureId: feature));
            if (profile == null) { Error("ProfileRequired", "A prepared profile is required."); return Snapshot.List(errors); }
            if (profile.SchemaVersion != 1) Error("UnsupportedSchema", "Only profile schema version 1 is supported.");
            if (profile.ModelId != model.Id) Error("ModelMismatch", "The profile belongs to a different model.");
            if (!HasId(profile.Id)) Error("InvalidProfileId", "A nonempty profile ID is required.");
            if (!HasId(profile.DisplayName)) Error("ProfileNameRequired", "A profile display name is required.");
            var ids = new HashSet<string>(model.Features.Select(f => f.Id), StringComparer.Ordinal);
            foreach (var id in profile.Values.Keys.Where(id => !ids.Contains(id))) Error("UnknownFeature", "The profile contains an undeclared feature.", id);
            foreach (var f in model.Features)
            {
                var value = profile.GetValue(f.Id);
                if (value.IsMissing)
                {
                    if (!f.AllowMissing) Error("MissingRequiredValue", "A required feature value has not been supplied.", f.Id);
                }
                else if (f.Kind == FeatureKind.Number)
                {
                    if (value.Kind != ValueKind.Number || !IsFinite(value.Number)) Error("InvalidNumericValue", "The feature requires a finite number.", f.Id);
                    else if (f.Integer && Math.Truncate(value.Number) != value.Number) Error("IntegerRequired", "The feature requires an integer.", f.Id);
                    else if ((f.Minimum.HasValue && value.Number < f.Minimum.Value) || (f.Maximum.HasValue && value.Number > f.Maximum.Value))
                        Error("ValueOutOfRange", "The feature value lies outside its declared range.", f.Id);
                }
                else if (value.Kind != ValueKind.Category || !f.Categories.Contains(value.Category, StringComparer.Ordinal))
                    Error("InvalidCategoryValue", "The feature requires an exact member of its category domain.", f.Id);
            }
            return Snapshot.List(errors);
        }
    }
}
