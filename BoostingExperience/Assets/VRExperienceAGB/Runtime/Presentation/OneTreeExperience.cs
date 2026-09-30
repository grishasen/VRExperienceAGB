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
        public TMP_Text ledgerText;
        private bool reviewingResult;
        private int ledgerPage;
        private bool freeExplorationAcknowledged;
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
        public AgbPreviewResult ExportPreview { get; private set; }
        private string exportName;
        public FullNameTooltip NameTooltip { get; private set; }
        public ForestGardenView Garden { get; private set; }
        public bool DeepExampleActive => deepExample;
        private FocusedTreeView focusedView;
        public FocusedTreeView FocusedView => focusedView;
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
        public bool IsSeated => seated;
        public void RefreshNavigationTree() => Refresh();
        private bool showProfileDetails;
        private string message = "Choose either branch, or follow a synthetic profile.";
        private static string Number(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        public Outcome<AgbPreviewResult> LoadAgbStructure(string json, string displayName)
        {
            var imported = AgbStructurePreview.Read(json, displayName);
            if (!imported.IsSuccess) return imported;
            if (!Ready && !Initialize()) return Outcome<AgbPreviewResult>.Failure(new[] { new Diagnostic("SceneUnavailable", "The teaching scene is unavailable.") });
            model = imported.Value.Model; ExportPreview = imported.Value; exportName = displayName;
            profiles = new ProfileSet(1, model.Id, Array.Empty<PreparedProfile>());
            profileIndex = 0; deepExample = false; treeMap = false; showProfileDetails = false;
            message = "Export structure loaded. Explore branches; profile scoring is unverified.";
            StartEnsemble(null, true); Refresh(); return imported;
        }

        private void Start() { Initialize(); }
        public bool Initialize()
        {
            Session = null; ExportPreview = null;
            if (modelFile == null) return Fail("The model file is missing.");
            var input = NormalizedModelJson.ReadModel(modelFile.text);
            if (input.IsSuccess)
            {
                model = input.Value;
                if (profilesFile == null) return Fail("Bundled synthetic profiles are missing.");
                var prepared = NormalizedModelJson.ReadProfiles(profilesFile.text, model);
                if (!prepared.IsSuccess) return Fail(string.Join("; ", prepared.Diagnostics.Select(d => d.Code)));
                profiles = prepared.Value;
            }
            else
            {
                var imported = AgbStructurePreview.Read(modelFile.text, modelFile.name);
                if (!imported.IsSuccess) return Fail(string.Join("; ", imported.Diagnostics.Select(d => d.Code + ": " + d.Message)));
                ExportPreview = imported.Value; exportName = modelFile.name; model = imported.Value.Model;
                profiles = new ProfileSet(1, model.Id, Array.Empty<PreparedProfile>());
                showProfileDetails = false;
                message = "Export structure loaded. Explore branches; profile scoring is unverified.";
            }
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
            if (!model.StructureOnlyPreview && branchLabels.Length != splits.Length * 2) return Fail("The scene's branch labels do not match the model.");
            for (var i = 0; !model.StructureOnlyPreview && i < splits.Length; i++)
            {
                branchLabels[i * 2].text = "TRUE\n" + Condition(splits[i], true);
                branchLabels[i * 2 + 1].text = "FALSE\n" + Condition(splits[i], false);
            }
            PrepareNameHover(); Garden = GetComponent<ForestGardenView>(); Garden?.Configure(this); return StartEnsemble(null, true);
        }

        public string FullNodeName(string nodeId)
        {
            var node = Session.Tree.Nodes.Single(n => n.Id == nodeId);
            if (!(node is SplitNode split)) return Session.Tree.Id + "/" + nodeId + "\nLeaf score " + ((LeafNode)node).Score.ToString("G17", CultureInfo.InvariantCulture);
            if (ExportPreview != null && ExportPreview.Metadata.TryGetValue(Session.Tree.Id + "/" + nodeId, out var audit))
                return split.FeatureId + "\n" + audit.SplitText + "\nSource: " + audit.SourceAddress;
            return split.FeatureId + "\n" + Condition(split, true);
        }
        private void PrepareNameHover()
        {
            if (NameTooltip == null)
            {
                NameTooltip = gameObject.AddComponent<FullNameTooltip>();
                NameTooltip.Build(explanation.transform.parent, explanation);
            }
            foreach (var node in nodeViews)
            {
                var canvas = node.title.GetComponentInParent<Canvas>(true);
                if (canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null) canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                node.title.raycastTarget = true;
                var hover = node.title.GetComponent<NodeNameHover>() ?? node.title.gameObject.AddComponent<NodeNameHover>();
                hover.experience = this; hover.node = node;
                // Reuse the already configured Meta canvas interaction prefab with its plane and ray surface.
                if (canvas.GetComponentInChildren<Oculus.Interaction.PointableCanvas>(true) == null)
                {
                    var template = explanation.GetComponentInParent<Canvas>(true).GetComponentInChildren<Oculus.Interaction.PointableCanvas>(true);
                    if (template != null)
                    {
                        var holder = new GameObject("NamePointerSurface", typeof(RectTransform));
                        holder.SetActive(false); holder.transform.SetParent(canvas.transform,false);
                        var bounds = (RectTransform)holder.transform; bounds.anchorMin=Vector2.zero; bounds.anchorMax=Vector2.one; bounds.sizeDelta=Vector2.zero;
                        var interaction = Instantiate(template.gameObject, holder.transform);
                        interaction.GetComponent<Oculus.Interaction.PointableCanvas>().InjectCanvas(canvas);
                        holder.SetActive(true);
                    }
                }
            }
            explanation.raycastTarget = true;
            var current = explanation.GetComponent<NodeNameHover>() ?? explanation.gameObject.AddComponent<NodeNameHover>();
            current.experience = this; current.node = null;
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
            NameTooltip?.Hide();
            var outcome = EnsembleSession.Create(model, profile);
            if (!outcome.IsSuccess) return Fail(string.Join("; ", outcome.Diagnostics.Select(d => d.Code)));
            if (Ensemble != null) foreach (var retired in Ensemble.Trees) retired.ReturnToOverview();
            Ensemble = outcome.Value; reviewingResult = false; freeExplorationAcknowledged = false; Ensemble.Select(treeIndex); editingProfile = false; inspectingNodes = false; profilePreview = profile != null; MenuOpen = false;
            AdoptCurrent(overview); return true;
        }
        private void AdoptCurrent(bool overview = false)
        {
            Session = Ensemble.Current; if (MenuOpen) Session.SetPaused(true); treeMap = false; focusedView.Bind(Session.Tree);
            if (overview) Session.ReturnToOverview();
            movingEvent = -1; dwell = 0; displayedRevision = -1; Refresh();
        }
        public void SelectGardenTree(int index)
        {
            if (Garden == null || !Garden.Visible || index < 0 || index >= model.Trees.Count) return;
            NameTooltip?.Hide();
            if (Ensemble.Select(index)) AdoptCurrent(true);
        }

        public void ShowNavigationOverview()
        {
            NameTooltip?.Hide(); MenuOpen=false; reviewingResult=false; editingProfile=false; inspectingNodes=false; treeMap=false;
            Session.ReturnToOverview(); Refresh();
        }
        public void EnterNavigationTree()
        {
            MenuOpen=false; treeMap=false; Session.EnterTree(); Refresh();
        }

        private string Feature(string id) => model.Features.Single(f => f.Id == id).DisplayName;
        public string Condition(SplitNode node, bool branch)
        {
            var f = Feature(node.FeatureId);
            switch (node.Condition.Operator)
            {
                case DecisionOperator.LessThan: return f + (branch ? " < " : " >= ") + Threshold(node.Condition.Threshold.Value);
                case DecisionOperator.In: return f + (branch ? " in " : " not in ") + string.Join(", ", node.Condition.Categories.Take(3)) + (node.Condition.Categories.Count > 3 ? " (+" + (node.Condition.Categories.Count - 3) + " categories)" : "");
                default: return f + (branch ? " is missing" : " is present");
            }
        }
        private static string Threshold(double value) => value.ToString("G15", CultureInfo.InvariantCulture);
        private static string Value(ProfileValue value) => value.Kind == ValueKind.Number ? Number(value.Number) :
            value.Kind == ValueKind.Category ? value.Category : value.Kind == ValueKind.Missing ? "Missing (explicit null)" : "Not supplied";
        private Vector3 Position(string nodeId) => (nodeViews.FirstOrDefault(v => v.gameObject.activeSelf && v.nodeId == nodeId) ?? nodeViews[0]).dropAnchor.position;

        public void Execute(TreeAction action, long revision, string nodeId)
        {
            if (Session == null || revision != Session.State.Revision || nodeId != Session.State.NodeId) return;
            if (Garden?.simplifiedNavigation == true && focusedView?.FocusRoot != null &&
                (action == TreeAction.TrueBranch || action == TreeAction.FalseBranch || action == TreeAction.Step || action == TreeAction.Play)) return;
            NameTooltip?.Hide();
            if (model.StructureOnlyPreview && (action == TreeAction.Profile || action == TreeAction.NextProfile || action == TreeAction.DeepExample || action == TreeAction.LargerEnsemble)) return;
            if(Garden?.simplifiedNavigation == true && Garden.Navigation != null)
            {
                if(action==TreeAction.Menu) { Garden.Navigation.ToggleTreeMenu(); return; }
                if(action==TreeAction.Overview && !Session.State.Overview) { Garden.Navigation.ReturnFromTree(); return; }
                if(action==TreeAction.Restart) { movingEvent=-1; Garden.Navigation.RestartTree(); return; }
                if(action==TreeAction.Back) { Session.Back(); movingEvent=-1; Refresh(); return; }
            }
            CommandReply reply = default;
            switch (action)
            {
                case TreeAction.Menu:
                    if (!MenuOpen) { pausedBeforeMenu = Session.State.Paused; Session.SetPaused(true); MenuOpen = true; }
                    else { Ensemble.CancelEdit(); MenuOpen = false; reviewingResult = false; editingProfile = false; inspectingNodes = false; Session.SetPaused(pausedBeforeMenu); }
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
                    if (!MenuOpen) { pausedBeforeMenu = Session.State.Paused; MenuOpen = true; }
                    Session.SetPaused(true); Ensemble.BeginEdit(); editingProfile = true; featureIndex = 0; Refresh(); return;
                case TreeAction.CloseEdit:
                    var applied = Ensemble.ApplyEdit();
                    if (!applied.IsSuccess) { message = string.Join("; ", applied.Diagnostics.Select(d => d.Message)); Refresh(); return; }
                    editingProfile = false; MenuOpen = false; AdoptCurrent(); Session.SetPaused(pausedBeforeMenu);
                    profilePreview = true; message = "Hypothetical profile applied. All trees recomputed; restart at Tree 1 root. Model sensitivity is not a causal promise."; Refresh(); return;
                case TreeAction.CancelEdit:
                    Ensemble.CancelEdit(); editingProfile = false; MenuOpen = false; Session.SetPaused(pausedBeforeMenu);
                    message = "Edit cancelled. Accepted profile, route and result preserved."; Refresh(); return;
                case TreeAction.SetMissing: Ensemble.StageEdit(model.Features[featureIndex].Id, ProfileValue.Missing); Refresh(); return;
                case TreeAction.TourDetail:
                    Ensemble.SetDetail(Ensemble.Detail == TourDetail.Detailed ? Application.TourDetail.Short : Application.TourDetail.Detailed);
                    message = "Tour detail changed. Full evaluation is unchanged; grouped trees can be inspected individually."; Refresh(); return;
                case TreeAction.GroupRemaining:
                    if (MenuOpen) { MenuOpen = false; Session.SetPaused(pausedBeforeMenu); }
                    if (Ensemble.ExplainRemainingGroup()) { message = "Remaining contributions explained as a group. Review the ledger for every tree."; pausedBeforeMenu = Session.State.Paused; Session.SetPaused(true); MenuOpen = true; reviewingResult = true; ledgerPage = 0; }
                    Refresh(); return;
                case TreeAction.Result:
                    if (!MenuOpen) { pausedBeforeMenu = Session.State.Paused; Session.SetPaused(true); MenuOpen = true; }
                    reviewingResult = true; ledgerPage = 0; Refresh(); return;
                case TreeAction.NextLedgerPage: ledgerPage = (ledgerPage + 1) % Math.Max(1, (model.Trees.Count + 3) / 4); Refresh(); return;
                case TreeAction.CloseResult: reviewingResult = false; MenuOpen = false; Session.SetPaused(pausedBeforeMenu); Refresh(); return;
                case TreeAction.ReviseChoice:
                    MenuOpen = false; reviewingResult = false;
                    if (Ensemble.ReviseConflict()) { AdoptCurrent(); Session.SetPaused(pausedBeforeMenu); freeExplorationAcknowledged = false; message = "Earlier conflicting decision restored. Later contributions cleared."; }
                    Refresh(); return;
                case TreeAction.ContinueFree:
                    freeExplorationAcknowledged = true; MenuOpen = false; Session.SetPaused(pausedBeforeMenu);
                    message = "Continue free exploration. The total remains a Route score without a profile probability."; Refresh(); return;
                case TreeAction.NextFeature: featureIndex = (featureIndex + 1) % model.Features.Count; Refresh(); return;
                case TreeAction.DecreaseValue: case TreeAction.IncreaseValue: EditValue(action == TreeAction.IncreaseValue ? 1 : -1); return;
                case TreeAction.RestoreProfile: Ensemble.ResetDraftToOriginal(); message = "Original values restored in the draft. Apply to accept them."; Refresh(); return;
                case TreeAction.LargerEnsemble:
                    if (model.Id == "synthetic-ensemble-24") Initialize();
                    else
                    {
                        var source = NormalizedModelJson.ReadModel(modelFile.text).Value;
                        var sourceProfiles = NormalizedModelJson.ReadProfiles(profilesFile.text, source).Value;
                        model = EnsembleTeachingExample.Model(source); profiles = EnsembleTeachingExample.Profiles(model, sourceProfiles);
                        profileIndex = 0; deepExample = false; StartEnsemble(null, false);
                    }
                    message = "Synthetic 24-tree example repeats predictors. Every tree contributes; try different choices to explore contradictions."; Refresh(); return;
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
                case TreeAction.Back: reply = Ensemble.Back(); AdoptCurrent(); freeExplorationAcknowledged = false; break;
                case TreeAction.Restart: Ensemble.RestartTour(); AdoptCurrent(); freeExplorationAcknowledged = false; message = "Entire tour restarted at the baseline."; Refresh(); return;
                case TreeAction.Overview: MenuOpen = false; Session.SetPaused(pausedBeforeMenu); treeMap = false; reply = Session.State.Overview ? Session.EnterTree() : Session.ReturnToOverview(); break;
                case TreeAction.Manual: showProfileDetails = false; StartEnsemble(null, false, Ensemble.Index); message = "Manual exploration. Choices are not a customer prediction."; Refresh(); return;
                case TreeAction.Profile: showProfileDetails = true; StartEnsemble(profiles.Profiles[profileIndex], false, Ensemble.Index); message = "Follow this synthetic profile with Step or Play."; Refresh(); return;
                case TreeAction.NextProfile: showProfileDetails = true; profileIndex = (profileIndex + 1) % profiles.Profiles.Count; message = "Profile selected. Review its values, then choose Follow profile to start."; Refresh(); return;
                case TreeAction.Seated:
                    seated = !seated; presentationRoot.localPosition = new Vector3(0, seated ? -0.4f : 0, 0);
                    message = seated ? "Seated layout: presentation lowered; tracking space unchanged." : "Standing layout restored.";
                    Refresh(); return;
            }
            if (reply.Accepted && (action == TreeAction.TrueBranch || action == TreeAction.FalseBranch)) { Ensemble.TakeOver(); Ensemble.InvalidateAfterCurrent(); }
            message = reply.Message;
            feedbackUntil = Time.unscaledTime + 4;
            Refresh();
        }

        private void Update() { Advance(Time.unscaledDeltaTime); }
        public void Advance(float seconds)
        {
            if (Garden?.simplifiedNavigation == true && focusedView?.FocusRoot != null) return;
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
            var chosen = profiles.Profiles.Count > 0 ? profiles.Profiles[profileIndex] : null;
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
                    "\n" + (model.Trees.Count <= 3 ? string.Join("  +  ", Ensemble.Trees.Select(t => t.State.AtLeaf ? Number(t.State.Contribution) : "pending")) : "Current leaf " + Number(s.Contribution) + " | " + (model.Trees.Count - Ensemble.CompletedCount) + " pending trees | See contribution ledger");
            if (Ensemble.Complete && Ensemble.Mode == ExperienceMode.PreparedProfile)
                score.text = "Full synthetic prediction: " + Percent(Ensemble.Evaluation.Probability) + "% " + model.OutcomeLabel +
                    "\nRaw score " + Number(Ensemble.Evaluation.RawScore) + " = baseline " + Number(model.BaseScore) + " + " + (model.Trees.Count <= 3 ? string.Join(" + ", Ensemble.Evaluation.Trees.Select(t => "(" + Number(t.Contribution) + ")")) : "all " + model.Trees.Count + " tree contributions (see ledger)");
            if (Ensemble.Mode == ExperienceMode.Manual)
                score.text += "\nRoute score. No profile probability. " + (Ensemble.Consistency.Status == RouteConsistency.Contradictory ? "Conflicting choices" :
                    Ensemble.Consistency.Status == RouteConsistency.NotVerified ? "Consistency not verified" : "Partial route; consistent choices");
            if (model.StructureOnlyPreview)
                score.text = exportName + " | Export structure\n" + model.Trees.Count + " trees | " + model.Trees.Sum(t => t.Nodes.Count) + " nodes | " + model.Features.Count + " predictors\n" +
                    "Manual route score " + Number(Ensemble.RouteTotal) + " | " + Ensemble.CompletedCount + " / " + model.Trees.Count + " leaves\nConsistency not verified. No profile probability.";
            if (Ensemble.Mode == ExperienceMode.PreparedProfile)
                score.text += "\n" + Ensemble.Profile.DisplayName + " | " + Ensemble.Detail + " tour";
            feedback.text = showProfileDetails && MenuOpen && chosen != null ? "Selected for next run: " + chosen.DisplayName + "\n" +
                string.Join("  |  ", chosen.Values.Select(p => Feature(p.Key) + ": " + Value(p.Value))) + "\n" + message :
                s.Overview ? (model.StructureOnlyPreview ? "Explore branches to inspect the export" : "Choose Explore branches or Follow a profile to begin") : message;
            if (editingProfile)
            {
                var feature = model.Features[featureIndex];
                explanation.text = "Try a hypothetical change\n" + feature.DisplayName + ": " + Value(Ensemble.Draft.GetValue(feature.Id));
                feedback.text = "Original: " + Value(Ensemble.OriginalProfile.GetValue(feature.Id)) + " | Apply restarts at Tree 1; Cancel preserves the accepted result.\n" + message;
                if (Ensemble.Evaluation != null) score.text = "Accepted full synthetic prediction: " + Percent(Ensemble.Evaluation.Probability) + "%\nRaw score " + Number(Ensemble.Evaluation.RawScore) + " | All " + model.Trees.Count + " trees evaluated";
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
                if (ExportPreview != null && ExportPreview.Metadata.TryGetValue(Session.Tree.Id + "/" + inspected.Id, out var audit))
                    feedback.text += "\nSource " + audit.SourceAddress + " | Node estimate " + Number(audit.Score) + " | Gain " + Number(audit.Gain) + " | Samples " + audit.SampleCount;
            }
            var consistency = Ensemble.Consistency;
            if (Ensemble.Mode == ExperienceMode.Manual && consistency.Status == RouteConsistency.Contradictory && !editingProfile && !inspectingNodes && !reviewingResult)
                feedback.text = "Conflicting choices: " + string.Join("; ", consistency.Conflicts.Take(2).Select(ConstraintCopy)) + "\nRevise earlier choice or Continue free exploration (Menu).";
            feedback.gameObject.SetActive(!reviewingResult && (MenuOpen || profilePreview || (Ensemble.Mode == ExperienceMode.Manual && consistency.Status == RouteConsistency.Contradictory && !freeExplorationAcknowledged) || Time.unscaledTime < feedbackUntil));
            if (ledgerText != null)
            {
                ledgerText.gameObject.SetActive(reviewingResult);
                ledgerText.text = LedgerCopy();
            }
            if (menuBackdrop != null) menuBackdrop.SetActive(MenuOpen || reviewingResult);
            if (s.Overview && !treeMap) { if (Garden == null) focusedView.Forest(Ensemble); }
            else focusedView.Refresh(s, treeMap || Session.Tree.Nodes.Count <= 7);
            if (inspectingNodes && !(s.Overview && !treeMap))
                foreach (var node in nodeViews.Where(v => v.gameObject.activeSelf && v.nodeId == inspectionIds[inspectedNode]))
                { node.platform.sharedMaterial = activeMaterial; node.marker.enabled = true; node.marker.text = "INSPECTING"; }
            if (routeHistory != null) routeHistory.text = "Depth " + s.Decisions.Count + "  |  " + Session.Tree.Nodes.Count + " nodes  |  " +
                (s.Decisions.Count == 0 ? "At root" : string.Join(" > ", s.Decisions.Select(d => d.Matched ? "TRUE" : "FALSE"))) +
                (treeMap ? "\nRoot overview - return to focus to continue" : "\nBack retraces your accepted decisions");
            if (routeHistory != null) routeHistory.gameObject.SetActive(MenuOpen && !reviewingResult && !editingProfile && !inspectingNodes);
            explanation.rectTransform.anchoredPosition = new Vector2(0, inspectingNodes ? 760 : s.Overview && !treeMap ? 650 : 420);
            explanation.rectTransform.sizeDelta = new Vector2(1200, inspectingNodes ? 210 : 100);
            explanation.gameObject.SetActive(!reviewingResult && (!MenuOpen || editingProfile || inspectingNodes));
            drop.gameObject.SetActive(!reviewingResult && !(s.Overview && !treeMap) && nodeViews.Any(v => v.gameObject.activeSelf && v.nodeId == s.NodeId));
            if (s.PendingDecision == null) drop.position = Position(s.NodeId);
            foreach (var c in controls)
            {
                var enabled = true; var label = c.label;
                switch (c.action)
                {
                    case TreeAction.Menu: label.text = MenuOpen ? "Close menu" : "Menu"; break;
                    case TreeAction.Profile: case TreeAction.NextProfile: enabled = profiles.Profiles.Count > 0; break;
                    case TreeAction.PreviousTree: enabled = Ensemble.Index > 0; break;
                    case TreeAction.NextTree: enabled = Ensemble.Index + 1 < model.Trees.Count; break;
                    case TreeAction.EditProfile: enabled = Ensemble.Profile != null; break;
                    case TreeAction.SetMissing: enabled = editingProfile && model.Features[featureIndex].AllowMissing; break;
                    case TreeAction.CloseEdit: label.text = "Apply change"; break;
                    case TreeAction.TourDetail: label.text = Ensemble.Detail == Application.TourDetail.Detailed ? "Choose short tour" : "Choose detailed tour"; enabled = Ensemble.Mode == ExperienceMode.PreparedProfile; break;
                    case TreeAction.GroupRemaining: enabled = Ensemble.Mode == ExperienceMode.PreparedProfile && Ensemble.Detail == Application.TourDetail.Short && s.AtLeaf && Ensemble.Index < model.Trees.Count - 1; break;
                    case TreeAction.ReviseChoice: case TreeAction.ContinueFree: enabled = Ensemble.Mode == ExperienceMode.Manual && consistency.Status == RouteConsistency.Contradictory; break;
                    case TreeAction.Result: label.text = Ensemble.Complete ? "Review final result" : "Review contributions"; break;
                    case TreeAction.LargerEnsemble: enabled = !model.StructureOnlyPreview; label.text = model.Id == "synthetic-ensemble-24" ? "Original three-tree example" : "Larger ensemble: 24 trees"; break;
                    case TreeAction.DeepExample: enabled = !model.StructureOnlyPreview; label.text = deepExample ? "Original example" : "Deep tree: 8 levels"; break;
                    case TreeAction.TreeMap: label.text = treeMap ? "Return to focus" : "Tree overview"; break;
                    case TreeAction.TrueBranch: case TreeAction.FalseBranch:
                        enabled = s.CanAdvance;
                        label.text = Session.CurrentNode is SplitNode n ?
                            (c.action == TreeAction.TrueBranch ? "TRUE\n" : "FALSE\n") +
                            (n.Condition.Operator == DecisionOperator.LessThan ?
                                (c.action == TreeAction.TrueBranch ? "Fewer than " : "At least ") + Threshold(n.Condition.Threshold.Value) :
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
                bool resultControl = c.action == TreeAction.NextLedgerPage || c.action == TreeAction.CloseResult;
                bool editorControl = c.action == TreeAction.CancelEdit || c.action == TreeAction.SetMissing || c.action == TreeAction.NextFeature || c.action == TreeAction.DecreaseValue || c.action == TreeAction.IncreaseValue || c.action == TreeAction.RestoreProfile || c.action == TreeAction.CloseEdit;
                bool inspectorControl = c.action == TreeAction.NextNode || c.action == TreeAction.CloseInspect;
                bool secondary = IsSecondary(c.action);
                bool visible = reviewingResult ? resultControl || c.action == TreeAction.Menu : resultControl ? false : inspectorControl ? MenuOpen && inspectingNodes : editorControl ? MenuOpen && editingProfile : secondary ? MenuOpen && !editingProfile && !inspectingNodes : true;
                if (c.action == TreeAction.Step || c.action == TreeAction.Play || c.action == TreeAction.Pause)
                    visible = c.action == TreeAction.Pause ? !MenuOpen : !MenuOpen && s.Mode == ExperienceMode.PreparedProfile;
                if (c.action == TreeAction.TrueBranch || c.action == TreeAction.FalseBranch)
                    visible = !MenuOpen && !s.Overview && !s.AtLeaf;
                if (MenuOpen && !secondary && !editorControl && !inspectorControl && !resultControl && c.action != TreeAction.Menu) enabled = false;
                if (reviewingResult) visible = resultControl || c.action == TreeAction.Menu;
                c.gameObject.SetActive(visible);
                c.GetComponent<UnityEngine.UI.Button>().interactable = enabled;

            }
            Garden?.Sync(s.Overview && !treeMap);
        }
        public static bool IsSecondary(TreeAction action) => action == TreeAction.LargerEnsemble || action == TreeAction.DeepExample || action == TreeAction.TreeMap ||
            action == TreeAction.PreviousTree || action == TreeAction.NextTree || action == TreeAction.EditProfile ||
            action == TreeAction.Result || action == TreeAction.TourDetail || action == TreeAction.GroupRemaining || action == TreeAction.ReviseChoice || action == TreeAction.ContinueFree || action == TreeAction.NextProfile || action == TreeAction.Seated || action == TreeAction.Overview || action == TreeAction.InspectNodes;

        private static string Percent(double probability) => (probability * 100).ToString(
            probability > 0 && probability < .0001 ? "0.###E+0" : "0.00", CultureInfo.InvariantCulture);

        private string ConstraintCopy(ManualConstraint choice) => choice.TreeId + "/" + choice.NodeId + ": " +
            Condition(new SplitNode(choice.NodeId, choice.FeatureId, choice.Condition, "true", "false"), choice.Matched);

        private string LedgerCopy()
        {
            var rows = Ensemble.Ledger;
            var output = Ensemble.Evaluation;
            string text = (Ensemble.Complete ? "FINAL RESULT" : "CONTRIBUTION LEDGER") + "  |  Page " + (ledgerPage + 1) + " / " + Math.Max(1, (rows.Count + 3) / 4) +
                (model.StructureOnlyPreview ? "\nExport structure | " : "\nSynthetic data | ") + (Ensemble.Mode == ExperienceMode.Manual ? "Manual Route score" : Ensemble.Profile.DisplayName) +
                "\nBaseline " + Number(model.BaseScore) + " | Explained " + Ensemble.CompletedCount + " / " + rows.Count + " | Running raw total " + Number(Ensemble.RouteTotal);
            foreach (var row in rows.Skip(ledgerPage * 4).Take(4))
                text += "\n" + (row.Index + 1) + ". " + row.TreeId + " | " + (row.Progress == ContributionProgress.Pending ? "Pending" : row.Progress == ContributionProgress.Grouped ? "Grouped" : "Explained") + " | " +
                    (row.EvaluatedContribution.HasValue ? "evaluated " + Number(row.EvaluatedContribution.Value) : row.Progress == ContributionProgress.Pending ? "pending" : "route " + Number(row.PresentedContribution));
            var grouped = Ensemble.GroupedRows;
            if (grouped.Count > 0) text += "\nGrouped: " + grouped.Count + " trees | contribution " + Number(grouped.Sum(r => r.PresentedContribution)) + " | Every identity is listed on these pages.";
            if (output != null) text += "\nComplete raw score " + Number(output.RawScore) + " = baseline + ALL " + rows.Count + " contributions" +
                "\nOutput = sigmoid(raw score): " + Percent(output.Probability) + "% " + output.OutcomeLabel +
                "\nAlready computed; pending rows have not yet been explained. Synthetic model; source scoring not verified." +
                (Ensemble.Profile.Id != Ensemble.OriginalProfile.Id ? "\nHypothetical model sensitivity; not a causal promise." : "");
            else
            {
                var report = Ensemble.Consistency;
                text += "\n" + (report.Status == RouteConsistency.Contradictory ? "Conflicting choices" : report.Message) + "\nRoute score only. No complete customer probability.";
                var pageTreeIds = rows.Skip(ledgerPage * 4).Take(4).Select(r => r.TreeId).ToArray();
                foreach (var conflict in report.Conflicts.Where(c => pageTreeIds.Contains(c.TreeId)).Take(4)) text += "\n" + ConstraintCopy(conflict);
            }
            return text;
        }

        private void EditValue(int direction)
        {
            var feature = model.Features[featureIndex]; var old = Ensemble.Draft.GetValue(feature.Id);
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
            var result = Ensemble.StageEdit(feature.Id, next);
            if (!result.IsSuccess) message = "Change rejected: " + string.Join("; ", result.Diagnostics.Select(d => d.Code));
            else { message = "Draft updated. Apply change to evaluate, or Cancel edit to keep the accepted result."; }
            Refresh();
        }
        private bool applicationPaused, applicationUnfocused, lifecycleSuspended, pausedBeforeSuspension;
        private void OnApplicationPause(bool paused) { applicationPaused=paused; RefreshApplicationSuspension(); }
        private void OnApplicationFocus(bool focus) { applicationUnfocused=!focus; RefreshApplicationSuspension(); }
        private void RefreshApplicationSuspension()
        {
            if(Session==null)return;
            bool suspended=applicationPaused||applicationUnfocused;
            if(suspended==lifecycleSuspended)return;
            lifecycleSuspended=suspended;
            if(suspended) {
                pausedBeforeSuspension=Session.State.Paused;
                Session.SetPaused(true);
                NameTooltip?.Hide();
                Garden?.Navigation?.InvalidatePointerPresses();
            } else Session.SetPaused(pausedBeforeSuspension);
            Refresh();
        }
    }
}
