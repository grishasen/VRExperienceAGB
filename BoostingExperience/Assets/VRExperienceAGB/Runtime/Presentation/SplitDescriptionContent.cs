using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Presentation
{
    /// <summary>Small source-colored inspector for the Quiet tree layout.</summary>
    public sealed class SplitDescriptionContent : MonoBehaviour
    {
        private OneTreeExperience view;
        private FullNameTooltip tooltip;
        private TMP_Text heading, family, feature, identifier, predicate, context, pin, score, details, pageLabel;
        private GameObject detailsButton, closeButton, previousButton, nextButton;
        private ModelDefinition model;
        private int page = 1;
        private bool isSplit, hasScore;
        public bool DetailsVisible { get; private set; }
        public TMP_Text DetailsText => details;
        private int treeIndex;
        private string nodeId;
        public long Revision { get; private set; }
        public TMP_Text PredicateText => predicate;
        public string NodeId => nodeId;
        public static SplitDescriptionContent Create(FullNameTooltip owner, OneTreeExperience view, Transform parent)
        {
            var go=new GameObject("CompactSplit",typeof(RectTransform),typeof(SplitDescriptionContent));go.transform.SetParent(parent,false);
            var card=go.GetComponent<SplitDescriptionContent>();card.view=view;card.tooltip=owner;
            card.heading=card.Text("Heading",new Vector2(-70,169),new Vector2(380,36),24,true);
            card.family=card.Text("Source",new Vector2(-45,122),new Vector2(430,30),21);
            card.score=card.Text("ScoreTag",new Vector2(213,122),new Vector2(94,30),21);
            card.score.color=new Color32(236,168,201,255);card.score.text="Score";
            card.feature=card.Text("Feature",new Vector2(0,75),new Vector2(520,58),29,true);
            card.feature.overflowMode=TextOverflowModes.Ellipsis;
            card.identifier=card.Text("Identifier",new Vector2(0,30),new Vector2(520,32),20);
            card.identifier.color=new Color(.70f,.77f,.83f);card.identifier.overflowMode=TextOverflowModes.Ellipsis;
            card.predicate=card.Text("Predicate",new Vector2(0,-27),new Vector2(520,62),32,true);
            card.predicate.overflowMode=TextOverflowModes.Ellipsis;
            card.context=card.Text("Context",new Vector2(0,-91),new Vector2(520,48),21);
            card.context.overflowMode=TextOverflowModes.Ellipsis;
            card.pin=card.Button("Pin",new Vector2(176,169),new Vector2(88,40),true);
            card.closeButton=card.Button("×",new Vector2(250,169),new Vector2(40,40),false,true).transform.parent.gameObject;
            card.detailsButton=card.Button("Model details  →",new Vector2(0,-158),new Vector2(520,54),false).transform.parent.gameObject;
            card.details=card.Text("InlineSplitDetails",new Vector2(0,45),new Vector2(520,390),24);
            card.details.overflowMode=TextOverflowModes.Page;
            card.pageLabel=card.Text("DetailPage",new Vector2(0,-204),new Vector2(100,40),21);
            card.pageLabel.alignment=TextAlignmentOptions.Center;
            card.previousButton=card.Button("Previous",new Vector2(-174,-204),new Vector2(172,48),false,false,-1).transform.parent.gameObject;
            card.nextButton=card.Button("Next",new Vector2(174,-204),new Vector2(172,48),false,false,1).transform.parent.gameObject;
            return card;
        }
        private TMP_Text Text(string name,Vector2 position,Vector2 size,int font,bool bold=false)
        {
            var text=view.Garden.Text(transform,name,"",position,size,font);TreeCardTypography.Apply(text,bold);return text;
        }
        private TMP_Text Button(string label,Vector2 position,Vector2 size,bool isPin,bool close=false,int pageDelta=0)
        {
            var go=new GameObject(isPin?"PinSplit":close?"CloseSplit":pageDelta!=0?"DetailPage"+pageDelta:"SplitModelDetails",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button),typeof(SplitDescriptionButton));
            go.transform.SetParent(transform,false);var rect=(RectTransform)go.transform;rect.anchoredPosition=position;rect.sizeDelta=size;
            go.GetComponent<UnityEngine.UI.Image>().color=new Color(.10f,.17f,.23f);
            go.GetComponent<UnityEngine.UI.Button>().navigation=new UnityEngine.UI.Navigation{mode=UnityEngine.UI.Navigation.Mode.None};
            var target=go.GetComponent<SplitDescriptionButton>();target.Card=this;target.IsPin=isPin;target.Close=close;target.PageDelta=pageDelta;
            var text=view.Garden.Text(go.transform,"Label",label,Vector2.zero,size-new Vector2(12,8),22);
            TreeCardTypography.Apply(text);text.alignment=TextAlignmentOptions.Center;return text;
        }
        public void Show(string id)
        {
            Revision++;treeIndex=view.Ensemble.Index;nodeId=id;model=view.Model;
            var node=view.Model.Trees[treeIndex].Nodes.First(n=>n.Id==id);
            isSplit=node is SplitNode;hasScore=node is SplitNode scored && QuietTreeText.IsScore(scored.FeatureId);
            if(node is SplitNode split) {
                family.text=QuietTreeText.Family(split.FeatureId);family.color=QuietTreeText.SourceColor(split.FeatureId);
                feature.color=identifier.color=family.color;
                score.gameObject.SetActive(QuietTreeText.IsScore(split.FeatureId));
                feature.text=view.FeatureLabel(split.FeatureId);identifier.text=split.FeatureId;predicate.text=QuietTreeText.Predicate(split,true);
            } else {
                family.text="Leaf";family.color=Color.white;score.gameObject.SetActive(false);
                feature.color=identifier.color=new Color(.93f,.96f,.98f);
                feature.text="Contribution";identifier.text="Tree "+(treeIndex+1);
                predicate.text=(view.Session.Tree.Weight*((LeafNode)node).Score).ToString("+0.######;-0.######;0",System.Globalization.CultureInfo.InvariantCulture);
            }
            context.text=view.Ensemble.Profile==null?"Manual exploration":"Prepared profile";
            var decision=view.Session.CurrentProfileDecision;
            if(decision!=null && id==view.Session.State.NodeId)context.text+=" · "+(decision.Matched?"True":"False");
            SetDetails(false);RefreshPin();
        }
        private void SetDetails(bool expanded)
        {
            DetailsVisible=expanded;page=1;Revision++;
            tooltip.SetCompactSize(new Vector2(580,expanded?640:400));
            tooltip.KeepVisible();
            heading.text=isSplit?(expanded?"Split details":"Selected split"):(expanded?"Leaf details":"Selected leaf");
            heading.rectTransform.anchoredPosition=new Vector2(-70,expanded?288:169);
            ((RectTransform)pin.transform.parent).anchoredPosition=new Vector2(176,expanded?288:169);
            ((RectTransform)closeButton.transform).anchoredPosition=new Vector2(250,expanded?288:169);
            ((RectTransform)detailsButton.transform).anchoredPosition=new Vector2(0,expanded?-270:-158);
            detailsButton.GetComponentInChildren<TMP_Text>().text=expanded?"←  Summary":"Model details  →";
            foreach(var label in new[]{family,feature,identifier,predicate,context})label.gameObject.SetActive(!expanded);
            score.gameObject.SetActive(!expanded&&hasScore);
            details.gameObject.SetActive(expanded);pageLabel.gameObject.SetActive(expanded);
            previousButton.SetActive(expanded);nextButton.SetActive(expanded);
            if(expanded) { details.text=view.Garden.M6.Analysis.NodeDetail(treeIndex,nodeId,compactSource:true);TurnPage(0); }
        }
        public void RefreshPin() { pin.text=tooltip.Pinned?"Unpin":"Pin"; }
        public bool CanInteract => gameObject.activeInHierarchy && tooltip.Visible && view.Model==model && view.Director?.MenuVisible!=true &&
            !view.Session.State.Overview && view.Ensemble.Index==treeIndex;
        internal long SessionRevision => view.Ensemble.Revision;
        internal long NavigationRevision => view.Garden.Navigation?.Revision??0;
        public void Activate(bool isPin)
        {
            if(!CanInteract)return;
            if(isPin)tooltip.TogglePin();
            else SetDetails(!DetailsVisible);
        }
        internal void Close() { if(CanInteract)tooltip.Hide(); }
        internal void TurnPage(int delta)
        {
            if(!DetailsVisible)return;
            details.ForceMeshUpdate();
            int count=Mathf.Max(1,details.textInfo.pageCount);
            page=Mathf.Clamp(page+delta,1,count);details.pageToDisplay=page;
            pageLabel.text=page+" / "+count;
            previousButton.GetComponent<UnityEngine.UI.Button>().interactable=page>1;
            nextButton.GetComponent<UnityEngine.UI.Button>().interactable=page<count;
            Revision++;
        }
        private void OnDisable() { Revision++; }
    }
    public sealed class SplitDescriptionButton : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
    {
        public SplitDescriptionContent Card;public bool IsPin, Close;public int PageDelta;
        private long revision=-1, sessionRevision, navigationRevision;private int pointer;
        private bool Interactable => Card.CanInteract && GetComponent<UnityEngine.UI.Button>().IsInteractable();
        public void OnPointerDown(PointerEventData e) { if(Interactable && e.button==PointerEventData.InputButton.Left){revision=Card.Revision;sessionRevision=Card.SessionRevision;navigationRevision=Card.NavigationRevision;pointer=e.pointerId;} }
        public void OnPointerClick(PointerEventData e)
        {
            if(!Interactable || revision!=Card.Revision || sessionRevision!=Card.SessionRevision || navigationRevision!=Card.NavigationRevision || pointer!=e.pointerId || e.button!=PointerEventData.InputButton.Left)return;
            revision=-1;ControllerSelectionFeedback.Pulse(e.pointerId);
            if(Close)Card.Close();else if(PageDelta!=0)Card.TurnPage(PageDelta);else Card.Activate(IsPin);
        }
        private void OnDisable(){revision=-1;}
    }
}
