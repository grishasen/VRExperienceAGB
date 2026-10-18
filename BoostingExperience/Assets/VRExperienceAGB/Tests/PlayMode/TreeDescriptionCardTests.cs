using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRExperienceAGB.Domain;
using VRExperienceAGB.Import;
using VRExperienceAGB.Presentation;

namespace VRExperienceAGB.Tests
{
    public class TreeDescriptionCardTests
    {
        private OneTreeExperience view;
        [UnitySetUp] public IEnumerator Open()
        {
            yield return SceneManager.LoadSceneAsync("OneTreeLearning"); yield return null;
            view=Object.FindAnyObjectByType<OneTreeExperience>();
            view.enabled=false;view.Director.enabled=false;view.GetComponent<DesktopTreePreview>().enabled=false;
            string data=Path.Combine(UnityEngine.Application.dataPath,"../../data/examples");
            var model=NormalizedModelJson.ReadModel(File.ReadAllText(Path.Combine(data,"demo-model.json"))).Value;
            var profiles=NormalizedModelJson.ReadProfiles(File.ReadAllText(Path.Combine(data,"demo-profiles.json")),model).Value;
            Assert.That(view.OpenImportedModel(new ImportedModel(model),profiles,"Synthetic fixture").IsSuccess,Is.True);
            view.SendMessage("OnApplicationFocus",true);view.SendMessage("OnApplicationPause",false);
            view.Director.Activate("forest");yield return null;
        }

