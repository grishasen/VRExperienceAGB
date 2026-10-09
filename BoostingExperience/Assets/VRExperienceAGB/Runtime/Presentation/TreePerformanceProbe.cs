#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;

namespace VRExperienceAGB.Presentation
{
    /// <summary>Opt-in on-device frame sampling; never moves the tracked head or changes evaluation.</summary>
    public sealed class TreePerformanceProbe : MonoBehaviour
    {
        private float pollAt, warmup, elapsed;
        private bool requested, entered;
        private readonly List<float> frames=new List<float>();
        private readonly List<XRDisplaySubsystem> displays=new List<XRDisplaySubsystem>();
        private string Output => Path.Combine(UnityEngine.Application.persistentDataPath,"tree-performance.json");
        [Serializable] private class Report
        {
            public string status, device, utc;
            public int frames, trees;
            public float seconds, fps, medianMs, p95Ms;
            public bool focused, xrRunning;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install() { new GameObject("DevelopmentPerformanceProbe").AddComponent<TreePerformanceProbe>(); }
        [Serializable] private class LibraryReport {
            public string status,address,code,modelId;public int trees,profiles;
            public double scoreA,scoreB,probabilityA,probabilityB;public bool complete;
        }
        private void LibraryCommand(string path)
        {
            var view=FindAnyObjectByType<OneTreeExperience>();if(view==null || !view.Ready || view.Director==null)return;
            string command=File.ReadAllText(path).Trim();File.Delete(path);var d=view.Director;
            if(command=="start"){d.Library.StartUpload();d.OpenMenu("upload");}
            if(command=="forest")d.Activate("forest");
            if(command=="screenshot")view.GetComponent<FeedbackCapture>().Request();
            if(command=="playback" || command=="ab") {
                d.StartPlayback(command=="ab");
                if(d.Playback!=null)d.Playback.Paused=true;
            }
            if(command=="compare" && view.AvailableProfiles.Profiles.Count>=2){d.StartPlayback(true);d.Activate("calculate-all");}
            if(command=="open-demo"){
                var file=d.Library.Storage.Files(false).FirstOrDefault(f=>VRExperienceAGB.Import.ModelFileLibrary.DisplayName(f)=="demo-model");
                if(file!=null)d.Library.ImportPath(file,false,false);
            }
            if(command=="stop")d.Library.StopUpload();
            File.WriteAllText(Path.Combine(UnityEngine.Application.persistentDataPath,"json-library-report.json"),JsonUtility.ToJson(new LibraryReport{
                status=d.Library.Status,address=d.Library.UploadAddress,code=d.Library.UploadCode,modelId=view.Model.Id,trees=view.Model.Trees.Count,profiles=view.AvailableProfiles.Profiles.Count,
                complete=d.Playback?.Complete==true,scoreA=d.Playback?.A.RawScore??0,scoreB=d.Playback?.B?.RawScore??0,probabilityA=d.Playback?.A.Probability??0,probabilityB=d.Playback?.B?.Probability??0
            },true));
        }
        private void Update()
        {
            if(!requested) {
                if(Time.unscaledTime<pollAt)return;pollAt=Time.unscaledTime+1;
                string libraryRequest=Path.Combine(UnityEngine.Application.persistentDataPath,"json-library-request.txt");
                if(File.Exists(libraryRequest))LibraryCommand(libraryRequest);
                string path=Path.Combine(UnityEngine.Application.persistentDataPath,"tree-performance-request.txt");
                if(!File.Exists(path))return;
                File.Delete(path);requested=true;entered=false;warmup=elapsed=0;frames.Clear();
                File.WriteAllText(Output,JsonUtility.ToJson(new Report{status="Waiting for active headset"},true));
            }
            var view=FindAnyObjectByType<OneTreeExperience>();
            if(view==null || !view.Ready || view.Director==null)return;
            SubsystemManager.GetSubsystems(displays);
            bool xr=displays.Any(d=>d.running);
            if(!UnityEngine.Application.isFocused || view.ApplicationSuspended || !xr){warmup=elapsed=0;frames.Clear();return;}
            if(!entered){view.Director.Activate("single");view.Garden.Navigation.OpenTree(0);entered=true;}
            warmup+=Time.unscaledDeltaTime;if(warmup<3)return;
            frames.Add(Time.unscaledDeltaTime*1000);elapsed+=Time.unscaledDeltaTime;
            if(elapsed<15)return;
            var sorted=frames.OrderBy(f=>f).ToArray();
            var report=new Report{status="Completed",device=SystemInfo.deviceModel,utc=DateTime.UtcNow.ToString("O"),
                frames=frames.Count,trees=view.Model.Trees.Count,seconds=elapsed,fps=frames.Count/elapsed,
                medianMs=sorted[sorted.Length/2],p95Ms=sorted[(int)((sorted.Length-1)*.95)],focused=true,xrRunning=xr};
            File.WriteAllText(Output,JsonUtility.ToJson(report,true));Debug.Log("Tree performance: "+JsonUtility.ToJson(report));requested=false;
        }
    }
}
#endif
