using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRExperienceAGB.Presentation;
using Object = UnityEngine.Object;

namespace VRExperienceAGB.Tests
{
    public class HorizonWolvesPlayModeTests
    {
        private OneTreeExperience view;
        private HorizonWolves wildlife;
        private string DirectoryPath => Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "../../artifacts/horizon-wolves"));
        [UnitySetUp]
        public IEnumerator Open()
        {
            yield return LegacyTeachingScene.Load(); yield return null;
            view = Object.FindAnyObjectByType<OneTreeExperience>();
             view.enabled = false;
            view.GetComponent<DesktopTreePreview>().enabled = false;
            wildlife = view.Garden.M5.Atmosphere.Wildlife; wildlife.enabled = false;
            wildlife.SendMessage("OnApplicationFocus", true); wildlife.SendMessage("OnApplicationPause", false);
            view.Garden.Navigation.ShowForest();
        }
        [UnityTest]
        public IEnumerator WolvesAreDistantSharedGeometryWithoutCollidersOrModelChanges()
        {
            var session = view.Session; var model = view.Model; var head = view.Garden.Locomotion.Head;
            var position = head.localPosition; var rotation = head.localRotation;
            wildlife.AdvanceAmbience(60);
            Assert.That(wildlife.Wolves.Count, Is.EqualTo(3));
            Assert.That(wildlife.LastCaller, Is.GreaterThanOrEqualTo(0));
            var root = view.transform.Find("HorizonWildlife");
            Assert.That(root.GetComponentsInChildren<Collider>().Length, Is.Zero);
            Assert.That(wildlife.Wolves[0].localPosition.x,Is.EqualTo(0));
            Assert.That(wildlife.Wolves.Skip(1).All(w => Mathf.Abs(w.position.x) > 20), Is.True);
            Assert.That(wildlife.Wolves.All(w => w.position.z >= 50), Is.True);
            Assert.That(root.Find("CentralRidge"),Is.Not.Null);
            Assert.That(wildlife.Wolves.Select(w => w.GetComponent<MeshFilter>().sharedMesh).Distinct().Count(), Is.EqualTo(1));
            Assert.That(view.Session, Is.SameAs(session)); Assert.That(view.Model, Is.SameAs(model));
            Assert.That(head.localPosition, Is.EqualTo(position)); Assert.That(head.localRotation, Is.EqualTo(rotation));
            view.Garden.Navigation.OpenTree(0); Assert.That(root.gameObject.activeInHierarchy, Is.True);
            yield return null;
        }
        [UnityTest]
        public IEnumerator ArrivalDelaySurvivesInterruptionsAndRestartsOnEveryTreeEntry()
        {
            Assert.That(wildlife.SecondsUntilHowl, Is.InRange(59f, 60f));
            wildlife.AdvanceAmbience(wildlife.SecondsUntilHowl - 1f);
            Assert.That(wildlife.LastCaller, Is.EqualTo(-1));
            Assert.That(wildlife.Visible, Is.False);

            wildlife.SetSoundEnabled(false); wildlife.AdvanceAmbience(100);
            wildlife.SetSoundEnabled(true);
            wildlife.SendMessage("OnApplicationFocus", false); wildlife.AdvanceAmbience(100);
            wildlife.SendMessage("OnApplicationFocus", true);
            wildlife.SendMessage("OnApplicationPause", true); wildlife.AdvanceAmbience(100);
            wildlife.SendMessage("OnApplicationPause", false);
            view.Garden.M5.OpenHelp(); wildlife.AdvanceAmbience(100); view.Garden.M5.CloseHelp();
            Assert.That(wildlife.SecondsUntilHowl, Is.EqualTo(1f).Within(.001f));
            Assert.That(wildlife.LastCaller, Is.EqualTo(-1));
            wildlife.AdvanceAmbience(1);
            Assert.That(wildlife.LastCaller, Is.EqualTo(0));
            Assert.That(wildlife.Visible, Is.True);

            foreach (int tree in new[] { 0, 1, 0 }) {
                view.Garden.Navigation.ShowForest(); view.Garden.Navigation.OpenTree(tree);
                Assert.That(wildlife.SecondsUntilHowl, Is.EqualTo(60f));
                Assert.That(wildlife.Visible, Is.False);
                Assert.That(wildlife.Voices.All(v => !v.isPlaying), Is.True);
                wildlife.AdvanceAmbience(59);
                Assert.That(wildlife.Visible, Is.False);
                view.Garden.Navigation.Refresh();
                Assert.That(wildlife.SecondsUntilHowl, Is.EqualTo(1f), "Refreshing the tree must not restart the timer.");
                wildlife.AdvanceAmbience(1);
                Assert.That(wildlife.Visible, Is.True);
                Assert.That(wildlife.SecondsUntilHowl, Is.InRange(32f, 54f));
            }
            yield return null;
        }
        [UnityTest]
        public IEnumerator ExplicitHowlAlwaysShowsTheCentralWolf()
        {
            wildlife.RequestHowl();
            Assert.That(wildlife.LastCaller,Is.EqualTo(0));
            Assert.That(wildlife.Wolves[0].gameObject.activeInHierarchy,Is.True);
            wildlife.AdvanceAmbience(60);
            wildlife.RequestHowl();
            Assert.That(wildlife.LastCaller,Is.EqualTo(0));
            Assert.That(wildlife.Wolves[0].gameObject.activeInHierarchy,Is.True);
            Assert.That(wildlife.Wolves.Skip(1).All(w=>!w.gameObject.activeSelf),Is.True);
            yield return null;
        }
        [UnityTest]
        public IEnumerator SoundToggleStopsEveryVoiceAndResumesAfterADelay()
        {
            var atmosphere = view.Garden.M5.Atmosphere;
            wildlife.AdvanceAmbience(60); int first = wildlife.LastCaller;
            Assert.That(first, Is.GreaterThanOrEqualTo(0));
            atmosphere.SoundEnabled = false; wildlife.AdvanceAmbience(100);
            Assert.That(wildlife.SoundEnabled, Is.False);
            Assert.That(wildlife.Voices.All(v => v.mute && !v.isPlaying), Is.True);
            Assert.That(wildlife.LastCaller, Is.EqualTo(first));
            atmosphere.SoundEnabled = true; wildlife.AdvanceAmbience(1);
            Assert.That(wildlife.LastCaller, Is.EqualTo(first)); Assert.That(wildlife.SecondsUntilHowl, Is.GreaterThan(0));
            wildlife.AdvanceAmbience(12); Assert.That(wildlife.LastCaller, Is.Not.EqualTo(first));
            Assert.That(wildlife.SecondsUntilHowl, Is.InRange(32f, 54f));
            Assert.That(wildlife.Voices.All(v => v.spatialBlend == 1 && v.dopplerLevel == 0 && !v.loop), Is.True);
            view.Garden.M5.OpenHelp(); int caller = wildlife.LastCaller; wildlife.AdvanceAmbience(100);
            Assert.That(wildlife.LastCaller, Is.EqualTo(caller)); Assert.That(wildlife.Voices.All(v => !v.isPlaying), Is.True);
            view.Garden.M5.CloseHelp();
            wildlife.SendMessage("OnApplicationPause", true); wildlife.AdvanceAmbience(100);
            Assert.That(wildlife.LastCaller, Is.EqualTo(caller));
            wildlife.SendMessage("OnApplicationPause", false); wildlife.AdvanceAmbience(1);
            Assert.That(wildlife.SecondsUntilHowl, Is.GreaterThan(0));
            yield return null;
        }
        [UnityTest]
        public IEnumerator ComparisonAndModelReloadKeepOneWildlifeLayerAndRespectMute()
        {
            view.Garden.M5.Atmosphere.SoundEnabled = false;
            view.Garden.M5.Activate(M5Action.CompareProfiles);
            wildlife.AdvanceAmbience(60);
            Assert.That(wildlife.LastCaller, Is.EqualTo(-1));
            Assert.That(Object.FindObjectsByType<HorizonWolves>().Length, Is.EqualTo(1));
            view.EndComparison(); wildlife.AdvanceAmbience(60);
            Assert.That(wildlife.Wolves.Count, Is.EqualTo(3)); Assert.That(wildlife.SoundEnabled, Is.False);
            Assert.That(view.LoadAgbStructure(view.modelFile.text, "Original export").IsSuccess, Is.True);
            yield return null;
            Assert.That(Object.FindObjectsByType<HorizonWolves>().Length, Is.EqualTo(1));
            Assert.That(wildlife.Voices.All(v => v.mute), Is.True);
        }
        [UnityTest]
        public IEnumerator RecordedHowlLoadsWithBoundedMonoSamplesAndSoftBoundaries()
        {
            var clip = wildlife.HowlClip; var samples = new float[clip.samples]; clip.GetData(samples, 0);
            Assert.That(clip.channels, Is.EqualTo(1)); Assert.That(clip.frequency, Is.EqualTo(22050));
            Assert.That(clip.length, Is.EqualTo(4.2f).Within(.01));
            Assert.That(samples.All(float.IsFinite), Is.True);
            Assert.That(samples.Max(x => Mathf.Abs(x)), Is.InRange(.01f, 1f));
            Assert.That(Mathf.Abs(samples.First()), Is.LessThan(.002f)); Assert.That(Mathf.Abs(samples.Last()), Is.LessThan(.002f));
            Assert.That(clip, Is.SameAs(Resources.Load<AudioClip>("WolfHowl")));
            Assert.That(wildlife.Voices.All(v => v.pitch == 1f), Is.True);
            Assert.That(samples.Take(128).Max(x => Mathf.Abs(x)), Is.LessThan(.03f));
            Directory.CreateDirectory(DirectoryPath);
            using (var writer = new BinaryWriter(File.Create(Path.Combine(DirectoryPath, "recorded-wolf-howl.wav"))))
            {
                int bytes = samples.Length * 2;
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + bytes);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1); writer.Write((short)1);
                writer.Write(clip.frequency); writer.Write(clip.frequency * 2); writer.Write((short)2); writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(bytes);
                foreach (float sample in samples) writer.Write((short)(sample * short.MaxValue));
            }
            yield return null;
        }
        [UnityTest]
        public IEnumerator CaptureHorizonAndWolfSilhouetteWithoutMovingTheObserver()
        {
            wildlife.AdvanceAmbience(1); yield return null;
            Directory.CreateDirectory(DirectoryPath);
            var head = view.Garden.Locomotion.Head;
            Capture(head.position, new Vector3(-25, 10, 51), 62, "horizon.png");
            Capture(new Vector3(-23, 10, 39), new Vector3(-25, 10, 51), 38, "wolf-detail.png");
            var renderers = view.transform.Find("HorizonWildlife").GetComponentsInChildren<MeshFilter>();
            File.WriteAllText(Path.Combine(DirectoryPath, "geometry.txt"), "Rendered triangles: " + renderers.Sum(r => r.sharedMesh.triangles.Length / 3) + "\n");
        }
        private void Capture(Vector3 position, Vector3 target, float fov, string name)
        {
            var cameraObject = new GameObject("WildlifeVerificationCamera"); var camera = cameraObject.AddComponent<Camera>();
            camera.CopyFrom(view.Garden.Locomotion.Head.GetComponent<Camera>()); camera.enabled = false;
            camera.transform.position = position; camera.transform.LookAt(target); camera.fieldOfView = fov;
            camera.stereoTargetEye = StereoTargetEyeMask.None;
            var texture = new RenderTexture(1600, 1000, 24); var previous = RenderTexture.active;
            camera.targetTexture = texture; camera.Render(); RenderTexture.active = texture;
            var image = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); image.Apply();
            File.WriteAllBytes(Path.Combine(DirectoryPath, name), image.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = previous; texture.Release();
            Object.Destroy(image); Object.Destroy(texture); Object.Destroy(cameraObject);
        }
    }
}
