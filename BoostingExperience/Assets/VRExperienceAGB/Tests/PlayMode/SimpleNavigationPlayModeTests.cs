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
    public class SimpleNavigationPlayModeTests
    {
        private OneTreeExperience view;
        private ForestGardenView Garden => view.Garden;
        [UnitySetUp]
        public IEnumerator Open()
        {
            yield return SceneManager.LoadSceneAsync("OneTreeLearning");yield return null;
            view=Object.FindAnyObjectByType<OneTreeExperience>();view.enabled=false;
            view.GetComponent<DesktopTreePreview>().enabled=false;
            Assert.That(view.Ready,Is.True);Assert.That(Garden.simplifiedNavigation,Is.True);
        }
        private GameObject Target(GardenCommand command,int index=0) => view.GetComponentsInChildren<GardenPointerTarget>().First(t=>t.command==command && t.index==index).gameObject;
        private void Click(GameObject target)
        {
            var p=new PointerEventData(EventSystem.current){pointerId=77,button=PointerEventData.InputButton.Left};
            ExecuteEvents.Execute(target,p,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(target,p,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(target,p,ExecuteEvents.pointerClickHandler);
        }
        private void Action(TreeAction action)
        {var s=view.Session.State;view.Execute(action,s.Revision,s.NodeId);}
        [UnityTest]
        public IEnumerator StartupUsesExactDemoAndOnlyTwoCaseChoices()
        {
            var raw=System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.dataPath,"../../data/examples/export_Mobile_Click_Through_Rate_AGB_demo.json"));
            Assert.That(view.modelFile.text,Is.EqualTo(raw));Assert.That(view.Model.Trees.Count,Is.EqualTo(50));
            Assert.That(Garden.Navigation.Page,Is.EqualTo(NavigationPage.Home));
            Assert.That(view.presentationRoot.gameObject.activeSelf,Is.False);
            Assert.That(view.transform.Find("ModelPineGarden/ModelTree-1").gameObject.activeSelf,Is.False);
            var buttons=view.GetComponentsInChildren<GardenPointerTarget>();Assert.That(buttons.Length,Is.EqualTo(2));
            CollectionAssert.AreEquivalent(new[]{GardenCommand.SingleTree,GardenCommand.Forest},buttons.Select(t=>t.command));
            Assert.That(view.Model.StructureOnlyPreview,Is.True);yield return null;
        }
        [UnityTest]
        public IEnumerator ListReachesLastTreeAndReturnsToSamePage()
        {
            Click(Target(GardenCommand.SingleTree));
            for(int i=0;i<8;i++)Click(Target(GardenCommand.ListNext));
            Click(Target(GardenCommand.SelectTree,49));
            Assert.That(view.Ensemble.Index,Is.EqualTo(49));Assert.That(view.Session.State.Overview,Is.False);
            Assert.That(Garden.Visible,Is.False);
            Action(TreeAction.Back);Assert.That(view.Ensemble.Index,Is.EqualTo(49),"Back at the root stays in this tree.");
            yield return null; // Newly enabled graphics register on the next frame.
            Canvas.ForceUpdateCanvases();
            var camera=Garden.Locomotion.Head.GetComponent<Camera>();
            foreach(var button in view.controls.Where(c=>c.gameObject.activeInHierarchy))
            {
                var pointer=new PointerEventData(EventSystem.current){position=camera.WorldToScreenPoint(button.transform.position)};
                var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
                Assert.That(hits.Count,Is.GreaterThan(0),button.action.ToString());
                Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject),Is.EqualTo(button.gameObject),button.action.ToString());
            }
            Action(TreeAction.Overview);
            Assert.That(Garden.Navigation.Page,Is.EqualTo(NavigationPage.TreeList));Assert.That(Target(GardenCommand.SelectTree,49),Is.Not.Null);
            Assert.That(view.Ensemble.CompletedCount,Is.Zero);yield return null;
        }
        [UnityTest]
        public IEnumerator ForestPointerHoverAndRoundTripPreserveRouteAndPose()
        {
            Click(Target(GardenCommand.Forest));
            for(int group=0;group<5;group++){Click(Target(GardenCommand.Waypoint,group));yield return new WaitForSecondsRealtime(.2f);}
            Click(Target(GardenCommand.Path,40));yield return null;
            var origin=Garden.Locomotion.Origin.position;var local=Garden.Locomotion.Head.localPosition;var rotation=Garden.Locomotion.Head.localRotation;
            var pine=Target(GardenCommand.SelectTree,40);var p=new PointerEventData(EventSystem.current){pointerId=77};
            var revision=view.Session.State.Revision;ExecuteEvents.Execute(pine,p,ExecuteEvents.pointerEnterHandler);
            Assert.That(view.Session.State.Revision,Is.EqualTo(revision));Assert.That(view.Ensemble.RouteTotal,Is.Zero);
            var plaque=view.transform.Find("ModelPineGarden/TreePlaque-41").GetComponentsInChildren<TMPro.TMP_Text>();
            Assert.That(plaque.Any(t=>t.text.Contains("Click to explore")),Is.True);
            Click(pine);Assert.That(view.Ensemble.Index,Is.EqualTo(40));Assert.That(Garden.Navigation.Page,Is.EqualTo(NavigationPage.Tree));
            while(!view.Session.State.AtLeaf){Action(TreeAction.TrueBranch);view.Advance(2);}
            var leaf=view.Session.State.NodeId;var total=view.Ensemble.RouteTotal;
            Action(TreeAction.Overview);
            Assert.That(Garden.Navigation.Page,Is.EqualTo(NavigationPage.Forest));
            Assert.That(Vector3.Distance(origin,Garden.Locomotion.Origin.position),Is.LessThan(.001f));
            Assert.That(Garden.Locomotion.Head.localPosition,Is.EqualTo(local));Assert.That(Garden.Locomotion.Head.localRotation,Is.EqualTo(rotation));
            Click(Target(GardenCommand.SelectTree,40));Assert.That(view.Session.State.NodeId,Is.EqualTo(leaf));Assert.That(view.Ensemble.RouteTotal,Is.EqualTo(total));
            Assert.That(view.controls.Where(c=>c.gameObject.activeInHierarchy).All(c=>new[]{TreeAction.Back,TreeAction.Restart,TreeAction.Overview,TreeAction.Menu}.Contains(c.action)),Is.True);
            yield return null;
        }
        [UnityTest]
        public IEnumerator TreeSubmenuRestoresPostureWithoutChangingTrackingOrRoute()
        {
            Click(Target(GardenCommand.SingleTree));Click(Target(GardenCommand.SelectTree,0));
            var head=Garden.Locomotion.Head;var local=head.localPosition;var rotation=head.localRotation;
            Action(TreeAction.TrueBranch);view.Advance(.2f);var pending=view.Session.State.PendingDecision;
            Click(view.controls.Single(c=>c.action==TreeAction.Menu).gameObject);
            Assert.That(Garden.Navigation.TreeMenuOpen,Is.True);Assert.That(view.Session.State.Paused,Is.True);
            view.Advance(2);Assert.That(view.Session.State.PendingDecision,Is.EqualTo(pending));
            var standing=view.presentationRoot.localPosition;
            Click(view.controls.Single(c=>c.action==TreeAction.Seated).gameObject);
            Assert.That(view.IsSeated,Is.True);Assert.That(view.presentationRoot.localPosition.y,Is.EqualTo(standing.y-.4f).Within(.001f));
            Assert.That(head.localPosition,Is.EqualTo(local));Assert.That(head.localRotation,Is.EqualTo(rotation));
            Assert.That(view.presentationRoot.Find("ConsoleStone").GetComponent<Renderer>().bounds.min.y,Is.GreaterThanOrEqualTo(0));
            Click(view.controls.Single(c=>c.action==TreeAction.Seated).gameObject);Assert.That(view.IsSeated,Is.False);
            Click(view.controls.Single(c=>c.action==TreeAction.Menu).gameObject);Assert.That(view.Session.State.Paused,Is.False);
            view.Advance(2);Assert.That(view.Session.State.Decisions.Count,Is.EqualTo(1));
            yield return null;
        }
        [UnityTest]
        public IEnumerator CompactControlsAndSubmenuAreReachableInBothPostures()
        {
            Click(Target(GardenCommand.SingleTree));Click(Target(GardenCommand.SelectTree,0));
            var camera=Garden.Locomotion.Head.GetComponent<Camera>();
            foreach(bool seated in new[]{false,true})
            {
                if(seated) { Action(TreeAction.Menu);Action(TreeAction.Seated);Action(TreeAction.Menu); }
                foreach(bool menu in new[]{false,true})
                {
                    if(menu)Action(TreeAction.Menu);
                    yield return null;Canvas.ForceUpdateCanvases();
                    var active=view.controls.Where(c=>c.gameObject.activeInHierarchy).ToArray();Assert.That(active.Length,Is.EqualTo(4));
                    foreach(var button in active)
                    {
                        var pointer=new PointerEventData(EventSystem.current){position=camera.WorldToScreenPoint(button.transform.position)};
                        var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
                        Assert.That(hits.Count,Is.GreaterThan(0),button.action.ToString());
                        Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject),Is.EqualTo(button.gameObject),button.action.ToString());
                    }
                    if(menu)Action(TreeAction.Menu);
                }
            }
            Assert.That(view.nodeViews.Max(n=>n.transform.localPosition.y),Is.LessThanOrEqualTo(1.85f));
            Assert.That(view.nodeViews.Skip(1).All(n=>n.title.rectTransform.sizeDelta.x<=300),Is.True);
        }
        [UnityTest]
        public IEnumerator CaseAndListButtonsAreReachableAndPageChangesRejectStalePresses()
        {
            Canvas.ForceUpdateCanvases();var camera=Garden.Locomotion.Head.GetComponent<Camera>();
            foreach(var target in view.GetComponentsInChildren<GardenPointerTarget>())
            {
                var p=new PointerEventData(EventSystem.current){position=camera.WorldToScreenPoint(target.transform.position)};
                var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(p,hits);
                Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject),Is.EqualTo(target.gameObject));
            }
            Click(Target(GardenCommand.SingleTree));
            var row=Target(GardenCommand.SelectTree);var press=new PointerEventData(EventSystem.current){pointerId=77,button=PointerEventData.InputButton.Left};
            ExecuteEvents.Execute(row,press,ExecuteEvents.pointerDownHandler);Click(Target(GardenCommand.ListNext));ExecuteEvents.Execute(row,press,ExecuteEvents.pointerClickHandler);
            Assert.That(Garden.Navigation.Page,Is.EqualTo(NavigationPage.TreeList));Assert.That(view.Ensemble.Index,Is.Zero);
            yield return null;
        }
    }
}
