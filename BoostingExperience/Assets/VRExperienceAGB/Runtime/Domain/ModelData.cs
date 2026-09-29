using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace VRExperienceAGB.Domain
{
    internal static class Snapshot
    {
        internal static ReadOnlyCollection<T> List<T>(IEnumerable<T> values) =>
            Array.AsReadOnly((values ?? Enumerable.Empty<T>()).ToArray());
    }

    public sealed class Diagnostic
    {
        public string Code { get; }
        public string Message { get; }
        public string Location { get; }
        public string ModelId { get; }
        public string TreeId { get; }
        public string NodeId { get; }
        public string FeatureId { get; }

        public Diagnostic(string code, string message, string location = null,
            string modelId = null, string treeId = null, string nodeId = null, string featureId = null)
        {
            Code = code; Message = message; Location = location;
            ModelId = modelId; TreeId = treeId; NodeId = nodeId; FeatureId = featureId;
        }
    }

    /// <summary>A failed operation never carries a partial or previous result.</summary>
    public sealed class Outcome<T> where T : class
    {
        public T Value { get; }
        public IReadOnlyList<Diagnostic> Diagnostics { get; }
        public bool IsSuccess => Value != null;

        private Outcome(T value, IEnumerable<Diagnostic> diagnostics)
        {
            Value = value; Diagnostics = Snapshot.List(diagnostics);
        }

        public static Outcome<T> Success(T value) =>
            new Outcome<T>(value ?? throw new ArgumentNullException(nameof(value)), null);

        public static Outcome<T> Failure(IEnumerable<Diagnostic> diagnostics)
        {
            var errors = Snapshot.List(diagnostics);
            if (errors.Count == 0) throw new ArgumentException("Failure requires a diagnostic.", nameof(diagnostics));
            return new Outcome<T>(null, errors);
        }
    }

    public enum FeatureKind { Number, Category }
    public enum DecisionOperator { LessThan, In, IsMissing }
    public enum ValueKind { NotSupplied, Missing, Number, Category }

    /// <summary>NotSupplied and explicit Missing remain distinct, including in decision traces.</summary>
    public readonly struct ProfileValue
    {
        public ValueKind Kind { get; }
        public double Number { get; }
        public string Category { get; }
        public bool IsMissing => Kind == ValueKind.Missing || Kind == ValueKind.NotSupplied;
        private ProfileValue(ValueKind kind, double number = 0, string category = null)
        { Kind = kind; Number = number; Category = category; }
        public static ProfileValue NotSupplied => default;
        public static ProfileValue Missing => new ProfileValue(ValueKind.Missing);
        public static ProfileValue FromNumber(double value) => new ProfileValue(ValueKind.Number, value);
        public static ProfileValue FromCategory(string value) => new ProfileValue(ValueKind.Category, category: value);
    }

    public sealed class FeatureDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public FeatureKind Kind { get; }
        public bool AllowMissing { get; }
        public bool Integer { get; }
        public double? Minimum { get; }
        public double? Maximum { get; }
        public IReadOnlyList<string> Categories { get; }
        public FeatureDefinition(string id, FeatureKind kind, bool allowMissing, string displayName = null,
            bool integer = false, double? minimum = null, double? maximum = null, IEnumerable<string> categories = null)
        {
            Id = id; DisplayName = displayName ?? id; Kind = kind; AllowMissing = allowMissing;
            Integer = integer; Minimum = minimum; Maximum = maximum; Categories = Snapshot.List(categories);
        }
    }

    public sealed class SplitCondition
    {
        public DecisionOperator Operator { get; }
        public double? Threshold { get; }
        public IReadOnlyList<string> Categories { get; }
        public SplitCondition(DecisionOperator @operator, double? threshold = null, IEnumerable<string> categories = null)
        { Operator = @operator; Threshold = threshold; Categories = Snapshot.List(categories); }
    }

    public abstract class ModelNode
    {
        public string Id { get; }
        protected ModelNode(string id) { Id = id; }
    }

    public sealed class LeafNode : ModelNode
    {
        public double Score { get; }
        public LeafNode(string id, double score) : base(id) { Score = score; }
    }

    public sealed class SplitNode : ModelNode
    {
        public string FeatureId { get; }
        public SplitCondition Condition { get; }
        public string TrueChild { get; }
        public string FalseChild { get; }
        public SplitNode(string id, string featureId, SplitCondition condition, string trueChild, string falseChild) : base(id)
        { FeatureId = featureId; Condition = condition; TrueChild = trueChild; FalseChild = falseChild; }
    }

    public sealed class ModelTree
    {
        public string Id { get; }
        public string RootId { get; }
        public double Weight { get; }
        public IReadOnlyList<ModelNode> Nodes { get; }
        public ModelTree(string id, string rootId, double weight, IEnumerable<ModelNode> nodes)
        { Id = id; RootId = rootId; Weight = weight; Nodes = Snapshot.List(nodes); }
    }

    public sealed class ModelDefinition
    {
        public int SchemaVersion { get; }
        public string Id { get; }
        public string Provenance { get; }
        public string Objective { get; }
        public string OutcomeLabel { get; }
        public double BaseScore { get; }
        public IReadOnlyList<FeatureDefinition> Features { get; }
        public IReadOnlyList<ModelTree> Trees { get; }
        public ModelDefinition(int schemaVersion, string id, string provenance, string objective, string outcomeLabel,
            double baseScore, IEnumerable<FeatureDefinition> features, IEnumerable<ModelTree> trees)
        {
            SchemaVersion = schemaVersion; Id = id; Provenance = provenance; Objective = objective;
            OutcomeLabel = outcomeLabel; BaseScore = baseScore;
            Features = Snapshot.List(features); Trees = Snapshot.List(trees);
        }
    }

    public sealed class PreparedProfile
    {
        public int SchemaVersion { get; }
        public string ModelId { get; }
        public string Id { get; }
        public string DisplayName { get; }
        public IReadOnlyDictionary<string, ProfileValue> Values { get; }
        public PreparedProfile(int schemaVersion, string modelId, string id, string displayName,
            IEnumerable<KeyValuePair<string, ProfileValue>> values)
        {
            SchemaVersion = schemaVersion; ModelId = modelId; Id = id; DisplayName = displayName;
            var copy = new Dictionary<string, ProfileValue>(StringComparer.Ordinal);
            foreach (var pair in values ?? Enumerable.Empty<KeyValuePair<string, ProfileValue>>())
                copy.Add(pair.Key, pair.Value);
            Values = new ReadOnlyDictionary<string, ProfileValue>(copy);
        }
        public ProfileValue GetValue(string featureId) =>
            Values.TryGetValue(featureId, out var value) ? value : ProfileValue.NotSupplied;

        public PreparedProfile WithValue(string featureId, ProfileValue value)
        {
            var copy = Values.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
            if (value.Kind == ValueKind.NotSupplied) copy.Remove(featureId);
            else copy[featureId] = value;
            return new PreparedProfile(SchemaVersion, ModelId, Id, DisplayName, copy);
        }
    }

    public sealed class ProfileSet
    {
        public int SchemaVersion { get; }
        public string ModelId { get; }
        public IReadOnlyList<PreparedProfile> Profiles { get; }
        public ProfileSet(int schemaVersion, string modelId, IEnumerable<PreparedProfile> profiles)
        { SchemaVersion = schemaVersion; ModelId = modelId; Profiles = Snapshot.List(profiles); }
    }
}
