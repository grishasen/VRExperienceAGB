using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using VRExperienceAGB.Application;

namespace VRExperienceAGB.Presentation
{
    public sealed partial class ExplorationExtensions
    {
        private GuidedReview review;
        private int stopIndex;
        private string reviewMessage = "Capture the current forest, tree, inspected node or profile comparison.";
        private Action restoreReview;
        private NavigationPage returnPage;
        private string returnFocus;
        private bool editingText;
        private int textPurpose;
        public string ReviewMessage => reviewMessage;
        private string textDraft = "";
        private Canvas keyboard, transport;
        private TMP_Text caption;
        private float reviewElapsed;
        public bool ReviewRunning { get; private set; }
        public bool ReviewPaused { get; private set; }
        public int ReviewStopIndex => stopIndex;
        public int ReviewStopCount => review?.stops.Count ?? 0;
        private const string Keys = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 .,-?";
        public string ReviewPath => Path.Combine(UnityEngine.Application.isEditor ?
            Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "../../data/private")) :
            Path.Combine(UnityEngine.Application.persistentDataPath, "private"), "guided-review.json");

        private void BuildReview()
        {
            var actions = new[] { M5Action.ReviewCapture, M5Action.ReviewPrevious, M5Action.ReviewNext,
                M5Action.ReviewEditText, M5Action.ReviewMoveEarlier, M5Action.ReviewMoveLater,
                M5Action.ReviewDelete, M5Action.ReviewSave, M5Action.ReviewLoad,
                M5Action.ReviewPlay, M5Action.ReviewPause, M5Action.ReviewStop };
            var labels = new[] { "Capture stop", "Previous stop", "Next stop", "Edit explanation", "Move earlier", "Move later",
                "Delete stop", "Save locally", "Load saved", "Replay", "Pause / Resume", "End review" };
            for (int i = 0; i < actions.Length; i++) Add(labels[i], -370 + (i % 3) * 370, -50 - (i / 3) * 60, actions[i]);
            keyboard = owner.Panel("ReviewExplanationKeyboard", new Vector2(1160, 410), panel.transform);
            keyboard.transform.localScale = Vector3.one; keyboard.transform.localPosition = new Vector3(0, -160, -1);
            for (int i = 0; i < Keys.Length; i++)
                owner.Button(keyboard, Keys[i] == ' ' ? "Space" : Keys[i].ToString(), new Vector2(-500 + (i % 10) * 111, 150 - (i / 10) * 64), new Vector2(103, 56), M5Action.ReviewKey, i, 24);
            owner.Button(keyboard, "Backspace", new Vector2(-230, -145), new Vector2(370, 60), M5Action.ReviewBackspace);
            owner.Button(keyboard, "Done", new Vector2(230, -145), new Vector2(370, 60), M5Action.ReviewTextDone);
            keyboard.gameObject.SetActive(false);
            transport = owner.Panel("GuidedReviewTransport", new Vector2(1050, 280), panel.transform.parent);
            caption = Garden.Text(transport.transform, "Explanation", "", new Vector2(0, 35), new Vector2(1000, 160), 25);
            caption.richText = false; caption.overflowMode = TextOverflowModes.Overflow; caption.enableAutoSizing = true; caption.fontSizeMin = 20; caption.fontSizeMax = 25;
            owner.Button(transport, "Pause / Resume", new Vector2(-340, -96), new Vector2(320, 56), M5Action.ReviewPause);
            owner.Button(transport, "Next stop", new Vector2(0, -96), new Vector2(320, 56), M5Action.ReviewNext);
            owner.Button(transport, "End review", new Vector2(340, -96), new Vector2(320, 56), M5Action.ReviewStop);
            transport.gameObject.SetActive(false);
        }
        private void BeginTextEntry(int purpose, string text) { textPurpose = purpose; textDraft = text; editingText = true; }
        public void TypeCharacter(int index)
        {
            if (!editingText || index < 0 || index >= Keys.Length || textDraft.Length >= 240) return;
            textDraft += Keys[index]; Refresh();
        }
        private void RefreshKeyboard()
        {
            keyboard.gameObject.SetActive(PanelOpen && editingText);
            foreach (var entry in buttons) {
                if (entry.Key == M5Action.ExtensionSearch || entry.Key == M5Action.ExtensionTrail || entry.Key == M5Action.ExtensionReview)
                    entry.Value.GetComponent<UnityEngine.UI.Button>().interactable = !editingText;
                else if (editingText) entry.Value.SetActive(false);
            }
            if (editingText) {
                body.rectTransform.sizeDelta = new Vector2(1080, 280); body.rectTransform.anchoredPosition = new Vector2(0, 155);
                body.text = (textPurpose == 1 ? "FIND FEATURE · " : textPurpose == 2 ? "ITERATION 0–" + model.Trees.Count + " · " : "EXPLANATION · ") + textDraft.Length + " / 240\n" + textDraft;
                body.pageToDisplay = 1;
            }
        }
        public void SetExplanation(string text)
        {
            if (ReviewRunning || review == null || review.stops.Count == 0) return;
            review.stops[stopIndex].explanation = (text ?? "").Substring(0, Math.Min(240, (text ?? "").Length));
        }
        private void ActivateReview(M5Action action)
        {
            if (editingText)
            {
                if (action == M5Action.ReviewBackspace && textDraft.Length > 0) textDraft = textDraft.Substring(0, textDraft.Length - 1);
                if (action == M5Action.ReviewTextDone) {
                    if (textPurpose == 1) FilterFeatures(textDraft);
                    else if (textPurpose == 2) { if (!int.TryParse(textDraft, out int iteration) || iteration < 0 || iteration > model.Trees.Count) { textDraft = ""; return; } Iteration = iteration; }
                    else SetExplanation(textDraft);
                    editingText = false; View.Refresh();
                }
                return;
            }
            if (ReviewRunning && action != M5Action.ReviewNext && action != M5Action.ReviewPrevious && action != M5Action.ReviewPause && action != M5Action.ReviewStop) return;
            switch (action)
            {
                case M5Action.ReviewCapture: CaptureStop(); break;
                case M5Action.ReviewPrevious: if (ReviewStopCount > 0) { stopIndex = Math.Max(0, stopIndex - 1); if (ReviewRunning) ShowStop(); } break;
                case M5Action.ReviewNext:
                    if (ReviewStopCount > 0 && stopIndex + 1 < ReviewStopCount) { stopIndex++; if (ReviewRunning) ShowStop(); }
                    else if (ReviewRunning) { ReviewPaused = true; View.Session.SetPaused(true); UpdateCaption(); } break;
                case M5Action.ReviewDelete: if (ReviewStopCount > 0) { review.stops.RemoveAt(stopIndex); stopIndex = Math.Max(0, Math.Min(stopIndex, ReviewStopCount - 1)); } break;
                case M5Action.ReviewMoveEarlier: MoveStop(-1); break;
                case M5Action.ReviewMoveLater: MoveStop(1); break;
                case M5Action.ReviewSave: SaveReview(); break;
                case M5Action.ReviewLoad: LoadReview(); break;
                case M5Action.ReviewEditText: if (ReviewStopCount > 0) { BeginTextEntry(0, review.stops[stopIndex].explanation ?? ""); } break;
                case M5Action.ReviewPlay: PlayReview(); break;
                case M5Action.ReviewPause: ReviewPaused = !ReviewPaused; View.Session.SetPaused(ReviewPaused); reviewElapsed = 0; UpdateCaption(); break;
                case M5Action.ReviewStop: StopReview("Review ended. Your previous exploration is restored."); break;
            }
            detailPage = 1;
        }
        public void CaptureStop()
        {
            if (ReviewRunning) return;
            if (review == null) review = GuidedReview.Create(View.Model);
            if (review.fingerprint != GuidedReview.Fingerprint(View.Model)) { reviewMessage = "This draft belongs to another model. Save it, then remove its stops before capturing here."; if (review.stops.Count == 0) review = GuidedReview.Create(View.Model); else return; }
            bool tree = Garden.Navigation.Page == NavigationPage.Tree;
            string node = tree ? View.FocusedView.FocusRoot : null;
            var kind = View.Comparison != null ? ReviewStopKind.Comparison : tree && View.Ensemble.Evaluation != null ? ReviewStopKind.Profile :
                node != null ? ReviewStopKind.Node : tree ? ReviewStopKind.Tree : ReviewStopKind.Forest;
            review.stops.Add(new ReviewStop {
                kind = kind, treeId = View.Session.Tree.Id, nodeId = node,
                explanation = kind == ReviewStopKind.Forest ? "Observe the ordered ensemble." : "Inspect tree " + (View.Ensemble.Index + 1) + (node == null ? "." : ", node " + node + "."),
                profileA = ReviewProfile.Capture(View.Comparison?.A.Profile ?? (View.Ensemble.Evaluation != null ? View.Ensemble.Profile : null)),
                profileB = ReviewProfile.Capture(View.Comparison?.B.Profile), showingB = View.Comparison?.ShowingB == true
            });
            stopIndex = review.stops.Count - 1; reviewMessage = "Stop captured. Edit its explanation, then close this panel to choose another target.";
        }
        private void MoveStop(int offset)
        {
            int destination = stopIndex + offset;
            if (review == null || destination < 0 || destination >= review.stops.Count) return;
            var stop = review.stops[stopIndex]; review.stops.RemoveAt(stopIndex); review.stops.Insert(destination, stop); stopIndex = destination;
        }
        public bool SaveReview()
        {
            if (ReviewRunning || review == null || ReviewStopCount == 0) { reviewMessage = "Capture at least one stop first."; return false; }
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(ReviewPath));
                string temp = ReviewPath + ".tmp"; File.WriteAllText(temp, JsonUtility.ToJson(review, true));
                if (File.Exists(ReviewPath)) File.Replace(temp, ReviewPath, null); else File.Move(temp, ReviewPath);
                reviewMessage = "Review saved locally, including explicit profile snapshots."; return true;
            } catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { reviewMessage = "Save failed: " + e.Message; return false; }
        }
        public bool LoadReview()
        {
            if (ReviewRunning) return false;
            try {
                var candidate = JsonUtility.FromJson<GuidedReview>(File.ReadAllText(ReviewPath));
                string error = candidate?.Validate(View.Model) ?? (candidate == null ? "Invalid review file." : null);
                if (error != null) { reviewMessage = error; return false; }
                review = candidate; stopIndex = 0; reviewMessage = "Saved review loaded. Targets and profiles validated."; return true;
            } catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException) { reviewMessage = "Load failed: " + e.Message; return false; }
        }
        public bool PlayReview()
        {
            string error = review?.Validate(View.Model) ?? (review == null ? "Capture stops first." : null);
            if (error != null) { reviewMessage = error; return false; }
            if (ReviewRunning) return false;
            returnPage = Garden.Navigation.Page; returnFocus = View.FocusedView.FocusRoot;
            restoreReview = View.IsolateReview(); ReviewRunning = true; ReviewPaused = false; stopIndex = 0;
            SearchActive = TrailActive = false; Close(); ShowStop(); return true;
        }
        private void ShowStop()
        {
            var stop = review.stops[stopIndex];
            Garden.Navigation.ShowForest();
            View.SetReviewProfiles(stop.profileA?.present == true ? stop.profileA.Restore(model.Id) : null, stop.profileB?.present == true ? stop.profileB.Restore(model.Id) : null, stop.showingB);
            if (stop.kind != ReviewStopKind.Forest) {
                int index = model.Trees.ToList().FindIndex(t => t.Id == stop.treeId);
                Inspect(index, stop.nodeId);
                if ((stop.kind == ReviewStopKind.Profile || stop.kind == ReviewStopKind.Comparison) && stop.nodeId == null) View.Session.SetPlaying(true);
                View.Session.SetPaused(ReviewPaused);
            }
            reviewElapsed = 0; UpdateCaption();
        }
        private void UpdateCaption()
        {
            transport.gameObject.SetActive(ReviewRunning && !PanelOpen);
            if (!ReviewRunning) return;
            caption.text = (ReviewPaused ? "PAUSED" : "GUIDED REVIEW") + " · stop " + (stopIndex + 1) + " / " + ReviewStopCount +
                "\n" + review.stops[stopIndex].explanation;
            var head = Garden.Locomotion.Head; var forward = head.forward; forward.y = 0;
            if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
            forward.Normalize(); transport.transform.rotation = Quaternion.LookRotation(forward);
            transport.transform.position = head.position + forward * 1.55f - Vector3.up * .65f;
        }
        public void AdvanceReview(float seconds)
        {
            if (!ReviewRunning || ReviewPaused || PanelOpen || owner.HelpOpen || owner.Comparison.PanelOpen ||
                View.ApplicationSuspended || View.Session.State.Paused || seconds <= 0 || !float.IsFinite(seconds)) return;
            reviewElapsed += seconds;
            if (reviewElapsed < 12 || View.Session.State.Playing || View.Session.State.PendingDecision != null) return;
            if (stopIndex + 1 < ReviewStopCount) { stopIndex++; ShowStop(); }
            else { ReviewPaused = true; View.Session.SetPaused(true); UpdateCaption(); }
        }
        public void StopReview(string message)
        {
            bool wasRunning = ReviewRunning; ReviewRunning = false; ReviewPaused = false; editingText = false;
            if (transport != null) transport.gameObject.SetActive(false);
            var restore = restoreReview; restoreReview = null;
            if (wasRunning && restore != null) {
                Garden.Navigation.ShowForest(); restore();
                if (returnPage == NavigationPage.Tree) { Garden.Navigation.OpenTree(View.Ensemble.Index); if (returnFocus != null) View.FocusedView.Inspect(returnFocus); }
                else if (returnPage == NavigationPage.Home) Garden.Navigation.ShowHome();
                else if (returnPage == NavigationPage.Diorama) Garden.Navigation.ShowDiorama();
                else if (returnPage == NavigationPage.TreeList) Garden.Navigation.Activate(GardenCommand.SingleTree, 0);
                View.Refresh();
            }
            reviewMessage = message;
        }
        private void RefreshReview()
        {
            heading.text = "RECORDED GUIDED REVIEW";
            body.rectTransform.sizeDelta = new Vector2(1080, 280);
            body.rectTransform.anchoredPosition = new Vector2(0, 155);
            body.text = ReviewStopCount == 0 ? "No stops yet. Close the panel, choose a view, then capture it here.\nStops can contain a forest, tree, node, prepared profile or A/B comparison." :
                "Stop " + (stopIndex + 1) + " / " + ReviewStopCount + " · " + review.stops[stopIndex].kind + "\n" +
                review.stops[stopIndex].treeId + (string.IsNullOrEmpty(review.stops[stopIndex].nodeId) ? "" : " / " + review.stops[stopIndex].nodeId) + "\n" + review.stops[stopIndex].explanation;
            notice.text = reviewMessage + "\nReplay: 12 seconds per stop; Pause to explore. Saved only on this device.";
            foreach (var a in new[] { M5Action.ReviewDelete, M5Action.ReviewSave, M5Action.ReviewPlay, M5Action.ReviewEditText }) Enable(a, ReviewStopCount > 0 && !ReviewRunning);
            Enable(M5Action.ReviewMoveEarlier, stopIndex > 0 && !ReviewRunning);
            Enable(M5Action.ReviewMoveLater, stopIndex + 1 < ReviewStopCount && !ReviewRunning);
            Enable(M5Action.ReviewPrevious, stopIndex > 0); Enable(M5Action.ReviewNext, stopIndex + 1 < ReviewStopCount);
            Enable(M5Action.ReviewCapture, !ReviewRunning); Enable(M5Action.ReviewLoad, !ReviewRunning);
            Enable(M5Action.ReviewPause, ReviewRunning); Enable(M5Action.ReviewStop, ReviewRunning);
        }
    }
}
