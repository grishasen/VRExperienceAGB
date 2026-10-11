using System;
using System.Linq;
using System.Text;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Import
{
    /// <summary>Evidence-backed copy; no fixed business-family or platform assumptions.</summary>
    public static class ModelInsightText
    {
        public const string CalibrationNotice = "Propensity comes from calibration. No calibration table is loaded; only score is shown.";
        public static string Passport(ForestExplanation a)
        {
            var m=a.Model; var splits=m.Trees.SelectMany(t=>t.Nodes.OfType<SplitNode>()).ToArray();
            return "MODEL: "+m.Id+"\nOUTCOME: "+m.OutcomeLabel+"\n"+a.ModelStory()+
                "\n\nSTRUCTURE\n"+m.Trees.Sum(t=>t.Nodes.Count)+" nodes · "+splits.Length+" splits · "+a.Trees.Sum(t=>t.Leaves)+" leaves\nMaximum depth (root = 0): "+a.Trees.Max(t=>t.Depth)+
                "\n"+string.Join(" · ",splits.GroupBy(s=>s.Condition.Operator).Select(g=>g.Key+": "+g.Count()))+
                "\n\nPROVENANCE\n"+m.Provenance+"\nSource SHA-256: "+(a.SourceSha256??"not supplied")+
                "\nMonitoring response rate is not an individual probability. AUC measures ranking, not calibration or accuracy.";
        }
        public static string Structure(ForestExplanation a)
        {
            var m=a.Model; var text=new StringBuilder("STORED GAIN AND STRUCTURE\nGain is not SHAP, traffic share or individual contribution.\nNamespace labels do not establish business meaning.\n");
            foreach(var family in a.Families) {
                var nodes=m.Trees.SelectMany(t=>t.Nodes.OfType<SplitNode>()).Where(n=>ForestExplanation.Family(n.FeatureId)==family).ToArray();
                double gain=a.Trees.Sum(t=>t.FamilyGains.TryGetValue(family,out var value)?value:0);
                text.AppendLine(family+": "+nodes.Length+" splits · "+nodes.Select(n=>n.FeatureId).Distinct().Count()+" predictors · "+(a.TotalGain>0?ForestExplanation.Number(100*gain/a.TotalGain.Value)+"% stored gain":"gain unavailable"));
            }
            if(a.TotalGain>0) {
                text.AppendLine("\nPREDICTORS RANKED BY STORED GAIN");double cumulative=0;
                foreach(var p in a.PredictorGains.OrderByDescending(p=>p.Value).ThenBy(p=>p.Key,StringComparer.Ordinal)) {
                    cumulative+=p.Value;
                    text.AppendLine(a.Label(p.Key)+": "+ForestExplanation.Number(100*p.Value/a.TotalGain.Value)+"% · cumulative "+ForestExplanation.Number(100*cumulative/a.TotalGain.Value)+"%");
                }
            }
            text.AppendLine("\nLEAF DEPTH BY TREE");
            foreach(var t in a.Trees.GroupBy(t=>t.Depth).OrderBy(g=>g.Key))text.AppendLine("Maximum depth "+t.Key+": "+t.Count()+" trees");
            text.AppendLine("\nMOST USED PREDICTORS (SPLIT COUNT)");
            foreach(var f in m.Features.OrderByDescending(f=>a.Occurrences(f.Id)).ThenBy(f=>f.Id,StringComparer.Ordinal))
                text.AppendLine(a.Label(f.Id)+": "+a.Occurrences(f.Id)+" splits in "+a.MatchingTrees(f.Id).Length+" trees");
            text.AppendLine("\nUse Search predictors to inspect actual source nodes. Co-occurrence does not prove an interaction; a low count is not proof of a defective branch.");
            return text.ToString();
        }
        public static string Dictionary(ForestExplanation a)
        {
            var text=new StringBuilder("FEATURE DICTIONARY\nNames and namespaces alone do not establish units or preparation rules.\n");
            foreach(var f in a.Model.Features) {
                text.AppendLine("\n"+a.Label(f.Id)+"\nID: "+f.Id+"\nType: "+f.Kind+" · Unit: "+(f.Unit??"not supplied"));
                text.AppendLine(a.Model.StructureOnlyPreview?"Domain, missing and preparation policy: unverified":a.Model.ExportInfo!=null?"Conditions inferred from exported splits. Missing requires an explicit missing predicate on the evaluated route; category lists are not closed domains.":
                    "Supplied missing policy: "+(f.AllowMissing?"allowed":"not allowed")+" · Preparation rules: not supplied");
                if(f.Categories.Count>0) text.AppendLine((a.Model.StructureOnlyPreview || a.Model.ExportInfo!=null?"Observed category tokens: ":"Supplied categories: ")+string.Join(", ",f.Categories));
                if(f.Minimum.HasValue || f.Maximum.HasValue)text.AppendLine("Supplied range: "+(f.Minimum?.ToString("G17")??"unbounded")+" to "+(f.Maximum?.ToString("G17")??"unbounded"));
            }
            return text.ToString();
        }
        public const string Glossary = "PREDICTOR\nAn input read by a decision. Its complete identifier must match the profile.\n\nBRANCH\nNumeric < is strict: equality goes to the false branch. Category membership is exact and case-sensitive. Missing follows the supplied explicit policy.\n\nLEAF CONTRIBUTION\nThe reached leaf score times the tree weight. A signed amount in raw-score space, not a probability.\n\nCOMPLETE RESULT\nScore = baseline + reached contributions from every tree. Propensity comes from a separate calibration mapping, not directly from score. No calibration table is loaded. An intermediate prefix is not the complete result.\n\nMANUAL ROUTE SCORE\nA total for chosen branches. It is not a profile prediction.\n\nGAIN\nStored structural split usefulness. It is not SHAP or a causal effect.\n\nOBSERVATION COUNT\nEvidence recorded for a node; not current traffic share, observed positives or a confidence interval.\n\nTHRESHOLD\nA model cutpoint, not evidence of a learning-memory horizon.\n\nA/B CHANGE\nModel sensitivity to changed inputs; not a causal promise.";
        public static string Verification(ForestExplanation a) => "VERIFICATION STATUS\n\nStructure: validated on import; all trees retained.\nInput policy: "+(a.Model.StructureOnlyPreview?"unverified; scoring disabled":a.Model.ExportInfo!=null?"exported conditions; exact input types checked, empty used values rejected; CDH reconciliation deferred":"supplied normalized contract; profiles validated before playback")+
            "\nCalculation: all trees evaluated for accepted prepared profiles.\nSource-scorer agreement: not verified.\n\nSOURCE\n"+(a.SourceSha256??"No bound source fingerprint supplied.")+"\n"+a.Model.Provenance+
            "\n\nTO VERIFY WITH THE MODEL OWNER\nObtain approved profiles, reference predictions and visited leaves. Cover equality, Missing, absent values and unseen categories. Record preparation rules, actual platform version, calibration and comparison tolerance.\n\nA successful structural parse or synthetic fixture test does not certify production scoring. One snapshot cannot establish drift dates, node age, fairness, generalization or causality.";
    }
}
