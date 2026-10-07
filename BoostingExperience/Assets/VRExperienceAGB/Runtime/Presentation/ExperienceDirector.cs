using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using VRExperienceAGB.Application;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Presentation
{
    /// <summary>One front-facing menu and one full-ensemble demonstration coordinator.</summary>
    public sealed class ExperienceDirector : MonoBehaviour
    {
        public OneTreeExperience View { get; private set; }
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
        private bool completeShown, configured, menuPlaced;
        private string runA, runB;
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
            menu = Garden.M5.Panel("UnifiedExperienceMenu",new Vector2(1100,820),view.transform);
            menu.sortingOrder = 50;
            menu.transform.localScale=Vector3.one*.00135f;
            title = Garden.Text(menu.transform,"Title","",new Vector2(-30,340),new Vector2(920,65),42);
            subtitle = Garden.Text(menu.transform,"Subtitle","",new Vector2(0,268),new Vector2(980,65),28);
            content = new GameObject("MenuContent",typeof(RectTransform)).transform; content.SetParent(menu.transform,false);
            AddButton(menu.transform,"X",new Vector2(480,345),new Vector2(75,65),"close");
            AddButton(menu.transform,"Main menu",new Vector2(-340,-350),new Vector2(290,65),"home");
            AddButton(menu.transform,"Position scene",new Vector2(0,-350),new Vector2(290,65),"position");
            AddButton(menu.transform,"Tools",new Vector2(340,-350),new Vector2(290,65),"tools");
            routeRoot = new GameObject("ProfileRoutePresentation").transform; routeRoot.SetParent(view.presentationRoot,false);
            detailCanvas = Garden.M5.Panel("ProfileExplanation",new Vector2(1400,460),routeRoot);
            detailCanvas.transform.localPosition = new Vector3(0,.90f,2.8f);
            detailCanvas.transform.localScale=Vector3.one*.0018f;
            explanation = Garden.Text(detailCanvas.transform,"Explanation","",Vector2.zero,new Vector2(1320,420),36);
            explanation.enableAutoSizing=true; explanation.fontSizeMin=28; explanation.fontSizeMax=36;
            resultCanvas = Garden.CanvasAt("WholeForestResult",Vector3.zero,new Vector2(1300,280),.0014f,view.transform);
            resultText = Garden.Text(resultCanvas.transform,"FinalProbability","",Vector2.zero,new Vector2(1280,270),36);
            resultText.enableAutoSizing=true;resultText.fontSizeMin=24;resultText.fontSizeMax=35;
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
            Garden.Text(go.transform,"Label",text,Vector2.zero,size-new Vector2(22,12),32);
        }
        private void Row(string label,string command,int row,int column=0,bool half=false)
        { AddButton(content,label,new Vector2(half?(column==0?-245:245):0,175-row*82),new Vector2(half?460:940,68),command); }
        public void OpenMenu(string page="home")
        {
            if(!MenuVisible) { wasPaused=View.Session.State.Paused;View.Session.SetPaused(true); }
            Garden.Navigation?.InvalidatePointerPresses(); View.NameTooltip?.Hide();
            MenuPage=page;menu.gameObject.SetActive(true);Revision++;BuildMenu();if(!menuPlaced)PlaceMenu(true);
        }
        public void CloseMenu()
        {
            menu.gameObject.SetActive(false);View.Session.SetPaused(wasPaused);Revision++;
            if(!PlayingTour)View.Refresh();
            Garden.Navigation?.InvalidatePointerPresses();
        }
        public void ToggleMenu()
        {
            if (Garden.M5.Extensions.PanelOpen) { Garden.M5.Extensions.Close();OpenMenu("tools");return; }
            if(MenuVisible)CloseMenu();else OpenMenu(PlayingTour?"playback":Garden.Navigation.Page==NavigationPage.Tree?"tree":"home");
        }
        private void BuildMenu()
        {
            foreach(Transform child in content){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            title.text="MODEL FOREST";subtitle.text=View.Model.Trees.Count+" trees · "+(View.LocalSampleActive?"Local sample · synthetic profiles":"Selected model");
            switch(MenuPage)
            {
                case "home":
                    Row("One tree · choose in the forest","single",0);Row("Explore the whole forest","forest",1);
                    Row("Play a prepared profile","profiles",2);Row("Compare profiles A / B","compare",3);
                    Row(ResultsVisible?"Table View · results":"Table View","table",4);break;
                case "tree":
                    title.text="TREE "+(View.Ensemble.Index+1)+" / "+View.Model.Trees.Count;
                    Row("Previous decision","tree-back",0,0,true);Row("Restart tree","tree-restart",0,1,true);
                    Row("Back to forest","return-forest",1,0,true);Row("Table View","table",1,1,true);
                    Row("Current decision","focus-current",2,0,true);Row("Seated / standing","seated",2,1,true);
                    Row("Continue exploring","close",3);break;
                case "position":
                    title.text="POSITION YOUR SCENE";subtitle.text="Place the forest in front of you. Tracking remains active.";
                    Row("Place in front of me","recenter",0);
                    Row("Closer","closer",1,0,true);Row("Farther","farther",1,1,true);
                    Row("Move left","left",2,0,true);Row("Move right","right",2,1,true);
                    Row("Turn left","turn-left",3,0,true);Row("Turn right","turn-right",3,1,true);
                    Row("Menu lower","lower",4,0,true);Row("Menu higher","higher",4,1,true);break;
                case "profiles":case "compare":
                    title.text=MenuPage=="compare"?"COMPARE TWO PROFILES":"PLAY A PREPARED PROFILE";
                    subtitle.text="Synthetic inputs · same complete "+View.Model.Trees.Count+"-tree model";
                    if(View.AvailableProfiles.Profiles.Count==0){subtitle.text="No compatible prepared profiles are loaded.";break;}
                    Row("A: "+View.AvailableProfiles.Profiles[SelectedA].DisplayName,"next-a",0);
                    if(MenuPage=="compare")Row("B: "+View.AvailableProfiles.Profiles[SelectedB%View.AvailableProfiles.Profiles.Count].DisplayName,"next-b",1);
                    Row("Review selected profile values","values",2);
                    Row("Start · visit every tree",MenuPage=="compare"?"start-ab":"start-profile",3);break;
                case "values":
                    title.text="SYNTHETIC PROFILE INPUTS";subtitle.text="Fully supplied values; no missing-value routing is inferred.";
                    var profile=View.AvailableProfiles.Profiles[SelectedA];
                    var values=profile.Values.Skip(valuePage*7).Take(7).Select(v=>v.Key+" = "+Value(v.Value));
                    var label=Garden.Text(content,"Values",string.Join("\n",values),new Vector2(0,20),new Vector2(970,380),23);label.richText=false;
                    Row("Previous values","values-prev",4,0,true);Row("Next values","values-next",4,1,true);break;
                case "playback":
                    title.text="PROFILE PLAYBACK";subtitle.text=Playback==null?"No active run":"Tree "+(Playback.TreeIndex+1)+" / "+View.Model.Trees.Count;
                    Row(Playback?.Paused==true?"Resume":"Pause","pause",0,0,true);Row("Next step","step",0,1,true);
                    Row("Previous tree","previous-tree",1,0,true);Row("Speed: "+(Playback?.Speed??1).ToString("0.#")+"x","speed",1,1,true);
                    Row("Restart all trees","restart",2,0,true);Row("Calculate whole model","calculate-all",2,1,true);Row("Continue viewing","close",3);Row("End playback · forest","stop",4);break;
                case "result":
                    title.text="COMPLETE MODEL RESULT";subtitle.text="All "+View.Model.Trees.Count+" trees evaluated · source scoring unverified";
                    var result=Garden.Text(content,"Result",ResultCopy(),new Vector2(0,75),new Vector2(960,260),32);result.enableAutoSizing=true;result.fontSizeMin=24;result.fontSizeMax=32;
                    Row("View the whole model with results? · Yes","results-forest",3);
                    Row("Table View with results","results-table",4);break;
                case "tools":
                    title.text="TOOLS AND SETTINGS";
                    Row("Sound: "+(Garden.M5.Atmosphere.SoundEnabled?"on":"off"),"audio",0,0,true);Row("Constellation guides","stars",0,1,true);
                    Row("Search predictors","search",1,0,true);Row("Boosting trail","trail",1,1,true);
                    Row("Recorded reviews","review",2,0,true);Row("Help","help",2,1,true);
                    Row("Table: smaller","table-smaller",3,0,true);Row("Table: larger","table-larger",3,1,true);
                    Row("Table: turn left","table-left",4,0,true);Row("Table: turn right","table-right",4,1,true);break;
                case "help":
                    title.text="HOW TO EXPLORE";subtitle.text="A / Tab: menu · B / R: center scene · X: close";
                    Garden.Text(content,"Help","Point and press the trigger to select.\nLeft stick: walk. Right stick: turn.\n\nChoose a tree in the forest for manual branches.\nPlay profiles to see every tree in order.\nGold = A · Purple = B\nBlue result = positive · Red = negative\n\nPosition scene places the forest in front of you.",new Vector2(0,0),new Vector2(970,440),27);break;
            }
        }
        private int valuePage;
        public void Activate(string command)
        {
            if(!View.Ready)return;
            Revision++;
            switch(command)
            {
                case "home":case "position":case "profiles":case "compare":case "tools":case "help":case "values":OpenMenu(command);return;
                case "close":CloseMenu();return;
                case "next-a":SelectedA=(SelectedA+1)%View.AvailableProfiles.Profiles.Count;BuildMenu();return;
                case "next-b":SelectedB=(SelectedB+1)%View.AvailableProfiles.Profiles.Count;BuildMenu();return;
                case "values-prev":valuePage=Math.Max(0,valuePage-1);BuildMenu();return;
                case "values-next":valuePage=(valuePage+1)%Math.Max(1,(View.Model.Features.Count+6)/7);BuildMenu();return;
                case "start-profile":StartPlayback(false);return;
                case "start-ab":StartPlayback(true);return;
                case "forest":case "single":StopPlayback(false);View.StartScenarioProfiles(-1);CloseMenu();Garden.Navigation.ShowForest();return;
                case "table":ShowResults(true);return;
                case "results-forest":ShowResults(false);return;
                case "results-table":ShowResults(true);return;
                case "pause":if(Playback!=null)Playback.Paused=!Playback.Paused;BuildMenu();return;
                case "step":if(Playback!=null){Playback.Next();RenderPlayback();}return;
                case "calculate-all":if(Playback!=null){Playback.FinishAll();RenderPlayback();}return;
                case "previous-tree":Playback?.PreviousTree();builtTree=-1;RenderPlayback();return;
                case "speed":if(Playback!=null)Playback.Speed=Playback.Speed>=4?.5:Playback.Speed*2;BuildMenu();return;
                case "restart":Playback?.Restart();completeShown=false;ResultsVisible=false;PlayingTour=true;builtTree=-1;CloseMenu();RenderPlayback();return;
                case "tree-back":View.Session.Back();View.Refresh();CloseMenu();return;
                case "tree-restart":View.Session.Restart();View.Refresh();CloseMenu();return;
                case "return-forest":CloseMenu();Garden.Navigation.ShowForest();return;
                case "focus-current":View.FocusedView.CurrentFocus();View.Refresh();CloseMenu();return;
                case "seated":View.Execute(TreeAction.Seated,View.Session.State.Revision,View.Session.State.NodeId);return;
                case "stop":StopPlayback(false);CloseMenu();Garden.Navigation.ShowForest();return;
                case "audio":Garden.M5.Atmosphere.SoundEnabled=!Garden.M5.Atmosphere.SoundEnabled;BuildMenu();return;
                case "stars":Garden.M5.Atmosphere.Sky.ToggleGuides();return;
                case "recenter":RecenterScene();return;
                case "closer":Garden.Locomotion.AdjustPlacement(0,-.5f,0);break;
                case "farther":Garden.Locomotion.AdjustPlacement(0,.5f,0);break;
                case "left":Garden.Locomotion.AdjustPlacement(-.5f,0,0);break;
                case "right":Garden.Locomotion.AdjustPlacement(.5f,0,0);break;
                case "turn-left":Garden.Locomotion.AdjustPlacement(0,0,-15);break;
                case "turn-right":Garden.Locomotion.AdjustPlacement(0,0,15);break;
                case "lower":menuHeight=Mathf.Max(-.5f,menuHeight-.1f);break;
                case "higher":menuHeight=Mathf.Min(.2f,menuHeight+.1f);break;
                case "table-smaller":Garden.M5.Diorama.ChangeScale(-.1f);return;
                case "table-larger":Garden.M5.Diorama.ChangeScale(.1f);return;
                case "table-left":Garden.M5.Diorama.Rotate(-15);return;
                case "table-right":Garden.M5.Diorama.Rotate(15);return;
                case "search":case "trail":case "review":
                    CloseMenu();Garden.M5.Activate(command=="search"?M5Action.ExtensionSearch:command=="trail"?M5Action.ExtensionTrail:M5Action.ExtensionReview);return;
            }
            PlaceMenu(true);if(PlayingTour)PlaceRoute();
        }
        public void StartPlayback(bool comparison)
        {
            CloseMenu(); StopPlayback(false);
            View.StartScenarioProfiles(SelectedA,comparison?SelectedB%View.AvailableProfiles.Profiles.Count:-1);
            Playback=new ForestPlayback(View.Ensemble.Evaluation,comparison?View.Comparison.B.Evaluation:null){DecisionSeconds=.45,MoveSeconds=.3,ResultSeconds=1.2};
            runA=View.AvailableProfiles.Profiles[SelectedA].DisplayName;runB=comparison?View.AvailableProfiles.Profiles[SelectedB%View.AvailableProfiles.Profiles.Count].DisplayName:null;
            completeShown=false;ResultsVisible=false;PlayingTour=true;builtTree=-1;
            Garden.Navigation.ShowForest();Garden.Navigation.OpenTree(0);PlaceRoute();RenderPlayback();
        }
        public void StopPlayback(bool retainResults)
        { PlayingTour=false;routeRoot.gameObject.SetActive(false);if(!retainResults){ResultsVisible=false;Playback=null;}resultCanvas.gameObject.SetActive(false); }
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
            PlaceMenu(true);Garden.Navigation.InvalidatePointerPresses();
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
            // The menu stays anchored in the world until an explicit placement action.
            Garden.Navigation.HideLegacyMenus();Garden.M5.HideLegacyTools();Garden.M6.RefreshVisibility();
            if(PlayingTour) {
                View.presentationRoot.gameObject.SetActive(true);routeRoot.gameObject.SetActive(true);View.drop.gameObject.SetActive(false);
                View.explanation.gameObject.SetActive(false);View.score.gameObject.SetActive(false);View.feedback.gameObject.SetActive(false);View.status.gameObject.SetActive(false);
            }
            if(MenuVisible || PlayingTour) { foreach(var c in View.controls)c.gameObject.SetActive(false); }
            if(ResultsVisible && Garden.Navigation.Page!=NavigationPage.Tree)PlaceResult(Garden.Navigation.Page==NavigationPage.Diorama);
            else resultCanvas.gameObject.SetActive(false);
        }
        private void PlaceMenu(bool immediate)
        {
            menuPlaced=true;
            var head=Garden.Locomotion.Head;var forward=Vector3.ProjectOnPlane(head.forward,Vector3.up).normalized;if(forward.sqrMagnitude<.1f)forward=Vector3.forward;
            var position=head.position+forward*1.45f+Vector3.up*menuHeight;var rotation=Quaternion.LookRotation(forward);
            float t=immediate?1:1-Mathf.Exp(-Time.unscaledDeltaTime*4);
            menu.transform.SetPositionAndRotation(Vector3.Lerp(menu.transform.position,position,t),Quaternion.Slerp(menu.transform.rotation,rotation,t));
        }
        private void PlaceRoute()
        { routeRoot.localPosition=Vector3.zero;routeRoot.localRotation=Quaternion.identity; }
        private void PlaceResult(bool table)
        {
            resultCanvas.gameObject.SetActive(ResultsVisible);if(!ResultsVisible)return;
            resultText.text="A: "+Percent(Playback.A.Probability)+" · Score "+Number(Playback.A.RawScore)+(Playback.B==null?"":"\nB: "+Percent(Playback.B.Probability)+" · Score "+Number(Playback.B.RawScore))+"\nAll "+Playback.A.Trees.Count+" trees · blue positive / red negative\nProbability = sigmoid(Score)";var head=Garden.Locomotion.Head;
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
            string heading="TREE "+(builtTree+1)+" / "+View.Model.Trees.Count+" · "+Playback.Speed.ToString("0.#")+"x · A / Tab: menu\n";
            if(Playback.Phase==PlaybackPhase.TreeResult)explanation.text=heading+TreeCopy(Playback.A,"A")+(Playback.B==null?"":"\n"+TreeCopy(Playback.B,"B"))+"\nNext tree follows automatically.";
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
        private string ResultCopy()
        {
            if(Playback==null)return "";
            string text="A · "+runA+"\nProbability "+Percent(Playback.A.Probability)+" · Score "+Number(Playback.A.RawScore);
            if(Playback.B!=null)text+="\nB · "+runB+"\nProbability "+Percent(Playback.B.Probability)+" · Score "+Number(Playback.B.RawScore)+"\nDifference B − A: "+Signed((Playback.B.Probability-Playback.A.Probability)*100)+" percentage points";
            return text+"\nProbability = 1 / (1 + exp(−Score))\nAll "+Playback.A.Trees.Count+" trees · source scoring unverified";
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
        {var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name=name;go.transform.SetParent(routeRoot,false);go.transform.localPosition=position;go.transform.localScale=Vector3.one*size;go.GetComponent<Renderer>().sharedMaterial=Material(color);Destroy(go.GetComponent<Collider>());return go;}
        private static string Number(double v)=>v.ToString("0.######",CultureInfo.InvariantCulture);
        private static string Signed(double v)=>v.ToString("+0.######;-0.######;0",CultureInfo.InvariantCulture);
        private static string Percent(double v)=>(v*100).ToString("0.0000",CultureInfo.InvariantCulture)+"%";
        private static string Value(ProfileValue v)=>v.Kind==ValueKind.Number?Number(v.Number):v.Kind==ValueKind.Category?v.Category:"Missing";
        private void OnDestroy(){foreach(var mat in materials)if(mat!=null)Destroy(mat);if(menu!=null)Destroy(menu.gameObject);if(routeRoot!=null)Destroy(routeRoot.gameObject);if(resultCanvas!=null)Destroy(resultCanvas.gameObject);}
    }
    public sealed class ExperienceMenuTarget : MonoBehaviour, IPointerDownHandler, IPointerClickHandler, IPointerExitHandler
    {
        public ExperienceDirector Owner;public string Command;
        private long revision=-1;private int pointer;
        public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left){revision=Owner.Revision;pointer=e.pointerId;}}
        public void OnPointerClick(PointerEventData e){if(revision==Owner.Revision&&pointer==e.pointerId&&e.button==PointerEventData.InputButton.Left&&gameObject.activeInHierarchy){revision=-1;Owner.Activate(Command);}}
        public void OnPointerExit(PointerEventData e){revision=-1;}
    }
}
