using System.Linq;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Application
{
    /// <summary>Repeats the fictional three-tree ensemble eight times to teach grouping and cross-tree constraints.</summary>
    public static class EnsembleTeachingExample
    {
        public static ModelDefinition Model(ModelDefinition source) => new ModelDefinition(1, "synthetic-ensemble-24",
            "Synthetic repeated teaching ensemble; not a trained model or production export.", source.Objective, source.OutcomeLabel, .5,
            source.Features, Enumerable.Range(0, 24).Select(i => {
                var tree = source.Trees[i % source.Trees.Count];
                return new ModelTree("ensemble-" + (i + 1).ToString("00"), tree.RootId, tree.Weight, tree.Nodes);
            }));
        public static ProfileSet Profiles(ModelDefinition model, ProfileSet source) => new ProfileSet(1, model.Id,
            source.Profiles.Select(p => new PreparedProfile(1, model.Id, p.Id, p.DisplayName, p.Values)));
    }
}
