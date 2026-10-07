using System.Collections;
using System.Collections.Generic;
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
    public class M5ForestPlayModeTests
    {
        private OneTreeExperience view;
        private ForestGardenView Garden => view.Garden;
        private M5ForestPresentation M5 => Garden.M5;
        [UnitySetUp]
        public IEnumerator Open()
        {
            yield return LegacyTeachingScene.Load(); yield return null;
            view = Object.FindAnyObjectByType<OneTreeExperience>();
             view.enabled = false;
            view.GetComponent<DesktopTreePreview>().enabled = false;
        }
        private void Action(TreeAction action) { var s = view.Session.State; view.Execute(action, s.Revision, s.NodeId); }
        private void Click(M5PointerTarget target)
        {
            var pointer = new PointerEventData(EventSystem.current) { pointerId = 103, button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(target.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }
        [UnityTest]
        public IEnumerator TablePagesRetainEveryTreeAndTheCompleteModelWhenTheExportIsLarger()
        {
            const string tree = "{\"score\":0,\"gain\":1,\"sampleCount\":10,\"split\":\"Example < 5\",\"left\":{\"score\":-0.25,\"gain\":0,\"sampleCount\":5},\"right\":{\"score\":0.75,\"gain\":0,\"sampleCount\":5}}";
            string export = "{\"type\":\"AdaptiveBoostScoringModel\",\"algorithm\":\"GRADIENT_BOOST\",\"model\":{\"booster\":{\"trees\":[" + string.Join(",", Enumerable.Repeat(tree, 113)) + "]}}}";
            Assert.That(view.LoadAgbStructure(export, "Synthetic 113-tree view check").IsSuccess, Is.True);
            Garden.Navigation.ShowForest(); M5.Activate(M5Action.Diorama);
            Assert.That(M5.Diorama.ModelRoot.GetComponentsInChildren<M5PointerTarget>().Length, Is.EqualTo(113));
            M5.Activate(M5Action.NextTablePage); Assert.That(view.Ensemble.Index, Is.EqualTo(50));
            M5.Activate(M5Action.NextTablePage); Assert.That(view.Ensemble.Index, Is.EqualTo(100));
            Assert.That(M5.Diorama.ModelRoot.GetComponentsInChildren<M5PointerTarget>().Length, Is.EqualTo(113));
            M5.Activate(M5Action.SelectTree, 112); M5.Activate(M5Action.EnterTree);
            for(int safety=0; !view.Session.State.AtLeaf && safety<100; safety++) { Action(TreeAction.TrueBranch); view.Advance(2); }
            double total = view.Ensemble.RouteTotal; Action(TreeAction.Overview); M5.Activate(M5Action.PreviousTablePage);
            Assert.That(view.Model.Trees.Count, Is.EqualTo(113)); Assert.That(view.Ensemble.Trees.Count, Is.EqualTo(113));
            Assert.That(view.Ensemble.RouteTotal, Is.EqualTo(total)); Assert.That(view.Ensemble.CompletedCount, Is.EqualTo(1));
            yield return null;
        }
        [UnityTest]
        public IEnumerator PreparedProfilesKeepTheirIdentityPauseAndFullResultAcrossAllM5Views()
        {
            view.modelFile = new TextAsset(System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.dataPath, "VRExperienceAGB/Data/demo-model.json")));
            view.Initialize(); Garden.Navigation.ShowForest(); Garden.Navigation.OpenTree(0); Action(TreeAction.Profile);
            var profile = view.Ensemble.Profile; var evaluation = view.Ensemble.Evaluation;
            for (int i = 0; i < 3; i++)
            {
                foreach (bool atLeaf in new[] { false, true })
                {
                    if (atLeaf) for(int safety=0; !view.Session.State.AtLeaf && safety<100; safety++) { Action(TreeAction.Step); view.Advance(2); }
                    var node = view.Session.State.NodeId; double total = view.Ensemble.RouteTotal;
                    view.Session.SetPaused(true); Action(TreeAction.Overview); M5.Activate(M5Action.Diorama);
                    M5.Activate(M5Action.Larger); M5.Activate(M5Action.RotateLeft); M5.OpenHelp(); M5.CloseHelp();
                    M5.Activate(M5Action.EnterTree);
                    Assert.That(view.Ensemble.Profile, Is.SameAs(profile)); Assert.That(view.Ensemble.Evaluation, Is.SameAs(evaluation));
                    Assert.That(view.Session.State.NodeId, Is.EqualTo(node)); Assert.That(view.Ensemble.RouteTotal, Is.EqualTo(total));
                    Assert.That(view.Session.State.Paused, Is.True); view.Session.SetPaused(false);
                }
                if (i < 2) { Action(TreeAction.Overview); M5.Activate(M5Action.NextTree); M5.Activate(M5Action.EnterTree); }
            }
            Assert.That(view.Ensemble.CompletedCount, Is.EqualTo(3)); Assert.That(view.Ensemble.RouteTotal, Is.EqualTo(evaluation.RawScore));
            yield return null;
        }
        [UnityTest]
        public IEnumerator StickWalkingMovesTheOriginWithinTheGardenAndPreservesTrackingAndScores()
        {
            Garden.Navigation.ShowForest(); var movement = Garden.Locomotion;
            var head = movement.Head; var position = head.localPosition; var rotation = head.localRotation;
            var origin = movement.Origin.position; var revision = view.Session.State.Revision;
            movement.Move(new Vector2(.1f, .1f), .1f); Assert.That(movement.Origin.position, Is.EqualTo(origin));
            movement.Move(Vector2.up, .1f); Assert.That(Vector3.Distance(origin, movement.Origin.position), Is.EqualTo(.135f).Within(.001f));
            for (int i = 0; i < 800; i++) movement.Move(Vector2.right, .1f);
            Assert.That(head.position.x, Is.InRange(-20, 20));
            Assert.That(head.localPosition, Is.EqualTo(position)); Assert.That(head.localRotation, Is.EqualTo(rotation));
            Assert.That(view.Ensemble.RouteTotal, Is.Zero); Assert.That(view.Session.State.Revision, Is.EqualTo(revision));
            origin = movement.Origin.position; M5.OpenHelp(); movement.Move(Vector2.up, .1f);
            Assert.That(movement.Origin.position, Is.EqualTo(origin)); M5.CloseHelp();
            M5.Activate(M5Action.Diorama); origin = movement.Origin.position; movement.Move(Vector2.up, .1f);
            Assert.That(movement.Origin.position, Is.EqualTo(origin));
            yield return null;
        }
        [UnityTest]
        public IEnumerator CrownTargetsAreReachableFromFrontSideAndRearWithoutUsingTheName()
        {
            Garden.Navigation.ShowForest();
            var plot = view.transform.Find("ModelPineGarden/ModelTree-1");
            var camera = Garden.Locomotion.Head.GetComponent<Camera>();
            var pine = plot.Find("Pine").GetComponent<Renderer>();
            foreach (var offset in new[] { Vector3.back * 2.5f, Vector3.right * 2.5f, Vector3.forward * 2.5f })
            {
                Garden.Locomotion.Teleport(plot.position + offset);
                // Only the explicit desktop test observer is aimed; tracked headset transforms are never scripted.
                camera.transform.LookAt(pine.bounds.center + Vector3.up * .15f);
                yield return new WaitForSecondsRealtime(.2f); Canvas.ForceUpdateCanvases();
                var pointer = new PointerEventData(EventSystem.current) { position = camera.WorldToScreenPoint(pine.bounds.center + Vector3.up * .15f) };
                var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
                Assert.That(hits.Count, Is.GreaterThan(0));
                var target = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject).GetComponent<GardenPointerTarget>();
                Assert.That(target, Is.Not.Null); Assert.That(target.name, Does.StartWith("PinePointerTarget"));
                Assert.That(target.index, Is.Zero);
                ExecuteEvents.Execute(target.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
                Assert.That(view.Ensemble.CompletedCount, Is.Zero, "Hover cannot choose a route.");
                var card = view.transform.Find("ModelPineGarden/TreePlaque-1/HoverBacking");
                Assert.That(Vector3.Dot(card.forward, (plot.Find("PineCrownTarget-1").position - camera.transform.position).normalized), Is.GreaterThan(.7f), "Hover details face the observer from each approach.");
            }
        }
        [UnityTest]
        public IEnumerator TabletopBoundsRotationSelectionAndReturnKeepTheSameRouteAndPose()
        {
            Garden.Navigation.ShowForest(); Garden.Navigation.OpenTree(40);
            for(int safety=0; !view.Session.State.AtLeaf && safety<100; safety++) { Action(TreeAction.TrueBranch); view.Advance(2); }
            view.Session.SetPaused(true); var leaf = view.Session.State.NodeId; var total = view.Ensemble.RouteTotal;
            Action(TreeAction.Overview); M5.Activate(M5Action.Diorama);
            var head = Garden.Locomotion.Head; var position = head.localPosition; var rotation = head.localRotation;
            var origin = Garden.Locomotion.Origin.position;
            for (int i = 0; i < 30; i++) M5.Activate(M5Action.Larger);
            Assert.That(M5.Diorama.Scale, Is.EqualTo(ForestDioramaView.MaximumScale));
            for (int i = 0; i < 30; i++) M5.Activate(M5Action.Smaller);
            Assert.That(M5.Diorama.Scale, Is.EqualTo(ForestDioramaView.MinimumScale));
            M5.Activate(M5Action.RotateRight); Assert.That(M5.Diorama.Yaw, Is.EqualTo(30));
            Assert.That(M5.Diorama.ModelRoot.childCount, Is.EqualTo(51), "All 50 trees plus the table.");
            M5.Activate(M5Action.SelectTree, 49); Assert.That(view.Ensemble.Index, Is.EqualTo(49));
            M5.Activate(M5Action.SelectTree, 40); M5.Activate(M5Action.EnterTree);
            Assert.That(view.Session.State.NodeId, Is.EqualTo(leaf)); Assert.That(view.Ensemble.RouteTotal, Is.EqualTo(total));
            Assert.That(view.Session.State.Paused, Is.True);
            Action(TreeAction.Menu);
            Assert.That(view.controls.Single(c => c.action == TreeAction.Overview).label.text, Is.EqualTo("Back to tabletop"));
            Action(TreeAction.Menu);
            Action(TreeAction.Overview); Assert.That(Garden.Navigation.Page, Is.EqualTo(NavigationPage.Diorama));
            Assert.That(M5.Diorama.Scale, Is.EqualTo(ForestDioramaView.MinimumScale)); Assert.That(M5.Diorama.Yaw, Is.EqualTo(30));
            Assert.That(head.localPosition, Is.EqualTo(position)); Assert.That(head.localRotation, Is.EqualTo(rotation));
            Assert.That(Garden.Locomotion.Origin.position, Is.EqualTo(origin));
            M5.Activate(M5Action.ResetView); Assert.That(M5.Diorama.Scale, Is.EqualTo(1)); Assert.That(M5.Diorama.Yaw, Is.Zero);
            yield return null;
        }
        [UnityTest]
        public IEnumerator DeepSubtreeInspectionPreservesTheCompletePreparedResultAndDecision()
        {
            view.modelFile = new TextAsset(System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.dataPath, "VRExperienceAGB/Data/demo-model.json")));
            view.Initialize(); Action(TreeAction.DeepExample); Garden.Navigation.ShowForest(); Garden.Navigation.OpenTree(0);
            Action(TreeAction.Profile); Action(TreeAction.Step); view.Advance(2);
            var route = view.Session.State; var result = view.Ensemble.Evaluation;
            var boundary = view.nodeViews.Skip(3).First(n => n.gameObject.activeSelf);
            var target = boundary.title.GetComponentInParent<Canvas>().GetComponentsInChildren<M5PointerTarget>().Single(t => t.action == M5Action.FocusNode);
            Assert.That(target.GetComponentInChildren<TMPro.TMP_Text>().text, Does.Contain("62 hidden"));
            string selectedNode = target.nodeId;
            Click(target); Assert.That(view.FocusedView.FocusRoot, Is.EqualTo(selectedNode));
            Assert.That(view.FocusedView.FocusDepth, Is.EqualTo(1)); view.Advance(100);
            Assert.That(view.Session.State.NodeId, Is.EqualTo(route.NodeId)); Assert.That(view.Session.State.Revision, Is.EqualTo(route.Revision));
            Assert.That(view.Ensemble.Evaluation, Is.SameAs(result)); Assert.That(view.Ensemble.CompletedCount, Is.Zero);
            Assert.That(view.drop.gameObject.activeSelf, Is.False);
            M5.Activate(M5Action.FocusParent); M5.Activate(M5Action.FocusCurrent);
            Assert.That(view.FocusedView.FocusRoot, Is.Null);
            for (int i = 1; i < 8; i++) { Action(TreeAction.Step); view.Advance(2); }
            Assert.That(view.Ensemble.RouteTotal, Is.EqualTo(result.RawScore));
            yield return null;
        }
        [UnityTest]
        public IEnumerator HelpAndExplicitPausePreserveMovementAndMenuState()
        {
            Garden.Navigation.ShowForest(); Garden.Navigation.OpenTree(0);
            Action(TreeAction.TrueBranch); view.Advance(.2f); var pending = view.Session.State.PendingDecision;
            M5.OpenHelp(); for (int i = 0; i < 3; i++) M5.Activate(M5Action.HelpNext);
            Assert.That(M5.HelpPage, Is.EqualTo(3)); view.Advance(20);
            Assert.That(view.Session.State.PendingDecision, Is.SameAs(pending));
            M5.CloseHelp(); Assert.That(view.Session.State.Paused, Is.False); view.Advance(2);
            Assert.That(view.Session.State.Decisions.Count, Is.EqualTo(1));
            Action(TreeAction.Menu); M5.Activate(M5Action.Pause); M5.OpenHelp(); M5.CloseHelp(); Action(TreeAction.Menu);
            Assert.That(view.Session.State.Paused, Is.True); Action(TreeAction.Overview);
            Garden.Navigation.OpenTree(0); Assert.That(view.Session.State.Paused, Is.True);
            Action(TreeAction.Menu); M5.Activate(M5Action.Pause); Action(TreeAction.Menu); Assert.That(view.Session.State.Paused, Is.False);
            yield return null;
        }
        [UnityTest]
        public IEnumerator TabletopSelectionRejectsAReleaseAfterRotationOrModelReplacement()
        {
            Garden.Navigation.ShowForest(); M5.Activate(M5Action.Diorama);
            var target = M5.Diorama.ModelRoot.GetComponentsInChildren<M5PointerTarget>().First(t => t.index == 49);
            var pointer = new PointerEventData(EventSystem.current) { pointerId = 103, button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(target.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            M5.Activate(M5Action.RotateRight); ExecuteEvents.Execute(target.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(view.Ensemble.Index, Is.Zero);
            ExecuteEvents.Execute(target.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            view.LoadAgbStructure(System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.dataPath, "../../data/examples/demo-agb-export.json")), "Synthetic replacement");
            ExecuteEvents.Execute(target.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(view.Model.Trees.Count, Is.EqualTo(3)); Assert.That(view.Ensemble.Index, Is.Zero);
            yield return null;
        }
        [UnityTest]
        public IEnumerator TableAndHelpControlsAreReachableWithoutOverlappingTheTreeTargets()
        {
            Garden.Navigation.ShowForest(); M5.Activate(M5Action.Diorama); yield return null; yield return null; Canvas.ForceUpdateCanvases();
            var camera = Garden.Locomotion.Head.GetComponent<Camera>();
            var targets = M5.GetComponentsInChildren<M5PointerTarget>().ToArray();
            foreach (var target in targets)
            {
                camera.transform.LookAt(target.transform.position);yield return null;Canvas.ForceUpdateCanvases();
                var screen = camera.WorldToViewportPoint(target.transform.position);
                Assert.That(screen.x, Is.InRange(.01f, .99f), target.name); Assert.That(screen.y, Is.InRange(.01f, .99f), target.name);
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = camera.WorldToScreenPoint(target.transform.position) }, hits);
                Assert.That(hits.Count, Is.GreaterThan(0), target.name);
                Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject), Is.EqualTo(target.gameObject), target.name);
            }
            M5.OpenHelp(); yield return null; Canvas.ForceUpdateCanvases();
            foreach (var target in M5.GetComponentsInChildren<M5PointerTarget>().Where(t => t.action == M5Action.CloseHelp || t.action == M5Action.HelpNext))
            {
                var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = camera.WorldToScreenPoint(target.transform.position) }, hits);
                Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject), Is.EqualTo(target.gameObject));
            }
            Assert.That(Object.FindObjectsByType<EventSystem>().Length, Is.EqualTo(1));
            M5.Activate(M5Action.CloseHelp); M5.Activate(M5Action.ToggleAudio); Assert.That(M5.Atmosphere.SoundEnabled, Is.False);
        }
    }
}
