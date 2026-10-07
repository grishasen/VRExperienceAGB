using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using VRExperienceAGB.Application;
using VRExperienceAGB.Domain;
using VRExperienceAGB.Import;

namespace VRExperienceAGB.Presentation
{
    /// <summary>Optional search, iteration and authored review tools over the accepted model.</summary>
    public sealed partial class ExplorationExtensions
    {
        private readonly M5ForestPresentation owner;
        private OneTreeExperience View => owner.View;
        private ForestGardenView Garden => owner.Garden;
        private readonly Canvas panel;
        private readonly TMP_Text heading, body, notice;
        private readonly Dictionary<M5Action, GameObject> buttons = new Dictionary<M5Action, GameObject>();
        private ModelDefinition model;
        private PredictorSearch search;
        private string[] filteredFeatures = Array.Empty<string>();
        private string featureFilter = "";
        private int featureIndex, occurrenceIndex, detailPage = 1, tab;
        private PredictorScope scope;
        private string evaluationId;
        private IReadOnlyList<PredictorOccurrence> matches = Array.Empty<PredictorOccurrence>();
        public bool PanelOpen { get; private set; }
        public bool SearchActive { get; private set; }
        public bool TrailActive { get; private set; }
        public int Iteration { get; private set; }
        public string Feature => filteredFeatures.Length > 0 ? filteredFeatures[featureIndex] : null;
        public int MatchCount => matches.Count;
        public string BodyText => body.text;
        public bool Matches(int tree, string node = null) => SearchActive && matches.Any(m => m.TreeIndex == tree && (node == null || m.Node.Id == node));
        public bool TreeVisible(int index) => !TrailActive || index < Iteration;
        private static string N(double value) => value.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture);