        [UnityTest] public IEnumerator FirstClickShowsChoicesWithoutHoverSelectionOrRouteChanges()
        {
            var garden=view.Garden;garden.Hover(0);Assert.That(garden.DescriptionCard.Visible,Is.False);OpenChoices(0);var card=garden.DescriptionCard;
            var selected=view.Ensemble.Index;var score=view.Ensemble.RouteTotal;
            var head=garden.Locomotion.Head;var position=head.localPosition;var rotation=head.localRotation;
            Assert.That(card.GetComponentsInChildren<TMP_Text>().All(t=>t.font.name.StartsWith("Inter-")),Is.True);
            Assert.That(card.GetComponentsInChildren<TMP_Text>().All(t=>!t.enableAutoSizing),Is.True);
            Assert.That(card.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("gain")),Is.False);
            var input=new PointerEventData(EventSystem.current){pointerId=76,button=PointerEventData.InputButton.Left};
            var cardPosition=card.transform.position;var cardRotation=card.transform.rotation;
            garden.ClearHover(0);garden.Hover(1);Assert.That(card.Index,Is.Zero,"Hover cannot retarget an explicit choice.");
            head.localRotation=Quaternion.Euler(0,12,0)*rotation;
            yield return new WaitForSecondsRealtime(1);
            Assert.That(card.Visible,Is.True,"Moving from crown to the card must allow reading and clicking.");
            Assert.That(card.transform.position,Is.EqualTo(cardPosition));Assert.That(card.transform.rotation,Is.EqualTo(cardRotation));
            head.localRotation=rotation;
            var button=card.GetComponentsInChildren<TreeCardButton>().Single(t=>!t.Explore && !t.Close && t.PageDelta==0);
            Canvas.ForceUpdateCanvases();
            foreach(float hand in new[]{-.2f,.2f}) {
                var origin=head.position+new Vector3(hand,-.35f,.15f);
                var ray=new Ray(origin,button.transform.position-origin);
                var surfaces=Object.FindObjectsByType<Oculus.Interaction.RayInteractable>(FindObjectsSortMode.None)
                    .Where(r=>r.isActiveAndEnabled && r.Surface.Raycast(ray,out _))
                    .OrderBy(r=>{r.Surface.Raycast(ray,out var hit);return hit.Distance;}).ToArray();
                Assert.That(surfaces,Is.Not.Empty);
                Assert.That(surfaces[0].GetComponent<Oculus.Interaction.PointableCanvas>().Canvas,Is.SameAs(card.GetComponent<Canvas>()));
            }
            input.position=head.GetComponent<Camera>().WorldToScreenPoint(button.transform.position);
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(input,hits);
            Assert.That(hits,Is.Not.Empty);
            Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject),Is.EqualTo(button.gameObject));
            button.OnPointerDown(input);button.OnPointerClick(input);
            Assert.That(view.Director.MenuVisible,Is.False);Assert.That(card.Visible,Is.True);Assert.That(card.DetailsVisible,Is.True);
            Assert.That(card.transform.position,Is.EqualTo(cardPosition));Assert.That(card.transform.rotation,Is.EqualTo(cardRotation));
            var reading=card.DetailsText;
            foreach(var feature in view.Model.Trees[0].Nodes.OfType<SplitNode>().Select(n=>n.FeatureId)) Assert.That(reading.text,Does.Contain(feature));
            Assert.That(reading.text,Does.Contain("ROOT CONDITION").And.Contain("Gain"));
            Assert.That(reading.text,Does.Not.Contain("SHA-256"));
            Assert.That(reading.font.name,Is.EqualTo("Inter-Medium"));
            int pages=reading.textInfo.pageCount;Assert.That(pages,Is.GreaterThan(1));
            AssertInlineReadingFits(card);
            for(int page=2;page<=pages;page++) {
                var next=card.GetComponentsInChildren<TreeCardButton>().Single(b=>b.PageDelta==1);
                next.OnPointerDown(input);next.OnPointerClick(input);
                Assert.That(reading.pageToDisplay,Is.EqualTo(page));
                AssertInlineReadingFits(card);
            }
            button.OnPointerDown(input);button.OnPointerClick(input);Assert.That(card.DetailsVisible,Is.False);
            Assert.That(card.transform.position,Is.EqualTo(cardPosition));
            card.Hide();
            Assert.That(view.Ensemble.Index,Is.EqualTo(selected));Assert.That(view.Ensemble.RouteTotal,Is.EqualTo(score));
            Assert.That(head.localPosition,Is.EqualTo(position));Assert.That(head.localRotation,Is.EqualTo(rotation));
            Assert.That(card.GetComponentsInChildren<Oculus.Interaction.RayInteractable>(true).All(r=>!r.isActiveAndEnabled),Is.True);
        }

        [UnityTest] public IEnumerator CrownHoverNeverCreatesAnEmptyHintAndBothPointersCanOpenChoices()
        {
            var target=view.transform.Find("ModelPineGarden/ModelTree-1/PineCrownTarget-1").GetComponentInChildren<GardenPointerTarget>();
            int hintCount=view.GetComponentsInChildren<Canvas>(true).Count(c=>c.name=="ButtonHint");
            foreach(int pointer in new[]{81,82}) {
                var input=new PointerEventData(EventSystem.current){pointerId=pointer,button=PointerEventData.InputButton.Left};
                ExecuteEvents.Execute(target.gameObject,input,ExecuteEvents.pointerEnterHandler);
                Assert.That(target.GetComponent<ButtonHint>().Visible,Is.False);
                Assert.That(target.GetComponent<UnityEngine.UI.Outline>(),Is.Null);
                Assert.That(view.GetComponentsInChildren<Canvas>(true).Count(c=>c.name=="ButtonHint"),Is.EqualTo(hintCount));
                Assert.That(view.Garden.DescriptionCard.Visible,Is.False);
                ExecuteEvents.Execute(target.gameObject,input,ExecuteEvents.pointerDownHandler);
                ExecuteEvents.Execute(target.gameObject,input,ExecuteEvents.pointerClickHandler);
                Assert.That(view.Garden.DescriptionCard.Visible,Is.True);
                Assert.That(view.Garden.DescriptionCard.Index,Is.Zero);
                ExecuteEvents.Execute(target.gameObject,input,ExecuteEvents.pointerExitHandler);
                view.Garden.DescriptionCard.Hide();
            }
            yield return null;
        }

        [UnityTest] public IEnumerator RetargetedPressCannotOpenWrongTreeAndExploreStillWorks()
        {
            var garden=view.Garden;garden.Hover(0);Assert.That(garden.DescriptionCard.Visible,Is.False);OpenChoices(0);var card=garden.DescriptionCard;
            var details=card.GetComponentsInChildren<TreeCardButton>().Single(t=>!t.Explore && !t.Close && t.PageDelta==0);
            var input=new PointerEventData(EventSystem.current){pointerId=77,button=PointerEventData.InputButton.Left};
            details.OnPointerDown(input);OpenChoices(1);details.OnPointerClick(input);
            Assert.That(view.Director.MenuVisible,Is.False,"An old press cannot open the new tree's details.");
            var explore=card.GetComponentsInChildren<TreeCardButton>().Single(t=>t.Explore);
            explore.OnPointerDown(input);explore.OnPointerClick(input);
            Assert.That(garden.Navigation.Page,Is.EqualTo(NavigationPage.Tree));Assert.That(view.Ensemble.Index,Is.EqualTo(1));
            Assert.That(view.Ensemble.RouteTotal,Is.Zero);Assert.That(card.Visible,Is.False);
            yield return null;
        }

        private static void AssertInlineReadingFits(TreeDescriptionCard card)
        {
            var reading=card.DetailsText;reading.ForceMeshUpdate();
            var next=card.GetComponentsInChildren<TreeCardButton>().Single(b=>b.PageDelta==1).GetComponent<RectTransform>();
            Assert.That(reading.rectTransform.anchoredPosition.y+reading.rectTransform.rect.yMin,Is.GreaterThan(next.anchoredPosition.y+next.rect.yMax+12));
            foreach(var c in reading.textInfo.characterInfo.Take(reading.textInfo.characterCount).Where(c=>c.isVisible&&c.pageNumber==reading.pageToDisplay-1)) {
                Assert.That(c.bottomLeft.x,Is.GreaterThanOrEqualTo(reading.rectTransform.rect.xMin-2));
                Assert.That(c.topRight.x,Is.LessThanOrEqualTo(reading.rectTransform.rect.xMax+2));
                Assert.That(c.bottomLeft.y,Is.GreaterThanOrEqualTo(reading.rectTransform.rect.yMin-2));
            }
        }

        [UnityTest] public IEnumerator MenusUseInterAndPreserveFullDetailsWithoutMovingDuringReading()
        {
            var d=view.Director;var head=view.Garden.Locomotion.Head;var pose=head.localPosition;var rotation=head.localRotation;
            foreach(string page in new[]{"home","library","upload","models","profile-files","tree","position","profiles","compare","values","playback","tools","table-tools","help","comfort","learn","model-info","passport","structure","dictionary","glossary","verification","performance","library-help"}) {
                d.OpenMenu(page);yield return null;
                var menu=view.transform.Find("UnifiedExperienceMenu");
                foreach(var label in menu.GetComponentsInChildren<TMP_Text>()) {
                    Assert.That(label.font.name,Does.StartWith("Inter-"),page+" / "+label.name);
                    Assert.That(label.enableAutoSizing,Is.False,page+" / "+label.name);
                }
                var before=menu.position;
                d.Activate("page-details");yield return null;
                Assert.That(menu.position,Is.EqualTo(before));
                Assert.That(menu.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="Reading").text,Does.Contain(view.Model.Id));
                d.Activate("details-back");Assert.That(d.MenuPage,Is.EqualTo(page));
            }
            d.CloseMenu();head.localPosition=pose+new Vector3(1,0,1);head.localRotation=Quaternion.Euler(15,120,0);
            d.OpenMenu();var anchored=view.transform.Find("UnifiedExperienceMenu").position;
            Assert.That(Vector3.Dot((anchored-head.position).normalized,head.forward),Is.GreaterThan(.99f));
            head.localRotation=Quaternion.Euler(0,170,0);d.SendMessage("LateUpdate");
            Assert.That(view.transform.Find("UnifiedExperienceMenu").position,Is.EqualTo(anchored));
            d.CloseMenu();head.localPosition=pose;head.localRotation=rotation;
        }

        [UnityTest] public IEnumerator SourceLegendAndBaseStripsShareStableColorsAcrossModels()
        {
            var garden=view.Garden;
            garden.M6.RefreshVisibility();Assert.That(garden.M6.Legend.Visible,Is.True);
            var legend=view.transform.Find("SourceLegend");
            Assert.That(garden.M6.Legend.Expanded,Is.True);
            Assert.That(legend.GetComponentsInChildren<TMP_Text>().Any(t=>t.name.StartsWith("Source")),Is.True);
            var toggle=legend.GetComponentsInChildren<SourceLegendTarget>().Single(t=>t.Delta==0);
            var input=new PointerEventData(EventSystem.current){pointerId=87,button=PointerEventData.InputButton.Left};
            var position=legend.position;var rotation=legend.rotation;
            var offset=Quaternion.Inverse(rotation)*(position-garden.Locomotion.Head.position);
            Assert.That(offset.x,Is.LessThan(0));Assert.That(offset.y,Is.LessThan(0));
            toggle.OnPointerDown(input);toggle.OnPointerClick(input);
            Assert.That(garden.M6.Legend.Expanded,Is.False);
            Assert.That(legend.GetComponentsInChildren<TMP_Text>().Any(t=>t.name.StartsWith("Source")),Is.False);
            toggle.OnPointerDown(input);toggle.OnPointerClick(input);
            Assert.That(garden.M6.Legend.Expanded,Is.True);
            Assert.That(legend.position,Is.EqualTo(position));Assert.That(legend.rotation,Is.EqualTo(rotation));
            Assert.That(((RectTransform)legend).rect.height,Is.EqualTo(64));
            foreach(var label in legend.GetComponentsInChildren<TMP_Text>().Where(t=>t.name.StartsWith("Source")))
                Assert.That(label.color,Is.EqualTo(PredictorPalette.ForFamily(label.text)));
            foreach(var canvas in garden.GetComponentsInChildren<Canvas>(true).Where(c=>c.name=="PredictorComposition"))
                foreach(var image in canvas.GetComponentsInChildren<UnityEngine.UI.Image>())
                    Assert.That(image.color,Is.EqualTo(PredictorPalette.ForFamily(image.name)));
            Assert.That(PredictorPalette.ForFeature("Customer.Age"),Is.EqualTo((Color)new Color32(103,213,245,255)));
            Assert.That(PredictorPalette.ForFeature("pyChannel"),Is.EqualTo(PredictorPalette.ForFamily("py")));
            var unknown=PredictorPalette.ForFeature("NewSource.Signal");
            Color.RGBToHSV(unknown,out _,out float saturation,out _);Assert.That(saturation,Is.GreaterThan(.4f));
            var export=File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,"../../data/examples/demo-agb-export.json"))
                .Replace("Customer.DigitalVisits30Days","NewSource.Signal");
            Assert.That(view.LoadAgbStructure(export,"Synthetic source palette").IsSuccess,Is.True);
            view.Director.Activate("forest");garden.M6.RefreshVisibility();yield return null;
            Assert.That(PredictorPalette.ForFeature("NewSource.Other"),Is.EqualTo(unknown));
            var source=legend.GetComponentsInChildren<TMP_Text>().Single(t=>t.text=="NewSource");Assert.That(source.color,Is.EqualTo(unknown));
            var strips=garden.GetComponentsInChildren<Canvas>(true).Where(c=>c.name=="PredictorComposition").ToArray();
            var segment=strips.SelectMany(c=>c.GetComponentsInChildren<UnityEngine.UI.Image>()).First(i=>i.name=="NewSource");
            Assert.That(segment.color,Is.EqualTo(source.color));
            view.Director.OpenMenu();garden.M6.RefreshVisibility();Assert.That(garden.M6.Legend.Visible,Is.False);
            view.Director.CloseMenu();garden.M6.RefreshVisibility();Assert.That(garden.M6.Legend.Visible,Is.True);
            toggle.OnPointerDown(input);toggle.OnPointerClick(input);
            Assert.That(garden.M6.Legend.Expanded,Is.False);
            Assert.That(((RectTransform)legend).rect.width,Is.EqualTo(180),"The hidden legend must not leave a wide ray surface.");
            Assert.That(legend.GetComponentsInChildren<TMP_Text>().Any(t=>t.name.StartsWith("Source")||t.name.StartsWith("Swatch")),Is.False);
        }

        [UnityTest] public IEnumerator LegendArrowPagesAllSourcesAndRejectsHiddenPresses()
        {
            var features=new List<FeatureDefinition>();var trees=new List<ModelTree>();
            for(int i=0;i<9;i++) {
                string id="Source"+i+".Value";features.Add(new FeatureDefinition(id,FeatureKind.Number,true));
                trees.Add(new ModelTree("tree"+i,"root",1,new ModelNode[]{
                    new SplitNode("root",id,new SplitCondition(DecisionOperator.LessThan,1),"a","b"),
                    new LeafNode("a",-.1),new LeafNode("b",.1)}));
            }
            var model=new ModelDefinition(1,"legend-pages","synthetic","binary_logistic","response",0,features,trees);
            Assert.That(view.OpenImportedModel(new ImportedModel(model),new ProfileSet(1,model.Id,System.Array.Empty<PreparedProfile>()),"Synthetic legend pages").IsSuccess,Is.True);
            view.Director.Activate("forest");view.Garden.M6.RefreshVisibility();yield return null;
            if(view.Garden.M6.Legend.Expanded)view.Garden.M6.Legend.Toggle();
            var root=(RectTransform)view.transform.Find("SourceLegend");var head=view.Garden.Locomotion.Head;
            var toggle=root.Find("ToggleSources").GetComponent<SourceLegendTarget>();
            var next=root.Find("Next").GetComponent<SourceLegendTarget>();
            var input=new PointerEventData(EventSystem.current){pointerId=88,button=PointerEventData.InputButton.Left};
            var anchored=root.position;
            Canvas.ForceUpdateCanvases();
            foreach(float hand in new[]{-.2f,.2f}) {
                var origin=head.position+new Vector3(hand,-.35f,.15f);var ray=new Ray(origin,toggle.transform.position-origin);
                var surfaces=Object.FindObjectsByType<Oculus.Interaction.RayInteractable>(FindObjectsSortMode.None)
                    .Where(r=>r.isActiveAndEnabled&&r.Surface.Raycast(ray,out _))
                    .OrderBy(r=>{r.Surface.Raycast(ray,out var hit);return hit.Distance;}).ToArray();
                Assert.That(surfaces,Is.Not.Empty);
                Assert.That(surfaces[0].GetComponent<Oculus.Interaction.PointableCanvas>().Canvas,Is.SameAs(root.GetComponent<Canvas>()));
            }
            input.position=head.GetComponent<Camera>().WorldToScreenPoint(toggle.transform.position);
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(input,hits);
            Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject),Is.EqualTo(toggle.gameObject));
            toggle.OnPointerDown(input);toggle.OnPointerClick(input);
            var seen=new HashSet<string>();
            for(int page=1;page<=3;page++) {
                Assert.That(root.Find("Page").GetComponent<TMP_Text>().text,Is.EqualTo(page+" / 3"));
                foreach(var label in root.GetComponentsInChildren<TMP_Text>().Where(t=>t.name.StartsWith("Source"))) {
                    Assert.That(label.color,Is.EqualTo(PredictorPalette.ForFamily(label.text)));seen.Add(label.text);
                }
                Assert.That(root.position,Is.EqualTo(anchored));
                if(page<3){next.OnPointerDown(input);next.OnPointerClick(input);}
            }
            Assert.That(seen.Count,Is.EqualTo(9));Assert.That(view.Model.Trees.Count,Is.EqualTo(9));
            Assert.That(next.GetComponent<UnityEngine.UI.Button>().IsInteractable(),Is.False);
            var previous=root.Find("Previous").GetComponent<SourceLegendTarget>();previous.OnPointerDown(input);
            toggle.OnPointerDown(input);toggle.OnPointerClick(input);toggle.OnPointerDown(input);toggle.OnPointerClick(input);
            previous.OnPointerClick(input);Assert.That(root.Find("Page").GetComponent<TMP_Text>().text,Is.EqualTo("3 / 3"));
            toggle.OnPointerDown(input);toggle.OnPointerClick(input);
            Assert.That(root.sizeDelta,Is.EqualTo(new Vector2(180,64)));
        }

        private void OpenChoices(int index)
        {
            var target=view.Garden.GetComponentsInChildren<GardenPointerTarget>().First(t=>t.command==GardenCommand.SelectTree && t.index==index);
            var input=new PointerEventData(EventSystem.current){pointerId=79,button=PointerEventData.InputButton.Left};
            target.OnPointerDown(input);target.OnPointerClick(input);
            Assert.That(view.Garden.Navigation.Page,Is.EqualTo(NavigationPage.Forest));
            Assert.That(view.Garden.DescriptionCard.Visible,Is.True);
        }

        [UnityTest] public IEnumerator CompactSplitKeepsFullConditionsInDetailsAndTransparentStoneTargets()
        {
            var head=view.Garden.Locomotion.Head;var pose=head.localPosition;var rotation=head.localRotation;
            foreach(int tree in new[]{0,1,2}) {
                view.Garden.Navigation.ShowForest();view.Garden.Navigation.OpenTree(tree);
                if(!view.Garden.M6.Legend.Expanded)view.Garden.M6.Legend.Toggle();
                var state=view.Session.State;var split=(SplitNode)view.Session.CurrentNode;double total=view.Ensemble.RouteTotal;
                view.NameTooltip.ShowNode(this,split.Id);var card=view.NameTooltip.CompactDescription;
                Assert.That(card.GetComponentsInChildren<TMP_Text>().All(t=>t.font.name.StartsWith("Inter-")&&!t.enableAutoSizing),Is.True);
                Assert.That(card.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("Gain")||t.text.Contains("sampleCount")),Is.False);
                Assert.That(card.transform.Find("Source").GetComponent<TMP_Text>().text,Is.EqualTo(tree==0?"IH":tree==1?"py":"Customer"));
                if(tree==2)Assert.That(card.PredicateText.text,Is.EqualTo("Missing"));
                view.NameTooltip.TogglePin();view.NameTooltip.Leave(this);
                yield return new WaitForSecondsRealtime(.8f);Assert.That(view.NameTooltip.Visible,Is.True);
                Canvas.ForceUpdateCanvases();
                AssertBranchTargetsClear();
                var details=card.GetComponentsInChildren<SplitDescriptionButton>().Single(b=>!b.IsPin&&!b.Close&&b.PageDelta==0);
                var input=new PointerEventData(EventSystem.current){pointerId=82,button=PointerEventData.InputButton.Left};
                var summaryPosition=card.GetComponentInParent<Canvas>().transform.position;
                details.OnPointerDown(input);details.OnPointerClick(input);
                Assert.That(card.GetComponentInParent<Canvas>().transform.position,Is.EqualTo(summaryPosition));
                Assert.That(view.Director.MenuVisible,Is.False);Assert.That(view.NameTooltip.Visible,Is.True);
                Assert.That(card.DetailsVisible,Is.True);view.NameTooltip.UseCompactLayout();
                Canvas.ForceUpdateCanvases();AssertBranchTargetsClear();
                view.NameTooltip.ShowNode(this,view.Model.Trees[tree].Nodes.OfType<LeafNode>().First().Id);
                Assert.That(card.NodeId,Is.EqualTo(split.Id),"Incidental hover cannot replace explicit reading.");
                var canvas=card.GetComponentInParent<Canvas>();var cardPosition=canvas.transform.position;var cardRotation=canvas.transform.rotation;
                Assert.That(((RectTransform)canvas.transform).sizeDelta,Is.EqualTo(new Vector2(580,640)));
                var reading=card.DetailsText;
                Assert.That(reading.text,Is.EqualTo(view.Garden.M6.Analysis.NodeDetail(tree,split.Id).Replace("SHA-256: not supplied\n","")));
                Assert.That(reading.text,Does.Contain(split.FeatureId).And.Contain("SEGMENT").And.Contain("Gain"));
                foreach(string value in split.Condition.Categories)Assert.That(reading.text,Does.Contain(value));
                view.NameTooltip.TogglePin();view.NameTooltip.Leave(this);
                yield return new WaitForSecondsRealtime(.8f);
                Assert.That(view.NameTooltip.Visible,Is.True,"Explicit Details must remain available without hover or pinning.");
                int pages=reading.textInfo.pageCount;Assert.That(pages,Is.GreaterThan(1));
                var next=card.GetComponentsInChildren<SplitDescriptionButton>().Single(b=>b.PageDelta==1);
                var previous=card.GetComponentsInChildren<SplitDescriptionButton>().Single(b=>b.PageDelta==-1);
                Assert.That(previous.GetComponent<UnityEngine.UI.Button>().IsInteractable(),Is.False);
                for(int page=1;page<=pages;page++) {
                    AssertReadingFits(reading,next.GetComponent<RectTransform>());
                    Assert.That(reading.pageToDisplay,Is.EqualTo(page));
                    Assert.That(canvas.transform.position,Is.EqualTo(cardPosition));Assert.That(canvas.transform.rotation,Is.EqualTo(cardRotation));
                    if(page<pages){next.OnPointerDown(input);next.OnPointerClick(input);}
                }
                Assert.That(next.GetComponent<UnityEngine.UI.Button>().IsInteractable(),Is.False);
                next.OnPointerDown(input);next.OnPointerClick(input);Assert.That(reading.pageToDisplay,Is.EqualTo(pages));
                details.OnPointerDown(input);details.OnPointerClick(input);
                Assert.That(card.DetailsVisible,Is.False);Assert.That(card.PredicateText.gameObject.activeInHierarchy,Is.True);
                Assert.That(((RectTransform)canvas.transform).sizeDelta,Is.EqualTo(new Vector2(580,400)));
                Assert.That(canvas.transform.position,Is.EqualTo(cardPosition));
                details.OnPointerDown(input);details.OnPointerClick(input);Assert.That(reading.pageToDisplay,Is.EqualTo(1));
                var close=card.GetComponentsInChildren<SplitDescriptionButton>().Single(b=>b.Close);
                close.OnPointerDown(input);close.OnPointerClick(input);
                Assert.That(view.NameTooltip.Visible,Is.False);
                Assert.That(canvas.GetComponentsInChildren<Oculus.Interaction.RayInteractable>(true).All(r=>!r.isActiveAndEnabled),Is.True);
                Assert.That(view.Session.State.NodeId,Is.EqualTo(state.NodeId));Assert.That(view.Ensemble.RouteTotal,Is.EqualTo(total));
                Assert.That(head.localPosition,Is.EqualTo(pose));Assert.That(head.localRotation,Is.EqualTo(rotation));
            }
        }

        [UnityTest] public IEnumerator SourceColorAndScoreTagRemainDistinctAndStaleSplitPressIsRejected()
        {
            string data=Path.Combine(UnityEngine.Application.dataPath,"../../data/examples");
            var model=NormalizedModelJson.ReadModel(File.ReadAllText(Path.Combine(data,"demo-model.json")).Replace("Customer.RelationshipMonths","Customer.Scores.Value")).Value;
            var profiles=NormalizedModelJson.ReadProfiles(File.ReadAllText(Path.Combine(data,"demo-profiles.json")).Replace("Customer.RelationshipMonths","Customer.Scores.Value"),model).Value;
            Assert.That(view.OpenImportedModel(new ImportedModel(model),profiles,"Synthetic score-tag fixture").IsSuccess,Is.True);
            view.Director.Activate("forest");view.Garden.Navigation.OpenTree(0);
            view.NameTooltip.ShowNode(this,"t1-value");var card=view.NameTooltip.CompactDescription;
            Assert.That(card.transform.Find("Source").GetComponent<TMP_Text>().color,Is.EqualTo((Color)new Color32(103,213,245,255)));
            Assert.That(card.transform.Find("ScoreTag").gameObject.activeInHierarchy,Is.True);
            Assert.That(card.transform.Find("ScoreTag").GetComponent<TMP_Text>().color,Is.EqualTo((Color)new Color32(236,168,201,255)));
            var details=card.GetComponentsInChildren<SplitDescriptionButton>().Single(b=>!b.IsPin&&!b.Close&&b.PageDelta==0);
            var input=new PointerEventData(EventSystem.current){pointerId=83,button=PointerEventData.InputButton.Left};
            details.OnPointerDown(input);view.NameTooltip.ShowNode(this,"t1-visits");details.OnPointerClick(input);
            Assert.That(view.Director.MenuVisible,Is.False);Assert.That(card.DetailsVisible,Is.False);
            view.NameTooltip.Hide();view.Garden.Navigation.ShowForest();OpenChoices(0);
            var close=view.Garden.DescriptionCard.GetComponentsInChildren<TreeCardButton>().Single(b=>b.Close);
            close.OnPointerDown(input);close.OnPointerClick(input);Assert.That(view.Garden.DescriptionCard.Visible,Is.False);
            Assert.That(view.Ensemble.RouteTotal,Is.Zero);yield return null;
        }

        [UnityTest] public IEnumerator ControllerRaysOpenSplitCardThroughMetaCanvasAndReturnFromDetails()
        {
            view.Garden.Navigation.OpenTree(0);yield return null;
            var head=view.Garden.Locomotion.Head;var headPosition=head.position;var headRotation=head.rotation;
            var total=view.Ensemble.RouteTotal;
            var module=Object.FindAnyObjectByType<Oculus.Interaction.PointableCanvasModule>();
            float reach=Object.FindObjectsByType<Oculus.Interaction.RayInteractor>(FindObjectsInactive.Include,FindObjectsSortMode.None)
                .Where(r=>r.name=="ControllerRayInteractor").Min(r=>r.MaxRayLength);
            foreach(bool reopenMenu in new[]{false,true}) {
                if(reopenMenu) {
                    view.Director.OpenMenu();view.Director.SendMessage("LateUpdate");yield return null;
                    view.Director.CloseMenu();yield return null;
                }
                foreach(float hand in new[]{-.2f,.2f})foreach(var node in view.nodeViews.Take(3)) {
                    var canvas=node.title.GetComponentInParent<Canvas>();
                    var points=new List<Vector3>{node.title.transform.position};
                    if(node==view.nodeViews[0]) {
                        // The source heading used to extend past the canvas's top ray boundary.
                        points.Add(node.title.transform.TransformPoint(new Vector3(0,node.title.rectTransform.rect.yMax-8,0)));
                        var backing=(RectTransform)canvas.transform.Find("QuietLabelBacking");
                        points.Add(backing.TransformPoint(new Vector3(backing.rect.xMax-4,0,0)));
                    }
                    foreach(var point in points) {
                        Canvas.ForceUpdateCanvases();
                        var hit=ControllerHit(head.position+new Vector3(hand,-.35f,.15f),point,reach,canvas);
                        SendMetaPointer(module,hit.target,941,Oculus.Interaction.PointerEventType.Hover,hit.pose);
                        Assert.That(view.NameTooltip.Visible,Is.True,"A routed Meta hover must open the inspected node.");
                        Assert.That(view.NameTooltip.CompactDescription.NodeId,Is.EqualTo(node.nodeId));
                        SendMetaPointer(module,hit.target,941,Oculus.Interaction.PointerEventType.Unhover,hit.pose);
                        view.NameTooltip.Hide();
                    }
                }
            }
            var root=view.nodeViews[0];var rootCanvas=root.title.GetComponentInParent<Canvas>();
            var rootHit=ControllerHit(head.position+new Vector3(.2f,-.35f,.15f),root.title.transform.position,reach,rootCanvas);
            SendMetaPointer(module,rootHit.target,942,Oculus.Interaction.PointerEventType.Hover,rootHit.pose);
            yield return null;Canvas.ForceUpdateCanvases();
            var card=view.NameTooltip.CompactDescription;var readingCanvas=card.GetComponentInParent<Canvas>();
            var anchored=readingCanvas.transform.position;
            SendMetaPointer(module,rootHit.target,942,Oculus.Interaction.PointerEventType.Unhover,rootHit.pose);
            var details=card.GetComponentsInChildren<SplitDescriptionButton>().Single(b=>!b.IsPin&&!b.Close&&b.PageDelta==0);
            var cardHit=ControllerHit(head.position+new Vector3(.2f,-.35f,.15f),details.transform.position,reach,readingCanvas);
            SendMetaPointer(module,cardHit.target,942,Oculus.Interaction.PointerEventType.Hover,cardHit.pose);
            SendMetaPointer(module,cardHit.target,942,Oculus.Interaction.PointerEventType.Select,cardHit.pose);
            SendMetaPointer(module,cardHit.target,942,Oculus.Interaction.PointerEventType.Unselect,cardHit.pose);
            Assert.That(card.DetailsVisible,Is.True);Assert.That(view.Director.MenuVisible,Is.False);
            Canvas.ForceUpdateCanvases();AssertControllerBranchesClear(module,reach);
            cardHit=ControllerHit(head.position+new Vector3(.2f,-.35f,.15f),details.transform.position,reach,readingCanvas);
            SendMetaPointer(module,cardHit.target,942,Oculus.Interaction.PointerEventType.Move,cardHit.pose);
            SendMetaPointer(module,cardHit.target,942,Oculus.Interaction.PointerEventType.Select,cardHit.pose);
            SendMetaPointer(module,cardHit.target,942,Oculus.Interaction.PointerEventType.Unselect,cardHit.pose);
            Assert.That(card.DetailsVisible,Is.False);Assert.That(readingCanvas.transform.position,Is.EqualTo(anchored));
            var close=card.GetComponentsInChildren<SplitDescriptionButton>().Single(b=>b.Close);
            var closeHit=ControllerHit(head.position+new Vector3(.2f,-.35f,.15f),close.transform.position,reach,readingCanvas);
            SendMetaPointer(module,closeHit.target,942,Oculus.Interaction.PointerEventType.Move,closeHit.pose);
            SendMetaPointer(module,closeHit.target,942,Oculus.Interaction.PointerEventType.Select,closeHit.pose);
            SendMetaPointer(module,closeHit.target,942,Oculus.Interaction.PointerEventType.Unselect,closeHit.pose);
            Assert.That(view.NameTooltip.Visible,Is.False);AssertControllerBranchesClear(module,reach);
            Assert.That(view.Session.State.NodeId,Is.EqualTo(root.nodeId));Assert.That(view.Ensemble.RouteTotal,Is.EqualTo(total));
            Assert.That(view.Session.State.Decisions,Is.Empty);
            Assert.That(head.position,Is.EqualTo(headPosition));Assert.That(head.rotation,Is.EqualTo(headRotation));
        }

        private void AssertControllerBranchesClear(Oculus.Interaction.PointableCanvasModule module,float reach)
        {
            GameObject hovered=null;
            System.Action<Oculus.Interaction.PointableCanvasEventArgs> onHover=e=>{if(e.PointerId==943)hovered=e.Hovered;};
            Oculus.Interaction.PointableCanvasModule.WhenSelectableHovered+=onHover;
            try {
                foreach(var branch in view.controls.Where(c=>c.action==TreeAction.TrueBranch||c.action==TreeAction.FalseBranch))
                foreach(float hand in new[]{-.2f,.2f}) {
                    hovered=null;
                    var hit=ControllerHit(view.Garden.Locomotion.Head.position+new Vector3(hand,-.35f,.15f),branch.transform.position,reach,branch.GetComponentInParent<Canvas>());
                    SendMetaPointer(module,hit.target,943,Oculus.Interaction.PointerEventType.Hover,hit.pose);
                    Assert.That(hovered,Is.EqualTo(branch.gameObject),"The SDK must route to the visible branch button.");
                    SendMetaPointer(module,hit.target,943,Oculus.Interaction.PointerEventType.Unhover,hit.pose);
                }
            } finally { Oculus.Interaction.PointableCanvasModule.WhenSelectableHovered-=onHover; }
        }

        private static (Oculus.Interaction.PointableCanvas target,Pose pose) ControllerHit(Vector3 origin,Vector3 point,float reach,Canvas expected)
        {
            var ray=new Ray(origin,point-origin);
            var hits=Object.FindObjectsByType<Oculus.Interaction.RayInteractable>(FindObjectsSortMode.None)
                .Where(r=>r.isActiveAndEnabled).Select(r=>{bool found=r.Raycast(ray,out var hit,reach,false);return new{r,hit,found};})
                .Where(x=>x.found).OrderBy(x=>x.hit.Distance).ToArray();
            Assert.That(hits,Is.Not.Empty,"Controller ray does not reach "+expected.name);
            var surface=hits[0].r.GetComponent<Oculus.Interaction.PointableCanvas>();
            Assert.That(surface,Is.Not.Null);
            Assert.That(surface.Canvas,Is.SameAs(expected),"A nearer canvas blocks "+expected.name);
            return (surface,new Pose(hits[0].hit.Point,expected.transform.rotation));
        }
        private static void SendMetaPointer(Oculus.Interaction.PointableCanvasModule module,Oculus.Interaction.PointableCanvas target,
            int id,Oculus.Interaction.PointerEventType type,Pose pose)
        {
            target.ProcessPointerEvent(new Oculus.Interaction.PointerEvent(id,type,pose));
            // Exercise the SDK's canvas filtering and uGUI dispatch, not the card's handlers directly.
            module.Process();
        }

        private void AssertBranchTargetsClear()
        {
            var head=view.Garden.Locomotion.Head;
            foreach(var branch in view.controls.Where(c=>c.action==TreeAction.TrueBranch||c.action==TreeAction.FalseBranch)) {
                Assert.That(branch.GetComponent<UnityEngine.UI.Image>().color.a,Is.Zero);
                var pointer=new PointerEventData(EventSystem.current){position=head.GetComponent<Camera>().WorldToScreenPoint(branch.transform.position)};
                var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
                Assert.That(hits,Is.Not.Empty);
                Assert.That(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject),Is.EqualTo(branch.gameObject),
                    "Neither the expanded legend nor inline reading may intercept branch selection.");
            }
        }

        private static void AssertReadingFits(TMP_Text reading,RectTransform next)
        {
            reading.ForceMeshUpdate();
            Assert.That(reading.rectTransform.anchoredPosition.y + reading.rectTransform.rect.yMin,
                Is.GreaterThan(next.anchoredPosition.y + next.rect.yMax + 12),"Text needs a gap above page controls.");
            foreach(var character in reading.textInfo.characterInfo.Take(reading.textInfo.characterCount)
                .Where(c=>c.isVisible && c.pageNumber==reading.pageToDisplay-1)) {
                Assert.That(character.bottomLeft.x,Is.GreaterThanOrEqualTo(reading.rectTransform.rect.xMin-1));
                Assert.That(character.topRight.x,Is.LessThanOrEqualTo(reading.rectTransform.rect.xMax+1));
                Assert.That(character.bottomLeft.y,Is.GreaterThanOrEqualTo(reading.rectTransform.rect.yMin-1));
                Assert.That(character.topRight.y,Is.LessThanOrEqualTo(reading.rectTransform.rect.yMax+1));
            }
        }

        [UnityTest] public IEnumerator CompletedProfilePairKeepsItsContributionsInCompactCardAndDetails()
        {
            view.Director.StartPlayback(true);view.Director.Activate("calculate-all");view.Director.Activate("results-forest");
            var expectedA=view.Director.Playback.A.RawScore;var expectedB=view.Director.Playback.B.RawScore;
            OpenChoices(1);
            Assert.That(view.Garden.DescriptionCard.ContributionText.text,Does.StartWith("A: ").And.Contain("B: "));
            view.Director.OpenTreeDetails(1);view.Director.CloseMenu();
            Assert.That(view.Director.Playback.A.RawScore,Is.EqualTo(expectedA));Assert.That(view.Director.Playback.B.RawScore,Is.EqualTo(expectedB));
            Assert.That(view.Director.Playback.CompletedTrees,Is.EqualTo(view.Model.Trees.Count));
            yield return null;
        }
    }
}
