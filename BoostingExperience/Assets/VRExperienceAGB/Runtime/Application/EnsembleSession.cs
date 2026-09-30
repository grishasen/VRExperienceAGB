using System;
using System.Collections.Generic;
using System.Linq;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Application
{
    public enum TourDetail { Detailed, Short }
    public enum ContributionProgress { Pending, ReachedLeaf, Grouped }

    public sealed class ContributionRow
    {
        public int Index { get; }
        public string TreeId { get; }
        public string LeafId { get; }
        public double? EvaluatedContribution { get; }
        public double PresentedContribution { get; }
        public ContributionProgress Progress { get; }
        internal ContributionRow(int index, string treeId, string leafId, double? evaluated, double presented, ContributionProgress progress)
        { Index = index; TreeId = treeId; LeafId = leafId; EvaluatedContribution = evaluated; PresentedContribution = presented; Progress = progress; }
    }

    /// <summary>Owns complete evaluation separately from route progress, tour grouping and edit drafts.</summary>
    public sealed class EnsembleSession
    {
        private TreeSession[] sessions;
        private readonly HashSet<int> grouped = new HashSet<int>();
        public ModelDefinition Model { get; }
        public PreparedProfile OriginalProfile { get; }
        public PreparedProfile Profile { get; private set; }
        public PreparedProfile Draft { get; private set; }
        public EvaluationResult Evaluation { get; private set; }
        public string EvaluationId { get; private set; } = Guid.NewGuid().ToString("N");
        public long Revision { get; private set; }
        public ExperienceMode Mode { get; private set; }
        public TourDetail Detail { get; private set; }
        public int Index { get; private set; }
        public TreeSession Current => sessions[Index];
        public int CompletedCount => sessions.Select((s, i) => s.State.AtLeaf || grouped.Contains(i)).Count(done => done);
        public bool Complete => CompletedCount == sessions.Length;
        public double RouteTotal => Model.BaseScore + Ledger.Sum(row => row.PresentedContribution);
        public IReadOnlyList<TreeSession> Trees => Array.AsReadOnly(sessions);
        public IReadOnlyList<ContributionRow> Ledger => Array.AsReadOnly(sessions.Select((s, i) => new ContributionRow(i, s.Tree.Id,
            Evaluation?.Trees[i].LeafId ?? (s.State.AtLeaf ? s.State.NodeId : null), Evaluation?.Trees[i].Contribution,
            grouped.Contains(i) ? Evaluation.Trees[i].Contribution : s.State.Contribution,
            grouped.Contains(i) ? ContributionProgress.Grouped : s.State.AtLeaf ? ContributionProgress.ReachedLeaf : ContributionProgress.Pending)).ToArray());
        public IReadOnlyList<ContributionRow> GroupedRows => Array.AsReadOnly(Ledger.Where(r => r.Progress == ContributionProgress.Grouped).ToArray());
        public ConsistencyReport Consistency => ManualRouteConsistency.Check(Model, sessions.SelectMany(s => s.State.Decisions.Select(d => {
            var node = (SplitNode)s.Tree.Nodes.Single(n => n.Id == d.NodeId);
            return new ManualConstraint(d.TreeId, d.NodeId, node.FeatureId, node.Condition, d.Matched);
        })));

        private EnsembleSession(ModelDefinition model, PreparedProfile profile, TreeSession[] trees, EvaluationResult evaluation)
        { Model = model; OriginalProfile = Profile = profile; sessions = trees; Evaluation = evaluation; Mode = profile == null ? ExperienceMode.Manual : ExperienceMode.PreparedProfile; }
        public static Outcome<EnsembleSession> Create(ModelDefinition model, PreparedProfile profile = null)
        {
            var errors = ModelValidator.Validate(model);
            if (errors.Count > 0) return Outcome<EnsembleSession>.Failure(errors);
            EvaluationResult evaluation = null;
            if (profile != null)
            {
                var result = ModelEvaluator.Evaluate(model, profile);
                if (!result.IsSuccess) return Outcome<EnsembleSession>.Failure(result.Diagnostics);
                evaluation = result.Value;
            }
            TreeSession[] trees;
            if (profile != null) trees = model.Trees.Select((t, i) => TreeSession.FromEvaluation(model, t, profile, evaluation.Trees[i])).ToArray();
            else
            {
                var outcomes = model.Trees.Select(t => TreeSession.ManualFromValidatedModel(model, t)).ToArray();
                var failures = outcomes.Where(o => !o.IsSuccess).SelectMany(o => o.Diagnostics).ToArray();
                if (failures.Length > 0) return Outcome<EnsembleSession>.Failure(failures);
                trees = outcomes.Select(o => o.Value).ToArray();
            }
            return Outcome<EnsembleSession>.Success(new EnsembleSession(model, profile, trees, evaluation));
        }
        public bool Select(int index)
        {
            if (index < 0 || index >= sessions.Length || index == Index) return false;
            bool paused = Current.State.Paused;
            Current.ReturnToOverview(); Index = index;
            Current.ReturnToOverview(); Current.EnterTree(); Current.SetPaused(paused); Revision++;
            return true;
        }
        public void TakeOver()
        {
            Mode = ExperienceMode.Manual; Evaluation = null; grouped.Clear(); Draft = null; Revision++;
            foreach (var session in sessions) if (!ReferenceEquals(session, Current)) session.UseManualMode();
        }
        public void InvalidateAfterCurrent()
        {
            for (int i = Index + 1; i < sessions.Length; i++) { sessions[i].Restart(); sessions[i].SetPaused(Current.State.Paused); }
            grouped.RemoveWhere(i => i >= Index); Revision++;
        }
        public CommandReply Back()
        {
            if (Current.State.PendingDecision != null || Current.State.Decisions.Count > 0)
            { var reply = Current.Back(); if (reply.Accepted) InvalidateAfterCurrent(); return reply; }
            if (Index == 0) return new CommandReply(false, "Already at the first tree root.");
            Select(Index - 1); InvalidateAfterCurrent();
            return new CommandReply(true, "Previous tree restored. Later route contributions were cleared.");
        }
        public void RestartTour()
        {
            bool paused = Current.State.Paused;
            foreach (var s in sessions) { s.Restart(); s.SetPaused(paused); }
            Index = 0; grouped.Clear(); Revision++;
        }
        public bool SetDetail(TourDetail detail)
        {
            if (Detail == detail) return false;
            Detail = detail;
            if (detail == TourDetail.Detailed) grouped.Clear();
            Revision++; return true;
        }
        public bool ExplainRemainingGroup()
        {
            if (Mode != ExperienceMode.PreparedProfile || Detail != TourDetail.Short || !Current.State.AtLeaf || Current.State.Paused || Current.State.Overview) return false;
            for (int i = Index + 1; i < sessions.Length; i++) if (!sessions[i].State.AtLeaf) grouped.Add(i);
            Revision++; return true;
        }
        public bool ReviseConflict()
        {
            var conflict = Consistency.Conflicts.FirstOrDefault();
            if (conflict == null) return false;
            int index = Array.FindIndex(sessions, s => s.Tree.Id == conflict.TreeId);
            if (index != Index) Select(index);
            while (Current.State.Decisions.Any(d => d.NodeId == conflict.NodeId)) Current.Back();
            InvalidateAfterCurrent(); Revision++; return true;
        }
        public bool BeginEdit()
        {
            if (Profile == null) return false;
            Draft = new PreparedProfile(Profile.SchemaVersion, Profile.ModelId, Guid.NewGuid().ToString("N"),
                "Hypothetical copy of " + OriginalProfile.DisplayName, Profile.Values);
            Revision++; return true;
        }
        public Outcome<PreparedProfile> StageEdit(string feature, ProfileValue value)
        {
            if (Draft == null) return Outcome<PreparedProfile>.Failure(new[] { new Diagnostic("NoDraft", "Choose Try a change first.") });
            if (!Model.Features.Any(f => f.Id == feature)) return Outcome<PreparedProfile>.Failure(new[] { new Diagnostic("UnknownFeature", "This feature is not declared.") });
            Draft = Draft.WithValue(feature, value); Revision++;
            return Outcome<PreparedProfile>.Success(Draft);
        }
        public void ResetDraftToOriginal()
        {
            if (Draft == null || OriginalProfile == null) return;
            Draft = new PreparedProfile(OriginalProfile.SchemaVersion, OriginalProfile.ModelId, Draft.Id, Draft.DisplayName, OriginalProfile.Values);
            Revision++;
        }
        public void CancelEdit() { Draft = null; Revision++; }
        public Outcome<EnsembleSession> ApplyEdit()
        {
            if (Draft == null) return Failure("NoDraft", "Choose Try a change first.");
            var unfinished = Model.Features.Where(f => Draft.GetValue(f.Id).Kind == ValueKind.NotSupplied).ToArray();
            if (unfinished.Length > 0) return Outcome<EnsembleSession>.Failure(unfinished.Select(f => new Diagnostic("IncompleteEdit", "Supply a value or deliberately choose Missing.", featureId: f.Id)));
            var candidate = Create(Model, Draft);
            if (!candidate.IsSuccess) return candidate;
            Replace(candidate.Value); Draft = null;
            return Outcome<EnsembleSession>.Success(this);
        }
        private static Outcome<EnsembleSession> Failure(string code, string message) => Outcome<EnsembleSession>.Failure(new[] { new Diagnostic(code, message) });
        private void Replace(EnsembleSession candidate)
        {
            bool paused = Current.State.Paused;
            foreach (var old in sessions) old.ReturnToOverview(); // Retire all pending animation tokens before replacement.
            Profile = candidate.Profile; Evaluation = candidate.Evaluation; EvaluationId = candidate.EvaluationId;
            sessions = candidate.sessions; Index = 0; Mode = ExperienceMode.PreparedProfile; grouped.Clear();
            foreach (var s in sessions) s.SetPaused(paused);
            Revision++;
        }
        public Outcome<EnsembleSession> Edit(string feature, ProfileValue value)
        {
            if (!BeginEdit()) return Failure("NoProfile", "Select a profile before editing.");
            var staged = StageEdit(feature, value);
            if (!staged.IsSuccess) { CancelEdit(); return Outcome<EnsembleSession>.Failure(staged.Diagnostics); }
            var result = ApplyEdit(); if (!result.IsSuccess) CancelEdit(); return result;
        }
        public void RestoreOriginal()
        {
            if (OriginalProfile == null) return;
            Replace(Create(Model, OriginalProfile).Value); Draft = null;
        }
    }
}
