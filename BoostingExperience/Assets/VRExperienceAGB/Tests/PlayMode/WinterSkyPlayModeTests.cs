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
            foreach(var name in new[]{"Sirius","Procyon","Betelgeuse","Rigel","Alcyone","Polaris","Alnitak","Alnilam","Mintaka","42 Ori","Theta1 Ori C","Theta2 Ori A","Hatysa"}) Assert.That(sky.NamedStars[name].y,Is.GreaterThan(0));
            var starVertices = view.transform.Find("WinterNightSky/CatalogStars").GetComponent<MeshFilter>().sharedMesh.vertices;
            foreach (var name in new[] { "42 Ori", "Theta1 Ori C", "Theta2 Ori A", "Hatysa" }) {
                float closest = 180;
                for (int i = 0; i < starVertices.Length; i += 4) {
                    var center = (starVertices[i] + starVertices[i+1] + starVertices[i+2] + starVertices[i+3]) * .25f;
                    closest = Mathf.Min(closest, Vector3.Angle(center, sky.NamedStars[name]));
                }
                Assert.That(closest, Is.LessThan(.02f), name + " must survive the faint-star cutoff in the actual mesh.");
            }
            Assert.That(Vector3.Angle(sky.NamedStars["Mintaka"], sky.NamedStars["Alnilam"]), Is.EqualTo(1.386f).Within(.01f));
            Assert.That(Vector3.Angle(sky.NamedStars["Alnilam"], sky.NamedStars["Alnitak"]), Is.EqualTo(1.356f).Within(.01f));
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
        public IEnumerator TwinkleSuspendsAndResumesWithoutChangingCatalogGeometry() {
            var stars = view.transform.Find("WinterNightSky/CatalogStars");
            var mesh = stars.GetComponent<MeshFilter>().sharedMesh;
            var vertices = mesh.vertices;
            var material = stars.GetComponent<Renderer>().sharedMaterial;
            sky.AdvanceSky(.1f);
            float activeTime = material.GetFloat("_TwinkleTime");
            Assert.That(activeTime, Is.GreaterThan(0));
            sky.SendMessage("OnApplicationFocus", false); sky.AdvanceSky(20);
            Assert.That(material.GetFloat("_TwinkleTime"), Is.EqualTo(activeTime));
            sky.SendMessage("OnApplicationFocus", true);
            sky.SendMessage("OnApplicationPause", true); sky.AdvanceSky(20);
            Assert.That(material.GetFloat("_TwinkleTime"), Is.EqualTo(activeTime));
            sky.SendMessage("OnApplicationPause", false);
            view.Session.SetPaused(true); sky.AdvanceSky(.1f);
            Assert.That(material.GetFloat("_TwinkleTime"), Is.EqualTo(activeTime + .1f).Within(.001f),
                "Inspecting a paused route must not freeze the ambient stars.");
            Assert.That(sky.MeteorVisible, Is.False);
            view.Session.SetPaused(false); sky.AdvanceSky(.1f);
            Assert.That(material.GetFloat("_TwinkleTime"), Is.EqualTo(activeTime + .2f).Within(.001f));
            CollectionAssert.AreEqual(vertices, mesh.vertices);
            Assert.That(view.transform.Find("WinterNightSky/ConstellationGuides/GuideLines")
                .GetComponent<MeshFilter>().sharedMesh.uv2, Is.Empty, "Guides must not receive star scintillation.");
            yield return null;
        }
        [UnityTest]
        public IEnumerator RenderedStarsVisiblyTwinkleDuringModelPause() {
            var stars = view.transform.Find("WinterNightSky/CatalogStars");
            var material = stars.GetComponent<Renderer>().sharedMaterial;
            int priorLayer = stars.gameObject.layer;
            var go = new GameObject("StarScintillationProbe", typeof(Camera));
            var camera = go.GetComponent<Camera>();
            var target = new RenderTexture(128, 128, 24);
            var pixels = new Texture2D(128, 128, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try {
                stars.gameObject.layer = 30;
                camera.cullingMask = 1 << 30;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
                camera.transform.position = view.Garden.Locomotion.Head.position;
                camera.transform.rotation = Quaternion.LookRotation(sky.NamedStars["Sirius"]);
                // About 21 pixels per degree, without magnifying the star's apparent diameter.
                camera.fieldOfView = 6; camera.farClipPlane = 160; camera.targetTexture = target;
                view.Session.SetPaused(true);
                yield return null;
                float minimum = float.MaxValue, maximum = 0;
                for (int sample = 0; sample < 16; sample++) {
                    sky.AdvanceSky(.2f);
                    camera.Render(); RenderTexture.active = target;
                    pixels.ReadPixels(new Rect(0, 0, 128, 128), 0, 0); pixels.Apply();
                    float light = 0;
                    for (int y = 60; y < 68; y++) for (int x = 60; x < 68; x++) {
                        var color = pixels.GetPixel(x, y); light += color.r + color.g + color.b;
                    }
                    minimum = Mathf.Min(minimum, light); maximum = Mathf.Max(maximum, light);
                }
                Assert.That(minimum, Is.GreaterThan(.05f), "The star must remain visible throughout the cycle.");
                Assert.That(maximum / minimum, Is.GreaterThan(1.5f),
                    "Rendered brightness must visibly change, even with model playback paused.");
                // Compare linear light, including point size/color, at headset-like sampling.
                // Decode the sRGB readback before measuring flux; the twinkle check above
                // deliberately measures display-encoded contrast instead.
                material.SetFloat("_TwinkleStrength", 0);
                var lightByName = new System.Collections.Generic.Dictionary<string, float>();
                foreach (var name in new[] { "Sirius", "Rigel", "Saiph", "Betelgeuse", "Bellatrix" }) {
                    camera.transform.rotation = Quaternion.LookRotation(sky.NamedStars[name]);
                    camera.Render(); RenderTexture.active = target;
                    pixels.ReadPixels(new Rect(0, 0, 128, 128), 0, 0); pixels.Apply();
                    float light = 0;
                    for (int y = 60; y < 68; y++) for (int x = 60; x < 68; x++) {
                        var color = pixels.GetPixel(x, y).linear; light += color.r + color.g + color.b;
                    }
                    lightByName[name] = light;
                }
                material.SetFloat("_TwinkleStrength", 1);
                Assert.That(lightByName["Sirius"] / lightByName["Rigel"], Is.GreaterThan(1.7f), "Sirius must stand out from Rigel.");
                Assert.That(lightByName["Rigel"] / lightByName["Saiph"], Is.GreaterThan(1.8f), "Orion's bright foot must stand out from Saiph.");
                Assert.That(lightByName["Betelgeuse"] / lightByName["Bellatrix"], Is.GreaterThan(1.25f), "The bright warm shoulder must retain its prominence.");
            } finally {
                material.SetFloat("_TwinkleStrength", 1);
                view.Session.SetPaused(false); stars.gameObject.layer = priorLayer;
                RenderTexture.active = previous; camera.targetTexture = null; target.Release();
                Object.Destroy(pixels); Object.Destroy(target); Object.Destroy(go);
            }
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
