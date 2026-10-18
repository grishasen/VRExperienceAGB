using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRExperienceAGB.Application;
using VRExperienceAGB.Domain;
using VRExperienceAGB.Presentation;
using Object = UnityEngine.Object;

namespace VRExperienceAGB.Tests
{
    /// <summary>Opt-in cinematic capture of real scene commands, using only the desktop preview camera.</summary>
    public class NarratedDemoVideoTests
    {
        private const int Width=2560, Height=1440, Fps=30;
        private OneTreeExperience view;
        private ExperienceDirector director;
        private Camera camera;
        private RenderTexture target;
        private Texture2D pixels;
        private Process encoder;
        private BinaryWriter sound;
        private NativeArray<float> audio;
        private bool audioStarted, preview, skyOnly, quietSky;
        private int frames, oldRate;
        private string folder;
        private readonly List<string> shots=new List<string>();
        private float timeline;
        private ForestGardenView Garden => view.Garden;
        private Vector3 V(float x,float y,float z) => new Vector3(x,y,z);
        private Transform Find(string name) => view.GetComponentsInChildren<Transform>(true).First(t=>t.name==name);
        private void Pose(Vector3 position,Vector3 look,float fov=65)
        {camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(look-position));camera.fieldOfView=fov;}
        private void Front() => Pose(V(0,1.65f,0),V(0,1.65f,8));

