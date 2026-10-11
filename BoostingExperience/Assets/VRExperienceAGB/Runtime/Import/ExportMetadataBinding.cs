using System;
using System.Linq;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Import
{
    /// <summary>Bind evidence only to the exact source or a fingerprinted, structurally identical reconstruction.</summary>
    public static class ExportMetadataBinding
    {
        public static bool Matches(ModelDefinition model, AgbPreviewResult preview, string expectedSha256 = null)
        {
            if (preview == null) return false;
            if (ReferenceEquals(model, preview.Model)) return true;
            if (expectedSha256 == null || expectedSha256.Length != 64 ||
                !string.Equals(expectedSha256, preview.Sha256, StringComparison.Ordinal) ||
                model.BaseScore != preview.Model.BaseScore || model.Trees.Count != preview.Model.Trees.Count) return false;
            if (preview.Metadata.Count != model.Trees.Sum(t => t.Nodes.Count)) return false;
            for (int i = 0; i < model.Trees.Count; i++)
            {
                var target = model.Trees[i]; var source = preview.Model.Trees[i];
                if (target.Id != source.Id || target.RootId != source.RootId || target.Weight != source.Weight || target.Nodes.Count != source.Nodes.Count) return false;
                var nodes = source.Nodes.ToDictionary(n => n.Id, StringComparer.Ordinal);
                foreach (var node in target.Nodes)
                {
                    if (!nodes.TryGetValue(node.Id, out var original) || !preview.Metadata.ContainsKey(target.Id + "/" + node.Id)) return false;
                    if (node is LeafNode leaf)
                    {
                        if (!(original is LeafNode other) || leaf.Score != other.Score) return false;
                    }
                    else if (node is SplitNode split && original is SplitNode other)
                    {
                        if (split.FeatureId != other.FeatureId || split.TrueChild != other.TrueChild || split.FalseChild != other.FalseChild ||
                            split.Condition.Operator != other.Condition.Operator || split.Condition.Threshold != other.Condition.Threshold ||
                            !split.Condition.Categories.SequenceEqual(other.Condition.Categories, StringComparer.Ordinal)) return false;
                    }
                    else return false;
                }
            }
            return true;
        }
    }
}
