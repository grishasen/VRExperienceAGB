using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRExperienceAGB.Domain;
using VRExperienceAGB.Import;
using VRExperienceAGB.Presentation;
using Object=UnityEngine.Object;

namespace VRExperienceAGB.Tests
{
    public class ReviewFixPlayModeTests
    {
        private OneTreeExperience view;
        [UnitySetUp] public IEnumerator Open()
        {
            yield return SceneManager.LoadSceneAsync("OneTreeLearning"); yield return null;
            view=Object.FindAnyObjectByType<OneTreeExperience>();
            view.enabled=false; view.Director.enabled=false;
            Object.FindAnyObjectByType<DesktopTreePreview>().enabled=false;
            string data=Path.Combine(UnityEngine.Application.dataPath,"../../data/examples");
            var model=NormalizedModelJson.ReadModel(File.ReadAllText(Path.Combine(data,"demo-model.json"))).Value;
            var profiles=NormalizedModelJson.ReadProfiles(File.ReadAllText(Path.Combine(data,"demo-profiles.json")),model).Value;
            Assert.That(view.OpenImportedModel(new ImportedModel(model),profiles,"Synthetic fixture").IsSuccess,Is.True);
            view.SendMessage("OnApplicationFocus",true); view.SendMessage("OnApplicationPause",false);
            view.Director.CloseMenu(); yield return null;
        }
        [UnityTest] public IEnumerator RejectedReplacementPreservesModelSessionPlaybackAndPair()
        {
            var d=view.Director; d.StartPlayback(true);d.Activate("calculate-all");
            var model=view.Model;var session=view.Session;var playback=d.Playback;var comparison=view.Comparison;var final=d.FinalText;
            var bad=new ModelDefinition(1,"overflow","synthetic","binary_logistic","response",0,new[]{new FeatureDefinition("x",FeatureKind.Number,true)},new[]{new ModelTree("t","n",1e308,new[]{new LeafNode("n",2)})});
            var result=view.OpenImportedModel(new ImportedModel(bad),new ProfileSet(1,bad.Id,Array.Empty<PreparedProfile>()),"Bad");
            Assert.That(result.IsSuccess,Is.False);Assert.That(view.Ready,Is.True);
            Assert.That(view.Model,Is.SameAs(model));Assert.That(view.Session,Is.SameAs(session));Assert.That(view.Comparison,Is.SameAs(comparison));
            Assert.That(d.Playback,Is.SameAs(playback));Assert.That(d.FinalText,Is.EqualTo(final));yield return null;
        }
        [UnityTest] public IEnumerator InputsShowBothProfilesAndClampEmptySearch()
        {
            var d=view.Director;d.OpenMenu("compare");d.Activate("values");yield return null;
            var texts=view.GetComponentsInChildren<TMPro.TMP_Text>().Select(t=>t.text).ToArray();
            Assert.That(texts.Any(t=>t.Contains("A: ")&&t.Contains("B: ")&&t.Contains("Page 1")),Is.True);
            Assert.That(view.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.name=="B-0"),Is.True);
            d.Activate("input-detail:0");Assert.That(d.MenuPage,Is.EqualTo("input-detail"));
            Assert.That(view.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains("Exact values")&&t.text.Contains("B ·")),Is.True);d.Activate("values");
            d.Activate("values-next");d.SetValueSearch("no-such-feature");yield return null;
            Assert.That(view.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains("No inputs match")),Is.True);
            d.Activate("values-back");Assert.That(d.MenuPage,Is.EqualTo("compare"));
        }
        [UnityTest] public IEnumerator MenuSuppressesWorldGeometryAndRestoresSameTable()
        {
            var d=view.Director;d.StartPlayback(true);d.Activate("calculate-all");d.Activate("results-table");
            var table=view.Garden.M5.Diorama.ModelRoot;var position=table.position;var score=d.Playback.A.RawScore;
            var meshes=table.GetComponentsInChildren<Renderer>();Assert.That(meshes.Any(r=>r.enabled),Is.True);
            d.OpenMenu("library");d.SendMessage("LateUpdate");
            Assert.That(meshes.All(r=>!r.enabled),Is.True);
            Assert.That(view.transform.Find("WholeForestResult").gameObject.activeSelf,Is.False);
            d.CloseMenu();d.SendMessage("LateUpdate");
            Assert.That(meshes.Any(r=>r.enabled),Is.True);Assert.That(table.position,Is.EqualTo(position));Assert.That(d.Playback.A.RawScore,Is.EqualTo(score));yield return null;
        }
        [UnityTest] public IEnumerator WorldResultRemainsAnchoredWhenHeadMovesAndKeepsProvenance()
        {
            var d=view.Director;d.StartPlayback(true);d.Activate("calculate-all");d.Activate("results-forest");d.SendMessage("LateUpdate");
            var panel=view.transform.Find("WholeForestResult");var p=panel.position;var q=panel.rotation;
            var head=view.Garden.Locomotion.Head;var original=head.localPosition;var rotation=head.localRotation;
            head.localPosition+=Vector3.right*.4f;head.localRotation*=Quaternion.Euler(0,35,0);d.SendMessage("LateUpdate");
            Assert.That(panel.position,Is.EqualTo(p));Assert.That(panel.rotation,Is.EqualTo(q));
            Assert.That(panel.GetComponentInChildren<TMPro.TMP_Text>().text,Does.Contain("ENSEMBLE SCORE").And.Contain("Propensity comes from calibration"));
            head.localPosition=original;head.localRotation=rotation;yield return null;
        }
        [UnityTest] public IEnumerator ComfortControlsBlockModalMovementAndPreserveTracking()
        {
            var movement=view.Garden.Locomotion;var d=view.Director;view.Garden.Navigation.ShowForest();
            var local=movement.Head.localPosition;var rotation=movement.Head.localRotation;var origin=movement.Origin.position;
            movement.WalkEnabled=false;movement.Move(Vector2.up,.1f);Assert.That(movement.Origin.position,Is.EqualTo(origin));
            d.OpenMenu("comfort");d.Activate("walk-mode");d.CloseMenu();movement.Move(Vector2.up,.1f);
            Assert.That(movement.Origin.position,Is.Not.EqualTo(origin));
            d.OpenMenu("comfort");origin=movement.Origin.position;var orientation=movement.Origin.rotation;
            movement.Move(Vector2.up,.1f);movement.SnapTurn(30);Assert.That(movement.Origin.position,Is.EqualTo(origin));Assert.That(movement.Origin.rotation,Is.EqualTo(orientation));
            Assert.That(movement.Head.localPosition,Is.EqualTo(local));Assert.That(movement.Head.localRotation,Is.EqualTo(rotation));yield return null;
        }
        [UnityTest] public IEnumerator ImportWhileMenuIsOpenRestoresLargeForestTrees()
        {
            var d=view.Director;
            for(int round=0;round<2;round++) {
                d.OpenMenu("models");d.SendMessage("LateUpdate");
                var original=view.Model;
                var model=new ModelDefinition(1,"replacement-"+round,"synthetic","binary_logistic","response",0,original.Features,original.Trees);
                Assert.That(view.OpenImportedModel(new ImportedModel(model),new ProfileSet(1,model.Id,Array.Empty<PreparedProfile>()),"Replacement").IsSuccess,Is.True);
                yield return null;
                var scenery=view.transform.Find("ModelPineGarden").Cast<Transform>().Where(t=>t.name=="GardenBackdrop-PineTree")
                    .SelectMany(t=>t.GetComponentsInChildren<Renderer>(true)).ToArray();
                Assert.That(scenery.Length,Is.GreaterThan(0));
                d.SendMessage("LateUpdate");Assert.That(scenery.All(r=>!r.enabled),Is.True,"The open menu still suppresses background geometry.");
                d.Activate("forest");
                Assert.That(scenery.All(r=>r.enabled && r.gameObject.activeInHierarchy),Is.True,"Cloned trees must not inherit the temporary hidden state.");
                d.OpenMenu("home");d.CloseMenu();
                Assert.That(scenery.All(r=>r.enabled),Is.True);
            }
        }
        [UnityTest] public IEnumerator EndingPlaybackRestoresBothManualBranchButtons()
        {
            var d=view.Director;
            foreach(string exit in new[]{"stop","forest"}) {
                d.StartPlayback(true);d.Activate(exit);view.Garden.Navigation.OpenTree(0);
                Assert.That(view.Comparison,Is.Null);Assert.That(view.Session.State.Mode,Is.EqualTo(VRExperienceAGB.Application.ExperienceMode.Manual));
                foreach(var action in new[]{TreeAction.TrueBranch,TreeAction.FalseBranch}) {
                    d.Activate("tree-restart");
                    var control=view.controls.Single(c=>c.action==action);
                    Assert.That(control.gameObject.activeInHierarchy,Is.True);
                    var input=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){pointerId=75,button=UnityEngine.EventSystems.PointerEventData.InputButton.Left};
                    control.OnPointerDown(input);control.OnPointerClick(input);
                    Assert.That(view.Session.State.PendingDecision,Is.Not.Null,action.ToString());
                    view.Advance(2);
                    Assert.That(view.Session.State.Decisions.Last().Matched,Is.EqualTo(action==TreeAction.TrueBranch));
                }
            }
            yield return null;
        }
        [UnityTest] public IEnumerator DictionaryFooterWinsRaysOverHiddenSceneAndRestoresInput()
        {
            view.Director.Activate("single");
            var menu=view.transform.Find("UnifiedExperienceMenu");
            var sceneRays=view.GetComponentsInChildren<Oculus.Interaction.RayInteractable>(true)
                .Where(r=>!r.transform.IsChildOf(menu)).ToArray();
            var unused=sceneRays.First(r=>r.GetComponent<Oculus.Interaction.PointableCanvas>()?.Canvas?.name!="LearningConsole");
            unused.enabled=false;
            var saved=sceneRays.ToDictionary(r=>r,r=>r.enabled);
            foreach(string command in new[]{"home","position","tools"}) {
                view.Director.OpenMenu("dictionary");
                // Place the anchored menu behind the now-invisible learning console.
                menu.SetPositionAndRotation(new Vector3(0,1.65f,4),Quaternion.identity);
                yield return null;
                Assert.That(sceneRays.All(r=>!r.enabled),Is.True,"Hidden scene interactables must leave Meta ray selection.");
                var button=menu.GetComponentsInChildren<ExperienceMenuTarget>().Single(b=>b.Command==command);
                var head=view.Garden.Locomotion.Head;
                foreach(float x in new[]{-.25f,.25f}) {
                    var origin=head.position+new Vector3(x,-.4f,.2f);var ray=new Ray(origin,button.transform.position-origin);
                    var hits=Object.FindObjectsByType<Oculus.Interaction.RayInteractable>(FindObjectsSortMode.None)
                        .Where(r=>r.isActiveAndEnabled && r.Surface.Raycast(ray,out _))
                        .OrderBy(r=>{r.Surface.Raycast(ray,out var hit);return hit.Distance;}).ToArray();
                    Assert.That(hits,Is.Not.Empty);
                    Assert.That(hits[0].transform.IsChildOf(menu),Is.True,command);
                }
                var input=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) {
                    pointerId=77,button=UnityEngine.EventSystems.PointerEventData.InputButton.Left,
                    position=head.GetComponent<Camera>().WorldToScreenPoint(button.transform.position)
                };
                var graphics=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
                UnityEngine.EventSystems.EventSystem.current.RaycastAll(input,graphics);
                Assert.That(graphics,Is.Not.Empty);
                Assert.That(graphics[0].gameObject.GetComponentInParent<ExperienceMenuTarget>(),Is.SameAs(button));
                UnityEngine.EventSystems.ExecuteEvents.Execute(graphics[0].gameObject,input,UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler);
                UnityEngine.EventSystems.ExecuteEvents.Execute(graphics[0].gameObject,input,UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
                Assert.That(view.Director.MenuPage,Is.EqualTo(command));
                view.Director.CloseMenu();
                foreach(var item in saved)Assert.That(item.Key.enabled,Is.EqualTo(item.Value),item.Key.name);
            }
        }
        [UnityTest] public IEnumerator ClosedNodeDetailsDoNotInterceptEitherBranchRay()
        {
            view.Director.Activate("single");
            var head=view.Garden.Locomotion.Head;
            var position=head.position;var rotation=head.rotation;
            foreach(var action in new[]{TreeAction.TrueBranch,TreeAction.FalseBranch}) {
                view.Director.Activate("tree-restart");
                var control=view.controls.Single(c=>c.action==action);
                head.position=position-Vector3.up*.3f;
                head.rotation=Quaternion.Euler(0,action==TreeAction.TrueBranch?-20:20,0);
                view.NameTooltip.Show(this,"Synthetic node evidence");
                head.SetPositionAndRotation(position,rotation);
                yield return null;
                Assert.That(view.NameTooltip.Visible,Is.True);
                var reading=view.transform.Find("NodeReadingPanel");
                Assert.That(reading.gameObject.activeInHierarchy,Is.True);
                view.NameTooltip.Hide();
                yield return null;
                foreach(float handX in new[]{-.25f,.25f}) {
                    var origin=position+new Vector3(handX,-.4f,.2f);
                    var ray=new Ray(origin,control.transform.position-origin);
                    var surfaces=Object.FindObjectsByType<Oculus.Interaction.RayInteractable>(FindObjectsSortMode.None)
                        .Where(r=>r.isActiveAndEnabled && r.Surface.Raycast(ray,out _))
                        .OrderBy(r=>{r.Surface.Raycast(ray,out var hit);return hit.Distance;}).ToArray();
                    Assert.That(surfaces,Is.Not.Empty);
                    Assert.That(surfaces[0].GetComponent<Oculus.Interaction.PointableCanvas>().Canvas,
                        Is.SameAs(control.GetComponentInParent<Canvas>()),"A closed evidence panel must not win controller ray selection.");
                }
                var input=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) {
                    pointerId=75,button=UnityEngine.EventSystems.PointerEventData.InputButton.Left,
                    position=head.GetComponent<Camera>().WorldToScreenPoint(control.transform.position)
                };
                var graphics=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
                UnityEngine.EventSystems.EventSystem.current.RaycastAll(input,graphics);
                Assert.That(graphics,Is.Not.Empty);
                Assert.That(graphics[0].gameObject.GetComponentInParent<TreeActionButton>(),Is.SameAs(control));
                var hint=control.GetComponent<ButtonHint>();hint.OnPointerEnter(input);
                Assert.That(hint.Visible,Is.False,"Branch buttons must not show the redundant hover tooltip.");
                UnityEngine.EventSystems.ExecuteEvents.Execute(graphics[0].gameObject,input,UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler);
                UnityEngine.EventSystems.ExecuteEvents.Execute(graphics[0].gameObject,input,UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
                Assert.That(view.Session.State.PendingDecision,Is.Not.Null,action.ToString());
                view.Advance(2);
                Assert.That(view.Session.State.Decisions.Last().Matched,Is.EqualTo(action==TreeAction.TrueBranch));
            }
        }
        [UnityTest] public IEnumerator ResultTreeCanExplicitlySwitchToManualBranches()
        {
            var d=view.Director;d.StartPlayback(true);d.Activate("calculate-all");d.Activate("results-forest");
            view.Garden.Navigation.OpenTree(1);d.OpenMenu("tree");d.Activate("manual-tree");
            Assert.That(view.Ensemble.Index,Is.EqualTo(1));Assert.That(view.Comparison,Is.Null);
            Assert.That(view.Session.State.Mode,Is.EqualTo(VRExperienceAGB.Application.ExperienceMode.Manual));
            Assert.That(d.PlayingTour,Is.False);Assert.That(d.MenuVisible,Is.False);
            Assert.That(view.Session.State.AtLeaf,Is.False);Assert.That(view.controls.Where(c=>c.action==TreeAction.TrueBranch||c.action==TreeAction.FalseBranch).All(c=>c.gameObject.activeInHierarchy),Is.True);
            yield return null;
        }
        [UnityTest] public IEnumerator DefaultForestWalkingAndHowlAreAvailableThroughNormalMenu()
        {
            var d=view.Director;d.Activate("forest");var movement=view.Garden.Locomotion;
            Assert.That(movement.WalkEnabled,Is.True);
            var before=movement.Origin.position;movement.Move(Vector2.up,.1f);
            Assert.That(movement.Origin.position,Is.Not.EqualTo(before));
            var wildlife=view.Garden.M5.Atmosphere.Wildlife;
            wildlife.SendMessage("OnApplicationFocus",true);wildlife.SendMessage("OnApplicationPause",false);
            d.OpenMenu("comfort");d.Activate("hear-wolves");
            Assert.That(d.MenuVisible,Is.False);Assert.That(view.Garden.Navigation.Page,Is.EqualTo(NavigationPage.Forest));
            Assert.That(wildlife.CanPlay,Is.True);Assert.That(wildlife.Voices.Any(v=>v.isPlaying),Is.True);
            d.OpenMenu("home");
            Assert.That(view.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains("Synthetic fixture")&&t.text.Contains("trees")),Is.True);
            yield return null;
        }
        [UnityTest] public IEnumerator StepAndFastPacesProduceTheSameCompletePair()
        {
            var d=view.Director;d.SetPace(PlaybackPace.Step);d.StartPlayback(true);var tree=d.Playback.TreeIndex;var step=d.Playback.StepIndex;
            bool seated=view.IsSeated;d.OpenMenu("comfort");d.Activate("seated");Assert.That(view.IsSeated,Is.Not.EqualTo(seated));d.CloseMenu();
            d.AdvanceTour(100);Assert.That(d.Playback.TreeIndex,Is.EqualTo(tree));Assert.That(d.Playback.StepIndex,Is.EqualTo(step));
            d.Activate("calculate-all");var a=d.Playback.A.RawScore;var b=d.Playback.B.RawScore;
            d.SetPace(PlaybackPace.Fast);d.StartPlayback(true);d.Activate("calculate-all");
            Assert.That(d.Playback.A.RawScore,Is.EqualTo(a));Assert.That(d.Playback.B.RawScore,Is.EqualTo(b));yield return null;
        }
        [UnityTest] public IEnumerator SearchKeyboardPreservesCompareContextAndSupportsNoMatches()
        {
            var d=view.Director;d.OpenMenu("compare");d.Activate("values");d.Activate("values-search");
            Assert.That(d.MenuPage,Is.EqualTo("text-entry"));d.Activate("entry-key:25");d.Activate("entry-apply");yield return null;
            Assert.That(d.ValueSearch,Is.EqualTo("Z"));d.Activate("values-back");Assert.That(d.MenuPage,Is.EqualTo("compare"));
            d.OpenMenu("tools");d.Activate("tree-jump");d.Activate("entry-key:0");d.Activate("entry-apply");Assert.That(d.MenuPage,Is.EqualTo("text-entry"));
            d.Activate("entry-clear");d.Activate("entry-key:1");d.Activate("entry-apply");Assert.That(view.Garden.Navigation.Page,Is.EqualTo(NavigationPage.Tree));
        }
        [UnityTest] public IEnumerator PinnedEvidenceStaysVisibleAfterPointerLeaves()
        {
            view.Garden.Navigation.OpenTree(0);var tooltip=view.NameTooltip;var owner=new object();
            tooltip.Show(owner,view.Garden.M6.Detail(view.Session.State.NodeId));tooltip.TogglePin();tooltip.Leave(owner);
            yield return new WaitForSecondsRealtime(.8f);Assert.That(tooltip.Visible&&tooltip.Pinned,Is.True);
            view.Director.OpenMenu("home");Assert.That(tooltip.Visible,Is.False);Assert.That(tooltip.Pinned,Is.False);
        }
        [UnityTest] public IEnumerator GroupedTableRetainsCompleteModelAndEvaluation()
        {
            var original=view.Model;var trees=Enumerable.Range(0,51).Select(i=>new ModelTree("tree-"+i,original.Trees[0].RootId,1,original.Trees[0].Nodes)).ToArray();
            var model=new ModelDefinition(1,"group-fixture","synthetic","binary_logistic","response",0,original.Features,trees);
            var profiles=new ProfileSet(1,model.Id,view.AvailableProfiles.Profiles.Select(p=>new PreparedProfile(1,model.Id,p.Id,p.DisplayName,p.Values)));
            Assert.That(view.OpenImportedModel(new ImportedModel(model),profiles,"Group fixture").IsSuccess,Is.True);
            var d=view.Director;d.StartPlayback(true);d.Activate("calculate-all");d.ShowResults(true);var score=d.Playback.A.RawScore;
            var table=view.Garden.M5.Diorama;table.NextGroup();yield return null;
            Assert.That(table.ModelRoot.GetComponentsInChildren<MeshFilter>().Count(m=>m.name=="Pine"),Is.EqualTo(50));
            table.NextGroup();yield return null;
            Assert.That(table.ModelRoot.GetComponentsInChildren<MeshFilter>().Count(m=>m.name=="Pine"),Is.EqualTo(1));
            Assert.That(d.Playback.A.Trees.Count,Is.EqualTo(51));Assert.That(d.Playback.A.RawScore,Is.EqualTo(score));
            table.NextGroup();yield return null;Assert.That(table.ModelRoot.GetComponentsInChildren<MeshFilter>().Count(m=>m.name=="Pine"),Is.EqualTo(51));
        }
        [UnityTest] public IEnumerator UnboundMetadataIsRemovedFromEveryPresentationPath()
        {
            string path=Path.Combine(UnityEngine.Application.dataPath,"../../data/examples/demo-agb-export.json");
            var preview=AgbStructurePreview.Read(File.ReadAllText(path),"Synthetic evidence").Value;
            var result=view.OpenImportedModel(new ImportedModel(view.Model,preview),view.AvailableProfiles,"No source fingerprint");
            Assert.That(result.IsSuccess,Is.True);Assert.That(view.ExportPreview,Is.Null);Assert.That(view.ExportSourceSha256,Is.Null);
            yield return null;
        }
    }
}
