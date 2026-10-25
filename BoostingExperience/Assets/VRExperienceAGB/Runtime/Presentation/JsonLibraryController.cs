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
        public string Status { get; private set; }="Import a ZIP package, or a model followed by profile JSON files.";
        public string[] ListedFiles { get; private set; }=Array.Empty<string>();
        private ExperienceDirector director;
        private ModelDefinition libraryModel;
        private string modelPath;
        public string ActiveModelPath => director.View.Model==libraryModel?modelPath:null;
        private bool pickingProfiles;
        private string callbackObject;
        [Serializable] private class PickResult { public string path, name, error;public bool profiles; }
        private sealed class Loaded { public ImportedModel model;public ProfileSet profiles;public string name,path;public int skipped; }
        public void Configure(ExperienceDirector owner,string root=null)
        {
            director=owner;Storage=new ModelFileLibrary(root??UnityEngine.Application.persistentDataPath);
            libraryModel=null;modelPath=null;
            callbackObject=gameObject.name;
            if(root==null)SeedBundledFiles();
        }
        public void SeedBundledFiles()
        {
            try {
                foreach(bool profiles in new[]{false,true})
                    foreach(var file in Resources.LoadAll<TextAsset>("LocalLibrary/"+(profiles?"Profiles":"Models")))
                        Storage.Save(profiles,file.name+".json",file.text);
            }
            catch(Exception error)when(error is IOException || error is UnauthorizedAccessException) {
                Status="Could not add bundled JSON files to the local library. Check available storage and reopen the app.";
            }
        }
        public void List(bool profiles) { ListedFiles=Storage.Files(profiles,ActiveModelPath); }
        public void Pick(bool profiles)
        {
            if(Busy)return;
            pickingProfiles=profiles;Busy=true;Status="Choose a ZIP package or JSON file in the system file picker.";
#if UNITY_EDITOR
            string path=UnityEditor.EditorUtility.OpenFilePanelWithFilters(profiles?"Import profiles or package":"Import model or package","",profiles?new[]{"ZIP / JSON / JSONL","zip,json,jsonl"}:new[]{"ZIP / JSON","zip,json"});
            Busy=false;if(string.IsNullOrEmpty(path)){Status="Import cancelled.";return;}
            ImportPath(path,profiles,true,Path.GetFileName(path));
#elif UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
            try {
#if UNITY_STANDALONE_WIN
                string path=WindowsFilePicker.Pick(profiles);
#else
                string path=MacFilePicker.Pick(profiles);
#endif
                Busy=false;
                if(string.IsNullOrEmpty(path)){Status="Import cancelled.";return;}
                ImportPath(path,profiles,true,Path.GetFileName(path));
            }
            catch(Exception error) when(error is System.DllNotFoundException || error is System.EntryPointNotFoundException || error is IOException) {
                Busy=false;Status="File picker unavailable. Copy files into Models / Profiles and use Refresh.";
                Debug.LogException(error);
            }
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
                expiresAt=Time.realtimeSinceStartup+900;Status="Open the address on a computer on the same Wi-Fi. Enter the code, then upload a ZIP package or model / profile JSON.";
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
            name=name??Path.GetFileName(path);
            if(string.Equals(Path.GetExtension(name),".zip",StringComparison.OrdinalIgnoreCase))profiles=false;
            Busy=true;Status="Reading and validating imported files…";
            director.OpenMenu(profiles?"profile-files":"models");
            StartCoroutine(Load(path,profiles,copy,name,removeTemporary));
        }
        private IEnumerator Load(string path,bool profileFile,bool copy,string name,bool removeTemporary)
        {
            var selectedModel=director.View.Model;
            string selectedPath=ActiveModelPath;
            var task=Task.Run(()=>{
                if(string.Equals(Path.GetExtension(name),".zip",StringComparison.OrdinalIgnoreCase)) {
                    string packageModel=Storage.ImportPackage(path,name);
                    var package=new Loaded{path=packageModel,name=Storage.Label(packageModel),model=ModelFileLibrary.ParseModel(ModelFileLibrary.Read(packageModel),packageModel)};
                    package.profiles=Storage.CompatibleProfiles(package.model.Model,out package.skipped,packageModel);
                    if(package.skipped>0)throw new InvalidDataException("Saved ZIP package contains invalid profiles. The current model is unchanged.");
                    return package;
                }
                string json=ModelFileLibrary.Read(path);
                var loaded=new Loaded{name=copy?name:Storage.Label(path),path=profileFile?selectedPath:path};
                if(profileFile) {
                    Storage.CheckProfileScope(path,selectedPath);
                    var incoming=ModelFileLibrary.ParseProfiles(json,selectedModel,Storage.PackageFor(selectedPath)!=null);
                    var check=VRExperienceAGB.Application.ModelActivation.Prepare(selectedModel,incoming);
                    if(!check.IsSuccess)throw new InvalidDataException(string.Join("; ",System.Linq.Enumerable.Select(check.Diagnostics,d=>d.Code+": "+d.Message)));
                } else {
                    loaded.model=ModelFileLibrary.ParseModel(json,name);
                    var check=VRExperienceAGB.Application.ModelActivation.Prepare(loaded.model.Model,new ProfileSet(1,loaded.model.Model.Id,Array.Empty<PreparedProfile>()));
                    if(!check.IsSuccess)throw new InvalidDataException(string.Join("; ",System.Linq.Enumerable.Select(check.Diagnostics,d=>d.Code+": "+d.Message)));
                }
                // Validate already stored profiles against the candidate before writing a new file.
                var compatible=Storage.CompatibleProfiles(profileFile?selectedModel:loaded.model.Model,out _,loaded.path);
                var prepared=VRExperienceAGB.Application.ModelActivation.Prepare(profileFile?selectedModel:loaded.model.Model,compatible);
                if(!prepared.IsSuccess)throw new InvalidDataException("Stored profiles cannot be evaluated: "+string.Join("; ",System.Linq.Enumerable.Select(prepared.Diagnostics,d=>d.Code)));
                if(copy) {
                    if(Storage.PackageFor(path)!=null)throw new InvalidDataException("Open package files from Saved models / profiles without copying them into the loose library.");
                    string saved=Storage.Save(profileFile,name,json);
                    if(!profileFile)loaded.path=saved;
                }
                loaded.profiles=Storage.CompatibleProfiles(profileFile?selectedModel:loaded.model.Model,out loaded.skipped,loaded.path);
                return loaded;
            });
            while(!task.IsCompleted)yield return null;
            if(removeTemporary && Path.GetFullPath(path).StartsWith(Path.GetFullPath(UnityEngine.Application.temporaryCachePath)+Path.DirectorySeparatorChar,StringComparison.Ordinal))
                try{File.Delete(path);}catch(IOException){}
            Busy=false;
            if(task.IsFaulted){
                var error=task.Exception.GetBaseException();
                Status=error is InvalidDataException?error.Message:"Could not read or save the imported file. The current model is unchanged.";
            }else if(director.View.Model!=selectedModel)Status="Selection changed. Open the imported file again.";
            else {
                var loaded=task.Result;
                var activated=profileFile?director.View.UseImportedProfiles(loaded.profiles):director.View.OpenImportedModel(loaded.model,loaded.profiles,loaded.name);
                if(activated.IsSuccess && !profileFile){libraryModel=director.View.Model;modelPath=loaded.path;}
                Status=!activated.IsSuccess?"Import rejected; current route preserved. "+string.Join("; ",System.Linq.Enumerable.Select(activated.Diagnostics,d=>d.Code)) :(profileFile?"Profiles loaded: "+loaded.profiles.Profiles.Count:loaded.name+" · "+loaded.model.Model.Trees.Count+" trees · "+(loaded.model.Model.StructureOnlyPreview?"structure only":loaded.model.Model.ExportInfo!=null?"tree calculation enabled · CDH reconciliation deferred":"calculation enabled · source scoring unverified"))+
                    (loaded.skipped==0?"":" · "+loaded.skipped+" incompatible/invalid profile files skipped");
            }
            director.OpenMenu(profileFile?"profile-files":"models");
        }
    }
}
