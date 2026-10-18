using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRExperienceAGB.Domain;
using VRExperienceAGB.Application;
using VRExperienceAGB.Import;
using VRExperienceAGB.Presentation;
using Object=UnityEngine.Object;

namespace VRExperienceAGB.Tests
{
    public class JsonLibraryPlayModeTests
    {
        private string folder;
        private OneTreeExperience view;
        [UnitySetUp] public IEnumerator Open()
        {
            yield return SceneManager.LoadSceneAsync("OneTreeLearning");yield return null;
            view=Object.FindAnyObjectByType<OneTreeExperience>();Assert.That(view.Ready,Is.True);
            view.enabled=false;view.Director.enabled=false;
            folder=Path.Combine(Path.GetTempPath(),"vr-import-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
            view.Director.Library.Configure(view.Director,Path.Combine(folder,"Library"));
            view.SendMessage("OnApplicationFocus",true);view.SendMessage("OnApplicationPause",false);
        }
        [TearDown] public void Close(){if(folder!=null && Directory.Exists(folder))Directory.Delete(folder,true);}
        private IEnumerator WaitForImport()
        {
            int frames=0;while(view.Director.Library.Busy && frames++<600)yield return null;
            Assert.That(view.Director.Library.Busy,Is.False,"Import did not complete");
        }
        private string Input(string name,string json){string path=Path.Combine(folder,name);File.WriteAllText(path,json);return path;}
        [UnityTest] public IEnumerator ImportModelThenSeparateProfilesRunsCompletePairAndReopensFromDisk()
        {
            var model=DeepTreeExample.Model();var library=view.Director.Library;
            library.ImportPath(Input("model.json",NormalizedModelJson.WriteModel(model).Value),false,true);yield return WaitForImport();
            Assert.That(view.Model.Id,Is.EqualTo(model.Id));Assert.That(view.Garden.PlotCount,Is.EqualTo(model.Trees.Count));
            Assert.That(view.AvailableProfiles.Profiles,Is.Empty);
            var original=DeepTreeExample.Profiles(model).Profiles[0];var alternate=original.WithValue("position",ProfileValue.FromNumber(255));
            foreach(var item in new[]{original,alternate}){
                string name=item==original?"A.json":"B.json";
                library.ImportPath(Input(name,NormalizedModelJson.WriteProfiles(new ProfileSet(1,model.Id,new[]{item}),model).Value),true,true);yield return WaitForImport();
            }
            Assert.That(view.AvailableProfiles.Profiles.Count,Is.EqualTo(2));
            view.Director.StartPlayback(true);view.Director.Activate("calculate-all");
            Assert.That(view.Director.Playback.A.RawScore,Is.Not.EqualTo(view.Director.Playback.B.RawScore));
            Assert.That(view.Director.Playback.CompletedTrees,Is.EqualTo(model.Trees.Count));
            library.ImportPath(library.Storage.Files(false).Single(),false,false);yield return WaitForImport();
            Assert.That(view.AvailableProfiles.Profiles.Count,Is.EqualTo(2));Assert.That(view.Comparison,Is.Null);
            view.Director.Activate("table");Assert.That(view.Garden.M5.Diorama.Visible,Is.True);
        }
        [UnityTest] public IEnumerator NestedExportEnablesTreeCalculationButRequiresProfilesForPlayback()
        {
            string json=File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,"../../data/examples/demo-agb-export.json"));
            var library=view.Director.Library;library.ImportPath(Input("export.json",json),false,true);yield return WaitForImport();
            Assert.That(view.Model.StructureOnlyPreview,Is.False);Assert.That(view.Model.ExportInfo,Is.Not.Null);Assert.That(view.Garden.PlotCount,Is.EqualTo(3));
            view.Director.CloseMenu();view.Garden.Navigation.OpenTree(0);Assert.That(view.Session.State.Overview,Is.False);
            view.Director.StartPlayback(true);Assert.That(view.Director.PlayingTour,Is.False);
            var accepted=view.Model;library.ImportPath(Input("profiles.json","{}"),true,true);yield return WaitForImport();
            Assert.That(view.Model,Is.SameAs(accepted));Assert.That(library.Status,Does.Contain("RequiredField"));
        }
        [UnityTest] public IEnumerator BrowserUploadOpensValidatedModelThroughMainThreadQueue()
        {
            var library=view.Director.Library;library.StartUpload();Assert.That(library.UploadAddress,Is.Not.Null,library.Status);
            int port=new Uri(library.UploadAddress).Port;string code=library.UploadCode;
            var model=DeepTreeExample.Model();string body=NormalizedModelJson.WriteModel(model).Value;
            var task=System.Threading.Tasks.Task.Run(()=>{
                using(var client=new System.Net.Sockets.TcpClient("127.0.0.1",port))using(var stream=client.GetStream()){
                    client.ReceiveTimeout=5000;
                    byte[] content=System.Text.Encoding.UTF8.GetBytes(body);
                    byte[] header=System.Text.Encoding.ASCII.GetBytes("POST /upload?token="+code+"&kind=model&name=browser.json HTTP/1.1\r\nHost: localhost\r\nContent-Length: "+content.Length+"\r\n\r\n");
                    stream.Write(header,0,header.Length);stream.Write(content,0,content.Length);
                    using(var reader=new StreamReader(stream))return reader.ReadToEnd();
                }
            });
            int frames=0;while(!task.IsCompleted && frames++<600)yield return null;
            Assert.That(task.IsCompleted,Is.True);Assert.That(task.Result,Does.Contain("202"));
            frames=0;while(view.Model.Id!=model.Id && frames++<600)yield return null;
            Assert.That(view.Model.Id,Is.EqualTo(model.Id),library.Status);Assert.That(library.Storage.Files(false).Length,Is.EqualTo(1));
            library.StopUpload();Assert.That(library.UploadAddress,Is.Null);
        }
        [UnityTest] public IEnumerator InvalidImportKeepsAcceptedModelSessionAndFiles()
        {
            var model=view.Model;var session=view.Session;var library=view.Director.Library;
            library.ImportPath(Input("bad.json","{}"),false,true);yield return WaitForImport();
            Assert.That(view.Model,Is.SameAs(model));Assert.That(view.Session,Is.SameAs(session));Assert.That(library.Storage.Files(false),Is.Empty);
            library.ImportPath(Input("wrong.json","{\"schemaVersion\":1,\"modelId\":\"other\",\"profiles\":[]}"),true,true);yield return WaitForImport();
            Assert.That(view.Model,Is.SameAs(model));Assert.That(library.Storage.Files(true),Is.Empty);
            Assert.That(library.Status,Does.Contain("ModelMismatch"));
        }

