using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Presentation
{
    /// <summary>A collapsible, world-anchored source key. Expansion grows from the same left edge.</summary>
    public sealed class SourceLegend
    {
        private const int PageSize = 4;
        private readonly ForestGardenView garden;
        private readonly Canvas canvas;
        private readonly TMP_Text[] labels = new TMP_Text[PageSize], swatches = new TMP_Text[PageSize];
        private readonly TMP_Text counter;
        private readonly GameObject toggle, previous, next;
        private ModelDefinition model;
        private string[] families = Array.Empty<string>();
        private int page;
        private NavigationPage placedPage;
        public long Revision { get; private set; }
        public bool Expanded { get; private set; } = true;
        public bool Visible => canvas.gameObject.activeInHierarchy;
        public SourceLegend(ForestGardenView garden)
        {
            this.garden = garden;
            canvas = garden.CanvasAt("SourceLegend", Vector3.zero, new Vector2(180,64), .0018f, garden.Experience.transform);
            ((RectTransform)canvas.transform).pivot=new Vector2(0,.5f);
            var image = canvas.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(.018f,.046f,.063f,1); image.raycastTarget = false;
            toggle=Button("ToggleSources","›  Sources",0);
            for(int i=0;i<labels.Length;i++) {
                swatches[i]=Text("Swatch"+i,"•",36);
                labels[i]=Text("Source"+i,"",24);
                labels[i].alignment=TextAlignmentOptions.MidlineLeft;
                labels[i].textWrappingMode=TextWrappingModes.NoWrap;
                labels[i].overflowMode=TextOverflowModes.Ellipsis;
            }
            counter=Text("Page","",20);
            previous=Button("Previous","‹",-1);next=Button("Next","›",1);
            Render();canvas.gameObject.SetActive(false);
        }
        private TMP_Text Text(string name,string copy,int fontSize)
        {
            var text=garden.Text(canvas.transform,name,copy,Vector2.zero,new Vector2(100,44),fontSize);
            text.rectTransform.anchorMin=text.rectTransform.anchorMax=new Vector2(0,.5f);
            return text;
        }
        private GameObject Button(string name,string text,int delta)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button),typeof(SourceLegendTarget));
            go.transform.SetParent(canvas.transform,false);var rect=(RectTransform)go.transform;
            rect.anchorMin=rect.anchorMax=new Vector2(0,.5f);rect.sizeDelta=new Vector2(56,56);
            go.GetComponent<UnityEngine.UI.Image>().color=new Color(.10f,.17f,.23f);
            go.GetComponent<UnityEngine.UI.Button>().navigation=new UnityEngine.UI.Navigation{mode=UnityEngine.UI.Navigation.Mode.None};
            var target=go.GetComponent<SourceLegendTarget>();target.Legend=this;target.Delta=delta;
            garden.Text(go.transform,"Label",text,Vector2.zero,new Vector2(48,44),delta==0?24:32);
            return go;
        }
        public void Refresh()
        {
            var view=garden.Experience;
            if(model!=view.Model) {
                model=view.Model;page=0;
                families=model.Trees.SelectMany(t=>t.Nodes.OfType<SplitNode>()).Select(n=>PredictorPalette.Family(n.FeatureId))
                    .Distinct(StringComparer.Ordinal).OrderBy(f=>f,StringComparer.Ordinal).ToArray();
                Render();
            }
            bool show=view.Director!=null && !view.Director.MenuVisible && !garden.M5.HelpOpen &&
                !garden.M5.Extensions.PanelOpen && !garden.M5.Comparison.PanelOpen && families.Length>0;
            if(show && (!Visible || placedPage!=garden.Navigation.Page))Place();
            if(canvas.gameObject.activeSelf!=show){Revision++;canvas.gameObject.SetActive(show);}
        }
        public void Place()
        {
            var head=garden.Locomotion.Head;var forward=Vector3.ProjectOnPlane(head.forward,Vector3.up).normalized;
            if(forward.sqrMagnitude<.01f)forward=Vector3.forward;
            canvas.transform.SetPositionAndRotation(head.position+forward*2.2f-Vector3.Cross(Vector3.up,forward)*1.65f-Vector3.up*.82f,Quaternion.LookRotation(forward));
            placedPage=garden.Navigation.Page;Revision++;
        }
        public void Toggle()
        {
            if(!Visible)return;
            Expanded=!Expanded;Render();
        }
        public void TurnPage(int delta)
        {
            if(!Visible || !Expanded)return;
            page=Mathf.Clamp(page+delta,0,Math.Max(0,(families.Length-1)/PageSize));Render();
        }
        private void Render()
        {
            Revision++;
            var toggleRect=(RectTransform)toggle.transform;
            toggleRect.sizeDelta=new Vector2(Expanded?56:172,56);
            toggleRect.anchoredPosition=new Vector2(4+toggleRect.sizeDelta.x*.5f,0);
            var toggleLabel=toggle.GetComponentInChildren<TMP_Text>();
            toggleLabel.text=Expanded?"‹":"›  Sources";toggleLabel.rectTransform.sizeDelta=toggleRect.sizeDelta-new Vector2(8,8);
            float x=76;
            for(int i=0;i<labels.Length;i++) {
                int index=page*PageSize+i;bool show=Expanded && index<families.Length;
                labels[i].gameObject.SetActive(show);swatches[i].gameObject.SetActive(show);
                if(!show)continue;
                labels[i].text=families[index];labels[i].color=swatches[i].color=PredictorPalette.ForFamily(families[index]);
                float width=Mathf.Clamp(labels[i].GetPreferredValues(families[index]).x,28,240);
                swatches[i].rectTransform.sizeDelta=new Vector2(24,44);swatches[i].rectTransform.anchoredPosition=new Vector2(x+12,0);
                labels[i].rectTransform.sizeDelta=new Vector2(width+2,44);labels[i].rectTransform.anchoredPosition=new Vector2(x+34+width*.5f,0);
                x+=width+60;
            }
            int count=Math.Max(1,(families.Length+PageSize-1)/PageSize);bool pages=Expanded && count>1;
            previous.SetActive(pages);next.SetActive(pages);counter.gameObject.SetActive(pages);
            previous.GetComponent<UnityEngine.UI.Button>().interactable=page>0;
            next.GetComponent<UnityEngine.UI.Button>().interactable=page+1<count;
            if(pages) {
                ((RectTransform)previous.transform).anchoredPosition=new Vector2(x+28,0);
                counter.rectTransform.sizeDelta=new Vector2(80,44);counter.rectTransform.anchoredPosition=new Vector2(x+100,0);
                ((RectTransform)next.transform).anchoredPosition=new Vector2(x+172,0);
                counter.text=(page+1)+" / "+count;x+=208;
            }
            ((RectTransform)canvas.transform).sizeDelta=new Vector2(Expanded?x:180,64);
        }
    }
    public sealed class SourceLegendTarget : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
    {
        [NonSerialized] public SourceLegend Legend;
        public int Delta;
        private long revision=-1;private int pointer;
        private bool Interactable => Legend!=null && Legend.Visible && GetComponent<UnityEngine.UI.Button>().IsInteractable();
        public void OnPointerDown(PointerEventData e)
        {
            if(e.button!=PointerEventData.InputButton.Left || !Interactable)return;
            revision=Legend.Revision;pointer=e.pointerId;
        }
        public void OnPointerClick(PointerEventData e)
        {
            if(!Interactable || e.button!=PointerEventData.InputButton.Left || pointer!=e.pointerId || revision!=Legend.Revision)return;
            revision=-1;ControllerSelectionFeedback.Pulse(e.pointerId);
            if(Delta==0)Legend.Toggle();else Legend.TurnPage(Delta);
        }
        private void OnDisable(){revision=-1;}
    }
}
