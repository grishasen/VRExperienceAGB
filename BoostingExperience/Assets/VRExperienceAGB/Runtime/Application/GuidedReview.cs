using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Application
{
    public enum ReviewStopKind { Forest, Tree, Node, Profile, Comparison }
    [Serializable]
    public sealed class ReviewValue
    {
        public string feature;
        public ValueKind kind;
        public double number;
        public string category;
    }
    [Serializable]
    public sealed class ReviewProfile
    {
        public bool present;
        public string id, name;
        public ReviewValue[] values;
        public static ReviewProfile Capture(PreparedProfile p) => p == null ? null : new ReviewProfile {
            present = true, id = p.Id, name = p.DisplayName, values = p.Values.Select(v => new ReviewValue {
                feature = v.Key, kind = v.Value.Kind, number = v.Value.Number, category = v.Value.Category }).ToArray() };
        public PreparedProfile Restore(string modelId)
        {
            if (values == null || values.Any(v => v == null || string.IsNullOrEmpty(v.feature) || !Enum.IsDefined(typeof(ValueKind), v.kind)))
                throw new ArgumentException("Invalid saved profile values.");
            return new PreparedProfile(1, modelId, id, name, values.Select(v => new KeyValuePair<string, ProfileValue>(v.feature,
                v.kind == ValueKind.Number ? ProfileValue.FromNumber(v.number) : v.kind == ValueKind.Category ? ProfileValue.FromCategory(v.category) :
                v.kind == ValueKind.Missing ? ProfileValue.Missing : ProfileValue.NotSupplied)));
        }
    }
    [Serializable]
    public sealed class ReviewStop
    {
        public ReviewStopKind kind;
        public string treeId, nodeId, explanation;
        public ReviewProfile profileA, profileB;
        public bool showingB;
    }
    [Serializable]
    public sealed class GuidedReview
    {
        public int version = 1;
        public string modelId, fingerprint;
        public List<ReviewStop> stops = new List<ReviewStop>();
        public static GuidedReview Create(ModelDefinition model) => new GuidedReview { modelId = model.Id, fingerprint = Fingerprint(model) };
        public string Validate(ModelDefinition model)
        {
            if (version != 1 || stops == null || stops.Count == 0) return "The review has no stops or uses an unsupported version.";
            if (modelId != model.Id || fingerprint != Fingerprint(model)) return "The model changed. Load the original model or record a new review.";
            foreach (var stop in stops)
            {
                if (stop == null || !Enum.IsDefined(typeof(ReviewStopKind), stop.kind)) return "Invalid review stop.";
                var tree = model.Trees.FirstOrDefault(t => t.Id == stop.treeId);
                if (stop.kind != ReviewStopKind.Forest && tree == null) return "Missing tree: " + stop.treeId;
                if (!string.IsNullOrEmpty(stop.nodeId) && (tree == null || !tree.Nodes.Any(n => n.Id == stop.nodeId))) return "Missing node: " + stop.nodeId;
                if (stop.kind == ReviewStopKind.Node && string.IsNullOrEmpty(stop.nodeId)) return "A node stop needs a target.";
                if ((stop.kind == ReviewStopKind.Profile || stop.kind == ReviewStopKind.Comparison) && (stop.profileA == null || !stop.profileA.present)) return "Missing saved profile A.";
                if (stop.kind == ReviewStopKind.Comparison && (stop.profileB == null || !stop.profileB.present)) return "Missing saved profile B.";
                try
                {
                    foreach (var p in new[] { stop.profileA, stop.profileB }.Where(p => p != null && p.present))
                    {
                        var result = ModelEvaluator.Evaluate(model, p.Restore(model.Id));
                        if (!result.IsSuccess) return "Saved profile is invalid: " + string.Join("; ", result.Diagnostics.Select(d => d.Message));
                    }
                }
                catch (ArgumentException) { return "Invalid or duplicate saved profile values."; }
            }
            return null;
        }
        /// <summary>Length-prefixed exact semantics protect stable IDs from silently changed targets.</summary>
        public static string Fingerprint(ModelDefinition model)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(model.Id); writer.Write(model.Objective); writer.Write(model.BaseScore); writer.Write(model.StructureOnlyPreview);
                writer.Write(model.Features.Count);
                foreach (var f in model.Features.OrderBy(f => f.Id, StringComparer.Ordinal))
                {
                    writer.Write(f.Id); writer.Write((int)f.Kind); writer.Write(f.AllowMissing); writer.Write(f.Integer);
                    writer.Write(f.Minimum.HasValue); if (f.Minimum.HasValue) writer.Write(f.Minimum.Value);
                    writer.Write(f.Maximum.HasValue); if (f.Maximum.HasValue) writer.Write(f.Maximum.Value);
                    writer.Write(f.Categories.Count); foreach (var c in f.Categories) writer.Write(c);
                }
                writer.Write(model.Trees.Count);
                foreach (var t in model.Trees)
                {
                    writer.Write(t.Id); writer.Write(t.RootId); writer.Write(t.Weight); writer.Write(t.Nodes.Count);
                    foreach (var n in t.Nodes.OrderBy(n => n.Id, StringComparer.Ordinal))
                    {
                        writer.Write(n.Id); writer.Write(n is SplitNode);
                        if (n is LeafNode leaf) writer.Write(leaf.Score);
                        else if (n is SplitNode s)
                        {
                            writer.Write(s.FeatureId); writer.Write((int)s.Condition.Operator); writer.Write(s.Condition.Threshold.HasValue);
                            if (s.Condition.Threshold.HasValue) writer.Write(s.Condition.Threshold.Value);
                            writer.Write(s.Condition.Categories.Count); foreach (var c in s.Condition.Categories) writer.Write(c);
                            writer.Write(s.TrueChild); writer.Write(s.FalseChild);
                        }
                    }
                }
                writer.Flush(); using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(stream.ToArray())).Replace("-", "");
            }
        }
    }
}
