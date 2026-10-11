using System.Linq;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Application
{
    /// <summary>Prepare a replacement without mutating the accepted model or route.</summary>
    public static class ModelActivation
    {
        public static Outcome<EnsembleSession> Prepare(ModelDefinition model, ProfileSet profiles)
        {
            var candidate = EnsembleSession.Create(model);
            if (!candidate.IsSuccess) return candidate;
            if (profiles == null || profiles.ModelId != model.Id)
                return Outcome<EnsembleSession>.Failure(new[] { new Diagnostic("ProfileModelMismatch", "Profiles must belong to the selected model.") });
            foreach (var profile in profiles.Profiles)
            {
                var evaluated = ModelEvaluator.Evaluate(model, profile);
                if (!evaluated.IsSuccess) return Outcome<EnsembleSession>.Failure(evaluated.Diagnostics);
            }
            return candidate;
        }
    }
}
