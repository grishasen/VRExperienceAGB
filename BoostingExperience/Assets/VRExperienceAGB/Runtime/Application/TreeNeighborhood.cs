using System;
using System.Collections.Generic;
using System.Linq;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Application
{
    /// <summary>Bounded presentation query. The complete model remains available to the evaluator.</summary>
    public sealed class TreeNeighborhood
    {
        private readonly Dictionary<string, ModelNode> nodes;
        private readonly Dictionary<string, int> descendants = new Dictionary<string, int>();
        public TreeNeighborhood(ModelTree tree)
        {
            nodes = tree.Nodes.ToDictionary(n => n.Id, StringComparer.Ordinal);
            var stack = new Stack<(string id, bool expanded)>();
            stack.Push((tree.RootId, false));
            while (stack.Count != 0)
            {
                var item = stack.Pop();
                if (!(nodes[item.id] is SplitNode split)) { descendants[item.id] = 0; continue; }
                if (item.expanded) descendants[item.id] = 2 + descendants[split.TrueChild] + descendants[split.FalseChild];
                else { stack.Push((item.id, true)); stack.Push((split.FalseChild, false)); stack.Push((split.TrueChild, false)); }
            }
        }
        public int Descendants(string id) => descendants[id];
        public IReadOnlyList<string> Visible(string root, int levels = 2)
        {
            if (levels < 0 || levels > 2) throw new ArgumentOutOfRangeException(nameof(levels));
            var result = new List<string>(); var queue = new Queue<(string id, int depth)>(); queue.Enqueue((root, 0));
            while (queue.Count > 0)
            {
                var item = queue.Dequeue(); result.Add(item.id);
                if (item.depth < levels && nodes[item.id] is SplitNode split)
                { queue.Enqueue((split.TrueChild, item.depth + 1)); queue.Enqueue((split.FalseChild, item.depth + 1)); }
            }
            return result.AsReadOnly();
        }
    }

    /// <summary>Deterministic synthetic stress example: eight numeric decisions, 511 nodes, 256 leaves.</summary>
    public static class DeepTreeExample
    {
        public static ModelDefinition Model()
        {
            var nodes = new List<ModelNode>();
            Add(nodes, "deep-root", 0, 0, 256);
            return new ModelDefinition(1, "synthetic-depth-eight", "Synthetic navigation test", "binary_logistic", "demo response", 0,
                new[] { new FeatureDefinition("position", FeatureKind.Number, false, "Example value", true, 0, 255) },
                new[] { new ModelTree("deep-tree", "deep-root", 1, nodes) });
        }
        private static void Add(List<ModelNode> nodes, string id, int depth, int low, int high)
        {
            if (depth == 8) { nodes.Add(new LeafNode(id, (low - 128) / 64.0)); return; }
            int mid = (low + high) / 2;
            nodes.Add(new SplitNode(id, "position", new SplitCondition(DecisionOperator.LessThan, mid), id + "T", id + "F"));
            Add(nodes, id + "T", depth + 1, low, mid); Add(nodes, id + "F", depth + 1, mid, high);
        }
        public static ProfileSet Profiles(ModelDefinition model) => new ProfileSet(1, model.Id,
            new[] { 0, 85, 170, 255 }.Select(value => new PreparedProfile(1, model.Id, "value-" + value, "Example " + value,
                new[] { new KeyValuePair<string, ProfileValue>("position", ProfileValue.FromNumber(value)) })));
    }
}
