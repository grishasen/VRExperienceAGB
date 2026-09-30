using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace VRExperienceAGB.Domain
{
    public enum RouteConsistency { Consistent, Contradictory, NotVerified }

    public sealed class ManualConstraint
    {
        public string TreeId { get; }
        public string NodeId { get; }
        public string FeatureId { get; }
        public SplitCondition Condition { get; }
        public bool Matched { get; }
        public ManualConstraint(string treeId, string nodeId, string featureId, SplitCondition condition, bool matched)
        { TreeId = treeId; NodeId = nodeId; FeatureId = featureId; Condition = condition; Matched = matched; }
        public string Description => TreeId + "/" + NodeId + ": " + FeatureId + " " +
            (Condition.Operator == DecisionOperator.LessThan ? (Matched ? "< " : ">= ") + Condition.Threshold.Value.ToString("G17", CultureInfo.InvariantCulture) :
             Condition.Operator == DecisionOperator.In ? (Matched ? "in {" : "not in {") + string.Join(", ", Condition.Categories) + "}" :
             Matched ? "is missing" : "is present");
    }

    public sealed class ConsistencyReport
    {
        public RouteConsistency Status { get; }
        public IReadOnlyList<ManualConstraint> Conflicts { get; }
        public string Message { get; }
        internal ConsistencyReport(RouteConsistency status, IEnumerable<ManualConstraint> conflicts, string message)
        { Status = status; Conflicts = Snapshot.List(conflicts); Message = message; }
    }

    /// <summary>Intersects accepted choices with declared domains. It never supplies a profile or prediction.</summary>
    public static class ManualRouteConsistency
    {
        public static ConsistencyReport Check(ModelDefinition model, IEnumerable<ManualConstraint> choices)
        {
            if (ModelValidator.Validate(model).Count != 0)
                return new ConsistencyReport(RouteConsistency.NotVerified, null, "Consistency not verified: unsupported model.");
            var conflicts = new List<ManualConstraint>();
            bool unknown = false;
            foreach (var group in choices.GroupBy(c => c.FeatureId))
            {
                var feature = model.Features.SingleOrDefault(f => f.Id == group.Key);
                if (feature == null) { unknown = true; continue; }
                bool missing = feature.AllowMissing, present = true;
                double lower = feature.Minimum ?? -double.MaxValue, upper = feature.Maximum ?? double.MaxValue;
                bool upperOpen = false;
                var categories = new HashSet<string>(feature.Categories, StringComparer.Ordinal);
                foreach (var choice in group)
                {
                    var condition = choice.Condition;
                    if (condition == null) { unknown = true; continue; }
                    switch (condition.Operator)
                    {
                        case DecisionOperator.IsMissing:
                            if (choice.Matched) present = false; else missing = false;
                            break;
                        case DecisionOperator.LessThan:
                            if (feature.Kind != FeatureKind.Number || !condition.Threshold.HasValue || !ModelValidator.IsFinite(condition.Threshold.Value)) { unknown = true; break; }
                            missing = false;
                            double threshold = condition.Threshold.Value;
                            if (choice.Matched && threshold <= upper) { upper = threshold; upperOpen = true; }
                            if (!choice.Matched) lower = Math.Max(lower, threshold);
                            break;
                        case DecisionOperator.In:
                            if (feature.Kind != FeatureKind.Category) { unknown = true; break; }
                            missing = false;
                            if (choice.Matched) categories.IntersectWith(condition.Categories); else categories.ExceptWith(condition.Categories);
                            break;
                        default: unknown = true; break;
                    }
                }
                if (feature.Kind == FeatureKind.Category) present &= categories.Count > 0;
                else
                {
                    // Test the smallest permitted integer directly. Subtracting one from a large
                    // open upper bound can round back to that bound in double precision.
                    if (feature.Integer) lower = Math.Ceiling(lower);
                    present &= lower < upper || (lower == upper && !upperOpen);
                }
                if (!missing && !present) conflicts.AddRange(group);
            }
            if (conflicts.Count > 0)
                return new ConsistencyReport(RouteConsistency.Contradictory, conflicts,
                    "Conflicting choices for " + string.Join(", ", conflicts.Select(c => c.FeatureId).Distinct()) + ". " +
                    string.Join("; ", conflicts.Select(c => c.Description)));
            return new ConsistencyReport(unknown ? RouteConsistency.NotVerified : RouteConsistency.Consistent, null,
                unknown ? "Consistency not verified." : "Accepted choices are consistent with the declared domains. A partial route is not a profile prediction.");
        }
    }
}
