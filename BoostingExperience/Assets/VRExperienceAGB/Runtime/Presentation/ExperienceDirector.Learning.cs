using System;
using System.Linq;
using TMPro;
using UnityEngine;
using VRExperienceAGB.Application;
using VRExperienceAGB.Import;

namespace VRExperienceAGB.Presentation
{
    public enum PlaybackPace { Learn, Present, Fast, Step }
    public sealed partial class ExperienceDirector
    {
        public PlaybackPace Pace { get; private set; } = PlaybackPace.Learn;
        private int lessonPage, readingPage=1;
        private string textPurpose="", textDraft="", textReturn="home";
        private const string EntryKeys="ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 ._-";
        private TMP_Text readingText;
        private int describedTree;
        private string describedNode;
        private string menuDetails, savedMenuDetails, detailsReturnPage;
        public void OpenNodeDetails(int index,string nodeId)
        {
            if(index<0 || index>=View.Model.Trees.Count || !View.Model.Trees[index].Nodes.Any(n=>n.Id==nodeId))return;
            describedTree=index;describedNode=nodeId;readingPage=1;View.NameTooltip.Hide();OpenMenu("node-details");
        }
        public void OpenTreeDetails(int index)
        {
            if(index<0 || index>=View.Model.Trees.Count)return;
            describedTree=index;readingPage=1;
            Garden.DescriptionCard?.Hide();
            OpenMenu("tree-details");
        }
        public void SetPace(PlaybackPace pace)
        {
            Pace=pace;
            if(Playback==null)return;
            Playback.DecisionSeconds=pace==PlaybackPace.Fast?.45:pace==PlaybackPace.Present?1.5:3;
            Playback.MoveSeconds=pace==PlaybackPace.Fast?.3:.65;
            Playback.ResultSeconds=pace==PlaybackPace.Fast?1.2:3;
            Playback.Paused=pace==PlaybackPace.Step; Playback.Speed=1;builtStep=-1;
        }
        private string CapabilityCopy => View.Model.StructureOnlyPreview?"Structure + manual routes · profile scoring unavailable":
            (View.Model.ExportInfo!=null?"Exported-tree calculation · CDH reconciliation deferred":"Full calculation under supplied contract · source scoring unverified")+" · "+View.AvailableProfiles.Profiles.Count+" profiles";
        private void BuildLearningMenu()
        {
            switch(MenuPage)
            {
                case "comfort":
                    title.text="COMFORT AND FEEDBACK";subtitle.text="Movement, sound and feedback.";
                    Row(Garden.Locomotion.WalkEnabled?"Movement: teleport + stick":"Movement: teleport only","walk-mode",0);
                    Row("Walk speed: "+Garden.Locomotion.WalkSpeed.ToString("0.00"),"walk-speed",1,0,true);Row(View.IsSeated?"Layout: seated":"Layout: standing","seated",1,1,true);
                    Row("Transition fade: "+(Garden.Locomotion.ComfortFadeEnabled?"on":"off"),"comfort-fade",2,0,true);Row("Haptics: "+(ControllerSelectionFeedback.Enabled?"on":"off"),"haptics",2,1,true);
                    Row("Ambience: "+(Garden.M5.Atmosphere.AmbienceEnabled?"on":"off"),"ambience",3,0,true);Row("Selection sound: "+(Garden.M5.Atmosphere.CuesEnabled?"on":"off"),"cues",3,1,true);
                    Row("Center scene","recenter",4,0,true);Row("Hear wolves in forest","hear-wolves",4,1,true);break;
                case "learn":
                    title.text="LEARN THE FOREST · "+(lessonPage+1)+" / 4";subtitle.text="Read a step, then try it.";
                    string[] lessons={
                        "1. ONE QUESTION\nPoint at a decision. Read its input and two conditions. Choose a branch manually, or let a prepared profile supply the value.\n\nPoint at a label for evidence; click to pin it. Back undoes the accepted decision.",
                        "2. ONE LEAF\nContinue until the route reaches a leaf. Its signed contribution adds to the raw score.\n\nA positive contribution increases the raw score; it is not a probability or proof of model quality.",
                        "3. THE WHOLE MODEL\nChoose a prepared profile. Use Step or Learn pace to read decisions. Calculate whole model skips animation and still evaluates every tree.\n\nManual branch choices remain route scores; they do not become probabilities.",
                        "4. COMPARE TWO INPUTS\nReview A and B before playback. In What-if, copy A to B and edit one input. Inspect the first divergence and largest contribution changes.\n\nThis demonstrates model sensitivity, not causality."
                    };
                    var lesson=ReadingBlock(lessons[lessonPage],new Vector2(0,100),new Vector2(950,240));
                    lesson.enableAutoSizing=false;lesson.fontSize=25;
                    Row("Previous lesson","lesson-prev",3,0,true);Row("Next lesson","lesson-next",3,1,true);
                    Row(lessonPage<2?"Try manual branches":lessonPage==2?"Choose a profile":"Compare A / B",lessonPage<2?"single":lessonPage==2?"profiles":"compare",4);break;
                case "model-info":
                    title.text="Model details";subtitle.text=View.Model.Trees.Count+" trees · "+View.Model.Features.Count+" inputs";
                    Row("Model passport","passport",0);Row("Structure and stored gain","structure",1);Row("Feature dictionary","dictionary",2);
                    Row("Terms and calculation","glossary",3);Row("Sources and verification","verification",4);break;
                case "tree-details":case "node-details":
                    Garden.M6.EnsureModel();
                    describedTree=Mathf.Clamp(describedTree,0,View.Model.Trees.Count-1);
                    title.text="Tree "+(describedTree+1).ToString("00")+ (MenuPage=="node-details"?" · Split details":" · Details");
                    readingText=ReadingBlock(MenuPage=="node-details"?Garden.M6.Analysis.NodeDetail(describedTree,describedNode):TreeDetailsCopy(describedTree),new Vector2(0,100),new Vector2(940,232));
                    TreeCardTypography.Apply(readingText);readingText.fontSize=28;
                    readingText.overflowMode=TextOverflowModes.Page;
                    readingText.ForceMeshUpdate();readingPage=Mathf.Clamp(readingPage,1,Math.Max(1,readingText.textInfo.pageCount));readingText.pageToDisplay=readingPage;
                    subtitle.text="Page "+readingPage+" / "+Math.Max(1,readingText.textInfo.pageCount);
                    Row("Previous page","reading-prev",3,0,true);Row("Next page","reading-next",3,1,true);
                    Row("Back to scene","close",4,0,true);Row("About model","model-info",4,1,true);break;
                case "menu-details":case "profile-source":case "input-detail":case "result-details":case "passport":case "structure":case "dictionary":case "glossary":case "verification":
                    Garden.M6.EnsureModel();var a=Garden.M6.Analysis;
                    title.text=MenuPage=="menu-details"?"Details":MenuPage=="profile-source"?"PROFILE SOURCE":MenuPage=="input-detail"?"INPUT DETAILS":MenuPage=="result-details"?"CALCULATION DETAILS":MenuPage.ToUpperInvariant();
                    string copy=MenuPage=="menu-details"?savedMenuDetails:MenuPage=="profile-source"?ProfileSourceCopy():MenuPage=="input-detail"?InputDetailCopy():MenuPage=="result-details"?ResultCopy():MenuPage=="passport"?ModelInsightText.Passport(a):MenuPage=="structure"?ModelInsightText.Structure(a):MenuPage=="dictionary"?ModelInsightText.Dictionary(a):MenuPage=="glossary"?ModelInsightText.Glossary:ModelInsightText.Verification(a);
                    readingText=ReadingBlock(copy,new Vector2(0,100),new Vector2(940,232));readingText.overflowMode=TextOverflowModes.Page;
                    readingText.ForceMeshUpdate();readingPage=Mathf.Clamp(readingPage,1,Math.Max(1,readingText.textInfo.pageCount));readingText.pageToDisplay=readingPage;
                    subtitle.text="Page "+readingPage+" / "+Math.Max(1,readingText.textInfo.pageCount);
                    Row("Previous page","reading-prev",3,0,true);Row("Next page","reading-next",3,1,true);
                    if(MenuPage=="menu-details") { Row("Back","details-back",4); }
                    else if(MenuPage=="input-detail" || MenuPage=="profile-source") { Row("Back to inputs","values",4); }
                    else if(MenuPage=="result-details") { Row("Back to result","result",4,0,true);Row("What-if · edit B","what-if",4,1,true); }
                    else { Row("About model","model-info",4,0,true);Row(MenuPage=="glossary"?"Try a Missing example":"Inspect a predictor",MenuPage=="glossary"?"missing-example":"search",4,1,true); }break;
                case "text-entry":
                    title.text=textPurpose=="tree"?"GO TO TREE":"SEARCH INPUTS";subtitle.text=textDraft.Length==0?"Select letters or numbers with the trigger.":textDraft;
                    string keys=textPurpose=="tree"?"0123456789":EntryKeys;
                    for(int i=0;i<keys.Length;i++)AddButton(content,keys[i]==' '?"Space":keys[i].ToString(),new Vector2(-450+(i%10)*100,175-(i/10)*65),new Vector2(90,55),"entry-key:"+i);
                    AddButton(content,"Delete",new Vector2(-320,-150),new Vector2(300,60),"entry-delete");AddButton(content,"Clear",new Vector2(0,-150),new Vector2(300,60),"entry-clear");AddButton(content,"Apply",new Vector2(320,-150),new Vector2(300,60),"entry-apply");
                    AddButton(content,"Cancel",new Vector2(0,-220),new Vector2(300,45),"entry-cancel");break;
                case "performance":
                    title.text="PERFORMANCE CAPTURE";subtitle.text=Performance.Status;
                    menuDetails += "\n\n60 seconds after a 3-second warm-up. Reports include frame time, CPU/GPU timing, memory and view transitions.\nEditor captures are diagnostic only; Quest acceptance needs the target build and an active headset.\n\n"+Performance.LastPath;
                    ReadingBlock("Record 60 seconds.\nClose this menu and explore normally.\n\n"+(Performance.LastPath.Length>0?"Last capture saved. See Details for its location.":""),new Vector2(0,70),new Vector2(950,290));
                    Row(Performance.Recording?"Stop and save":"Start 60-second capture",Performance.Recording?"performance-stop":"performance-start",4);break;
            }
        }
        private string ProfileSourceCopy()
        {
            var profiles=View.AvailableProfiles.Profiles;
            if(profiles.Count==0)return "No profile selected.";
            return "A · "+CdhSourceText.Describe(profiles[SelectedA])+
                (valuesReturnPage=="compare"?"\n\nB · "+CdhSourceText.Describe(profiles[SelectedB]):"");
        }
        internal string TreeDetailsCopy(int index)
        {
            Garden.M6.EnsureModel();
            var analysis=Garden.M6.Analysis;
            var tree=View.Model.Trees[index];
            var root=tree.Nodes.First(n=>n.Id==tree.RootId);
            string condition=root is VRExperienceAGB.Domain.SplitNode split?analysis.NodeDetail(index,split.Id,compactSource:true):"Single leaf; no split.";
            var identifiers=tree.Nodes.OfType<VRExperienceAGB.Domain.SplitNode>().Select(n=>n.FeatureId).Distinct().OrderBy(id=>id,StringComparer.Ordinal);
            return analysis.Passport(index)+"\n\n"+Garden.TreeContributionCopy(index)+
                "\n\nROOT CONDITION\n"+condition+"\n\nPREDICTOR IDENTIFIERS\n"+string.Join("\n",identifiers)+
                "\n\nStored gain describes split structure, not a profile contribution or probability.\nTree numbers preserve export order.";
        }
        private TMP_Text ReadingBlock(string copy,Vector2 position,Vector2 size)
        {
            var t=Garden.Text(content,"Reading",copy,position,size,27);t.alignment=TextAlignmentOptions.TopLeft;t.richText=false;return t;
        }
        private bool ActivateLearning(string command)
        {
            if(command=="page-details") { savedMenuDetails=menuDetails;detailsReturnPage=MenuPage;readingPage=1;OpenMenu("menu-details");return true; }
            if(command=="details-back") { OpenMenu(detailsReturnPage??"home");return true; }
            if(command=="hear-wolves") {
                Activate("forest");Garden.M5.Atmosphere.SoundEnabled=true;Garden.M5.Atmosphere.AmbienceEnabled=true;
                Garden.M5.Atmosphere.Wildlife.RequestHowl();return true;
            }
            if(command.StartsWith("input-detail:")) {
                if(int.TryParse(command.Substring(13),out int index)&&index>=0&&index<View.Model.Features.Count) { inputDetailIndex=index;readingPage=1;OpenMenu("input-detail"); }
                return true;
            }
            if(command.StartsWith("entry-key:")) {
                string keys=textPurpose=="tree"?"0123456789":EntryKeys;
                if(int.TryParse(command.Substring(10),out int index)&&index>=0&&index<keys.Length&&textDraft.Length<120)textDraft+=keys[index];
                BuildMenu();return true;
            }
            switch(ExperienceCommands.Parse(command))
            {
                case ExperienceCommand.ProfileSource:case ExperienceCommand.ResultDetails:case ExperienceCommand.Result:case ExperienceCommand.Comfort:case ExperienceCommand.Learn:case ExperienceCommand.ModelInfo:case ExperienceCommand.Passport:case ExperienceCommand.Structure:case ExperienceCommand.Dictionary:case ExperienceCommand.Glossary:case ExperienceCommand.Verification:case ExperienceCommand.Performance:readingPage=1;OpenMenu(command);return true;
                case ExperienceCommand.WalkMode:Garden.Locomotion.WalkEnabled=!Garden.Locomotion.WalkEnabled;break;
                case ExperienceCommand.WalkSpeed:Garden.Locomotion.WalkSpeed=Garden.Locomotion.WalkSpeed>=1.75f?.75f:Garden.Locomotion.WalkSpeed+.25f;break;
                case ExperienceCommand.ComfortFade:Garden.Locomotion.ComfortFadeEnabled=!Garden.Locomotion.ComfortFadeEnabled;break;
                case ExperienceCommand.Haptics:ControllerSelectionFeedback.Enabled=!ControllerSelectionFeedback.Enabled;break;
                case ExperienceCommand.Ambience:Garden.M5.Atmosphere.AmbienceEnabled=!Garden.M5.Atmosphere.AmbienceEnabled;break;
                case ExperienceCommand.Cues:Garden.M5.Atmosphere.CuesEnabled=!Garden.M5.Atmosphere.CuesEnabled;break;
                case ExperienceCommand.LessonPrev:lessonPage=Math.Max(0,lessonPage-1);break;
                case ExperienceCommand.LessonNext:lessonPage=Math.Min(3,lessonPage+1);break;
                case ExperienceCommand.ReadingPrev:readingPage=Math.Max(1,readingPage-1);break;
                case ExperienceCommand.ReadingNext:readingPage++;break;
                case ExperienceCommand.Pace:SetPace((PlaybackPace)(((int)Pace+1)%4));break;
                case ExperienceCommand.ValuesSearch:textPurpose="values";textDraft=ValueSearch;textReturn="values";OpenMenu("text-entry");return true;
                case ExperienceCommand.TreeJump:textPurpose="tree";textDraft="";textReturn=MenuPage;OpenMenu("text-entry");return true;
                case ExperienceCommand.EntryDelete:if(textDraft.Length>0)textDraft=textDraft.Substring(0,textDraft.Length-1);break;
                case ExperienceCommand.EntryClear:textDraft="";break;
                case ExperienceCommand.EntryCancel:OpenMenu(textReturn);return true;
                case ExperienceCommand.EntryApply:
                    if(textPurpose=="values") { SetValueSearch(textDraft);OpenMenu("values");return true; }
                    if(!int.TryParse(textDraft,out int tree)||tree<1||tree>View.Model.Trees.Count){subtitle.text="Enter a tree number from 1 to "+View.Model.Trees.Count;return true;}
                    StopPlayback(true);CloseMenu();Garden.Navigation.ShowForest();Garden.Navigation.OpenTree(tree-1);return true;
                case ExperienceCommand.MissingExample:
                    var selected=View.AvailableProfiles.Profiles.Select((p,i)=>new { Profile=p, Index=i }).FirstOrDefault(p=>p.Profile.Values.Any(v=>v.Value.IsMissing));
                    if(selected==null){subtitle.text="No prepared Missing example is loaded. Import a validated synthetic profile.";return true;}
                    var result=VRExperienceAGB.Domain.ModelEvaluator.Evaluate(View.Model,selected.Profile);
                    var missingTree=result.IsSuccess?result.Value.Trees.Select((t,i)=>new { Tree=t, Index=i }).FirstOrDefault(t=>t.Tree.Decisions.Any(d=>d.Condition.Operator==VRExperienceAGB.Domain.DecisionOperator.IsMissing&&d.ObservedValue.IsMissing)):null;
                    if(missingTree==null){subtitle.text="No reached Missing predicate for this profile. Use predictor search to inspect conditions.";return true;}
                    SelectedA=selected.Index;StopPlayback(false);View.StartScenarioProfiles(SelectedA);CloseMenu();Garden.Navigation.ShowForest();Garden.Navigation.OpenTree(missingTree.Index);return true;
                case ExperienceCommand.WhatIf:
                    if(View.Model.StructureOnlyPreview||View.AvailableProfiles.Profiles.Count==0){OpenMenu("profiles");return true;}
                    StopPlayback(false);CloseMenu();
                    if(View.Comparison==null)View.StartScenarioProfiles(SelectedA,SelectedB%View.AvailableProfiles.Profiles.Count);
                    Garden.M5.Comparison.Open();return true;
                case ExperienceCommand.PerformanceStart:Performance.Begin(View);CloseMenu();return true;
                case ExperienceCommand.PerformanceStop:Performance.Finish();break;
                default:return false;
            }
            BuildMenu();return true;
        }
    }
}
