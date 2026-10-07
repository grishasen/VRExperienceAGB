using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using VRExperienceAGB.Application;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Presentation
{
    /// <summary>Controller-accessible comparison and B draft editor over two independent sessions.</summary>
    public sealed class ProfileComparisonPresentation
    {
        private readonly M5ForestPresentation owner;
        private OneTreeExperience View => owner.View;
        private ProfileComparisonSession Pair => View.Comparison;
        private readonly Canvas panel;
        private readonly TMP_Text heading, totals, detail, notice;
        private readonly Dictionary<M5Action, GameObject> buttons = new Dictionary<M5Action, GameObject>();
        private readonly Transform[] drops = new Transform[2];
        private readonly Material[] dropMaterials = new Material[2];
        private int tree, feature, detailPage = 1;
        private string message = "";
        public bool PanelOpen { get; private set; }
        public int SelectedTree => tree;
        public string DetailText => detail.text;
        public string SummaryText => totals.text;
        private bool Editing => Pair?.Draft != null;
        private static string Number(double n) => n.ToString("0.######", CultureInfo.InvariantCulture);
        private static string Signed(double n) => (n > 0 ? "+" : "") + Number(n);
        private static string Value(ProfileValue v) => v.Kind == ValueKind.Number ? v.Number.ToString("G17", CultureInfo.InvariantCulture) :
            v.Kind == ValueKind.Category ? v.Category : v.Kind == ValueKind.Missing ? "Missing (explicit null)" : "Not supplied";

        public ProfileComparisonPresentation(M5ForestPresentation owner, Transform root)
        {
            this.owner = owner;
            panel = owner.Panel("ProfileComparison", new Vector2(1180, 920), root);
            panel.overrideSorting = true; panel.sortingOrder = 100;
            panel.GetComponent<UnityEngine.UI.Image>().color = new Color(.025f, .065f, .085f, 1f);
            heading = Text("Heading", new Vector2(0, 404), new Vector2(1110, 66), 28);
            totals = Text("Totals", new Vector2(0, 290), new Vector2(1110, 155), 25);
            detail = Text("Details", new Vector2(0, 65), new Vector2(1090, 235), 24);
            detail.alignment = TextAlignmentOptions.TopLeft; detail.overflowMode = TextOverflowModes.Page;
            notice = Text("Notice", new Vector2(0, -397), new Vector2(1090, 100), 21);
            Add("Previous tree", -375, -95, M5Action.ComparePreviousTree);
            Add("Next tree", 0, -95, M5Action.CompareNextTree);
            Add("Next difference", 375, -95, M5Action.CompareNextDifference);
            Add("Next profile A", -375, -165, M5Action.CompareNextA);
            Add("Next profile B", 0, -165, M5Action.CompareNextB);
            Add("Copy A to B", 375, -165, M5Action.CompareCopyA);
            Add("Follow A", -375, -235, M5Action.CompareA);
            Add("Follow B", 0, -235, M5Action.CompareB);
            Add("Edit B", 375, -235, M5Action.CompareEdit);
            Add("Close", -375, -305, M5Action.CompareClose);
            Add("Exit comparison", 0, -305, M5Action.CompareExit);
            Add("More detail", 375, -305, M5Action.CompareNextDetail);
            Add("Inspect split", 0, -305, M5Action.CompareInspect);
            var bottom = new[] { M5Action.CompareClose, M5Action.CompareExit, M5Action.CompareInspect, M5Action.CompareNextDetail };
            for (int i = 0; i < bottom.Length; i++)
            {
                var rect = (RectTransform)buttons[bottom[i]].transform;
                rect.anchoredPosition = new Vector2(-420 + i * 280, -305); rect.sizeDelta = new Vector2(265, 56);
                buttons[bottom[i]].GetComponentInChildren<TMP_Text>().rectTransform.sizeDelta = new Vector2(250, 48);
            }
            Add("Previous feature", -375, -95, M5Action.ComparePreviousFeature);
            Add("Next feature", 0, -95, M5Action.CompareNextFeature);
            Add("Set Missing", 375, -95, M5Action.CompareMissing);
            Add("Decrease / previous", -375, -165, M5Action.CompareDecrease);
            Add("Increase / next", 0, -165, M5Action.CompareIncrease);
            Add("Apply B", -375, -305, M5Action.CompareApply);
            Add("Cancel edit", 0, -305, M5Action.CompareCancel);
            panel.gameObject.SetActive(false);
            for (int i = 0; i < 2; i++)
            {
                var marker = new GameObject(i == 0 ? "ProfileDropA" : "ProfileDropB");
                marker.transform.SetParent(View.presentationRoot, false); drops[i] = marker.transform;
                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere); sphere.name = "Drop";
                UnityEngine.Object.Destroy(sphere.GetComponent<Collider>());
                sphere.transform.SetParent(marker.transform, false); sphere.transform.localScale = Vector3.one * .15f;
                dropMaterials[i] = new Material(View.activeMaterial) { name = i == 0 ? "ProfileA_Violet" : "ProfileB_Blue" };
                var color = i == 0 ? new Color(.85f, .35f, 1f) : new Color(.30f, .50f, 1f);
                dropMaterials[i].color = color; dropMaterials[i].EnableKeyword("_EMISSION"); dropMaterials[i].SetColor("_EmissionColor", color);
                sphere.GetComponent<Renderer>().sharedMaterial = dropMaterials[i];
                var label = owner.Garden.CanvasAt("Identity", Vector3.up * .17f, new Vector2(90, 60), .003f, marker.transform);
                owner.Garden.Text(label.transform, "Label", i == 0 ? "A" : "B", Vector2.zero, new Vector2(90, 60), 38);
                marker.SetActive(false);
            }
        }
        private TMP_Text Text(string name, Vector2 position, Vector2 size, int font)
        {
            var label = owner.Garden.Text(panel.transform, name, "", position, size, font);
            label.richText = false; return label;
        }
        private void Add(string label, float x, float y, M5Action action) =>
            buttons.Add(action, owner.Button(panel, label, new Vector2(x, y), new Vector2(350, 56), action, 0, 23));
        public void Reset()
        {
            PanelOpen = false; panel.gameObject.SetActive(false); message = "";
            foreach (var drop in drops) if (drop != null) drop.gameObject.SetActive(false);
        }
        public void Dispose()
        {
            foreach (var drop in drops) if (drop != null) UnityEngine.Object.Destroy(drop.gameObject);
            foreach (var material in dropMaterials) if (material != null) UnityEngine.Object.Destroy(material);
        }
        public void RefreshDrops()
        {
            if(View.Director?.PlayingTour==true){foreach(var marker in drops)marker.gameObject.SetActive(false);return;}
            if (Pair == null) return;
            bool inTree = owner.Garden.Navigation?.Page == NavigationPage.Tree && !PanelOpen;
            for (int i = 0; i < drops.Length; i++)
            {
                var session = (i == 0 ? Pair.A : Pair.B).Trees[View.Ensemble.Index];
                var slot = View.nodeViews.FirstOrDefault(n => n.gameObject.activeSelf && n.nodeId == session.State.NodeId);
                drops[i].gameObject.SetActive(inTree && slot != null);
                if (slot == null) continue;
                var position = session == View.Session ? View.drop.position : slot.dropAnchor.position;
                drops[i].position = position + Vector3.right * (i == 0 ? -.13f : .13f);
            }
            View.drop.gameObject.SetActive(false);
        }
        public void Open()
        {
            if (Pair == null)
            {
                var started = View.BeginComparison(View.Model.StructureOnlyPreview);
                if (!started.IsSuccess) { Debug.LogWarning(string.Join("; ", started.Diagnostics.Select(d => d.Message))); return; }
            }
            tree = View.Ensemble.Index; detailPage = 1; message = ""; PanelOpen = true;
            View.NameTooltip?.Hide();
            var head = owner.Garden.Locomotion.Head; var forward = head.forward; forward.y = 0;
            if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
            forward.Normalize(); panel.transform.rotation = Quaternion.LookRotation(forward);
            panel.transform.position = head.position + forward * 1.55f - Vector3.up * .10f;
            View.Refresh();
        }
        public void Close()
        {
            Pair?.CancelEdit(); PanelOpen = false; panel.gameObject.SetActive(false); View.Refresh();
        }
        public void Activate(M5Action action)
        {
            if (action == M5Action.CompareProfiles) { Open(); return; }
            if (Pair == null) return;
            if (!PanelOpen) return;
            var pair = Pair;
            bool editAction = IsEditAction(action);
            if (Editing && !editAction && action != M5Action.CompareNextDetail && action != M5Action.CompareClose) return;
            if (!Editing && editAction) return;
            message = "";
            switch (action)
            {
                case M5Action.CompareClose: Close(); return;
                case M5Action.CompareExit: Close(); View.EndComparison(); return;
                case M5Action.ComparePreviousTree: tree = Math.Max(0, tree - 1); detailPage = 1; break;
                case M5Action.CompareNextTree: tree = Math.Min(pair.Trees.Count - 1, tree + 1); detailPage = 1; break;
                case M5Action.CompareNextDifference:
                    for (int offset = 1; offset <= pair.Trees.Count; offset++)
                    { int index = (tree + offset) % pair.Trees.Count; if (!pair.Trees[index].PathsDiffer) continue; tree = index; break; }
                    detailPage = 1; break;
                case M5Action.CompareNextA: SelectNext(false); break;
                case M5Action.CompareNextB: SelectNext(true); break;
                case M5Action.CompareCopyA: Accept(pair.CopyAToB()); break;
                case M5Action.CompareA: case M5Action.CompareB:
                    Close(); View.ShowComparisonProfile(action == M5Action.CompareB, tree);
                    if (owner.Garden.Navigation.Page != NavigationPage.Tree) owner.Garden.Navigation.OpenTree(tree);
                    return;
                case M5Action.CompareInspect:
                    var divergence = pair.Trees[tree].DivergenceA;
                    if (divergence == null) break;
                    Close(); View.ShowComparisonProfile(pair.ShowingB, tree);
                    if (owner.Garden.Navigation.Page != NavigationPage.Tree) owner.Garden.Navigation.OpenTree(tree);
                    View.FocusedView.Inspect(divergence.NodeId); View.Refresh(); return;
                case M5Action.CompareEdit: pair.BeginEdit(); feature = 0; detailPage = 1; break;
                case M5Action.CompareApply: Accept(pair.ApplyEdit()); break;
                case M5Action.CompareCancel: pair.CancelEdit(); detailPage = 1; break;
                case M5Action.ComparePreviousFeature: feature = (feature + View.Model.Features.Count - 1) % View.Model.Features.Count; detailPage = 1; break;
                case M5Action.CompareNextFeature: feature = (feature + 1) % View.Model.Features.Count; detailPage = 1; break;
                case M5Action.CompareMissing: pair.StageEdit(View.Model.Features[feature].Id, ProfileValue.Missing); break;
                case M5Action.CompareDecrease: EditValue(-1); break;
                case M5Action.CompareIncrease: EditValue(1); break;
                case M5Action.CompareNextDetail: detail.ForceMeshUpdate(); detailPage = detailPage % Math.Max(1, detail.textInfo.pageCount) + 1; break;
            }
            Refresh();
        }
        private void SelectNext(bool b)
        {
            var choices = View.AvailableProfiles.Profiles;
            var current = b ? Pair.B.Profile : Pair.A.Profile;
            int index = choices.ToList().FindIndex(p => p.Id == current.Id);
            if (index < 0) index = choices.ToList().FindIndex(p => View.Model.Features.All(f => p.GetValue(f.Id).Equals(current.GetValue(f.Id))));
            var selected = choices[(index + 1) % choices.Count];
            Accept(b ? Pair.ReplaceB(selected) : Pair.ReplaceA(selected));
        }
        private void Accept(Outcome<ProfileComparisonSession> outcome)
        {
            if (!outcome.IsSuccess) { message = string.Join("; ", outcome.Diagnostics.Select(d => d.Message)); return; }
            View.ShowComparisonProfile(Pair.ShowingB, tree); detailPage = 1;
            message = "Complete model recomputed. Unchanged profile progress is retained.";
        }
        private void EditValue(int direction)
        {
            var f = View.Model.Features[feature]; var old = Pair.Draft.GetValue(f.Id); ProfileValue next;
            if (f.Kind == FeatureKind.Category)
            {
                int index = f.Categories.ToList().IndexOf(old.Category);
                index = (index + direction + f.Categories.Count) % f.Categories.Count;
                next = ProfileValue.FromCategory(f.Categories[index]);
            }
            else
            {
                double step = f.Integer ? 1 : .1;
                double value = old.IsMissing ? f.Minimum ?? 0 : old.Number + direction * step;
                value = Math.Max(f.Minimum ?? double.MinValue, Math.Min(f.Maximum ?? double.MaxValue, value));
                next = ProfileValue.FromNumber(value);
            }
            Pair.StageEdit(f.Id, next);
        }
        private static bool IsEditAction(M5Action a) => a == M5Action.CompareApply || a == M5Action.CompareCancel ||
            a == M5Action.ComparePreviousFeature || a == M5Action.CompareNextFeature || a == M5Action.CompareMissing ||
            a == M5Action.CompareDecrease || a == M5Action.CompareIncrease;

        public void Refresh()
        {
            if (Pair == null) { Reset(); return; }
            RefreshDrops();
            panel.gameObject.SetActive(PanelOpen);
            if (!PanelOpen) return;
            tree = Math.Min(tree, Pair.Trees.Count - 1);
            heading.text = Editing ? "EDIT PROFILE B · DRAFT" : "COMPARE PROFILES · SYNTHETIC MODEL";
            totals.text = "A · " + Pair.A.Profile.DisplayName + "    |    B · " + Pair.B.Profile.DisplayName +
                "\nRaw score: A " + Number(Pair.A.Evaluation.RawScore) + "    B " + Number(Pair.B.Evaluation.RawScore) + "    Δ B − A " + Signed(Pair.RawDelta) +
                "\nProbability: A " + Number(Pair.A.Evaluation.Probability * 100) + "%    B " + Number(Pair.B.Evaluation.Probability * 100) + "%" +
                "\nΔ " + Signed(Pair.ProbabilityPointDelta) + " percentage points · all " + Pair.Trees.Count + " trees evaluated";
            if (Editing)
            {
                var f = View.Model.Features[feature];
                detail.text = "Feature " + (feature + 1) + " / " + View.Model.Features.Count + " · " + f.DisplayName + "\n" + f.Id +
                    "\nA: " + Value(Pair.A.Profile.GetValue(f.Id)) + "\nAccepted B: " + Value(Pair.B.Profile.GetValue(f.Id)) +
                    "\nDraft B: " + Value(Pair.Draft.GetValue(f.Id)) + "\nApply recomputes every tree and resets B's route. A is preserved.";
            }
            else
            {
                var row = Pair.Trees[tree]; var text = new StringBuilder();
                text.AppendLine("Tree " + (tree + 1) + " / " + Pair.Trees.Count + " · " + row.A.TreeId + " · " + (row.PathsDiffer ? "PATHS DIFFER" : "SAME PATH"));
                text.AppendLine("A leaf " + row.A.LeafId + " · " + Signed(row.A.Contribution) + "     B leaf " + row.B.LeafId + " · " + Signed(row.B.Contribution));
                text.AppendLine("Contribution Δ B − A: " + Signed(row.ContributionDelta));
                if (row.PathsDiffer)
                {
                    var node = (SplitNode)View.Model.Trees[tree].Nodes.Single(n => n.Id == row.DivergenceA.NodeId);
                    text.AppendLine("First divergence: " + node.Id + " · " + node.FeatureId);
                    text.AppendLine("A " + Value(row.DivergenceA.ObservedValue) + " → " + (row.DivergenceA.Matched ? "TRUE" : "FALSE") +
                        "    B " + Value(row.DivergenceB.ObservedValue) + " → " + (row.DivergenceB.Matched ? "TRUE" : "FALSE"));
                    text.AppendLine("Condition: " + ExactCondition(node));
                }
                text.AppendLine("A path: " + string.Join(" → ", row.A.VisitedNodeIds));
                text.AppendLine("B path: " + string.Join(" → ", row.B.VisitedNodeIds));
                text.AppendLine("Changed features (" + Pair.ChangedFeatures.Count + "):");
                foreach (string id in Pair.ChangedFeatures) text.AppendLine(id + ": " + Value(Pair.A.Profile.GetValue(id)) + " → " + Value(Pair.B.Profile.GetValue(id)));
                detail.text = text.ToString();
            }
            detail.pageToDisplay = detailPage;
            detail.ForceMeshUpdate();
            notice.text = (message.Length > 0 ? message + "\n" : "") + (Editing ? "Totals show accepted profiles until Apply. " : Pair.Trees.Count(t => t.PathsDiffer) + " trees have different paths. ") +
                "Model sensitivity, not causality.\nDetail page " + detailPage + " / " + Math.Max(1, detail.textInfo.pageCount);
            foreach (var entry in buttons)
            {
                bool visible = IsEditAction(entry.Key) ? Editing : entry.Key == M5Action.CompareNextDetail || !Editing;
                entry.Value.SetActive(visible);
            }
            Enable(M5Action.ComparePreviousTree, tree > 0);
            Enable(M5Action.CompareNextTree, tree + 1 < Pair.Trees.Count);
            Enable(M5Action.CompareNextDifference, Pair.Trees.Any(t => t.PathsDiffer));
            Enable(M5Action.CompareInspect, Pair.Trees[tree].PathsDiffer);
            Enable(M5Action.CompareMissing, View.Model.Features[feature].AllowMissing);
            Enable(M5Action.CompareNextDetail, detail.textInfo.pageCount > 1);
        }
        private void Enable(M5Action action, bool value) => buttons[action].GetComponent<UnityEngine.UI.Button>().interactable = value;
        private static string ExactCondition(SplitNode node) => node.FeatureId + (node.Condition.Operator == DecisionOperator.LessThan ?
            " < " + node.Condition.Threshold.Value.ToString("G17", CultureInfo.InvariantCulture) : node.Condition.Operator == DecisionOperator.In ?
            " in {" + string.Join(", ", node.Condition.Categories) + "}" : " is Missing");
    }
}
