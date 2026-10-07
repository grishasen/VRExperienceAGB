using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRExperienceAGB.Presentation;
using Object=UnityEngine.Object;

namespace VRExperienceAGB.Tests
{
    /// <summary>Opt-in deterministic Editor recording. Never runs or moves a tracked headset camera.</summary>
    public class ScenarioVideoTests
    {
        private const int Fps=15;
        private OneTreeExperience view;
        private ExperienceDirector director;
        private Camera camera;
        private RenderTexture target;
        private Texture2D pixels;
        private BinaryWriter sound;
        private NativeArray<float> audioBuffer;
        private TMP_Text caption;
        private int frame, priorCaptureRate;
        private bool recordingAudio;
        private string folder;
        [UnityTest,Timeout(900000)] public IEnumerator RecordRequestedScenario()
        {
            folder=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../../artifacts/scenario-revision/video"));
            string request=Path.Combine(folder,"record-request.txt");
            if(!File.Exists(request))Assert.Ignore("Video is opt-in. Run scripts/record-scenario.sh.");
            File.Delete(request);
            yield return SceneManager.LoadSceneAsync("OneTreeLearning");yield return null;
            view=Object.FindAnyObjectByType<OneTreeExperience>();Assert.That(view.Ready,Is.True,view.explanation.text);
            director=view.Director;director.enabled=false;view.enabled=false;
            var preview=view.GetComponent<DesktopTreePreview>();preview.enabled=false;
            Assert.That(preview.previewCamera.gameObject.activeInHierarchy,Is.True,"Recording requires Editor desktop preview.");
            camera=preview.previewCamera;view.SendMessage("OnApplicationFocus",true);view.SendMessage("OnApplicationPause",false);
            view.Garden.Locomotion.Recenter();view.Garden.Locomotion.enabled=false;
            var wildlife=view.Garden.M5.Atmosphere.Wildlife;wildlife.enabled=false;wildlife.SendMessage("OnApplicationFocus",true);
            view.Garden.M5.Atmosphere.Sky.enabled=false;
            var overlay=new GameObject("RecordingCaption",typeof(RectTransform),typeof(Canvas));
            var canvas=overlay.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.5f;canvas.sortingOrder=32000;
            var textObject=new GameObject("Caption",typeof(RectTransform),typeof(TextMeshProUGUI));textObject.transform.SetParent(overlay.transform,false);
            caption=textObject.GetComponent<TextMeshProUGUI>();caption.font=view.explanation.font;caption.fontSize=20;caption.alignment=TextAlignmentOptions.TopLeft;caption.color=new Color(.8f,.92f,1);caption.raycastTarget=false;
            var rect=caption.rectTransform;rect.anchorMin=new Vector2(0,1);rect.anchorMax=Vector2.one;rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(20,-12);rect.sizeDelta=new Vector2(-40,55);
            target=new RenderTexture(1280,720,24);pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);camera.targetTexture=target;camera.fieldOfView=65;camera.farClipPlane=250;
            priorCaptureRate=Time.captureFramerate;Time.captureFramerate=Fps;
            recordingAudio=AudioRenderer.Start();Assert.That(recordingAudio,Is.True);
            audioBuffer=new NativeArray<float>(AudioSettings.outputSampleRate/Fps*2,Allocator.Persistent);
            sound=new BinaryWriter(File.Create(Path.Combine(folder,"audio.f32")));
            File.WriteAllText(Path.Combine(folder,"audio-rate.txt"),AudioSettings.outputSampleRate.ToString());
            try {
                director.OpenMenu("home");yield return Frames(45,"01 · Main menu · 100-tree sample");
                director.OpenMenu("position");director.Activate("farther");yield return Frames(30,"02 · Scene placement");director.Activate("recenter");
                director.Activate("single");yield return Frames(40,"03 · Choose one tree from the forest");
                view.Garden.Navigation.OpenTree(0);yield return Frames(35,"04 · Manual branch exploration");
                for(int i=0;i<2;i++){
                    view.Execute(TreeAction.TrueBranch,view.Session.State.Revision,view.Session.State.NodeId);
                    yield return Frames(30,"04 · Manual branch · TRUE",()=>view.Advance(1f/Fps));
                }
                director.OpenMenu("tree");yield return Frames(25,"05 · One menu in front of the observer");
                director.OpenMenu("profiles");yield return Frames(35,"06 · Select prepared profile");director.Activate("start-profile");
                yield return Frames(80,"07 · Profile A · route through the stones",()=>director.AdvanceTour(1f/Fps));
                director.OpenMenu("playback");director.Activate("pause");yield return Frames(35,"07 · Pause / resume · calculate whole model");
                director.CloseMenu();yield return Frames(25,"07 · Paused ball stays in place",()=>director.AdvanceTour(1f/Fps));
                director.OpenMenu("playback");director.Activate("pause");director.CloseMenu();
                yield return Frames(70,"07 · Resume stone route",()=>director.AdvanceTour(1f/Fps));
                yield return FinishRun("08 · Accelerated traversal · every remaining tree evaluated and visited");
                yield return Frames(60,"09 · Final Score → sigmoid → probability");
                director.Activate("results-forest");yield return Frames(50,"10 · Complete forest · blue positive / red negative");
                director.Activate("results-table");camera.transform.localRotation=Quaternion.Euler(10,0,0);yield return Frames(65,"11 · All 100 trees on the table · final probability above");
                camera.transform.localRotation=Quaternion.identity;director.OpenMenu("compare");yield return Frames(35,"12 · Compare synthetic profiles A / B");director.Activate("start-ab");
                yield return Frames(160,"13 · Synchronized A / B · independent moving points",()=>director.AdvanceTour(1f/Fps));
                yield return FinishRun("14 · Accelerated A / B · every tree, no ensemble truncation");
                yield return Frames(70,"15 · Two probabilities and difference in percentage points");
                director.Activate("results-table");camera.transform.localRotation=Quaternion.Euler(10,0,0);yield return Frames(70,"16 · Table results · outer A / inner B rings");
                string completedResult=director.FinalText;
                director.OpenMenu("compare");director.Activate("start-ab");director.OpenMenu("playback");
                yield return Frames(35,"16 · Calculate whole model · skip the animation");director.Activate("calculate-all");
                Assert.That(director.Playback.CompletedTrees,Is.EqualTo(100));yield return Frames(35,"16 · Immediate complete A / B result");
                director.ShowResults(false);director.StopPlayback(false);camera.transform.localRotation=Quaternion.Euler(-30,0,0);yield return Frames(45,"17 · Winter sky · Orion, Taurus and Pleiades");
                view.Garden.M5.Atmosphere.Sky.ToggleGuides();yield return Frames(45,"18 · Optional constellation guides");
                camera.transform.localRotation=Quaternion.Euler(-30,180,0);yield return Frames(45,"19 · Northern sky · Ursa Major, Ursa Minor, Cassiopeia");
                view.Garden.M5.Atmosphere.Sky.ToggleGuides();
                // Only the Editor preview camera is moved for this close visual inspection.
                camera.transform.position=wildlife.Wolves[0].position+new Vector3(0,2,-9);
                camera.transform.LookAt(wildlife.Wolves[0].position+Vector3.up*1.2f);
                wildlife.SetSoundEnabled(false);wildlife.SetSoundEnabled(true);wildlife.AdvanceAmbience(13);
                var active=wildlife.Wolves[0];foreach(var w in wildlife.Wolves)if(w.gameObject.activeSelf)active=w;
                camera.transform.position=active.position+new Vector3(0,1.4f,-8);camera.transform.LookAt(active.position+Vector3.up);
                yield return Frames(120,"20 · Wolf appearance, natural howl and departure",()=>wildlife.AdvanceAmbience(1f/Fps));
                wildlife.SetSoundEnabled(false);Assert.That(wildlife.Visible,Is.False);
                yield return Frames(25,"21 · Sound off · wildlife event stops");
                File.WriteAllText(Path.Combine(folder,"completed.txt"),"PASS\nFrames: "+frame+"\nFPS: "+Fps+"\n"+completedResult);
            } finally {
                Cleanup();Object.Destroy(overlay);
            }
        }
        [TearDown] public void Cleanup()
        {
            if(recordingAudio)AudioRenderer.Stop();recordingAudio=false;
            sound?.Dispose();sound=null;if(audioBuffer.IsCreated)audioBuffer.Dispose();
            if(target!=null){
                Time.captureFramerate=priorCaptureRate;
                if(camera!=null)camera.targetTexture=null;
                RenderTexture.active=null;target.Release();Object.Destroy(target);target=null;
            }
            if(pixels!=null){Object.Destroy(pixels);pixels=null;}
        }
        private IEnumerator FinishRun(string label)
        {
            int guard=0;
            while(!director.Playback.Complete&&guard++<1000){
                for(int i=0;i<18&&!director.Playback.Complete;i++)director.AdvanceTour(100);
                yield return Frames(1,label+" · tree "+(director.Playback.TreeIndex+1));
            }
            Assert.That(director.Playback.Complete,Is.True);Assert.That(director.Playback.CompletedTrees,Is.EqualTo(100));
        }
        private IEnumerator Frames(int count,string label,Action advance=null)
        {
            caption.text=label;
            for(int i=0;i<count;i++){
                // The offscreen Editor recorder owns simulated lifecycle state; headset behavior is unchanged.
                view.SendMessage("OnApplicationFocus",true);view.SendMessage("OnApplicationPause",false);
                view.Garden.M5.Atmosphere.Wildlife.SendMessage("OnApplicationFocus",true);
                advance?.Invoke();
                yield return null;
                director.SendMessage("LateUpdate");view.Garden.M5.Atmosphere.Sky.SendMessage("LateUpdate");
                camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();RenderTexture.active=previous;
                File.WriteAllBytes(Path.Combine(folder,"frame-"+frame.ToString("D5")+".jpg"),pixels.EncodeToJPG(88));
                Assert.That(AudioRenderer.Render(audioBuffer),Is.True);
                for(int n=0;n<audioBuffer.Length;n++)sound.Write(audioBuffer[n]);
                frame++;
            }
        }
    }
}
