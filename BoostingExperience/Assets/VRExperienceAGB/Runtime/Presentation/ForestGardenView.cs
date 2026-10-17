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
        public M5ForestPresentation M5 { get; private set; }
        public M6ExplanationPresentation M6 { get; private set; }
        public Mesh pineMesh, trunkMesh, planterMesh, ringMesh;
        public Mesh clearingRockMesh, clearingFernMesh;
        public Mesh sceneryPineBranchesMesh, sceneryPineTrunkMesh;
        public Material carezzaBackdropMaterial, carezzaSpruceMaterial, carezzaWaterMaterial;
        public Material carezzaGroundMaterial, carezzaRockMaterial;
        public Material pineMaterial, barkMaterial, stoneMaterial, soilMaterial, pathMaterial, selectedMaterial, completedMaterial;
        public OneTreeExperience Experience { get; private set; }
        public GardenLocomotion Locomotion { get; private set; }
        public bool Visible => root != null && root.activeSelf;
        public int PlotCount => plots.Count;
        public IReadOnlyList<GardenTreeMetrics> Metrics => metrics;
        public TMP_Text Details { get; private set; }
        public TreeDescriptionCard DescriptionCard { get; private set; }
        public GameObject EnterButton { get; private set; }
        public GameObject PreviousButton { get; private set; }
        public GameObject NextButton { get; private set; }
        private LineRenderer boostingTrail;
        public int VisiblePlotCount => plots.Count(p => p.root.gameObject.activeSelf);
        private sealed class Plot
        {
            public Transform root;
            public Canvas plaque;
            public TMP_Text label, linkLabel;
            public Renderer ring, ringB;
            public Canvas crownTarget;
            public Transform composition;
            public GameObject predictorMarker;
            public Vector3 ringScale;
        }
        private Mesh predictorMarkerMesh;
        private Material predictorMarkerMaterial;
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
        private Material gardenFoliage, backdropFoliage;
        internal Material ModelFoliage => gardenFoliage;
        private TMP_Text header;
        private GameObject profileButton;
        private Material quietRing, positiveRing, negativeRing;
        private double maximumContribution;
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
            if(environment!=null) {
                walkthroughObjects.Add(environment.gameObject,environment.gameObject.activeSelf);
                foreach(Transform decoration in environment)
                    if(decoration.name=="PineTree")StyleBackdrop(decoration.gameObject);
            }
            gardenFoliage=new Material(pineMaterial) { name="GardenEvergreen", enableInstancing=true };
            gardenFoliage.SetColor("_BaseColor",new Color(.28f,.46f,.31f));
            gardenFoliage.EnableKeyword("_EMISSION");gardenFoliage.SetColor("_EmissionColor",new Color(.012f,.06f,.018f));
            quietRing=new Material(completedMaterial) { name="UnvisitedPlanterRing" };
            quietRing.color=new Color(.10f,.25f,.27f); quietRing.enableInstancing=true;
            quietRing.DisableKeyword("_EMISSION");quietRing.SetColor("_EmissionColor",Color.black);
            positiveRing=ContributionMaterial("PositiveContribution",new Color(.06f,.32f,1f));
            negativeRing=ContributionMaterial("NegativeContribution",new Color(1f,.08f,.12f));
            M5 = gameObject.AddComponent<M5ForestPresentation>(); M5.Configure(this);
            M6 = gameObject.AddComponent<M6ExplanationPresentation>(); M6.Configure(this);
        }
        private Material ContributionMaterial(string name,Color color)
        {
            var material=new Material(completedMaterial){name=name,enableInstancing=true,color=color};
            material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",color*2f);return material;
        }
        internal Material ContributionRing(bool reached, double value) => !reached || value == 0 ? quietRing : value > 0 ? positiveRing : negativeRing;
        public void Sync(bool garden)
        {
            if (Experience == null) return;
            if (builtModel != Experience.Model) Build();
            if (simplifiedNavigation) garden = Navigation.Page != NavigationPage.Tree;
            if (garden != Visible)
            {
                if (garden) { root.SetActive(true); Locomotion.SetGarden(true); PlaceGuide(); }
                else { DescriptionCard.Hide();root.SetActive(false); hovered=-1; Locomotion.SetGarden(false); }
            }
            foreach(var entry in walkthroughObjects) entry.Key.SetActive(!garden && entry.Value);
            if(garden)RefreshState();
            if(simplifiedNavigation) Navigation.Refresh();
            M5.Refresh();

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
            // Clone authored scenery before temporary menu suppression is applied to the rebuilt scene.
            Experience.Director?.PrepareSceneRebuild();
            if(root!=null){root.SetActive(false);Destroy(root);}
            plots.Clear();waypoints.Clear();numberButtons.Clear();hovered=-1;shownBed=-1;
            builtModel=Experience.Model;metrics=builtModel.Trees.Select(t=>new GardenTreeMetrics(t)).ToArray();
            maximumContribution=builtModel.Trees.SelectMany(t=>t.Nodes.OfType<LeafNode>().Select(n=>Math.Abs(t.Weight*n.Score))).DefaultIfEmpty(0).Max();
            root=new GameObject("ModelPineGarden");root.transform.SetParent(Experience.transform,false);root.SetActive(false);
            DescriptionCard=TreeDescriptionCard.Create(this,root.transform);
            int rows=(metrics.Length+TreesPerBed-1)/TreesPerBed;
            float length=rows*4.8f+12;
            var groundBounds=new Bounds(new Vector3(0,0,length*.5f-3),new Vector3(42,0,length+10));
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
                    StyleBackdrop(copy);
                    // Include the actual scenery footprint, even for a short imported ensemble.
                    foreach(var renderer in copy.GetComponentsInChildren<Renderer>()) {
                        var bounds=renderer.localBounds;
                        for(int corner=0;corner<8;corner++) {
                            var point=bounds.center+Vector3.Scale(bounds.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1));
                            groundBounds.Encapsulate(root.transform.InverseTransformPoint(renderer.transform.TransformPoint(point)));
                        }
                    }
                }
            }
            root.AddComponent<ForestPerimeterScenery>().Build(length,stoneMaterial,pineMaterial,oldEnvironment,clearingRockMesh,clearingFernMesh,
                carezzaBackdropMaterial,carezzaSpruceMaterial,carezzaWaterMaterial,carezzaGroundMaterial,carezzaRockMaterial);
            RestoreInteriorPines(rows, oldEnvironment);
            Block("GardenGround",new Vector3(groundBounds.center.x,-.13f,groundBounds.center.z),new Vector3(groundBounds.size.x+24,.22f,groundBounds.size.z+24),carezzaGroundMaterial!=null?carezzaGroundMaterial:soilMaterial);
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
            M6.EnsureModel();
            for(int i=0;i<metrics.Length;i++)BuildPlot(i);
            var trailObject = new GameObject("BoostingOrderTrail"); trailObject.transform.SetParent(root.transform, false);
            boostingTrail = trailObject.AddComponent<LineRenderer>(); boostingTrail.useWorldSpace = false;
            boostingTrail.sharedMaterial = selectedMaterial; boostingTrail.widthMultiplier = .06f;
            boostingTrail.gameObject.SetActive(false);
            if(simplifiedNavigation) Navigation = new SimpleExperienceNavigation(this, root.transform);
            else { Navigation = null; BuildGuide(); }
        }
        private void RestoreInteriorPines(int rows, Transform originalEnvironment)
        {
            if (rows < 2 || sceneryPineBranchesMesh == null || sceneryPineTrunkMesh == null) return;
            var sourceTrunk = originalEnvironment != null ? originalEnvironment.Find("PineTree/Trunk") : null;
            var bark = sourceTrunk != null ? sourceTrunk.GetComponent<Renderer>().sharedMaterial : barkMaterial;
            var scenery = root.GetComponent<ForestPerimeterScenery>();
            var openCrown = scenery.OpenInteriorCrown(sceneryPineBranchesMesh);
            var limbs = scenery.InteriorLimbs(sceneryPineTrunkMesh);
            int groups = Mathf.Min(3, rows - 1);
            for (int group = 0; group < groups; group++)
            {
                int row = groups == 1 ? 0 : Mathf.RoundToInt(Mathf.Lerp(0, rows - 2, group / (float)(groups - 1)));
                foreach (int side in new[] { -1, 1 })
                {
                    var tree = new GameObject("GardenInteriorPine");
                    tree.transform.SetParent(root.transform, false);
                    // Between planters, before the next crosswalk, away from the central aisle.
                    tree.transform.localPosition = new Vector3(side * (group % 2 == 0 ? 7.55f : 13.75f), 0, 8.05f + row * 4.8f);
                    tree.transform.localRotation = Quaternion.Euler(0, group * 71 + side * 23, 0);
                    float height = 7.8f + group * .55f + (side > 0 ? .5f : 0);
                    MeshObject("InteriorBranches", tree.transform, Vector3.up * height * .08f,
                        new Vector3(height * 1.18f, height * .9f, height * 1.18f),
                        openCrown, backdropFoliage != null ? backdropFoliage : pineMaterial, false);
                    MeshObject("InteriorTrunk", tree.transform, Vector3.zero, Vector3.one * height,
                        sceneryPineTrunkMesh, bark, false);
                    MeshObject("InteriorWoodyBranches", tree.transform, Vector3.zero, Vector3.one * height, limbs, bark, false);
                    foreach (var renderer in tree.GetComponentsInChildren<Renderer>())
                    {
                        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        renderer.receiveShadows = false;
                    }
                }
            }
        }

        private void StyleBackdrop(GameObject tree)
        {
            foreach(var renderer in tree.GetComponentsInChildren<Renderer>(true)) {
                if(renderer.name!="RadialBranches")continue;
                if(backdropFoliage==null) {
                    backdropFoliage=new Material(renderer.sharedMaterial){name="BlueFrostSceneryFoliage",enableInstancing=true};
                    backdropFoliage.SetColor("_BaseColor",new Color(.36f,.47f,.58f));
                    backdropFoliage.EnableKeyword("_EMISSION");
                    backdropFoliage.SetColor("_EmissionColor",new Color(.018f,.035f,.065f));
                }
                renderer.sharedMaterial=backdropFoliage;
            }
        }
        private void BuildPlot(int index)
        {
            var tree=metrics[index];var container=new GameObject("ModelTree-"+(index+1)).transform;
            container.SetParent(root.transform,false);container.localPosition=PlotPosition(index);
            MeshObject("Planter",container,Vector3.zero,new Vector3(2.1f,.36f,2.1f),planterMesh,stoneMaterial,true);
            MeshObject("Soil",container,new Vector3(0,.37f,0),new Vector3(1.84f,.06f,1.84f),planterMesh,soilMaterial,true);
            MeshObject("Trunk",container,new Vector3(0,.4f,0),new Vector3(.12f+tree.CrownRadius*.1f,tree.PineHeight,.12f+tree.CrownRadius*.1f),trunkMesh,barkMaterial,true);
            var pine=MeshObject("Pine",container,new Vector3(0,.4f,0),new Vector3(tree.CrownRadius*2,tree.PineHeight,tree.CrownRadius*2),pineMesh,gardenFoliage,true);
            var ring=MeshObject("ProgressRing",container,new Vector3(0,.40f,0),new Vector3(1.04f/.555f,1,1.04f/.555f),ringMesh,quietRing,false).GetComponent<Renderer>();
            var ringB=MeshObject("ProfileBRing",container,new Vector3(0,.44f,0),new Vector3(.83f/.555f,1,.83f/.555f),ringMesh,quietRing,false).GetComponent<Renderer>();
            ringB.gameObject.SetActive(false);
            var plaque=CanvasAt("TreePlaque-"+(index+1),PlotPosition(index)+new Vector3(0,.20f,-1.15f),new Vector2(640,1500),.0024f,root.transform);
            // The crown owns its target, independent of the fixed planter label. It faces the observer from every approach.
            var crown=CanvasAt("PineCrownTarget-"+(index+1),pine.GetComponent<Renderer>().bounds.center-container.position+Vector3.up*tree.PineHeight*.1f,new Vector2(tree.CrownRadius*2/.0024f,tree.PineHeight*.72f/.0024f),.0024f,container);
            var pineTarget=Button(crown,"",Vector2.zero,((RectTransform)crown.transform).sizeDelta,GardenCommand.SelectTree,index,29);
            pineTarget.name="PinePointerTarget-"+(index+1);pineTarget.GetComponent<UnityEngine.UI.Image>().color=Color.clear;
            var button=Button(plaque,"",Vector2.zero,new Vector2(360,62),GardenCommand.SelectTree,index,25);
            button.name="PlanterName-"+(index+1);button.GetComponent<UnityEngine.UI.Image>().color=new Color(.015f,.035f,.045f,.96f);
            var linkCanvas=CanvasAt("PredictorMatch",new Vector3(0,tree.PineHeight+1.5f,0),new Vector2(700,95),.0024f,container,false);
            var linkLabel=Text(linkCanvas.transform,"Match","",Vector2.zero,new Vector2(700,95),25);
            M6.AddComposition(container,index,new Vector3(0,.48f,-1.12f),.002f);
            plots.Add(new Plot{root=container,predictorMarker=CreatePredictorMarker(container,tree.PineHeight),composition=container.Find("PredictorComposition"),plaque=plaque,crownTarget=crown,label=button.GetComponentInChildren<TMP_Text>(),linkLabel=linkLabel,ring=ring,ringB=ringB,ringScale=ring.transform.localScale});
        }
        internal GameObject CreatePredictorMarker(Transform parent,float pineHeight,float scale=1)
        {
            if(predictorMarkerMesh==null) {
                var vertices=new List<Vector3>();var triangles=new List<int>();
                var ring=new[]{new Vector3(.17f,0,0),new Vector3(0,0,.17f),new Vector3(-.17f,0,0),new Vector3(0,0,-.17f)};
                for(int i=0;i<4;i++) {
                    int j=(i+1)%4;
                    foreach(var v in new[]{ring[i],Vector3.up*.25f,ring[j],ring[j],Vector3.down*.32f,ring[i]}) {triangles.Add(vertices.Count);vertices.Add(v);}
                }
                predictorMarkerMesh=new Mesh{name="SelectedPredictorDiamond"};
                predictorMarkerMesh.SetVertices(vertices);predictorMarkerMesh.SetTriangles(triangles,0);predictorMarkerMesh.RecalculateNormals();predictorMarkerMesh.RecalculateBounds();
                predictorMarkerMaterial=new Material(stoneMaterial){name="SelectedPredictorGreen",enableInstancing=true};
                predictorMarkerMaterial.color=new Color(.12f,.95f,.23f);predictorMarkerMaterial.mainTexture=Texture2D.whiteTexture;
                predictorMarkerMaterial.DisableKeyword("_ALPHATEST_ON");predictorMarkerMaterial.SetFloat("_AlphaClip",0);
                predictorMarkerMaterial.EnableKeyword("_EMISSION");predictorMarkerMaterial.SetColor("_EmissionColor",new Color(.035f,.45f,.065f));
            }
            var marker=MeshObject("SelectedPredictorMarker",parent,new Vector3(0,pineHeight+.85f*scale,0),Vector3.one*scale,predictorMarkerMesh,predictorMarkerMaterial,false);
            var renderer=marker.GetComponent<MeshRenderer>();renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            marker.SetActive(false);return marker;
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
            if(Experience.Director?.MenuVisible == true || M5.HelpOpen || M5.Comparison.PanelOpen || M5.Extensions.PanelOpen)return;
            if(command==GardenCommand.SelectTree && index>=0 && index<PlotCount) M5.Atmosphere.SelectAt(plots[index].root.position+Vector3.up);
            if(simplifiedNavigation && Navigation.Activate(command,index)) return;
            M5.Extensions.RefreshMatches();
            boostingTrail.gameObject.SetActive(M5.Extensions.TrailActive && Navigation?.Page == NavigationPage.Forest);
            boostingTrail.positionCount = M5.Extensions.TrailActive ? M5.Extensions.Iteration : 0;
            for (int t = 0; t < boostingTrail.positionCount; t++) boostingTrail.SetPosition(t, StandingPoint(t) + Vector3.up * .07f);
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
            if(Experience.Director != null) { Experience.Director.ToggleMenu(); return; }
            if (M5?.Extensions?.PanelOpen == true) { M5.Extensions.Close(); return; }
            if (M5?.Comparison?.PanelOpen == true) { M5.Comparison.Close(); return; }
            if (M5 != null && M5.HelpOpen) { M5.CloseHelp(); return; }
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
        public void RefreshExplanationLinks() { if (root != null) RefreshState(); if(M5?.Diorama?.Visible==true)M5.Diorama.Refresh(); }
        internal bool IsPredictorMatch(int index) => M5.Extensions.SearchActive ? M5.Extensions.Matches(index) : M6.LinkedFeature!=null && M6.Analysis.MatchingTrees(M6.LinkedFeature).Contains(index);
        public void Hover(int index) { if(index>=0&&index<PlotCount && Experience.Director?.MenuVisible!=true){hovered=index;RefreshState();} }
        public void ClearHover(int index) { if(hovered==index){hovered=-1;hoverDirty=true;} }
        public void ShowTreeChoices(int index)
        {
            if(!Visible || index<0 || index>=PlotCount || Experience.Director?.MenuVisible==true ||
                M5.HelpOpen || M5.Comparison.PanelOpen || M5.Extensions.PanelOpen)return;
            DescriptionCard.Show(index);
        }
        public string TreeContributionCopy(int index)
        {
            var row=Experience.Ensemble.Ledger[index];
            bool reached=row.Progress!=ContributionProgress.Pending;
            double value=row.PresentedContribution;
            if(M5.Extensions.TrailActive && Experience.Ensemble.Evaluation!=null) {
                reached=index<M5.Extensions.Iteration;value=Experience.Ensemble.Evaluation.Trees[index].Contribution;
            }
            if(Experience.Director!=null && Experience.Director.TryContribution(index,false,out var result)) { reached=true;value=result; }
            string signed=value.ToString("+0.###;-0.###;0",System.Globalization.CultureInfo.InvariantCulture);
            if(Experience.Director!=null && Experience.Director.TryContribution(index,true,out var b))
                return "A: "+signed+"  ·  B: "+b.ToString("+0.###;-0.###;0",System.Globalization.CultureInfo.InvariantCulture);
            return reached?(Experience.Ensemble.Mode==ExperienceMode.Manual?"Route contribution: ":"Profile contribution: ")+signed:
                Experience.Ensemble.Trees[index].State.Decisions.Count>0?"Leaf not reached":"Not visited";
        }
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
            M5.Extensions.RefreshMatches();
            boostingTrail.gameObject.SetActive(M5.Extensions.TrailActive && Navigation?.Page == NavigationPage.Forest);
            boostingTrail.positionCount = M5.Extensions.TrailActive ? M5.Extensions.Iteration : 0;
            for (int t = 0; t < boostingTrail.positionCount; t++) boostingTrail.SetPosition(t, StandingPoint(t) + Vector3.up * .07f);
            int selected=Experience.Ensemble.Index;int bed=selected/TreesPerBed;
            if(!simplifiedNavigation) {
            header.text="MOONLIT MODEL GARDEN  |  "+PlotCount+" trees\nBed "+(bed+1)+" / "+((PlotCount+9)/10)+"  |  "+Experience.Ensemble.CompletedCount+" leaves reached";
            RefreshDetails();
            profileButton.GetComponent<UnityEngine.UI.Button>().interactable=!Experience.Model.StructureOnlyPreview;
            PreviousButton.GetComponent<UnityEngine.UI.Button>().interactable=selected>0;NextButton.GetComponent<UnityEngine.UI.Button>().interactable=selected+1<PlotCount;
            }
            var ledger=Experience.Ensemble.Ledger;
            for(int i=0;i<plots.Count;i++)
            {
                plots[i].root.gameObject.SetActive(M5.Extensions.TreeVisible(i));
                var state=Experience.Ensemble.Trees[i].State;var tree=metrics[i];
                var row=ledger[i];bool reached=row.Progress!=ContributionProgress.Pending;
                double contribution=row.PresentedContribution;
                if (M5.Extensions.TrailActive && Experience.Ensemble.Evaluation != null) {
                    reached = i < M5.Extensions.Iteration; contribution = Experience.Ensemble.Evaluation.Trees[i].Contribution;
                }
                if(Experience.Director != null && Experience.Director.TryContribution(i,false,out var finalContribution)) { reached=true; contribution=finalContribution; }
                var material=!reached||contribution==0?quietRing:contribution>0?positiveRing:negativeRing;
                plots[i].ring.sharedMaterial=material;
                bool hasB=Experience.Director!=null && Experience.Director.TryContribution(i,true,out _);
                plots[i].ringB.gameObject.SetActive(hasB);
                if(hasB){Experience.Director.TryContribution(i,true,out var bv);plots[i].ringB.sharedMaterial=ContributionRing(true,bv);}
                float magnitude=reached&&maximumContribution>0?(float)Math.Min(1,Math.Abs(contribution)/maximumContribution):0;
                var scale=plots[i].ringScale;scale.y*=1+4*magnitude;plots[i].ring.transform.localScale=scale;
                string sign=!reached?"":contribution>0?" +":contribution<0?" −":" 0";
                string linkedFeature = M5.Extensions.SearchActive ? M5.Extensions.Feature : M6.LinkedFeature;
                bool linked = IsPredictorMatch(i);
                plots[i].label.text="TREE "+(i+1)+sign;
                plots[i].label.color=new Color(.96f,.98f,1f);
                plots[i].linkLabel.text=linked?"Selected predictor: "+M6.Analysis.Label(linkedFeature):"";
                plots[i].linkLabel.transform.parent.gameObject.SetActive(linked);
                plots[i].predictorMarker.SetActive(linked);
            }
            if(DescriptionCard.Visible)DescriptionCard.RefreshContent();
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
            Navigation?.RefreshForestHint();
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
            for (int i = 0; i < plots.Count; i++)
            {
                var plot = plots[i];
                plot.root.gameObject.SetActive(forest && M5.Extensions.TreeVisible(i));
                float distance=Vector3.Distance(Locomotion.Head.position,plot.root.position);
                bool nearby=forest && distance<12;
                if(plot.composition!=null)plot.composition.gameObject.SetActive(forest && distance<8);
                var link=plot.linkLabel.transform.parent;
                link.gameObject.SetActive(forest && plot.linkLabel.text.Length>0 && Vector3.Distance(Locomotion.Head.position,plot.root.position)<32);
                if(link.gameObject.activeSelf)link.rotation=Quaternion.LookRotation(link.position-Locomotion.Head.position);
                plot.plaque.gameObject.SetActive(forest && (distance<8 || (nearby && i==hovered)));
                plot.crownTarget.gameObject.SetActive(nearby);
                if(nearby)
                {
                    plot.label.fontSize=Mathf.Clamp(distance*6.4f,32,50);
                    var direction=plot.crownTarget.transform.position-Locomotion.Head.position;direction.y=0;
                    if(direction.sqrMagnitude>.01f)plot.crownTarget.transform.rotation=Quaternion.LookRotation(direction);
                }

            }
            foreach(var waypoint in waypoints)waypoint.gameObject.SetActive(forest && Vector3.Distance(Locomotion.Head.position,waypoint.transform.position)<16);
        }
        internal Canvas CanvasAt(string name,Vector3 position,Vector2 size,float scale,Transform parent,bool interactive=true)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Canvas));
            if(interactive)go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=Vector3.one*scale;
            ((RectTransform)go.transform).sizeDelta=size;
            var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=Locomotion.Head.GetComponent<Camera>();
            var template=Experience.explanation.GetComponentInParent<Canvas>(true).GetComponentInChildren<Oculus.Interaction.PointableCanvas>(true);
            if(interactive && template!=null)
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
            Text(go.transform,"Label",label,Vector2.zero,size-new Vector2(12,8),fontSize);go.AddComponent<ButtonHint>();return go;
        }
        internal TMP_Text Text(Transform parent,string name,string text,Vector2 position,Vector2 size,int fontSize)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);
            var label=go.GetComponent<TextMeshProUGUI>();TreeCardTypography.SetFont(label);label.fontStyle=FontStyles.Normal;label.enableAutoSizing=false;label.richText=false;
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
        private void OnDestroy() { if(predictorMarkerMesh!=null)Destroy(predictorMarkerMesh);if(predictorMarkerMaterial!=null)Destroy(predictorMarkerMaterial); if(backdropFoliage!=null)Destroy(backdropFoliage);if(quietRing!=null)Destroy(quietRing);if(gardenFoliage!=null)Destroy(gardenFoliage);if(positiveRing!=null)Destroy(positiveRing);if(negativeRing!=null)Destroy(negativeRing); }
    }
}
