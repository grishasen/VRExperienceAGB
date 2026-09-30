using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRExperienceAGB.Application;
using VRExperienceAGB.Presentation;

namespace VRExperienceAGB.Tests
{
    public class OneTreePlayModeTests
    {
        private OneTreeExperience view;
        [UnitySetUp]
        public IEnumerator OpenTeachingScene()
        {
            yield return SceneManager.LoadSceneAsync("OneTreeLearning", LoadSceneMode.Single);
            yield return null;
            view = Object.FindAnyObjectByType<OneTreeExperience>();
            Assert.That(view,Is.Not.Null); Assert.That(view.Ready,Is.True);
            // Drive deterministic time explicitly; production uses the same Advance entry point from Update.
            view.enabled = false;
            if(view.Session.State.Paused) view.Session.SetPaused(false);
        }
        private void Click(TreeAction action,int pointerId=1)
        {
            bool editorAction = action == TreeAction.CancelEdit || action == TreeAction.SetMissing || action == TreeAction.NextLedgerPage || action == TreeAction.CloseResult || action == TreeAction.NextFeature || action == TreeAction.DecreaseValue || action == TreeAction.IncreaseValue || action == TreeAction.RestoreProfile || action == TreeAction.CloseEdit || action == TreeAction.NextNode || action == TreeAction.CloseInspect;
            if (OneTreeExperience.IsSecondary(action) && !view.MenuOpen) Click(TreeAction.Menu, pointerId);
            else if (!OneTreeExperience.IsSecondary(action) && !editorAction && action != TreeAction.Menu && view.MenuOpen) Click(TreeAction.Menu, pointerId);
            var button=view.controls.Single(c=>c.action==action);
            var pointer=new PointerEventData(EventSystem.current) { pointerId=pointerId,button=PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        }
        private void Arrive() { view.Advance(2); }
        [UnityTest]
        public IEnumerator NameHoverShowsCompleteSourceAndScrollsWithoutChangingTheRoute()
        {
            var categories = string.Join(", ", Enumerable.Range(0,108).Select(i => "FictionalCategory_" + i));
            var raw = "Audience.VeryLongPredictorName in { " + categories + " }";
            var json = "{\"type\":\"AdaptiveBoostScoringModel\",\"algorithm\":\"GRADIENT_BOOST\",\"model\":{\"booster\":{\"trees\":[{\"score\":999,\"gain\":1,\"sampleCount\":10,\"split\":\"PREDICATE\",\"left\":{\"score\":-0.25,\"gain\":0,\"sampleCount\":5},\"right\":{\"score\":0.75,\"gain\":0,\"sampleCount\":5}}]}}}".Replace("PREDICATE", raw);
            Assert.That(view.LoadAgbStructure(json.ToString(),"Fictional hover example").IsSuccess,Is.True);
            Click(TreeAction.Manual);
            var state=view.Session.State; var total=view.Ensemble.RouteTotal;
            // Keep the physical mouse from injecting a second pointer during this deterministic event test.
            Object.FindAnyObjectByType<DesktopTreePreview>().enabled=false;
            var hover=view.explanation.GetComponent<NodeNameHover>();
            var pointer=new PointerEventData(EventSystem.current) { pointerId=-1 };
            ExecuteEvents.Execute(hover.gameObject,pointer,ExecuteEvents.pointerEnterHandler);
            Assert.That(view.NameTooltip.Visible,Is.True);
            Assert.That(view.NameTooltip.Text.text,Does.Contain("Audience.VeryLongPredictorName").And.Contain(raw).And.Contain("FictionalCategory_107"));
            Canvas.ForceUpdateCanvases();
            var scroll=view.NameTooltip.Text.GetComponentInParent<UnityEngine.UI.ScrollRect>();
            Assert.That(scroll.content.rect.height,Is.GreaterThan(scroll.viewport.rect.height));
            pointer.scrollDelta=new Vector2(0,-5); scroll.OnScroll(pointer);
            Assert.That(scroll.verticalNormalizedPosition,Is.LessThan(1));
            Assert.That(view.Session.State.Revision,Is.EqualTo(state.Revision));
            Assert.That(view.Ensemble.RouteTotal,Is.EqualTo(total));
            ExecuteEvents.Execute(hover.gameObject,pointer,ExecuteEvents.pointerExitHandler);
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(view.NameTooltip.Visible,Is.False);
            foreach(var node in view.nodeViews)
            {
                Assert.That(node.title.raycastTarget,Is.True);
                Assert.That(node.title.GetComponentInParent<Canvas>(true).GetComponent<UnityEngine.UI.GraphicRaycaster>(),Is.Not.Null);
            }
        }

        [UnityTest]
        public IEnumerator NestedExportLoadsDirectlyAndManualUndoNeverProducesProbability()
        {
            const string json = "{\"type\":\"AdaptiveBoostScoringModel\",\"algorithm\":\"GRADIENT_BOOST\",\"model\":{\"booster\":{\"trees\":[{\"score\":999,\"gain\":4,\"sampleCount\":10,\"split\":\"Audience.Score < 5.017708e-4\",\"left\":{\"score\":-0.25,\"gain\":0,\"sampleCount\":5},\"right\":{\"score\":0.75,\"gain\":0,\"sampleCount\":5}}]}}}";
            var camera = Object.FindAnyObjectByType<DesktopTreePreview>().previewCamera;
            var position = camera.transform.position; var rotation = camera.transform.rotation;
            view.modelFile = new TextAsset(json) { name = "Fictional nested export" };
            view.profilesFile = null;
            Assert.That(view.Initialize(), Is.True);
            Assert.That(view.Model.StructureOnlyPreview, Is.True);
            Assert.That(view.score.text, Does.Contain("Export structure").And.Contain("No profile probability"));
            Assert.That(view.controls.Single(c => c.action == TreeAction.Profile).GetComponent<UnityEngine.UI.Button>().interactable, Is.False);
            var accepted = view.Ensemble;
            Assert.That(view.LoadAgbStructure("{}", "Invalid").IsSuccess, Is.False);
            Assert.That(view.Ensemble, Is.SameAs(accepted));
            Click(TreeAction.Manual);
            Assert.That(view.explanation.text, Does.Not.Contain("< 0?"));
            Click(TreeAction.TrueBranch); Arrive();
            Assert.That(view.Session.State.NodeId, Is.EqualTo("root/left"));
            Assert.That(view.Ensemble.RouteTotal, Is.EqualTo(-.25));
            Assert.That(view.Ensemble.Evaluation, Is.Null);
            Click(TreeAction.Result);
            Assert.That(view.ledgerText.text, Does.Contain("Export structure").And.Contain("No complete customer probability"));
            Click(TreeAction.CloseResult); Click(TreeAction.Back);
            Assert.That(view.Ensemble.RouteTotal, Is.Zero);
            Click(TreeAction.FalseBranch); Arrive();
            Assert.That(view.Ensemble.RouteTotal, Is.EqualTo(.75));
            Assert.That(camera.transform.position, Is.EqualTo(position));
            Assert.That(camera.transform.rotation, Is.EqualTo(rotation));
            yield return null;
        }

        [UnityTest]
        public IEnumerator PostureMovesAllTeachingPartsTogetherAndReturnsWithoutDrift()
        {
            Click(TreeAction.Manual);
            var camera=Object.FindAnyObjectByType<DesktopTreePreview>().previewCamera;
            var cameraPosition=camera.transform.position;
            var objects=view.nodeViews.SelectMany(n=>new[]{n.transform,n.title.transform.parent,n.dropAnchor,n.transform.Find("LuminousRim")})
                .Concat(new[]{view.drop,view.score.transform,view.presentationRoot.Find("ConsoleStone"),view.presentationRoot.Find("TrueChoiceStone")}).ToArray();
            var before=objects.Select(t=>t.position).ToArray();
            Assert.That(view.presentationRoot.GetComponentsInChildren<Transform>(true).All(t=>!t.gameObject.isStatic),Is.True,
                "Moving teaching geometry must not enter build-time static batches.");
            for(int repeat=0;repeat<3;repeat++)
            {
                Click(TreeAction.Seated);
                for(int i=0;i<objects.Length;i++) Assert.That(Vector3.Distance(objects[i].position,before[i]+Vector3.down*.4f),Is.LessThan(.001f),objects[i].name);
                Click(TreeAction.Seated);
                for(int i=0;i<objects.Length;i++) Assert.That(Vector3.Distance(objects[i].position,before[i]),Is.LessThan(.001f),objects[i].name);
            }
            Assert.That(camera.transform.position,Is.EqualTo(cameraPosition));
            Assert.That(view.nodeViews.Max(n=>n.transform.position.y),Is.LessThan(3f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator InspectionAndProfilePreviewExplainWithoutChangingTheRoute()
        {
            Click(TreeAction.Profile);
            Assert.That(view.feedback.gameObject.activeSelf, Is.True);
            Assert.That(view.feedback.text, Does.Contain("Step or Play to begin"));
            Assert.That(view.feedback.text, Does.Contain("Previous clicks"));
            Click(TreeAction.Step); Arrive();
            var node = view.Session.State.NodeId;
            var total = view.Ensemble.RouteTotal;
            Click(TreeAction.InspectNodes);
            Assert.That(view.explanation.gameObject.activeSelf, Is.True);
            Assert.That(view.explanation.text, Does.Contain("TRUE:").And.Contain("FALSE:"));
            for (int i = 0; i < 8; i++) Click(TreeAction.NextNode);
            Assert.That(view.Session.State.NodeId, Is.EqualTo(node));
            Assert.That(view.Ensemble.RouteTotal, Is.EqualTo(total));
            Click(TreeAction.CloseInspect); Click(TreeAction.Menu);
            Click(TreeAction.Step); Arrive();
            Assert.That(view.explanation.text, Does.Contain("Leaf contribution -3.8"));
            Assert.That(view.explanation.text, Does.Contain("Previous total 0").And.Contain("New total -3.8"));
            Click(TreeAction.Restart); Click(TreeAction.Back);
            Assert.That(view.feedback.gameObject.activeSelf, Is.True);
            Assert.That(view.Session.State.Decisions.Count, Is.Zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneMapsAllSevenNodesAndMouseOrControllerEventsReachSession()
        {
            var branchText = view.presentationRoot.GetComponentsInChildren<TMPro.TMP_Text>(true)
                .Where(t => t.name == "Meaning" && t.transform.parent.name == "BranchMeaning").Select(t => t.text).ToArray();
            Assert.That(branchText, Is.EqualTo(new[] { "TRUE\nPrevious clicks < 5", "FALSE\nPrevious clicks >= 5",
                "TRUE\nVisits in 30 days < 3", "FALSE\nVisits in 30 days >= 3",
                "TRUE\nRelationship months < 100", "FALSE\nRelationship months >= 100" }));
            Assert.That(view.nodeViews.Length,Is.EqualTo(7)); Assert.That(view.nodeViews.Select(v=>v.nodeId).Distinct().Count(),Is.EqualTo(7));
            Assert.That(Object.FindObjectsByType<EventSystem>().Length,Is.EqualTo(1));
            Assert.That(view.controls.All(c=>c.GetComponent<UnityEngine.UI.Image>().raycastTarget),Is.True);
            Click(TreeAction.Manual); Click(TreeAction.TrueBranch); Arrive(); Click(TreeAction.TrueBranch); Arrive();
            Assert.That(view.Session.State.NodeId,Is.EqualTo("t1-new")); Assert.That(view.Session.State.RouteTotal,Is.EqualTo(-3.8));
            Assert.That(view.score.text,Does.Contain("Manual route score")); Assert.That(view.score.text,Does.Contain("Not the full prediction"));
            Click(TreeAction.Back); Assert.That(view.Session.State.Contribution,Is.Zero);
            Click(TreeAction.FalseBranch); Arrive(); Assert.That(view.Session.State.NodeId,Is.EqualTo("t1-returning"));
            Assert.That(view.Session.State.RouteTotal,Is.EqualTo(-3.2));
            Assert.That(Vector3.Distance(view.drop.position,view.nodeViews.Single(n=>n.nodeId=="t1-returning").dropAnchor.position),Is.LessThan(.001f));
            yield return null;
        }
        [UnityTest]
        public IEnumerator PauseFreezesDropAndBackCancelsMoveWhileCameraRemainsIndependent()
        {
            var camera=Object.FindAnyObjectByType<DesktopTreePreview>().previewCamera;
            var position=camera.transform.position; var rotation=camera.transform.rotation;
            Click(TreeAction.Manual); Click(TreeAction.TrueBranch); view.Advance(.2f);
            var moving=view.drop.position; Click(TreeAction.Pause); view.Advance(4);
            Assert.That(view.drop.position,Is.EqualTo(moving)); Assert.That(view.Session.State.NodeId,Is.EqualTo("t1-root"));
            Click(TreeAction.Back); Click(TreeAction.Pause); view.Advance(2);
            Assert.That(view.Session.State.NodeId,Is.EqualTo("t1-root")); Assert.That(view.Session.State.Contribution,Is.Zero);
            Click(TreeAction.Seated); Assert.That(camera.transform.position,Is.EqualTo(position)); Assert.That(camera.transform.rotation,Is.EqualTo(rotation));
            Assert.That(view.presentationRoot.localPosition.y,Is.EqualTo(-.4f));
            yield return null;
        }
        [UnityTest]
        public IEnumerator FourPreparedProfilesShowExpectedLeavesAndReplayWithoutDuplicatingScore()
        {
            var leaves=new[]{"t1-new","t1-returning","t1-engaged","t1-engaged"};
            // Independent first-tree contributions from data/examples/expected-predictions.json.
            var contributions=new[]{-3.8,-3.2,-2.6,-2.6};
            for(var i=0;i<4;i++)
            {
                Click(TreeAction.Profile); Assert.That(view.explanation.text,Does.Contain("->"));
                Click(TreeAction.Play);
                for(var frame=0;frame<10;frame++) view.Advance(3);
                Assert.That(view.Session.State.NodeId,Is.EqualTo(leaves[i])); Assert.That(view.Session.State.Contribution,Is.EqualTo(contributions[i]));
                Assert.That(view.Session.State.Playing,Is.False); Assert.That(view.score.text,Does.Contain("One-tree teaching subtotal"));
                Click(TreeAction.Overview); var total=view.Session.State.RouteTotal; Click(TreeAction.Overview);
                Assert.That(view.Session.State.RouteTotal,Is.EqualTo(total));
                Click(TreeAction.Restart); Click(TreeAction.Step); Arrive(); Click(TreeAction.Step); Arrive();
                Assert.That(view.Session.State.NodeId,Is.EqualTo(leaves[i])); Assert.That(view.Session.State.Contribution,Is.EqualTo(contributions[i]));
                Click(TreeAction.NextProfile);
            }
            yield return null;
        }
        [UnityTest]
        public IEnumerator VisibleControlsRemainReachableInBothModes()
        {
            var camera = Object.FindAnyObjectByType<DesktopTreePreview>().previewCamera;
            foreach (var mode in new[] { TreeAction.Manual, TreeAction.Profile, TreeAction.EditProfile })
            {
                Click(mode);
                yield return null; // Newly enabled graphics register with the canvas on the next frame.
                Canvas.ForceUpdateCanvases();
                foreach (var control in view.controls.Where(c => c.gameObject.activeInHierarchy))
                {
                    var position = camera.WorldToScreenPoint(control.transform.position);
                    Assert.That(position.z, Is.GreaterThan(0), control.name);
                    var pointer = new PointerEventData(EventSystem.current) { position = position };
                    var hits = new System.Collections.Generic.List<RaycastResult>();
                    EventSystem.current.RaycastAll(pointer, hits);
                    Assert.That(hits.Count, Is.GreaterThan(0), control.name);
                    Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject),
                        Is.EqualTo(control.gameObject), control.name + " is obscured by another UI target");
                }
            }
            yield return null;
        }
        [UnityTest]
        public IEnumerator OldPressCannotChooseInReplacementSessionOrAfterAnotherController()
        {
            Click(TreeAction.Manual);
            var button=view.controls.Single(c=>c.action==TreeAction.TrueBranch);
            var pointer=new PointerEventData(EventSystem.current){pointerId=2,button=PointerEventData.InputButton.Left};
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerDownHandler);
            Click(TreeAction.Manual,1);
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
            Assert.That(view.Session.State.PendingDecision,Is.Null);
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerDownHandler);
            Click(TreeAction.FalseBranch,1); Arrive();
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
            Assert.That(view.Session.State.NodeId,Is.EqualTo("t1-value")); Assert.That(view.Session.State.PendingDecision,Is.Null);
            yield return null;
        }
        [UnityTest]
        public IEnumerator DeepTreeTraversesEightDecisionsWithBoundedViewsAndRestoresOriginal()
        {
            var camera = Object.FindAnyObjectByType<DesktopTreePreview>().previewCamera;
            var pose = camera.transform.position;
            Click(TreeAction.DeepExample); Click(TreeAction.Profile);
            for (int depth = 0; depth < 8; depth++)
            {
                Assert.That(view.nodeViews.Count(n => n.gameObject.activeSelf), Is.LessThanOrEqualTo(7));
                Click(TreeAction.Step); Arrive();
                Assert.That(view.Session.State.Decisions.Count, Is.EqualTo(depth + 1));
            }
            Assert.That(view.Session.State.Contribution, Is.EqualTo(-2));
            var leaf = view.Session.State.NodeId;
            Click(TreeAction.TreeMap); Assert.That(view.Session.State.Overview, Is.True);
            Click(TreeAction.TreeMap); Assert.That(view.Session.State.NodeId, Is.EqualTo(leaf));
            Click(TreeAction.Back); Assert.That(view.Session.State.Contribution, Is.Zero);
            Assert.That(camera.transform.position, Is.EqualTo(pose));
            Click(TreeAction.DeepExample); Assert.That(view.DeepExampleActive, Is.False);
            Click(TreeAction.Manual); Click(TreeAction.TrueBranch); Arrive();
            Assert.That(view.Session.State.NodeId, Is.EqualTo("t1-visits"));
            yield return null;
        }
        [UnityTest]
        public IEnumerator EnsembleTourEditAndForestReturnUseTheSameSession()
        {
            Click(TreeAction.Profile);
            for (int tree = 0; tree < 3; tree++)
            {
                if (tree > 0) Click(TreeAction.NextTree);
                while (!view.Session.State.AtLeaf) { Click(TreeAction.Step); Arrive(); }
            }
            Assert.That(view.Ensemble.Complete, Is.True);
            Assert.That(view.score.text, Does.Contain("Full synthetic prediction"));
            Assert.That(view.Ensemble.RouteTotal, Is.EqualTo(-4.1).Within(1e-12));
            Click(TreeAction.Overview); var saved = view.Session.State.NodeId;
            Click(TreeAction.PreviousTree); Click(TreeAction.NextTree); Click(TreeAction.Overview);
            Assert.That(view.Session.State.NodeId, Is.EqualTo(saved));
            var original = view.Ensemble.OriginalProfile;
            Click(TreeAction.EditProfile); Click(TreeAction.IncreaseValue);
            Assert.That(view.Ensemble.CompletedCount, Is.EqualTo(3), "A draft must preserve accepted route progress until Apply.");
            Assert.That(view.Ensemble.OriginalProfile, Is.SameAs(original));
            Assert.That(view.score.text, Does.Contain("Accepted full synthetic prediction"));
            Click(TreeAction.RestoreProfile); Click(TreeAction.CloseEdit);
            Assert.That(view.Ensemble.Evaluation.RawScore, Is.EqualTo(-4.1).Within(1e-12));
            Assert.That(view.Session.State.Paused, Is.False);
            yield return null;
        }
        [UnityTest]
        public IEnumerator SelectingModeRetainsTheTreeChosenInForest()
        {
            Click(TreeAction.NextTree);
            Assert.That(view.Ensemble.Index, Is.EqualTo(1));
            Click(TreeAction.Profile);
            Assert.That(view.Ensemble.Index, Is.EqualTo(1));
            Click(TreeAction.Step); Arrive();
            Assert.That(view.Session.State.AtLeaf, Is.True);
            Click(TreeAction.PreviousTree);
            Assert.That(view.score.text, Does.Contain("Visited-tree subtotal"));
            Assert.That(view.score.text, Does.Contain("pending"));
            yield return null;
        }
        [UnityTest]
        public IEnumerator MenuHidesSecondaryControlsAndPausesWithoutLosingProgress()
        {
            Click(TreeAction.Manual); Click(TreeAction.TrueBranch); view.Advance(.2f);
            var position = view.drop.position;
            Assert.That(view.controls.Where(c => OneTreeExperience.IsSecondary(c.action)).All(c => !c.gameObject.activeSelf), Is.True);
            Click(TreeAction.Menu); view.Advance(3);
            Assert.That(view.MenuOpen, Is.True); Assert.That(view.Session.State.Paused, Is.True);
            Assert.That(view.drop.position, Is.EqualTo(position));
            yield return null;
            Canvas.ForceUpdateCanvases();
            var camera = Object.FindAnyObjectByType<DesktopTreePreview>().previewCamera;
            foreach (var control in view.controls.Where(c => OneTreeExperience.IsSecondary(c.action)))
            {
                Assert.That(control.gameObject.activeInHierarchy, Is.True);
                var pointer = new PointerEventData(EventSystem.current) { position = camera.WorldToScreenPoint(control.transform.position) };
                var hits = new System.Collections.Generic.List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
                Assert.That(hits.Count, Is.GreaterThan(0));
                Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject), Is.EqualTo(control.gameObject));
            }
            Click(TreeAction.Menu); Assert.That(view.Session.State.Paused, Is.False);
            Arrive(); Assert.That(view.Session.State.NodeId, Is.EqualTo("t1-visits"));
            Click(TreeAction.Pause); Click(TreeAction.Menu); Click(TreeAction.Menu);
            Assert.That(view.Session.State.Paused, Is.True, "Closing menu must preserve an explicit pause.");
        }
        [UnityTest]
        public IEnumerator ShortTourLedgerAndDetailedTourReachTheSameFinalResult()
        {
            Click(TreeAction.Profile); Click(TreeAction.TourDetail);
            while (!view.Session.State.AtLeaf) { Click(TreeAction.Step); Arrive(); }
            Click(TreeAction.GroupRemaining);
            Assert.That(view.Ensemble.Complete, Is.True);
            Assert.That(view.Ensemble.RouteTotal, Is.EqualTo(-4.1).Within(1e-12));
            Assert.That(view.ledgerText.gameObject.activeSelf, Is.True);
            Assert.That(view.ledgerText.text, Does.Contain("Grouped: 2 trees").And.Contain("tree-02").And.Contain("tree-03"));
            Assert.That(view.ledgerText.text, Does.Contain("sigmoid(raw score)").And.Contain("Click probability"));
            Click(TreeAction.CloseResult); Click(TreeAction.TourDetail);
            Assert.That(view.Ensemble.CompletedCount, Is.EqualTo(1));
            for (int i = 1; i < 3; i++) { Click(TreeAction.NextTree); while (!view.Session.State.AtLeaf) { Click(TreeAction.Step); Arrive(); } }
            Assert.That(view.Ensemble.RouteTotal, Is.EqualTo(-4.1).Within(1e-12));
            Click(TreeAction.Result);
            Assert.That(view.ledgerText.text, Does.Contain("FINAL RESULT").And.Contain("Complete raw score -4.1"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator DraftCancelAndApplyReplaceAllStateAndRejectDelayedInput()
        {
            Click(TreeAction.Profile); Click(TreeAction.Step); view.Advance(.2f);
            var accepted = view.Ensemble.Evaluation; var old = view.Session;
            var pending = old.State.PendingDecision;
            Click(TreeAction.EditProfile); Click(TreeAction.IncreaseValue);
            Assert.That(view.Ensemble.Evaluation, Is.SameAs(accepted));
            Assert.That(old.State.PendingDecision, Is.SameAs(pending));
            Click(TreeAction.CancelEdit); Arrive();
            Assert.That(view.Session.State.NodeId, Is.EqualTo("t1-visits"));
            Click(TreeAction.EditProfile); Click(TreeAction.IncreaseValue);
            Click(TreeAction.CloseEdit);
            Assert.That(view.Ensemble.Profile.DisplayName, Does.Contain("Hypothetical copy of New visitor"));
            Assert.That(view.Ensemble.Profile.Id, Is.Not.EqualTo(view.Ensemble.OriginalProfile.Id));
            Assert.That(view.Ensemble.CompletedCount, Is.Zero); Assert.That(view.Ensemble.Index, Is.Zero);
            Assert.That(view.Session.State.NodeId, Is.EqualTo("t1-root"));
            Assert.That(old.CompleteMove(pending.EventId).Accepted, Is.False);
            Assert.That(view.Session.State.Paused, Is.False);
            Assert.That(view.Ensemble.Evaluation.RawScore, Is.EqualTo(-4.1).Within(1e-12));
            var button = view.controls.Single(c => c.action == TreeAction.NextProfile);
            var pointer = new PointerEventData(EventSystem.current) { pointerId = -1, button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            Click(TreeAction.Profile);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Click(TreeAction.Profile);
            Assert.That(view.Ensemble.Profile.Id, Is.EqualTo("new-visitor"), "A delayed selection release must not change the next selected profile.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ProfileSwitchAtRootMoveLeafAndFinalClearsOldStateTogether()
        {
            for (int stage = 0; stage < 4; stage++)
            {
                Click(TreeAction.Profile);
                if (stage == 1) { Click(TreeAction.Step); view.Advance(.2f); }
                if (stage >= 2) while (!view.Session.State.AtLeaf) { Click(TreeAction.Step); Arrive(); }
                if (stage == 3) for (int i = 1; i < 3; i++) { Click(TreeAction.NextTree); while (!view.Session.State.AtLeaf) { Click(TreeAction.Step); Arrive(); } }
                var old = view.Session; var pending = old.State.PendingDecision;
                Click(TreeAction.NextProfile); Click(TreeAction.Profile);
                Assert.That(view.Session, Is.Not.SameAs(old));
                Assert.That(view.Ensemble.CompletedCount, Is.Zero);
                Assert.That(view.Session.State.Decisions.Count, Is.Zero);
                Assert.That(view.Session.State.PendingDecision, Is.Null);
                Assert.That(view.score.text, Does.Contain(view.Ensemble.Profile.DisplayName));
                if (pending != null) Assert.That(old.CompleteMove(pending.EventId).Accepted, Is.False);
                Click(TreeAction.Manual);
                Assert.That(view.Ensemble.Evaluation, Is.Null); Assert.That(view.score.text, Does.Not.Contain("%"));
                Assert.That(view.Ensemble.Consistency.Status, Is.EqualTo(VRExperienceAGB.Domain.RouteConsistency.Consistent));
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator LargerEnsembleManualWarningAllowsFreeExplorationAndRevision()
        {
            Click(TreeAction.LargerEnsemble);
            Assert.That(view.Model.Trees.Count, Is.EqualTo(24));
            Click(TreeAction.TrueBranch); Arrive(); Click(TreeAction.TrueBranch); Arrive();
            for (int i = 0; i < 3; i++) Click(TreeAction.NextTree);
            Click(TreeAction.FalseBranch); Arrive();
            Assert.That(view.Ensemble.Consistency.Status, Is.EqualTo(VRExperienceAGB.Domain.RouteConsistency.Contradictory));
            Assert.That(view.feedback.gameObject.activeSelf, Is.True);
            Assert.That(view.feedback.text, Does.Contain("ensemble-01/t1-root").And.Contain("ensemble-04/t1-root"));
            var total = view.Ensemble.RouteTotal;
            Click(TreeAction.ContinueFree);
            Assert.That(view.Ensemble.RouteTotal, Is.EqualTo(total));
            Assert.That(view.score.text, Does.Contain("Route score").And.Not.Contain("%"));
            Click(TreeAction.ReviseChoice);
            Assert.That(view.Ensemble.Index, Is.Zero); Assert.That(view.Session.State.NodeId, Is.EqualTo("t1-root"));
            Assert.That(view.Ensemble.Consistency.Status, Is.EqualTo(VRExperienceAGB.Domain.RouteConsistency.Consistent));
            Assert.That(view.Ensemble.RouteTotal, Is.EqualTo(.5));
            Click(TreeAction.Profile); Click(TreeAction.TourDetail);
            while (!view.Session.State.AtLeaf) { Click(TreeAction.Step); Arrive(); }
            Click(TreeAction.GroupRemaining);
            Assert.That(view.ledgerText.text, Does.Contain("Grouped: 23 trees").And.Contain("Complete raw score -32.3"));
            for (int i = 0; i < 5; i++) Click(TreeAction.NextLedgerPage);
            Assert.That(view.ledgerText.text, Does.Contain("ensemble-24"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ProfileTakeoverAndInvalidReloadNeverLeaveMisleadingOutput()
        {
            Click(TreeAction.Profile); Click(TreeAction.FalseBranch); Arrive();
            Assert.That(view.Session.State.Mode,Is.EqualTo(ExperienceMode.Manual)); Assert.That(view.status.text,Does.Contain("EXPLORE BRANCHES"));
            view.modelFile=new TextAsset("{}"); Assert.That(view.Initialize(),Is.False);
            Assert.That(view.Session,Is.Null); Assert.That(view.score.text,Is.EqualTo("No route score is available."));
            Assert.That(view.controls.All(c=>!c.GetComponent<UnityEngine.UI.Button>().interactable),Is.True);
            yield return null;
        }
    }
}
