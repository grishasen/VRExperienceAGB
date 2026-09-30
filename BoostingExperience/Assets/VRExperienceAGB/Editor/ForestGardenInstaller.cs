using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRExperienceAGB.Presentation;

namespace VRExperienceAGB.Editor
{
    public static class ForestGardenInstaller
    {
        public static string InstallSimpleNavigation()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
            var view=UnityEngine.Object.FindAnyObjectByType<OneTreeExperience>();
            if(view==null)throw new InvalidOperationException("Open the teaching scene first.");
            const string filename="export_Mobile_Click_Through_Rate_AGB_demo.json";
            string source=System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath,"../../data/examples/",filename));
            const string destination="Assets/VRExperienceAGB/Data/"+filename;
            var json=System.IO.File.ReadAllText(source);
            var result=VRExperienceAGB.Import.AgbStructurePreview.Read(json,"Mobile Click-Through Rate Demo");
            if(!result.IsSuccess)throw new InvalidOperationException("Demo import failed: "+string.Join("; ",System.Linq.Enumerable.Select(result.Diagnostics,d=>d.Message)));
            System.IO.File.WriteAllText(destination,json);AssetDatabase.ImportAsset(destination);
            Undo.RecordObject(view,"Set default demo model");
            view.modelFile=AssetDatabase.LoadAssetAtPath<TextAsset>(destination);
            var garden=view.GetComponent<ForestGardenView>();Undo.RecordObject(garden,"Simplify navigation");garden.simplifiedNavigation=true;
            EditorUtility.SetDirty(view);EditorUtility.SetDirty(garden);
            EditorSceneManager.MarkSceneDirty(view.gameObject.scene);EditorSceneManager.SaveScene(view.gameObject.scene);AssetDatabase.SaveAssets();
            return "Default demo installed: "+result.Value.Model.Trees.Count+" trees; simple M3/M4 navigation enabled.";
        }
        public static string Install()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
            var view=UnityEngine.Object.FindAnyObjectByType<OneTreeExperience>();
            if(view==null)throw new InvalidOperationException("Open the teaching scene first.");
            Undo.RegisterFullObjectHierarchyUndo(view.gameObject,"Install pine garden overview");
            var garden=view.GetComponent<ForestGardenView>() ?? view.gameObject.AddComponent<ForestGardenView>();
            const string path="Assets/VRExperienceAGB/Art/Moonlit/";
            garden.pineMesh=AssetDatabase.LoadAssetAtPath<Mesh>(path+"ForegroundPineBranches.asset");
            garden.trunkMesh=AssetDatabase.LoadAssetAtPath<Mesh>(path+"ForegroundPineTrunk.asset");
            garden.planterMesh=AssetDatabase.LoadAssetAtPath<Mesh>(path+"StonePlatform.asset");
            garden.ringMesh=AssetDatabase.LoadAssetAtPath<Mesh>(path+"SmoothLuminousRim.asset");
            var gardenMaterial=AssetDatabase.LoadAssetAtPath<Material>(path+"GardenEvergreen.mat");
            if(gardenMaterial==null)
            {
                gardenMaterial=new Material(AssetDatabase.LoadAssetAtPath<Material>(path+"PineFoliage.mat"));
                AssetDatabase.CreateAsset(gardenMaterial,path+"GardenEvergreen.mat");
            }
            gardenMaterial.color=new Color(.42f,.8f,.5f);gardenMaterial.enableInstancing=true;
            gardenMaterial.EnableKeyword("_EMISSION");gardenMaterial.SetColor("_EmissionColor",new Color(.01f,.045f,.016f));
            EditorUtility.SetDirty(gardenMaterial);garden.pineMaterial=gardenMaterial;
            garden.barkMaterial=AssetDatabase.LoadAssetAtPath<Material>(path+"DetailedBark.mat");
            garden.stoneMaterial=AssetDatabase.LoadAssetAtPath<Material>(path+"Slate.mat");
            garden.soilMaterial=AssetDatabase.LoadAssetAtPath<Material>(path+"Earth.mat");
            garden.pathMaterial=garden.stoneMaterial;
            garden.selectedMaterial=AssetDatabase.LoadAssetAtPath<Material>(path+"AmberLight.mat");
            garden.completedMaterial=AssetDatabase.LoadAssetAtPath<Material>(path+"CyanLight.mat");
            if(garden.pineMesh==null||garden.pineMaterial==null||garden.trunkMesh==null)throw new InvalidOperationException("Existing pine assets are missing.");
            // This is only the authored desktop preview angle; the headset camera remains tracked.
            view.GetComponent<DesktopTreePreview>().previewCamera.transform.rotation=Quaternion.Euler(-5,0,0);
            EditorUtility.SetDirty(garden);
            EditorSceneManager.MarkSceneDirty(view.gameObject.scene);EditorSceneManager.SaveScene(view.gameObject.scene);AssetDatabase.SaveAssets();
            return "Pine garden configured with existing art; teaching scene saved.";
        }
    }
}
