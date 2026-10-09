using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;
using VRExperienceAGB.Import;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Presentation
{
    public sealed class JsonLibraryController : MonoBehaviour
    {
        public ModelFileLibrary Storage { get; private set; }
        private volatile bool busy;
        public bool Busy { get=>busy;private set=>busy=value; }
        private LocalJsonUploadServer upload;
        private float expiresAt;
        private int queued;
        private readonly ConcurrentQueue<PickResult> uploads=new ConcurrentQueue<PickResult>();
        public string UploadAddress => upload?.Address;
        public string UploadCode => upload?.Token;
        public bool PickerUnavailable { get; private set; }
        public string Status { get; private set; }="Import a model, then compatible profile JSON files.";
        public string[] ListedFiles { get; private set; }=Array.Empty<string>();
        private ExperienceDirector director;
        private bool pickingProfiles;
        private string callbackObject;
        [Serializable] private class PickResult { public string path, name, error;public bool profiles; }
        private sealed class Loaded { public ImportedModel model;public ProfileSet profiles;public string name;public int skipped; }
        public void Configure(ExperienceDirector owner,string root=null)
        {
            director=owner;Storage=new ModelFileLibrary(root??UnityEngine.Application.persistentDataPath);
            callbackObject=gameObject.name;
        }
        public void List(bool profiles) { ListedFiles=Storage.Files(profiles); }
        public void Pick(bool profiles)
        {
            if(Busy)return;
            pickingProfiles=profiles;Busy=true;Status="Choose a JSON file in the system file picker.";
#if UNITY_EDITOR
            string path=UnityEditor.EditorUtility.OpenFilePanel(profiles?"Import profiles":"Import model","","json");
            Busy=false;if(string.IsNullOrEmpty(path)){Status="Import cancelled.";return;}
            ImportPath(path,profiles,true,Path.GetFileName(path));
#elif UNITY_ANDROID
            try { using(var bridge=new AndroidJavaClass("com.vrexperienceagb.importer.JsonFilePicker")) {
                if(!bridge.CallStatic<bool>("available")){Busy=false;PickerUnavailable=true;StartUpload();return;}
                bridge.CallStatic("open",callbackObject);
            } }
            catch(Exception){Busy=false;Status="File picker unavailable. Copy JSON into Models / Profiles and use Refresh.";}
#else
            Busy=false;Status="Copy JSON into Models / Profiles and use Refresh.";
#endif
        }
        public void StartUpload()
        {
            if(upload!=null)return;
            try {
                upload=new LocalJsonUploadServer(UnityEngine.Application.temporaryCachePath,(path,name,profiles)=>{
                    if(Interlocked.Increment(ref queued)>8){Interlocked.Decrement(ref queued);return false;}
                    uploads.Enqueue(new PickResult{path=path,name=name,profiles=profiles});return true;
                },()=> Volatile.Read(ref queued)>0?"Received files: "+Volatile.Read(ref queued)+". Put on the headset to validate and open them.":(Busy?"Import in progress. ":"")+Status,Path.Combine(UnityEngine.Application.persistentDataPath,"Screenshots"));
                expiresAt=Time.realtimeSinceStartup+900;Status="Open the address on a computer on the same Wi-Fi. Enter the code, then upload model / profile JSON.";
            }catch(Exception){StopUpload();Status="Could not start Wi-Fi upload. Check the Wi-Fi connection or copy files by USB.";}
        }
        public void StopUpload(){upload?.Dispose();upload=null;}
        private void Update()
        {
            if(upload!=null && Time.realtimeSinceStartup>expiresAt){StopUpload();Status="Wi-Fi upload session expired.";if(director.MenuPage=="upload")director.OpenMenu("upload");}
            if(!Busy && uploads.TryDequeue(out var item)){
                ImportPath(item.path,item.profiles,true,item.name,true);Interlocked.Decrement(ref queued);
            }
        }
        private void OnDestroy()
        {
            StopUpload();while(uploads.TryDequeue(out var item))try{File.Delete(item.path);}catch(IOException){}
        }
        [Preserve] public void OnJsonFilePicked(string payload)
        {
            if(!Busy)return;
            var picked=JsonUtility.FromJson<PickResult>(payload);Busy=false;
            if(!string.IsNullOrEmpty(picked.error)){Status=picked.error;director.OpenMenu(pickingProfiles?"profile-files":"models");return;}
            ImportPath(picked.path,pickingProfiles,true,picked.name,true);
        }
        public void ImportPath(string path,bool profiles,bool copy,string name=null,bool removeTemporary=false)
        {
            if(Busy)return;
            Busy=true;Status="Reading and validating JSON…";
            director.OpenMenu(profiles?"profile-files":"models");
            StartCoroutine(Load(path,profiles,copy,name??Path.GetFileName(path),removeTemporary));
        }
        private IEnumerator Load(string path,bool profileFile,bool copy,string name,bool removeTemporary)
        {
            var selectedModel=director.View.Model;
            var task=Task.Run(()=>{
                string json=ModelFileLibrary.Read(path);
                var loaded=new Loaded{name=name};
                if(profileFile)ModelFileLibrary.ParseProfiles(json,selectedModel);
                else loaded.model=ModelFileLibrary.ParseModel(json,name);
                if(copy)Storage.Save(profileFile,name,json);
                loaded.profiles=Storage.CompatibleProfiles(profileFile?selectedModel:loaded.model.Model,out loaded.skipped);
                return loaded;
            });
            while(!task.IsCompleted)yield return null;
            if(removeTemporary && Path.GetFullPath(path).StartsWith(Path.GetFullPath(UnityEngine.Application.temporaryCachePath)+Path.DirectorySeparatorChar,StringComparison.Ordinal))
                try{File.Delete(path);}catch(IOException){}
            Busy=false;
            if(task.IsFaulted){
                var error=task.Exception.GetBaseException();
                Status=error is InvalidDataException?error.Message:"Could not read or save the JSON file. The current model is unchanged.";
            }else if(director.View.Model!=selectedModel)Status="Selection changed. Open the imported file again.";
            else {
                var loaded=task.Result;
                if(profileFile)director.View.UseImportedProfiles(loaded.profiles);
                else director.View.OpenImportedModel(loaded.model,loaded.profiles,loaded.name);
                Status=(profileFile?"Profiles loaded: "+loaded.profiles.Profiles.Count:loaded.name+" · "+loaded.model.Model.Trees.Count+" trees · "+(loaded.model.Model.StructureOnlyPreview?"structure only":"scoring enabled"))+
                    (loaded.skipped==0?"":" · "+loaded.skipped+" incompatible/invalid profile files skipped");
            }
            director.OpenMenu(profileFile?"profile-files":"models");
        }
    }
}
