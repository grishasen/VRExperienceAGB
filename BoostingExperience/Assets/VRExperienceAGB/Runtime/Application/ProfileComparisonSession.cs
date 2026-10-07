using System;
using System.Collections.Generic;
using System.Linq;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Application
{
    public sealed class TreeProfileComparison
    {
        public int Index { get; }
        public TreeEvaluation A { get; }
        public TreeEvaluation B { get; }
        public DecisionTrace DivergenceA { get; }
        public DecisionTrace DivergenceB { get; }
        public bool PathsDiffer => DivergenceA != null;
        public double ContributionDelta => B.Contribution - A.Contribution;

        internal TreeProfileComparison(int index, TreeEvaluation a, TreeEvaluation b)
        {
            Index = index; A = a; B = b;
            for (int i = 0; i < Math.Min(a.Decisions.Count, b.Decisions.Count); i++)
            {
                if (a.Decisions[i].ChosenChildId == b.Decisions[i].ChosenChildId) continue;
                DivergenceA = a.Decisions[i]; DivergenceB = b.Decisions[i]; break;
            }
        }
    }

    /// <summary>Two independent prepared-profile routes; comparison always covers the complete model.</summary>
    public sealed class ProfileComparisonSession
    {
        public EnsembleSession A { get; private set; }
        public EnsembleSession B { get; private set; }
        public bool ShowingB { get; private set; }
        public EnsembleSession Active => ShowingB ? B : A;
        public IReadOnlyList<TreeProfileComparison> Trees { get; private set; }
        public IReadOnlyList<string> ChangedFeatures { get; private set; }
        public double RawDelta => B.Evaluation.RawScore - A.Evaluation.RawScore;
        public double ProbabilityPointDelta => (B.Evaluation.Probability - A.Evaluation.Probability) * 100;
        public PreparedProfile Draft => B.Draft;
        public long Revision { get; private set; }

        private ProfileComparisonSession(EnsembleSession a, EnsembleSession b)
        { A = a; B = b; Rebuild(); }

        public static Outcome<ProfileComparisonSession> Create(ModelDefinition model, PreparedProfile a, PreparedProfile b = null)
        {
            if (a == null) return Failure("ProfileRequired", "Select a prepared profile for A.");
            var first = EnsembleSession.Create(model, a);
            if (!first.IsSuccess) return Outcome<ProfileComparisonSession>.Failure(first.Diagnostics);
            var second = EnsembleSession.Create(model, b ?? new PreparedProfile(a.SchemaVersion, a.ModelId,
                Guid.NewGuid().ToString("N"), "Copy of " + a.DisplayName, a.Values));
            if (!second.IsSuccess) return Outcome<ProfileComparisonSession>.Failure(second.Diagnostics);
            if (!FiniteDifference(first.Value, second.Value)) return Failure("ComparisonOverflow", "The profile difference is not finite.");
            return Outcome<ProfileComparisonSession>.Success(new ProfileComparisonSession(first.Value, second.Value));
        }

        public void Show(bool b, int treeIndex)
        {
            if (treeIndex < 0 || treeIndex >= Trees.Count) return;
            Active.Current.ReturnToOverview(); // Retire only unfinished movement, retaining accepted decisions.
            ShowingB = b; Active.Select(treeIndex); Active.Current.ReturnToOverview(); Revision++;
        }

        public void BeginEdit() { B.BeginEdit(); Revision++; }
        public Outcome<PreparedProfile> StageEdit(string feature, ProfileValue value) => B.StageEdit(feature, value);
        public void CancelEdit() { B.CancelEdit(); Revision++; }
        public Outcome<ProfileComparisonSession> ApplyEdit()
        {
            if (Draft == null) return Failure("NoDraft", "Choose Edit B first.");
            var missing = A.Model.Features.Where(f => Draft.GetValue(f.Id).Kind == ValueKind.NotSupplied).ToArray();
            if (missing.Length > 0) return Outcome<ProfileComparisonSession>.Failure(missing.Select(f =>
                new Diagnostic("IncompleteEdit", "Supply a value or deliberately choose Missing.", featureId: f.Id)));
            return ReplaceB(Draft);
        }
        public Outcome<ProfileComparisonSession> ReplaceB(PreparedProfile profile)
        {
            if (profile == null) return Failure("ProfileRequired", "Select a prepared profile for B.");
            var candidate = EnsembleSession.Create(A.Model, profile);
            if (!candidate.IsSuccess) return Outcome<ProfileComparisonSession>.Failure(candidate.Diagnostics);
            if (!FiniteDifference(A, candidate.Value)) return Failure("ComparisonOverflow", "The profile difference is not finite.");
            int index = Active.Index;
            foreach (var old in B.Trees) old.ReturnToOverview();
            B = candidate.Value; B.Select(index); B.Current.ReturnToOverview();
            Rebuild(); Revision++; return Outcome<ProfileComparisonSession>.Success(this);
        }
        public Outcome<ProfileComparisonSession> ReplaceA(PreparedProfile profile)
        {
            if (profile == null) return Failure("ProfileRequired", "Select a prepared profile for A.");
            var candidate = EnsembleSession.Create(A.Model, profile);
            if (!candidate.IsSuccess) return Outcome<ProfileComparisonSession>.Failure(candidate.Diagnostics);
            if (!FiniteDifference(candidate.Value, B)) return Failure("ComparisonOverflow", "The profile difference is not finite.");
            int index = Active.Index;
            foreach (var old in A.Trees) old.ReturnToOverview();
            A = candidate.Value; A.Select(index); A.Current.ReturnToOverview();
            Rebuild(); Revision++; return Outcome<ProfileComparisonSession>.Success(this);
        }
        public Outcome<ProfileComparisonSession> CopyAToB() => ReplaceB(new PreparedProfile(A.Profile.SchemaVersion,
            A.Profile.ModelId, Guid.NewGuid().ToString("N"), "Copy of " + A.Profile.DisplayName, A.Profile.Values));

        private void Rebuild()
        {
            Trees = Array.AsReadOnly(A.Evaluation.Trees.Select((a, i) => new TreeProfileComparison(i, a, B.Evaluation.Trees[i])).ToArray());
            ChangedFeatures = Array.AsReadOnly(A.Model.Features.Where(f => !A.Profile.GetValue(f.Id).Equals(B.Profile.GetValue(f.Id)))
                .Select(f => f.Id).ToArray());
        }
        private static bool FiniteDifference(EnsembleSession a, EnsembleSession b) =>
            Finite(b.Evaluation.RawScore - a.Evaluation.RawScore) && a.Evaluation.Trees.Select((t, i) =>
                Finite(b.Evaluation.Trees[i].Contribution - t.Contribution)).All(f => f);
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static Outcome<ProfileComparisonSession> Failure(string code, string message) =>
            Outcome<ProfileComparisonSession>.Failure(new[] { new Diagnostic(code, message) });
    }
}
