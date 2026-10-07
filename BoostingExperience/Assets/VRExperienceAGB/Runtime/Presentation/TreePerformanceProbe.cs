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
        private void Update()
        {
            if(!requested) {
                if(Time.unscaledTime<pollAt)return;pollAt=Time.unscaledTime+1;
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
