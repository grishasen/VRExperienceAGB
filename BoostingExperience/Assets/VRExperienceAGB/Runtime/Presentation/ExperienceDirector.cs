using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using VRExperienceAGB.Application;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Presentation
{
    /// <summary>One front-facing menu and one full-ensemble demonstration coordinator.</summary>
    public sealed partial class ExperienceDirector : MonoBehaviour
    {
        public OneTreeExperience View { get; private set; }
        public JsonLibraryController Library { get; private set; }
        public ExperiencePerformanceCapture Performance { get; private set; }
        private int libraryPage;
        private readonly MenuReadingSpace readingSpace = new MenuReadingSpace();
        private bool resultPlaced;
        private NavigationPage resultPage;
        public void ResetProfileSelection() { SelectedA=0;SelectedB=View.AvailableProfiles.Profiles.Count>1?1:0;wasPaused=false;valuePage=0;ValueSearch="";DifferencesOnly=false; }
        public ForestPlayback Playback { get; private set; }
        public bool PlayingTour { get; private set; }
        public bool ResultsVisible { get; private set; }
        public bool MenuVisible => menu != null && menu.gameObject.activeSelf;
        public long Revision { get; private set; }
        public string MenuPage { get; private set; } = "home";
        public string Explanation => explanation == null ? "" : explanation.text;
        public string FinalText => Playback == null ? "" : ResultCopy();
        public int SelectedA { get; private set; }
        public int SelectedB { get; private set; } = 1;
        private Canvas menu, detailCanvas, resultCanvas;
        private TMP_Text title, subtitle, explanation, resultText;
        private Transform content, routeRoot;
        private readonly List<Material> materials = new List<Material>();

        private readonly Transform[] dots = new Transform[2];
        private int builtTree = -1, builtStep = -1;
        private PlaybackPhase shownPhase=(PlaybackPhase)(-1);
        private bool completeShown, configured;
        private string runA, runB;
        private VRExperienceAGB.Domain.PreparedProfile runProfileA, runProfileB;
        private float startupDelay = .5f;
        private float menuHeight = -.10f;
        private bool wasPaused, recenterButtonHeld;
        private Color AColor => new Color(1f,.78f,.18f);
        private Color BColor => new Color(.85f,.35f,1f);
        private ForestGardenView Garden => View.Garden;
        public void Configure(OneTreeExperience view)
        {
            View = view;
            if (configured) return;
            configured = true;
            Library=gameObject.AddComponent<JsonLibraryController>();Library.Configure(this);
            Performance=gameObject.AddComponent<ExperiencePerformanceCapture>();
            gameObject.AddComponent<FeedbackCapture>().Configure(view);
            foreach(var control in view.controls)if(control.GetComponent<ButtonHint>()==null)control.gameObject.AddComponent<ButtonHint>();
            menu = Garden.M5.Panel("UnifiedExperienceMenu",new Vector2(1100,820),view.transform);
            menu.sortingOrder = 50;
            menu.transform.localScale=Vector3.one*.00135f;
            title = Garden.Text(menu.transform,"Title","",new Vector2(-30,340),new Vector2(920,65),42);
            subtitle = Garden.Text(menu.transform,"Subtitle","",new Vector2(0,268),new Vector2(980,65),28);
            TreeCardTypography.SetFont(title,true);title.fontSize=38;
            title.richText=false;subtitle.richText=false;subtitle.fontSize=25;subtitle.enableAutoSizing=false;subtitle.overflowMode=TextOverflowModes.Ellipsis;
            content = new GameObject("MenuContent",typeof(RectTransform)).transform; content.SetParent(menu.transform,false);
            AddButton(menu.transform,"X",new Vector2(480,345),new Vector2(75,65),"close");
            AddButton(menu.transform,"Main menu",new Vector2(-340,-350),new Vector2(290,65),"home");
            AddButton(menu.transform,"Position scene",new Vector2(0,-350),new Vector2(290,65),"position");
            AddButton(menu.transform,"Tools",new Vector2(340,-350),new Vector2(290,65),"tools");
            Garden.Text(menu.transform,"ControllerShortcuts","A: menu  ·  B: center  ·  X: screenshot",new Vector2(-140,-260),new Vector2(700,45),22);
            AddButton(menu.transform,"Details",new Vector2(380,-260),new Vector2(220,48),"page-details");
            foreach(var text in view.GetComponentsInChildren<TMP_Text>(true)) TreeCardTypography.SetFont(text);
            TreeCardTypography.SetFont(title,true);
            routeRoot = new GameObject("ProfileRoutePresentation").transform; routeRoot.SetParent(view.presentationRoot,false);
            detailCanvas = Garden.M5.Panel("ProfileExplanation",new Vector2(1100,560),routeRoot);
            detailCanvas.transform.localPosition = new Vector3(0,.6f,2.8f);
            detailCanvas.transform.localScale=Vector3.one*.0019f;
            detailCanvas.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
            explanation = Garden.Text(detailCanvas.transform,"Explanation","",Vector2.zero,new Vector2(1020,520),36);
            explanation.enableAutoSizing=true; explanation.fontSizeMin=32; explanation.fontSizeMax=40;
            resultCanvas = Garden.CanvasAt("WholeForestResult",Vector3.zero,new Vector2(1300,280),.0014f,view.transform);
            resultText = Garden.Text(resultCanvas.transform,"FinalScore","",Vector2.zero,new Vector2(1280,270),36);
            resultText.richText=false;resultText.enableAutoSizing=true;resultText.fontSizeMin=24;resultText.fontSizeMax=35;
            routeRoot.gameObject.SetActive(false);resultCanvas.gameObject.SetActive(false);
            OpenMenu("home");
        }
        private void AddButton(Transform parent,string text,Vector2 position,Vector2 size,string command)
        {
            var go=new GameObject(command,typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button),typeof(ExperienceMenuTarget));
            go.transform.SetParent(parent,false);var rt=(RectTransform)go.transform;rt.sizeDelta=size;rt.anchoredPosition=position;
            go.GetComponent<UnityEngine.UI.Image>().color=new Color(.045f,.19f,.25f,.98f);
            go.GetComponent<UnityEngine.UI.Button>().navigation=new UnityEngine.UI.Navigation{mode=UnityEngine.UI.Navigation.Mode.None};
            var target=go.GetComponent<ExperienceMenuTarget>();target.Owner=this;target.Command=command;
            var label=Garden.Text(go.transform,"Label",text,Vector2.zero,size-new Vector2(22,12),27);
            label.richText=false;label.enableAutoSizing=false;label.overflowMode=TextOverflowModes.Ellipsis;
            go.AddComponent<ButtonHint>();
        }
        private void Row(string label,string command,int row,int column=0,bool half=false)
        { AddButton(content,label,new Vector2(half?(column==0?-245:245):0,175-row*82),new Vector2(half?460:940,68),command); }
        public void OpenMenu(string page="home")
        {
            bool opening = !MenuVisible;
            if(opening) { wasPaused=View.SessionPausedWithoutSuspension;View.SetSessionPaused(true); }
            Garden.Navigation?.InvalidatePointerPresses(); View.NameTooltip?.Hide();
            if(page=="values" && (MenuPage=="profiles" || MenuPage=="compare")) { valuesReturnPage=MenuPage=="compare"?"compare":"profiles";valuePage=0; }
            MenuPage=page;menu.gameObject.SetActive(true);Revision++;BuildMenu();if(opening)PlaceMenu(true);
            readingSpace.Hide(View.transform,menu.transform,Revision);
        }
        internal void PrepareSceneRebuild() => readingSpace.Restore();
        public void CloseMenu()
        {
            if(!MenuVisible)return;
            readingSpace.Restore();menu.gameObject.SetActive(false);View.SetSessionPaused(wasPaused);Revision++;
            if(!PlayingTour)View.Refresh();
            Garden.Navigation?.InvalidatePointerPresses();
        }
        public void ToggleMenu()
        {
            if (Garden.M5.Extensions.PanelOpen) { Garden.M5.Extensions.Close();OpenMenu("tools");return; }
            if(MenuVisible)CloseMenu();else OpenMenu(PlayingTour?"playback":Garden.Navigation.Page==NavigationPage.Tree?"tree":"home");
        }
        private int valuePage;
        public void Activate(string command)
        {
            if(!View.Ready)return;
            if(Library.Busy && command!="close")return;
            if(command.StartsWith("library-open:")) {
                int index;if(int.TryParse(command.Substring(13),out index) && index>=0 && index<Library.ListedFiles.Length)
                    Library.ImportPath(Library.ListedFiles[index],MenuPage=="profile-files",false);
                return;
            }
            Revision++;
            if(ActivateLearning(command))return;
            switch(ExperienceCommands.Parse(command))
            {
                case ExperienceCommand.TableTools:case ExperienceCommand.Home:case ExperienceCommand.Position:case ExperienceCommand.Profiles:case ExperienceCommand.Compare:case ExperienceCommand.Tools:case ExperienceCommand.Help:case ExperienceCommand.Values:case ExperienceCommand.Library:case ExperienceCommand.Models:case ExperienceCommand.ProfileFiles:case ExperienceCommand.LibraryHelp:case ExperienceCommand.Upload:OpenMenu(command);return;
                case ExperienceCommand.ImportModel:Library.Pick(false);OpenMenu(Library.PickerUnavailable?"upload":"models");return;
                case ExperienceCommand.ImportProfile:Library.Pick(true);OpenMenu(Library.PickerUnavailable?"upload":"profile-files");return;
                case ExperienceCommand.UploadStart:Library.StartUpload();OpenMenu("upload");return;
                case ExperienceCommand.UploadStop:Library.StopUpload();OpenMenu("upload");return;
                case ExperienceCommand.LibraryRefresh:BuildMenu();return;
                case ExperienceCommand.LibraryPrev:libraryPage=Math.Max(0,libraryPage-1);BuildMenu();return;
                case ExperienceCommand.LibraryNext:libraryPage++;BuildMenu();return;
                case ExperienceCommand.Close:CloseMenu();return;
                case ExperienceCommand.ForestInfo:StopPlayback(true);CloseMenu();Garden.Navigation.ShowForest();Garden.M6.SetStoryOpen(true);return;
                case ExperienceCommand.NextA:SelectedA=(SelectedA+1)%View.AvailableProfiles.Profiles.Count;BuildMenu();return;
                case ExperienceCommand.NextB:SelectedB=(SelectedB+1)%View.AvailableProfiles.Profiles.Count;BuildMenu();return;
                case ExperienceCommand.ValuesPrev:valuePage=Math.Max(0,valuePage-1);BuildMenu();return;
                case ExperienceCommand.ValuesNext:valuePage++;BuildMenu();return;
                case ExperienceCommand.ValuesBack:OpenMenu(valuesReturnPage);return;
                case ExperienceCommand.ValuesDiff:DifferencesOnly=!DifferencesOnly;valuePage=0;BuildMenu();return;
                case ExperienceCommand.ValuesSearch:SetValueSearch("");return;
                case ExperienceCommand.StartProfile:StartPlayback(false);return;
                case ExperienceCommand.StartAb:StartPlayback(true);return;
                case ExperienceCommand.Forest:EnterManualForest();return;
                case ExperienceCommand.Single:OpenManualTree(0);return;
                case ExperienceCommand.ManualTree:OpenManualTree(View.Ensemble.Index);return;
                case ExperienceCommand.Table:ShowResults(true);return;
                case ExperienceCommand.ResultsForest:ShowResults(false);return;
                case ExperienceCommand.ResultsTable:ShowResults(true);return;
                case ExperienceCommand.Pause:if(Playback!=null)Playback.Paused=!Playback.Paused;BuildMenu();return;
                case ExperienceCommand.Step:if(Playback!=null){Playback.Next();RenderPlayback();}return;
                case ExperienceCommand.CalculateAll:if(Playback!=null){Playback.FinishAll();RenderPlayback();}return;
                case ExperienceCommand.PreviousTree:Playback?.PreviousTree();builtTree=-1;RenderPlayback();return;
                case ExperienceCommand.Speed:if(Playback!=null)Playback.Speed=Playback.Speed>=4?.5:Playback.Speed*2;BuildMenu();return;
                case ExperienceCommand.Restart:Playback?.Restart();SetPace(Pace);completeShown=false;ResultsVisible=false;resultPlaced=false;PlayingTour=true;builtTree=-1;CloseMenu();RenderPlayback();return;
                case ExperienceCommand.TreeBack:View.Session.Back();View.Refresh();CloseMenu();return;
                case ExperienceCommand.TreeRestart:View.Session.Restart();View.Refresh();CloseMenu();return;
                case ExperienceCommand.ReturnForest:CloseMenu();Garden.Navigation.ShowForest();return;
                case ExperienceCommand.FocusCurrent:View.FocusedView.CurrentFocus();View.Refresh();CloseMenu();return;
                case ExperienceCommand.Seated:View.Execute(TreeAction.Seated,View.Session.State.Revision,View.Session.State.NodeId);BuildMenu();return;
                case ExperienceCommand.Stop:EnterManualForest();return;
                case ExperienceCommand.Audio:Garden.M5.Atmosphere.SoundEnabled=!Garden.M5.Atmosphere.SoundEnabled;BuildMenu();return;
                case ExperienceCommand.Stars:Garden.M5.Atmosphere.Sky.ToggleGuides();return;
                case ExperienceCommand.Recenter:RecenterScene();return;
                case ExperienceCommand.Closer:Garden.Locomotion.AdjustPlacement(0,-.5f,0);break;
                case ExperienceCommand.Farther:Garden.Locomotion.AdjustPlacement(0,.5f,0);break;
                case ExperienceCommand.Left:Garden.Locomotion.AdjustPlacement(-.5f,0,0);break;
                case ExperienceCommand.Right:Garden.Locomotion.AdjustPlacement(.5f,0,0);break;
                case ExperienceCommand.TurnLeft:Garden.Locomotion.AdjustPlacement(0,0,-15);break;
                case ExperienceCommand.TurnRight:Garden.Locomotion.AdjustPlacement(0,0,15);break;
                case ExperienceCommand.Lower:menuHeight=Mathf.Max(-.5f,menuHeight-.1f);break;
                case ExperienceCommand.Higher:menuHeight=Mathf.Min(.2f,menuHeight+.1f);break;
                case ExperienceCommand.TableGroup:Garden.M5.Diorama.NextGroup();BuildMenu();return;
                case ExperienceCommand.TableLabels:Garden.M5.Diorama.ShowAllLabels=!Garden.M5.Diorama.ShowAllLabels;Garden.M5.Diorama.Refresh();BuildMenu();return;
                case ExperienceCommand.TableInspect:CloseMenu();Garden.Navigation.ShowDiorama();Garden.M5.Diorama.Hover(View.Ensemble.Index);return;
                case ExperienceCommand.TableSmaller:Garden.M5.Diorama.ChangeScale(-.1f);return;
                case ExperienceCommand.TableLarger:Garden.M5.Diorama.ChangeScale(.1f);return;
                case ExperienceCommand.TableLeft:Garden.M5.Diorama.Rotate(-15);return;
                case ExperienceCommand.TableRight:Garden.M5.Diorama.Rotate(15);return;
                case ExperienceCommand.Search:case ExperienceCommand.Trail:case ExperienceCommand.Review:
                    CloseMenu();Garden.M5.Activate(command=="search"?M5Action.ExtensionSearch:command=="trail"?M5Action.ExtensionTrail:M5Action.ExtensionReview);return;
            }
            PlaceMenu(true);if(PlayingTour)PlaceRoute();
        }
        private void EnterManualForest()
        {
            StopPlayback(false);
            if(View.Comparison!=null || View.Ensemble.Mode!=ExperienceMode.Manual)View.StartScenarioProfiles(-1);
            CloseMenu();Garden.Navigation.ShowForest();
        }
        private void OpenManualTree(int index)
        {
            StopPlayback(false);View.StartScenarioProfiles(-1);CloseMenu();
            Garden.Navigation.ShowForest();Garden.Navigation.OpenTree(index);
        }
        public void StartPlayback(bool comparison)
        {
            if(View.Model.StructureOnlyPreview || View.AvailableProfiles.Profiles.Count==0){OpenMenu("profile-files");return;}
            CloseMenu(); StopPlayback(false);
            View.StartScenarioProfiles(SelectedA,comparison?SelectedB%View.AvailableProfiles.Profiles.Count:-1);
            Playback=new ForestPlayback(View.Ensemble.Evaluation,comparison?View.Comparison.B.Evaluation:null);SetPace(Pace);
            runProfileA=View.AvailableProfiles.Profiles[SelectedA];runProfileB=comparison?View.AvailableProfiles.Profiles[SelectedB%View.AvailableProfiles.Profiles.Count]:null;
            runA=View.AvailableProfiles.Profiles[SelectedA].DisplayName;runB=comparison?View.AvailableProfiles.Profiles[SelectedB%View.AvailableProfiles.Profiles.Count].DisplayName:null;
            completeShown=false;ResultsVisible=false;resultPlaced=false;PlayingTour=true;builtTree=-1;
            Garden.Navigation.ShowForest();Garden.Navigation.OpenTree(0);PlaceRoute();RenderPlayback();
        }
        public void StopPlayback(bool retainResults)
        { PlayingTour=false;routeRoot.gameObject.SetActive(false);if(!retainResults){ResultsVisible=false;resultPlaced=false;Playback=null;}resultCanvas.gameObject.SetActive(false); }
        public void ShowResults(bool table)
        {
            StopPlayback(true);ResultsVisible=Playback?.Complete==true;CloseMenu();
            if(table){Garden.M5.Diorama.Place();Garden.Navigation.ShowDiorama();}
            else Garden.Navigation.ShowForest();
            PlaceResult(table);Garden.RefreshExplanationLinks();
        }
        public bool TryContribution(int index,bool b,out double value)
        {
            value=0;if(!ResultsVisible || Playback==null || b&&Playback.B==null)return false;
            value=(b?Playback.B:Playback.A).Trees[index].Contribution;return true;
        }
        public void AdvanceTour(float seconds)
        {
            if(!PlayingTour||Playback==null||MenuVisible||View.ApplicationSuspended||Garden.M5.Extensions.PanelOpen)return;
            Playback.Advance(seconds);RenderPlayback();
        }
        public void HandleRecenterButton(bool pressed)
        {
            if(pressed && !recenterButtonHeld)RecenterScene();
            recenterButtonHeld=pressed;
        }
        public void RecenterScene()
        {
            Garden.Locomotion.Recenter();
            if(Garden.Navigation.Page==NavigationPage.Diorama)Garden.M5.Diorama.Place();
            if(PlayingTour) { PlaceRoute();RenderPlayback(); }
            else View.Refresh();
            PlaceMenu(true);Garden.M6.Legend?.Place();resultPlaced=false;Garden.Navigation.InvalidatePointerPresses();
        }
        private void Update()
        {
            if(!configured||!View.Ready)return;
            if(startupDelay>=0){startupDelay-=Time.unscaledDeltaTime;if(startupDelay<0){Garden.Locomotion.Recenter();PlaceMenu(true);}}
            var right=UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
            HandleRecenterButton(right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton,out bool pressed) && pressed);
            if(UnityEngine.InputSystem.Keyboard.current?.rKey.wasPressedThisFrame==true)RecenterScene();
            AdvanceTour(Time.unscaledDeltaTime);
        }
        private void LateUpdate()
        {
            if(!configured||!View.Ready)return;
            // Each opening uses the current viewer pose; page changes and reading stay world-anchored.
            Garden.Navigation.HideLegacyMenus();Garden.M5.HideLegacyTools();Garden.M6.RefreshVisibility();
            if(PlayingTour) {
                View.presentationRoot.gameObject.SetActive(true);routeRoot.gameObject.SetActive(true);View.drop.gameObject.SetActive(false);
                View.explanation.gameObject.SetActive(false);View.score.gameObject.SetActive(false);View.feedback.gameObject.SetActive(false);View.status.gameObject.SetActive(false);
            }
            if(MenuVisible || PlayingTour) { foreach(var c in View.controls)c.gameObject.SetActive(false); }
            if(MenuVisible) { readingSpace.Hide(View.transform,menu.transform,Revision); resultCanvas.gameObject.SetActive(false); }
            else if(ResultsVisible && Garden.Navigation.Page!=NavigationPage.Tree) {
                if(!resultPlaced || resultPage!=Garden.Navigation.Page)PlaceResult(Garden.Navigation.Page==NavigationPage.Diorama);
                resultCanvas.gameObject.SetActive(true);
            } else resultCanvas.gameObject.SetActive(false);
        }
        private void PlaceMenu(bool immediate)
        {
            var head=Garden.Locomotion.Head;var forward=head.forward.normalized;
            var position=head.position+forward*1.6f+Vector3.up*menuHeight;var rotation=Quaternion.LookRotation(forward);
            float t=immediate?1:1-Mathf.Exp(-Time.unscaledDeltaTime*4);
            menu.transform.SetPositionAndRotation(Vector3.Lerp(menu.transform.position,position,t),Quaternion.Slerp(menu.transform.rotation,rotation,t));
        }
        private void PlaceRoute()
        {
            routeRoot.localPosition=Vector3.zero;routeRoot.localRotation=Quaternion.identity;
            // Place once at entry/recenter; never attach the explanation board to head motion.
            detailCanvas.transform.rotation=Quaternion.LookRotation(detailCanvas.transform.position-Garden.Locomotion.Head.position);
        }
        private void PlaceResult(bool table)
        {
            resultCanvas.gameObject.SetActive(ResultsVisible && !MenuVisible);if(!ResultsVisible)return;
            resultPlaced=true;resultPage=Garden.Navigation.Page;
            resultText.text=ResultSummary();var head=Garden.Locomotion.Head;
            if(table) resultCanvas.transform.position=Garden.M5.Diorama.ModelRoot.position+Vector3.up*.85f;
            else {var f=Vector3.ProjectOnPlane(head.forward,Vector3.up).normalized;resultCanvas.transform.position=head.position+f*2.7f+Vector3.up*.55f;}
            resultCanvas.transform.rotation=Quaternion.LookRotation(resultCanvas.transform.position-head.position);
        }
        public void InvalidatePlaybackPresentation() { if(PlayingTour)builtStep=-1; }
        private void RenderPlayback()
        {
            if(Playback==null)return;
            if(Playback.Complete){
                if(!completeShown){completeShown=true;CommitResultSessions();PlayingTour=false;ResultsVisible=true;routeRoot.gameObject.SetActive(false);Garden.Navigation.ShowForest();OpenMenu("result");}
                return;
            }
            bool changed=builtTree!=Playback.TreeIndex || builtStep!=Playback.StepIndex || shownPhase!=Playback.Phase;
            if(builtTree!=Playback.TreeIndex)BuildRoute();
            var a=Playback.A.Trees[builtTree];var b=Playback.B?.Trees[builtTree];
            if(builtStep!=Playback.StepIndex){
                View.FocusedView.RefreshPlayback(a.VisitedNodeIds[Playback.RouteStep(false)],b?.VisitedNodeIds[Playback.RouteStep(true)]);
                builtStep=Playback.StepIndex;
            }
            for(int p=0;p<(b==null?1:2);p++) {
                var tree=p==0?a:b;int step=Playback.RouteStep(p==1);
                var from=View.nodeViews.First(v=>v.gameObject.activeSelf && v.nodeId==tree.VisitedNodeIds[step]).dropAnchor.position;
                var pos=from;
                if(Playback.Phase==PlaybackPhase.Moving && step<tree.Decisions.Count){
                    var to=View.nodeViews.First(v=>v.gameObject.activeSelf && v.nodeId==tree.VisitedNodeIds[step+1]).dropAnchor.position;
                    pos=Vector3.Lerp(from,to,Mathf.SmoothStep(0,1,(float)Playback.Movement));
                }
                dots[p].position=pos+(b==null?Vector3.zero:View.presentationRoot.right*(p==0?-.1f:.1f));
            }
            if(!changed)return;
            shownPhase=Playback.Phase;
            string heading="TREE "+(builtTree+1)+" / "+View.Model.Trees.Count+" · decision "+(Playback.StepIndex+1)+" · "+Pace+"\n"+Playback.CompletedTrees+" complete · animation estimate "+TimeSpan.FromSeconds(Playback.EstimatedRemainingSeconds).ToString(@"hh\:mm\:ss")+(Playback.Paused?" after resume":" remaining")+"\n";
            if(Playback.Phase==PlaybackPhase.TreeResult)explanation.text=heading+TreeCopy(Playback.A,"A")+(Playback.B==null?"":"\n"+TreeCopy(Playback.B,"B"))+(Playback.Paused?"\nPaused · Next step continues.":"\nNext tree follows automatically.");
            else explanation.text=heading+DecisionCopy(Playback.A,"A")+(Playback.B==null?"":"\n\n"+DecisionCopy(Playback.B,"B"));
        }
        private string TreeCopy(EvaluationResult result,string prefix)
        {var t=result.Trees[Playback.TreeIndex];return prefix+": leaf "+Signed(t.Contribution)+" · accumulated Score "+Number(t.RunningRawScore);}
        private string DecisionCopy(EvaluationResult result,string prefix)
        {
            var tree=result.Trees[Playback.TreeIndex];int step=Math.Min(Playback.StepIndex,tree.Decisions.Count);
            if(step==tree.Decisions.Count)return prefix+": leaf reached · "+Signed(tree.Contribution)+" · waiting for the other profile";
            var d=tree.Decisions[step];var split=(SplitNode)View.Model.Trees[Playback.TreeIndex].Nodes.First(n=>n.Id==d.NodeId);
            return prefix+": "+View.Condition(split,true)+"\nValue: "+Value(d.ObservedValue)+" → "+(d.Matched?"TRUE / left":"FALSE / right");
        }
        private string ResultSummary()
        {
            if(Playback==null)return "No completed profile result.";
            return "ENSEMBLE SCORE\nA · "+runA+": "+Number(Playback.A.RawScore)+
                (Playback.B==null?"":"\nB · "+runB+": "+Number(Playback.B.RawScore)+"\nScore difference B − A: "+Signed(Playback.B.RawScore-Playback.A.RawScore))+
                "\nAll "+Playback.A.Trees.Count+" trees evaluated\n"+VRExperienceAGB.Import.ModelInsightText.CalibrationNotice;
        }
        private string ResultCopy()
        {
            if(Playback==null)return "";
            string text="ENSEMBLE SCORE\nA · "+runA+"\nScore "+Number(Playback.A.RawScore);
            if(Playback.B!=null)text+="\nB · "+runB+"\nScore "+Number(Playback.B.RawScore)+"\nScore difference B − A: "+Signed(Playback.B.RawScore-Playback.A.RawScore);
            return text+"\nScore = baseline + sum of reached leaf contributions\nAll "+Playback.A.Trees.Count+" trees evaluated\n"+
                VRExperienceAGB.Import.ModelInsightText.CalibrationNotice+
                (runProfileA?.Source==null?"":"\n\nA · "+VRExperienceAGB.Import.CdhSourceText.Describe(runProfileA))+
                (runProfileB?.Source==null?"":"\n\nB · "+VRExperienceAGB.Import.CdhSourceText.Describe(runProfileB));
        }
        private void CommitResultSessions()
        {
            var sessions = View.Comparison == null ? View.Ensemble.Trees : View.Comparison.A.Trees.Concat(View.Comparison.B.Trees);
            foreach(var session in sessions) {
                session.Restart();session.EnterTree();session.SetPaused(false);
                while(!session.State.AtLeaf) {
                    session.StepProfile(session.State.Revision,session.State.NodeId);
                    session.CompleteMove(session.State.PendingDecision.EventId);
                }
                session.ReturnToOverview();
            }
        }
        private void BuildRoute()
        {
            builtTree=Playback.TreeIndex;builtStep=-1;shownPhase=(PlaybackPhase)(-1);
            View.SelectPlaybackTree(builtTree);
            if(dots[0]==null)dots[0]=Sphere("ProfileA",Vector3.zero,.17f,AColor).transform;
            if(dots[1]==null)dots[1]=Sphere("ProfileB",Vector3.zero,.17f,BColor).transform;
            dots[0].gameObject.SetActive(true);dots[1].gameObject.SetActive(Playback.B!=null);
            routeRoot.gameObject.SetActive(true);
        }
        private Material Material(Color color)
        { var mat=new Material(View.activeMaterial);mat.color=color;mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color);materials.Add(mat);return mat; }
        private GameObject Sphere(string name,Vector3 position,float size,Color color)
        {var go=GameObject.CreatePrimitive(name=="ProfileB"?PrimitiveType.Cube:PrimitiveType.Sphere);go.name=name;go.transform.SetParent(routeRoot,false);go.transform.localPosition=position;go.transform.localScale=Vector3.one*size;go.GetComponent<Renderer>().sharedMaterial=Material(color);Destroy(go.GetComponent<Collider>());return go;}
        private static string Number(double v)=>v.ToString("0.######",CultureInfo.InvariantCulture);
        private static string Signed(double v)=>v.ToString("+0.######;-0.######;0",CultureInfo.InvariantCulture);
        private static string Percent(double v)=>(v*100).ToString("0.0000",CultureInfo.InvariantCulture)+"%";
        private static string Value(ProfileValue v)=>v.Kind==ValueKind.Number?Number(v.Number):v.Kind==ValueKind.Category?v.Category:v.Kind==ValueKind.NotSupplied?"Not supplied":"Missing";
        private void OnDestroy(){readingSpace.Restore();foreach(var mat in materials)if(mat!=null)Destroy(mat);if(menu!=null)Destroy(menu.gameObject);if(routeRoot!=null)Destroy(routeRoot.gameObject);if(resultCanvas!=null)Destroy(resultCanvas.gameObject);}
    }
    public sealed class ExperienceMenuTarget : MonoBehaviour, IPointerDownHandler, IPointerClickHandler, IPointerExitHandler
    {
        public ExperienceDirector Owner;public string Command;
        private long revision=-1;private int pointer;
        public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left){revision=Owner.Revision;pointer=e.pointerId;}}
        public void OnPointerClick(PointerEventData e){if(revision==Owner.Revision&&pointer==e.pointerId&&e.button==PointerEventData.InputButton.Left&&gameObject.activeInHierarchy){revision=-1;ControllerSelectionFeedback.Pulse(pointer);Owner.Activate(Command);}}
        public void OnPointerExit(PointerEventData e){revision=-1;}
    }
}
