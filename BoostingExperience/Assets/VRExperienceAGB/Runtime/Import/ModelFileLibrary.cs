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
    public sealed class ModelFileLibrary
    {
        public const int MaximumBytes=32*1024*1024;
        public string ModelsPath { get; }
        public string ProfilesPath { get; }
        public ModelFileLibrary(string root)
        {
            ModelsPath=Path.Combine(root,"Models");ProfilesPath=Path.Combine(root,"Profiles");
            Directory.CreateDirectory(ModelsPath);Directory.CreateDirectory(ProfilesPath);
        }
        public string[] Files(bool profiles)
        {
            string folder=profiles?ProfilesPath:ModelsPath;
            return Directory.Exists(folder)?Directory.GetFiles(folder).Where(p=>string.Equals(Path.GetExtension(p),".json",StringComparison.OrdinalIgnoreCase)).OrderBy(Path.GetFileName,StringComparer.OrdinalIgnoreCase).ToArray():Array.Empty<string>();
        }
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
            var nested=AgbStructurePreview.Read(json,name);
            if(nested.IsSuccess)return new ImportedModel(nested.Value.Model,nested.Value);
            throw new InvalidDataException("Unsupported or invalid model. "+Errors(normalized.Diagnostics)+" / "+Errors(nested.Diagnostics));
        }
        public static ProfileSet ParseProfiles(string json,ModelDefinition model)
        {
            if(model.StructureOnlyPreview)throw new InvalidDataException("This model supports structure viewing only. Profile playback requires a normalized scoring model.");
            var result=NormalizedModelJson.ReadProfiles(json,model);
            if(!result.IsSuccess)throw new InvalidDataException(Errors(result.Diagnostics));
            return result.Value;
        }
        private static string Errors(IEnumerable<Diagnostic> errors) => string.Join("; ",errors.Take(3).Select(d=>d.Code+": "+d.Message));
        public string Save(bool profiles,string name,string json)
        {
            var bytes=Encoding.UTF8.GetBytes(json);
            if(bytes.Length>MaximumBytes)throw new InvalidDataException("JSON exceeds the 32 MiB import limit.");
            var stem=Path.GetFileNameWithoutExtension(name??"import");
            stem=new string(stem.Where(c=>char.IsLetterOrDigit(c)||c=='-'||c=='_').Take(60).ToArray());
            if(string.IsNullOrEmpty(stem))stem="import";
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
        public ProfileSet CompatibleProfiles(ModelDefinition model,out int skipped)
        {
            skipped=0;var items=new List<PreparedProfile>();
            foreach(var file in Files(true))try {
                var set=ParseProfiles(Read(file),model);string key=Path.GetFileNameWithoutExtension(file);
                foreach(var p in set.Profiles)items.Add(new PreparedProfile(1,model.Id,key+":"+p.Id,DisplayName(file)+" / "+p.DisplayName,p.Values));
            }catch(Exception e)when(e is InvalidDataException || e is IOException || e is UnauthorizedAccessException){skipped++;}
            return new ProfileSet(1,model.Id,items);
        }
    }
}
