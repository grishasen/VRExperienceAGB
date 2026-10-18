using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using NUnit.Framework;
using VRExperienceAGB.Domain;
using VRExperienceAGB.Application;
using VRExperienceAGB.Import;

namespace VRExperienceAGB.Tests
{
    public class ModelFileLibraryTests
    {
        private string root;
        private ModelFileLibrary library;
        [SetUp] public void Open(){root=Path.Combine(Path.GetTempPath(),"vr-library-"+Guid.NewGuid().ToString("N"));library=new ModelFileLibrary(root);}
        [TearDown] public void Close(){Directory.Delete(root,true);}
        [Test] public void RoundTripUsesSeparateFoldersAndSafeStableNames()
        {
            var model=DeepTreeExample.Model();var json=NormalizedModelJson.WriteModel(model).Value;
            string file=library.Save(false,"../../my-model.json",json);
            Assert.That(Path.GetDirectoryName(file),Is.EqualTo(library.ModelsPath));
            Assert.That(library.Save(false,"../../my-model.json",json),Is.EqualTo(file));
            Assert.That(library.Files(false).Length,Is.EqualTo(1));Assert.That(library.Files(true),Is.Empty);
            Assert.That(ModelFileLibrary.ParseModel(ModelFileLibrary.Read(file),"Imported").Model.Trees.Count,Is.EqualTo(model.Trees.Count));
            var reopened=new ModelFileLibrary(root);Assert.That(reopened.Files(false).Single(),Is.EqualTo(file));
        }
        [Test] public void SeparateProfileFilesCanReuseIdsAndKeepIndependentValues()
        {
            var model=DeepTreeExample.Model();var original=DeepTreeExample.Profiles(model).Profiles[0];
            var alternate=original.WithValue("position",ProfileValue.FromNumber(255));
            library.Save(true,"A.json",NormalizedModelJson.WriteProfiles(new ProfileSet(1,model.Id,new[]{original}),model).Value);
            library.Save(true,"B.json",NormalizedModelJson.WriteProfiles(new ProfileSet(1,model.Id,new[]{alternate}),model).Value);
            var set=library.CompatibleProfiles(model,out int skipped);
            Assert.That(skipped,Is.Zero);Assert.That(set.Profiles.Count,Is.EqualTo(2));
            Assert.That(set.Profiles.Select(p=>p.Id).Distinct().Count(),Is.EqualTo(2));
            Assert.That(set.Profiles.Select(p=>p.GetValue("position").Number),Is.EquivalentTo(new[]{0d,255d}));
        }
        [Test] public void InvalidAndIncompatibleProfilesAreSkippedWithoutDiscardingValidOnes()
        {
            var model=DeepTreeExample.Model();string json=NormalizedModelJson.WriteProfiles(DeepTreeExample.Profiles(model),model).Value;
            library.Save(true,"valid.json",json);library.Save(true,"broken.json","{");
            library.Save(true,"other.json",json.Replace(model.Id,"unrelated-model"));
            Assert.That(library.CompatibleProfiles(model,out int skipped).Profiles.Count,Is.GreaterThan(0));Assert.That(skipped,Is.EqualTo(2));
            Assert.Throws<InvalidDataException>(()=>ModelFileLibrary.ParseModel("{}","bad"));
        }
        [Test] public void OversizedInputIsRejectedBeforeReadingItsContents()
        {
            var file=Path.Combine(root,"large.json");using(var stream=File.Create(file))stream.SetLength(ModelFileLibrary.MaximumBytes+1L);
            Assert.Throws<InvalidDataException>(()=>ModelFileLibrary.Read(file));
        }

