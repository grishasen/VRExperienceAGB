using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRExperienceAGB.Presentation;

namespace VRExperienceAGB.Editor
{
    /// <summary>Reproducible, bounded scenery assets; scene serialization is owned by Unity.</summary>
    public static class NearSceneryInstaller
    {
        private const string Folder = "Assets/VRExperienceAGB/Art/NearScenery/";

        [MenuItem("VRExperienceAGB/Apply Near Scenery Detail")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            var floor = Import("ForestFloor.png", false, 4);
            var slate = Import("LayeredSlate.png", false, 2);
            var branch = Import("SpruceBranch.png", true, 2);
            var ground = Material("ForestFloor", "VRExperienceAGB/ForestFloor", floor, new Color(.34f,.40f,.48f));
            ground.SetFloat("_Scale", .28f);
            var rock = Material("LayeredSlate", "VRExperienceAGB/CarezzaGround", slate, new Color(.46f,.51f,.60f));
            rock.SetFloat("_Scale", .7f);
            var needles = Material("SpruceBranch", "VRExperienceAGB/SpruceBranch", branch, new Color(.50f,.66f,.85f));
            needles.SetFloat("_Cutoff", .42f);
            var high = SaveMesh("FirHigh", Fir(11, 11, true));
            var medium = SaveMesh("FirMedium", Fir(9, 8, false));
            var low = SaveMesh("FirLow", Fir(6, 6, false));
            var rocks = Enumerable.Range(0,3).Select(i => SaveMesh("SlateShape"+i, Rock(71+i*13))).ToArray();
            const string path = "Assets/VRExperienceAGB/Scenes/OneTreeLearning.unity";
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
            if (!scene.isLoaded) {
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
                    throw new InvalidOperationException("Save the active scene first.");
                scene = EditorSceneManager.OpenScene(path);
            }
            foreach (var root in scene.GetRootGameObjects())
            foreach (var garden in root.GetComponentsInChildren<ForestGardenView>(true))
            {
                Undo.RecordObject(garden, "Improve nearby scenery");
                garden.carezzaGroundMaterial = ground;
                garden.carezzaRockMaterial = rock;
                garden.detailedFirMaterial = needles;
                garden.detailedFirHighMesh = high;
                garden.detailedFirMediumMesh = medium;
                garden.detailedFirLowMesh = low;
                garden.detailedRockMeshes = rocks;
                var clearing = garden.transform.Find("MoonlitEnvironment");
                if (clearing != null) {
                    var floorRenderer = clearing.Find("Ground")?.GetComponent<Renderer>();
                    if (floorRenderer != null) { Undo.RecordObject(floorRenderer,"Improve clearing floor"); floorRenderer.sharedMaterial=ground; }
                    var stones = clearing.Cast<Transform>().Where(t=>t.name=="SlateBoulder").OrderBy(t=>t.localPosition.sqrMagnitude).ToArray();
                    for (int i=0;i<stones.Length;i++) {
                        var renderer=stones[i].GetComponent<Renderer>();
                        Undo.RecordObject(renderer,"Improve slate surface"); renderer.sharedMaterial=rock;
                        if (i>=12) continue;
                        var filter=stones[i].GetComponent<MeshFilter>();
                        Undo.RecordObject(filter,"Refine nearby rock silhouette"); filter.sharedMesh=rocks[i%rocks.Length];
                    }
                }
                EditorUtility.SetDirty(garden);
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }

        private static Texture2D Import(string name, bool alpha, int anisotropy)
        {
            var path=Folder+name;
            AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Default; importer.sRGBTexture=true;
            importer.alphaSource=alpha?TextureImporterAlphaSource.FromInput:TextureImporterAlphaSource.None;
            importer.alphaIsTransparency=alpha; importer.mipmapEnabled=true; importer.isReadable=false;
            importer.mipMapsPreserveCoverage=alpha; importer.alphaTestReferenceValue=.42f;
            importer.wrapMode=alpha?TextureWrapMode.Clamp:TextureWrapMode.Repeat;
            importer.filterMode=FilterMode.Trilinear; importer.anisoLevel=anisotropy;
            importer.maxTextureSize=1024; importer.npotScale=TextureImporterNPOTScale.ToNearest;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings {
                name="Android",overridden=true,maxTextureSize=1024,format=TextureImporterFormat.ASTC_6x6,compressionQuality=80
            });
            importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Material Material(string name, string shader, Texture2D texture, Color tint)
        {
            var path=Folder+name+".mat";var found=Shader.Find(shader);
            if (found==null) throw new InvalidOperationException("Missing shader: "+shader);
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material==null) {material=new Material(found);AssetDatabase.CreateAsset(material,path);}
            material.shader=found;material.mainTexture=texture;material.color=tint;material.enableInstancing=true;
            EditorUtility.SetDirty(material);return material;
        }

