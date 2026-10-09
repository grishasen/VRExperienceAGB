using System;
using System.IO;
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
    }
}
