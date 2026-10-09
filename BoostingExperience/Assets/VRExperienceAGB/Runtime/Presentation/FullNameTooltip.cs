using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VRExperienceAGB.Presentation
{
    /// <summary>Read-only, scrollable source condition. Hovering never changes a route or score.</summary>
    public sealed class FullNameTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public TMP_Text Text { get; private set; }
        private GameObject panel;
        private ScrollRect scroll;
        private Canvas readingCanvas;
        private string shownText;
        private object source;
        private bool overPanel;
        private float hideAt = float.PositiveInfinity;
        public bool Visible => panel != null && panel.activeSelf;
        public void Build(Transform parent, TMP_Text style)
        {
            panel = new GameObject("FullPredictorName", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            panel.transform.SetParent(parent, false);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f,.5f);
            rect.anchoredPosition = new Vector2(0,570); rect.sizeDelta = new Vector2(1180,500);
            panel.GetComponent<Image>().color = new Color(.025f,.06f,.09f,1f);
            var forwarding = panel.AddComponent<TooltipPanelHover>(); forwarding.tooltip = this;
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(rect,false);
            var viewportRect=(RectTransform)viewport.transform;
            viewportRect.anchorMin=Vector2.zero; viewportRect.anchorMax=Vector2.one; viewportRect.offsetMin=new Vector2(24,24); viewportRect.offsetMax=new Vector2(-24,-24);
            Text = Instantiate(style,viewportRect); Text.name="FullNameAndCondition";
            Text.gameObject.SetActive(true); Text.raycastTarget=false; Text.enableAutoSizing=false; Text.fontSize=32; Text.color=new Color(.94f,.97f,1f);
            Text.textWrappingMode=TextWrappingModes.Normal; Text.alignment=TextAlignmentOptions.TopLeft;
            var content=Text.rectTransform; content.anchorMin=new Vector2(0,1); content.anchorMax=new Vector2(1,1); content.pivot=new Vector2(.5f,1);
            content.anchoredPosition3D=new Vector3(0,0,-2); content.sizeDelta=new Vector2(0,0);
            // Resolve content height once per source, avoiding a layout feedback loop at the clipping edge.
            scroll=panel.GetComponent<ScrollRect>(); scroll.viewport=viewportRect; scroll.content=content; scroll.scrollSensitivity=32; scroll.inertia=false; scroll.horizontal=false; scroll.vertical=true; scroll.movementType=ScrollRect.MovementType.Clamped;
            panel.SetActive(false);
        }
        public void UseCompactLayout()
        {
            var view=GetComponent<OneTreeExperience>();
            if(readingCanvas==null && view.Garden!=null) {
                readingCanvas=view.Garden.CanvasAt("NodeReadingPanel",Vector3.zero,new Vector2(1000,600),.0016f,transform);
                readingCanvas.overrideSorting=true;readingCanvas.sortingOrder=1000;
                panel.transform.SetParent(readingCanvas.transform,false);
            }
            var rect=(RectTransform)panel.transform;
            rect.anchoredPosition3D=Vector3.zero;rect.sizeDelta=new Vector2(1000,600);
            Text.fontSize=32;
        }
        public void Show(object owner, string text)
        {
            bool unchanged=Visible && shownText==text;
            source=owner; hideAt=float.PositiveInfinity;
            if(unchanged)return;
            overPanel=false;shownText=text;
            Text.richText=false;
            Text.text="NODE DETAILS · scroll for more\n"+text;
            if(readingCanvas!=null) {
                var head=GetComponent<OneTreeExperience>().Garden.Locomotion.Head;
                var forward=Vector3.ProjectOnPlane(head.forward,Vector3.up).normalized;
                if(forward.sqrMagnitude<.01f)forward=Vector3.forward;
                readingCanvas.transform.SetPositionAndRotation(head.position+forward*2.2f+Vector3.up*.1f,Quaternion.LookRotation(forward));
            }
            panel.SetActive(true);
            Canvas.ForceUpdateCanvases();
            Text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                Mathf.Max(scroll.viewport.rect.height,Text.GetPreferredValues(Text.text,scroll.viewport.rect.width,Mathf.Infinity).y+8));
            scroll.StopMovement();scroll.verticalNormalizedPosition=1;
        }
        public void Leave(object owner) { if (source == owner) hideAt=Time.unscaledTime+.2f; }
        public void Hide() { source=null; overPanel=false; hideAt=float.PositiveInfinity; if(panel!=null)panel.SetActive(false); }
        public void OnPointerEnter(PointerEventData data) { overPanel=true; }
        public void OnPointerExit(PointerEventData data) { overPanel=false; hideAt=Time.unscaledTime+.2f; }
        private void Update() { if(!overPanel && Time.unscaledTime>=hideAt) Hide(); }
        private void OnDestroy() { if(panel!=null) Destroy(panel); }
    }
    public sealed class TooltipPanelHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public FullNameTooltip tooltip;
        public void OnPointerEnter(PointerEventData data) => tooltip.OnPointerEnter(data);
        public void OnPointerExit(PointerEventData data) => tooltip.OnPointerExit(data);
    }
}
