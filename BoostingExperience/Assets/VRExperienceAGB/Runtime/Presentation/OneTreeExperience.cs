using System;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using VRExperienceAGB.Application;
using VRExperienceAGB.Domain;
using VRExperienceAGB.Import;

namespace VRExperienceAGB.Presentation
{
    public sealed class OneTreeExperience : MonoBehaviour
    {
        public TextAsset modelFile;
        public TextAsset profilesFile;
        public TreeNodeView[] nodeViews;
        public Transform drop;
        public Transform presentationRoot;
        public TMP_Text status;
        public TMP_Text explanation;
        public TMP_Text score;
        public TMP_Text feedback;
        public TreeActionButton[] controls;
        public Material idleMaterial;
        public Material visitedMaterial;
        public Material activeMaterial;
        public float moveSeconds = 0.8f;
        public float decisionSeconds = 2.5f;
        public TreeSession Session { get; private set; }
        public EnsembleSession Ensemble { get; private set; }
        private bool editingProfile;
        private bool inspectingNodes;
        private int inspectedNode;
        private string[] inspectionIds = Array.Empty<string>();
        private bool profilePreview;
        private float feedbackUntil;
        public GameObject menuBackdrop;
        public bool MenuOpen { get; private set; }
        private bool pausedBeforeMenu;
        private int featureIndex;
        public bool Ready => Session != null;
        public ModelDefinition Model => model;
        public bool DeepExampleActive => deepExample;
        private FocusedTreeView focusedView;
        private bool deepExample;
        private bool treeMap;
        public TMP_Text routeHistory;
        private ModelDefinition model;
        private ProfileSet profiles;
        private int profileIndex;
        private long movingEvent = -1;
        private float moveElapsed;
        private float dwell;
        private long displayedRevision = -1;
        private bool seated;
        private bool showProfileDetails;
        private string message = "Choose either branch, or follow a synthetic profile.";
        private static string Number(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private void Start() { Initialize(); }
        public bool Initialize()
        {
            Session = null;
            if (modelFile == null || profilesFile == null) return Fail("Bundled synthetic data is missing.");
            var input = NormalizedModelJson.ReadModel(modelFile.text);
            if (!input.IsSuccess) return Fail(string.Join("; ", input.Diagnostics.Select(d => d.Code)));
            model = input.Value;
            var prepared = NormalizedModelJson.ReadProfiles(profilesFile.text, model);
            if (!prepared.IsSuccess) return Fail(string.Join("; ", prepared.Diagnostics.Select(d => d.Code)));
            profiles = prepared.Value;
            if (nodeViews == null || nodeViews.Any(v => v == null || v.platform == null || v.title == null || v.marker == null || v.dropAnchor == null) ||
                nodeViews.Length != 7)
                return Fail("The scene does not match the model's node identities.");
            if (drop == null || status == null || explanation == null || score == null || feedback == null || controls == null ||
                idleMaterial == null || visitedMaterial == null || activeMaterial == null || presentationRoot == null)
                return Fail("The teaching scene is missing a required reference.");
            if (focusedView == null) focusedView = new FocusedTreeView(this);
            deepExample = false; treeMap = false; profileIndex = 0;
            // The fixed teaching layout serializes branch canvases in split order, true then false.
            // Refresh their copy with the fixture, just as node titles are refreshed above.
            var branchLabels = presentationRoot.GetComponentsInChildren<TMP_Text>(true)
                .Where(label => label.name == "Meaning" && label.transform.parent.name == "BranchMeaning").ToArray();
            var splits = model.Trees[0].Nodes.OfType<SplitNode>().ToArray();
            if (branchLabels.Length != splits.Length * 2) return Fail("The scene's branch labels do not match the model.");
            for (var i = 0; i < splits.Length; i++)
            {
                branchLabels[i * 2].text = "TRUE\n" + Condition(splits[i], true);
                branchLabels[i * 2 + 1].text = "FALSE\n" + Condition(splits[i], false);
            }
            return StartEnsemble(null, true);
        }

        private bool Fail(string reason)
        {
            Session = null; Ensemble = null;
            if (status != null) status.text = "Demo unavailable";
            if (explanation != null) explanation.text = "Data could not be loaded. " + reason;
            if (score != null) score.text = "No route score is available.";
            if (controls != null) foreach (var c in controls.Where(c => c != null)) c.GetComponent<UnityEngine.UI.Button>().interactable = false;
            return false;
        }
        private bool StartEnsemble(PreparedProfile profile, bool overview, int treeIndex = 0)
        {
            var outcome = EnsembleSession.Create(model, profile);
            if (!outcome.IsSuccess) return Fail(string.Join("; ", outcome.Diagnostics.Select(d => d.Code)));
            Ensemble = outcome.Value; Ensemble.Select(treeIndex); editingProfile = false; inspectingNodes = false; profilePreview = profile != null; MenuOpen = false;
            AdoptCurrent(overview); return true;
        }
        private void AdoptCurrent(bool overview = false)
        {
            Session = Ensemble.Current; if (MenuOpen) Session.SetPaused(true); treeMap = false; focusedView.Bind(Session.Tree);
            if (overview) Session.ReturnToOverview();
            movingEvent = -1; dwell = 0; displayedRevision = -1; Refresh();
        }
        private string Feature(string id) => model.Features.Single(f => f.Id == id).DisplayName;
        public string Condition(SplitNode node, bool branch)
        {
            var f = Feature(node.FeatureId);
            switch (node.Condition.Operator)
            {
                case DecisionOperator.LessThan: return f + (branch ? " < " : " >= ") + Number(node.Condition.Threshold.Value);
                case DecisionOperator.In: return f + (branch ? " in " : " not in ") + string.Join(", ", node.Condition.Categories);
                default: return f + (branch ? " is missing" : " is present");
            }
        }
        private static string Value(ProfileValue value) => value.Kind == ValueKind.Number ? Number(value.Number) :
            value.Kind == ValueKind.Category ? value.Category : value.Kind == ValueKind.Missing ? "Missing (explicit null)" : "Not supplied";
        private Vector3 Position(string nodeId) => (nodeViews.FirstOrDefault(v => v.gameObject.activeSelf && v.nodeId == nodeId) ?? nodeViews[0]).dropAnchor.position;

        public void Execute(TreeAction action, long revision, string nodeId)
        {
            if (Session == null) return;
            CommandReply reply = default;
            switch (action)
            {
                case TreeAction.Menu:
                    if (!MenuOpen) { pausedBeforeMenu = Session.State.Paused; Session.SetPaused(true); MenuOpen = true; }
                    else { MenuOpen = false; editingProfile = false; inspectingNodes = false; Session.SetPaused(pausedBeforeMenu); }
                    Refresh(); return;
                case TreeAction.InspectNodes:
                    inspectionIds = nodeViews.Where(v => v.gameObject.activeSelf).Select(v => v.nodeId).ToArray();
                    if (Session.State.Overview && !treeMap) inspectionIds = new TreeNeighborhood(Session.Tree).Visible(Session.Tree.RootId).ToArray();
                    inspectedNode = Math.Max(0, Array.IndexOf(inspectionIds, Session.State.NodeId));
                    inspectingNodes = true; Refresh(); return;
                case TreeAction.NextNode: inspectedNode = (inspectedNode + 1) % inspectionIds.Length; Refresh(); return;
                case TreeAction.CloseInspect: inspectingNodes = false; Refresh(); return;
                case TreeAction.PreviousTree:
                case TreeAction.NextTree:
                    if (MenuOpen) { MenuOpen = false; Session.SetPaused(pausedBeforeMenu); }
                    bool keepOverview = Session.State.Overview && !treeMap;
                    if (Ensemble.Select(Ensemble.Index + (action == TreeAction.NextTree ? 1 : -1)))
                    { AdoptCurrent(keepOverview); message = "Tree selected. Its accepted route is preserved."; Refresh(); }
                    return;
                case TreeAction.EditProfile:
                    if (Ensemble.Profile == null) { message = "Choose Follow a profile first."; Refresh(); return; }
                    Session.SetPaused(true); editingProfile = true; featureIndex = 0; Refresh(); return;
                case TreeAction.CloseEdit: editingProfile = false; MenuOpen = false; Session.SetPaused(pausedBeforeMenu); message = "Profile ready. Use Step or Play to inspect the updated routes."; Refresh(); return;
                case TreeAction.NextFeature: featureIndex = (featureIndex + 1) % model.Features.Count; Refresh(); return;
                case TreeAction.DecreaseValue: case TreeAction.IncreaseValue: EditValue(action == TreeAction.IncreaseValue ? 1 : -1); return;
                case TreeAction.RestoreProfile: Ensemble.RestoreOriginal(); AdoptCurrent(); message = "Original profile restored; all routes reset."; Refresh(); return;
                case TreeAction.DeepExample:
                    if (deepExample) { Initialize(); message = "Original three-tree example restored."; }
                    else { model = DeepTreeExample.Model(); profiles = DeepTreeExample.Profiles(model); profileIndex = 0; deepExample = true;
                        StartEnsemble(null, false); message = "Eight decisions, 511 nodes. Hidden branches still belong to the model."; }
                    Refresh(); return;
                case TreeAction.TreeMap:
                    MenuOpen = false; Session.SetPaused(pausedBeforeMenu);
                    treeMap = !treeMap;
                    if (treeMap) Session.ReturnToOverview(); else Session.EnterTree();
                    message = treeMap ? "Root overview. Collapsed branches show hidden-node counts. Return to focus to continue." : "Returned to your saved decision.";
                    Refresh(); return;
                case TreeAction.TrueBranch: profilePreview = false; reply = Session.ChooseBranch(revision, nodeId, true); break;
                case TreeAction.FalseBranch: profilePreview = false; reply = Session.ChooseBranch(revision, nodeId, false); break;
                case TreeAction.Step: profilePreview = false; reply = Session.StepProfile(revision, nodeId); break;
                case TreeAction.Play: profilePreview = false; reply = Session.SetPlaying(true); break;
                case TreeAction.Pause: reply = Session.SetPaused(!Session.State.Paused); break;
                case TreeAction.Back: reply = Session.Back(); break;
                case TreeAction.Restart: reply = Session.Restart(); break;
                case TreeAction.Overview: MenuOpen = false; Session.SetPaused(pausedBeforeMenu); treeMap = false; reply = Session.State.Overview ? Session.EnterTree() : Session.ReturnToOverview(); break;
                case TreeAction.Manual: showProfileDetails = false; StartEnsemble(null, false, Ensemble.Index); message = "Manual exploration. Choices are not a customer prediction."; Refresh(); return;
                case TreeAction.Profile: showProfileDetails = true; StartEnsemble(profiles.Profiles[profileIndex], false, Ensemble.Index); message = "Follow this synthetic profile with Step or Play."; Refresh(); return;
                case TreeAction.NextProfile: showProfileDetails = true; profileIndex = (profileIndex + 1) % profiles.Profiles.Count; message = "Profile selected. Review its values, then choose Follow profile to start."; Refresh(); return;
                case TreeAction.Seated:
                    seated = !seated; presentationRoot.localPosition = new Vector3(0, seated ? -0.4f : 0, 0);
                    message = seated ? "Seated layout: presentation lowered; tracking space unchanged." : "Standing layout restored.";
                    Refresh(); return;
            }
            if (reply.Accepted && (action == TreeAction.TrueBranch || action == TreeAction.FalseBranch)) Ensemble.TakeOver();
            message = reply.Message;
            feedbackUntil = Time.unscaledTime + 4;
            Refresh();
        }

        private void Update() { Advance(Time.unscaledDeltaTime); }
        public void Advance(float seconds)
        {
            if (Session == null || !float.IsFinite(seconds) || seconds < 0) return;
            var state = Session.State;
            if (state.PendingDecision == null)
            {
                movingEvent = -1; drop.position = Position(state.NodeId);
                if (state.Playing && state.CanAdvance)
                {
                    dwell += seconds;
                    if (dwell >= Mathf.Max(0.1f, decisionSeconds))
                    { dwell = 0; Session.StepProfile(state.Revision, state.NodeId); }
                }
            }
            else
            {
                dwell = 0;
                if (movingEvent != state.PendingDecision.EventId) { movingEvent = state.PendingDecision.EventId; moveElapsed = 0; }
                if (!state.Paused && !state.Overview) moveElapsed += seconds;
                var t = Mathf.Clamp01(moveElapsed / Mathf.Max(0.05f, moveSeconds));
                drop.position = Vector3.Lerp(Position(state.NodeId), Position(state.PendingDecision.ChildId), Mathf.SmoothStep(0, 1, t));
                if (t >= 1 && !state.Paused && !state.Overview) {
                    var reply = Session.CompleteMove(movingEvent); movingEvent = -1;
                    message = Session.State.AtLeaf ? (Ensemble.Complete ? "All tree leaves reached. Review the complete result." : "Leaf recorded. Choose Next tree, or inspect another tree in Forest overview.") : reply.Message;
                }
            }
            if (Session.State.Revision != displayedRevision || (feedback.gameObject.activeSelf && !MenuOpen && !profilePreview && Time.unscaledTime >= feedbackUntil)) Refresh();
        }

        public void Refresh()
        {
            if (Session == null) return;
            var s = Session.State; displayedRevision = s.Revision;
            status.text = (s.Overview ? "FOREST ENTRY  |  " : "TREE " + (Ensemble.Index + 1) + " / " + model.Trees.Count + "  |  ") + (s.Mode == ExperienceMode.Manual ? "EXPLORE BRANCHES" : "FOLLOW A PROFILE") +
                (s.Paused ? "  |  PAUSED" : s.PendingDecision != null ? "  |  MOVING" : s.Playing ? "  |  PLAYING" : "");
            var chosen = profiles.Profiles[profileIndex];
            if (s.Overview)
                explanation.text = treeMap ? "Root overview - your route is saved" : "Forest overview\nTree " + (Ensemble.Index + 1) + " selected - enter to continue";
            else if (Session.CurrentNode is SplitNode split)
            {
                var decision = Session.CurrentProfileDecision;
                explanation.text = Condition(split, true) + "?\n" + (decision == null ? "Choose a branch below" :
                    Session.Profile.DisplayName + ": " + Value(decision.ObservedValue) + " -> " + (decision.Matched ? "TRUE" : "FALSE"));
            }
            else explanation.text = "Leaf reached\nThis tree " + (s.Contribution >= 0 ? "adds " : "subtracts ") + Number(Math.Abs(s.Contribution)) +
                " to the raw score.";
            score.text = s.ScoreMeaning + "  " + Number(s.RouteTotal) + "\nBaseline " + Number(s.Baseline) + "  +  this leaf " + Number(s.Contribution) +
                "\nSynthetic example | " + (Ensemble.Index + 1) + " of " + model.Trees.Count + " trees | Not the full prediction";
            if (Ensemble.Trees.Skip(1).Any(t => t.State.AtLeaf) || Ensemble.Index > 0)
                score.text = (Ensemble.Mode == ExperienceMode.Manual ? "Manual route score  " : "Visited-tree subtotal  ") + Number(Ensemble.RouteTotal) +
                    "\n" + Ensemble.CompletedCount + " / " + model.Trees.Count + " leaves reached | Baseline " + Number(model.BaseScore) +
                    "\n" + string.Join("  +  ", Ensemble.Trees.Select(t => t.State.AtLeaf ? Number(t.State.Contribution) : "pending"));
            if (Ensemble.Complete && Ensemble.Mode == ExperienceMode.PreparedProfile)
                score.text = "Full synthetic prediction: " + (Ensemble.Evaluation.Probability * 100).ToString("0.00", CultureInfo.InvariantCulture) + "% " + model.OutcomeLabel +
                    "\nRaw score " + Number(Ensemble.Evaluation.RawScore) + " = baseline " + Number(model.BaseScore) + " + " + string.Join(" + ", Ensemble.Evaluation.Trees.Select(t => "(" + Number(t.Contribution) + ")"));
            if (Ensemble.Mode == ExperienceMode.Manual && Ensemble.CompletedCount > 1)
                score.text += "\nFree exploration; route consistency not verified. No profile probability.";
            feedback.text = showProfileDetails && MenuOpen ? "Selected for next run: " + chosen.DisplayName + "\n" +
                string.Join("  |  ", chosen.Values.Select(p => Feature(p.Key) + ": " + Value(p.Value))) + "\n" + message :
                s.Overview ? "Choose Explore branches or Follow a profile to begin" : message;
            if (editingProfile)
            {
                var feature = model.Features[featureIndex];
                explanation.text = "Try a hypothetical change\n" + feature.DisplayName + ": " + Value(Ensemble.Profile.GetValue(feature.Id));
                feedback.text = "Original: " + Value(Ensemble.OriginalProfile.GetValue(feature.Id)) + " | Edits reset every tree route and recompute the full model.\n" + message;
                if (Ensemble.Evaluation != null) score.text = "Updated full synthetic prediction: " + (Ensemble.Evaluation.Probability * 100).ToString("0.00", CultureInfo.InvariantCulture) + "%\nRaw score " + Number(Ensemble.Evaluation.RawScore) + " | All " + model.Trees.Count + " trees evaluated";
            }
            if (profilePreview && !MenuOpen && Ensemble.Profile != null)
                feedback.text = "Synthetic profile: " + Ensemble.Profile.DisplayName + "\n" +
                    string.Join("  |  ", Ensemble.Profile.Values.Select(p => Feature(p.Key) + ": " + Value(p.Value))) + "\nStep or Play to begin";
            if (s.AtLeaf && !editingProfile)
                explanation.text = "Leaf contribution " + (s.Contribution >= 0 ? "+" : "") + Number(s.Contribution) +
                    "\nPrevious total " + Number(Ensemble.RouteTotal - s.Contribution) + "  ->  New total " + Number(Ensemble.RouteTotal);
            if (inspectingNodes)
            {
                var inspected = Session.Tree.Nodes.Single(n => n.Id == inspectionIds[inspectedNode]);
                explanation.text = "Inspect node " + (inspectedNode + 1) + " / " + inspectionIds.Length + "  |  " + inspected.Id + "\n" +
                    (inspected is SplitNode sn ? "TRUE: " + Condition(sn, true) + "\nFALSE: " + Condition(sn, false) :
                    "Leaf contribution: " + Number(Session.Tree.Weight * ((LeafNode)inspected).Score));
                feedback.text = "Read-only inspection. Your route and score are preserved.";
            }
            feedback.gameObject.SetActive(MenuOpen || profilePreview || Time.unscaledTime < feedbackUntil);
            if (menuBackdrop != null) menuBackdrop.SetActive(MenuOpen);
            if (s.Overview && !treeMap) focusedView.Forest(Ensemble);
            else focusedView.Refresh(s, treeMap || Session.Tree.Nodes.Count <= 7);
            if (inspectingNodes && !(s.Overview && !treeMap))
                foreach (var node in nodeViews.Where(v => v.gameObject.activeSelf && v.nodeId == inspectionIds[inspectedNode]))
                { node.platform.sharedMaterial = activeMaterial; node.marker.enabled = true; node.marker.text = "INSPECTING"; }
            if (routeHistory != null) routeHistory.text = "Depth " + s.Decisions.Count + "  |  " + Session.Tree.Nodes.Count + " nodes  |  " +
                (s.Decisions.Count == 0 ? "At root" : string.Join(" > ", s.Decisions.Select(d => d.Matched ? "TRUE" : "FALSE"))) +
                (treeMap ? "\nRoot overview - return to focus to continue" : "\nBack retraces your accepted decisions");
            if (routeHistory != null) routeHistory.gameObject.SetActive(MenuOpen && !editingProfile && !inspectingNodes);
            explanation.rectTransform.anchoredPosition = new Vector2(0, inspectingNodes ? 760 : 420);
            explanation.rectTransform.sizeDelta = new Vector2(1200, inspectingNodes ? 210 : 100);
            explanation.gameObject.SetActive(!MenuOpen || editingProfile || inspectingNodes);
            drop.gameObject.SetActive(!(s.Overview && !treeMap) && (!treeMap || nodeViews.Any(v => v.gameObject.activeSelf && v.nodeId == s.NodeId)));
            if (s.PendingDecision == null) drop.position = Position(s.NodeId);
            foreach (var c in controls)
            {
                var enabled = true; var label = c.label;
                switch (c.action)
                {
                    case TreeAction.Menu: label.text = MenuOpen ? "Close menu" : "Menu"; break;
                    case TreeAction.PreviousTree: enabled = Ensemble.Index > 0; break;
                    case TreeAction.NextTree: enabled = Ensemble.Index + 1 < model.Trees.Count; break;
                    case TreeAction.EditProfile: enabled = Ensemble.Profile != null; break;
                    case TreeAction.DeepExample: label.text = deepExample ? "Original example" : "Deep tree: 8 levels"; break;
                    case TreeAction.TreeMap: label.text = treeMap ? "Return to focus" : "Tree overview"; break;
                    case TreeAction.TrueBranch: case TreeAction.FalseBranch:
                        enabled = s.CanAdvance;
                        label.text = Session.CurrentNode is SplitNode n ?
                            (c.action == TreeAction.TrueBranch ? "TRUE\n" : "FALSE\n") +
                            (n.Condition.Operator == DecisionOperator.LessThan ?
                                (c.action == TreeAction.TrueBranch ? "Fewer than " : "At least ") + Number(n.Condition.Threshold.Value) :
                                Condition(n, c.action == TreeAction.TrueBranch)) : "Leaf reached";
                        break;
                    case TreeAction.Step: enabled = s.CanAdvance && s.Mode == ExperienceMode.PreparedProfile; break;
                    case TreeAction.Play: enabled = !s.Overview && !s.AtLeaf && s.Mode == ExperienceMode.PreparedProfile; break;
                    case TreeAction.Pause: label.text = s.Paused ? "Resume" : "Pause"; break;
                    case TreeAction.Overview: label.text = s.Overview ? "Enter tree" : "Forest overview"; break;
                    case TreeAction.Restart: label.text = s.Mode == ExperienceMode.PreparedProfile ? "Replay" : "Restart"; break;
                    case TreeAction.Seated: label.text = seated ? "Standing layout" : "Seated layout"; break;
                }
                if (c.action == TreeAction.Manual || c.action == TreeAction.Profile)
                {
                    var selected = c.action == TreeAction.Manual ? s.Mode == ExperienceMode.Manual : s.Mode == ExperienceMode.PreparedProfile;
                    c.GetComponent<UnityEngine.UI.Image>().color = selected ? new Color(.06f, .28f, .35f, .98f) : new Color(.045f, .12f, .19f, .98f);
                }
                bool editorControl = c.action == TreeAction.NextFeature || c.action == TreeAction.DecreaseValue || c.action == TreeAction.IncreaseValue || c.action == TreeAction.RestoreProfile || c.action == TreeAction.CloseEdit;
                bool inspectorControl = c.action == TreeAction.NextNode || c.action == TreeAction.CloseInspect;
                bool secondary = IsSecondary(c.action);
                bool visible = inspectorControl ? MenuOpen && inspectingNodes : editorControl ? MenuOpen && editingProfile : secondary ? MenuOpen && !editingProfile && !inspectingNodes : true;
                if (c.action == TreeAction.Step || c.action == TreeAction.Play || c.action == TreeAction.Pause)
                    visible = c.action == TreeAction.Pause ? !MenuOpen : !MenuOpen && s.Mode == ExperienceMode.PreparedProfile;
                if (c.action == TreeAction.TrueBranch || c.action == TreeAction.FalseBranch)
                    visible = !MenuOpen && !s.Overview && !s.AtLeaf;
                if (MenuOpen && !secondary && !editorControl && !inspectorControl && c.action != TreeAction.Menu) enabled = false;
                c.gameObject.SetActive(visible);
                c.GetComponent<UnityEngine.UI.Button>().interactable = enabled;

            }
        }
        public static bool IsSecondary(TreeAction action) => action == TreeAction.DeepExample || action == TreeAction.TreeMap ||
            action == TreeAction.PreviousTree || action == TreeAction.NextTree || action == TreeAction.EditProfile ||
            action == TreeAction.NextProfile || action == TreeAction.Seated || action == TreeAction.Overview || action == TreeAction.InspectNodes;

        private void EditValue(int direction)
        {
            var feature = model.Features[featureIndex]; var old = Ensemble.Profile.GetValue(feature.Id);
            ProfileValue next;
            if (feature.Kind == FeatureKind.Category)
            {
                var values = feature.Categories.Select(ProfileValue.FromCategory).ToList();
                if (feature.AllowMissing) values.Add(ProfileValue.Missing);
                int index = values.FindIndex(v => v.Kind == old.Kind && v.Category == old.Category);
                next = values[(index + direction + values.Count) % values.Count];
            }
            else
            {
                double step = feature.Integer ? 1 : .1;
                double number = old.IsMissing ? feature.Minimum ?? 0 : old.Number + direction * step;
                if (feature.AllowMissing && !old.IsMissing && direction < 0 && number < (feature.Minimum ?? 0)) next = ProfileValue.Missing;
                else next = ProfileValue.FromNumber(Math.Max(feature.Minimum ?? double.MinValue, Math.Min(feature.Maximum ?? double.MaxValue, number)));
            }
            var result = Ensemble.Edit(feature.Id, next);
            if (!result.IsSuccess) message = "Change rejected: " + string.Join("; ", result.Diagnostics.Select(d => d.Code));
            else { AdoptCurrent(); message = "Updated " + feature.DisplayName + ". All tree routes were reset."; }
            Refresh();
        }
        private void OnApplicationPause(bool paused) { if (paused && Session != null) { Session.SetPaused(true); Refresh(); } }
        private void OnApplicationFocus(bool focus) { if (!focus && Session != null) { Session.SetPaused(true); Refresh(); } }
    }
}
