using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRExperienceAGB.Application;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Presentation
{
    /// <summary>Reuses seven platforms for arbitrarily deep trees; never moves the user's camera.</summary>
    public sealed class FocusedTreeView
    {
        private readonly OneTreeExperience view;
        private readonly TreeNodeView[] slots;
        private readonly Vector3[] positions;
        private readonly LineRenderer[] routes;
        private TreeNeighborhood neighborhood;
        private ModelTree tree;
        private string focus;
        private readonly Stack<string> focusHistory = new Stack<string>();
        public string FocusRoot => focus;
        public int FocusDepth => focusHistory.Count;
        public bool Inspect(string id)
        {
            if (tree == null || !tree.Nodes.Any(n => n.Id == id)) return false;
            focusHistory.Push(focus ?? view.Session.State.NodeId); focus = id; return true;
        }
        public bool Focus(string id)
        {
            if (tree == null || !tree.Nodes.Any(n => n.Id == id)) return false;
            var visible = slots.Where(s => s.gameObject.activeSelf).Select(s => s.nodeId);
            if (!visible.Contains(id) || id == focus) return false;
            focusHistory.Push(focus ?? view.Session.State.NodeId); focus = id; return true;
        }
        public void ParentFocus()
        {
            if (focusHistory.Count == 0) return;
            focus = focusHistory.Pop();
            if (focusHistory.Count == 0) focus = null;
        }
        public void CurrentFocus() { focus = null; focusHistory.Clear(); }
        public int Hidden(string id)
        {
            if (neighborhood == null || !tree.Nodes.Any(n => n.Id == id)) return 0;
            var visible = slots.Where(s => s.gameObject.activeSelf).Select(s => s.nodeId).ToArray();
            var lookup = tree.Nodes.ToDictionary(n => n.Id);
            return neighborhood.Descendants(id) - visible.Count(n => n != id && IsDescendant(lookup, id, n));
        }
        public FocusedTreeView(OneTreeExperience view)
        {
            this.view = view; slots = view.nodeViews;
            positions = slots.Select(n => n.transform.localPosition).ToArray();
            routes = view.presentationRoot.GetComponentsInChildren<LineRenderer>(true)
                .Where(l => l.transform.parent == view.presentationRoot).Take(6).ToArray();
        }
        public void Bind(ModelTree modelTree)
        { tree = modelTree; neighborhood = new TreeNeighborhood(tree); CurrentFocus(); }
        public void Refresh(SessionState state, bool map)
        {
            string root = focus ?? (map ? tree.RootId : state.NodeId);
            var ids = neighborhood.Visible(root);
            var lookup = tree.Nodes.ToDictionary(n => n.Id);
            var addresses = new string[7]; addresses[0] = root;
            for (int i = 0; i < 3; i++)
                if (addresses[i] != null && lookup[addresses[i]] is SplitNode parent)
                { addresses[i * 2 + 1] = parent.TrueChild; addresses[i * 2 + 2] = parent.FalseChild; }
            var visible = new Dictionary<string, Vector3>();
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i]; bool active = addresses[i] != null;
                slot.gameObject.SetActive(active); slot.title.transform.parent.gameObject.SetActive(active);
                if (!active) continue;
                bool compact = view.Garden?.simplifiedNavigation == true;
                var position = positions[i];
                if(compact)position.y = i==0 ? 1.3f : i<3 ? 1.5f : 1.85f;
                slot.nodeId = addresses[i]; slot.transform.localPosition = position;
                slot.dropAnchor.localPosition = position + Vector3.up * .29f;
                slot.title.transform.parent.localPosition = position + new Vector3(0, compact ? .48f : i < 3 ? .32f : .36f, -.16f);
                slot.title.transform.parent.localScale = Vector3.one * (compact ? .0035f : .0045f);
                var node = lookup[addresses[i]];
                bool preview = view.Model.StructureOnlyPreview;
                slot.title.enableAutoSizing = preview;
                slot.title.fontSizeMin = compact ? 16 : 18; slot.title.fontSizeMax = compact ? 22 : 29;
                slot.title.rectTransform.sizeDelta = new Vector2(compact ? 300 : preview ? 400 : i == 0 ? 610 : 520, compact || preview ? 110 : 65);
                slot.marker.rectTransform.anchoredPosition = new Vector2(0, compact ? -70 : preview ? -64 : -37);
                if(compact) { slot.marker.fontSize=15; slot.marker.rectTransform.sizeDelta=new Vector2(300,30); }
                slot.title.textWrappingMode = TMPro.TextWrappingModes.Normal;
                slot.title.text = node is SplitNode split ? (preview && split.Condition.Categories.Count > 3 ?
                    view.Model.Features.Single(f => f.Id == split.FeatureId).DisplayName + " in set (" + split.Condition.Categories.Count + " categories)" : view.Condition(split, true)) : "LEAF  " + (tree.Weight * ((LeafNode)node).Score).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
                int hidden = neighborhood.Descendants(addresses[i]) - ids.Count(id => id != addresses[i] && IsDescendant(lookup, addresses[i], id));
                slot.marker.text = (state.NodeId == addresses[i] ? "CURRENT" : state.Path.Contains(addresses[i]) ? "VISITED" : "") +
                    (hidden > 0 ? "  +" + hidden + " hidden" : "");
                slot.title.enabled = i != 0 || focus != null;
                slot.marker.enabled = i != 0 && state.NodeId != addresses[i];
                if (view.Comparison != null)
                {
                    var compared = view.Comparison.Trees[view.Ensemble.Index];
                    bool inA = compared.A.VisitedNodeIds.Contains(addresses[i]);
                    bool inB = compared.B.VisitedNodeIds.Contains(addresses[i]);
                    slot.marker.text = (inA && inB ? "A + B" : inA ? "A ONLY" : inB ? "B ONLY" : "NEITHER PATH") +
                        (state.NodeId == addresses[i] ? " · CURRENT" : "") + (hidden > 0 ? " · +" + hidden + " hidden" : "");
                    slot.marker.enabled = true;
                }
                slot.platform.sharedMaterial = state.NodeId == addresses[i] ? view.activeMaterial : state.Path.Contains(addresses[i]) ? view.visitedMaterial : view.idleMaterial;
                visible[addresses[i]] = position;
            }
            int edge = 0;
            foreach (string id in ids)
            if (lookup[id] is SplitNode split)
            foreach (string child in new[] { split.TrueChild, split.FalseChild })
            {
                if (!visible.ContainsKey(child) || edge >= routes.Length) continue;
                var line = routes[edge++]; line.gameObject.SetActive(true); line.positionCount = 24;
                for (int j = 0; j < 24; j++)
                { float t = j / 23f; var p = Vector3.Lerp(visible[id], visible[child], t); p.y -= Mathf.Sin(t * Mathf.PI) * .1f; line.SetPosition(j, p); }
            }
            for (; edge < routes.Length; edge++) routes[edge].gameObject.SetActive(false);
        }
        public void RefreshPlayback(string a, string b)
        {
            var lookup=tree.Nodes.ToDictionary(n=>n.Id);
            var addresses=new string[7];var locations=new Vector3[7];
            bool shared=b==null || a==b;
            if(shared){
                addresses[0]=a;
                for(int i=0;i<3;i++)if(addresses[i]!=null && lookup[addresses[i]] is SplitNode split){addresses[i*2+1]=split.TrueChild;addresses[i*2+2]=split.FalseChild;}
                for(int i=0;i<7;i++){locations[i]=positions[i];locations[i].y=i==0?1.3f:i<3?1.5f:1.85f;}
            } else {
                addresses[0]=a;addresses[3]=b;
                for(int side=0;side<2;side++){
                    int start=side*3;float x=side==0?-1.4f:1.4f;
                    locations[start]=new Vector3(x,1.3f,4.5f);
                    locations[start+1]=new Vector3(x-.75f,1.65f,6.1f);locations[start+2]=new Vector3(x+.75f,1.65f,6.1f);
                    if(lookup[addresses[start]] is SplitNode split){addresses[start+1]=split.TrueChild;addresses[start+2]=split.FalseChild;}
                }
            }
            var visible=new Dictionary<string,Vector3>();
            for(int i=0;i<7;i++){
                var slot=slots[i];bool active=addresses[i]!=null;
                slot.gameObject.SetActive(active);slot.title.transform.parent.gameObject.SetActive(active);
                if(!active)continue;
                slot.nodeId=addresses[i];slot.transform.localPosition=locations[i];slot.dropAnchor.localPosition=locations[i]+Vector3.up*.29f;
                slot.title.transform.parent.localPosition=locations[i]+new Vector3(0,.48f,-.16f);
                slot.title.transform.parent.localScale=Vector3.one*.0035f;
                slot.title.enabled=true;slot.title.enableAutoSizing=true;slot.title.fontSizeMin=16;slot.title.fontSizeMax=22;
                slot.title.rectTransform.sizeDelta=new Vector2(300,110);
                var node=lookup[addresses[i]];
                slot.title.text=node is SplitNode split?view.Condition(split,true):"LEAF "+(tree.Weight*((LeafNode)node).Score).ToString("+0.###;-0.###;0",System.Globalization.CultureInfo.InvariantCulture);
                slot.marker.enabled=false;
                slot.platform.sharedMaterial=addresses[i]==a || addresses[i]==b?view.activeMaterial:view.idleMaterial;
                visible[addresses[i]]=locations[i];
            }
            int edge=0;
            foreach(var item in visible)if(lookup[item.Key] is SplitNode split)
                foreach(var child in new[]{split.TrueChild,split.FalseChild})if(visible.TryGetValue(child,out var end) && edge<routes.Length){
                    var line=routes[edge++];line.gameObject.SetActive(true);line.positionCount=24;
                    for(int j=0;j<24;j++){float t=j/23f;var point=Vector3.Lerp(item.Value,end,t);point.y-=Mathf.Sin(t*Mathf.PI)*.1f;line.SetPosition(j,point);}
                }
            for(;edge<routes.Length;edge++)routes[edge].gameObject.SetActive(false);
        }
        public void Forest(EnsembleSession ensemble)
        {
            int start = (ensemble.Index / slots.Length) * slots.Length;
            int count = System.Math.Min(slots.Length, ensemble.Model.Trees.Count - start);
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i]; bool active = i < count;
                slot.gameObject.SetActive(active); slot.title.transform.parent.gameObject.SetActive(active);
                if (!active) continue;
                int index = start + i; var session = ensemble.Trees[index];
                var position = new Vector3((i % 3 - (System.Math.Min(3,count)-1)*.5f) * 2.3f, 2.05f + (i/3)*.65f, 6.5f + (i/3)*1.3f);
                slot.transform.localPosition = position;
                slot.title.transform.parent.localPosition = position + Vector3.up * .4f;
                slot.title.enableAutoSizing = false; slot.title.fontSize = 29;
                slot.title.rectTransform.sizeDelta = new Vector2(520,65);
                slot.marker.rectTransform.anchoredPosition = new Vector2(0,-37);
                slot.title.text = "TREE " + (index + 1) + "  |  " + session.Tree.Nodes.Count + " nodes";
                slot.marker.text = (index == ensemble.Index ? "SELECTED  |  " : "") +
                    (session.State.AtLeaf ? "Leaf " + session.State.Contribution.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) : "Depth " + session.State.Decisions.Count);
                slot.title.enabled = slot.marker.enabled = true;
                slot.platform.sharedMaterial = index == ensemble.Index ? view.activeMaterial : session.State.AtLeaf ? view.visitedMaterial : view.idleMaterial;
            }
            foreach (var route in routes) route.gameObject.SetActive(false);
        }
        private static bool IsDescendant(Dictionary<string, ModelNode> nodes, string root, string target)
        {
            if (!(nodes[root] is SplitNode split)) return false;
            // Called only for seven visible nodes, with at most two displayed generations.
            return split.TrueChild == target || split.FalseChild == target ||
                ChildContains(nodes, split.TrueChild, target) || ChildContains(nodes, split.FalseChild, target);
        }
        private static bool ChildContains(Dictionary<string, ModelNode> nodes, string id, string target) =>
            nodes[id] is SplitNode split && (split.TrueChild == target || split.FalseChild == target);
    }
}
