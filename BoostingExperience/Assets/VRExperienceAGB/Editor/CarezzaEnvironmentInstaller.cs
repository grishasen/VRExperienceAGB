using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRExperienceAGB.Presentation;

namespace VRExperienceAGB.Editor
{
    /// <summary>Imports the selected artwork and serializes scene references through the Editor.</summary>
    public static class CarezzaEnvironmentInstaller
    {
        private const string Folder = "Assets/VRExperienceAGB/Art/Carezza/";
        [MenuItem("VRExperienceAGB/Apply Carezza Environment")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before applying scene assets.");
            AssetDatabase.Refresh();
            var panorama = Import("LatemarPanorama.png",4096);
            var spruce = Import("AlpineSpruce.png",2048);
            var ground = Import("AlpineGround.png",1024);
            var groundImporter=(TextureImporter)AssetImporter.GetAtPath(Folder+"AlpineGround.png");
            groundImporter.wrapMode=TextureWrapMode.Repeat;groundImporter.SaveAndReimport();
            var backdrop = Material("LatemarBackdrop","VRExperienceAGB/CarezzaScenery",panorama,new Color(.72f,.76f,.82f));
            var trees = Material("AlpineSpruce","VRExperienceAGB/CarezzaScenery",spruce,new Color(.8f,.88f,.95f));
            var water = Material("CarezzaWater","VRExperienceAGB/CarezzaWater",panorama,new Color(.022f,.055f,.072f));
            var shore = Material("AlpineGround","VRExperienceAGB/CarezzaGround",ground,new Color(.34f,.40f,.48f));
            var rock = Material("ShoreRock","VRExperienceAGB/CarezzaGround",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/VRExperienceAGB/Art/Moonlit/SlateAlbedo.png"),new Color(.46f,.51f,.6f));
            const string path = "Assets/VRExperienceAGB/Scenes/OneTreeLearning.unity";
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
            if (!scene.isLoaded)
            {
                var current = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if (current.isDirty) throw new InvalidOperationException("Save the current scene before applying Carezza.");
                scene = EditorSceneManager.OpenScene(path);
            }
            foreach (var root in scene.GetRootGameObjects())
                foreach (var garden in root.GetComponentsInChildren<ForestGardenView>(true))
                {
                    Undo.RecordObject(garden,"Apply Carezza scenery");
                    garden.carezzaBackdropMaterial=backdrop;
                    garden.carezzaSpruceMaterial=trees;
                    garden.carezzaWaterMaterial=water;
                    garden.carezzaGroundMaterial=shore;
                    garden.carezzaRockMaterial=rock;
                    garden.sceneryPineBranchesMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/VRExperienceAGB/Art/Moonlit/ForegroundPineBranches.asset");
                    garden.sceneryPineTrunkMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/VRExperienceAGB/Art/Moonlit/ForegroundPineTrunk.asset");
                    EditorUtility.SetDirty(garden);
                }
            foreach(var root in scene.GetRootGameObjects())
                foreach(var camera in root.GetComponentsInChildren<Camera>(true))
                {
                    Undo.RecordObject(camera,"Include distant Carezza scenery");
                    // 400 m contains the ridge from the garden while avoiding the observed
                    // 500 m RectTransformUtility screen-ray precision regression on small keys.
                    camera.farClipPlane=400;
                    EditorUtility.SetDirty(camera);
                }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        private static Texture2D Import(string name,int size)
        {
            var path=Folder+name;
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Default;
            importer.sRGBTexture=true;importer.alphaSource=TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=true;importer.isReadable=false;
            importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Trilinear;
            importer.maxTextureSize=size;importer.npotScale=TextureImporterNPOTScale.None;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings {
                name="Android",overridden=true,maxTextureSize=size,format=TextureImporterFormat.ASTC_6x6,compressionQuality=80
            });
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Material Material(string name,string shader,Texture2D texture,Color color)
        {
            var path=Folder+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            var found=Shader.Find(shader);
            if(found==null)throw new InvalidOperationException("Missing shader: "+shader);
            if(material==null){material=new Material(found);AssetDatabase.CreateAsset(material,path);}
            material.shader=found;material.mainTexture=texture;material.color=color;material.enableInstancing=true;
            EditorUtility.SetDirty(material);return material;
        }
    }
}