        public ExplorationExtensions(M5ForestPresentation owner, Transform root)
        {
            this.owner = owner;
            panel = owner.Panel("ExplorationExtensions", new Vector2(1160, 920), root);
            panel.overrideSorting = true; panel.sortingOrder = 110;
            heading = Label("Heading", new Vector2(0, 405), new Vector2(1100, 55), 30);
            body = Label("Body", new Vector2(0, 92), new Vector2(1080, 415), 25);
            body.alignment = TextAlignmentOptions.TopLeft; body.overflowMode = TextOverflowModes.Page;
            notice = Label("Notice", new Vector2(0, -400), new Vector2(1080, 95), 21);
            Add("Predictors", -370, 330, M5Action.ExtensionSearch);
            Add("Boosting trail", 0, 330, M5Action.ExtensionTrail);
            Add("Guided review", 370, 330, M5Action.ExtensionReview);
            Add("Previous feature", -370, -150, M5Action.SearchPreviousFeature);
            Add("Next feature", 0, -150, M5Action.SearchNextFeature);
            Add("Find feature", 0, -150, M5Action.SearchFilter);
            Add("Entire model / path", 370, -150, M5Action.SearchScope);
            Add("Previous match", -370, -220, M5Action.SearchPreviousMatch);
            Add("Next match", 0, -220, M5Action.SearchNextMatch);
            Add("Inspect match", 370, -220, M5Action.SearchInspect);
            Add("Baseline", -370, -150, M5Action.TrailBaseline);
            Add("Previous iteration", 0, -150, M5Action.TrailPrevious);
            Add("Next iteration", 370, -150, M5Action.TrailNext);
            Add("Go to iteration", 0, -150, M5Action.TrailJump);
            Add("All iterations", -370, -220, M5Action.TrailLast);
            Add("Reveal trees: off", 0, -220, M5Action.TrailReveal);
            Add("Inspect tree", 370, -220, M5Action.TrailInspect);
            Add("X · Close", -370, -300, M5Action.ExtensionClose);
            Add("Clear overlays", 0, -300, M5Action.ExtensionClear);
            Add("More detail", 370, -300, M5Action.ExtensionMore);
            foreach (var row in new[] {
                new[] { M5Action.SearchPreviousFeature, M5Action.SearchNextFeature, M5Action.SearchFilter, M5Action.SearchScope },
                new[] { M5Action.TrailBaseline, M5Action.TrailPrevious, M5Action.TrailNext, M5Action.TrailJump } })
                for (int i = 0; i < row.Length; i++) {
                    var rect = (RectTransform)buttons[row[i]].transform; rect.anchoredPosition = new Vector2(-420 + 280 * i, -150); rect.sizeDelta = new Vector2(265, 56);
                    buttons[row[i]].GetComponentInChildren<TMP_Text>().rectTransform.sizeDelta = new Vector2(250, 48);
                }
            BuildReview();
            panel.gameObject.SetActive(false);
        }
        private TMP_Text Label(string name, Vector2 position, Vector2 size, int font)
        { var t = Garden.Text(panel.transform, name, "", position, size, font); t.richText = false; return t; }
        private void Add(string label, float x, float y, M5Action action) => buttons.Add(action,
            owner.Button(panel, label, new Vector2(x, y), new Vector2(345, 56), action, 0, 23));
        public void EnsureModel()
        {
            if (model == View.Model) return;
            model = View.Model; search = new PredictorSearch(model); filteredFeatures = search.Features.ToArray(); featureFilter = ""; featureIndex = occurrenceIndex = 0;
            matches = Array.Empty<PredictorOccurrence>(); scope = PredictorScope.EntireModel;
            SearchActive = TrailActive = false; Iteration = model.Trees.Count;
            evaluationId = null; PanelOpen = false; panel.gameObject.SetActive(false);
            restoreReview = null; StopReview("Model changed. Review targets must be checked before replay.");
        }
        public void Open(int selectedTab = 0)
        {
            EnsureModel();
            if (Garden.Navigation.TreeMenuOpen) Garden.Navigation.ToggleTreeMenu();
            if (ReviewRunning) { ReviewPaused = true; View.Session.SetPaused(true); transport.gameObject.SetActive(false); }
            tab = selectedTab; PanelOpen = true; detailPage = 1;
            if (tab == 0) { SearchActive = true; TrailActive = false; UpdateMatches(); }
            Place(); View.NameTooltip?.Hide(); View.Refresh();
        }
        private void Place()
        {
            var head = Garden.Locomotion.Head; var forward = head.forward; forward.y = 0;
            if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
            forward.Normalize(); panel.transform.rotation = Quaternion.LookRotation(forward);
            panel.transform.position = head.position + forward * 1.65f - Vector3.up * .08f;
        }
        public void Close() { PanelOpen = false; editingText = false; if (ReviewRunning) UpdateCaption(); panel.gameObject.SetActive(false); View.Refresh(); }
        public void RefreshMatches() { EnsureModel(); UpdateMatches(); }
        private void UpdateMatches()
        {
            matches = search.Query(Feature, scope, View.Ensemble);
            occurrenceIndex = Math.Min(occurrenceIndex, Math.Max(0, matches.Count - 1));
            evaluationId = View.Ensemble.EvaluationId;
        }
        public void Activate(M5Action action)
        {
            if (editingText && action != M5Action.ReviewBackspace && action != M5Action.ReviewTextDone) return;
            if (action == M5Action.Extensions) { Open(); return; }
            if (!PanelOpen) { if (ReviewRunning && action >= M5Action.ReviewCapture) ActivateReview(action); return; }
            if (action == M5Action.ExtensionSearch) { Open(0); return; }
            if (action == M5Action.ExtensionTrail) { Open(1); return; }
            if (action == M5Action.ExtensionReview) { Open(2); return; }
            if (action >= M5Action.ReviewCapture) { ActivateReview(action); Refresh(); return; }
            switch (action)
            {
                case M5Action.ExtensionClose: Close(); return;
                case M5Action.ExtensionClear: SearchActive = TrailActive = false; Garden.M6.LinkFeature(null); break;
                case M5Action.ExtensionMore: body.ForceMeshUpdate(); detailPage = detailPage % Math.Max(1, body.textInfo.pageCount) + 1; break;
                case M5Action.SearchPreviousFeature: ChangeFeature(-1); break;
                case M5Action.SearchNextFeature: ChangeFeature(1); break;
                case M5Action.SearchFilter: BeginTextEntry(1, featureFilter); break;
                case M5Action.TrailJump: BeginTextEntry(2, Iteration.ToString()); break;
                case M5Action.SearchScope: scope = scope == PredictorScope.EntireModel ? PredictorScope.CurrentProfilePath : PredictorScope.EntireModel; occurrenceIndex = 0; SearchActive = true; break;
                case M5Action.SearchPreviousMatch: if (matches.Count > 0) occurrenceIndex = (occurrenceIndex + matches.Count - 1) % matches.Count; detailPage = 1; break;
                case M5Action.SearchNextMatch: if (matches.Count > 0) occurrenceIndex = (occurrenceIndex + 1) % matches.Count; detailPage = 1; break;
                case M5Action.SearchInspect:
                    if (matches.Count > 0) { var m = matches[occurrenceIndex]; Close(); Inspect(m.TreeIndex, m.Node.Id); } return;
                case M5Action.TrailBaseline: Iteration = 0; break;
                case M5Action.TrailPrevious: Iteration = Math.Max(0, Iteration - 1); break;
                case M5Action.TrailNext: Iteration = Math.Min(model.Trees.Count, Iteration + 1); break;
                case M5Action.TrailLast: Iteration = model.Trees.Count; break;
                case M5Action.TrailReveal: TrailActive = !TrailActive; SearchActive = false; break;
                case M5Action.TrailInspect: if (Iteration > 0) { Close(); Inspect(Iteration - 1, null); } return;
            }
            UpdateMatches(); View.Refresh();
        }
        public void FilterFeatures(string text)
        {
            featureFilter = text ?? "";
            filteredFeatures = search.Features.Where(f => f.IndexOf(featureFilter, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            featureIndex = occurrenceIndex = 0; SearchActive = true; UpdateMatches();
        }
        private void ChangeFeature(int delta)
        {
            if (filteredFeatures.Length == 0) return;
            featureIndex = (featureIndex + delta + filteredFeatures.Length) % filteredFeatures.Length;
            occurrenceIndex = 0; detailPage = 1; SearchActive = true;
        }
        public void Inspect(int tree, string node)
        {
            if (Garden.Navigation.TreeMenuOpen) Garden.Navigation.ToggleTreeMenu();
            if (Garden.Navigation.Page != NavigationPage.Forest) Garden.Navigation.ShowForest();
            Garden.Navigation.OpenTree(tree);
            if (node != null) View.FocusedView.Inspect(node);
            View.Refresh();
        }
        public void Refresh()
        {
            if (!View.Ready) return;
            EnsureModel();
            if (SearchActive || evaluationId != View.Ensemble.EvaluationId) UpdateMatches();
            panel.gameObject.SetActive(PanelOpen);
            if (!PanelOpen) return;
            if(View.Director!=null)Place();
            foreach (var item in buttons)
            {
                bool searchButton = item.Key >= M5Action.SearchPreviousFeature && item.Key <= M5Action.SearchInspect;
                bool trailButton = item.Key >= M5Action.TrailBaseline && item.Key <= M5Action.TrailInspect;
                bool reviewButton = item.Key >= M5Action.ReviewCapture;
                item.Value.SetActive(searchButton ? tab == 0 : trailButton ? tab == 1 : reviewButton ? tab == 2 : true);
            }
            body.rectTransform.sizeDelta = new Vector2(1080, 415);
            body.rectTransform.anchoredPosition = new Vector2(0, 92);
            if (tab == 0) RefreshSearch();
            else if (tab == 1) RefreshTrail();
            else RefreshReview();
            body.pageToDisplay = detailPage; body.ForceMeshUpdate();
            notice.text += "\nDetail page " + detailPage + " / " + Math.Max(1, body.textInfo.pageCount);
            Enable(M5Action.ExtensionMore, body.textInfo.pageCount > 1);
            RefreshKeyboard();
        }
        private void Enable(M5Action action, bool enabled) => buttons[action].GetComponent<UnityEngine.UI.Button>().interactable = enabled;
        private void RefreshSearch()
        {
            heading.text = "PREDICTOR FIREFLIES";
            string scopeText = scope == PredictorScope.EntireModel ? "Entire model" : "Current profile path";
            body.text = "Feature " + (filteredFeatures.Length == 0 ? 0 : featureIndex + 1) + " / " + filteredFeatures.Length + " (filter: " + (featureFilter.Length == 0 ? "all" : featureFilter) + ")\n" + (Feature ?? "No matching predictors. Clear or change Find feature.") +
                "\nScope: " + scopeText + " · " + matches.Count + " splits in " + matches.Select(m => m.TreeIndex).Distinct().Count() + " trees\n";
            if (scope == PredictorScope.CurrentProfilePath && View.Ensemble.Evaluation == null)
                body.text += "A verified prepared profile is required. Open Compare synthetic profiles to try this scope.\n";
            else if (scope == PredictorScope.CurrentProfilePath) body.text += "Profile: " + View.Ensemble.Profile.DisplayName + "\n";
            if (matches.Count > 0)
            {
                var m = matches[occurrenceIndex];
                body.text += "Match " + (occurrenceIndex + 1) + " / " + matches.Count + " · iteration " + (m.TreeIndex + 1) + "\n" +
                    Garden.M6.Analysis.NodeDetail(m.TreeIndex, m.Node.Id);
            }
            notice.text = "Markers use exact feature IDs. Occurrence and gain describe model structure, not causality.";
            foreach (var a in new[] { M5Action.SearchPreviousMatch, M5Action.SearchNextMatch, M5Action.SearchInspect }) Enable(a, matches.Count > 0);
            Enable(M5Action.SearchPreviousFeature, filteredFeatures.Length > 1); Enable(M5Action.SearchNextFeature, filteredFeatures.Length > 1);
        }
        private void RefreshTrail()
        {
            heading.text = "BOOSTING TRAIL · ENSEMBLE PROGRESSION";
            var points = BoostingProgression.Points(View.Ensemble);
            body.text = "Iteration " + Iteration + " / " + model.Trees.Count + " · 0 is the baseline\n";
            if (points.Count > 0)
            {
                var point = points[Iteration]; var final = points[points.Count - 1];
                body.text += "Profile: " + View.Ensemble.Profile.DisplayName + "\nIntermediate raw score " + N(point.RawScore) +
                    " · probability " + N(point.Probability * 100) + "%\nFull-model raw score " + N(final.RawScore) + " · probability " + N(final.Probability * 100) + "%\n";
                if (Iteration > 0) body.text += "This tree's weighted contribution: " + N(View.Ensemble.Evaluation.Trees[Iteration - 1].Contribution) + "\n";
            }
            else body.text += "Structure / manual preview: no profile probability.\nUse Compare synthetic profiles for verified intermediate results.\n";
            if (Iteration > 0) body.text += "\n" + Garden.M6.Analysis.Passport(Iteration - 1);
            body.text += "\n\nHeight = depth; crown = leaf count. Stored gain describes splits. Signed contribution belongs to this profile.\nThis is ensemble progression, not a training-loss curve.";
            notice.text = "Reveal trees changes visibility only. Every tree remains in evaluation. Return from inspection restores your forest location.";
            buttons[M5Action.TrailReveal].GetComponentInChildren<TMP_Text>().text = "Reveal trees: " + (TrailActive ? "on" : "off");
            Enable(M5Action.TrailInspect, Iteration > 0); Enable(M5Action.TrailPrevious, Iteration > 0); Enable(M5Action.TrailNext, Iteration < model.Trees.Count);
        }
    }
}
