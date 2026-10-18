using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.XR;

namespace VRExperienceAGB.Presentation
{
    /// <summary>Explicit opt-in measurement. Editor samples never claim headset acceptance.</summary>
    public sealed class ExperiencePerformanceCapture : MonoBehaviour
    {
        [Serializable] public sealed class Report
        {
            public string utc, buildGuid, version, unityVersion, device, modelId, status, measurementKind;
            public bool editor, xrRunning, cpuTimingAvailable, gpuTimingAvailable;
            public int trees, frames, framesOverBudget, interruptedFrames;
            public float seconds, averageFps, medianMs, p95Ms, p99Ms, maximumMs, refreshHz;
            public double meanCpuMs, meanGpuMs;
            public long peakAllocatedBytes;
            public string[] viewTransitions;
            public float requestedSeconds;
            public string gpuTimingSource;
            public bool compositorGpuTimingAvailable, droppedFrameCountAvailable, presentedFrameCountAvailable;
            public double meanCompositorGpuMs;
            public int droppedFrames, presentedFrames;
            public Vector3 headStart, headEnd, headMinimum, headMaximum, headEulerStart, headEulerEnd;
            public string limitations="Frames over budget are an application timing estimate, not compositor dropped-frame counters. Zero CPU/GPU means unavailable when its availability flag is false. Confirm thermals, compositor counters and sustained performance on Quest.";
        }
        public bool Recording { get; private set; }
        public string Status { get; private set; }="No capture recorded in this session.";
        public string LastPath { get; private set; }="";
        private OneTreeExperience view;
        private readonly List<float> frames=new List<float>();
        private readonly List<string> transitions=new List<string>();
        private readonly List<XRDisplaySubsystem> displays=new List<XRDisplaySubsystem>();
        private readonly FrameTiming[] timings=new FrameTiming[1];
        private float warmup,elapsed;
        private double cpu,gpu;
        private int cpuCount,gpuCount,interrupted;
        private long peakMemory;
        private string lastState;
        private float duration;
        private XRDisplaySubsystem xrDisplay;
        private double compositorGpu;
        private int compositorCount, xrGpuCount, droppedStart, presentedStart;
        private bool droppedAvailable,presentedAvailable;
        private Vector3 headStart,headMinimum,headMaximum,headEulerStart;
        public void Begin(OneTreeExperience target,float durationSeconds=60)
        {
            view=target;frames.Clear();transitions.Clear();warmup=elapsed=0;cpu=gpu=0;cpuCount=gpuCount=interrupted=0;peakMemory=0;lastState=null;
            duration=float.IsFinite(durationSeconds)?Mathf.Clamp(durationSeconds,10,600):60;
            compositorGpu=0;compositorCount=xrGpuCount=0;droppedAvailable=presentedAvailable=false;
            SubsystemManager.GetSubsystems(displays);xrDisplay=displays.FirstOrDefault(d=>d.running);
            Recording=true;Status="Recording after warm-up; close this menu to use the scene.";
        }
        private void Update()
        {
            if(!Recording||view==null||!view.Ready)return;
            if(view.ApplicationSuspended){interrupted++;return;}
            float dt=Time.unscaledDeltaTime;if(!float.IsFinite(dt)||dt<=0)return;
            warmup+=dt;if(warmup<3)return;
            var head=view.Garden.Locomotion.Head;
            if(frames.Count==0) {
                headStart=headMinimum=headMaximum=head.position;headEulerStart=head.eulerAngles;
                if(xrDisplay!=null) {
                    droppedAvailable=xrDisplay.TryGetDroppedFrameCount(out droppedStart);
                    presentedAvailable=xrDisplay.TryGetFramePresentCount(out presentedStart);
                }
            }
            headMinimum=Vector3.Min(headMinimum,head.position);headMaximum=Vector3.Max(headMaximum,head.position);
            frames.Add(dt*1000);elapsed+=dt;
            peakMemory=Math.Max(peakMemory,Profiler.GetTotalAllocatedMemoryLong());
            FrameTimingManager.CaptureFrameTimings();
            bool hasGpu=false;
            if(FrameTimingManager.GetLatestTimings(1,timings)>0) {
                if(timings[0].cpuFrameTime>0){cpu+=timings[0].cpuFrameTime;cpuCount++;}
                if(timings[0].gpuFrameTime>0){gpu+=timings[0].gpuFrameTime;gpuCount++;hasGpu=true;}
            }
            // XR provider times are seconds; FrameTimingManager times are milliseconds.
            if(xrDisplay!=null) {
                if(!hasGpu && xrDisplay.TryGetAppGPUTimeLastFrame(out float seconds) && float.IsFinite(seconds) && seconds>0) {
                    gpu+=seconds*1000;gpuCount++;xrGpuCount++;
                }
                if(xrDisplay.TryGetCompositorGPUTimeLastFrame(out float compositor) && float.IsFinite(compositor) && compositor>0) {
                    compositorGpu+=compositor*1000;compositorCount++;
                }
            }
            string state=view.Director.MenuVisible?"Menu/"+view.Director.MenuPage:view.Director.PlayingTour?"Playback":view.Garden.Navigation.Page.ToString();
            if(state!=lastState){lastState=state;transitions.Add(elapsed.ToString("0.00",CultureInfo.InvariantCulture)+"s: "+state);}
            if(elapsed>=duration)Finish();
        }
        public void Finish()
        {
            if(!Recording)return;Recording=false;
            if(frames.Count==0){Status="No measured frames; capture stopped during warm-up.";return;}
            SubsystemManager.GetSubsystems(displays);var display=displays.FirstOrDefault(d=>d.running);
            float refresh=0;if(display!=null)display.TryGetDisplayRefreshRate(out refresh);
            var sorted=frames.OrderBy(f=>f).ToArray();
            int droppedEnd=0,presentedEnd=0;
            droppedAvailable=droppedAvailable && display==xrDisplay && display!=null && display.TryGetDroppedFrameCount(out droppedEnd) && droppedEnd>=droppedStart;
            presentedAvailable=presentedAvailable && display==xrDisplay && display!=null && display.TryGetFramePresentCount(out presentedEnd) && presentedEnd>=presentedStart;
            var report=new Report {
                utc=DateTime.UtcNow.ToString("O"),buildGuid=UnityEngine.Application.buildGUID,version=UnityEngine.Application.version,unityVersion=UnityEngine.Application.unityVersion,
                device=SystemInfo.deviceModel,modelId=view.Model.Id,trees=view.Model.Trees.Count,editor=UnityEngine.Application.isEditor,xrRunning=display!=null,
                measurementKind=UnityEngine.Application.isEditor?"Editor diagnostic; not Quest acceptance":display==null?"Player without active XR":"XR device measurement; acceptance requires review",
                status=elapsed>=duration?"Completed":"Stopped early",seconds=elapsed,requestedSeconds=duration,frames=frames.Count,averageFps=frames.Count/elapsed,
                medianMs=sorted[sorted.Length/2],p95Ms=sorted[(int)((sorted.Length-1)*.95)],p99Ms=sorted[(int)((sorted.Length-1)*.99)],maximumMs=sorted[sorted.Length-1],refreshHz=refresh,
                framesOverBudget=refresh>0?frames.Count(f=>f>1000/refresh):-1,interruptedFrames=interrupted,peakAllocatedBytes=peakMemory,
                cpuTimingAvailable=cpuCount>0,gpuTimingAvailable=gpuCount>0,meanCpuMs=cpuCount>0?cpu/cpuCount:0,meanGpuMs=gpuCount>0?gpu/gpuCount:0,viewTransitions=transitions.ToArray(),
                gpuTimingSource=gpuCount==0?"Unavailable":xrGpuCount==gpuCount?"XRDisplaySubsystem":xrGpuCount==0?"FrameTimingManager":"Mixed FrameTimingManager / XRDisplaySubsystem",
                compositorGpuTimingAvailable=compositorCount>0,meanCompositorGpuMs=compositorCount>0?compositorGpu/compositorCount:0,
                droppedFrameCountAvailable=droppedAvailable,presentedFrameCountAvailable=presentedAvailable,
                droppedFrames=droppedAvailable?droppedEnd-droppedStart:-1,presentedFrames=presentedAvailable?presentedEnd-presentedStart:-1,
                headStart=headStart,headEnd=view.Garden.Locomotion.Head.position,headMinimum=headMinimum,headMaximum=headMaximum,
                headEulerStart=headEulerStart,headEulerEnd=view.Garden.Locomotion.Head.eulerAngles
            };
            try {
                string folder=UnityEngine.Application.isEditor?Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../../artifacts/performance")):Path.Combine(UnityEngine.Application.persistentDataPath,"Measurements");
                Directory.CreateDirectory(folder);LastPath=Path.Combine(folder,"capture-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+".json");File.WriteAllText(LastPath,JsonUtility.ToJson(report,true));Status=report.status+" · "+report.measurementKind;
            }catch(IOException){Status="Capture could not be saved. Check available device storage.";}
        }
        private void OnDisable(){if(Recording)Finish();}
    }
}
