using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRExperienceAGB.Presentation;

namespace VRExperienceAGB.Tests
{
    public class WinterSkyPlayModeTests
    {
        private OneTreeExperience view;
        private WinterNightSky sky;
        [UnitySetUp]
        public IEnumerator Open() {
            yield return LegacyTeachingScene.Load(); yield return null;
            view=Object.FindAnyObjectByType<OneTreeExperience>();
             view.enabled=false;
            view.GetComponent<DesktopTreePreview>().enabled=false;
            sky=view.Garden.M5.Atmosphere.Sky; sky.enabled=false;
            sky.SendMessage("OnApplicationFocus",true); sky.SendMessage("OnApplicationPause",false);
        }
        [UnityTest]
        public IEnumerator CatalogLandmarksAndCameraPoseSurviveNavigationAndImport() {
            Assert.That(sky.VisibleStarCount,Is.InRange(100,500));
            foreach(var name in new[]{"Sirius","Procyon","Betelgeuse","Rigel","Alcyone","Polaris","Alnitak","Alnilam","Mintaka"}) Assert.That(sky.NamedStars[name].y,Is.GreaterThan(0));
            Assert.That(sky.NamedStars["Sirius"].x, Is.LessThan(0), "East must appear left when facing south.");
            Assert.That(sky.NamedStars["Alcyone"].x, Is.GreaterThan(0));
            var head=view.Garden.Locomotion.Head; var p=head.localPosition; var q=head.localRotation;
            view.Garden.Navigation.ShowForest(); view.Garden.Navigation.OpenTree(0); sky.AdvanceSky(27);
            Assert.That(head.localPosition,Is.EqualTo(p)); Assert.That(head.localRotation,Is.EqualTo(q));
            Assert.That(view.transform.Find("WinterNightSky/WinterMoon").gameObject.activeInHierarchy,Is.True);
            foreach(var name in new[]{"Sirius","Procyon","Betelgeuse","Alcyone"}) Assert.That(Vector3.Angle(sky.MoonDirection,sky.NamedStars[name]),Is.GreaterThan(10));
            view.BeginComparison(true);
            Assert.That(Object.FindObjectsByType<WinterNightSky>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
            Assert.That(RenderSettings.skybox.shader.name,Is.EqualTo("VRExperienceAGB/WinterSkyBackground"));
            yield return null;
        }
        [UnityTest]
        public IEnumerator MeteorsAreBriefAndSuspendOnFocusLoss() {
            sky.AdvanceSky(26); Assert.That(sky.MeteorCount,Is.EqualTo(1));
            sky.AdvanceSky(.3f); Assert.That(sky.MeteorVisible,Is.True);
            sky.SendMessage("OnApplicationFocus",false); sky.AdvanceSky(80);
            Assert.That(sky.MeteorVisible,Is.False); Assert.That(sky.MeteorCount,Is.EqualTo(1));
            sky.SendMessage("OnApplicationFocus",true); sky.AdvanceSky(.1f); Assert.That(sky.MeteorCount,Is.EqualTo(1));
            yield return null;
        }
        [UnityTest]
        public IEnumerator CaptureSouthernWinterSkyWithoutMovingTrackedCamera() {
            view.Garden.Navigation.ShowForest();
            var go=new GameObject("AstronomicalPreview",typeof(Camera)); var camera=go.GetComponent<Camera>();
            camera.transform.position=view.Garden.Locomotion.Head.position;
            camera.transform.rotation=Quaternion.LookRotation(new Vector3(0,Mathf.Sin(30*Mathf.Deg2Rad),Mathf.Cos(30*Mathf.Deg2Rad)));
            camera.clearFlags=CameraClearFlags.Skybox; camera.fieldOfView=75; camera.farClipPlane=160;
            yield return null;
            var target=new RenderTexture(1800,1200,24); var previous=RenderTexture.active; camera.targetTexture=target;
            camera.Render(); RenderTexture.active=target;
            var pixels=new Texture2D(1800,1200,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1800,1200),0,0);pixels.Apply();
            string folder=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../../artifacts/winter-sky"));Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder,"moonlit-selected.png"),pixels.EncodeToPNG());
            RenderTexture.active=previous; camera.targetTexture=null; target.Release(); Object.Destroy(target); Object.Destroy(pixels);
            Object.Destroy(go);
        }
    }
}
