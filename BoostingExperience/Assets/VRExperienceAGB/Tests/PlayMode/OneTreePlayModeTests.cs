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
            var button=view.controls.Single(c=>c.action==action);
            var pointer=new PointerEventData(EventSystem.current) { pointerId=pointerId,button=PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        }
        private void Arrive() { view.Advance(2); }
        [UnityTest]
        public IEnumerator SceneMapsAllSevenNodesAndMouseOrControllerEventsReachSession()
        {
            Assert.That(view.nodeViews.Length,Is.EqualTo(7)); Assert.That(view.nodeViews.Select(v=>v.nodeId).Distinct().Count(),Is.EqualTo(7));
            Assert.That(Object.FindObjectsByType<EventSystem>().Length,Is.EqualTo(1));
            Assert.That(view.controls.All(c=>c.GetComponent<UnityEngine.UI.Image>().raycastTarget),Is.True);
            Click(TreeAction.Manual); Click(TreeAction.TrueBranch); Arrive(); Click(TreeAction.TrueBranch); Arrive();
            Assert.That(view.Session.State.NodeId,Is.EqualTo("t1-new")); Assert.That(view.Session.State.RouteTotal,Is.EqualTo(-1.9));
            Assert.That(view.score.text,Does.Contain("Manual route score")); Assert.That(view.score.text,Does.Contain("Not the full prediction"));
            Click(TreeAction.Back); Assert.That(view.Session.State.Contribution,Is.Zero);
            Click(TreeAction.FalseBranch); Arrive(); Assert.That(view.Session.State.NodeId,Is.EqualTo("t1-returning"));
            Assert.That(view.Session.State.RouteTotal,Is.EqualTo(-1.3));
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
            var contributions=new[]{-.4,.2,.8,.8};
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
