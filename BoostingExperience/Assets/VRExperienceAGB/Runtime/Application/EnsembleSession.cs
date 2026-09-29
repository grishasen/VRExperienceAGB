using System.Linq;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Application
{
    /// <summary>Owns independent tree progress and a complete profile evaluation, never a rendered subset.</summary>
    public sealed class EnsembleSession
    {
        private TreeSession[] sessions;
        public ModelDefinition Model { get; }
        public PreparedProfile OriginalProfile { get; }
        public PreparedProfile Profile { get; private set; }
        public EvaluationResult Evaluation { get; private set; }
        public ExperienceMode Mode { get; private set; }
        public int Index { get; private set; }
        public TreeSession Current => sessions[Index];
        public int CompletedCount => sessions.Count(s => s.State.AtLeaf);
        public bool Complete => CompletedCount == sessions.Length;
        public double RouteTotal => Model.BaseScore + sessions.Sum(s => s.State.Contribution);
        public System.Collections.Generic.IReadOnlyList<TreeSession> Trees => System.Array.AsReadOnly(sessions);
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
            var outcomes = model.Trees.Select(t => profile == null ? TreeSession.CreateManual(model, t.Id) : TreeSession.CreatePrepared(model, t.Id, profile)).ToArray();
            var failures = outcomes.Where(o => !o.IsSuccess).SelectMany(o => o.Diagnostics).ToArray();
            if (failures.Length > 0) return Outcome<EnsembleSession>.Failure(failures);
            return Outcome<EnsembleSession>.Success(new EnsembleSession(model, profile, outcomes.Select(o => o.Value).ToArray(), evaluation));
        }
        public bool Select(int index)
        {
            if (index < 0 || index >= sessions.Length || index == Index) return false;
            Current.ReturnToOverview(); Index = index;
            Current.ReturnToOverview(); Current.EnterTree();
            return true;
        }
        public void TakeOver()
        {
            Mode = ExperienceMode.Manual; Evaluation = null;
            foreach (var session in sessions) if (!ReferenceEquals(session, Current)) session.UseManualMode();
        }
        public Outcome<EnsembleSession> Edit(string feature, ProfileValue value)
        {
            if (Profile == null) return Outcome<EnsembleSession>.Failure(new[] { new Diagnostic("NoProfile", "Select a profile before editing.") });
            var candidate = Create(Model, Profile.WithValue(feature, value));
            if (!candidate.IsSuccess) return candidate;
            // Validate and evaluate first; failed edits preserve the last valid session atomically.
            Profile = candidate.Value.Profile; Evaluation = candidate.Value.Evaluation;
            sessions = candidate.Value.sessions; Index = 0; Mode = ExperienceMode.PreparedProfile;
            return Outcome<EnsembleSession>.Success(this);
        }
        public void RestoreOriginal()
        {
            if (OriginalProfile == null) return;
            var restored = Create(Model, OriginalProfile).Value;
            Profile = OriginalProfile; Evaluation = restored.Evaluation; sessions = restored.sessions; Index = 0; Mode = ExperienceMode.PreparedProfile;
        }
    }
}
