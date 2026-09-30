using System;
using System.Collections.Generic;
using System.Linq;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Application
{
    public enum ExperienceMode { Manual, PreparedProfile }

    public sealed class RouteDecision
    {
        public long EventId { get; }
        public string TreeId { get; }
        public string NodeId { get; }
        public string ChildId { get; }
        public bool Matched { get; }
        internal RouteDecision(long eventId, string treeId, SplitNode node, bool matched)
        { EventId = eventId; TreeId = treeId; NodeId = node.Id; Matched = matched; ChildId = matched ? node.TrueChild : node.FalseChild; }
    }

    public sealed class SessionState
    {
        public long Revision { get; }
        public ExperienceMode Mode { get; }
        public string TreeId { get; }
        public string NodeId { get; }
        public IReadOnlyList<string> Path { get; }
        public IReadOnlyList<RouteDecision> Decisions { get; }
        public RouteDecision PendingDecision { get; }
        public bool Paused { get; }
        public bool Playing { get; }
        public bool Overview { get; }
        public bool AtLeaf { get; }
        public double Baseline { get; }
        public double Contribution { get; }
        public double RouteTotal => Baseline + Contribution;
        public bool CanAdvance => !Paused && !Overview && PendingDecision == null && !AtLeaf;
        public string ScoreMeaning => Mode == ExperienceMode.Manual ? "Manual route score" : "One-tree teaching subtotal";
        internal SessionState(TreeSession session)
        {
            Revision = session.Revision; Mode = session.Mode; TreeId = session.Tree.Id; NodeId = session.CurrentNode.Id;
            Decisions = Array.AsReadOnly(session.History.ToArray());
            Path = Array.AsReadOnly(new[] { session.Tree.RootId }.Concat(Decisions.Select(d => d.ChildId)).ToArray());
            PendingDecision = session.Pending; Paused = session.Paused; Playing = session.Playing; Overview = session.Overview;
            AtLeaf = session.CurrentNode is LeafNode; Baseline = session.Model.BaseScore;
            Contribution = AtLeaf ? session.Tree.Weight * ((LeafNode)session.CurrentNode).Score : 0;
        }
    }

    public readonly struct CommandReply
    {
        public bool Accepted { get; }
        public string Message { get; }
        internal CommandReply(bool accepted, string message) { Accepted = accepted; Message = message; }
    }

    /// <summary>One-tree teaching session. Scores come from accepted state, never animation callbacks alone.</summary>
    public sealed class TreeSession
    {
        private readonly Dictionary<string, ModelNode> nodes;
        private readonly TreeEvaluation evaluatedTree;
        internal readonly List<RouteDecision> History = new List<RouteDecision>();
        internal RouteDecision Pending { get; private set; }
        internal bool Paused { get; private set; }
        internal bool Playing { get; private set; }
        internal bool Overview { get; private set; }
        internal long Revision { get; private set; }
        internal ExperienceMode Mode { get; private set; }
        public ModelDefinition Model { get; }
        public ModelTree Tree { get; }
        public PreparedProfile Profile { get; }
        public ModelNode CurrentNode => nodes[History.Count == 0 ? Tree.RootId : History[History.Count - 1].ChildId];
        public SessionState State => new SessionState(this);
        public DecisionTrace CurrentProfileDecision => Mode == ExperienceMode.PreparedProfile && CurrentNode is SplitNode
            ? evaluatedTree.Decisions[History.Count] : null;

        private TreeSession(ModelDefinition model, ModelTree tree, PreparedProfile profile, TreeEvaluation evaluated)
        {
            Model = model; Tree = tree; Profile = profile; evaluatedTree = evaluated;
            Mode = profile == null ? ExperienceMode.Manual : ExperienceMode.PreparedProfile;
            nodes = tree.Nodes.ToDictionary(n => n.Id, StringComparer.Ordinal);
        }

        public static Outcome<TreeSession> CreateManual(ModelDefinition model, string treeId) => Create(model, treeId, null, false);
        public static Outcome<TreeSession> CreatePrepared(ModelDefinition model, string treeId, PreparedProfile profile) => Create(model, treeId, profile, true);
        private static Outcome<TreeSession> Create(ModelDefinition model, string treeId, PreparedProfile profile, bool prepared)
        {
            var errors = ModelValidator.Validate(model);
            if (errors.Count != 0) return Outcome<TreeSession>.Failure(errors);
            var tree = model.Trees.SingleOrDefault(t => t.Id == treeId);
            if (tree == null) return Outcome<TreeSession>.Failure(new[] { new Diagnostic("UnknownTree", "The selected tree is absent.", modelId: model.Id, treeId: treeId) });
            // Manual exploration can reach every leaf, so reject any unrepresentable teaching subtotal up front.
            if (tree.Nodes.OfType<LeafNode>().Any(l => !ModelValidator.IsFinite(tree.Weight * l.Score) || !ModelValidator.IsFinite(model.BaseScore + tree.Weight * l.Score)))
                return Outcome<TreeSession>.Failure(new[] { new Diagnostic("ScoreOverflow", "A leaf cannot produce a finite teaching subtotal.", modelId: model.Id, treeId: treeId) });
            TreeEvaluation evaluated = null;
            if (prepared)
            {
                var result = ModelEvaluator.Evaluate(model, profile);
                if (!result.IsSuccess) return Outcome<TreeSession>.Failure(result.Diagnostics);
                evaluated = result.Value.Trees.Single(t => t.TreeId == treeId);
            }
            return Outcome<TreeSession>.Success(new TreeSession(model, tree, profile, evaluated));
        }

        internal static TreeSession FromEvaluation(ModelDefinition model, ModelTree tree, PreparedProfile profile, TreeEvaluation evaluated)
            => new TreeSession(model, tree, profile, evaluated);

        private CommandReply Accept(string message) { Revision++; return new CommandReply(true, message); }
        private static CommandReply Reject(string message) => new CommandReply(false, message);
        private bool Matches(long revision, string nodeId) => revision == Revision && nodeId == CurrentNode.Id;

        public CommandReply ChooseBranch(long revision, string nodeId, bool matched)
        {
            if (!Matches(revision, nodeId)) return Reject("That choice belongs to an earlier decision.");
            if (!State.CanAdvance) return Reject("Wait for the current move, or resume the session.");
            // A deliberate arbitrary branch selection is always manual, even if it happens to match the profile.
            Mode = ExperienceMode.Manual; Playing = false;
            Pending = new RouteDecision(Revision + 1, Tree.Id, (SplitNode)CurrentNode, matched);
            return Accept("Manual exploration: follow the selected branch.");
        }

        public CommandReply StepProfile(long revision, string nodeId)
        {
            if (!Matches(revision, nodeId)) return Reject("That step belongs to an earlier decision.");
            if (Mode != ExperienceMode.PreparedProfile) return Reject("Choose a prepared profile to use Step.");
            if (!State.CanAdvance) return Reject("Wait for the current move, or resume the session.");
            Pending = new RouteDecision(Revision + 1, Tree.Id, (SplitNode)CurrentNode, CurrentProfileDecision.Matched);
            return Accept("Follow the profile's evaluated branch.");
        }

        public CommandReply CompleteMove(long eventId)
        {
            if (Pending == null || Pending.EventId != eventId) return Reject("This movement is no longer active.");
            if (Paused || Overview) return Reject("Movement is paused.");
            History.Add(Pending); Pending = null;
            if (CurrentNode is LeafNode) Playing = false;
            return Accept(CurrentNode is LeafNode ? "Leaf reached. This tree contributes once." : "Read the next decision.");
        }

        public CommandReply Back()
        {
            Playing = false;
            if (Pending != null) { Pending = null; return Accept("Move cancelled; the previous decision is unchanged."); }
            if (History.Count == 0) return Reject("Already at the root.");
            History.RemoveAt(History.Count - 1);
            return Accept("Previous decision restored; the leaf contribution has been removed.");
        }

        public void UseManualMode() { Mode = ExperienceMode.Manual; Playing = false; Pending = null; Revision++; }

        public CommandReply Restart()
        {
            Pending = null; History.Clear(); Paused = false; Playing = false; Overview = false;
            return Accept("Restarted at the baseline.");
        }

        public CommandReply SetPaused(bool paused)
        {
            if (Paused == paused) return Reject(paused ? "Already paused." : "Already resumed.");
            Paused = paused; return Accept(paused ? "Paused. Head and controller tracking remain active." : "Resumed.");
        }

        public CommandReply SetPlaying(bool playing)
        {
            if (Mode != ExperienceMode.PreparedProfile) return Reject("Automatic playback requires a prepared profile.");
            if (Overview || CurrentNode is LeafNode) return Reject("Enter the tree or replay from the root first.");
            Playing = playing; Paused = false;
            return Accept(playing ? "Playing the prepared profile." : "Automatic playback stopped.");
        }

        public CommandReply ReturnToOverview()
        {
            Pending = null; Playing = false; Overview = true;
            return Accept("Overview: accepted route and contribution are preserved.");
        }

        public CommandReply EnterTree()
        {
            if (!Overview) return Reject("Already in the tree.");
            Overview = false; return Accept("Returned to the saved decision.");
        }
    }
}
