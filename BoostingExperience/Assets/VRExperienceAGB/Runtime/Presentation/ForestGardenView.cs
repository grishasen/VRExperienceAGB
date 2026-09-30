using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using VRExperienceAGB.Application;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Presentation
{
    /// <summary>A separate, ordered garden. All trees stay in the model; only distant plaques are hidden.</summary>
    public sealed class ForestGardenView : MonoBehaviour
    {
        public bool simplifiedNavigation;
        public SimpleExperienceNavigation Navigation { get; private set; }
        public Mesh pineMesh, trunkMesh, planterMesh, ringMesh;
        public Material pineMaterial, barkMaterial, stoneMaterial, soilMaterial, pathMaterial, selectedMaterial, completedMaterial;
        public OneTreeExperience Experience { get; private set; }
        public GardenLocomotion Locomotion { get; private set; }
        public bool Visible => root != null && root.activeSelf;
        public int PlotCount => plots.Count;
        public IReadOnlyList<GardenTreeMetrics> Metrics => metrics;
        public TMP_Text Details { get; private set; }
        public GameObject EnterButton { get; private set; }
        public GameObject PreviousButton { get; private set; }
        public GameObject NextButton { get; private set; }
        private sealed class Plot
        {
            public Transform root;
            public Canvas plaque;
            public TMP_Text label, hoverLabel;
            public Renderer ring;
            public Vector3 ringScale;
        }
        private readonly List<Plot> plots = new List<Plot>();
        private readonly List<Canvas> waypoints = new List<Canvas>();
        private readonly List<GameObject> numberButtons = new List<GameObject>();
        private readonly Dictionary<GameObject,bool> walkthroughObjects = new Dictionary<GameObject,bool>();
        private GardenTreeMetrics[] metrics;
        private ModelDefinition builtModel;
        private GameObject root;
        private Canvas guide, guideHandle;
        public bool GuideOpen => guide != null && guide.gameObject.activeSelf;
        private bool guideButtonReleased = true;
        private Material gardenFoliage;
        private TMP_Text header;
        private GameObject profileButton;
        private Material quietRing;
        private int hovered = -1;
        private bool hoverDirty;
        private float nextPlaqueRefresh;
        private int shownBed = -1;
        private const int TreesPerBed = 10;
        public void Configure(OneTreeExperience experience)
        {
            if (Experience != null) return;
            Experience=experience;
            if (pineMesh == null || trunkMesh == null || planterMesh == null || ringMesh == null) throw new InvalidOperationException("Garden artwork is not configured.");
            Locomotion=gameObject.AddComponent<GardenLocomotion>(); Locomotion.Configure(experience);
            walkthroughObjects.Add(experience.presentationRoot.gameObject,experience.presentationRoot.gameObject.activeSelf);
            var environment=experience.transform.Find("MoonlitEnvironment");
            if(environment!=null)walkthroughObjects.Add(environment.gameObject,environment.gameObject.activeSelf);
            gardenFoliage=new Material(pineMaterial) { name="GardenEvergreen", enableInstancing=true };
            gardenFoliage.color=new Color(.42f,.8f,.5f);
            gardenFoliage.EnableKeyword("_EMISSION");gardenFoliage.SetColor("_EmissionColor",new Color(.01f,.045f,.016f));
            quietRing=new Material(completedMaterial) { name="UnvisitedPlanterRing" };
            quietRing.color=new Color(.10f,.25f,.27f); quietRing.enableInstancing=true;
        }
        public void Sync(bool garden)
        {
            if (Experience == null) return;
            if (builtModel != Experience.Model) Build();
            if (simplifiedNavigation) garden = Navigation.Page != NavigationPage.Tree;
            if (garden != Visible)
            {
                if (garden) { root.SetActive(true); Locomotion.SetGarden(true); PlaceGuide(); }
                else { root.SetActive(false); hovered=-1; Locomotion.SetGarden(false); }
            }
            foreach(var entry in walkthroughObjects) entry.Key.SetActive(!garden && entry.Value);
            if(garden)RefreshState();
            if(simplifiedNavigation) Navigation.Refresh();
        }
        public Vector3 PlotPosition(int index)
        {
            if(metrics.Length<=5)return new Vector3((index-(metrics.Length-1)*.5f)*3.4f,0,6.6f);
            int within=index%TreesPerBed, column=within%5;
            float x=(within<5?-1:1)*(2.9f+column*3.1f);
            return new Vector3(x,0,6.6f+(index/TreesPerBed)*4.8f);
        }
        public Vector3 StandingPoint(int index) => PlotPosition(index) + new Vector3(0,0,-2.1f);
        private void Build()
        {
            if(root!=null){root.SetActive(false);Destroy(root);}
            plots.Clear();waypoints.Clear();numberButtons.Clear();hovered=-1;shownBed=-1;
            builtModel=Experience.Model;metrics=builtModel.Trees.Select(t=>new GardenTreeMetrics(t)).ToArray();
            root=new GameObject("ModelPineGarden");root.transform.SetParent(Experience.transform,false);root.SetActive(false);
            int rows=(metrics.Length+TreesPerBed-1)/TreesPerBed;
            float length=rows*4.8f+12;
            Block("GardenGround",new Vector3(0,-.13f,length*.5f-3),new Vector3(42,.22f,length+10),soilMaterial);
            Block("CentralWalkway",new Vector3(0,-.005f,length*.5f-3),new Vector3(2.2f,.025f,length),pathMaterial);
            var oldEnvironment=Experience.transform.Find("MoonlitEnvironment");
            if(oldEnvironment!=null)
            foreach(Transform decoration in oldEnvironment)
            {
                if(decoration.name!="PineTree" && decoration.name!="Moon")continue;
                var copy=Instantiate(decoration.gameObject,root.transform);copy.name="GardenBackdrop-"+decoration.name;
                if(decoration.name=="PineTree")
                {
                    var p=decoration.localPosition;
                    p.x = (p.x<0?-1:1)*(23+Mathf.Abs(p.x)*.4f);p.z=p.z*1.7f;
                    copy.transform.localPosition=p;
                }
            }
            for(int row=0;row<rows;row++)
            {
                float z=6.6f+row*4.8f-2.1f;
                Block("BedPath-"+(row+1),new Vector3(0,.01f,z),new Vector3(34,.025f,1.25f),pathMaterial);
                var waypoint=CanvasAt("StandingPoint-"+(row+1),new Vector3(0,.04f,z),new Vector2(440,440),.0025f,root.transform);
                waypoint.transform.localRotation=Quaternion.Euler(90,0,0);
                var target=Button(waypoint,"GO TO GROUP "+(row+1),Vector2.zero,new Vector2(430,430),GardenCommand.Waypoint,row,40);
                target.GetComponent<UnityEngine.UI.Image>().color=new Color(.08f,.35f,.38f,.75f);waypoints.Add(waypoint);
            }
            if(simplifiedNavigation)
            for(int i=0;i<metrics.Length;i++)
            {
                var waypoint=CanvasAt("PlanterPath-"+(i+1),StandingPoint(i)+Vector3.up*.05f,new Vector2(440,440),.0025f,root.transform);
                waypoint.transform.localRotation=Quaternion.Euler(90,0,0);
                Button(waypoint,"GO HERE",Vector2.zero,new Vector2(430,430),GardenCommand.Path,i,42);
                waypoints.Add(waypoint);
            }
            for(int i=0;i<metrics.Length;i++)BuildPlot(i);
            if(simplifiedNavigation) Navigation = new SimpleExperienceNavigation(this, root.transform);
            else { Navigation = null; BuildGuide(); }
        }
        private void BuildPlot(int index)
        {
            var tree=metrics[index];var container=new GameObject("ModelTree-"+(index+1)).transform;
            container.SetParent(root.transform,false);container.localPosition=PlotPosition(index);
            MeshObject("Planter",container,Vector3.zero,new Vector3(2.1f,.36f,2.1f),planterMesh,stoneMaterial,true);
            MeshObject("Soil",container,new Vector3(0,.37f,0),new Vector3(1.84f,.06f,1.84f),planterMesh,soilMaterial,true);
            MeshObject("Trunk",container,new Vector3(0,.4f,0),new Vector3(.12f+tree.CrownRadius*.1f,tree.PineHeight,.12f+tree.CrownRadius*.1f),trunkMesh,barkMaterial,true);
            MeshObject("Pine",container,new Vector3(0,.4f,0),new Vector3(tree.CrownRadius*2,tree.PineHeight,tree.CrownRadius*2),pineMesh,gardenFoliage,true);
            var ring=MeshObject("ProgressRing",container,new Vector3(0,.40f,0),new Vector3(1.04f/.555f,1,1.04f/.555f),ringMesh,quietRing,false).GetComponent<Renderer>();
            var plaque=CanvasAt("TreePlaque-"+(index+1),PlotPosition(index)+new Vector3(0,.20f,-1.15f),new Vector2(640,1500),.0024f,root.transform);
            var pineTarget=Button(plaque,"",new Vector2(0,(.4f+tree.PineHeight*.5f-.20f)/.0024f),new Vector2(tree.CrownRadius*2/.0024f,tree.PineHeight/.0024f),GardenCommand.SelectTree,index,29);
            pineTarget.name="PinePointerTarget-"+(index+1);pineTarget.GetComponent<UnityEngine.UI.Image>().color=Color.clear;
            var button=Button(plaque,"",Vector2.zero,new Vector2(360,62),GardenCommand.SelectTree,index,25);
            button.name="PlanterName-"+(index+1);button.GetComponent<UnityEngine.UI.Image>().color=Color.clear;
            var hoverLabel=Text(plaque.transform,"HoverDetails","",new Vector2(0,420),new Vector2(400,135),21);
            var backing=new GameObject("HoverBacking",typeof(RectTransform),typeof(UnityEngine.UI.Image));
            backing.transform.SetParent(plaque.transform,false);
            backing.transform.localPosition=new Vector3(520,420,400);
            hoverLabel.transform.SetParent(backing.transform,false);hoverLabel.rectTransform.anchoredPosition=Vector2.zero;
            ((RectTransform)backing.transform).sizeDelta=new Vector2(420,145);
            backing.GetComponent<UnityEngine.UI.Image>().color=new Color(.025f,.065f,.085f,.96f);
            backing.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
            plots.Add(new Plot{root=container,plaque=plaque,label=button.GetComponentInChildren<TMP_Text>(),hoverLabel=hoverLabel,ring=ring,ringScale=ring.transform.localScale});
        }
        private void BuildGuide()
        {
            guide=CanvasAt("GardenFieldGuide",new Vector3(0,.84f,3.5f),new Vector2(1400,510),.0008f,root.transform);
            var background=guide.gameObject.AddComponent<UnityEngine.UI.Image>();background.color=new Color(.025f,.065f,.085f,.96f);background.raycastTarget=false;
            header=Text(guide.transform,"GardenTitle","",new Vector2(-100,208),new Vector2(1100,65),30);
            Button(guide,"Hide guide",new Vector2(574,215),new Vector2(210,55),GardenCommand.FieldGuide,0,24);
            guideHandle=CanvasAt("FieldGuideHandle",Vector3.zero,new Vector2(520,80),.0008f,root.transform);
            Button(guideHandle,"Field guide  ·  A / Tab",Vector2.zero,new Vector2(520,80),GardenCommand.FieldGuide,0,27);
            guideHandle.gameObject.SetActive(false);
            Details=Text(guide.transform,"SelectedTree","",new Vector2(0,132),new Vector2(1340,86),27);
            EnterButton=Button(guide,"Enter / resume tree",new Vector2(-475,42),new Vector2(330,60),GardenCommand.Enter,0,27);
            Button(guide,"Go to planter",new Vector2(-140,42),new Vector2(280,60),GardenCommand.Visit,0,26);
            profileButton=Button(guide,"Follow profile",new Vector2(170,42),new Vector2(280,60),GardenCommand.Profile,0,26);
            Button(guide,"Explore branches",new Vector2(480,42),new Vector2(280,60),GardenCommand.Manual,0,26);
            PreviousButton=Button(guide,"Previous tree",new Vector2(-560,-34),new Vector2(220,55),GardenCommand.PreviousTree,0,24);
            NextButton=Button(guide,"Next tree",new Vector2(-325,-34),new Vector2(220,55),GardenCommand.NextTree,0,24);
            Button(guide,"Previous bed",new Vector2(-90,-34),new Vector2(220,55),GardenCommand.PreviousBed,0,24);
            Button(guide,"Next bed",new Vector2(145,-34),new Vector2(220,55),GardenCommand.NextBed,0,24);
            Button(guide,"Entrance",new Vector2(350,-34),new Vector2(160,55),GardenCommand.Entrance,0,24);
            Button(guide,"Turn left",new Vector2(520,-34),new Vector2(150,55),GardenCommand.TurnLeft,0,23);
            Button(guide,"Turn right",new Vector2(520,-105),new Vector2(150,55),GardenCommand.TurnRight,0,23);
            for(int i=0;i<10;i++)numberButtons.Add(Button(guide,(i+1).ToString(),new Vector2(-608+i*108,-108),new Vector2(96,55),GardenCommand.SelectTree,i,25));
            Text(guide.transform,"Legend","Height = maximum depth  |  Crown width = leaf count\nAmber ring: selected  |  Bright cyan: leaf reached  |  Point at a plaque to inspect",new Vector2(0,-198),new Vector2(1320,85),23);
        }
        public GameObject ButtonFor(TreeAction action)
        {
            GardenCommand command;
            switch(action)
            {
                case TreeAction.Overview: command=GardenCommand.Enter;break;
                case TreeAction.PreviousTree: command=GardenCommand.PreviousTree;break;
                case TreeAction.NextTree: command=GardenCommand.NextTree;break;
                case TreeAction.Manual: command=GardenCommand.Manual;break;
                case TreeAction.Profile: command=GardenCommand.Profile;break;
                default:return null;
            }
            return root.GetComponentsInChildren<GardenPointerTarget>(true).First(t=>t.command==command).gameObject;
        }
        public void EnterSelected()
        {
            if(!Visible)return;
            if(simplifiedNavigation) { Navigation.OpenTree(Experience.Ensemble.Index); return; }
            var state=Experience.Session.State;Experience.Execute(TreeAction.Overview,state.Revision,state.NodeId);
        }
        public void Activate(GardenCommand command,int index=0)
        {
            if(!Visible)return;
            if(simplifiedNavigation && Navigation.Activate(command,index)) return;
            int selected=Experience.Ensemble.Index;
            hovered=-1;
            switch(command)
            {
                case GardenCommand.FieldGuide: ToggleGuide();break;
                case GardenCommand.SelectTree: Experience.SelectGardenTree(index);break;
                case GardenCommand.Enter: EnterSelected();return;
                case GardenCommand.PreviousTree: Experience.SelectGardenTree(Mathf.Max(0,selected-1));break;
                case GardenCommand.NextTree: Experience.SelectGardenTree(Mathf.Min(PlotCount-1,selected+1));break;
                case GardenCommand.PreviousBed: GoToBed(Mathf.Max(0,selected/TreesPerBed-1));break;
                case GardenCommand.NextBed: GoToBed(Mathf.Min((PlotCount-1)/TreesPerBed,selected/TreesPerBed+1));break;
                case GardenCommand.Waypoint: GoToBed(index);break;
                case GardenCommand.Visit: Locomotion.Teleport(StandingPoint(selected));PlaceGuide();break;
                case GardenCommand.Entrance: Locomotion.ReturnToEntrance();PlaceGuide();break;
                case GardenCommand.TurnLeft: Locomotion.SnapTurn(-30);PlaceGuide();break;
                case GardenCommand.TurnRight: Locomotion.SnapTurn(30);PlaceGuide();break;
                case GardenCommand.Profile: case GardenCommand.Manual:
                    var state=Experience.Session.State;Experience.Execute(command==GardenCommand.Profile?TreeAction.Profile:TreeAction.Manual,state.Revision,state.NodeId);return;
            }
            RefreshState();
        }
        public void ToggleGuide()
        {
            if(simplifiedNavigation && Navigation.Page==NavigationPage.Tree) { Navigation.ToggleTreeMenu(); return; }
            if(!Visible)return;
            if(simplifiedNavigation) { Navigation.ShowHome(); return; }
            bool show=!GuideOpen;guide.gameObject.SetActive(show);guideHandle.gameObject.SetActive(!show);PlaceGuide();
        }
        private void GoToBed(int bed)
        {
            if(bed<0||bed*TreesPerBed>=PlotCount)return;
            Experience.SelectGardenTree(bed*TreesPerBed);
            Locomotion.Teleport(new Vector3(0,0,6.6f+bed*4.8f-2.1f));PlaceGuide();
        }
        public void Hover(int index) { if(index>=0&&index<PlotCount){hovered=index;RefreshState();} }
        public void ClearHover(int index) { if(hovered==index){hovered=-1;hoverDirty=true;} }
        private void RefreshDetails()
        {
            if(simplifiedNavigation)return;
            int index=hovered>=0?hovered:Experience.Ensemble.Index;
            var tree=metrics[index];var state=Experience.Ensemble.Trees[index].State;
            Details.text=(hovered>=0?"POINTING AT ":"SELECTED ")+"TREE "+(index+1)+"  |  Max depth "+tree.MaximumDepth+"  |  "+tree.LeafCount+" leaves  |  "+tree.NodeCount+" nodes\n"+
                (state.AtLeaf?"Leaf reached · route contribution "+state.Contribution.ToString("0.###",System.Globalization.CultureInfo.InvariantCulture):state.Decisions.Count==0?"Unvisited":"Explored "+state.Decisions.Count+" decisions · route saved")+
                (hovered>=0?"  |  Select its plaque to choose this tree":"  |  Enter to explore, or teleport beside its planter");
        }
        private void RefreshState()
        {
            int selected=Experience.Ensemble.Index;int bed=selected/TreesPerBed;
            if(!simplifiedNavigation) {
            header.text="MOONLIT MODEL GARDEN  |  "+PlotCount+" trees\nBed "+(bed+1)+" / "+((PlotCount+9)/10)+"  |  "+Experience.Ensemble.CompletedCount+" leaves reached";
            RefreshDetails();
            profileButton.GetComponent<UnityEngine.UI.Button>().interactable=!Experience.Model.StructureOnlyPreview;
            PreviousButton.GetComponent<UnityEngine.UI.Button>().interactable=selected>0;NextButton.GetComponent<UnityEngine.UI.Button>().interactable=selected+1<PlotCount;
            }
            for(int i=0;i<plots.Count;i++)
            {
                var state=Experience.Ensemble.Trees[i].State;var tree=metrics[i];
                plots[i].ring.sharedMaterial=i==selected?selectedMaterial:state.AtLeaf?completedMaterial:quietRing;
                plots[i].ring.transform.localScale=plots[i].ringScale*(i==hovered?1.07f:1);
                plots[i].label.text="TREE "+(i+1);
                plots[i].hoverLabel.transform.parent.gameObject.SetActive(i==hovered);
                plots[i].hoverLabel.text="TREE "+(i+1)+" · Depth "+tree.MaximumDepth+"\n"+tree.LeafCount+" leaves · "+tree.NodeCount+" nodes\nClick to explore";
            }
            if(simplifiedNavigation) { Navigation.Refresh(); RefreshNearbyPlaques(); return; }
            if(shownBed!=bed)
            {
                shownBed=bed;
                for(int i=0;i<numberButtons.Count;i++)
                {
                    int index=bed*TreesPerBed+i;var button=numberButtons[i];button.SetActive(index<PlotCount);
                    button.GetComponent<GardenPointerTarget>().index=index;button.GetComponentInChildren<TMP_Text>().text=(index+1).ToString();
                }
            }
            RefreshNearbyPlaques();
        }
        private void PlaceGuide()
        {
            if(simplifiedNavigation) { Navigation?.Place(); return; }
            if(guide==null)return;
            Vector3 forward=Locomotion.Head.forward;forward.y=0;if(forward.sqrMagnitude<.01f)forward=Vector3.forward;forward.Normalize();
            var position=Locomotion.Head.position+forward*1.2f;position.y=Locomotion.Head.position.y-.30f;
            guide.transform.position=position;guide.transform.rotation=Quaternion.LookRotation(forward);
            guideHandle.transform.position=position-Vector3.up*.12f-guide.transform.right*.52f;guideHandle.transform.rotation=guide.transform.rotation;
        }
        private void Update()
        {
            if(!Visible && !(simplifiedNavigation && Navigation?.Page==NavigationPage.Tree))return;
            if(hoverDirty) { hoverDirty=false; RefreshState(); }
            if(UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.tabKey.wasPressedThisFrame)ToggleGuide();
            var right=UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
            if(right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton,out bool pressed))
            {
                if(!pressed)guideButtonReleased=true;
                if(pressed&&guideButtonReleased){guideButtonReleased=false;ToggleGuide();}
            }
            if(!Visible || Time.unscaledTime<nextPlaqueRefresh)return;
            nextPlaqueRefresh=Time.unscaledTime+.15f;RefreshNearbyPlaques();
        }
        private void RefreshNearbyPlaques()
        {
            if(!Visible)return;
            bool forest = !simplifiedNavigation || Navigation.Page == NavigationPage.Forest;
            foreach(var plot in plots)
            {
                plot.root.gameObject.SetActive(forest);
                bool nearby=forest && Vector3.Distance(Locomotion.Head.position,plot.root.position)<12;
                plot.plaque.gameObject.SetActive(nearby);

            }
            foreach(var waypoint in waypoints)waypoint.gameObject.SetActive(forest && Vector3.Distance(Locomotion.Head.position,waypoint.transform.position)<16);
        }
        internal Canvas CanvasAt(string name,Vector3 position,Vector2 size,float scale,Transform parent)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.GraphicRaycaster));
            go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=Vector3.one*scale;
            ((RectTransform)go.transform).sizeDelta=size;
            var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=Locomotion.Head.GetComponent<Camera>();
            var template=Experience.explanation.GetComponentInParent<Canvas>(true).GetComponentInChildren<Oculus.Interaction.PointableCanvas>(true);
            if(template!=null)
            {
                var interaction=Instantiate(template.gameObject,go.transform);
                interaction.GetComponent<Oculus.Interaction.PointableCanvas>().InjectCanvas(canvas);
            }
            return canvas;
        }
        internal GameObject Button(Canvas canvas,string label,Vector2 position,Vector2 size,GardenCommand command,int index,int fontSize)
        {
            var go=new GameObject(command+"-"+index,typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button),typeof(GardenPointerTarget));
            go.transform.SetParent(canvas.transform,false);var rect=(RectTransform)go.transform;rect.sizeDelta=size;rect.anchoredPosition=position;
            go.GetComponent<UnityEngine.UI.Image>().color=new Color(.045f,.17f,.21f,.96f);
            go.GetComponent<UnityEngine.UI.Button>().navigation=new UnityEngine.UI.Navigation{mode=UnityEngine.UI.Navigation.Mode.None};
            var target=go.GetComponent<GardenPointerTarget>();target.garden=this;target.command=command;target.index=index;
            Text(go.transform,"Label",label,Vector2.zero,size-new Vector2(12,8),fontSize);return go;
        }
        internal TMP_Text Text(Transform parent,string name,string text,Vector2 position,Vector2 size,int fontSize)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);
            var label=go.GetComponent<TextMeshProUGUI>();label.font=Experience.status.font;label.fontSharedMaterial=Experience.status.fontSharedMaterial;
            label.text=text;label.fontSize=fontSize;label.color=new Color(.88f,.96f,.94f);label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;
            label.textWrappingMode=TextWrappingModes.Normal;label.rectTransform.sizeDelta=size;label.rectTransform.anchoredPosition=position;return label;
        }
        private void Block(string name,Vector3 position,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root.transform,false);
            go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;Destroy(go.GetComponent<Collider>());
        }
        private GameObject MeshObject(string name,Transform parent,Vector3 position,Vector3 size,Mesh mesh,Material material,bool fit)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
            var bounds=mesh.bounds;
            go.transform.localScale=fit?new Vector3(size.x/bounds.size.x,size.y/bounds.size.y,size.z/bounds.size.z):size;
            go.transform.localPosition=position;
            if(fit)go.transform.localPosition-=Vector3.Scale(bounds.center,go.transform.localScale)-Vector3.up*size.y*.5f;
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;return go;
        }
        private void OnDestroy() { if(quietRing!=null)Destroy(quietRing);if(gardenFoliage!=null)Destroy(gardenFoliage); }
    }
}
