using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using VRExperienceAGB.Presentation;

namespace VRExperienceAGB.Editor
{
    /// <summary>Applies the selected moonlit art direction through Unity serialization, preserving the teaching rig and controls.</summary>
    public static class MoonlitClearingStyler
    {
        private const string Folder = "Assets/VRExperienceAGB/Art/Moonlit";
        private static Material stone, ground, bark, pine, pineLight, cyan, amber, distant;
        private static Mesh rockMesh, crownMesh, platformMesh, foliageMesh, grassMesh, rimMesh, foregroundFoliageMesh, trunkMesh;
        private static Material foliage;
        private static System.Random random;
        public static string Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop preview before styling.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != OneTreeSceneBuilder.ScenePath) throw new InvalidOperationException("Open OneTreeLearning first.");
            var view = UnityEngine.Object.FindAnyObjectByType<OneTreeExperience>();
            if (view == null || view.nodeViews.Length != 7 || view.controls.Length < 12) throw new InvalidOperationException("Teaching references are incomplete.");
            Undo.RegisterFullObjectHierarchyUndo(view.gameObject, "Style moonlit clearing");
            random = new System.Random(731);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/SlateAlbedo.png");
            var importer = (TextureImporter)AssetImporter.GetAtPath(Folder + "/SlateAlbedo.png");
            importer.maxTextureSize = 2048; importer.anisoLevel = 8; importer.mipmapEnabled = true; importer.wrapMode = TextureWrapMode.Repeat; importer.SaveAndReimport();
            stone = Mat("Slate", new Color(.70f,.76f,.85f), true, texture);
            ground = Mat("Earth", new Color(.28f,.32f,.33f), true, texture);
            bark = Mat("Bark", new Color(.18f,.15f,.13f), true);
            pine = Mat("Pine", new Color(.065f,.18f,.19f), true);
            pineLight = Mat("PineTips", new Color(.12f,.29f,.28f), true);
            distant = Mat("DistantPine", new Color(.095f,.17f,.27f), false);
            cyan = Mat("CyanLight", new Color(.20f,.74f,.86f), false);
            amber = Mat("AmberLight", new Color(1f,.62f,.16f), false);
            rockMesh = MeshAsset("FacetedRock", Rock());
            crownMesh = MeshAsset("PineCrown", Crown());
            platformMesh = MeshAsset("StonePlatform", Disc());
            rimMesh = MeshAsset("SmoothLuminousRim", Torus(128, 8, .555f, .0125f));
            foregroundFoliageMesh = MeshAsset("ForegroundPineBranches", ForegroundFoliage());
            trunkMesh = MeshAsset("ForegroundPineTrunk", BarkTrunk());
            foliageMesh = MeshAsset("PineCards", FoliageCards());
            grassMesh = MeshAsset("GroundLeaves", Grass());
            var foliageImporter=(TextureImporter)AssetImporter.GetAtPath(Folder+"/PineFoliage.png");
            foliageImporter.alphaIsTransparency=true; foliageImporter.mipmapEnabled=true; foliageImporter.maxTextureSize=2048; foliageImporter.SaveAndReimport();
            foliage=Mat("PineFoliage",new Color(.72f,.83f,.91f),true,AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/PineFoliage.png"));
            foliage.SetFloat("_AlphaClip",1); foliage.EnableKeyword("_ALPHATEST_ON"); foliage.SetFloat("_Cutoff",.5f); foliage.SetFloat("_AlphaToMask",1);
            foliage.SetFloat("_Cull",0); foliage.SetOverrideTag("RenderType","TransparentCutout"); foliage.renderQueue=2450;
            var old = view.transform.Find("MoonlitEnvironment");
            if(old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var world = new GameObject("MoonlitEnvironment").transform; world.SetParent(view.transform,false);
            foreach (Transform child in view.transform)
                if (child.name == "DecorativeCrown" || child.name == "DecorativeTrunk" || child.name == "Clearing") child.gameObject.SetActive(false);
            Environment(world);
            Layout(view);
            Console(view);
            // Every teaching object moves with posture and deep-tree focus. Never bake it into static batches.
            foreach (var item in view.presentationRoot.GetComponentsInChildren<Transform>(true)) item.gameObject.isStatic = false;
            var preview = view.GetComponent<DesktopTreePreview>();
            preview.previewCamera.transform.position = new Vector3(0,1.65f,-.2f);
            preview.previewCamera.transform.rotation = Quaternion.Euler(-5,0,0);
            preview.previewCamera.fieldOfView = 58;
            preview.canvases = view.GetComponentsInChildren<Canvas>(true);
            foreach (var camera in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)))
            { camera.clearFlags=CameraClearFlags.Skybox; camera.backgroundColor=new Color(.035f,.08f,.16f); camera.farClipPlane=160; }
            var sky=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/TwilightSky.mat");
            if(sky==null){sky=new Material(Shader.Find("VRExperienceAGB/TwilightSky"));AssetDatabase.CreateAsset(sky,Folder+"/TwilightSky.mat");}
            sky.shader=Shader.Find("VRExperienceAGB/TwilightSky"); sky.SetFloat("_HorizonOffset",-.23f);
            sky.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/TwilightPanorama.png"));
            sky.SetFloat("_Exposure",.42f); RenderSettings.skybox=sky;
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.27f,.37f,.55f);
            RenderSettings.ambientEquatorColor=new Color(.22f,.30f,.39f);
            RenderSettings.ambientGroundColor=new Color(.14f,.17f,.20f);
            RenderSettings.fog=true; RenderSettings.fogColor=new Color(.055f,.115f,.20f);
            RenderSettings.fogMode=FogMode.ExponentialSquared; RenderSettings.fogDensity=.012f;
            foreach(var light in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)))
                if(!light.transform.IsChildOf(world)) light.enabled=false;
            var moonlight=new GameObject("Moonlight",typeof(Light)); moonlight.transform.SetParent(world,false);
            moonlight.transform.rotation=Quaternion.Euler(48,-35,0);
            var sun=moonlight.GetComponent<Light>(); sun.type=LightType.Directional; sun.color=new Color(.66f,.78f,1); sun.intensity=2.1f; sun.shadows=LightShadows.None;
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            return "Moonlit clearing saved; tracked rig and all 12 controls retained.";
        }
        private static void Layout(OneTreeExperience view)
        {
            var positions=new[]{new Vector3(0,1.75f,4.8f),new Vector3(-1.65f,2.20f,6),new Vector3(1.65f,2.20f,6),
                new Vector3(-2.7f,2.85f,7.2f),new Vector3(-.9f,2.85f,7.2f),new Vector3(.9f,2.85f,7.2f),new Vector3(2.7f,2.85f,7.2f)};
            view.idleMaterial=stone; view.visitedMaterial=Mat("VisitedSlate",new Color(.25f,.63f,.69f),true,stone.mainTexture);
            view.activeMaterial=Mat("ActiveSlate",new Color(.82f,.66f,.38f),true,stone.mainTexture);
            for(int i=0;i<7;i++)
            {
                var n=view.nodeViews[i]; n.transform.localPosition=positions[i]; n.transform.localScale=Vector3.one;
                n.GetComponent<MeshFilter>().sharedMesh=platformMesh; n.platform.sharedMaterial=stone;
                n.transform.localScale=Vector3.one*(i==0?1.05f:.78f); n.transform.localRotation=Quaternion.Euler(-20,0,0);
                n.dropAnchor.localPosition=positions[i]+Vector3.up*.29f;
                var label=n.title.transform.parent; label.localPosition=positions[i]+new Vector3(0,i<3?.32f:.36f,-.16f); label.localScale=Vector3.one*.0045f;
                n.title.fontSize=i==0?34:29; n.title.rectTransform.sizeDelta=new Vector2(i==0?610:520,65);
                n.title.fontStyle=FontStyles.Bold; n.title.color=new Color(.90f,.96f,1); n.marker.fontSize=20;
                n.title.rectTransform.anchoredPosition=new Vector2(0,8); n.marker.rectTransform.anchoredPosition=new Vector2(0,-37);
                var rim=n.transform.Find("LuminousRim"); if(rim!=null) UnityEngine.Object.DestroyImmediate(rim.gameObject);
                Obj("LuminousRim",n.transform,new Vector3(0,-.10f,0),Vector3.one,rimMesh,cyan);
            }
            var model=VRExperienceAGB.Import.NormalizedModelJson.ReadModel(view.modelFile.text).Value;
            int branchIndex=0;
            var branchLabels=view.presentationRoot.GetComponentsInChildren<Canvas>(true).Where(c=>c.name=="BranchMeaning").ToArray();
            foreach(var split in model.Trees[0].Nodes.OfType<VRExperienceAGB.Domain.SplitNode>())
            foreach(var truth in new[]{true,false})
            {
                var from=view.nodeViews.Single(n=>n.nodeId==split.Id).transform.localPosition;
                var to=view.nodeViews.Single(n=>n.nodeId==(truth?split.TrueChild:split.FalseChild)).transform.localPosition;
                var lr=view.presentationRoot.Find(split.Id+(truth?"-True":"-False")).GetComponent<LineRenderer>();
                lr.positionCount=24; lr.widthMultiplier=.042f; lr.startWidth=lr.endWidth=.042f; lr.sharedMaterial=cyan;
                for(int k=0;k<24;k++){float t=k/23f;var p=Vector3.Lerp(from,to,t);p.y-=Mathf.Sin(t*Mathf.PI)*.10f;lr.SetPosition(k,p);}
                // Keep exact branch copy in the scene for inspection; only the current fork needs large visible labels.
                var label=branchLabels[branchIndex++]; label.transform.localPosition=Vector3.Lerp(from,to,.5f)+new Vector3(truth?-.28f:.28f,.06f,-.03f);
                label.gameObject.SetActive(false);
            }
            view.drop.localScale=Vector3.one*.22f;
            var orb=Mat("SampleAmber",new Color(1,.64f,.17f),true);
            orb.EnableKeyword("_EMISSION"); orb.SetColor("_EmissionColor",new Color(.45f,.17f,.015f)); orb.SetFloat("_Smoothness",.55f);
            view.drop.GetComponent<Renderer>().sharedMaterial=orb;
            view.drop.position=view.nodeViews[0].dropAnchor.position;
        }
        private static void Console(OneTreeExperience view)
        {
            var panel=(RectTransform)view.status.transform.parent;
            panel.localPosition=new Vector3(0,1.05f,3.45f); panel.localScale=Vector3.one*.0025f; panel.sizeDelta=new Vector2(1400,1000);
            panel.GetComponent<UnityEngine.UI.Image>().color=Color.clear;
            SetText(view.status,-70,-175,870,35,19);
            SetText(view.explanation,0,420,1200,100,34);
            SetText(view.score,0,-30,790,108,24);
            SetText(view.feedback,0,-255,1130,110,19);
            view.explanation.fontStyle=FontStyles.Bold;
            foreach(var c in view.controls)
            {
                var rt=(RectTransform)c.transform; var image=c.GetComponent<UnityEngine.UI.Image>();
                image.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); image.type=UnityEngine.UI.Image.Type.Sliced;
                image.color=new Color(.045f,.12f,.19f,.98f);
                var colors=c.GetComponent<UnityEngine.UI.Button>().colors;
                colors.normalColor=Color.white; colors.highlightedColor=new Color(.6f,.95f,1); colors.selectedColor=Color.white;
                colors.pressedColor=new Color(1,.76f,.38f); colors.disabledColor=new Color(.45f,.5f,.56f,.7f); c.GetComponent<UnityEngine.UI.Button>().colors=colors;
                var outline=c.GetComponent<UnityEngine.UI.Outline>()??c.gameObject.AddComponent<UnityEngine.UI.Outline>();
                outline.effectColor=new Color(.19f,.49f,.6f,.9f); outline.effectDistance=new Vector2(1.5f,-1.5f);
                c.label.fontSize=24; c.label.fontStyle=FontStyles.Normal; c.label.textWrappingMode=TextWrappingModes.Normal;
                switch(c.action)
                {
                    case TreeAction.TrueBranch: Place(rt,-370,205,330,112); c.label.fontSize=28; break;
                    case TreeAction.FalseBranch: Place(rt,370,205,330,112); c.label.fontSize=28; break;
                    case TreeAction.Manual: Place(rt,-245,75,455,64); c.label.fontSize=27; c.label.fontStyle=FontStyles.Bold; break;
                    case TreeAction.Profile: Place(rt,245,75,455,64); c.label.fontSize=27; c.label.text="Follow a profile"; break;
                    case TreeAction.Back: Place(rt,-475,-30,140,55); break;
                    case TreeAction.Restart: Place(rt,475,-30,140,55); break;
                    case TreeAction.Step: Place(rt,-170,-110,155,48); break;
                    case TreeAction.Play: Place(rt,0,-110,155,48); break;
                    case TreeAction.Pause: Place(rt,170,-110,155,48); break;
                    case TreeAction.Overview: Place(rt,-470,-110,260,48); c.label.fontSize=21; break;
                    case TreeAction.NextProfile: Place(rt,470,-110,260,48); c.label.fontSize=21; break;
                    case TreeAction.Seated: Place(rt,525,-175,170,36); c.label.fontSize=17; break;
                }
                c.label.rectTransform.anchorMin=Vector2.zero; c.label.rectTransform.anchorMax=Vector2.one;
                c.label.rectTransform.offsetMin=new Vector2(10,5); c.label.rectTransform.offsetMax=new Vector2(-10,-5);
            }
            var old=view.presentationRoot.Find("ConsoleStone"); if(old!=null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var stand=Obj("ConsoleStone",view.presentationRoot,new Vector3(0,.96f,3.93f),new Vector3(3.3f,1.03f,.52f),rockMesh,stone);
            stand.transform.localRotation=Quaternion.Euler(0,0,0);
            foreach(float x in new[]{-.925f,.925f})
            {
                var name=x<0?"TrueChoiceStone":"FalseChoiceStone";
                var existing=view.presentationRoot.Find(name);if(existing!=null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
                Obj(name,view.presentationRoot,new Vector3(x,1.56f,3.72f),new Vector3(.91f,.38f,.32f),rockMesh,stone);
            }
        }
        private static void SetText(TMP_Text t,float x,float y,float w,float h,int size)
        {Place(t.rectTransform,x,y,w,h);t.fontSize=size;t.textWrappingMode=TextWrappingModes.Normal;t.color=new Color(.91f,.96f,1);}
        private static void Place(RectTransform rt,float x,float y,float w,float h)
        {rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);rt.pivot=new Vector2(.5f,.5f);rt.anchoredPosition=new Vector2(x,y);rt.sizeDelta=new Vector2(w,h);}
        private static void Environment(Transform root)
        {
            Obj("Ground",root,new Vector3(0,-.12f,15),new Vector3(85,.2f,85),Cube(),ground);
            for(int i=0;i<40;i++)
            {
                float x=Range(-28,28),z=Range(7,42);
                if(Mathf.Abs(x)<6.5f) x=Mathf.Sign(x)*Range(7,25);
                float h=Range(3.5f,7); Tree(root,new Vector3(x,0,z),h,i%3==0);
            }
            Tree(root,new Vector3(-6.4f,0,5.5f),8.5f,false); Tree(root,new Vector3(6.8f,0,6),9,false);
            for(int i=0;i<55;i++)
            {
                float x=Range(-13,13),z=Range(1,17); if(Mathf.Abs(x)<2.4f&&z<9) x=Mathf.Sign(x)*Range(3.5f,8);
                float size=Range(.2f,.95f);
                var rock=Obj("SlateBoulder",root,new Vector3(x,size*.18f,z),new Vector3(size*1.5f,size,size),rockMesh,stone);
                rock.transform.localRotation=Quaternion.Euler(Range(-12,12),Range(0,360),Range(-12,12));
            }
            // Reuse a small set of saved meshes and materials; no animated foliage or physics colliders.
            for(int i=0;i<100;i++)
            {
                float x=Range(-12,12),z=Range(1,18); if(Mathf.Abs(x)<2.6f&&z<9) continue;
                var tuft=Obj("GroundFern",root,new Vector3(x,0,z),new Vector3(.18f,Range(.18f,.38f),.18f),grassMesh,pineLight);
                tuft.transform.localRotation=Quaternion.Euler(0,Range(0,360),0);
            }
            
            // A distant, world-space lunar disc with a shader halo remains stable under head movement.
            var moon=GameObject.CreatePrimitive(PrimitiveType.Quad); moon.name="Moon";
            moon.transform.SetParent(root,false); moon.transform.localPosition=new Vector3(32,43,75);
            moon.transform.localRotation=Quaternion.LookRotation(moon.transform.localPosition-new Vector3(0,1.65f,0));
            moon.transform.localScale=Vector3.one*14.5f;
            UnityEngine.Object.DestroyImmediate(moon.GetComponent<Collider>());
            var lunar=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Moon.mat");
            lunar.shader=Shader.Find("VRExperienceAGB/LunarDisc");
            moon.GetComponent<Renderer>().sharedMaterial=lunar;
            PlaceLanterns(root);
        }
        public static string FixLanterns()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop preview first.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!=OneTreeSceneBuilder.ScenePath) throw new InvalidOperationException("Open the teaching scene first.");
            var view=UnityEngine.Object.FindAnyObjectByType<OneTreeExperience>();
            var root=view.transform.Find("MoonlitEnvironment");
            stone=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Slate.mat");
            bark=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Bark.mat");
            amber=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/AmberLight.mat");
            rockMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/FacetedRock.asset");
            platformMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/StonePlatform.asset");
            PlaceLanterns(root);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            return "Both lanterns seated on level plinths; intersecting generated rocks removed.";
        }
        private static void PlaceLanterns(Transform root)
        {
            foreach(var child in root.Cast<Transform>().Where(t=>t.name.StartsWith("Lantern",StringComparison.Ordinal)).ToArray())
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            foreach(float x in new[]{-2.8f,2.8f})
            {
                // Reserve a clear footprint so randomly scattered rocks cannot swallow a lantern.
                var clearance=new Bounds(root.TransformPoint(new Vector3(x,.4f,4.5f)),new Vector3(1.2f,1.4f,1.2f));
                foreach(var rock in root.Cast<Transform>().Where(t=>t.name=="SlateBoulder").ToArray())
                    if(rock.GetComponent<Renderer>().bounds.Intersects(clearance)) UnityEngine.Object.DestroyImmediate(rock.gameObject);
                var scale=new Vector3(.7f,.8f,.7f);
                float baseY=-.02f-platformMesh.bounds.min.y*scale.y;
                float top=baseY+.10f*scale.y; // Flat central face, inside the bevel.
                Obj("LanternBase",root,new Vector3(x,baseY,4.5f),scale,platformMesh,stone);
                Obj("LanternGlow",root,new Vector3(x,top+.12f,4.5f),new Vector3(.15f,.24f,.15f),Cube(),amber);
                float capY=top+.24f-rockMesh.bounds.min.y*.12f;
                Obj("LanternCap",root,new Vector3(x,capY,4.5f),new Vector3(.3f,.12f,.3f),rockMesh,bark);
                foreach(float dx in new[]{-.09f,.09f})
                foreach(float dz in new[]{-.09f,.09f})
                    Obj("LanternFrame",root,new Vector3(x+dx,top+.12f,4.5f+dz),new Vector3(.018f,.24f,.018f),Cube(),bark);
                var go=new GameObject("LanternLight",typeof(Light));go.transform.SetParent(root,false);go.transform.localPosition=new Vector3(x,top+.2f,4.5f);
                var light=go.GetComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.60f,.22f);light.intensity=5;light.range=7;light.shadows=LightShadows.None;
            }
        }

        private static void Tree(Transform root,Vector3 p,float height,bool far)
        {
            var tree=new GameObject("PineTree").transform;tree.SetParent(root,false);tree.localPosition=p;tree.localRotation=Quaternion.Euler(0,Range(0,360),0);
            // Radial branch geometry gives stationary environment trees depth from every viewing angle.
            if (p.z <= 6.1f)
            {
                var detailedBark=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/DetailedBark.mat");
                if(detailedBark==null) { detailedBark=new Material(Shader.Find("VRExperienceAGB/Bark")); AssetDatabase.CreateAsset(detailedBark,Folder+"/DetailedBark.mat"); }
                detailedBark.shader=Shader.Find("VRExperienceAGB/Bark"); detailedBark.enableInstancing=true;
                Obj("Trunk",tree,Vector3.zero,Vector3.one*height,trunkMesh,detailedBark);
                for(int branch=0;branch<14;branch++)
                {
                    float y=.18f+branch*.043f, angle=branch*137.5f*Mathf.Deg2Rad;
                    float length=height*(.24f-y*.18f);
                    var direction=new Vector3(Mathf.Cos(angle),.22f,Mathf.Sin(angle)).normalized;
                    var limb=Obj("WoodyBranch",tree,new Vector3(0,y*height,0),new Vector3(height*.17f,length,height*.17f),trunkMesh,detailedBark);
                    limb.transform.localRotation=Quaternion.FromToRotation(Vector3.up,direction);
                }
                Obj("RadialBranches",tree,Vector3.zero,Vector3.one*height,foregroundFoliageMesh,foliage);
            }
            else {
                Obj("RadialBranches",tree,Vector3.zero,Vector3.one*height,foregroundFoliageMesh,foliage);
                Obj("Trunk",tree,Vector3.zero,Vector3.one*height,trunkMesh,
                    AssetDatabase.LoadAssetAtPath<Material>(Folder+"/DetailedBark.mat"));
            }
        }
        public static string UpgradeBackgroundPines()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop preview before upgrading trees.");
            var view=UnityEngine.Object.FindAnyObjectByType<OneTreeExperience>();
            var environment=view?.transform.Find("MoonlitEnvironment");
            if(environment==null)throw new InvalidOperationException("Open the teaching scene first.");
            var branches=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/ForegroundPineBranches.asset");
            var trunk=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/ForegroundPineTrunk.asset");
            var barkMaterial=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/DetailedBark.mat");
            if(branches==null||trunk==null||barkMaterial==null)throw new InvalidOperationException("Existing pine artwork is missing.");
            Undo.RegisterFullObjectHierarchyUndo(environment.gameObject,"Give background pines radial branches");
            int changed=0;
            foreach(Transform tree in environment)
            {
                if(tree.name!="PineTree")continue;
                var cards=tree.Find("Foliage"); if(cards==null)continue;
                cards.GetComponent<MeshFilter>().sharedMesh=branches; cards.name="RadialBranches";
                if(tree.Find("Trunk")==null)Obj("Trunk",tree,Vector3.zero,cards.localScale,trunk,barkMaterial);
                changed++;
            }
            EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
            EditorSceneManager.SaveScene(view.gameObject.scene);
            return "Upgraded "+changed+" background pines; model trees and foreground trees preserved.";
        }
        private static GameObject Obj(string name,Transform parent,Vector3 p,Vector3 scale,Mesh mesh,Material material)
        {var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=scale;go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=material;go.isStatic=true;return go;}
        private static Mesh Cube(){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);var mesh=go.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(go);return mesh;}
        private static float Range(float min,float max)=>min+(float)random.NextDouble()*(max-min);
        private static Material Mat(string name,Color color,bool lit,Texture texture=null)
        {var path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);var shader=Shader.Find(lit?"Universal Render Pipeline/Lit":"Universal Render Pipeline/Unlit");if(m==null){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}m.shader=shader;m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.12f);if(texture!=null)m.SetTexture("_BaseMap",texture);m.enableInstancing=true;return m;}
        private static Mesh MeshAsset(string name,Mesh source)
        {var path=Folder+"/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old==null){source.name=name;AssetDatabase.CreateAsset(source,path);return source;}EditorUtility.CopySerialized(source,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(source);return old;}
        private static Mesh Grass()
        {
            var v=new List<Vector3>();var t=new List<int>();
            for(int i=0;i<7;i++)
            {
                var rotation=Quaternion.Euler(0,i*137.5f,0);
                var a=rotation*new Vector3(-.08f,0,0);var b=rotation*new Vector3(.08f,0,0);
                var c=rotation*new Vector3(.03f,.6f,.26f);var d=rotation*new Vector3(-.05f,.6f,.26f);var tip=rotation*new Vector3(0,.8f,.58f);
                Tri(v,t,a,c,b);Tri(v,t,a,d,c);Tri(v,t,d,tip,c);
                Tri(v,t,b,c,a);Tri(v,t,c,d,a);Tri(v,t,c,tip,d);
            }
            return Make(v,t);
        }
        private static Mesh FoliageCards()
        {
            var vertices=new List<Vector3>(); var uv=new List<Vector2>(); var triangles=new List<int>();
            for(int i=0;i<2;i++)
            {
                var rotation=Quaternion.Euler(0,i*90,0);int n=vertices.Count;
                vertices.Add(rotation*new Vector3(-.333f,0,0));vertices.Add(rotation*new Vector3(.333f,0,0));
                vertices.Add(rotation*new Vector3(.333f,1,0));vertices.Add(rotation*new Vector3(-.333f,1,0));
                uv.AddRange(new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)});
                triangles.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});
            }
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        // Radial, staggered branch layers give nearby conifers depth under head movement.
        // The existing alpha-tested foliage atlas is shared with distant cards.
        private static Mesh BarkTrunk()
        {
            const int sides=36, levels=18;
            var v=new List<Vector3>(); var t=new List<int>(); var uv=new List<Vector2>();
            for(int row=0;row<=levels;row++)
            for(int col=0;col<=sides;col++)
            {
                float h=(float)row/levels,a=(float)col/sides*Mathf.PI*2;
                float radius=Mathf.Lerp(.040f,.0015f,h)*(1+.11f*Mathf.Sin(a*7+h*5)+.07f*Mathf.Sin(a*13-h*3));
                radius+=.008f*Mathf.Exp(-h*18)*(1+.4f*Mathf.Sin(a*5));
                var bend=new Vector3(Mathf.Sin(h*4)*.009f,h,Mathf.Sin(h*3)*.007f);
                v.Add(bend+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius)); uv.Add(new Vector2((float)col/sides,h));
                if(row<levels&&col<sides) {int n=row*(sides+1)+col;t.AddRange(new[]{n,n+sides+1,n+1,n+1,n+sides+1,n+sides+2});}
            }
            var mesh=new Mesh(); mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        private static Mesh ForegroundFoliage()
        {
            var vertices=new List<Vector3>(); var uv=new List<Vector2>(); var indices=new List<int>();
            for(int layer=0;layer<7;layer++)
            for(int branch=0;branch<10;branch++)
            {
                float bottom=.12f+layer*.105f, top=bottom+.25f;
                float radius=.34f*(1-bottom);
                var rotation=Quaternion.Euler(0,branch*36+layer*23,0);
                int n=vertices.Count;
                vertices.Add(rotation*new Vector3(-radius*.42f,bottom,radius));
                vertices.Add(rotation*new Vector3(radius*.42f,bottom,radius));
                vertices.Add(rotation*new Vector3(radius*.16f,top,radius*.2f));
                vertices.Add(rotation*new Vector3(-radius*.16f,top,radius*.2f));
                uv.AddRange(new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)});
                indices.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});
            }
            var mesh=new Mesh(); mesh.SetVertices(vertices); mesh.SetUVs(0,uv); mesh.SetTriangles(indices,0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        private static Mesh Torus(int segments,int sides,float radius,float tube)
        {
            var vertices=new List<Vector3>(); var normals=new List<Vector3>(); var triangles=new List<int>();
            for(int i=0;i<segments;i++)
            for(int j=0;j<sides;j++)
            {
                float a=i*Mathf.PI*2/segments,b=j*Mathf.PI*2/sides;
                var radial=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                var normal=radial*Mathf.Cos(b)+Vector3.up*Mathf.Sin(b);
                vertices.Add(radial*radius+normal*tube); normals.Add(normal);
                int n=i*sides+j,next=((i+1)%segments)*sides+j,up=i*sides+(j+1)%sides,diagonal=((i+1)%segments)*sides+(j+1)%sides;
                triangles.AddRange(new[]{n,up,next,up,diagonal,next});
            }
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();return mesh;
        }
        private static Mesh Crown()
        {
            var v=new List<Vector3>();var t=new List<int>();
            for(int i=0;i<20;i++)
            {float a=i*Mathf.PI*2/20,b=(i+1)*Mathf.PI*2/20;int n=v.Count;
                float ra=i%2==0?1:.64f, rb=(i+1)%2==0?1:.64f;
                v.Add(new Vector3(Mathf.Cos(a)*ra,i%2==0?0:.1f,Mathf.Sin(a)*ra));v.Add(new Vector3(.08f,1,0));v.Add(new Vector3(Mathf.Cos(b)*rb,(i+1)%2==0?0:.1f,Mathf.Sin(b)*rb));t.AddRange(new[]{n,n+1,n+2});}
            return Make(v,t);
        }
        private static Mesh Disc()
        {
            return RingStone(128, new[]{.46f,.50f,.526f,.54f,.536f,.515f,.49f}, new[]{.10f,.091f,.065f,.015f,-.075f,-.125f,-.15f}, .002f);
        }
        private static Mesh Rock()
        {
            return RingStone(48, new[]{.30f,.40f,.48f,.525f,.54f,.52f,.46f,.35f}, new[]{.5f,.46f,.36f,.20f,.02f,-.22f,-.40f,-.5f}, .024f);
        }
        // Closed rings add small irregular bevels without subdividing the flat stone faces.
        private static Mesh RingStone(int segments,float[] radii,float[] heights,float roughness)
        {
            var v=new List<Vector3>();var t=new List<int>();
            var rings=new Vector3[radii.Length,segments];
            for(int r=0;r<radii.Length;r++)
            for(int i=0;i<segments;i++)
            {
                float a=i*Mathf.PI*2/segments;
                float chip=(Mathf.Sin(a*5+.4f)+Mathf.Sin(a*9)*.35f)*roughness;
                float radius=radii[r]+chip;
                rings[r,i]=new Vector3(Mathf.Cos(a)*radius,heights[r]+chip*.25f,Mathf.Sin(a)*radius);
            }
            for(int i=0;i<segments;i++)
            {
                int j=(i+1)%segments;
                Tri(v,t,Vector3.up*heights[0],rings[0,j],rings[0,i]);
                int bottom=radii.Length-1;
                Tri(v,t,Vector3.up*heights[bottom],rings[bottom,i],rings[bottom,j]);
                for(int r=0;r<bottom;r++)
                {
                    Tri(v,t,rings[r,i],rings[r,j],rings[r+1,j]);
                    Tri(v,t,rings[r,i],rings[r+1,j],rings[r+1,i]);
                }
            }
            var mesh = Make(v,t);
            var normals = mesh.normals;
            var averages = new Dictionary<Vector3, Vector3>();
            for (int i = 0; i < v.Count; i++)
                averages[v[i]] = averages.TryGetValue(v[i], out var sum) ? sum + normals[i] : normals[i];
            for (int i = 0; i < v.Count; i++) normals[i] = Vector3.Lerp(normals[i], averages[v[i]].normalized, 1f).normalized;
            mesh.normals = normals;
            return mesh;
        }
        private static void Tri(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c){int n=v.Count;v.AddRange(new[]{a,b,c});t.AddRange(new[]{n,n+1,n+2});}
        private static Mesh Make(List<Vector3> v,List<int> t)
        {var m=new Mesh();m.SetVertices(v);m.SetTriangles(t,0);m.SetUVs(0,v.Select(p=>new Vector2(p.x+p.y*.3f,p.z+p.y*.7f)).ToList());m.RecalculateNormals();m.RecalculateBounds();return m;}
    }
}