        [UnityTest,Timeout(3600000)] public IEnumerator RecordNarratedDemo()
        {
            folder=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../../artifacts/narrated-demo-2026-10-18"));
            string request=Path.Combine(folder,"record-request.txt");
            if(!File.Exists(request))Assert.Ignore("Opt-in: create a preview or full record-request.txt in the demo artifact folder.");
            string mode=File.ReadAllText(request).Trim();preview=mode.Contains("preview");skyOnly=mode.StartsWith("sky");File.Delete(request);
            quietSky=mode.StartsWith("sky-look");
            if(skyOnly)folder=Path.Combine(folder,quietSky?"sky-look":"orion-revision");
            else if(mode.StartsWith("revision"))folder=Path.Combine(folder,"revision-2");
            Directory.CreateDirectory(Path.Combine(folder,preview?"preview":"review"));
            yield return SceneManager.LoadSceneAsync("OneTreeLearning");yield return null;
            view=Object.FindAnyObjectByType<OneTreeExperience>();Assert.That(view.Ready,Is.True,view.explanation.text);
            director=view.Director;director.enabled=false;view.enabled=false;
            var desktop=view.GetComponent<DesktopTreePreview>();desktop.enabled=false;camera=desktop.previewCamera;
            Assert.That(camera.gameObject.activeInHierarchy,Is.True,"Only desktop recording is permitted.");
            Garden.Locomotion.ComfortFadeEnabled=false;Garden.Locomotion.enabled=false;
            view.SendMessage("OnApplicationFocus",true);view.SendMessage("OnApplicationPause",false);
            Garden.M5.Atmosphere.Wildlife.enabled=false;Garden.M5.Atmosphere.Sky.enabled=false;
            // Keep wildlife a surprise in the application, including its sound, during filming.
            Garden.M5.Atmosphere.Wildlife.SetSoundEnabled(false);
            // Isolate the filming library from the user's saved models and profiles.
            director.Library.Configure(director,Path.Combine(folder,"Library"));
            string zip=Path.Combine(folder,"Forest-demo.zip");
            if(File.Exists(zip))File.Delete(zip);
            using(var archive=ZipFile.Open(zip,ZipArchiveMode.Create)) {
                using(var writer=new StreamWriter(archive.CreateEntry("model.json").Open()))writer.Write(Resources.Load<TextAsset>("LocalModel/model").text);
                using(var writer=new StreamWriter(archive.CreateEntry("profiles.json").Open()))writer.Write(Resources.Load<TextAsset>("LocalModel/profiles").text);
            }
            director.Library.ImportPath(zip,false,true);
            while(director.Library.Busy)yield return null;
            Assert.That(view.Model.Trees.Count,Is.EqualTo(100),director.Library.Status);
            Assert.That(view.AvailableProfiles.Profiles.Count,Is.EqualTo(2),director.Library.Status);
            director.CloseMenu();Garden.Navigation.ShowForest();Front();
            oldRate=Time.captureFramerate;Time.captureFramerate=Fps;
            target=new RenderTexture(Width,Height,24);pixels=new Texture2D(Width,Height,TextureFormat.RGB24,false);
            camera.targetTexture=target;camera.aspect=(float)Width/Height;camera.farClipPlane=400;
            if(!preview) {
                var start=new ProcessStartInfo("/opt/homebrew/bin/ffmpeg","-hide_banner -loglevel warning -y -f image2pipe -framerate 30 -c:v mjpeg -i pipe:0 -an -c:v libx264 -preset fast -crf 17 -pix_fmt yuv420p -movflags +faststart \""+Path.Combine(folder,"scene.mp4")+"\"") {
                    UseShellExecute=false,RedirectStandardInput=true,RedirectStandardError=true,CreateNoWindow=true
                };
                encoder=Process.Start(start);encoder.ErrorDataReceived+=(s,e)=>{if(e.Data!=null)File.AppendAllText(Path.Combine(folder,"encoder.log"),e.Data+"\n");};encoder.BeginErrorReadLine();
                audioStarted=AudioRenderer.Start();Assert.That(audioStarted,Is.True);
                audio=new NativeArray<float>(AudioSettings.outputSampleRate/Fps*2,Allocator.Persistent);
                sound=new BinaryWriter(File.Create(Path.Combine(folder,"ambience.f32")));
                File.WriteAllText(Path.Combine(folder,"audio-rate.txt"),AudioSettings.outputSampleRate.ToString());
            }
            try {
                if(skyOnly) {
                    if(quietSky)yield return QuietSkyShots();else yield return OrionShots();
                    File.WriteAllLines(Path.Combine(folder,"shots.tsv"),shots);
                    File.WriteAllText(Path.Combine(folder,preview?"preview-completed.txt":"completed.txt"),"Frames: "+frames+"\nSeconds: "+timeline+"\nSky replacement; wildlife disabled for capture only.");
                    yield break;
                }
                // 00:00 — an establishing view of the actual environment.
                yield return Shot("01-opening",12,V(0,2.2f,-5),V(1,2.3f,-2),V(0,4,17),V(0,4,23));
                Front();director.OpenMenu("models");
                yield return Still("02-select-model",8);
                director.Library.List(false);director.Activate("library-open:0");while(director.Library.Busy)yield return null;
                yield return Still("03-package-loaded",6);
                director.CloseMenu();Garden.Navigation.ShowForest();
                yield return Shot("04-forest-walk",14,V(0,1.8f,0),V(0,1.8f,10),V(-4,2.5f,10),V(3,2.8f,20));
                yield return Shot("05-lake",10,V(3,2,-5),V(0,2.3f,-8),V(-15,5,-55),V(12,7,-55));
                yield return OrionShots();
                Front();var treePoint=Garden.PlotPosition(16);
                Pose(treePoint+V(0,1.65f,-3.6f),treePoint+V(0,1.65f,0));Garden.ShowTreeChoices(16);
                yield return Still("08-tree-card",8);
                Garden.DescriptionCard.Hide();Front();director.Activate("forest");Garden.Navigation.OpenTree(16);Front();view.Refresh();
                yield return Shot("09-tree-overview",8,V(0,1.9f,-1.7f),V(.3f,1.85f,-.6f),V(0,1.75f,4),V(0,1.75f,4));
                Front();view.NameTooltip.ShowNode(this,view.Session.State.NodeId);view.NameTooltip.TogglePin();
                var reading=Find("NodeReadingPanel");
                var readingPosition=reading.position-reading.forward*1.5f;
                yield return Shot("10-read-split",10,readingPosition,readingPosition,reading.position,reading.position);
                director.OpenNodeDetails(16,view.Session.State.NodeId);
                yield return Still("11-split-details",9);
                director.CloseMenu();Front();
                view.Execute(TreeAction.TrueBranch,view.Session.State.Revision,view.Session.State.NodeId);
                yield return Shot("12-manual-branch",8,V(-.2f,1.85f,-.5f),V(-.7f,1.85f,.2f),V(0,1.75f,4),V(0,1.75f,4),dt=>view.Advance(dt));
                int guard=0;while(!view.Session.State.AtLeaf && guard++<50) {
                    view.Execute(TreeAction.TrueBranch,view.Session.State.Revision,view.Session.State.NodeId);view.Advance(5);
                }
                Assert.That(view.Session.State.AtLeaf,Is.True);
                yield return Shot("13-manual-leaf",7,V(0,1.85f,0),V(0,1.85f,.5f),view.nodeViews[0].title.transform.position,view.nodeViews[0].title.transform.position);
                Front();director.OpenMenu("profiles");yield return Still("14-choose-profile",7);
                director.StartPlayback(false);FocusPlaybackTree(16);Front();director.SetPace(PlaybackPace.Learn);
                yield return Shot("15-follow-profile",13,V(0,2,-.7f),V(.3f,2,-.3f),V(0,1.65f,3.6f),V(0,1.65f,3.6f),dt=>director.AdvanceTour(dt));
                director.Playback.Paused=true;director.AdvanceTour(.001f);director.OpenMenu("playback");
                yield return Still("16-playback-controls",7);
                director.CloseMenu();director.Playback.Paused=false;
                yield return Shot("17-resume",7,V(.3f,2,-.3f),V(0,2,-.1f),V(0,1.65f,3.6f),V(0,1.65f,3.6f),dt=>director.AdvanceTour(dt));
                director.OpenMenu("playback");yield return Still("18-calculate-all",5);director.Activate("calculate-all");
                Assert.That(director.Playback.CompletedTrees,Is.EqualTo(100));
                yield return Still("19-score",9);
                Front();director.ShowResults(true);var table=Garden.M5.Diorama.ModelRoot;
                yield return Shot("20-table",13,camera.transform.position,table.position+V(-.25f,1.25f,-1.65f),table.position+V(0,.38f,0),table.position+V(0,.38f,0));
                yield return Shot("21-table-orbit",10,camera.transform.position,table.position+V(.65f,1.15f,-1.55f),table.position+V(0,.38f,0),table.position+V(0,.38f,0),dt=>Garden.M5.Diorama.Rotate(dt*2));
                Front();director.OpenMenu("compare");yield return Still("22-compare",7);
                director.StartPlayback(true);FocusPlaybackTree(16);Front();director.SetPace(PlaybackPace.Learn);
                yield return Shot("23-ab-route",17,V(0,2.1f,-.8f),V(.3f,2.1f,-.3f),V(0,1.75f,3.7f),V(0,1.75f,3.7f),dt=>director.AdvanceTour(dt));
                director.OpenMenu("playback");director.Activate("calculate-all");
                Assert.That(director.Playback.CompletedTrees,Is.EqualTo(100));Assert.That(director.Playback.B,Is.Not.Null);
                yield return Still("24-ab-result",9);
                Front();director.ShowResults(true);table=Garden.M5.Diorama.ModelRoot;
                yield return Shot("25-ab-table",9,V(0,2.1f,-.15f),table.position+V(.2f,1.2f,-1.7f),table.position+V(0,.4f,0),table.position+V(0,.4f,0));
                Front();director.ShowResults(false);
                // Read the accepted A/B result, then walk among the colored contribution rings.
                yield return Still("26-forest-results",6);
                yield return Shot("27-final-walk",13,V(0,2.1f,4),V(0,2.1f,16),V(-5,1.3f,12),V(6,1.5f,24));
                yield return Shot("28-closing",8,V(0,2.5f,-5),V(0,3,-8),V(5,6,-50),V(5,9,-50));
                File.WriteAllLines(Path.Combine(folder,"shots.tsv"),shots);
                File.WriteAllText(Path.Combine(folder,preview?"preview-completed.txt":"completed.txt"),"Frames: "+frames+"\nSeconds: "+timeline+"\n"+director.FinalText);
            } finally { Cleanup(); }
        }
        private void FocusPlaybackTree(int index)
        {
            while(director.Playback.TreeIndex<index && !director.Playback.Complete)director.Playback.Next();
            director.AdvanceTour(.001f);
        }
        private IEnumerator OrionShots()
        {
            var sky=Garden.M5.Atmosphere.Sky;
            var belt=sky.NamedStars["Alnilam"];
            Assert.That(belt.y,Is.GreaterThan(0));
            var position=V(0,2.6f,0);
            var approach=Quaternion.AngleAxis(-28,Vector3.up)*belt;
            yield return Shot("06-stars",8,position,position,position+approach*100,position+belt*100,null,65,55);
            sky.ToggleGuides();
            yield return Shot("07-orion",8,position,position,position+belt*100,position+belt*100,null,55,42);
            foreach(var name in new[]{"Betelgeuse","Bellatrix","Mintaka","Alnilam","Alnitak","Saiph","Rigel"}) {
                var point=camera.WorldToViewportPoint(camera.transform.position+sky.NamedStars[name]*100);
                Assert.That(point.x,Is.InRange(.08f,.92f),name);Assert.That(point.y,Is.InRange(.08f,.92f),name);
            }
            sky.ToggleGuides();
        }
        private IEnumerator QuietSkyShots()
        {
            var guides=Find("ConstellationGuides");guides.gameObject.SetActive(false);
            var position=V(0,2.3f,-8);
            var lake=V(12,7,-55);
            var raised=position+Quaternion.Euler(-45,125,0)*Vector3.forward*100;
            var across=position+Quaternion.Euler(-38,55,0)*Vector3.forward*100;
            // Match the preceding lake shot, lift the gaze, then look across the sky at a fixed wide angle.
            yield return Shot("06-stars",8,position,position,lake,raised);
            yield return Shot("07-sky-look",8,position,position,raised,across);
            Assert.That(guides.gameObject.activeSelf,Is.False);
            Assert.That(camera.fieldOfView,Is.EqualTo(65));
        }
        private IEnumerator Still(string id,float seconds) => Shot(id,seconds,camera.transform.position,camera.transform.position,camera.transform.position+camera.transform.forward*5,camera.transform.position+camera.transform.forward*5);
        private IEnumerator Shot(string id,float seconds,Vector3 from,Vector3 to,Vector3 lookFrom,Vector3 lookTo,Action<float> advance=null,float fovFrom=65,float fovTo=65)
        {
            shots.Add(id+"\t"+timeline+"\t"+seconds);
            int count=preview?3:Mathf.RoundToInt(seconds*Fps);float dt=seconds/count;
            for(int i=0;i<count;i++) {
                view.SendMessage("OnApplicationFocus",true);view.SendMessage("OnApplicationPause",false);
                Garden.M5.Atmosphere.Wildlife.SendMessage("OnApplicationFocus",true);
                float t=count==1?1:(float)i/(count-1);t=t*t*(3-2*t);
                Pose(Vector3.Lerp(from,to,t),Vector3.Lerp(lookFrom,lookTo,t),Mathf.Lerp(fovFrom,fovTo,t));
                advance?.Invoke(dt);Garden.M5.Atmosphere.Wildlife.AdvanceAmbience(dt);Garden.M5.Atmosphere.Sky.AdvanceSky(dt);
                yield return null;
                director.SendMessage("LateUpdate");Garden.M5.Atmosphere.Sky.SendMessage("LateUpdate");
                camera.Render();var prior=RenderTexture.active;RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,Width,Height),0,0);pixels.Apply();RenderTexture.active=prior;
                byte[] bytes=pixels.EncodeToJPG(96);
                if(preview || i==count/2)File.WriteAllBytes(Path.Combine(folder,preview?"preview":"review",id+"-"+(preview?i:1)+".jpg"),bytes);
                if(!preview) {
                    encoder.StandardInput.BaseStream.Write(bytes,0,bytes.Length);
                    Assert.That(AudioRenderer.Render(audio),Is.True);
                    for(int n=0;n<audio.Length;n++)sound.Write(audio[n]);
                }
                frames++;
                if(i%30==0)File.WriteAllText(Path.Combine(folder,"progress.txt"),id+" · "+i+" / "+count+" · total frames "+frames);
            }
            timeline+=seconds;
        }
        [TearDown] public void Cleanup()
        {
            if(audioStarted)AudioRenderer.Stop();audioStarted=false;
            sound?.Dispose();sound=null;if(audio.IsCreated)audio.Dispose();
            if(encoder!=null) {
                encoder.StandardInput.Close();if(!encoder.WaitForExit(120000))encoder.Kill();
                int code=encoder.ExitCode;encoder.Dispose();encoder=null;Assert.That(code,Is.Zero,"Video encoder failed.");
            }
            Time.captureFramerate=oldRate;
            if(camera!=null)camera.targetTexture=null;
            if(target!=null){target.Release();Object.Destroy(target);target=null;}
            if(pixels!=null){Object.Destroy(pixels);pixels=null;}
        }
    }
}
