using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Threading.Tasks;

namespace VRExperienceAGB.Presentation
{
    /// <summary>Explicit screenshot capture. Never moves a tracked camera or captures in the background.</summary>
    public sealed class FeedbackCapture : MonoBehaviour
    {
        public string Folder => Path.Combine(UnityEngine.Application.persistentDataPath,"Screenshots");
        public string LastPath { get; private set; }
        public string Status { get; private set; }
        public bool Busy { get; private set; }
        private bool held;
        private float nextCapture,hideAt;
        private OneTreeExperience view;
        private TMP_Text toast;
        public void Configure(OneTreeExperience owner)
        {
            view=owner;
            var canvas=view.Garden.CanvasAt("ScreenshotNotice",Vector3.zero,new Vector2(1000,110),.0012f,view.transform);
            foreach(var raycaster in canvas.GetComponents<UnityEngine.UI.GraphicRaycaster>())Destroy(raycaster);
            foreach(var surface in canvas.GetComponentsInChildren<Oculus.Interaction.PointableCanvas>())surface.gameObject.SetActive(false);
            toast=view.Garden.Text(canvas.transform,"Notice","",Vector2.zero,new Vector2(970,100),26);toast.richText=false;
            canvas.gameObject.SetActive(false);
        }
        public void HandleButton(bool pressed)
        {
            if(pressed && !held)Request();held=pressed;
        }
        public void Request()
        {
            if(Busy || Time.realtimeSinceStartup<nextCapture || view.ApplicationSuspended)return;
            nextCapture=Time.realtimeSinceStartup+2;Busy=true;toast.transform.parent.gameObject.SetActive(false);
            StartCoroutine(Capture());
        }
        private IEnumerator Capture()
        {
            string name="forest-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")+"-"+Guid.NewGuid().ToString("N").Substring(0,6)+".png";
            string pending=Path.Combine(Folder,"pending-"+name);byte[] png=null;
            try {
                Directory.CreateDirectory(Folder);
                png=RenderView();
            }catch(Exception e){Debug.LogWarning("Screenshot render failed: "+e.Message);Notify("Could not capture this view. Try X again.");}
            if(png!=null) {
                string completed=Path.Combine(Folder,name);
                var write=Task.Run(()=>{
                    try{File.WriteAllBytes(pending,png);File.Move(pending,completed);}
                    finally{if(File.Exists(pending))File.Delete(pending);}
                });
                while(!write.IsCompleted)yield return null;
                if(write.IsFaulted)Notify("Could not save screenshot. Check device storage.");
                else {LastPath=completed;Notify("Screenshot saved · X / F12\nDownload from Models / profiles → From computer / Wi-Fi");}
            }
            Busy=false;
        }
        private byte[] RenderView()
        {
            // Render a mono copy of the current view. XR swapchain readback can produce a black PNG.
            // Copy only camera properties; no tracking components or transforms are modified.
            var head=view.Garden.Locomotion.Head;var source=head.GetComponent<Camera>();
            var go=new GameObject("FeedbackScreenshotCamera");var camera=go.AddComponent<Camera>();
            var target=RenderTexture.GetTemporary(1600,900,24,RenderTextureFormat.ARGB32);
            var prior=RenderTexture.active;Texture2D pixels=null;
            var fade=head.Find("ComfortFade");bool fadeActive=fade!=null && fade.gameObject.activeSelf;
            try {
                camera.CopyFrom(source);camera.enabled=false;camera.stereoTargetEye=StereoTargetEyeMask.None;
                camera.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);
                camera.aspect=1600f/900;camera.ResetProjectionMatrix();camera.targetTexture=target;
                var data=camera.GetUniversalAdditionalCameraData();data.allowXRRendering=false;
                var original=source.GetUniversalAdditionalCameraData();data.renderPostProcessing=original.renderPostProcessing;
                data.volumeLayerMask=original.volumeLayerMask;data.volumeTrigger=source.transform;
                if(fadeActive)fade.gameObject.SetActive(false);
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
                RenderTexture.active=target;pixels=new Texture2D(1600,900,TextureFormat.RGB24,false);
                pixels.ReadPixels(new Rect(0,0,1600,900),0,0);pixels.Apply();return pixels.EncodeToPNG();
            }finally {
                if(fadeActive && fade!=null)fade.gameObject.SetActive(true);
                RenderTexture.active=prior;camera.targetTexture=null;RenderTexture.ReleaseTemporary(target);
                if(pixels!=null)Destroy(pixels);Destroy(go);
            }
        }
        private void Notify(string message)
        {
            Status=message;toast.text=message;var head=view.Garden.Locomotion.Head;
            var f=Vector3.ProjectOnPlane(head.forward,Vector3.up).normalized;
            toast.transform.parent.SetPositionAndRotation(head.position+f*1.8f-Vector3.up*.45f,Quaternion.LookRotation(f));
            toast.transform.parent.gameObject.SetActive(true);hideAt=Time.realtimeSinceStartup+4;
        }
        private void Update()
        {
            if(view==null)return;
            var left=InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            HandleButton(left.TryGetFeatureValue(CommonUsages.primaryButton,out bool pressed)&&pressed);
            if(UnityEngine.InputSystem.Keyboard.current?.f12Key.wasPressedThisFrame==true)Request();
            if(toast.transform.parent.gameObject.activeSelf && Time.realtimeSinceStartup>hideAt)toast.transform.parent.gameObject.SetActive(false);
        }
    }
}
