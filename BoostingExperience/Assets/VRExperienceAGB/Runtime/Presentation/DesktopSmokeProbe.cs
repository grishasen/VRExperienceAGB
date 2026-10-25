using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace VRExperienceAGB.Presentation
{
    /// <summary>Opt-in acceptance probe for the actual development player, never the Quest rig.</summary>
    public sealed class DesktopSmokeProbe : MonoBehaviour
    {
        [Serializable] private sealed class Report
        {
            public bool passed;
            public string platform, graphics;
            public int trees, frames;
            public List<string> checks = new List<string>();
            public List<string> errors = new List<string>();
        }
        private string folder;
        private readonly Report report = new Report();
        private OneTreeExperience view;
        private DesktopTreePreview observer;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            if (UnityEngine.Application.isEditor || !Debug.isDebugBuild || UnityEngine.Application.platform != RuntimePlatform.OSXPlayer) return;
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-desktop-smoke");
            if (index < 0 || index + 1 == args.Length) return;
            var probe = new GameObject("DesktopAcceptanceProbe").AddComponent<DesktopSmokeProbe>();
            probe.folder = Path.GetFullPath(args[index + 1]); Directory.CreateDirectory(probe.folder);
        }
        private IEnumerator Start()
        {
            UnityEngine.Application.logMessageReceived += Log;
            var work = Verify();
            while (true)
            {
                object next;
                try { if (!work.MoveNext()) break; next = work.Current; }
                catch (Exception error) { report.errors.Add(error.ToString()); break; }
                yield return next;
            }
            report.frames = Time.frameCount; report.passed = report.errors.Count == 0;
            File.WriteAllText(Path.Combine(folder, "result.json"), JsonUtility.ToJson(report, true));
            UnityEngine.Application.logMessageReceived -= Log;
            UnityEngine.Application.Quit(report.passed ? 0 : 1);
        }
        private void Log(string message, string stack, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) report.errors.Add(message); }
        private void Require(bool condition, string description)
        { if (!condition) throw new InvalidOperationException(description); report.checks.Add(description); }
        private void Capture(string name) => ScreenCapture.CaptureScreenshot(Path.Combine(folder, name + ".png"));
        private IEnumerator Verify()
        {
            yield return new WaitForSecondsRealtime(2);
            view = FindAnyObjectByType<OneTreeExperience>(); observer = view.GetComponent<DesktopTreePreview>();
            report.platform = UnityEngine.Application.platform.ToString(); report.graphics = SystemInfo.graphicsDeviceType.ToString(); report.trees = view.Model.Trees.Count;
            Require(view.Ready && observer.Active && !observer.trackedRig.activeSelf, "Standalone camera active; tracked rig inactive");
            Require(view.GetComponent<DesktopExperienceInput>() != null, "Desktop input installed in player");
            int frames = Time.frameCount; yield return new WaitForSecondsRealtime(.3f);
            Require(Time.frameCount > frames + 2, "Player frames advance");
            Capture("01-menu"); yield return new WaitForSecondsRealtime(.5f);
            var forest = view.GetComponentsInChildren<ExperienceMenuTarget>().Single(t => t.Command == "forest");
            var screen = (Vector2)observer.previewCamera.WorldToScreenPoint(forest.transform.position);
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = screen }); yield return null;
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = screen }.WithButton(MouseButton.Left)); yield return null;
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = screen }); yield return null;
            Require(!view.Director.MenuVisible && view.Garden.Navigation.Page == NavigationPage.Forest, "Mouse click enters forest through canvas routing");
            Capture("02-forest"); yield return new WaitForSecondsRealtime(.5f);
            var origin = view.Garden.Locomotion.Origin.position;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.W)); yield return new WaitForSecondsRealtime(.25f);
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); yield return null;
            Require(view.Garden.Locomotion.Origin.position != origin, "Keyboard W walks in player");
            view.Director.OpenMenu(); origin = view.Garden.Locomotion.Origin.position;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.W)); yield return new WaitForSecondsRealtime(.15f);
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); yield return null;
            Require(view.Garden.Locomotion.Origin.position == origin, "Menu blocks walking");
            view.Director.CloseMenu(); view.Garden.Navigation.OpenTree(0); yield return null;
            view.Execute(TreeAction.TrueBranch, view.Session.State.Revision, view.Session.State.NodeId);
            view.Advance(2); yield return null;
            Require(view.Session.State.Decisions.Count == 1, "Manual branch exploration preserved");
            Capture("03-tree"); yield return new WaitForSecondsRealtime(.5f);
            view.Director.StartPlayback(false); yield return null;
            Require(view.Director.Playback.A.Trees.Count == report.trees, "Prepared profile evaluates every tree");
            view.Director.StartPlayback(true); yield return null;
            Require(view.Director.Playback.B.Trees.Count == report.trees, "Comparison evaluates every tree for both profiles");
            double scoreA = view.Director.Playback.A.RawScore, scoreB = view.Director.Playback.B.RawScore;
            view.Director.Playback.FinishAll(); view.Director.ShowResults(true); yield return null;
            origin = view.Garden.Locomotion.Origin.position;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.D)); yield return new WaitForSecondsRealtime(.2f);
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); yield return null;
            Require(view.Garden.M5.Diorama.Yaw > 0 && view.Garden.Locomotion.Origin.position == origin, "Table keyboard rotates model and preserves observer");
            Require(view.Director.Playback.PresentedScore() == scoreA && view.Director.Playback.PresentedScore(true) == scoreB, "Complete A/B scores survive Table View");
            Capture("04-table-results"); yield return new WaitForSecondsRealtime(.5f);
            var extensions = view.Garden.M5.Extensions;
            var feature = view.Model.Trees.SelectMany(t => t.Nodes).OfType<VRExperienceAGB.Domain.SplitNode>().First().FeatureId;
            extensions.Open(); extensions.Activate(M5Action.SearchFilter);
            foreach (char character in feature) InputSystem.QueueTextEvent(Keyboard.current, character);
            yield return null;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.Enter)); yield return null;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); yield return null;
            Require(!extensions.TextEntryActive && extensions.Feature == feature, "Keyboard text and Enter apply predictor search");
            extensions.Close();
            view.Director.OpenMenu("library"); Capture("05-library"); yield return new WaitForSecondsRealtime(.5f);
            string packagePath = Path.Combine(folder, "synthetic-package.zip");
            int expectedTrees = VRExperienceAGB.Import.NormalizedModelJson.ReadModel(view.comparisonModelFile.text).Value.Trees.Count;
            using (var archive = ZipFile.Open(packagePath, ZipArchiveMode.Create))
            {
                using (var writer = new StreamWriter(archive.CreateEntry("model.json").Open())) writer.Write(view.comparisonModelFile.text);
                using (var writer = new StreamWriter(archive.CreateEntry("profiles.json").Open())) writer.Write(view.comparisonProfilesFile.text);
            }
            view.Director.Library.ImportPath(packagePath, false, true);
            float deadline = Time.realtimeSinceStartup + 20;
            while (view.Director.Library.Busy && Time.realtimeSinceStartup < deadline) yield return null;
            Require(!view.Director.Library.Busy && view.Model.Trees.Count == expectedTrees && view.AvailableProfiles.Profiles.Count > 0,
                "Standalone ZIP import loads synthetic model and profiles");
        }
    }
}
