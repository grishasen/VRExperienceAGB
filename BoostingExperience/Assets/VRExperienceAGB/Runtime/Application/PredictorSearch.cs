using System;
using System.Collections.Generic;
using System.Linq;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Application
{
    public enum PredictorScope { EntireModel, CurrentProfilePath }
    public sealed class PredictorOccurrence
    {
        public int TreeIndex { get; }
        public string TreeId { get; }
        public SplitNode Node { get; }
        internal PredictorOccurrence(int index, string tree, SplitNode node)
        { TreeIndex = index; TreeId = tree; Node = node; }
    }

    /// <summary>Exact feature identity queries, independent of visible geometry and route animation.</summary>
    public sealed class PredictorSearch
    {
        private readonly ModelDefinition model;
        private readonly Dictionary<string, PredictorOccurrence[]> index;
        public IReadOnlyList<string> Features { get; }
        public PredictorSearch(ModelDefinition model)
        {
            this.model = model;
            index = model.Trees.SelectMany((t, i) => t.Nodes.OfType<SplitNode>()
                .Select(n => new PredictorOccurrence(i, t.Id, n))).GroupBy(o => o.Node.FeatureId)
                .ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.Ordinal);
            Features = Array.AsReadOnly(index.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray());
        }
        public IReadOnlyList<PredictorOccurrence> Query(string feature, PredictorScope scope, EnsembleSession session)
        {
            if (!index.TryGetValue(feature ?? "", out var matches)) return Array.Empty<PredictorOccurrence>();
            if (scope == PredictorScope.EntireModel) return Array.AsReadOnly(matches);
            if (session == null || session.Model != model || session.Evaluation == null)
                return Array.Empty<PredictorOccurrence>();
            var paths = session.Evaluation.Trees.Select(t => new HashSet<string>(t.VisitedNodeIds, StringComparer.Ordinal)).ToArray();
            return Array.AsReadOnly(matches.Where(m => paths[m.TreeIndex].Contains(m.Node.Id)).ToArray());
        }
    }
}
