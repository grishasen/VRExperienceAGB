using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRExperienceAGB.Presentation;

namespace VRExperienceAGB.Tests
{
    public class ForestGardenPlayModeTests
    {
        private OneTreeExperience view;
        [UnitySetUp]
        public IEnumerator Open()
        {
            yield return SceneManager.LoadSceneAsync("OneTreeLearning");yield return null;
            view=Object.FindAnyObjectByType<OneTreeExperience>();view.enabled=false;
            Assert.That(view.Ready,Is.True);Assert.That(view.Garden.Visible,Is.True);
        }
        private void PointerClick(GameObject target)
        {
            var pointer=new PointerEventData(EventSystem.current){pointerId=91,button=PointerEventData.InputButton.Left};
            ExecuteEvents.Execute(target,pointer,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target,pointer,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target,pointer,ExecuteEvents.pointerClickHandler);
        }
        private void Action(TreeAction action)
        { var state=view.Session.State;view.Execute(action,state.Revision,state.NodeId); }
        [UnityTest]
        public IEnumerator PointingAtThePineItselfSelectsItsTree()
        {
            view.Garden.ToggleGuide();yield return null;Canvas.ForceUpdateCanvases();
            var pine=view.transform.Find("ModelPineGarden/ModelTree-1/Pine").GetComponent<Renderer>();
            var camera=view.Garden.Locomotion.Head.GetComponent<Camera>();
            var pointer=new PointerEventData(EventSystem.current){position=camera.WorldToScreenPoint(pine.bounds.center)};
            var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            Assert.That(hits.Count,Is.GreaterThan(0));
            var target=hits[0].gameObject.GetComponent<GardenPointerTarget>();Assert.That(target,Is.Not.Null);
            Assert.That(target.command,Is.EqualTo(GardenCommand.SelectTree));Assert.That(target.index,Is.Zero);
            PointerClick(target.gameObject);Assert.That(view.Ensemble.Index,Is.Zero);
            Assert.That(view.Session.State.Overview,Is.True);
        }
        [UnityTest]
        public IEnumerator FieldGuideButtonsRemainReachableByPointer()
        {
            yield return null;Canvas.ForceUpdateCanvases();
            var camera=view.Garden.Locomotion.Head.GetComponent<Camera>();
            var canvas=view.Garden.EnterButton.GetComponentInParent<Canvas>();
            foreach(var target in canvas.GetComponentsInChildren<GardenPointerTarget>())
            {
                var position=camera.WorldToScreenPoint(target.transform.position);
                var pointer=new PointerEventData(EventSystem.current){position=position};
                var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
                Assert.That(hits.Count,Is.GreaterThan(0),target.name);
                Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject),Is.EqualTo(target.gameObject),target.name);
            }
        }
        [UnityTest]
        public IEnumerator GardenShowsStructureAndSelectsARealTreeWithoutStartingARoute()
        {
            Assert.That(view.Garden.PlotCount,Is.EqualTo(view.Model.Trees.Count));
            Assert.That(view.presentationRoot.gameObject.activeSelf,Is.False);
            view.Garden.ToggleGuide();Assert.That(view.Garden.GuideOpen,Is.False);
            Assert.That(view.Garden.PlotCount,Is.EqualTo(view.Model.Trees.Count));
            view.Garden.ToggleGuide();Assert.That(view.Garden.GuideOpen,Is.True);
            Assert.That(view.Garden.Details.text,Does.Contain("Max depth 2").And.Contain("4 leaves").And.Contain("Unvisited"));
            PointerClick(view.Garden.NextButton);
            Assert.That(view.Ensemble.Index,Is.EqualTo(1));Assert.That(view.Session.State.Overview,Is.True);
            Assert.That(view.Ensemble.RouteTotal,Is.Zero);Assert.That(view.Garden.Details.text,Does.Contain("TREE 2"));
            PointerClick(view.Garden.EnterButton);
            Assert.That(view.Garden.Visible,Is.False);Assert.That(view.presentationRoot.gameObject.activeSelf,Is.True);
            Assert.That(view.Session.Tree.Id,Is.EqualTo(view.Model.Trees[1].Id));
            yield return null;
        }
        [UnityTest]
        public IEnumerator TeleportSnapAndRoundTripPreserveTrackingAndRoute()
        {
            var movement=view.Garden.Locomotion;var localPosition=movement.Head.localPosition;var localRotation=movement.Head.localRotation;
            view.Garden.Activate(GardenCommand.Visit);
            var standing=view.Garden.StandingPoint(0);var head=movement.Head.position;
            Assert.That(Vector2.Distance(new Vector2(head.x,head.z),new Vector2(standing.x,standing.z)),Is.LessThan(.001f));
            var beforeTurn=movement.Head.position;movement.SnapTurn(30);
            Assert.That(Vector3.Distance(movement.Head.position,beforeTurn),Is.LessThan(.001f));
            Assert.That(movement.Head.localPosition,Is.EqualTo(localPosition));Assert.That(movement.Head.localRotation,Is.EqualTo(localRotation));
            var gardenPosition=movement.Origin.position;var gardenRotation=movement.Origin.rotation;
            PointerClick(view.Garden.EnterButton);
            view.Session.SetPaused(false);Action(TreeAction.TrueBranch);view.Advance(2);Action(TreeAction.TrueBranch);view.Advance(2);
            var node=view.Session.State.NodeId;var total=view.Ensemble.RouteTotal;
            Action(TreeAction.Overview);
            Assert.That(view.Garden.Visible,Is.True);Assert.That(Vector3.Distance(movement.Origin.position,gardenPosition),Is.LessThan(.001f));
            Assert.That(Quaternion.Angle(movement.Origin.rotation,gardenRotation),Is.LessThan(.001f));
            Assert.That(view.Garden.Details.text,Does.Contain("Leaf reached"));
            PointerClick(view.Garden.EnterButton);
            Assert.That(view.Session.State.NodeId,Is.EqualTo(node));Assert.That(view.Ensemble.RouteTotal,Is.EqualTo(total));
            Assert.That(movement.Head.localPosition,Is.EqualTo(localPosition));Assert.That(movement.Head.localRotation,Is.EqualTo(localRotation));
            yield return null;
        }
        [UnityTest]
        public IEnumerator BedNavigationRetainsAllTreesAndRejectsAnEarlierPress()
        {
            view.Garden.EnterSelected();Action(TreeAction.LargerEnsemble);Action(TreeAction.Overview);
            Assert.That(view.Garden.PlotCount,Is.EqualTo(24));
            var stale=view.Garden.NextButton;
            var pointer=new PointerEventData(EventSystem.current){pointerId=92,button=PointerEventData.InputButton.Left};
            ExecuteEvents.Execute(stale,pointer,ExecuteEvents.pointerDownHandler);
            view.Garden.Activate(GardenCommand.NextBed);
            Assert.That(view.Ensemble.Index,Is.EqualTo(10));
            ExecuteEvents.Execute(stale,pointer,ExecuteEvents.pointerClickHandler);
            Assert.That(view.Ensemble.Index,Is.EqualTo(10),"A press from before selection changed is stale.");
            view.Garden.Activate(GardenCommand.NextBed);Assert.That(view.Ensemble.Index,Is.EqualTo(20));
            view.Garden.Activate(GardenCommand.SelectTree,23);Assert.That(view.Ensemble.Index,Is.EqualTo(23));
            Assert.That(view.Ensemble.Trees.Count,Is.EqualTo(24));Assert.That(view.Ensemble.CompletedCount,Is.Zero);
            yield return null;
        }
    }
}
