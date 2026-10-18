using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Import
{
    public sealed class ImportedModel
    {
        public ModelDefinition Model { get; }
        public AgbPreviewResult Preview { get; }
        public ImportedModel(ModelDefinition model, AgbPreviewResult preview=null) { Model=model;Preview=preview; }
    }
    /// <summary>Local JSON storage independent of Unity and scene state.</summary>
    public sealed partial class ModelFileLibrary
    {
        public const int MaximumBytes=32*1024*1024;
        public string ModelsPath { get; }
        public string ProfilesPath { get; }
        public string PackagesPath { get; }
        public ModelFileLibrary(string root)
        {
            ModelsPath=Path.Combine(root,"Models");ProfilesPath=Path.Combine(root,"Profiles");
            PackagesPath=Path.Combine(root,"Packages");
            Directory.CreateDirectory(ModelsPath);Directory.CreateDirectory(ProfilesPath);
            Directory.CreateDirectory(PackagesPath);
        }
        public string[] Files(bool profiles, string modelPath=null)
        {
            string package=PackageFor(modelPath);
            if(profiles && package!=null)return JsonFiles(Path.Combine(package,"Profiles"),true,SearchOption.AllDirectories);
            string folder=profiles?ProfilesPath:ModelsPath;
            var files=JsonFiles(folder,profiles,SearchOption.TopDirectoryOnly).AsEnumerable();
            if(!profiles)files=files.Concat(Directory.GetDirectories(PackagesPath).Where(p=>!Path.GetFileName(p).StartsWith(".",StringComparison.Ordinal))
                .SelectMany(p=>JsonFiles(Path.Combine(p,"Models"),false,SearchOption.TopDirectoryOnly)));
            return files.OrderBy(Label,StringComparer.OrdinalIgnoreCase).ToArray();
        }
        private static string[] JsonFiles(string folder,bool profiles,SearchOption search) => Directory.Exists(folder)?Directory.GetFiles(folder,"*",search)
            .Where(p=>string.Equals(Path.GetExtension(p),".json",StringComparison.OrdinalIgnoreCase) || (profiles && string.Equals(Path.GetExtension(p),".jsonl",StringComparison.OrdinalIgnoreCase)))
            .OrderBy(p=>p,StringComparer.OrdinalIgnoreCase).ToArray():Array.Empty<string>();
        public static string Read(string path)
        {
            using(var stream=File.OpenRead(path)) {
                if(stream.Length>MaximumBytes)throw new InvalidDataException("JSON exceeds the 32 MiB import limit.");
                using(var reader=new StreamReader(stream,Encoding.UTF8,true))return reader.ReadToEnd();
            }
        }
        public static ImportedModel ParseModel(string json,string name)
        {
            var normalized=NormalizedModelJson.ReadModel(json);
            if(normalized.IsSuccess)return new ImportedModel(normalized.Value);
            var nested=AgbScoringImport.Read(json,name);
            if(nested.IsSuccess)return new ImportedModel(nested.Value.Model,nested.Value);
            throw new InvalidDataException("Unsupported or invalid model. "+Errors(normalized.Diagnostics)+" / "+Errors(nested.Diagnostics));
        }
        public static ProfileSet ParseProfiles(string json,ModelDefinition model,bool packageBinding=false)
        {
            if(model.StructureOnlyPreview)throw new InvalidDataException("This model supports structure viewing only. Profile playback requires a normalized scoring model.");
            var result=json != null && json.Contains("\"pxCommonInputs\"") && json.Contains("\"pxModelExecutionResults\"")
                ? CdhProfileJson.Read(json,model,packageBinding) : NormalizedModelJson.ReadProfiles(json,model,packageBinding);
            if(!result.IsSuccess)throw new InvalidDataException(Errors(result.Diagnostics));
            return result.Value;
        }
        private static string Errors(IEnumerable<Diagnostic> errors) => string.Join("; ",errors.Take(3).Select(d=>d.Code+": "+d.Message+(string.IsNullOrEmpty(d.Location)?"":" ("+d.Location+")")));
        public string Save(bool profiles,string name,string json)
        {
            var bytes=Encoding.UTF8.GetBytes(json);
            if(bytes.Length>MaximumBytes)throw new InvalidDataException("JSON exceeds the 32 MiB import limit.");
            var stem=Path.GetFileNameWithoutExtension(name??"import");
            stem=new string(stem.Where(c=>char.IsLetterOrDigit(c)||c=='-'||c=='_'||c=='.').ToArray());
            if(string.IsNullOrEmpty(stem))stem="import";
            if(Encoding.UTF8.GetByteCount(stem)>220)throw new InvalidDataException("The filename is too long to preserve model identity. Use a shorter model name.");
            string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").Substring(0,16).ToLowerInvariant();
            var path=Path.Combine(profiles?ProfilesPath:ModelsPath,stem+"-"+hash+".json");
            if(File.Exists(path))return path;
            var temporary=path+".tmp";
            try{File.WriteAllBytes(temporary,bytes);File.Move(temporary,path);}finally{if(File.Exists(temporary))File.Delete(temporary);}
            return path;
        }
        public static string DisplayName(string path)
        {
            string name=Path.GetFileNameWithoutExtension(path);
            int suffix=name.LastIndexOf('-');
            return suffix>=0 && name.Length-suffix==17 && name.Substring(suffix+1).All(Uri.IsHexDigit)?name.Substring(0,suffix):name;
        }
        public ProfileSet CompatibleProfiles(ModelDefinition model,out int skipped,string modelPath=null)
        {
            skipped=0;var items=new List<PreparedProfile>();
            string package=PackageFor(modelPath);
            foreach(var file in Files(true,modelPath))try {
                var set=ParseProfiles(Read(file),model,package!=null);
                if(set.Profiles.Any(p=>!ModelEvaluator.Evaluate(model,p).IsSuccess)){skipped++;continue;}
                string key=package==null?Path.GetFileNameWithoutExtension(file):file.Substring(package.Length+1);
                foreach(var p in set.Profiles)items.Add(new PreparedProfile(1,model.Id,key+":"+p.Id,Label(file)+" / "+p.DisplayName,p.Values,p.Source));
            }catch(Exception e)when(e is InvalidDataException || e is IOException || e is UnauthorizedAccessException){skipped++;}
            return new ProfileSet(1,model.Id,items);
        }
    }
}
