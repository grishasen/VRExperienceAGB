using System;
using System.Collections.Generic;
using System.Linq;

namespace VRExperienceAGB.Domain
{
    public sealed class DecisionTrace
    {
        public string NodeId { get; }
        public string FeatureId { get; }
        public ProfileValue ObservedValue { get; }
        public SplitCondition Condition { get; }
        public bool Matched { get; }
        public string ChosenChildId { get; }
        internal DecisionTrace(SplitNode node, ProfileValue value, bool matched)
        {
            NodeId = node.Id; FeatureId = node.FeatureId; ObservedValue = value; Condition = node.Condition;
            Matched = matched; ChosenChildId = matched ? node.TrueChild : node.FalseChild;
        }
    }

    public sealed class TreeEvaluation
    {
        public string TreeId { get; }
        public IReadOnlyList<string> VisitedNodeIds { get; }
        public IReadOnlyList<DecisionTrace> Decisions { get; }
        public string LeafId { get; }
        public double LeafScore { get; }
        public double Weight { get; }
        public double Contribution { get; }
        public double RunningRawScore { get; }
        internal TreeEvaluation(ModelTree tree, IEnumerable<string> path, IEnumerable<DecisionTrace> decisions,
            LeafNode leaf, double contribution, double runningRawScore)
        {
            TreeId = tree.Id; VisitedNodeIds = Snapshot.List(path); Decisions = Snapshot.List(decisions);
            LeafId = leaf.Id; LeafScore = leaf.Score; Weight = tree.Weight;
            Contribution = contribution; RunningRawScore = runningRawScore;
        }
    }

    public sealed class EvaluationResult
    {
        public string ModelId { get; }
        public string ProfileId { get; }
        public string OutcomeLabel { get; }
        public string Provenance { get; }
        public double BaseScore { get; }
        public IReadOnlyList<TreeEvaluation> Trees { get; }
        public double RawScore { get; }
        public double Probability { get; }
        public string OutputMeaning => "Synthetic binary logistic probability";
        public bool IsSourceModelVerified => false;
        internal EvaluationResult(ModelDefinition model, PreparedProfile profile, IEnumerable<TreeEvaluation> trees, double rawScore)
        {
            ModelId = model.Id; ProfileId = profile.Id; OutcomeLabel = model.OutcomeLabel; Provenance = model.Provenance;
            BaseScore = model.BaseScore; Trees = Snapshot.List(trees); RawScore = rawScore;
            Probability = ModelEvaluator.Sigmoid(rawScore);
        }
    }

    /// <summary>Evaluates the complete normalized synthetic model. No presentation budget or manual-route API is accepted.</summary>
    public static class ModelEvaluator
    {
        public const double ReferenceTolerance = 1e-12;

        public static Outcome<EvaluationResult> Evaluate(ModelDefinition model, PreparedProfile profile)
        {
            var errors = ModelValidator.Validate(model);
            if (errors.Count != 0) return Outcome<EvaluationResult>.Failure(errors);
            errors = ModelValidator.ValidateProfileValues(model, profile);
            if (errors.Count != 0) return Outcome<EvaluationResult>.Failure(errors);
            var trees = new List<TreeEvaluation>();
            var rawScore = model.BaseScore;
            foreach (var tree in model.Trees)
            {
                var nodes = tree.Nodes.ToDictionary(n => n.Id, StringComparer.Ordinal);
                var node = nodes[tree.RootId];
                var path = new List<string>(); var decisions = new List<DecisionTrace>();
                while (true)
                {
                    path.Add(node.Id);
                    if (node is LeafNode leaf)
                    {
                        var contribution = tree.Weight * leaf.Score;
                        var nextScore = rawScore + contribution;
                        if (!ModelValidator.IsFinite(contribution) || !ModelValidator.IsFinite(nextScore))
                            return Failure("ScoreOverflow", "The weighted contribution or running score is not finite.", model, tree, node);
                        rawScore = nextScore;
                        trees.Add(new TreeEvaluation(tree, path, decisions, leaf, contribution, rawScore));
                        break;
                    }
                    var split = (SplitNode)node;
                    var value = profile.GetValue(split.FeatureId);
                    if (value.IsMissing && split.Condition.Operator != DecisionOperator.IsMissing)
                        return Failure("MissingSplitOperand", "Missing values require an explicit missing predicate before comparison.", model, tree, node, split.FeatureId);
                    bool matched;
                    switch (split.Condition.Operator)
                    {
                        case DecisionOperator.LessThan: matched = value.Number < split.Condition.Threshold.Value; break;
                        case DecisionOperator.In: matched = split.Condition.Categories.Contains(value.Category, StringComparer.Ordinal); break;
                        case DecisionOperator.IsMissing: matched = value.IsMissing; break;
                        default: return Failure("UnsupportedOperator", "Unsupported decision operator.", model, tree, node, split.FeatureId);
                    }
                    var decision = new DecisionTrace(split, value, matched); decisions.Add(decision);
                    node = nodes[decision.ChosenChildId];
                }
            }
            return Outcome<EvaluationResult>.Success(new EvaluationResult(model, profile, trees, rawScore));
        }

        private static Outcome<EvaluationResult> Failure(string code, string message, ModelDefinition model,
            ModelTree tree, ModelNode node, string feature = null) => Outcome<EvaluationResult>.Failure(new[] {
                new Diagnostic(code, message, modelId: model.Id, treeId: tree.Id, nodeId: node.Id, featureId: feature) });

        internal static double Sigmoid(double value)
        {
            if (value >= 0) return 1 / (1 + Math.Exp(-value));
            var exponential = Math.Exp(value);
            return exponential / (1 + exponential);
        }
    }
}
