using System.Globalization;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Import
{
    public static class CdhSourceText
    {
        public static string Describe(PreparedProfile profile)
        {
            if (profile == null) return "No profile selected.";
            var source = profile.Source;
            if (source == null) return profile.DisplayName + "\nNo recorded CDH execution attached. Edited copies are hypothetical inputs.";
            return profile.DisplayName + "\nCDH record " + source.Record + " · execution " + source.ExecutionIndex +
                "\nAction: " + source.Action + "\nTreatment: " + source.Treatment +
                "\nRule: " + source.RuleName + "\nComponent: " + source.Component +
                "\nRecorded technique: " + source.Technique + "\nRecorded version: " + source.ModelVersion +
                "\nRecorded update timestamp: " + source.UpdateTime +
                "\nRecorded CDH propensity: " + (source.RecordedPropensity.HasValue ? source.RecordedPropensity.Value.ToString("R", CultureInfo.InvariantCulture) : "not supplied") +
                "\nCDH reconciliation deferred. This recorded output is never substituted for the exported-tree calculation.";
        }
    }
}
