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
        public bool Ready => Session != null;
        private ModelDefinition model;
        private ProfileSet profiles;
        private int profileIndex;
        private long movingEvent = -1;
        private float moveElapsed;
        private float dwell;
        private long displayedRevision = -1;
        private bool seated;
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
                !nodeViews.Select(v => v.nodeId).OrderBy(id => id).SequenceEqual(model.Trees[0].Nodes.Select(n => n.Id).OrderBy(id => id)))
                return Fail("The scene does not match the model's node identities.");
            if (drop == null || status == null || explanation == null || score == null || feedback == null || controls == null ||
                idleMaterial == null || visitedMaterial == null || activeMaterial == null || presentationRoot == null)
                return Fail("The teaching scene is missing a required reference.");
            foreach (var view in nodeViews)
            {
                var node = model.Trees[0].Nodes.Single(n => n.Id == view.nodeId);
                view.title.text = node is SplitNode split ? Condition(split, true) : "LEAF  " + Number(((LeafNode)node).Score * model.Trees[0].Weight);
            }
            return Replace(TreeSession.CreateManual(model, model.Trees[0].Id), true);
        }

        private bool Fail(string reason)
        {
            Session = null;
            if (status != null) status.text = "Demo unavailable";
            if (explanation != null) explanation.text = "Data could not be loaded. " + reason;
            if (score != null) score.text = "No route score is available.";
            if (controls != null) foreach (var c in controls.Where(c => c != null)) c.GetComponent<UnityEngine.UI.Button>().interactable = false;
            return false;
        }
        private bool Replace(Outcome<TreeSession> outcome, bool overview)
        {
            if (!outcome.IsSuccess) return Fail(string.Join("; ", outcome.Diagnostics.Select(d => d.Code)));
            Session = outcome.Value;
            if (overview) Session.ReturnToOverview();
            movingEvent = -1; dwell = 0; displayedRevision = -1; Refresh();
            return true;
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
        private Vector3 Position(string nodeId) => nodeViews.Single(v => v.nodeId == nodeId).dropAnchor.position;

        public void Execute(TreeAction action, long revision, string nodeId)
        {
            if (Session == null) return;
            CommandReply reply = default;
            switch (action)
            {
                case TreeAction.TrueBranch: reply = Session.ChooseBranch(revision, nodeId, true); break;
                case TreeAction.FalseBranch: reply = Session.ChooseBranch(revision, nodeId, false); break;
                case TreeAction.Step: reply = Session.StepProfile(revision, nodeId); break;
                case TreeAction.Play: reply = Session.SetPlaying(true); break;
                case TreeAction.Pause: reply = Session.SetPaused(!Session.State.Paused); break;
                case TreeAction.Back: reply = Session.Back(); break;
                case TreeAction.Restart: reply = Session.Restart(); break;
                case TreeAction.Overview: reply = Session.State.Overview ? Session.EnterTree() : Session.ReturnToOverview(); break;
                case TreeAction.Manual: Replace(TreeSession.CreateManual(model, model.Trees[0].Id), false); message = "Manual exploration. Choices are not a customer prediction."; Refresh(); return;
                case TreeAction.Profile: Replace(TreeSession.CreatePrepared(model, model.Trees[0].Id, profiles.Profiles[profileIndex]), false); message = "Follow this synthetic profile with Step or Play."; Refresh(); return;
                case TreeAction.NextProfile: profileIndex = (profileIndex + 1) % profiles.Profiles.Count; message = "Profile selected. Review its values, then choose Follow profile to start."; Refresh(); return;
                case TreeAction.Seated:
                    seated = !seated; presentationRoot.localPosition = new Vector3(0, seated ? -0.4f : 0, 0);
                    message = seated ? "Seated layout: presentation lowered; tracking space unchanged." : "Standing layout restored.";
                    Refresh(); return;
            }
            message = reply.Message;
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
                if (t >= 1 && !state.Paused && !state.Overview) { Session.CompleteMove(movingEvent); movingEvent = -1; }
            }
            if (Session.State.Revision != displayedRevision) Refresh();
        }

        public void Refresh()
        {
            if (Session == null) return;
            var s = Session.State; displayedRevision = s.Revision;
            status.text = (s.Overview ? "FOREST ENTRY  |  " : "TREE 01  |  ") + (s.Mode == ExperienceMode.Manual ? "EXPLORE BRANCHES" : "FOLLOW A PROFILE") +
                (s.Paused ? "  |  PAUSED" : s.PendingDecision != null ? "  |  MOVING" : s.Playing ? "  |  PLAYING" : "");
            var chosen = profiles.Profiles[profileIndex];
            if (s.Overview)
                explanation.text = "One tree. Two ways to learn.\nSelect Explore branches to choose freely, or review a profile below and follow its decisions.\nReturn to tree keeps your saved route.";
            else if (Session.CurrentNode is SplitNode split)
            {
                var decision = Session.CurrentProfileDecision;
                explanation.text = Condition(split, true) + "?\n" + (decision == null ? "Choose TRUE or FALSE. Either route is available." :
                    Session.Profile.DisplayName + ": " + Value(decision.ObservedValue) + " -> " + (decision.Matched ? "TRUE" : "FALSE")) +
                    "\nChoosing a branch yourself enters manual exploration.";
            }
            else explanation.text = "Leaf reached: " + s.NodeId + "\nThis tree " + (s.Contribution >= 0 ? "adds " : "subtracts ") + Number(Math.Abs(s.Contribution)) +
                " to the raw score.\nUse Back to try another route, or Replay to start again.";
            score.text = s.ScoreMeaning + "  " + Number(s.RouteTotal) + "\nBaseline " + Number(s.Baseline) + "  +  this leaf " + Number(s.Contribution) +
                "\nSynthetic example | 1 of " + model.Trees.Count + " trees | Not the full prediction";
            feedback.text = "Selected profile: " + chosen.DisplayName + "\n" + string.Join("  |  ", chosen.Values.Select(p => Feature(p.Key) + ": " + Value(p.Value))) + "\n" + message;
            foreach (var v in nodeViews)
            {
                var current = v.nodeId == s.NodeId; var visited = s.Path.Contains(v.nodeId);
                v.platform.sharedMaterial = current ? activeMaterial : visited ? visitedMaterial : idleMaterial;
                v.marker.text = current ? "CURRENT" : visited ? "VISITED" : "";
            }
            if (s.PendingDecision == null) drop.position = Position(s.NodeId);
            foreach (var c in controls)
            {
                var enabled = true; var label = c.label;
                switch (c.action)
                {
                    case TreeAction.TrueBranch: case TreeAction.FalseBranch:
                        enabled = s.CanAdvance;
                        label.text = Session.CurrentNode is SplitNode n ? (c.action == TreeAction.TrueBranch ? "TRUE  " : "FALSE  ") + Condition(n, c.action == TreeAction.TrueBranch) : "Leaf reached";
                        break;
                    case TreeAction.Step: enabled = s.CanAdvance && s.Mode == ExperienceMode.PreparedProfile; break;
                    case TreeAction.Play: enabled = !s.Overview && !s.AtLeaf && s.Mode == ExperienceMode.PreparedProfile; break;
                    case TreeAction.Pause: label.text = s.Paused ? "Resume" : "Pause"; break;
                    case TreeAction.Overview: label.text = s.Overview ? "Return to tree" : "Forest entry"; break;
                    case TreeAction.Restart: label.text = s.Mode == ExperienceMode.PreparedProfile ? "Replay" : "Restart"; break;
                    case TreeAction.Seated: label.text = seated ? "Standing layout" : "Seated layout"; break;
                }
                c.GetComponent<UnityEngine.UI.Button>().interactable = enabled;
            }
        }
        private void OnApplicationPause(bool paused) { if (paused && Session != null) { Session.SetPaused(true); Refresh(); } }
        private void OnApplicationFocus(bool focus) { if (!focus && Session != null) { Session.SetPaused(true); Refresh(); } }
    }
}
