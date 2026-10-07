using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRExperienceAGB.Presentation;

namespace VRExperienceAGB.Tests
{
    public class ExplorationExtensionsPlayModeTests
    {
        private OneTreeExperience view;
        private ExplorationExtensions UI => view.Garden.M5.Extensions;
        [UnitySetUp]
        public IEnumerator Open()
        {
            yield return SceneManager.LoadSceneAsync("OneTreeLearning"); yield return null;
            view = Object.FindAnyObjectByType<OneTreeExperience>(); view.enabled = false;
            view.GetComponent<DesktopTreePreview>().enabled = false;
        }
        private void Click(M5Action action)
        {
            var target = view.GetComponentsInChildren<M5PointerTarget>().First(t => t.action == action);
            var data = new PointerEventData(EventSystem.current) { pointerId = 351, button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(target.gameObject, data, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target.gameObject, data, ExecuteEvents.pointerClickHandler);
        }
        [UnityTest]
        public IEnumerator SearchOpensFromHomeAndInspectsExactMatchWithoutRouteCommit()
        {
            Click(M5Action.Extensions); Assert.That(UI.PanelOpen, Is.True); Assert.That(UI.MatchCount, Is.GreaterThan(0));
            Assert.That(UI.BodyText, Does.Contain(UI.Feature));
            var head = view.Garden.Locomotion.Head; var position = head.localPosition; var rotation = head.localRotation;
            Click(M5Action.SearchInspect);
            Assert.That(UI.PanelOpen, Is.False); Assert.That(view.FocusedView.FocusRoot, Is.Not.Null);
            var node = view.Session.Tree.Nodes.OfType<VRExperienceAGB.Domain.SplitNode>().Single(n => n.Id == view.FocusedView.FocusRoot);
            Assert.That(node.FeatureId, Is.EqualTo(UI.Feature)); Assert.That(view.Ensemble.CompletedCount, Is.Zero);
            Assert.That(head.localPosition, Is.EqualTo(position)); Assert.That(head.localRotation, Is.EqualTo(rotation));
            yield return null;
        }
        [UnityTest]
        public IEnumerator PathScopeAndRevealDoNotChangeCompleteEvaluation()
        {
            view.BeginComparison(true); var pair = view.Comparison; var full = pair.A.Evaluation;
            UI.Open(); Click(M5Action.SearchScope); Assert.That(UI.MatchCount, Is.GreaterThan(0));
            Click(M5Action.ExtensionTrail); Click(M5Action.TrailBaseline); Click(M5Action.TrailReveal);
            Assert.That(view.Garden.VisiblePlotCount, Is.Zero);
            Click(M5Action.TrailNext); Assert.That(view.Garden.VisiblePlotCount, Is.EqualTo(1));
            Assert.That(UI.BodyText, Does.Contain("-3.8")); Assert.That(pair.A.Evaluation, Is.SameAs(full));
            Click(M5Action.TrailLast); Assert.That(view.Garden.VisiblePlotCount, Is.EqualTo(3));
            Assert.That(UI.BodyText, Does.Contain("-4.1")); Assert.That(pair.A.CompletedCount, Is.Zero);
            Click(M5Action.TrailInspect); Assert.That(view.Ensemble.Index, Is.EqualTo(2));
            yield return null;
        }
        [UnityTest]
        public IEnumerator RecordedStopsPersistPauseReplayAndRestoreManualRoute()
        {
            view.Garden.Navigation.ShowForest(); UI.Open(2); Click(M5Action.ReviewCapture); UI.Close();
            view.Garden.Navigation.OpenTree(1);
            var state = view.Session.State; view.Execute(TreeAction.TrueBranch, state.Revision, state.NodeId); view.Advance(2);
            var original = view.Ensemble; string node = view.Session.State.NodeId;
            UI.Open(2); Click(M5Action.ReviewCapture); UI.SetExplanation("Inspect the selected tree.");
            byte[] previous = File.Exists(UI.ReviewPath) ? File.ReadAllBytes(UI.ReviewPath) : null;
            try {
                Assert.That(UI.SaveReview(), Is.True, UI.ReviewMessage); Assert.That(UI.LoadReview(), Is.True, UI.ReviewMessage); Assert.That(UI.ReviewStopCount, Is.EqualTo(2));
            } finally { if (previous == null) File.Delete(UI.ReviewPath); else File.WriteAllBytes(UI.ReviewPath, previous); }
            Click(M5Action.ReviewPlay); Assert.That(UI.ReviewRunning, Is.True); Assert.That(view.Ensemble, Is.Not.SameAs(original));
            Click(M5Action.ReviewPause); UI.AdvanceReview(30); Assert.That(UI.ReviewStopIndex, Is.Zero);
            Click(M5Action.ReviewPause); UI.AdvanceReview(13); Assert.That(UI.ReviewStopIndex, Is.EqualTo(1));
            Click(M5Action.ReviewStop); Assert.That(view.Ensemble, Is.SameAs(original));
            Assert.That(view.Session.State.NodeId, Is.EqualTo(node)); Assert.That(UI.ReviewRunning, Is.False);
            yield return null;
        }
        [UnityTest]
        public IEnumerator KeyboardAndReorderingKeepAuthoredExplanationWithItsStop()
        {
            UI.Open(2); Click(M5Action.ReviewCapture); UI.SetExplanation("");
            Click(M5Action.ReviewEditText); UI.TypeCharacter(0); UI.TypeCharacter(1); Click(M5Action.ReviewBackspace); Click(M5Action.ReviewTextDone);
            Assert.That(UI.BodyText, Does.Contain("\nA"));
            Click(M5Action.ReviewCapture); UI.SetExplanation("Second"); Click(M5Action.ReviewMoveEarlier);
            Assert.That(UI.BodyText, Does.Contain("Second")); Click(M5Action.ReviewNext); Assert.That(UI.BodyText, Does.Contain("\nA"));
            yield return null;
        }
        [UnityTest]
        public IEnumerator ReplacementClearsOverlaysAndRejectsSavedTourForChangedModel()
        {
            UI.Open(2); Click(M5Action.ReviewCapture); UI.Close();
            view.BeginComparison(true); UI.Open(2);
            Assert.That(UI.PlayReview(), Is.False); Assert.That(UI.SearchActive, Is.False); Assert.That(UI.TrailActive, Is.False);
            yield return null;
        }
        [UnityTest]
        public IEnumerator PanelsHavePointerCanvasAndCaptureActualRendering()
        {
            UI.Open(); yield return null;
            var canvas = view.GetComponentsInChildren<Canvas>().Single(c => c.name == "ExplorationExtensions");
            Assert.That(canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>(), Is.Not.Null);
            Assert.That(canvas.GetComponentInChildren<Oculus.Interaction.PointableCanvas>(), Is.Not.Null);
            Capture("predictor-search");
            Click(M5Action.ExtensionTrail); yield return null; Capture("boosting-trail");
            Click(M5Action.ExtensionReview); Click(M5Action.ReviewCapture); yield return null; Capture("guided-review"); Click(M5Action.ReviewEditText); yield return null; Capture("review-keyboard");
        }
        [UnityTest]
        public IEnumerator FeatureFilterAndIterationEntryUseControllerKeyboard()
        {
            view.BeginComparison(true); UI.Open();
            Click(M5Action.SearchFilter); foreach (char c in "LOYALTY") UI.TypeCharacter(c - 'A'); Click(M5Action.ReviewTextDone);
            Assert.That(UI.Feature, Is.EqualTo("Customer.LoyaltyTier")); Assert.That(UI.MatchCount, Is.EqualTo(1));
            UI.FilterFeatures("no_such_feature"); UI.Refresh(); Assert.That(UI.MatchCount, Is.Zero);
            Assert.That(UI.BodyText, Does.Contain("No matching predictors"));
            Click(M5Action.ExtensionTrail); Click(M5Action.TrailJump); Click(M5Action.ReviewBackspace); UI.TypeCharacter(27); Click(M5Action.ReviewTextDone);
            Assert.That(UI.Iteration, Is.EqualTo(1)); Assert.That(UI.BodyText, Does.Contain("-3.8"));
            yield return null;
        }
        [UnityTest]
        public IEnumerator SavedComparisonRestoresEditedBAndPausesDeterministicPlayback()
        {
            view.BeginComparison(true); var original = view.Comparison;
            original.BeginEdit(); original.StageEdit("IH.Web.Inbound.Clicked.pyHistoricalOutcomeCount", VRExperienceAGB.Domain.ProfileValue.FromNumber(5));
            Assert.That(original.ApplyEdit().IsSuccess, Is.True);
            view.ShowComparisonProfile(true, 0); view.Garden.Navigation.OpenTree(0);
            UI.Open(2); Click(M5Action.ReviewCapture); Click(M5Action.ReviewPlay);
            Assert.That(view.Comparison, Is.Not.SameAs(original)); Assert.That(view.Comparison.RawDelta, Is.EqualTo(.9).Within(1e-12));
            Assert.That(view.Comparison.ShowingB, Is.True); Assert.That(view.Session.State.Playing, Is.True);
            Click(M5Action.ReviewPause); Assert.That(view.Session.State.Paused, Is.True);
            var node = view.Session.State.NodeId; view.Advance(30); Assert.That(view.Session.State.NodeId, Is.EqualTo(node));
            Click(M5Action.ReviewStop); Assert.That(view.Comparison, Is.SameAs(original));
            yield return null;
        }
        private void Capture(string name)
        {
            var camera = view.Garden.Locomotion.Head.GetComponent<Camera>(); var previous = camera.targetTexture; var active = RenderTexture.active;
            var texture = new RenderTexture(1600, 1200, 24); camera.targetTexture = texture; camera.Render(); RenderTexture.active = texture;
            var pixels = new Texture2D(1600, 1200, TextureFormat.RGB24, false); pixels.ReadPixels(new Rect(0, 0, 1600, 1200), 0, 0); pixels.Apply();
            string directory = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "../../artifacts/extensions"));
            Directory.CreateDirectory(directory); File.WriteAllBytes(Path.Combine(directory, name + ".png"), pixels.EncodeToPNG());
            camera.targetTexture = previous; RenderTexture.active = active; Object.Destroy(pixels); texture.Release(); Object.Destroy(texture);
        }
    }
}
