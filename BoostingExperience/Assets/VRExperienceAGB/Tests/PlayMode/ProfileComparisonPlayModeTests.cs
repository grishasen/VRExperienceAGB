using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRExperienceAGB.Domain;
using VRExperienceAGB.Presentation;

namespace VRExperienceAGB.Tests
{
    public class ProfileComparisonPlayModeTests
    {
        private OneTreeExperience view;
        private M5ForestPresentation Owner => view.Garden.M5;
        private ProfileComparisonPresentation UI => Owner.Comparison;
        [UnitySetUp]
        public IEnumerator Open()
        {
            yield return LegacyTeachingScene.Load(); yield return null;
            view = Object.FindAnyObjectByType<OneTreeExperience>();
             view.enabled = false;
            view.GetComponent<DesktopTreePreview>().enabled = false;
        }
        private void Click(M5Action action)
        {
            var target = view.GetComponentsInChildren<M5PointerTarget>().First(t => t.action == action);
            var data = new PointerEventData(EventSystem.current) { pointerId = 301, button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(target.gameObject, data, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target.gameObject, data, ExecuteEvents.pointerClickHandler);
        }
        private void Action(TreeAction action)
        { var s = view.Session.State; view.Execute(action, s.Revision, s.NodeId); }
        [UnityTest]
        public IEnumerator SyntheticEntryAndExitRestoreExportAndManualProgress()
        {
            var originalModel = view.Model;
            view.Garden.Navigation.ShowForest(); view.Garden.Navigation.OpenTree(1);
            Action(TreeAction.TrueBranch); view.Advance(2);
            var original = view.Ensemble; string node = view.Session.State.NodeId; double score = original.RouteTotal;
            Action(TreeAction.Menu); Click(M5Action.CompareProfiles);
            Assert.That(UI.PanelOpen, Is.True); Assert.That(view.Model.StructureOnlyPreview, Is.False);
            Assert.That(view.Comparison, Is.Not.Null); Assert.That(UI.SummaryText, Does.Contain("all 3 trees evaluated"));
            Assert.That(Object.FindObjectsByType<EventSystem>().Length, Is.EqualTo(1));
            Click(M5Action.CompareExit);
            Assert.That(view.Model, Is.SameAs(originalModel)); Assert.That(view.Ensemble, Is.SameAs(original));
            Assert.That(view.Ensemble.RouteTotal, Is.EqualTo(score)); Assert.That(view.Session.State.NodeId, Is.EqualTo(node));
            Assert.That(view.Comparison, Is.Null); Assert.That(UI.PanelOpen, Is.False);
            yield return null;
        }
        [UnityTest]
        public IEnumerator EditCancelApplyAndProfileSwitchPreserveAAndShowDivergence()
        {
            Click(M5Action.CompareProfiles); var pair = view.Comparison;
            Click(M5Action.CompareEdit); Click(M5Action.CompareIncrease); Click(M5Action.CompareCancel);
            Assert.That(pair.RawDelta, Is.Zero);
            Click(M5Action.CompareEdit);
            for (int i = 0; i < 5; i++) Click(M5Action.CompareIncrease);
            Click(M5Action.CompareApply);
            Assert.That(pair.RawDelta, Is.EqualTo(.9).Within(1e-12)); Assert.That(pair.Draft, Is.Null);
            Assert.That(UI.DetailText, Does.Contain("First divergence:"));
            Click(M5Action.CompareA); Assert.That(UI.PanelOpen, Is.False);
            Assert.That(view.Session.State.Overview, Is.False);
            Assert.That(view.controls.Single(c => c.action == TreeAction.Step).gameObject.activeInHierarchy, Is.True);
            Action(TreeAction.Step); view.Advance(2); string aNode = view.Session.State.NodeId;
            Assert.That(aNode, Is.Not.EqualTo(view.Session.Tree.RootId));
            Action(TreeAction.Menu); Click(M5Action.CompareProfiles); Click(M5Action.CompareB);
            Assert.That(view.Ensemble, Is.SameAs(pair.B));
            Assert.That(view.Session.State.NodeId, Is.EqualTo(view.Session.Tree.RootId));
            Assert.That(view.nodeViews.Any(n => n.gameObject.activeSelf && n.marker.text.Contains("A ONLY")), Is.True);
            Assert.That(view.nodeViews.Any(n => n.gameObject.activeSelf && n.marker.text.Contains("B ONLY")), Is.True);
            Assert.That(view.presentationRoot.Find("ProfileDropA").gameObject.activeInHierarchy, Is.True);
            Assert.That(view.presentationRoot.Find("ProfileDropB").gameObject.activeInHierarchy, Is.True);
            Action(TreeAction.TrueBranch); Assert.That(view.Ensemble.Evaluation, Is.Not.Null);
            Action(TreeAction.Menu); Click(M5Action.CompareProfiles); Click(M5Action.CompareA);
            Assert.That(view.Session.State.NodeId, Is.EqualTo(aNode));
            yield return null;
        }
        [UnityTest]
        public IEnumerator InspectDifferenceAndReturnDoNotCommitALeafOrMoveTrackedPose()
        {
            Click(M5Action.CompareProfiles); Click(M5Action.CompareNextB);
            Click(M5Action.CompareNextDifference);
            var head = view.Garden.Locomotion.Head; var position = head.localPosition; var rotation = head.localRotation;
            Click(M5Action.CompareInspect);
            Assert.That(view.FocusedView.FocusRoot, Is.Not.Null);
            Assert.That(view.Comparison.A.CompletedCount, Is.Zero); Assert.That(view.Comparison.B.CompletedCount, Is.Zero);
            Assert.That(head.localPosition, Is.EqualTo(position)); Assert.That(head.localRotation, Is.EqualTo(rotation));
            Owner.Activate(M5Action.FocusCurrent); Action(TreeAction.Menu); Action(TreeAction.Overview);
            Assert.That(view.Comparison, Is.Not.Null);
            yield return null;
        }
        [UnityTest]
        public IEnumerator RetiredPointerAndModelReplacementCannotMutateComparison()
        {
            Click(M5Action.CompareProfiles);
            var target = view.GetComponentsInChildren<M5PointerTarget>().Single(t => t.action == M5Action.CompareNextB);
            var data = new PointerEventData(EventSystem.current) { pointerId = 302, button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(target.gameObject, data, ExecuteEvents.pointerDownHandler);
            var before = view.Comparison.B.Profile;
            Click(M5Action.CompareEdit); Click(M5Action.CompareCancel);
            ExecuteEvents.Execute(target.gameObject, data, ExecuteEvents.pointerClickHandler);
            Assert.That(view.Comparison.B.Profile, Is.SameAs(before));
            Assert.That(view.LoadAgbStructure(view.modelFile.text, "Original export").IsSuccess, Is.True);
            Assert.That(view.Comparison, Is.Null); Assert.That(UI.PanelOpen, Is.False);
            yield return null;
        }
        [UnityTest]
        public IEnumerator ComparisonPanelHasReachableCanvasAndPagedFullDetails()
        {
            Click(M5Action.CompareProfiles); Click(M5Action.CompareNextB);
            yield return null;
            var canvas = view.GetComponentsInChildren<Canvas>().Single(c => c.name == "ProfileComparison");
            Assert.That(canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>(), Is.Not.Null);
            Assert.That(canvas.GetComponentInChildren<Oculus.Interaction.PointableCanvas>(), Is.Not.Null);
            Assert.That(view.Garden.M6.StoryText.gameObject.activeInHierarchy, Is.False);
            Assert.That(UI.DetailText, Does.Contain("A path:")); Assert.That(UI.DetailText, Does.Contain("B path:"));
            var text = canvas.GetComponentsInChildren<TMPro.TMP_Text>().Single(t => t.name == "Details");
            text.ForceMeshUpdate(); Assert.That(text.textInfo.pageCount, Is.GreaterThan(1));
            Click(M5Action.CompareNextDetail); Assert.That(text.pageToDisplay, Is.EqualTo(2));
            // Capture the real world-space panel in the Editor; never a substitute for headset readability.
            Click(M5Action.CompareClose); Owner.Activate(M5Action.CompareProfiles);
            var camera = view.Garden.Locomotion.Head.GetComponent<Camera>();
            var texture = new RenderTexture(1600, 1200, 24); var previous = camera.targetTexture;
            var active = RenderTexture.active; camera.targetTexture = texture; camera.Render(); RenderTexture.active = texture;
            var pixels = new Texture2D(1600, 1200, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, 1600, 1200), 0, 0); pixels.Apply();
            var directory = System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath, "../../artifacts/profile-comparison"));
            System.IO.Directory.CreateDirectory(directory); System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory, "comparison-panel.png"), pixels.EncodeToPNG());
            camera.targetTexture = previous; RenderTexture.active = active; Object.Destroy(pixels); texture.Release(); Object.Destroy(texture);
        }
    }
}
