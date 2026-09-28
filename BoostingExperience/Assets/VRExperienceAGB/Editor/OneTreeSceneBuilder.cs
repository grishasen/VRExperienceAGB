using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Oculus.Interaction;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using VRExperienceAGB.Domain;
using VRExperienceAGB.Import;
using VRExperienceAGB.Presentation;

namespace VRExperienceAGB.Editor
{
    /// <summary>Creates the initial slice once, through Unity serialization. Existing scenes are never overwritten.</summary>
    public static class OneTreeSceneBuilder
    {
        public const string ScenePath = "Assets/VRExperienceAGB/Scenes/OneTreeLearning.unity";
        private const string AssetsRoot = "Assets/VRExperienceAGB";
        private static TMP_FontAsset font;
        private static Material stone, cyan, gold, floor, line;

        public static string Create()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) return "Scene already exists; edit it in place.";
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the current scene before generating the slice.");
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (font == null) throw new InvalidOperationException("Import TMP essentials before generating the scene.");
            var sourceScene = EditorSceneManager.OpenScene(AssetsRoot + "/Scenes/SmokeTest.unity");
            // Save a copy before modifying any object; the smoke-test source remains intact.
            if (!EditorSceneManager.SaveScene(sourceScene, ScenePath, true)) throw new InvalidOperationException("Could not copy the smoke scene.");
            var scene = EditorSceneManager.OpenScene(ScenePath);
            foreach (var name in new[] { "Floor", "SelectableCube", "Label" })
            {
                var old = scene.GetRootGameObjects().SingleOrDefault(g => g.name == name);
                if (old != null) UnityEngine.Object.DestroyImmediate(old);
            }
            var rig = scene.GetRootGameObjects().Single(g => g.name == "OVRCameraRig");
            var root = new GameObject("OneTreeExperience");
            var view = root.AddComponent<OneTreeExperience>();
            var content = new GameObject("Presentation").transform; content.SetParent(root.transform, false); view.presentationRoot = content;
            EnsureFolder(AssetsRoot + "/Data"); EnsureFolder(AssetsRoot + "/Materials/OneTree");
            var repository = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "../.."));
            foreach (var file in new[] { "demo-model.json", "demo-profiles.json" })
                File.Copy(Path.Combine(repository, "data/examples", file), AssetsRoot + "/Data/" + file, false);
            AssetDatabase.Refresh();
            view.modelFile = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetsRoot + "/Data/demo-model.json");
            view.profilesFile = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetsRoot + "/Data/demo-profiles.json");
            var model = NormalizedModelJson.ReadModel(view.modelFile.text).Value;
            var tree = model.Trees[0];
            stone = Material("Stone", new Color(0.075f, 0.14f, 0.18f));
            cyan = Material("Visited", new Color(0.05f, 0.6f, 0.64f));
            gold = Material("Current", new Color(1f, 0.68f, 0.2f));
            floor = Material("Ground", new Color(0.018f, 0.042f, 0.055f));
            line = Material("Path", new Color(0.11f, 0.32f, 0.37f));
            view.idleMaterial = stone; view.visitedMaterial = cyan; view.activeMaterial = gold;
            var ground = Primitive("Clearing", PrimitiveType.Cylinder, root.transform, new Vector3(0,-0.08f,3), new Vector3(13,0.06f,13), floor);
            var positions = new Dictionary<string, Vector3>();
            void Layout(string id, float x, int depth, float span)
            {
                positions.Add(id, new Vector3(x, 1.95f + depth * 0.90f, 3.8f + depth * 1.0f));
                if (tree.Nodes.Single(n => n.Id == id) is SplitNode split)
                { Layout(split.TrueChild, x - span, depth + 1, span * 0.5f); Layout(split.FalseChild, x + span, depth + 1, span * 0.5f); }
            }
            Layout(tree.RootId, 0, 0, 1.2f);
            var nodes = new List<TreeNodeView>();
            foreach (var node in tree.Nodes)
            {
                var platform = Primitive(node.Id, PrimitiveType.Cylinder, content, positions[node.Id], new Vector3(.60f,.035f,.60f), stone);
                var nv = platform.AddComponent<TreeNodeView>(); nv.nodeId = node.Id; nv.platform = platform.GetComponent<Renderer>();
                var anchor = new GameObject("DropAnchor").transform; anchor.SetParent(content, false); anchor.localPosition = positions[node.Id] + Vector3.up * .18f; nv.dropAnchor = anchor;
                var card = Canvas("NodeLabel-" + node.Id, content, positions[node.Id] + new Vector3(0,.39f,0), new Vector2(400,100), .003f);
                nv.title = Text("Condition", card.transform, node is SplitNode ? "Condition" : "Leaf", 32, new Vector2(0,10), new Vector2(390,55));
                nv.marker = Text("State", card.transform, "", 22, new Vector2(0,-30), new Vector2(380,25)); nv.marker.color = new Color(1,.8f,.4f);
                nodes.Add(nv);
            }
            foreach (var split in tree.Nodes.OfType<SplitNode>())
            foreach (var branch in new[] { true, false })
            {
                var child = branch ? split.TrueChild : split.FalseChild;
                var a = positions[split.Id]; var b = positions[child];
                var path = new GameObject(split.Id + (branch ? "-True" : "-False")); path.transform.SetParent(content, false);
                var lr = path.AddComponent<LineRenderer>(); lr.useWorldSpace = false; lr.positionCount = 2; lr.SetPositions(new[] { a, b }); lr.startWidth = lr.endWidth = .025f; lr.sharedMaterial = line;
                var label = Canvas("BranchMeaning", content, Vector3.Lerp(a,b,.54f) + new Vector3(branch ? -.3f : .3f, -.22f, 0), new Vector2(340,80), .0026f);
                var f = model.Features.Single(x => x.Id == split.FeatureId).DisplayName;
                Text("Meaning", label.transform, (branch ? "TRUE\n" : "FALSE\n") + f + (branch ? " < " : " >= ") + split.Condition.Threshold.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), 24, Vector2.zero, new Vector2(340,80));
            }
            view.nodeViews = nodes.ToArray();
            view.drop = Primitive("DataDrop", PrimitiveType.Sphere, content, positions[tree.RootId] + Vector3.up * .18f, Vector3.one * .11f, gold).transform;
            // Decorative silhouettes are outside the model layout and have no labels, controls, or model identities.
            for (var i=0; i<10; i++)
            {
                var angle = (i * 25 + 30) * Mathf.Deg2Rad;
                var p = new Vector3(Mathf.Cos(angle)*7, .8f, 5+Mathf.Sin(angle)*6);
                Primitive("DecorativeTrunk", PrimitiveType.Cylinder, root.transform, p, new Vector3(.14f,.8f,.14f), floor);
                Primitive("DecorativeCrown", PrimitiveType.Sphere, root.transform, p + Vector3.up*1.1f, new Vector3(1.1f,2,1.1f), stone);
            }
            BuildPanel(view, content);
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(PointableCanvasModule));
            var previewObject = new GameObject("DesktopPreviewCamera", typeof(Camera), typeof(AudioListener));
            previewObject.transform.position = new Vector3(0,1.6f,-.2f); previewObject.GetComponent<Camera>().fieldOfView=65;
            previewObject.GetComponent<Camera>().clearFlags=CameraClearFlags.SolidColor; previewObject.GetComponent<Camera>().backgroundColor=new Color(.016f,.027f,.055f);
            var preview = root.AddComponent<DesktopTreePreview>(); preview.trackedRig=rig; preview.previewCamera=previewObject.GetComponent<Camera>();
            preview.canvases = root.GetComponentsInChildren<Canvas>(); previewObject.SetActive(false);
            RenderSettings.ambientLight = new Color(.22f,.29f,.38f);
            RenderSettings.fog = true; RenderSettings.fogColor=new Color(.016f,.027f,.055f); RenderSettings.fogMode=FogMode.ExponentialSquared; RenderSettings.fogDensity=.025f;
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath,true) }.Concat(EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath)).ToArray();
            AssetDatabase.SaveAssets();
            return ScenePath;
        }

        private static void BuildPanel(OneTreeExperience view, Transform parent)
        {
            var panel = Canvas("LearningConsole", parent, new Vector3(0,.91f,2.55f), new Vector2(1000,600), .002f);
            panel.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var image=panel.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color=new Color(.02f,.05f,.075f,.98f); image.raycastTarget=false;
            view.status=Text("Status",panel.transform,"One tree • two ways to learn",27,new Vector2(0,264),new Vector2(960,44));
            view.explanation=Text("Explanation",panel.transform,"",24,new Vector2(0,185),new Vector2(960,110));
            view.score=Text("Score",panel.transform,"",22,new Vector2(0,80),new Vector2(960,92));
            var buttons=new List<TreeActionButton>();
            void Add(TreeAction action,string label,float x,float y,float w=150)
            {
                var go=new GameObject(action.ToString(),typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));
                go.transform.SetParent(panel.transform,false); var rt=(RectTransform)go.transform; rt.sizeDelta=new Vector2(w,52); rt.anchoredPosition=new Vector2(x,y);
                var bg=go.GetComponent<UnityEngine.UI.Image>(); bg.color=new Color(.09f,.22f,.28f); bg.raycastTarget=true;
                var button=go.GetComponent<UnityEngine.UI.Button>(); var colors=button.colors; colors.highlightedColor=new Color(.5f,1,1); colors.pressedColor=new Color(1,.8f,.4f); colors.disabledColor=new Color(.35f,.4f,.43f); button.colors=colors;
                button.navigation=new UnityEngine.UI.Navigation { mode=UnityEngine.UI.Navigation.Mode.None };
                var actionButton=go.AddComponent<TreeActionButton>(); actionButton.experience=view; actionButton.action=action;
                actionButton.label=Text("Label",go.transform,label,20,Vector2.zero,new Vector2(w-12,46)); buttons.Add(actionButton);
            }
            Add(TreeAction.TrueBranch,"TRUE",-240,4,460); Add(TreeAction.FalseBranch,"FALSE",240,4,460);
            Add(TreeAction.Back,"Back",-400,-61); Add(TreeAction.Step,"Step",-240,-61); Add(TreeAction.Play,"Play",-80,-61);
            Add(TreeAction.Pause,"Pause",80,-61); Add(TreeAction.Restart,"Restart",240,-61); Add(TreeAction.Overview,"Forest entry",400,-61);
            Add(TreeAction.Manual,"Explore branches",-368,-126,230); Add(TreeAction.Profile,"Follow profile",-124,-126,230);
            Add(TreeAction.NextProfile,"Next profile",120,-126,230); Add(TreeAction.Seated,"Seated layout",364,-126,230);
            view.feedback=Text("ProfileAndFeedback",panel.transform,"",19,new Vector2(0,-227),new Vector2(960,132));
            view.controls=buttons.ToArray();
            var path=AssetDatabase.GUIDToAssetPath("8369d93f7b6b99742bbea0649a41b7b1");
            var interaction=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),panel.transform);
            var rect=interaction.GetComponent<RectTransform>(); rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.sizeDelta=Vector2.zero; rect.anchoredPosition=Vector2.zero; rect.localScale=Vector3.one; rect.localRotation=Quaternion.identity;
            interaction.GetComponent<PointableCanvas>().InjectCanvas(panel);
        }
        private static Canvas Canvas(string name,Transform parent,Vector3 position,Vector2 size,float scale)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Canvas)); go.transform.SetParent(parent,false); go.transform.localPosition=position; go.transform.localScale=Vector3.one*scale;
            ((RectTransform)go.transform).sizeDelta=size; var canvas=go.GetComponent<Canvas>(); canvas.renderMode=RenderMode.WorldSpace; return canvas;
        }
        private static TMP_Text Text(string name,Transform parent,string value,int size,Vector2 position,Vector2 dimensions)
        {
            var go=new GameObject(name,typeof(RectTransform)); go.transform.SetParent(parent,false); var rt=(RectTransform)go.transform; rt.anchoredPosition=position; rt.sizeDelta=dimensions;
            var text=go.AddComponent<TextMeshProUGUI>(); text.font=font; text.text=value; text.fontSize=size; text.alignment=TextAlignmentOptions.Center; text.color=new Color(.9f,.97f,1); text.raycastTarget=false; text.richText=false;
            return text;
        }
        private static GameObject Primitive(string name,PrimitiveType type,Transform parent,Vector3 position,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(type); go.name=name; go.transform.SetParent(parent,false); go.transform.localPosition=position; go.transform.localScale=scale; go.GetComponent<Renderer>().sharedMaterial=material;
            var collider=go.GetComponent<Collider>(); if(collider!=null) UnityEngine.Object.DestroyImmediate(collider); return go;
        }
        private static Material Material(string name,Color color)
        {
            var shader=Shader.Find("Universal Render Pipeline/Unlit"); if(shader==null) throw new InvalidOperationException("URP Unlit shader is unavailable.");
            var material=new Material(shader); material.SetColor("_BaseColor",color); AssetDatabase.CreateAsset(material,AssetsRoot+"/Materials/OneTree/"+name+".mat"); return material;
        }
        private static void EnsureFolder(string path)
        {
            if(AssetDatabase.IsValidFolder(path)) return;
            var parent=Path.GetDirectoryName(path).Replace('\\','/'); EnsureFolder(parent); AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
        }
    }
}