        private string Package(string name,double value,bool invalid=false)
        {
            var model=DeepTreeExample.Model();var p=DeepTreeExample.Profiles(model).Profiles[0];
            string path=Path.Combine(folder,name);
            using(var zip=ZipFile.Open(path,ZipArchiveMode.Create)) {
                using(var writer=new StreamWriter(zip.CreateEntry("model.json").Open()))writer.Write(NormalizedModelJson.WriteModel(model).Value);
                using(var writer=new StreamWriter(zip.CreateEntry("profiles.json").Open()))writer.Write(invalid?"{}":NormalizedModelJson.WriteProfiles(
                    new ProfileSet(1,model.Id,new[]{p,new PreparedProfile(1,model.Id,"alternate","Alternate",p.WithValue("position",ProfileValue.FromNumber(value)).Values)}),model).Value);
            }
            return path;
        }
        [UnityTest] public IEnumerator PackagesKeepIndependentPairsAndRejectLooseInputsWithoutChangingPlayback()
        {
            var library=view.Director.Library;
            library.ImportPath(Package("First.zip",100),false,true);yield return WaitForImport();
            Assert.That(view.AvailableProfiles.Profiles.Count,Is.EqualTo(2),library.Status);
            string first=library.ActiveModelPath;
            view.Director.StartPlayback(true);view.Director.Activate("calculate-all");
            double score=view.Director.Playback.B.RawScore;
            Assert.That(view.Director.Playback.CompletedTrees,Is.EqualTo(view.Model.Trees.Count));
            var model=view.Model;var session=view.Session;var playback=view.Director.Playback;
            library.ImportPath(Package("Invalid.zip",255,true),false,true);yield return WaitForImport();
            Assert.That(view.Model,Is.SameAs(model));Assert.That(view.Session,Is.SameAs(session));Assert.That(view.Director.Playback,Is.SameAs(playback));
            var loose=NormalizedModelJson.WriteProfiles(DeepTreeExample.Profiles(model),model).Value;
            library.ImportPath(Input("loose.json",loose),true,true);yield return WaitForImport();
            Assert.That(library.Status,Does.Contain("same ZIP package"));Assert.That(view.Director.Playback,Is.SameAs(playback));
            Assert.That(library.Storage.Files(true),Is.Empty);
            library.ImportPath(Package("Second.zip",255),false,true);yield return WaitForImport();
            Assert.That(library.ActiveModelPath,Is.Not.EqualTo(first));
            Assert.That(view.AvailableProfiles.Profiles.Count,Is.EqualTo(2));
            Assert.That(view.AvailableProfiles.Profiles[1].GetValue("position").Number,Is.EqualTo(255));
            library.ImportPath(library.Storage.Files(true,first).Single(),true,false);yield return WaitForImport();
            Assert.That(library.Status,Does.Contain("same ZIP package"));
            library.Configure(view.Director,Path.Combine(folder,"Library"));
            library.ImportPath(first,false,false);yield return WaitForImport();
            Assert.That(view.AvailableProfiles.Profiles[1].GetValue("position").Number,Is.EqualTo(100));
            view.Director.StartPlayback(true);view.Director.Activate("calculate-all");
            Assert.That(view.Director.Playback.B.RawScore,Is.EqualTo(score));
            library.List(true);Assert.That(library.ListedFiles.Length,Is.EqualTo(1));
            library.ImportPath(library.ListedFiles[0],true,false);yield return WaitForImport();
            Assert.That(view.AvailableProfiles.Profiles.Count,Is.EqualTo(2),library.Status);
        }
        [UnityTest] public IEnumerator BrowserUploadsBinaryZipAndActivatesItsProfilesTogether()
        {
            var library=view.Director.Library;byte[] bytes=File.ReadAllBytes(Package("Browser.zip",255));
            library.StartUpload();Assert.That(library.UploadAddress,Is.Not.Null,library.Status);
            int port=new Uri(library.UploadAddress).Port;string code=library.UploadCode;
            var task=System.Threading.Tasks.Task.Run(()=>{
                using(var client=new System.Net.Sockets.TcpClient("127.0.0.1",port))using(var stream=client.GetStream()) {
                    client.ReceiveTimeout=5000;
                    byte[] header=System.Text.Encoding.ASCII.GetBytes("POST /upload?token="+code+"&kind=package&name=Browser.zip HTTP/1.1\r\nHost: localhost\r\nContent-Length: "+bytes.Length+"\r\n\r\n");
                    stream.Write(header,0,header.Length);stream.Write(bytes,0,bytes.Length);
                    using(var reader=new StreamReader(stream))return reader.ReadToEnd();
                }
            });
            int frames=0;while(!task.IsCompleted && frames++<600)yield return null;
            Assert.That(task.IsCompleted,Is.True);Assert.That(task.Result,Does.Contain("202"));
            frames=0;while(library.ActiveModelPath==null && frames++<600)yield return null;
            Assert.That(library.ActiveModelPath,Is.Not.Null,library.Status);
            Assert.That(view.AvailableProfiles.Profiles.Count,Is.EqualTo(2));
            Assert.That(library.Storage.PackageFor(library.ActiveModelPath),Is.Not.Null);
            library.StopUpload();
        }
    }
}
