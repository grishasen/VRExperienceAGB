using System;
using System.Linq;
using System.Globalization;
using TMPro;
using UnityEngine;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Presentation
{
    public sealed partial class ExperienceDirector
    {
        private string valuesReturnPage = "profiles";
        public bool DifferencesOnly { get; private set; }
        public string ValueSearch { get; private set; } = "";
        private const int InputRows = 4;
        private int inputDetailIndex;
        public void SetValueSearch(string query) { ValueSearch = query ?? ""; valuePage = 0; if (MenuPage == "values") BuildMenu(); }
        private static bool SameValue(ProfileValue a, ProfileValue b) => a.Kind == b.Kind && a.Number == b.Number && a.Category == b.Category;
        private void BuildInputs()
        {
            title.text = "PROFILE INPUTS";
            var profiles = View.AvailableProfiles.Profiles;
            if (profiles.Count == 0) { subtitle.text = "No profiles loaded. Open Models / profiles to import compatible inputs."; Row("Models / profiles", "library", 0); return; }
            SelectedA = Math.Min(SelectedA, profiles.Count - 1); SelectedB = Math.Min(SelectedB, profiles.Count - 1);
            var a = profiles[SelectedA]; var b = valuesReturnPage == "compare" ? profiles[SelectedB] : null;
            var features = View.Model.Features.Where(f => (ValueSearch.Length == 0 || f.Id.IndexOf(ValueSearch, StringComparison.OrdinalIgnoreCase) >= 0 || View.FeatureLabel(f.Id).IndexOf(ValueSearch, StringComparison.OrdinalIgnoreCase) >= 0) &&
                (!DifferencesOnly || b == null || !SameValue(a.GetValue(f.Id), b.GetValue(f.Id)))).ToArray();
            int pages = Math.Max(1, (features.Length + InputRows - 1) / InputRows);
            valuePage = Math.Max(0, Math.Min(valuePage, pages - 1));
            subtitle.text = "A: " + a.DisplayName + (b == null ? "" : " · B: " + b.DisplayName) + "\nPage " + (valuePage + 1) + " / " + pages + " · " + features.Length + " inputs";
            AddButton(content, "Back to selection", new Vector2(-320,175), new Vector2(300,60), "values-back");
            AddButton(content, DifferencesOnly ? "Differences only" : "All inputs", new Vector2(0,175), new Vector2(300,60), "values-diff");
            AddButton(content, ValueSearch.Length == 0 ? "Search inputs" : "Search: " + ValueSearch, new Vector2(320,175), new Vector2(300,60), "values-search");
            InputCell("Column", "FEATURE / UNIT", -230, 118, 490, 32);
            InputCell("ColumnA", "A", 120, 118, 180, 32);
            if (b != null) InputCell("ColumnB", "B", 350, 118, 210, 32);
            int row = 0;
            foreach (var f in features.Skip(valuePage * InputRows).Take(InputRows))
            {
                float y = 63 - row * 50;
                AddButton(content,"",new Vector2(0,y),new Vector2(965,46),"input-detail:"+View.Model.Features.ToList().IndexOf(f));
                var av = a.GetValue(f.Id); var bv = b?.GetValue(f.Id);
                InputCell("Feature-" + row, View.FeatureLabel(f.Id) + (string.IsNullOrEmpty(f.Unit) ? "" : " (" + f.Unit + ")"), -230, y, 490, 46).color=PredictorPalette.ForFeature(f.Id);
                InputCell("A-" + row, Value(av), 120, y, 180, 46);
                if (b != null) InputCell("B-" + row, (SameValue(av,bv.Value) ? "= " : "≠ ") + Value(bv.Value), 350, y, 210, 46);
                row++;
            }
            if (features.Length == 0) InputCell("NoInputs", View.Model.Features.Count==0?"This model has only constant leaves and uses no predictors.":"No inputs match this filter.", 0, 40, 900, 80);
            AddButton(content,"Previous",new Vector2(-320,-153),new Vector2(300,68),"values-prev");
            AddButton(content,"Details",new Vector2(0,-153),new Vector2(300,68),"profile-source");
            AddButton(content,"Next",new Vector2(320,-153),new Vector2(300,68),"values-next");
        }
        private string InputDetailCopy()
        {
            if(View.AvailableProfiles.Profiles.Count==0 || inputDetailIndex>=View.Model.Features.Count)return "No input selected.";
            var f=View.Model.Features[inputDetailIndex];var a=View.AvailableProfiles.Profiles[SelectedA];
            var b=valuesReturnPage=="compare"?View.AvailableProfiles.Profiles[SelectedB]:null;
            string Exact(ProfileValue v)=>v.Kind==ValueKind.Number?v.Number.ToString("G17",CultureInfo.InvariantCulture):Value(v);
            return View.FeatureLabel(f.Id)+"\nID: "+f.Id+"\nType: "+f.Kind+" · Unit: "+(f.Unit??"not supplied")+
                "\n\nExact values\nA · "+a.DisplayName+": "+Exact(a.GetValue(f.Id))+(b==null?"":"\nB · "+b.DisplayName+": "+Exact(b.GetValue(f.Id)))+
                "\n\nMissing and Not supplied are distinct input states. Names do not establish feature-preparation rules.";
        }
        private TMP_Text InputCell(string name, string text, float x, float y, float width, float height)
        {
            var label = Garden.Text(content, name, text, new Vector2(x,y), new Vector2(width,height), 23);
            label.richText = false; label.alignment = TextAlignmentOptions.MidlineLeft;
            label.overflowMode = TextOverflowModes.Ellipsis;
            return label;
        }
    }
}
