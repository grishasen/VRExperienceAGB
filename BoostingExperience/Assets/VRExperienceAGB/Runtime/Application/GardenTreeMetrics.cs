using System;
using System.Collections.Generic;
using System.Linq;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Application
{
    /// <summary>Structural summaries for the garden; exploration progress and scores are separate.</summary>
    public sealed class GardenTreeMetrics
    {
        public string TreeId { get; }
        public int MaximumDepth { get; }
        public int LeafCount { get; }
        public int NodeCount { get; }
        public GardenTreeMetrics(ModelTree tree)
        {
            TreeId = tree.Id; NodeCount = tree.Nodes.Count;
            var nodes = tree.Nodes.ToDictionary(n => n.Id);
            var pending = new Stack<(string id, int depth)>(); pending.Push((tree.RootId, 0));
            while (pending.Count > 0)
            {
                var item = pending.Pop(); MaximumDepth = Math.Max(MaximumDepth, item.depth);
                if (nodes[item.id] is SplitNode split)
                { pending.Push((split.TrueChild, item.depth + 1)); pending.Push((split.FalseChild, item.depth + 1)); }
                else LeafCount++;
            }
        }
        // Fixed, bounded scales keep trees approachable and comparable across models.
        public float PineHeight => .85f + 1.4f * Math.Min(MaximumDepth, 14) / 14f;
        public float CrownRadius => .38f + .48f * (float)Math.Min(1, Math.Sqrt(LeafCount / 128d));
    }
}
