using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Import
{
    /// <summary>Enable deterministic scoring of supported exported trees, independently of CDH output reconciliation.</summary>
    public static class AgbScoringImport
    {
        public static Outcome<AgbPreviewResult> Read(string json, string fileName)
        {
            var parsed = AgbStructurePreview.Read(json, fileName);
            if (!parsed.IsSuccess) return parsed;
            var source = parsed.Value;
            string name = ModelFileLibrary.DisplayName(fileName ?? "AGB export");
            if (name.StartsWith("export_", StringComparison.OrdinalIgnoreCase)) name = name.Substring(7);
            name = Regex.Replace(name, @"_\d{8}T\d{6}$", "");
            var identity = new AgbExportInfo(name, source.Monitoring.Version, source.Sha256, source.Monitoring.Updated);
            var model = new ModelDefinition(1, "agb-trees-" + source.Sha256,
                "AGB exported-tree calculation: " + name + ". Sum every reached leaf; learning rate is already serialized. Propensity requires a separate calibration mapping, which is not supplied.",
                "binary_logistic", "Ensemble score", 0, source.Model.Features, source.Model.Trees, exportInfo: identity);
            var errors = ModelValidator.Validate(model);
            return errors.Count == 0
                ? Outcome<AgbPreviewResult>.Success(new AgbPreviewResult(model,
                    source.Metadata.ToDictionary(p => p.Key, p => p.Value), source.Sha256, source.Monitoring))
                : Outcome<AgbPreviewResult>.Failure(errors);
        }
    }
}
