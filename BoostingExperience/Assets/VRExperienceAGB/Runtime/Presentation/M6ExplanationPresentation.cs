using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using VRExperienceAGB.Domain;
using VRExperienceAGB.Import;

namespace VRExperienceAGB.Presentation
{
    /// <summary>Read-only in-world explanations. Refreshes from accepted state; never drives evaluation or tracking.</summary>
    public sealed class M6ExplanationPresentation : MonoBehaviour
    {
        public ForestExplanation Analysis { get; private set; }
        public string LinkedFeature { get; private set; }
        public TMP_Text SegmentText { get; private set; }
        public TMP_Text EvidenceText { get; private set; }
        public TMP_Text StoryText { get; private set; }
        public IReadOnlyList<TMP_Text> EdgeLabels => edges;
        private ForestGardenView garden;
        private OneTreeExperience View => garden.Experience;
        private Transform root;
        private Canvas segment, evidence, story;
        private readonly List<TMP_Text> edges = new List<TMP_Text>();
        private readonly List<TMP_Text> badges = new List<TMP_Text>();
        private readonly List<RectTransform> ticks = new List<RectTransform>();
        private RectTransform currentTick;
        private TMP_Text spectrum;
        private string inspected;
        private object hoverOwner;
        private VRExperienceAGB.Application.TreeSession describedSession;
        private long describedRevision = -1;
        private string describedFocus;
        private string pendingFeature;
        private float linkAt;
        private float nextFacing;
        private readonly List<Canvas> facing = new List<Canvas>();
        private static readonly Color[] FamilyColors = { new Color(.6f,.48f,.9f), new Color(.24f,.55f,.92f), new Color(.88f,.7f,.3f), new Color(.6f,.65f,.68f) };
        public void Configure(ForestGardenView owner)
        {
            garden = owner;
            root = new GameObject("M6InWorldExplanations").transform; root.SetParent(View.transform, false);
            segment = Card("SegmentPortrait", new Vector3(-1.5f,.72f,3), new Vector2(680,480), .0015f);
            SegmentText = Text(segment, "Segment", new Vector2(0,0), new Vector2(620,430), 26);
            Hoverable(SegmentText, null, "segment");
            evidence = Card("NodeEvidence", new Vector3(1.5f,.72f,3), new Vector2(680,480), .0015f);
            EvidenceText = Text(evidence, "Evidence", new Vector2(0,65), new Vector2(620,310), 26);
            Hoverable(EvidenceText, null, "evidence");
            spectrum = Text(evidence, "ThresholdSpectrum", new Vector2(0,-181), new Vector2(620,88), 19);
            for(int i=0;i<64;i++) ticks.Add(Tick("Cutpoint", new Color(.7f,.68f,.95f), 3, 18));
            currentTick = Tick("CurrentCutpoint", Color.white, 7, 28);
            story = Card("ForestStory", new Vector3(-1.25f,1.8f,4.5f), new Vector2(1060,720), .0016f);
            StoryText = Text(story, "ModelStory", Vector2.zero, new Vector2(990,660), 32);
            StoryText.enableAutoSizing=true;StoryText.fontSizeMin=28;StoryText.fontSizeMax=32;
            for (int i = 0; i < 6; i++) {
                var card=Card("BranchExplanation-"+i, Vector3.zero, new Vector2(430,92), .0022f);
                var label=Text(card,"Condition",Vector2.zero,new Vector2(410,82),28); edges.Add(label);
                Hoverable(label,null,"edge");
            }
            foreach(var slot in View.nodeViews) {
                var label=garden.Text(slot.title.transform.parent,"PredictorLink","",new Vector2(0,80),new Vector2(350,35),18);
                badges.Add(label);
            }
        }
        private Canvas Card(string name, Vector3 p, Vector2 size, float scale)
        {
            var canvas=garden.CanvasAt(name,p,size,scale,root);
            var image=canvas.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color=new Color(.018f,.046f,.063f,.96f); image.raycastTarget=false;
            facing.Add(canvas); return canvas;
        }
        private TMP_Text Text(Canvas canvas,string name,Vector2 p,Vector2 size,int font)
        {
            var text=garden.Text(canvas.transform,name,"",p,size,font);text.alignment=TextAlignmentOptions.TopLeft;text.richText=false;return text;
        }
        private void Hoverable(TMP_Text text,string id,string kind)
        {
            text.raycastTarget=true; var hover=text.gameObject.AddComponent<M6ExplanationHover>();hover.Owner=this;hover.NodeId=id;hover.Kind=kind;
        }
        private RectTransform Tick(string name,Color color,float width,float height)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));go.transform.SetParent(evidence.transform,false);
            var rect=(RectTransform)go.transform;rect.sizeDelta=new Vector2(width,height);var image=go.GetComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=false;return rect;
        }
        public void EnsureModel()
        {
            if (Analysis?.Model == View.Model) return;
            Analysis=new ForestExplanation(View.Model,View.ExportPreview);LinkedFeature=null;inspected=null;hoverOwner=null;pendingFeature=null;
            View.NameTooltip?.Hide(); StoryText.text=Analysis.ModelStory();
        }
        public string Detail(string id) { EnsureModel(); return Analysis.NodeDetail(View.Ensemble.Index,id); }
        public void Point(object owner,string id)
        {
            EnsureModel();
            if(!Analysis.Trees[View.Ensemble.Index].Nodes.ContainsKey(id))return;
            hoverOwner=owner;inspected=id;
            var node=Analysis.Trees[View.Ensemble.Index].Nodes[id];
            pendingFeature=(node as SplitNode)?.FeatureId;linkAt=Time.unscaledTime+.45f;
            Refresh();
        }
        public void Leave(object owner) { if(ReferenceEquals(hoverOwner,owner)) { hoverOwner=null;inspected=null;pendingFeature=null;Refresh(); } }
        public void LinkFeature(string feature)
        {
            EnsureModel(); LinkedFeature=Analysis.Occurrences(feature)>0?feature:null;
            garden.RefreshExplanationLinks(); Refresh();
        }
        public void Refresh()
        {
            if(garden==null||!View.Ready)return;
            EnsureModel();
            if(describedSession!=View.Session || describedRevision!=View.Session.State.Revision || describedFocus!=View.FocusedView.FocusRoot) {
                describedSession=View.Session;describedRevision=View.Session.State.Revision;describedFocus=View.FocusedView.FocusRoot;
                inspected=null;hoverOwner=null;pendingFeature=null;View.NameTooltip?.Hide();
            }
            bool tree=garden.Navigation?.Page==NavigationPage.Tree && !garden.Navigation.TreeMenuOpen && !garden.M5.HelpOpen;
            bool forest=garden.Navigation?.Page==NavigationPage.Forest && !garden.M5.HelpOpen;
            segment.gameObject.SetActive(tree); evidence.gameObject.SetActive(tree); story.gameObject.SetActive(forest);
            StoryText.text=Analysis.ModelStory()+(LinkedFeature==null?"":"\nLINKED: "+Analysis.Label(LinkedFeature)+" · "+Analysis.Occurrences(LinkedFeature)+" splits / "+Analysis.MatchingTrees(LinkedFeature).Length+" trees");
            if(!tree) foreach(var edge in edges)edge.transform.parent.gameObject.SetActive(false);
            if(!tree) { inspected=null;pendingFeature=null;return; }
            int index=View.Ensemble.Index;var modelTree=Analysis.Trees[index];
            if(inspected!=null&&!modelTree.Nodes.ContainsKey(inspected))inspected=null;
            string id=inspected??View.FocusedView.FocusRoot??View.Session.State.NodeId;
            var node=modelTree.Nodes[id];
            float shift=View.IsSeated?-.4f:0;
            segment.transform.localPosition=new Vector3(-1.5f,Mathf.Max(.5f,.72f+shift),3);
            evidence.transform.localPosition=new Vector3(1.5f,Mathf.Max(.5f,.72f+shift),3);
            string portrait=Analysis.Segment(index,id);
            var conflicts=View.Ensemble.Consistency.Conflicts;
            var relevant=conflicts.Where(c=>modelTree.Nodes[id] is SplitNode s ? c.FeatureId==s.FeatureId : true).Select(c=>Analysis.Label(c.FeatureId)).Distinct();
            SegmentText.text="SEGMENT AT "+id+"\n"+ShortLines(portrait,5)+
                (relevant.Any()?"\nCONFLICT: "+string.Join(", ",relevant):"")+"\nPoint here for all conditions";
            if(node is SplitNode split) {
                EvidenceText.text=ShortLines(Analysis.Kind(split),1)+"\n"+Analysis.NodeEvidence(index,id)+"\n"+
                    Analysis.Occurrences(split.FeatureId)+" matching splits · hold pointer to link";
            } else {
                double contribution=((LeafNode)node).Score*modelTree.Tree.Weight;
                bool current=id==View.Session.State.NodeId;
                EvidenceText.text="LEAF · raw contribution "+ForestExplanation.Number(contribution)+"\n"+
                    (current?"Previous subtotal "+ForestExplanation.Number(View.Ensemble.RouteTotal-contribution)+" → "+ForestExplanation.Number(View.Ensemble.RouteTotal):"Inspection only · route unchanged")+"\n"+
                    (View.Ensemble.Profile!=null&&View.Ensemble.Evaluation!=null?"Full profile result "+ForestExplanation.Number(100*View.Ensemble.Evaluation.Probability)+"% · all "+View.Model.Trees.Count+" trees": "Manual route · no profile probability")+"\n"+Analysis.NodeEvidence(index,id);
            }
            Spectrum(node as SplitNode);
            if(garden.simplifiedNavigation && View.FocusedView.FocusRoot==null) {
                View.explanation.text=node==View.Session.CurrentNode && node is SplitNode current?Analysis.Question(current):View.Session.CurrentNode is SplitNode decisionNode ? Analysis.Question(decisionNode) : "Leaf reached · see contribution beside the tree";
                var decision=View.Session.CurrentProfileDecision;
                if(decision!=null)View.explanation.text+="\nProfile: "+Value(decision.ObservedValue)+" → "+(decision.Matched?"TRUE":"FALSE");
                View.score.text="Tree "+(index+1)+" / "+View.Model.Trees.Count+" · "+(View.Ensemble.Profile==null?"Manual route · no profile probability":"Prepared profile · full ensemble evaluated")+"\nPoint at labels for evidence · A: tree menu";
                foreach(var control in View.controls.Where(c=>c.action==TreeAction.TrueBranch||c.action==TreeAction.FalseBranch))
                    if(View.Session.CurrentNode is SplitNode branch)control.label.text=ShortBranch(branch,control.action==TreeAction.TrueBranch);
            }
            int edgeIndex=0;
            foreach(var slot in View.nodeViews.Where(s=>s.gameObject.activeSelf)) {
                if(!modelTree.Nodes.TryGetValue(slot.nodeId,out var source))continue;
                slot.title.richText=false;
                if(source is SplitNode question)slot.title.text=Analysis.Question(question);
                int si=Array.IndexOf(View.nodeViews,slot);
                bool linked=source is SplitNode match&&match.FeatureId==LinkedFeature;
                badges[si].text=(source is SplitNode missing&&missing.Condition.Operator==DecisionOperator.IsMissing?"[MISSING] ":"")+(linked?"LINKED":"");
                if(!(source is SplitNode parent))continue;
                foreach(bool matched in new[]{true,false}) {
                    string child=matched?parent.TrueChild:parent.FalseChild;
                    var target=View.nodeViews.FirstOrDefault(s=>s.gameObject.activeSelf&&s.nodeId==child);
                    if(target==null||edgeIndex>=edges.Count)continue;
                    var label=edges[edgeIndex++];label.transform.parent.gameObject.SetActive(true);
                    label.transform.parent.position=Vector3.Lerp(slot.transform.position,target.transform.position,.48f)+new Vector3(0,-.14f,-.12f);
                    label.text=ShortBranch(parent,matched);
                    bool taken=View.Session.State.Decisions.Any(d=>d.NodeId==parent.Id&&d.Matched==matched) ||
                        (View.Session.CurrentProfileDecision!=null && parent.Id==View.Session.State.NodeId && View.Session.CurrentProfileDecision.Matched==matched);
                    label.color=taken?new Color(.6f,1f,.75f):Color.white;
                    if(taken)label.text="PATH · "+label.text;
                    var hover=label.GetComponent<M6ExplanationHover>();hover.NodeId=parent.Id;
                }
            }
            for(;edgeIndex<edges.Count;edgeIndex++)edges[edgeIndex].transform.parent.gameObject.SetActive(false);
        }
        private string ShortBranch(SplitNode node,bool matched)
        {
            var c=node.Condition;
            return c.Operator==DecisionOperator.IsMissing?(matched?"MISSING":"PRESENT"):
                c.Operator==DecisionOperator.LessThan?(matched?"< ":">= ")+ForestExplanation.Number(c.Threshold.Value):
                (matched?"IN ":"OUTSIDE ")+c.Categories.Count+" values · point for list";
        }
        private void Spectrum(SplitNode node)
        {
            foreach(var tick in ticks)tick.gameObject.SetActive(false);currentTick.gameObject.SetActive(false);
            if(node==null||node.Condition.Operator!=DecisionOperator.LessThan){spectrum.text="Point for full evidence and source condition";return;}
            var values=Analysis.Thresholds(node.FeatureId);double min=values.First(),max=values.Last();
            // Bounded visual bins preserve every source cutpoint in the exact read-only detail.
            var bins=new HashSet<int>();foreach(var value in values)bins.Add(max==min?32:(int)Math.Round(63*((value*.5-min*.5)/(max*.5-min*.5))));
            int i=0;foreach(int bin in bins){var tick=ticks[i++];tick.gameObject.SetActive(true);tick.anchoredPosition=new Vector2(-270+540*bin/63f,-130);}
            currentTick.gameObject.SetActive(true);currentTick.anchoredPosition=new Vector2(max==min?0:(float)(-270+540*((node.Condition.Threshold.Value*.5-min*.5)/(max*.5-min*.5))),-130);
            spectrum.text="THRESHOLDS · "+ForestExplanation.Number(min)+" … "+ForestExplanation.Number(max)+"\nWhite: current "+ForestExplanation.Number(node.Condition.Threshold.Value)+" · "+values.Length+" distinct cuts\n64 display bins · point for exact values";
        }
        private static string ShortLines(string text,int count)
        {
            var lines=text.Split('\n');var selected=lines.Take(count).Select(l=>l.Length>65?l.Substring(0,62)+"…":l);
            return string.Join("\n",selected)+(lines.Length>count?"\n+ "+(lines.Length-count)+" more conditions":"");
        }
        private static string Value(ProfileValue value) => value.Kind==ValueKind.Number?ForestExplanation.Number(value.Number):value.Kind==ValueKind.Category?value.Category:value.Kind==ValueKind.Missing?"missing":"not supplied";
        private void Update()
        {
            if(pendingFeature!=null&&Time.unscaledTime>=linkAt){var feature=pendingFeature;pendingFeature=null;LinkFeature(feature);}
            if(Time.unscaledTime<nextFacing||garden==null)return;nextFacing=Time.unscaledTime+.15f;
            foreach(var canvas in facing.Where(c=>c.gameObject.activeInHierarchy)){
                var direction=canvas.transform.position-garden.Locomotion.Head.position;
                if(direction.sqrMagnitude>.01f)canvas.transform.rotation=Quaternion.LookRotation(direction);
            }
        }
        public void ShowHover(M6ExplanationHover source)
        {
            string id=source.NodeId??View.FocusedView.FocusRoot??View.Session.State.NodeId;
            Point(source,id);View.NameTooltip.Show(source,Detail(id));
        }
        public void AddComposition(Transform parent,int index,Vector3 position,float scale)
        {
            EnsureModel();var canvas=garden.CanvasAt("PredictorComposition",position,new Vector2(600,56),scale,parent);
            var gains=Analysis.Trees[index];float left=-300;
            if(!gains.Gain.HasValue||gains.Gain<=0){garden.Text(canvas.transform,"Unavailable",gains.Gain==0?"No split gain":"Gain composition unavailable",Vector2.zero,new Vector2(600,56),21);return;}
            for(int i=0;i<ForestExplanation.Families.Length;i++){
                double share=gains.FamilyGains[ForestExplanation.Families[i]]/gains.Gain.Value;if(share<=0)continue;
                float width=600*(float)share;var go=new GameObject(ForestExplanation.Families[i],typeof(RectTransform),typeof(UnityEngine.UI.Image));go.transform.SetParent(canvas.transform,false);
                var rect=(RectTransform)go.transform;rect.sizeDelta=new Vector2(width,25);rect.anchoredPosition=new Vector2(left+width*.5f,14);left+=width;
                var image=go.GetComponent<UnityEngine.UI.Image>();image.color=FamilyColors[i];image.raycastTarget=false;
            }
            garden.Text(canvas.transform,"Legend","H / C / X / O · stored gain",new Vector2(0,-18),new Vector2(600,30),19);
        }
        private void OnDestroy(){if(root!=null)Destroy(root.gameObject);}
    }
    public sealed class M6ExplanationHover : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        public M6ExplanationPresentation Owner;public string NodeId;public string Kind;
        public void OnPointerEnter(PointerEventData data){if(Owner!=null)Owner.ShowHover(this);}
        public void OnPointerExit(PointerEventData data){if(Owner!=null){Owner.Leave(this);Owner.GetComponent<ForestGardenView>().Experience.NameTooltip.Leave(this);}}
        private void OnDisable(){if(Owner!=null)Owner.Leave(this);}
    }
}
