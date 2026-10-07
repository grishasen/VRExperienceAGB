using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using VRExperienceAGB.Application;
using VRExperienceAGB.Presentation;
using Object=UnityEngine.Object;

namespace VRExperienceAGB.Tests
{
    public class ScenarioRevisionTests
    {
        [Serializable] private class ReferenceFile { public ReferenceProfile[] results; }
        [Serializable] private class ReferenceProfile { public double score, probability; public ReferenceTree[] trees; }
        [Serializable] private class ReferenceTree { public string leaf; }
        private OneTreeExperience view;
        [UnitySetUp] public IEnumerator Open()
        {
            if(Resources.Load<TextAsset>("LocalModel/expected")==null)Assert.Ignore("Prepare the local sample with scripts/prepare-local-sample.py.");
            yield return SceneManager.LoadSceneAsync("OneTreeLearning");yield return null;
            view=Object.FindAnyObjectByType<OneTreeExperience>();Assert.That(view.Ready,Is.True,view.explanation.text);
            view.enabled=false;view.Director.enabled=false;
            view.SendMessage("OnApplicationFocus",true);view.SendMessage("OnApplicationPause",false);
            view.Director.CloseMenu();
        }
        [UnityTest] public IEnumerator SampleProfilesMatchIndependentReferenceForEveryLeaf()
        {
            Assert.That(view.Model.Trees.Count,Is.EqualTo(100));
            var expected=JsonUtility.FromJson<ReferenceFile>(Resources.Load<TextAsset>("LocalModel/expected").text);
            for(int p=0;p<3;p++) {
                view.StartScenarioProfiles(p);
                var result=view.Ensemble.Evaluation;var reference=expected.results[p];
                Assert.That(result.RawScore,Is.EqualTo(reference.score).Within(1e-10));
                Assert.That(result.Probability,Is.EqualTo(reference.probability).Within(1e-12));
                for(int i=0;i<100;i++)Assert.That(result.Trees[i].LeafId,Is.EqualTo(reference.trees[i].leaf));
            }
            yield return null;
        }
        [UnityTest] public IEnumerator FullPairVisitsEveryTreeAndKeepsResultsAcrossViews()
        {
            var d=view.Director;d.StartPlayback(true);int guard=0,last=-1,visited=0;
            while(!d.Playback.Complete&&guard++<10000){
                if(last!=d.Playback.TreeIndex){visited++;last=d.Playback.TreeIndex;}
                d.AdvanceTour(100);if(guard%80==0)yield return null;
            }
            Assert.That(d.Playback.Complete,Is.True);Assert.That(visited,Is.EqualTo(100));
            Assert.That(d.MenuPage,Is.EqualTo("result"));Assert.That(view.Comparison.A.Complete&&view.Comparison.B.Complete,Is.True);
            var a=d.Playback.A.RawScore;var b=d.Playback.B.RawScore;
            d.Activate("results-table");Assert.That(view.Garden.M5.Diorama.ModelRoot.GetComponentsInChildren<MeshFilter>().Count(m=>m.name=="Pine"),Is.EqualTo(100));
            Assert.That(d.TryContribution(99,true,out _),Is.True);
            d.ShowResults(false);view.Garden.Navigation.OpenTree(0);view.Garden.Navigation.ReturnFromTree();
            Assert.That(d.Playback.A.RawScore,Is.EqualTo(a));Assert.That(d.Playback.B.RawScore,Is.EqualTo(b));
            var saved=d.FinalText;d.OpenMenu("compare");d.Activate("next-a");Assert.That(d.FinalText,Is.EqualTo(saved));
            d.Activate("forest");d.Activate("table");Assert.That(d.ResultsVisible,Is.False);
            yield return null;
        }
        [UnityTest] public IEnumerator MenuAndPlacementPreserveTrackedLocalPoseAndPauseProgress()
        {
            var d=view.Director;var head=view.Garden.Locomotion.Head;var p=head.localPosition;var q=head.localRotation;
            d.StartPlayback(false);d.AdvanceTour(1);var phase=d.Playback.Phase;
            d.OpenMenu("position");d.Activate("recenter");d.Activate("farther");d.Activate("turn-left");d.AdvanceTour(200);
            Assert.That(d.Playback.Phase,Is.EqualTo(phase));Assert.That(head.localPosition,Is.EqualTo(p));Assert.That(head.localRotation,Is.EqualTo(q));
            d.ToggleMenu();Assert.That(d.MenuVisible,Is.False);d.ToggleMenu();Assert.That(d.MenuVisible,Is.True);
            yield return null;
        }
        [UnityTest] public IEnumerator RealMenuPointerOpensScenarioAndStaleClickCannotActivateNewPage()
        {
            var d=view.Director;d.OpenMenu("home");yield return null;
            var target=view.GetComponentsInChildren<ExperienceMenuTarget>().First(t=>t.Command=="profiles");
            var pointer=new PointerEventData(EventSystem.current){pointerId=901,button=PointerEventData.InputButton.Left};
            ExecuteEvents.Execute(target.gameObject,pointer,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target.gameObject,pointer,ExecuteEvents.pointerClickHandler);
            Assert.That(d.MenuPage,Is.EqualTo("profiles"));yield return null;
            Canvas.ForceUpdateCanvases();var camera=view.Garden.Locomotion.Head.GetComponent<Camera>();
            foreach(var button in view.GetComponentsInChildren<ExperienceMenuTarget>()){
                var aim=new PointerEventData(EventSystem.current){position=camera.WorldToScreenPoint(button.transform.position)};
                var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(aim,hits);
                Assert.That(hits.Count,Is.GreaterThan(0),button.Command);
                Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject),Is.EqualTo(button.gameObject),button.Command);
            }
            var start=view.GetComponentsInChildren<ExperienceMenuTarget>().First(t=>t.Command=="start-profile");
            ExecuteEvents.Execute(start.gameObject,pointer,ExecuteEvents.pointerDownHandler);
            d.OpenMenu("home");
            ExecuteEvents.Execute(start.gameObject,pointer,ExecuteEvents.pointerClickHandler);
            Assert.That(d.PlayingTour,Is.False);
            var close=view.GetComponentsInChildren<ExperienceMenuTarget>().First(t=>t.Command=="close");
            ExecuteEvents.Execute(close.gameObject,pointer,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(close.gameObject,pointer,ExecuteEvents.pointerClickHandler);
            Assert.That(d.MenuVisible,Is.False);
        }
        [UnityTest] public IEnumerator StonePlaybackMovesAlongActualBranchAndPausesWithoutChangingScore()
        {
            var d=view.Director;d.StartPlayback(false);d.SendMessage("LateUpdate");
            Assert.That(view.presentationRoot.gameObject.activeInHierarchy,Is.True);
            var dot=view.presentationRoot.Find("ProfileRoutePresentation/ProfileA");
            var route=d.Playback.A.Trees[0];
            var from=view.nodeViews.First(n=>n.gameObject.activeSelf && n.nodeId==route.VisitedNodeIds[0]).dropAnchor.position;
            var to=view.nodeViews.First(n=>n.gameObject.activeSelf && n.nodeId==route.VisitedNodeIds[1]).dropAnchor.position;
            Assert.That(Vector3.Distance(dot.position,from),Is.LessThan(.001f));
            d.AdvanceTour(.46f);d.AdvanceTour(.15f);
            Assert.That(Vector3.Distance(dot.position,(from+to)*.5f),Is.LessThan(.001f));
            var saved=dot.position;var score=d.Playback.A.RawScore;
            d.OpenMenu("playback");d.Activate("pause");d.CloseMenu();d.AdvanceTour(10);
            Assert.That(dot.position,Is.EqualTo(saved));Assert.That(d.Playback.A.RawScore,Is.EqualTo(score));
            d.OpenMenu("playback");d.Activate("pause");d.CloseMenu();d.AdvanceTour(.2f);
            Assert.That(d.Playback.StepIndex,Is.EqualTo(1));
            Assert.That(view.nodeViews.Count(n=>n.gameObject.activeSelf),Is.LessThanOrEqualTo(7));
            yield return null;
        }
        [UnityTest] public IEnumerator AnchoredMenuAndTreeEntryFollowOnlyDeliberatePlacement()
        {
            var d=view.Director;var head=view.Garden.Locomotion.Head;
            d.OpenMenu();var menu=view.transform.Find("UnifiedExperienceMenu");var p=menu.position;var q=menu.rotation;
            head.localRotation=Quaternion.Euler(0,95,0);d.SendMessage("LateUpdate");d.CloseMenu();d.OpenMenu("tools");
            Assert.That(menu.position,Is.EqualTo(p));Assert.That(menu.rotation,Is.EqualTo(q));
            d.Activate("single");var local=head.localPosition;var rotation=head.localRotation;
            view.Garden.Navigation.OpenTree(0);
            var delta=Vector3.ProjectOnPlane(view.nodeViews[0].transform.position-head.position,Vector3.up).normalized;
            Assert.That(Vector3.Dot(delta,Vector3.ProjectOnPlane(head.forward,Vector3.up).normalized),Is.GreaterThan(.99f));
            Assert.That(head.localPosition,Is.EqualTo(local));Assert.That(head.localRotation,Is.EqualTo(rotation));
            var anchor=view.presentationRoot.rotation;head.localRotation=Quaternion.Euler(0,150,0);d.SendMessage("LateUpdate");
            Assert.That(view.presentationRoot.rotation,Is.EqualTo(anchor));
            int refreshes=view.Garden.M6.ContentRefreshCount;
            for(int i=0;i<30;i++){d.SendMessage("LateUpdate");yield return null;}
            Assert.That(view.Garden.M6.ContentRefreshCount,Is.EqualTo(refreshes),"Idle frames must not rebuild explanations or validate the ensemble.");
        }
        [UnityTest] public IEnumerator TableSizesVaryAndPointerHoverShowsPassportWithoutSelecting()
        {
            view.Director.Activate("table");yield return null;
            var table=view.Garden.M5.Diorama;
            var pines=table.ModelRoot.GetComponentsInChildren<MeshFilter>().Where(m=>m.name=="Pine").ToArray();
            var heights=pines.Select(m=>m.sharedMesh.bounds.size.y*m.transform.localScale.y).ToArray();
            var widths=pines.Select(m=>m.sharedMesh.bounds.size.x*m.transform.localScale.x).ToArray();
            Assert.That(heights.Max()/heights.Min(),Is.GreaterThan(2));Assert.That(widths.Max()/widths.Min(),Is.GreaterThan(2));
            var target=table.ModelRoot.GetComponentsInChildren<M5PointerTarget>().First(t=>t.action==M5Action.SelectTree && t.index==25);
            var revision=view.Session.State.Revision;var selected=view.Ensemble.Index;
            var pointer=new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(target.gameObject,pointer,ExecuteEvents.pointerEnterHandler);
            Assert.That(table.HoveredIndex,Is.EqualTo(25));Assert.That(table.HoverText.text,Does.Contain("26"));
            Assert.That(view.Session.State.Revision,Is.EqualTo(revision));Assert.That(view.Ensemble.Index,Is.EqualTo(selected));
            ExecuteEvents.Execute(target.gameObject,pointer,ExecuteEvents.pointerExitHandler);Assert.That(table.HoveredIndex,Is.EqualTo(-1));
        }
        [UnityTest] public IEnumerator CalculateWholeModelSkipsAnimationForSingleAndPairWithCompleteResults()
        {
            var d=view.Director;
            foreach(bool pair in new[]{false,true}){
                d.StartPlayback(pair);d.AdvanceTour(.1f);d.OpenMenu("playback");d.Activate("pause");
                var a=d.Playback.A.RawScore;var b=d.Playback.B?.RawScore;
                d.Activate("calculate-all");
                Assert.That(d.Playback.Complete,Is.True);Assert.That(d.Playback.CompletedTrees,Is.EqualTo(100));
                Assert.That(d.PlayingTour,Is.False);Assert.That(d.MenuPage,Is.EqualTo("result"));
                Assert.That(d.Playback.A.RawScore,Is.EqualTo(a));Assert.That(view.Ensemble.Complete,Is.True);
                if(pair){Assert.That(d.Playback.B.RawScore,Is.EqualTo(b));Assert.That(view.Comparison.B.Complete,Is.True);}
                d.Activate("results-table");Assert.That(d.TryContribution(99,false,out var value),Is.True);
                Assert.That(value,Is.EqualTo(d.Playback.A.Trees[99].Contribution));
                yield return null;
            }
        }
        [UnityTest] public IEnumerator RecenterButtonCentersTreeTableAndMenuOncePerPressWithoutChangingPlayback()
        {
            var d=view.Director;var head=view.Garden.Locomotion.Head;
            view.GetComponent<DesktopTreePreview>().enabled=false;
            d.StartPlayback(true);d.AdvanceTour(.46f);d.AdvanceTour(.1f);d.Playback.Paused=true;
            head.localRotation=Quaternion.Euler(0,113,0);var local=head.localPosition;var rotation=head.localRotation;
            var progress=d.Playback.Movement;var score=d.Playback.A.RawScore;
            d.HandleRecenterButton(true);
            Assert.That(head.localPosition,Is.EqualTo(local));Assert.That(head.localRotation,Is.EqualTo(rotation));
            Assert.That(d.Playback.Movement,Is.EqualTo(progress));Assert.That(d.Playback.A.RawScore,Is.EqualTo(score));Assert.That(d.Playback.Paused,Is.True);
            var ahead=Vector3.ProjectOnPlane(view.nodeViews[0].transform.position-head.position,Vector3.up).normalized;
            Assert.That(Vector3.Dot(ahead,Vector3.ProjectOnPlane(head.forward,Vector3.up).normalized),Is.GreaterThan(.99));
            var origin=view.Garden.Locomotion.Origin.rotation;head.localRotation=Quaternion.Euler(0,150,0);d.HandleRecenterButton(true);
            Assert.That(view.Garden.Locomotion.Origin.rotation,Is.EqualTo(origin),"Holding B must not follow the head.");
            d.HandleRecenterButton(false);d.Activate("table");head.localRotation=Quaternion.Euler(0,75,0);d.HandleRecenterButton(true);
            ahead=Vector3.ProjectOnPlane(view.Garden.M5.Diorama.ModelRoot.position-head.position,Vector3.up).normalized;
            Assert.That(Vector3.Dot(ahead,Vector3.ProjectOnPlane(head.forward,Vector3.up).normalized),Is.GreaterThan(.99));
            var menu=view.transform.Find("UnifiedExperienceMenu");ahead=Vector3.ProjectOnPlane(menu.position-head.position,Vector3.up).normalized;
            Assert.That(Vector3.Dot(ahead,Vector3.ProjectOnPlane(head.forward,Vector3.up).normalized),Is.GreaterThan(.99));
            yield return null;
        }
        [UnityTest] public IEnumerator SceneryPinesShareBlueFrostAcrossBothScenesAndKeepModelMaterialSeparate()
        {
            var branches=view.GetComponentsInChildren<Renderer>(true).Where(r=>r.name=="RadialBranches").ToArray();
            Assert.That(branches.Length,Is.GreaterThan(10));
            var material=branches[0].sharedMaterial;
            Assert.That(material.name,Is.EqualTo("BlueFrostSceneryFoliage"));
            Assert.That(branches.All(r=>r.sharedMaterial==material),Is.True);
            var color=material.GetColor("_BaseColor");Assert.That(color.b-color.g,Is.GreaterThan(.35f));
            Assert.That(material.GetColor("_EmissionColor").b,Is.GreaterThan(.2f));
            var model=view.transform.Find("ModelPineGarden/ModelTree-1/Pine").GetComponent<Renderer>();
            Assert.That(model.sharedMaterial,Is.Not.SameAs(material));Assert.That(model.sharedMaterial.color.g,Is.GreaterThan(model.sharedMaterial.color.b));
            yield return null;
        }
        [UnityTest] public IEnumerator WildlifeOnlyAppearsDuringSoundEvent()
        {
            var wildlife=view.Garden.M5.Atmosphere.Wildlife;wildlife.enabled=false;
            wildlife.SendMessage("OnApplicationFocus",true);wildlife.SendMessage("OnApplicationPause",false);
            wildlife.SetSoundEnabled(false);Assert.That(wildlife.Visible,Is.False);
            wildlife.SetSoundEnabled(true);wildlife.AdvanceAmbience(13);Assert.That(wildlife.Visible,Is.True);
            wildlife.AdvanceAmbience(8);Assert.That(wildlife.Visible,Is.False);
            yield return null;
        }
    }
}