        private string Zip(params (string name,string text)[] files)
        {
            string path=Path.Combine(root,Guid.NewGuid()+".zip");
            using(var archive=ZipFile.Open(path,ZipArchiveMode.Create))
                foreach(var file in files)using(var writer=new StreamWriter(archive.CreateEntry(file.name).Open()))writer.Write(file.text);
            return path;
        }
        private string ModelJson => NormalizedModelJson.WriteModel(DeepTreeExample.Model()).Value;
        private string ProfilesJson(double position=0)
        {
            var model=DeepTreeExample.Model();var profile=DeepTreeExample.Profiles(model).Profiles[0].WithValue("position",ProfileValue.FromNumber(position));
            return NormalizedModelJson.WriteProfiles(new ProfileSet(1,model.Id,new[]{profile}),model).Value;
        }
        [Test] public void ZipBindsByPackageAndIsolatesIdenticalModelsAcrossRestart()
        {
            var model=DeepTreeExample.Model();
            string first=library.ImportPackage(Zip(("model.json",ModelJson),("a/profile.json",ProfilesJson().Replace(model.Id,"old-model-id")),("b/profile.json",ProfilesJson(100))),"First.zip");
            string second=library.ImportPackage(Zip(("model.json",ModelJson),("profile.json",ProfilesJson(255))),"Second.zip");
            library.Save(true,"loose.json",ProfilesJson(45));
            library=new ModelFileLibrary(root);
            var a=library.CompatibleProfiles(model,out int skipped,first);
            Assert.That(skipped,Is.Zero);Assert.That(a.Profiles.Select(p=>p.GetValue("position").Number),Is.EquivalentTo(new[]{0d,100d}));
            Assert.That(a.Profiles.Select(p=>p.Id).Distinct().Count(),Is.EqualTo(2));
            var b=library.CompatibleProfiles(model,out skipped,second);
            Assert.That(b.Profiles.Single().GetValue("position").Number,Is.EqualTo(255));
            Assert.That(library.CompatibleProfiles(model,out skipped).Profiles.Single().GetValue("position").Number,Is.EqualTo(45));
            Assert.That(library.Files(false).Length,Is.EqualTo(2));
            Assert.Throws<InvalidDataException>(()=>library.CheckProfileScope(library.Files(true,first)[0],second));
            Assert.Throws<InvalidDataException>(()=>library.CheckProfileScope(library.Files(true)[0],first));
            Assert.Throws<InvalidDataException>(()=>library.CheckProfileScope(library.Files(true,first)[0],null));
        }
        [Test] public void ReimportIsIdempotentAndFinderMetadataIsIgnored()
        {
            string zip=Zip(("folder/model.json",ModelJson),("folder/profile.json",ProfilesJson()),("__MACOSX/._model.json","metadata"),(".DS_Store","metadata"));
            string first=library.ImportPackage(zip,"Demo.zip");
            Assert.That(library.ImportPackage(zip,"Demo.zip"),Is.EqualTo(first));
            Assert.That(library.Files(false).Length,Is.EqualTo(1));
            Assert.That(Directory.GetDirectories(library.PackagesPath).Length,Is.EqualTo(1));
        }
        [TestCase("../escape.json")][TestCase("/escape.json")][TestCase("a/../../escape.json")]
        [TestCase("a\\escape.json")][TestCase("C:/escape.json")][TestCase("a//escape.json")]
        public void UnsafeArchivePathsLeaveNoPublishedOrStagingFiles(string name)
        {
            Assert.Throws<InvalidDataException>(()=>library.ImportPackage(Zip(("model.json",ModelJson),(name,ProfilesJson())),"Unsafe.zip"));
            Assert.That(Directory.GetFileSystemEntries(library.PackagesPath),Is.Empty);
            Assert.That(File.Exists(Path.Combine(root,"escape.json")),Is.False);
        }
        [Test] public void InvalidPackageIsAtomicAndKeepsExistingPackages()
        {
            string accepted=library.ImportPackage(Zip(("model.json",ModelJson),("profile.json",ProfilesJson())),"Accepted.zip");
            string before=ModelFileLibrary.Read(accepted);
            foreach(var zip in new[]{
                Zip(("model.json",ModelJson)),
                Zip(("profile.json",ProfilesJson())),
                Zip(("model.jsonl",ModelJson),("profile.json",ProfilesJson())),
                Zip(("model.json",ModelJson),("second.json",ModelJson),("profile.json",ProfilesJson())),
                Zip(("model.json",ModelJson),("profile.json","{}")),
                Zip(("model.json",ModelJson),("profile.json",ProfilesJson().Replace("\"position\": 0","\"position\": null"))),
                Zip(("model.json",ModelJson),("profile.json",ProfilesJson()),("notes.txt","text")),
                Zip(("model.json",ModelJson),("profile.json",ProfilesJson()),("PROFILE.json",ProfilesJson()))})
                Assert.Throws<InvalidDataException>(()=>library.ImportPackage(zip,"Rejected.zip"));
            Assert.That(library.Files(false),Is.EqualTo(new[]{accepted}));
            Assert.That(Directory.GetDirectories(library.PackagesPath).Length,Is.EqualTo(1));
            Assert.That(ModelFileLibrary.Read(accepted),Is.EqualTo(before));
        }
        [Test] public void ArchiveLinksEntryCountAndExpandedSizeAreBounded()
        {
            string link=Zip(("model.json",ModelJson),("profile.json",ProfilesJson()));
            using(var archive=ZipFile.Open(link,ZipArchiveMode.Update))archive.GetEntry("profile.json").ExternalAttributes=unchecked((int)0xA1FF0000);
            Assert.Throws<InvalidDataException>(()=>library.ImportPackage(link,"Link.zip"));
            string many=Zip(Enumerable.Range(0,ModelFileLibrary.MaximumPackageEntries+1).Select(i=>(i+".json","{}")).ToArray());
            Assert.Throws<InvalidDataException>(()=>library.ImportPackage(many,"Many.zip"));
            string large=Zip();
            using(var archive=ZipFile.Open(large,ZipArchiveMode.Update))
            using(var stream=archive.CreateEntry("large.json",CompressionLevel.Optimal).Open())
            {
                byte[] block=new byte[1024*1024];
                for(int i=0;i<33;i++)stream.Write(block,0,block.Length);
            }
            Assert.Throws<InvalidDataException>(()=>library.ImportPackage(large,"Large.zip"));
            string expanded=Zip();
            using(var archive=ZipFile.Open(expanded,ZipArchiveMode.Update))
                for(int i=0;i<3;i++)using(var stream=archive.CreateEntry(i+".json",CompressionLevel.Optimal).Open()) {
                    byte[] block=new byte[1024*1024];for(int j=0;j<24;j++)stream.Write(block,0,block.Length);
                }
            Assert.Throws<InvalidDataException>(()=>library.ImportPackage(expanded,"Expanded.zip"));
            string broken=Path.Combine(root,"broken.zip");File.WriteAllText(broken,"not a ZIP");
            Assert.Throws<InvalidDataException>(()=>library.ImportPackage(broken,"Broken.zip"));
            Assert.That(Directory.GetFileSystemEntries(library.PackagesPath),Is.Empty);
        }
    }
}
