using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Presentation
{
    /// <summary>A second presentation of the complete forest, sharing the accepted ensemble session.</summary>
    public sealed class ForestDioramaView
    {
        public const float MinimumScale = .7f, MaximumScale = 1.4f;
        private readonly M5ForestPresentation owner;
        private readonly Transform root, model;
        private readonly Canvas controls, hoverCard;
        public TMP_Text HoverText { get; private set; }
        public int HoveredIndex { get; private set; } = -1;
        private readonly TMP_Text details;
        private readonly GameObject previousPage, nextPage;
        private readonly List<Renderer> ringsB = new List<Renderer>();
        private readonly List<Renderer> rings = new List<Renderer>();
        private readonly List<UnityEngine.UI.Image> targets = new List<UnityEngine.UI.Image>();
        private readonly List<Transform> crownTargets = new List<Transform>();
        private ModelDefinition builtModel;
        private int builtPage = -1;
        private bool placed;
        public const int TreesPerPage = 50;
        public float Scale { get; private set; } = 1;
        public float Yaw { get; private set; }
        public Transform ModelRoot => model;
        public bool Visible => root.gameObject.activeSelf;

        public ForestDioramaView(M5ForestPresentation owner, Transform parent)
        {
            this.owner = owner;
            root = new GameObject("ForestDiorama").transform; root.SetParent(parent, false);
            model = new GameObject("DioramaModel").transform; model.SetParent(root, false);
            model.localPosition = new Vector3(0, 0, 0);
            controls = owner.Panel("DioramaControls", new Vector2(1000, 500), root);
            controls.transform.localScale = Vector3.one * .0007f;
            details = owner.Garden.Text(controls.transform, "SelectedTree", "", new Vector2(0, 135), new Vector2(940, 110), 25);
            owner.Button(controls, "Previous", new Vector2(-360, 42), new Vector2(200, 55), M5Action.PreviousTree);
            owner.Button(controls, "Enter selected tree", new Vector2(0, 42), new Vector2(440, 55), M5Action.EnterTree);
            owner.Button(controls, "Next", new Vector2(360, 42), new Vector2(200, 55), M5Action.NextTree);
            owner.Button(controls, "Smaller", new Vector2(-390, -28), new Vector2(180, 55), M5Action.Smaller);
            owner.Button(controls, "Larger", new Vector2(-190, -28), new Vector2(180, 55), M5Action.Larger);
            owner.Button(controls, "Rotate left", new Vector2(35, -28), new Vector2(230, 55), M5Action.RotateLeft);
            owner.Button(controls, "Rotate right", new Vector2(290, -28), new Vector2(230, 55), M5Action.RotateRight);
            owner.Button(controls, "Reset view", new Vector2(-320, -100), new Vector2(270, 55), M5Action.ResetView);
            owner.Button(controls, "Back to garden", new Vector2(0, -100), new Vector2(320, 55), M5Action.Forest);
            owner.Button(controls, "Help / legend", new Vector2(340, -100), new Vector2(270, 55), M5Action.Help);
            previousPage = owner.Button(controls, "Previous 50 trees", new Vector2(-245, -169), new Vector2(450, 55), M5Action.PreviousTablePage);
            nextPage = owner.Button(controls, "Next 50 trees", new Vector2(245, -169), new Vector2(450, 55), M5Action.NextTablePage);
            owner.Garden.Text(controls.transform, "Legend", "Height = depth · Width = leaf count · Ring = reached contribution", new Vector2(0, -224), new Vector2(960, 40), 21);
            hoverCard=owner.Panel("TableTreeInformation",new Vector2(800,480),root);
            hoverCard.transform.localScale=Vector3.one*.00085f;
            HoverText=owner.Garden.Text(hoverCard.transform,"Information","",Vector2.zero,new Vector2(750,430),28);
            foreach(var graphic in hoverCard.GetComponentsInChildren<UnityEngine.UI.Graphic>())graphic.raycastTarget=false;
            hoverCard.gameObject.SetActive(false);
            root.gameObject.SetActive(false);
        }
        public void SetVisible(bool visible)
        {
            if (visible && !placed) Place();
            if(!visible)ClearHover(HoveredIndex);
            root.gameObject.SetActive(visible);
            if (visible) Refresh();
        }
        public void Place()
        {
            placed = true;
            var head = owner.Garden.Locomotion.Head;
            var forward = head.forward; forward.y = 0;
            if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
            forward.Normalize(); root.rotation = Quaternion.LookRotation(forward);
            // The table follows a deliberate opening/reset action, never the viewer's gaze each frame.
            root.position = head.position + forward * 1.6f - Vector3.up * .60f;
            controls.transform.localPosition = new Vector3(1.15f, .4f, .12f);
            hoverCard.transform.localPosition=new Vector3(-1.12f,.35f,.05f);
        }
        public void ChangeScale(float amount) { Scale = Mathf.Clamp(Scale + amount, MinimumScale, MaximumScale); ApplyTransform(); }
        public void Rotate(float degrees) { Yaw = Mathf.Repeat(Yaw + degrees, 360); ApplyTransform(); }
        public void Reset() { Scale = 1; Yaw = 0; ApplyTransform(); Place(); }
        private void ApplyTransform() { ClearHover(HoveredIndex); model.localScale = Vector3.one * Scale; model.localRotation = Quaternion.Euler(0, Yaw, 0); }
        public void Refresh()
        {
            if (builtModel != owner.View.Model || builtPage != 0) Build();
            int selected = owner.View.Ensemble.Index;
            var metric = owner.Garden.Metrics[selected];
            var ensemble = owner.View.Ensemble;
            previousPage.SetActive(false); nextPage.SetActive(false);
            controls.gameObject.SetActive(owner.View.Director == null);
            previousPage.GetComponent<UnityEngine.UI.Button>().interactable = builtPage > 0;
            nextPage.GetComponent<UnityEngine.UI.Button>().interactable = (builtPage + 1) * TreesPerPage < builtModel.Trees.Count;
            string identity = ensemble.Profile == null ? "Manual routes · no profile probability" : "Prepared profile: " + ensemble.Profile.DisplayName;
            details.text = "TREE " + (selected + 1) + " / " + builtModel.Trees.Count + " · Depth " + metric.MaximumDepth + " · " + metric.LeafCount + " leaves\n" +
                identity + "\n" + ensemble.CompletedCount + " leaves reached · Scale " + Scale.ToString("0.0", CultureInfo.InvariantCulture) + "×" +
                " · Showing all " + builtModel.Trees.Count + " trees";
            var ledger = ensemble.Ledger;
            for (int i = 0; i < rings.Count; i++)
            {
                int index = builtPage * TreesPerPage + i;
                bool reached = ledger[index].Progress != VRExperienceAGB.Application.ContributionProgress.Pending;
                double contribution=ledger[index].PresentedContribution;
                if(owner.View.Director != null && owner.View.Director.TryContribution(index,false,out var finalValue)){reached=true;contribution=finalValue;}
                rings[i].sharedMaterial = owner.Garden.ContributionRing(reached, contribution);
                bool hasB=owner.View.Director!=null && owner.View.Director.TryContribution(index,true,out _);
                ringsB[i].gameObject.SetActive(hasB);
                if(hasB){owner.View.Director.TryContribution(index,true,out var bv);ringsB[i].sharedMaterial=owner.Garden.ContributionRing(true,bv);}
                targets[i].color = new Color(.02f,.07f,.1f,.025f);
                targets[i].GetComponentInChildren<TMP_Text>().color=index==selected?new Color(1,.8f,.25f):Color.white;
                string sign = reached ? ledger[index].PresentedContribution > 0 ? "+" : ledger[index].PresentedContribution < 0 ? "−" : "0" : "";
                targets[i].GetComponentInChildren<TMP_Text>().text = (index + 1) + sign;
                // Pointer cards face the table's observer even after model rotation.
                crownTargets[i].rotation = root.rotation;
            }
        }
        private void Build()
        {
            foreach (Transform child in model) { child.gameObject.SetActive(false); Object.Destroy(child.gameObject); }
            ClearHover(HoveredIndex);
            bool replacingModel = builtModel != owner.View.Model;
            rings.Clear(); ringsB.Clear(); targets.Clear(); crownTargets.Clear(); builtModel = owner.View.Model;
            builtPage = 0;
            if (replacingModel) { Scale = 1; Yaw = 0; ApplyTransform(); }
            var garden = owner.Garden;
            int start = 0, count = builtModel.Trees.Count;
            int columns = Mathf.Min(10, count), rows = (count + columns - 1) / columns;
            float spacing = Mathf.Min(.15f, 1.5f / Mathf.Max(columns, rows));
            float width = Mathf.Max(.5f, columns * spacing + .15f), length = Mathf.Max(.35f, rows * spacing + .15f);
            Mesh("Table", model, new Vector3(0, -.035f, .20f), new Vector3(width, .055f, length), garden.planterMesh, garden.stoneMaterial);
            int minDepth=garden.Metrics.Min(m=>m.MaximumDepth),maxDepth=garden.Metrics.Max(m=>m.MaximumDepth);
            int minLeaves=garden.Metrics.Min(m=>m.LeafCount),maxLeaves=garden.Metrics.Max(m=>m.LeafCount);
            for (int i = 0; i < count; i++)
            {
                int index = start + i;
                var metric = garden.Metrics[index]; var plot = new GameObject("DioramaTree-" + (index + 1)).transform;
                plot.SetParent(model, false);
                plot.localPosition = new Vector3((i % columns - (columns - 1) * .5f) * spacing, i / columns * .045f, .20f + (i / columns - (rows - 1) * .5f) * spacing);
                float depth=maxDepth==minDepth?.5f:(float)(metric.MaximumDepth-minDepth)/(maxDepth-minDepth);
                float leaves=maxLeaves==minLeaves?.5f:Mathf.Sqrt((float)(metric.LeafCount-minLeaves)/(maxLeaves-minLeaves));
                float height=spacing*(.65f+1.25f*depth),diameter=spacing*(.32f+.42f*leaves);
                // Stepped supports keep rear labels above the foreground rows instead of hiding them.
                float support = .02f + i / columns * .045f;
                Mesh("Planter", plot, Vector3.down * (support - .02f), new Vector3(spacing * .8f, support, spacing * .8f), garden.planterMesh, garden.stoneMaterial);
                Mesh("Pine", plot, new Vector3(0, .02f, 0), new Vector3(diameter, height, diameter), garden.pineMesh, garden.pineMaterial);
                garden.M6.AddComposition(plot,index,new Vector3(0,.012f,-spacing*.42f),spacing*.0012f);
                rings.Add(Mesh("Ring", plot, new Vector3(0, .024f, 0), new Vector3(spacing * .8f, .009f, spacing * .8f), garden.ringMesh, garden.completedMaterial));
                ringsB.Add(Mesh("RingB", plot, new Vector3(0, .034f, 0), new Vector3(spacing * .6f, .009f, spacing * .6f), garden.ringMesh, garden.completedMaterial));
                float pointerScale=spacing*.009f, pointerHeight=height/pointerScale;
                var canvas = garden.CanvasAt("TreeTarget-" + (index + 1), new Vector3(0, .02f + height*.5f, 0), new Vector2(100,pointerHeight+52), pointerScale, plot);
                var button = owner.Button(canvas, (index + 1).ToString(), new Vector2(0,pointerHeight*.5f+32), new Vector2(100,52), M5Action.SelectTree, index, 30);
                // Keep the selectable number above the crown; a separate child catches pointing at the foliage.
                var hit=new GameObject("CrownHitArea",typeof(RectTransform),typeof(UnityEngine.UI.Image));hit.transform.SetParent(button.transform,false);
                var hitRect=(RectTransform)hit.transform;hitRect.sizeDelta=new Vector2(100,pointerHeight);hitRect.anchoredPosition=new Vector2(0,-pointerHeight*.5f-32);
                hit.GetComponent<UnityEngine.UI.Image>().color=new Color(.02f,.07f,.1f,.025f);
                var number=button.GetComponentInChildren<TMP_Text>();number.rectTransform.sizeDelta=new Vector2(100,52);
                targets.Add(button.GetComponent<UnityEngine.UI.Image>()); crownTargets.Add(canvas.transform);
            }
        }
        public void Hover(int index)
        {
            if(!Visible || owner.View.Director?.MenuVisible==true || index<0 || index>=owner.Garden.PlotCount)return;
            HoveredIndex=index;
            HoverText.text=owner.Garden.M6.Analysis.Passport(index)+"\nHeight = depth · Width = leaf count";
            if(owner.View.Director!=null && owner.View.Director.TryContribution(index,false,out var a))
                HoverText.text+="\nA contribution: "+a.ToString("+0.######;-0.######;0",CultureInfo.InvariantCulture);
            if(owner.View.Director!=null && owner.View.Director.TryContribution(index,true,out var b))
                HoverText.text+="\nB contribution: "+b.ToString("+0.######;-0.######;0",CultureInfo.InvariantCulture);
            hoverCard.transform.rotation=Quaternion.LookRotation(hoverCard.transform.position-owner.Garden.Locomotion.Head.position);
            hoverCard.gameObject.SetActive(true);
        }
        public void ClearHover(int index)
        { if(index!=HoveredIndex)return;HoveredIndex=-1;if(hoverCard!=null)hoverCard.gameObject.SetActive(false); }
        private static Renderer Mesh(string name, Transform parent, Vector3 position, Vector3 size, Mesh mesh, Material material)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(parent, false);
            var bounds = mesh.bounds;
            go.transform.localScale = new Vector3(size.x / bounds.size.x, size.y / bounds.size.y, size.z / bounds.size.z);
            go.transform.localPosition = position - Vector3.Scale(bounds.center, go.transform.localScale) + Vector3.up * size.y * .5f;
            go.GetComponent<MeshFilter>().sharedMesh = mesh; var renderer = go.GetComponent<Renderer>(); renderer.sharedMaterial = material; return renderer;
        }
    }
}