        private static Mesh SaveMesh(string name, Mesh generated)
        {
            generated.name=name;var path=Folder+name+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing==null) {AssetDatabase.CreateAsset(generated,path);return generated;}
            EditorUtility.CopySerialized(generated,existing);UnityEngine.Object.DestroyImmediate(generated);
            EditorUtility.SetDirty(existing);return existing;
        }

        internal static Mesh Fir(int layers, int branches, bool bent)
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var indices=new List<int>();
            for (int layer=0;layer<layers;layer++)
            for (int branch=0;branch<branches;branch++)
            {
                float y=Mathf.Lerp(.34f,1.04f,layer/(float)(layers-1))+.018f*Mathf.Sin(branch*2.1f+layer);
                float angle=(branch*360f/branches+layer*137.5f)*Mathf.Deg2Rad;
                var radial=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                var side=new Vector3(-radial.z,.32f*Mathf.Sin(branch*1.7f+layer),radial.x);
                float radius=Mathf.Max(.035f,(1.08f-y)*.50f)*(.94f+.09f*Mathf.Sin(branch*3.7f+layer));
                var stem=Vector3.up*y;
                var middle=stem+radial*radius*.50f-Vector3.up*radius*.20f;
                var tip=stem+radial*radius-Vector3.up*radius*.65f;
                int n=vertices.Count;
                Add(stem-side*radius*.10f,new Vector2(0,0),.64f);
                Add(stem+side*radius*.10f,new Vector2(1,0),.64f);
                if (bent) {
                    Add(middle-side*radius*.43f,new Vector2(0,.5f),.86f);
                    Add(middle+side*radius*.43f,new Vector2(1,.5f),.86f);
                    Add(tip-side*radius*.31f,new Vector2(0,1),1);
                    Add(tip+side*radius*.31f,new Vector2(1,1),1);
                    indices.AddRange(new[]{n,n+2,n+1,n+1,n+2,n+3,n+2,n+4,n+3,n+3,n+4,n+5});
                } else {
                    Add(tip-side*radius*.43f,new Vector2(0,1),1);
                    Add(tip+side*radius*.43f,new Vector2(1,1),1);
                    indices.AddRange(new[]{n,n+2,n+1,n+1,n+2,n+3});
                }
            }
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(indices,0);
            mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
            void Add(Vector3 position,Vector2 texcoord,float brightness) {
                vertices.Add(position);uv.Add(texcoord);colors.Add(new Color(brightness,brightness,brightness,1));
            }
        }

        private static Mesh Rock(int seed)
        {
            var random=new System.Random(seed);var rings=new Vector3[5,10];
            for(int ring=0;ring<5;ring++) for(int side=0;side<10;side++) {
                float t=ring/4f,angle=(side/10f+ring*.023f)*Mathf.PI*2;
                float radius=Mathf.Sin(t*Mathf.PI)*(.43f+(float)random.NextDouble()*.10f);
                rings[ring,side]=new Vector3(Mathf.Cos(angle)*radius,t*.79f-.35f,Mathf.Sin(angle)*radius);
            }
            var vertices=new List<Vector3>();var indices=new List<int>();
            for(int ring=0;ring<4;ring++) for(int side=0;side<10;side++) {
                int next=(side+1)%10;
                if(ring>0) Triangle(rings[ring,side],rings[ring+1,side],rings[ring,next]);
                if(ring<3) Triangle(rings[ring,next],rings[ring+1,side],rings[ring+1,next]);
            }
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
            void Triangle(Vector3 a,Vector3 b,Vector3 c) {indices.Add(vertices.Count);indices.Add(vertices.Count+1);indices.Add(vertices.Count+2);vertices.Add(a);vertices.Add(b);vertices.Add(c);}
        }
    }
}
