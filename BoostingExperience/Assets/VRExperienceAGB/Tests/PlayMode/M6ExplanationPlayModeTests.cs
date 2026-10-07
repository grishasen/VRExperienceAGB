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
    public class M6ExplanationPlayModeTests
    {
        private OneTreeExperience view;
        private M6ExplanationPresentation M6=>view.Garden.M6;
        [UnitySetUp] public IEnumerator Open()
        {
            yield return LegacyTeachingScene.Load();yield return null;
            view=Object.FindAnyObjectByType<OneTreeExperience>();
            view.enabled=false;view.GetComponent<DesktopTreePreview>().enabled=false;
            view.Garden.Navigation.ShowForest();view.Garden.Navigation.OpenTree(0);view.NameTooltip.Hide();
        }
        [UnityTest] public IEnumerator HoverLinksExactPredictorsWithoutMutatingTheSession()
        {
            var node=(SplitNode)view.Session.CurrentNode;var revision=view.Session.State.Revision;double total=view.Ensemble.RouteTotal;
            var hover=view.explanation.GetComponent<NodeNameHover>();var pointer=new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(hover.gameObject,pointer,ExecuteEvents.pointerEnterHandler);
            yield return new WaitForSecondsRealtime(.5f);
            Assert.That(M6.LinkedFeature,Is.EqualTo(node.FeatureId));
            Assert.That(view.NameTooltip.Text.text,Does.Contain("Stored gain"));
            Assert.That(view.NameTooltip.Text.text,Does.Contain(node.FeatureId));
            Assert.That(view.Session.State.Revision,Is.EqualTo(revision));Assert.That(view.Ensemble.RouteTotal,Is.EqualTo(total));
            view.Garden.Navigation.ShowForest();Assert.That(M6.StoryText.text,Does.Contain("LINKED:"));
            Assert.That(view.transform.Find("ModelPineGarden").GetComponentsInChildren<Canvas>(true).Count(c=>c.name=="PredictorComposition"),Is.EqualTo(view.Model.Trees.Count));
        }
        [UnityTest] public IEnumerator BranchCardsAndPortraitFollowActualNodesIncludingFocus()
        {
            Assert.That(M6.EdgeLabels.Count(t=>t.gameObject.activeInHierarchy),Is.GreaterThan(0));
            Assert.That(M6.SegmentText.text,Does.Contain("At root"));
            view.Garden.Navigation.Refresh();
            Assert.That(view.explanation.text,Does.Contain("is less than"));
            Assert.That(view.controls.First(c=>c.action==TreeAction.TrueBranch).label.text,Does.Contain("<"));
            var s=view.Session.State;view.Execute(TreeAction.FalseBranch,s.Revision,s.NodeId);view.Advance(2);
            Assert.That(M6.SegmentText.text,Does.Contain("<="));
            var saved=view.Session.State.NodeId;double total=view.Ensemble.RouteTotal;
            string focus=view.nodeViews.First(n=>n.gameObject.activeSelf&&n.nodeId!=saved).nodeId;
            Assert.That(view.FocusedView.Focus(focus),Is.True);view.Refresh();
            Assert.That(M6.SegmentText.text,Does.Contain(focus));Assert.That(view.Session.State.NodeId,Is.EqualTo(saved));Assert.That(view.Ensemble.RouteTotal,Is.EqualTo(total));
            view.Garden.M5.OpenHelp();Assert.That(M6.SegmentText.gameObject.activeInHierarchy,Is.False);
            view.Garden.M5.CloseHelp();yield return null;
        }
        [UnityTest] public IEnumerator PreparedLeafShowsFullResultAndUndoCannotDoubleCount()
        {
            view.modelFile=new TextAsset(System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.dataPath,"VRExperienceAGB/Data/demo-model.json")));
            view.Initialize();view.Garden.Navigation.ShowForest();view.Garden.Navigation.OpenTree(0);
            var state=view.Session.State;view.Execute(TreeAction.Profile,state.Revision,state.NodeId);
            for(int safety=0; !view.Session.State.AtLeaf && safety<100; safety++){state=view.Session.State;view.Execute(TreeAction.Step,state.Revision,state.NodeId);view.Advance(2);}
            var evaluation=view.Ensemble.Evaluation;double total=view.Ensemble.RouteTotal;
            Assert.That(M6.EvidenceText.text,Does.Contain("Full profile result"));Assert.That(M6.EvidenceText.text,Does.Contain("Previous subtotal"));
            M6.Point(this,view.Session.State.NodeId);M6.Leave(this);
            Assert.That(view.Ensemble.RouteTotal,Is.EqualTo(total));Assert.That(view.Ensemble.Evaluation,Is.SameAs(evaluation));
            state=view.Session.State;view.Execute(TreeAction.Back,state.Revision,state.NodeId);
            Assert.That(M6.EvidenceText.text,Does.Not.Contain("Previous subtotal"));yield return null;
        }
        [UnityTest] public IEnumerator ReplacementClearsStaleLinksAndShowsUnavailableEvidence()
        {
            M6.LinkFeature(((SplitNode)view.Session.CurrentNode).FeatureId);
            view.modelFile=new TextAsset(System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.dataPath,"VRExperienceAGB/Data/demo-model.json")));
            view.Initialize();view.Garden.Navigation.ShowForest();view.Garden.Navigation.OpenTree(0);
            Assert.That(M6.LinkedFeature,Is.Null);Assert.That(M6.EvidenceText.text,Does.Contain("not supplied"));
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
            yield return null;
        }
    }
}
