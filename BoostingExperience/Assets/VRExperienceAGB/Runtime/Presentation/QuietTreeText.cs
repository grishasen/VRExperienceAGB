using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using VRExperienceAGB.Domain;
using VRExperienceAGB.Import;

namespace VRExperienceAGB.Presentation
{
    /// <summary>Compact display copy only. Exact identifiers and full predicates stay in model details.</summary>
    internal static class QuietTreeText
    {
        public static string Family(string id) => PredictorPalette.Family(id);
        public static Color SourceColor(string id) => PredictorPalette.ForFeature(id);
        public static bool IsScore(string id) => id.IndexOf("Score", StringComparison.OrdinalIgnoreCase)>=0;
        public static string Short(string text, int limit=30) => text.Length<=limit ? text : text.Substring(0,limit-1)+"…";
        public static string Predicate(SplitNode node, bool matched, int valueLimit=22)
        {
            var c=node.Condition;
            if(c.Operator==DecisionOperator.IsMissing)return matched?"Missing":"Present";
            if(c.Operator==DecisionOperator.LessThan)return (matched?"< ":"≥ ")+c.Threshold.Value.ToString("G17",CultureInfo.InvariantCulture);
            string values=string.Join(", ",c.Categories);
            return (matched?"IN ":"NOT IN ")+(c.Categories.Count<=3 && values.Length<=valueLimit?"{"+values+"}":c.Categories.Count+" values");
        }
        public static void StyleNode(OneTreeExperience view, TreeNodeView slot, ModelNode node, bool current)
        {
            TreeCardTypography.Apply(slot.title,current);
            slot.title.enabled=true;slot.title.fontSize=current?28:25;
            slot.title.alignment=TextAlignmentOptions.Center;
            slot.title.overflowMode=TextOverflowModes.Ellipsis;
            slot.title.rectTransform.sizeDelta=new Vector2(current?330:300,current?120:90);
            var canvas=slot.title.transform.parent;
            var position=canvas.localPosition;position.y=slot.transform.localPosition.y+(current?.72f:.48f);canvas.localPosition=position;
            var backing=canvas.Find("QuietLabelBacking");
            if(backing==null) {
                var go=new GameObject("QuietLabelBacking",typeof(RectTransform),typeof(UnityEngine.UI.Image));
                go.transform.SetParent(canvas,false);go.transform.SetAsFirstSibling();backing=go.transform;
                go.GetComponent<UnityEngine.UI.Image>().color=new Color(.018f,.046f,.063f,.94f);
            }
            // The source heading extends above the old 100-unit canvas. Include the whole label,
            // and let its backing receive the same hover/click events as the text.
            ((RectTransform)canvas).sizeDelta=current?new Vector2(360,152):new Vector2(320,112);
            backing.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            var hover=backing.GetComponent<NodeNameHover>()??backing.gameObject.AddComponent<NodeNameHover>();
            hover.experience=view;hover.node=slot;
            backing.gameObject.SetActive(node is SplitNode);
            ((RectTransform)backing).sizeDelta=current?new Vector2(360,140):new Vector2(320,76);
            slot.marker.enabled=false;
            if(node is SplitNode split) {
                slot.title.color=SourceColor(split.FeatureId);
                string label=Short(view.FeatureLabel(split.FeatureId),current?30:26);
                slot.title.text=current?Family(split.FeatureId)+"\n"+label+"\n"+Predicate(split,true):label;
            } else {
                slot.title.color=new Color(.93f,.96f,.98f);
                slot.title.text="Leaf "+(view.Session.Tree.Weight*((LeafNode)node).Score).ToString("+0.###;-0.###;0",CultureInfo.InvariantCulture);
            }
            TreeCardTypography.SetFont(slot.marker);slot.marker.fontSize=16;
        }
    }
}
