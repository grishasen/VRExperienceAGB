using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRExperienceAGB.Presentation;

namespace VRExperienceAGB.Editor
{
    /// <summary>Shared, reproducible forest detail assets. Keeps teaching metrics and interaction geometry unchanged.</summary>
    public static class ForestDetailInstaller
    {
        private const string Folder="Assets/VRExperienceAGB/Art/ForestDetail/";
        [MenuItem("VRExperienceAGB/Apply Forest Detail")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
            var panorama=Import("LatemarDetail.png",true,4096,2,false);
            var bark=Material("SpruceBark","VRExperienceAGB/TexturedBark",Import("SpruceBark.png",false,1024,4,true),new Color(.43f,.46f,.51f));
            var grass=Material("AlpineGrass","VRExperienceAGB/SpruceBranch",Import("AlpineGrass.png",true,512,2,false),new Color(.40f,.52f,.45f));
            var branch=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/VRExperienceAGB/Art/NearScenery/SpruceBranch.png");
            var model=Material("ModelNeedles","VRExperienceAGB/SpruceBranch",branch,new Color(.62f,.80f,.66f));
            var spruce=Material("DistantSpruce","VRExperienceAGB/SpruceBranch",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/VRExperienceAGB/Art/Carezza/AlpineSpruce.png"),new Color(.80f,.88f,.95f));
            var trunkHigh=SaveMesh("TrunkHigh",Trunk(12,5));var trunkLow=SaveMesh("TrunkLow",Trunk(6,2));
            var modelHigh=SaveMesh("ModelFirHigh",NearSceneryInstaller.Fir(10,9,true));
            // Retain most branch layers and the same radial directions in the distant silhouette.
            var modelLow=SaveMesh("ModelFirLow",NearSceneryInstaller.Fir(8,9,false));
            var tuft=SaveMesh("GrassTuft",Grass());
            const string path="Assets/VRExperienceAGB/Scenes/OneTreeLearning.unity";
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
            if(!scene.isLoaded) {
                if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save the active scene first.");
                scene=EditorSceneManager.OpenScene(path);
            }
            foreach(var root in scene.GetRootGameObjects())
            foreach(var garden in root.GetComponentsInChildren<ForestGardenView>(true)) {
                Undo.RecordObject(garden,"Complete forest detail");
                garden.detailedTrunkMesh=trunkHigh;garden.detailedTrunkLowMesh=trunkLow;garden.detailedBarkMaterial=bark;
                garden.detailedFirLowMesh=modelLow;
                garden.sceneryPineTrunkMesh=trunkHigh;garden.pineMesh=modelHigh;garden.modelPineLowMesh=modelLow;garden.pineMaterial=model;garden.barkMaterial=bark;
                garden.grassMesh=tuft;garden.grassMaterial=grass;garden.clearingFernMesh=tuft;garden.carezzaSpruceMaterial=spruce;
                garden.carezzaBackdropMaterial=CopyMaterial("LatemarBackdrop",garden.carezzaBackdropMaterial,panorama);
                garden.carezzaWaterMaterial=CopyMaterial("LatemarReflection",garden.carezzaWaterMaterial,panorama);
                var clearing=garden.transform.Find("MoonlitEnvironment");
                if(clearing!=null)foreach(Transform item in clearing) {
                    if(item.name!="GroundFern")continue;
                    var filter=item.GetComponent<MeshFilter>();var renderer=item.GetComponent<Renderer>();
                    Undo.RecordObject(filter,"Refine grass silhouette");Undo.RecordObject(renderer,"Refine grass surface");
                    filter.sharedMesh=tuft;renderer.sharedMaterial=grass;
                }
                EditorUtility.SetDirty(garden);
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }

        private static Texture2D Import(string name,bool alpha,int size,int anisotropy,bool repeat)
        {
            string path=Folder+name;AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.isReadable=false;
            importer.alphaSource=alpha?TextureImporterAlphaSource.FromInput:TextureImporterAlphaSource.None;importer.alphaIsTransparency=alpha;
            importer.mipmapEnabled=true;importer.mipMapsPreserveCoverage=alpha;importer.alphaTestReferenceValue=.42f;
            importer.wrapMode=repeat?TextureWrapMode.Repeat:TextureWrapMode.Clamp;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=anisotropy;
            importer.maxTextureSize=size;importer.npotScale=TextureImporterNPOTScale.ToNearest;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings {name="Android",overridden=true,maxTextureSize=size,format=TextureImporterFormat.ASTC_6x6,compressionQuality=80});
            importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        private static Material Material(string name,string shader,Texture2D texture,Color color)
        {
            var found=Shader.Find(shader);if(found==null||texture==null)throw new InvalidOperationException("Missing forest artwork: "+name);
            string path=Folder+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(found);AssetDatabase.CreateAsset(material,path);}
            material.shader=found;material.mainTexture=texture;material.color=color;material.enableInstancing=true;
            if(material.HasProperty("_Cutoff"))material.SetFloat("_Cutoff",.42f);
            EditorUtility.SetDirty(material);return material;
        }
        private static Material CopyMaterial(string name,Material source,Texture2D texture)
        {
            string path=Folder+name+".mat";var result=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(result==null){result=new Material(source);AssetDatabase.CreateAsset(result,path);}
            result.mainTexture=texture;EditorUtility.SetDirty(result);return result;
        }
        private static Mesh SaveMesh(string name,Mesh generated)
        {
            generated.name=name;string path=Folder+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing==null){AssetDatabase.CreateAsset(generated,path);return generated;}
            EditorUtility.CopySerialized(generated,existing);UnityEngine.Object.DestroyImmediate(generated);EditorUtility.SetDirty(existing);return existing;
        }
        private static Mesh Trunk(int sides,int layers)
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();
            for(int level=0;level<=layers;level++)for(int side=0;side<=sides;side++) {
                float t=level/(float)layers,angle=side/(float)sides*Mathf.PI*2;
                float radius=Mathf.Lerp(.028f,.003f,t)+(level==0?.015f:0);
                vertices.Add(new Vector3(Mathf.Cos(angle)*radius+.008f*Mathf.Sin(t*3),t,Mathf.Sin(angle)*radius));uv.Add(new Vector2(side/(float)sides,t));
                if(level==layers||side==sides)continue;int a=level*(sides+1)+side,b=a+sides+1;
                indices.AddRange(new[]{a,b,a+1,a+1,b,b+1});
            }
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        private static Mesh Grass()
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();var colors=new List<Color>();
            for(int i=0;i<3;i++) {
                int n=vertices.Count;var rotation=Quaternion.Euler(0,i*60,0);
                foreach(var v in new[]{new Vector3(-.5f,-.035f,0),new Vector3(.5f,-.035f,0),new Vector3(.5f,.965f,0),new Vector3(-.5f,.965f,0)})vertices.Add(rotation*v);
                uv.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});colors.AddRange(new[]{Color.white,Color.white,Color.white,Color.white});
                indices.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});
            }
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
}
