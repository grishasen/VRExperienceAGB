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
        [UnityTest] public IEnumerator MenuAndFocusPauseRestoreIndependently()
        {
            view.Garden.Navigation.ShowForest();view.Garden.Navigation.OpenTree(8);
            Assert.That(view.IsSeated,Is.True);
            foreach(bool menuFirst in new[]{true,false})foreach(bool focusFirst in new[]{true,false}) {
                if(menuFirst)view.Director.OpenMenu("tree");
                view.SendMessage("OnApplicationFocus",false);
                if(!menuFirst)view.Director.OpenMenu("tree");
                if(focusFirst)view.SendMessage("OnApplicationFocus",true);
                view.Director.CloseMenu();
                if(!focusFirst) {
                    Assert.That(view.Session.State.Paused,Is.True,"Focus loss still suspends movement.");
                    view.SendMessage("OnApplicationFocus",true);
                }
                Assert.That(view.Session.State.CanAdvance,Is.True,"Menu pause must not survive focus recovery.");
                foreach(var branch in new[]{TreeAction.TrueBranch,TreeAction.FalseBranch}) {
                    var button=view.controls.Single(c=>c.action==branch);
                    Assert.That(button.GetComponent<UnityEngine.UI.Button>().interactable,Is.True);
                    var input=new PointerEventData(EventSystem.current){pointerId=77,button=PointerEventData.InputButton.Left};
                    button.OnPointerDown(input);button.OnPointerClick(input);view.Advance(2);
                    Assert.That(view.Session.State.Decisions.Count,Is.EqualTo(1));
                    view.Execute(TreeAction.Back,view.Session.State.Revision,view.Session.State.NodeId);
                }
            }
            view.Session.SetPaused(true);view.Director.OpenMenu("tree");
            view.SendMessage("OnApplicationFocus",false);view.Director.CloseMenu();view.SendMessage("OnApplicationFocus",true);
            Assert.That(view.Session.State.Paused,Is.True,"Preserve an explicit preexisting pause.");
            view.Session.SetPaused(false);view.Director.CloseMenu();
            Assert.That(view.Session.State.Paused,Is.False,"Closing an already closed menu must not restore a stale pause.");
            yield return null;
        }
        [UnityTest] public IEnumerator SelectedPredictorMarksExactlyMatchingTrees()
        {
            view.Director.Activate("forest");
            var feature=view.Model.Trees[0].Nodes.OfType<VRExperienceAGB.Domain.SplitNode>().First().FeatureId;
            view.Garden.M6.LinkFeature(feature);yield return null;
            var matches=view.Garden.M6.Analysis.MatchingTrees(feature);
            var root=view.transform.Find("ModelPineGarden");
            for(int i=0;i<view.Garden.PlotCount;i++) {
                var plot=root.Find("ModelTree-"+(i+1));var marker=plot.Find("SelectedPredictorMarker");
                Assert.That(marker.gameObject.activeInHierarchy,Is.EqualTo(matches.Contains(i)));
                Assert.That(marker.GetComponent<Collider>(),Is.Null);
                if(matches.Contains(i))Assert.That(plot.Find("PredictorMatch").GetComponentInChildren<TMPro.TMP_Text>(true).text,Does.StartWith("Selected predictor: "));
            }
            view.Garden.M6.LinkFeature("__no_matching_predictor__");
            Assert.That(root.GetComponentsInChildren<MeshRenderer>().Count(r=>r.name=="SelectedPredictorMarker"),Is.Zero);
        }
        [UnityTest] public IEnumerator TablePredictorMarkersFollowSelectionAndGrouping()
        {
            view.Garden.Navigation.ShowDiorama();
            var table=view.Garden.M5.Diorama;
            var feature=view.Model.Trees[0].Nodes.OfType<VRExperienceAGB.Domain.SplitNode>().First().FeatureId;
            for(int group=0;group<3;group++) {
                view.Garden.M6.LinkFeature(feature);yield return null;
                foreach(Transform plot in table.ModelRoot) {
                    if(!plot.name.StartsWith("DioramaTree-") || !plot.gameObject.activeSelf)continue;
                    int index=int.Parse(plot.name.Substring("DioramaTree-".Length))-1;
                    var marker=plot.Find("SelectedPredictorMarker");
                    Assert.That(marker.gameObject.activeSelf,Is.EqualTo(view.Garden.M6.Analysis.MatchingTrees(feature).Contains(index)));
                    Assert.That(marker.GetComponent<Collider>(),Is.Null);
                }
                view.Garden.M6.LinkFeature(null);
                Assert.That(table.ModelRoot.GetComponentsInChildren<MeshRenderer>().Count(r=>r.name=="SelectedPredictorMarker"),Is.Zero);
                table.NextGroup();
            }
        }
        [UnityTest] public IEnumerator MainMenuSearchOpensClosedPanelAndKeepsTableSelection()
        {
            view.Garden.Navigation.ShowDiorama();view.Director.OpenMenu("home");yield return null;
            var target=view.GetComponentsInChildren<ExperienceMenuTarget>().Single(t=>t.Command=="search");
            var pointer=new PointerEventData(EventSystem.current){pointerId=903,button=PointerEventData.InputButton.Left};
            ExecuteEvents.Execute(target.gameObject,pointer,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target.gameObject,pointer,ExecuteEvents.pointerClickHandler);yield return null;
            var extension=view.Garden.M5.Extensions;
            Assert.That(view.Director.MenuVisible,Is.False);Assert.That(extension.PanelOpen,Is.True);
            var panel=view.GetComponentsInChildren<Canvas>().Single(c=>c.name=="ExplorationExtensions");
            Assert.That(panel.enabled,Is.True);
            Assert.That(panel.GetComponentInChildren<Oculus.Interaction.RayInteractable>().enabled,Is.True);
            var feature=view.Model.Trees[0].Nodes.OfType<VRExperienceAGB.Domain.SplitNode>().First().FeatureId;
            extension.FilterFeatures(feature);extension.Close();
            Assert.That(view.Garden.Navigation.Page,Is.EqualTo(NavigationPage.Diorama));
            Assert.That(extension.Feature,Is.EqualTo(feature));Assert.That(extension.SearchActive,Is.True);
            Assert.That(view.Garden.M5.Diorama.ModelRoot.GetComponentsInChildren<MeshRenderer>().Count(r=>r.name=="SelectedPredictorMarker"),Is.GreaterThan(0));
            foreach(var command in new[]{"search","trail","review"}) {
                view.Director.OpenMenu("home");view.Director.Activate(command);
                Assert.That(extension.PanelOpen,Is.True,command);extension.Close();
            }
        }
        [UnityTest] public IEnumerator SearchKeyboardOwnsRaysAndTypesWithoutFollowingHead()
        {
            view.Director.Activate("search");view.Garden.M5.Activate(M5Action.SearchFilter);yield return null;
            var panel=view.GetComponentsInChildren<Canvas>().Single(c=>c.name=="ExplorationExtensions");
            var position=panel.transform.position;var rotation=panel.transform.rotation;
            var head=view.Garden.Locomotion.Head;head.position+=Vector3.right*.15f;head.rotation*=Quaternion.Euler(0,8,0);
            view.Garden.M5.Extensions.Refresh();
            Assert.That(panel.transform.position,Is.EqualTo(position));Assert.That(panel.transform.rotation,Is.EqualTo(rotation));
            var keys=view.GetComponentsInChildren<M5PointerTarget>().Where(t=>t.action==M5Action.ReviewKey || t.action==M5Action.ReviewBackspace || t.action==M5Action.ReviewTextDone).ToArray();
            Canvas.ForceUpdateCanvases();
            foreach(var key in keys) {
                var hits=new System.Collections.Generic.List<RaycastResult>();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=head.GetComponent<Camera>().WorldToScreenPoint(key.transform.position)},hits);
                Assert.That(hits.Count,Is.GreaterThan(0),key.name);
                Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject),Is.EqualTo(key.gameObject),key.name);
                var canvas=key.GetComponentInParent<Canvas>();
                var surface=canvas.GetComponentInChildren<Oculus.Interaction.PointableCanvas>();
                var filter=typeof(Oculus.Interaction.PointableCanvasModule).GetMethod("FindFirstRaycastWithinCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
                var routed=(RaycastResult)filter.Invoke(null,new object[]{hits,surface.Canvas});
                Assert.That(routed.gameObject,Is.Not.Null,"Meta canvas routing rejected "+key.name);
                Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(routed.gameObject),Is.EqualTo(key.gameObject),key.name);
            }
            foreach(var key in keys)foreach(float side in new[]{-.3f,.3f}) {
                var origin=head.position+new Vector3(side,-.45f,0);var ray=new Ray(origin,key.transform.position-origin);
                var hit=Object.FindObjectsByType<Oculus.Interaction.RayInteractable>(FindObjectsSortMode.None).Where(r=>r.enabled)
                    .Select(r=>new {r,d=r.Raycast(ray,out var h,20,false)?h.Distance:float.PositiveInfinity}).OrderBy(x=>x.d).First();
                Assert.That(hit.r.GetComponent<Oculus.Interaction.PointableCanvas>().Canvas,Is.EqualTo(key.GetComponentInParent<Canvas>()),key.name);
            }
            var input=new PointerEventData(EventSystem.current){pointerId=904,button=PointerEventData.InputButton.Left};
            foreach(var key in new[]{keys.First(t=>t.action==M5Action.ReviewKey),keys.First(t=>t.action==M5Action.ReviewBackspace),keys.First(t=>t.action==M5Action.ReviewTextDone)}) {
                ExecuteEvents.Execute(key.gameObject,input,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(key.gameObject,input,ExecuteEvents.pointerClickHandler);
                if(key.action==M5Action.ReviewKey)Assert.That(view.Garden.M5.Extensions.BodyText,Does.Contain("1 / 240"));
                if(key.action==M5Action.ReviewBackspace)Assert.That(view.Garden.M5.Extensions.BodyText,Does.Contain("0 / 240"));
            }
            Assert.That(panel.GetComponent<UnityEngine.UI.GraphicRaycaster>().enabled,Is.True);
            Assert.That(view.GetComponentsInChildren<RectTransform>().Any(c=>c.name=="ReviewExplanationKeyboard"),Is.False);
        }
        [UnityTest] public IEnumerator DistantDetailsCullWithoutRemovingTreesAndRestoreNearViewer()
        {
            // This test drives hover explicitly; the desktop cursor must not re-enter a crown.
            Object.FindAnyObjectByType<DesktopTreePreview>().enabled=false;
            view.Director.Activate("forest");yield return null;
            var garden=view.transform.Find("ModelPineGarden");
            var compositions=garden.GetComponentsInChildren<Canvas>(true).Where(c=>c.name=="PredictorComposition").ToArray();
            Assert.That(compositions.Length,Is.EqualTo(100));
            Assert.That(compositions.Count(c=>c.gameObject.activeInHierarchy),Is.LessThan(20));
            Assert.That(compositions.All(c=>c.GetComponent<UnityEngine.UI.GraphicRaycaster>()==null && c.GetComponentInChildren<Oculus.Interaction.RayInteractable>(true)==null),Is.True);
            Assert.That(view.Garden.VisiblePlotCount,Is.EqualTo(100));
            var last=compositions.Last();view.Garden.Locomotion.Head.position=last.transform.position+new Vector3(0,1.5f,-2);
            view.Garden.SendMessage("RefreshNearbyPlaques");
            Assert.That(last.gameObject.activeInHierarchy,Is.True);
            Assert.That(view.Garden.VisiblePlotCount,Is.EqualTo(100));
            var first=garden.Find("ModelTree-1");
            view.Garden.Locomotion.Head.position=first.position+new Vector3(0,1.5f,-10);
            view.Garden.Hover(0);Assert.That(view.Garden.DescriptionCard.Visible,Is.False);
            view.Garden.ShowTreeChoices(0);
            Assert.That(view.Garden.DescriptionCard.Visible,Is.True,"Distant crown selection must still show its readable card.");
            view.Garden.ClearHover(0);yield return null;
            Assert.That(garden.Find("TreePlaque-1").gameObject.activeSelf,Is.False);
            var scenery=garden.GetComponent<ForestPerimeterScenery>();Assert.That(scenery,Is.Not.Null);
            foreach(var name in new[]{"CarezzaMountainPanorama","CarezzaLake","CarezzaShore","CarezzaShorePines","RearSlateBoulder","RearGroundFern"}) {
                var item=garden.Find(name);Assert.That(item,Is.Not.Null);
                Assert.That(item.GetComponent<Collider>(),Is.Null);
                Assert.That(item.GetComponent<MeshFilter>().sharedMesh.vertexCount,Is.InRange(1,59999));
                Assert.That(item.GetComponent<MeshFilter>().sharedMesh.bounds.size.sqrMagnitude,Is.GreaterThan(1));
            }
        }
        [UnityTest] public IEnumerator NodeDetailsStayReadableAndStableWithoutSidePanels()
        {
            view.Garden.Navigation.ShowForest();view.Garden.Navigation.OpenTree(10);
            yield return null;
            Object.FindAnyObjectByType<DesktopTreePreview>().enabled=false;
            Assert.That(view.Garden.M6.SegmentText.gameObject.activeInHierarchy,Is.False);
            Assert.That(view.Garden.M6.EvidenceText.gameObject.activeInHierarchy,Is.False);
            var tooltip=view.NameTooltip;
            var owner=view.explanation.GetComponent<NodeNameHover>();
            owner.OnPointerEnter(new PointerEventData(EventSystem.current));
            yield return null;
            Assert.That(tooltip.Visible,Is.True);
            Assert.That(tooltip.CompactDescription,Is.Not.Null);
            Assert.That(tooltip.CompactDescription.PredicateText.font.name,Is.EqualTo("Inter-SemiBold"));
            Assert.That(tooltip.CompactDescription.PredicateText.enableAutoSizing,Is.False);
            var canvas=tooltip.CompactDescription.GetComponentInParent<Canvas>();
            Assert.That(((RectTransform)canvas.transform).sizeDelta,Is.EqualTo(new Vector2(580,400)));
            var world=canvas.transform.position;var revision=view.Session.State.Revision;
            yield return new WaitForSecondsRealtime(.6f);
            Assert.That(tooltip.Visible,Is.True);Assert.That(canvas.transform.position,Is.EqualTo(world));
            Assert.That(view.Session.State.Revision,Is.EqualTo(revision));
            Assert.That(view.Garden.M6.EdgeLabels.All(t=>!t.gameObject.activeInHierarchy),Is.True);

        }
        [UnityTest] public IEnumerator SampleProfilesMatchIndependentReferenceForEveryLeaf()
        {
            Assert.That(view.Model.Trees.Count,Is.EqualTo(100));
            var expected=JsonUtility.FromJson<ReferenceFile>(Resources.Load<TextAsset>("LocalModel/expected").text);
            Assert.That(view.AvailableProfiles.Profiles.Count,Is.EqualTo(2));
            Assert.That(view.ActiveModelName,Is.EqualTo("export_Mobile_Click_Through_Rate_AGB_sample.json"));
            for(int p=0;p<2;p++) {
                view.StartScenarioProfiles(p);
                var result=view.Ensemble.Evaluation;var reference=expected.results[p];
                Assert.That(result.RawScore,Is.EqualTo(reference.score).Within(1e-10));
                Assert.That(result.Probability,Is.EqualTo(reference.probability).Within(1e-12));
                for(int i=0;i<view.Model.Trees.Count;i++)Assert.That(result.Trees[i].LeafId,Is.EqualTo(reference.trees[i].leaf));
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
            Assert.That(d.Playback.Complete,Is.True);Assert.That(visited,Is.EqualTo(view.Model.Trees.Count));
            Assert.That(d.FinalText, Does.Contain("ENSEMBLE SCORE").And.Contain("Score difference B − A").And.Contain("Propensity comes from calibration"));
            Assert.That(d.FinalText, Does.Not.Contain("Probability").And.Not.Contain("sigmoid").And.Not.Contain("percentage points"));
            Assert.That(d.MenuPage,Is.EqualTo("result"));Assert.That(view.Comparison.A.Complete&&view.Comparison.B.Complete,Is.True);
            var a=d.Playback.A.RawScore;var b=d.Playback.B.RawScore;
            d.Activate("results-table");Assert.That(view.Garden.M5.Diorama.ModelRoot.GetComponentsInChildren<MeshFilter>().Count(m=>m.name=="Pine"),Is.EqualTo(view.Model.Trees.Count));
            Assert.That(d.TryContribution(view.Model.Trees.Count-1,true,out _),Is.True);
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
            var d=view.Director;d.SetPace(PlaybackPace.Fast);d.StartPlayback(false);d.SendMessage("LateUpdate");
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
        [UnityTest] public IEnumerator MenuReopensAheadAndStaysAnchoredWhileReading()
        {
            var d=view.Director;var head=view.Garden.Locomotion.Head;
            d.OpenMenu();var menu=view.transform.Find("UnifiedExperienceMenu");var p=menu.position;var q=menu.rotation;
            head.localRotation=Quaternion.Euler(0,95,0);d.SendMessage("LateUpdate");d.CloseMenu();d.OpenMenu("tools");
            Assert.That(Vector3.Dot((menu.position-head.position).normalized,head.forward),Is.GreaterThan(.99f));
            Assert.That(menu.position,Is.Not.EqualTo(p));
            p=menu.position;q=menu.rotation;
            head.localRotation=Quaternion.Euler(0,110,0);d.OpenMenu("help");d.SendMessage("LateUpdate");
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
                Assert.That(d.Playback.Complete,Is.True);Assert.That(d.Playback.CompletedTrees,Is.EqualTo(view.Model.Trees.Count));
                Assert.That(d.PlayingTour,Is.False);Assert.That(d.FinalText, Does.Contain("ENSEMBLE SCORE").And.Contain("Propensity comes from calibration"));
            Assert.That(d.FinalText, Does.Not.Contain("Probability").And.Not.Contain("sigmoid").And.Not.Contain("percentage points"));
            Assert.That(d.MenuPage,Is.EqualTo("result"));
                Assert.That(d.Playback.A.RawScore,Is.EqualTo(a));Assert.That(view.Ensemble.Complete,Is.True);
                if(pair){Assert.That(d.Playback.B.RawScore,Is.EqualTo(b));Assert.That(view.Comparison.B.Complete,Is.True);}
                d.Activate("results-table");Assert.That(d.TryContribution(view.Model.Trees.Count-1,false,out var value),Is.True);
                Assert.That(value,Is.EqualTo(d.Playback.A.Trees[view.Model.Trees.Count-1].Contribution));
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
        [UnityTest] public IEnumerator CarezzaSceneryKeepsTeachingTreeMaterialsSeparate()
        {
            var branches=view.transform.Find("MoonlitEnvironment").GetComponentsInChildren<Renderer>(true).Where(r=>r.name=="RadialBranches").ToArray();
            Assert.That(branches.Length,Is.GreaterThan(10));
            var material=branches[0].sharedMaterial;
            Assert.That(material.name,Is.EqualTo("BlueFrostSceneryFoliage"));
            Assert.That(branches.All(r=>r.sharedMaterial==material),Is.True);
            var color=material.GetColor("_BaseColor");Assert.That(color.b-color.g,Is.InRange(.08f,.25f));
            Assert.That(material.GetColor("_EmissionColor").b,Is.InRange(.04f,.09f));
            var model=view.transform.Find("ModelPineGarden/ModelTree-1/Pine").GetComponent<Renderer>();
            Assert.That(model.sharedMaterial,Is.Not.SameAs(material));Assert.That(model.sharedMaterial.color.g,Is.GreaterThan(model.sharedMaterial.color.b));
            Assert.That(model.sharedMaterial.GetColor("_EmissionColor").g,Is.InRange(.04f,.09f));
            var scenery=view.transform.Find("ModelPineGarden").GetComponentsInChildren<Renderer>(true).Where(r=>r.name=="RadialBranches").ToArray();
            Assert.That(scenery.Length,Is.GreaterThan(10));
            Assert.That(scenery.All(r=>r.sharedMaterial==view.Garden.carezzaSpruceMaterial),Is.True);
            Assert.That(view.Garden.carezzaSpruceMaterial.mainTexture,Is.Not.Null);
            Assert.That(view.Garden.carezzaSpruceMaterial.shader.isSupported,Is.True);
            view.Director.Activate("table");
            foreach(var pine in view.Garden.M5.Diorama.ModelRoot.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name=="Pine"))
                Assert.That(pine.sharedMaterial,Is.SameAs(model.sharedMaterial));
            yield return null;
        }
        [UnityTest] public IEnumerator CarezzaSceneryStaysWorldAnchoredAndReleasesMeshesOnModelReplacement()
        {
            view.Director.Activate("forest");yield return null;
            var root=view.transform.Find("ModelPineGarden");
            var panorama=root.Find("CarezzaMountainPanorama");var lake=root.Find("CarezzaLake");
            var panoramaPosition=panorama.position;
            var oldMesh=panorama.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(lake.GetComponent<Renderer>().bounds.max.z,Is.LessThan(-8),"Keep the bank outside the walkable garden.");
            Assert.That(lake.GetComponent<Renderer>().sharedMaterial.shader.isSupported,Is.True);
            view.Garden.Locomotion.Origin.position+=new Vector3(2,0,3);yield return null;
            Assert.That(panorama.position,Is.EqualTo(panoramaPosition),"Scenery must not follow the head or locomotion origin.");
            var model=DeepTreeExample.Model();
            view.OpenImportedModel(new VRExperienceAGB.Import.ImportedModel(model),new VRExperienceAGB.Domain.ProfileSet(1,model.Id,Array.Empty<VRExperienceAGB.Domain.PreparedProfile>()),"Small scenery regression model");
            view.Director.Activate("forest");yield return null;yield return null;
            Assert.That(oldMesh==null,Is.True,"Discarded garden meshes must be released.");
            var replacement=view.transform.Find("ModelPineGarden");
            Assert.That(replacement.Find("CarezzaLake"),Is.Not.Null);
            Assert.That(replacement.Find("CarezzaMountainPanorama").GetComponent<MeshFilter>().sharedMesh.vertexCount,Is.GreaterThan(0));
            Assert.That(view.Garden.PlotCount,Is.EqualTo(model.Trees.Count));
        }
        [UnityTest] public IEnumerator WildlifeOnlyAppearsDuringSoundEvent()
        {
            var wildlife=view.Garden.M5.Atmosphere.Wildlife;wildlife.enabled=false;
            wildlife.SendMessage("OnApplicationFocus",true);wildlife.SendMessage("OnApplicationPause",false);
            wildlife.SetSoundEnabled(false);Assert.That(wildlife.Visible,Is.False);
            wildlife.SetSoundEnabled(true);wildlife.AdvanceAmbience(60);Assert.That(wildlife.Visible,Is.True);
            wildlife.AdvanceAmbience(8);Assert.That(wildlife.Visible,Is.False);
            yield return null;
        }
        [UnityTest] public IEnumerator ForestGroundCoversEverySceneryTreeIncludingSmallImportedModels()
        {
            for(int pass=0;pass<2;pass++) {
                if(pass==1) {
                    var model=DeepTreeExample.Model();
                    view.OpenImportedModel(new VRExperienceAGB.Import.ImportedModel(model),new VRExperienceAGB.Domain.ProfileSet(1,model.Id,Array.Empty<VRExperienceAGB.Domain.PreparedProfile>()),"Small test model");
                }
                view.Director.Activate("forest");yield return null;
                var root=view.transform.Find("ModelPineGarden");var ground=root.Find("GardenGround").GetComponent<Renderer>().bounds;
                foreach(var tree in root.Cast<Transform>().Where(t=>t.name=="GardenBackdrop-PineTree" || t.name.StartsWith("ModelTree-")))
                    foreach(var renderer in tree.GetComponentsInChildren<MeshRenderer>()) {
                        var bounds=renderer.bounds;
                        Assert.That(bounds.min.x,Is.GreaterThanOrEqualTo(ground.min.x),tree.name);
                        Assert.That(bounds.max.x,Is.LessThanOrEqualTo(ground.max.x),tree.name);
                        Assert.That(bounds.min.z,Is.GreaterThanOrEqualTo(ground.min.z),tree.name);
                        Assert.That(bounds.max.z,Is.LessThanOrEqualTo(ground.max.z),tree.name);
                    }
            }
        }
        [UnityTest] public IEnumerator PlaybackBoardDoesNotCoverTheBallsAndStaysAnchored()
        {
            var d=view.Director;var camera=view.Garden.Locomotion.Head.GetComponent<Camera>();
            foreach(bool pair in new[]{false,true}) {
                d.StartPlayback(pair);d.SendMessage("LateUpdate");
                var panel=(RectTransform)view.presentationRoot.Find("ProfileRoutePresentation/ProfileExplanation");
                for(int step=0;step<35;step++) {
                    d.AdvanceTour(.15f);
                    foreach(var dot in panel.parent.Cast<Transform>().Where(t=>t.name.StartsWith("Profile") && t!=panel && t.gameObject.activeSelf))
                        Assert.That(RectTransformUtility.RectangleContainsScreenPoint(panel,camera.WorldToScreenPoint(dot.position),camera),Is.False,"Explanation must not obscure "+dot.name);
                }
                var position=panel.position;var rotation=panel.rotation;
                camera.transform.localRotation*=Quaternion.Euler(0,20,0);d.SendMessage("LateUpdate");
                Assert.That(panel.position,Is.EqualTo(position));Assert.That(panel.rotation,Is.EqualTo(rotation));
            }
            yield return null;
        }
        [UnityTest] public IEnumerator ButtonHintsExplainCommandsWithoutInterceptingClicks()
        {
            Object.FindAnyObjectByType<DesktopTreePreview>().enabled=false;
            view.Director.OpenMenu("home");yield return null;
            var button=view.GetComponentsInChildren<ExperienceMenuTarget>().First(b=>b.Command=="profiles");
            var pointer=new PointerEventData(EventSystem.current){pointerId=92,button=PointerEventData.InputButton.Left};
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerEnterHandler);yield return null;
            var hint=button.GetComponent<ButtonHint>();Assert.That(hint.Visible,Is.True);
            var canvas=button.GetComponentInParent<Canvas>().transform.Cast<Transform>().Single(t=>t.name=="ButtonHint" && t.gameObject.activeInHierarchy);
            Assert.That(hint.HintText,Does.Contain("every tree"));
            Assert.That(canvas.GetComponentsInChildren<UnityEngine.UI.Graphic>().All(g=>!g.raycastTarget),Is.True);
            Assert.That(button.GetComponent<UnityEngine.UI.Outline>().enabled,Is.True);
            Assert.That(button.transform.Find("HoverArrow"),Is.Null);
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
            Assert.That(view.Director.MenuPage,Is.EqualTo("profiles"));yield return null;
            Assert.That(canvas==null || !canvas.gameObject.activeInHierarchy,Is.True);
        }
        [UnityTest] public IEnumerator ScreenshotButtonSavesOnePngPerPressWithoutChangingPlayback()
        {
            var capture=view.GetComponent<FeedbackCapture>();capture.enabled=false;
            view.Director.StartPlayback(true);var score=view.Director.Playback.A.RawScore;
            var head=view.Garden.Locomotion.Head;var position=head.localPosition;var rotation=head.localRotation;
            capture.HandleButton(true);float deadline=Time.realtimeSinceStartup+20;
            while(capture.Busy && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(capture.Busy,Is.False);Assert.That(capture.LastPath,Is.Not.Null,capture.Status);
            string file=capture.LastPath;var bytes=System.IO.File.ReadAllBytes(file);
            Assert.That(bytes.Take(8),Is.EqualTo(new byte[]{137,80,78,71,13,10,26,10}));
            var image=new Texture2D(2,2);Assert.That(image.LoadImage(bytes),Is.True);
            Assert.That(image.width,Is.EqualTo(1600));Assert.That(image.height,Is.EqualTo(900));
            Assert.That(image.GetPixels32().Count(c=>c.r>30 || c.g>30 || c.b>30),Is.GreaterThan(1000),"Capture must contain scene pixels, not an empty XR buffer.");
            Object.Destroy(image);
            capture.HandleButton(true);Assert.That(capture.Busy,Is.False);Assert.That(capture.LastPath,Is.EqualTo(file));
            Assert.That(head.localPosition,Is.EqualTo(position));Assert.That(head.localRotation,Is.EqualTo(rotation));
            Assert.That(view.Director.Playback.A.RawScore,Is.EqualTo(score));
            System.IO.File.Delete(file);
        }
        [UnityTest] public IEnumerator ForestInfoCanBeClosedAndReopenedWithoutChangingTheModel()
        {
            view.Director.Activate("forest");yield return null;
            var info=view.Garden.M6;var model=view.Model;var score=view.Ensemble.RouteTotal;
            Assert.That(info.StoryText.gameObject.activeInHierarchy,Is.False);
            var handle=view.GetComponentsInChildren<ForestInfoTarget>().Single(t=>!t.Close);
            var pointer=new PointerEventData(EventSystem.current){pointerId=95,button=PointerEventData.InputButton.Left};
            ExecuteEvents.Execute(handle.gameObject,pointer,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(handle.gameObject,pointer,ExecuteEvents.pointerClickHandler);yield return null;
            Assert.That(info.StoryText.gameObject.activeInHierarchy,Is.True);
            var close=view.GetComponentsInChildren<ForestInfoTarget>().Single(t=>t.Close);
            Canvas.ForceUpdateCanvases();var camera=view.Garden.Locomotion.Head.GetComponent<Camera>();
            var hits=new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=camera.WorldToScreenPoint(close.transform.position)},hits);
            Assert.That(hits.Count,Is.GreaterThan(0));Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject),Is.EqualTo(close.gameObject));
            ExecuteEvents.Execute(close.gameObject,pointer,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(close.gameObject,pointer,ExecuteEvents.pointerClickHandler);info.Refresh();
            Assert.That(info.StoryText.gameObject.activeInHierarchy,Is.False);
            view.Garden.Navigation.OpenTree(0);view.Garden.Navigation.ReturnFromTree();
            Assert.That(info.StoryText.gameObject.activeInHierarchy,Is.False);
            view.Director.OpenMenu("home");view.Director.Activate("forest-info");yield return null;
            Assert.That(info.StoryText.gameObject.activeInHierarchy,Is.True);Assert.That(view.Director.MenuVisible,Is.False);
            Assert.That(view.Model,Is.SameAs(model));Assert.That(view.Ensemble.RouteTotal,Is.EqualTo(score));
        }
    }
}
