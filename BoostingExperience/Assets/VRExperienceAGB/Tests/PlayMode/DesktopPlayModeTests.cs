using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRExperienceAGB.Presentation;

namespace VRExperienceAGB.Tests
{
    public class DesktopPlayModeTests
    {
        private OneTreeExperience view;
        private DesktopExperienceInput input;
        [UnitySetUp] public IEnumerator Open()
        {
            yield return SceneManager.LoadSceneAsync("OneTreeLearning"); yield return null;
            view = Object.FindAnyObjectByType<OneTreeExperience>();
            Assert.That(view.Ready, Is.True);
            view.enabled = false; view.Director.enabled = false;
            view.SendMessage("OnApplicationFocus", true); view.SendMessage("OnApplicationPause", false);
            input = view.GetComponent<DesktopExperienceInput>() ?? view.gameObject.AddComponent<DesktopExperienceInput>();
            view.Director.Activate("forest"); yield return null;
        }
        [UnityTest] public IEnumerator DesktopCameraAndAllInteractiveCanvasesAgree()
        {
            var observer = view.GetComponent<DesktopTreePreview>();
            Assert.That(observer.Active, Is.True);
            Assert.That(observer.trackedRig.activeSelf, Is.False);
            Assert.That(view.Garden.Locomotion.Head, Is.EqualTo(observer.previewCamera.transform));
            view.Director.OpenMenu(); yield return null;
            foreach (var canvas in view.GetComponentsInChildren<Canvas>())
                if (canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() != null)
                    Assert.That(canvas.worldCamera, Is.EqualTo(observer.previewCamera), canvas.name);
        }
        [UnityTest] public IEnumerator WalkingRespectsMenuAndSuspensionWithoutChangingModel()
        {
            var movement = view.Garden.Locomotion;
            var origin = movement.Origin.position; var model = view.Model;
            input.HandleMovement(Vector2.up, .1f);
            Assert.That(movement.Origin.position.z, Is.GreaterThan(origin.z));
            origin = movement.Origin.position;
            view.Director.OpenMenu(); input.HandleMovement(Vector2.up, .1f);
            Assert.That(movement.Origin.position, Is.EqualTo(origin));
            view.Director.CloseMenu(); view.SendMessage("OnApplicationFocus", false);
            input.HandleMovement(Vector2.up, .1f);
            Assert.That(movement.Origin.position, Is.EqualTo(origin));
            Assert.That(view.Model, Is.SameAs(model));
            Assert.That(view.Ensemble.CompletedCount, Is.Zero);
            yield return null;
        }
        [UnityTest] public IEnumerator TableKeysManipulateTableAndPreserveObserverAndFullModel()
        {
            view.Garden.Navigation.ShowDiorama();
            var table = view.Garden.M5.Diorama;
            var origin = view.Garden.Locomotion.Origin.position;
            var position = table.ModelRoot.position;
            int count = view.Model.Trees.Count;
            input.HandleMovement(Vector2.one, .1f);
            Assert.That(table.Yaw, Is.GreaterThan(0));
            Assert.That(table.ModelRoot.position, Is.Not.EqualTo(position));
            Assert.That(view.Garden.Locomotion.Origin.position, Is.EqualTo(origin));
            Assert.That(view.Ensemble.Trees.Count, Is.EqualTo(count));
            view.Director.OpenMenu(); var yaw = table.Yaw;
            input.HandleMovement(Vector2.right, .1f);
            Assert.That(table.Yaw, Is.EqualTo(yaw));
            yield return null;
        }
        [UnityTest] public IEnumerator PhysicalTextEntryUsesSearchDraftAndCanCancel()
        {
            var extensions = view.Garden.M5.Extensions;
            var feature = view.Model.Trees.SelectMany(t => t.Nodes).OfType<VRExperienceAGB.Domain.SplitNode>().First().FeatureId;
            extensions.Open(); extensions.Activate(M5Action.SearchFilter);
            Assert.That(input.TextEntryActive, Is.True);
            extensions.AppendDesktopText(feature);
            extensions.Activate(M5Action.ReviewTextDone);
            Assert.That(input.TextEntryActive, Is.False);
            Assert.That(extensions.Feature, Is.EqualTo(feature));
            extensions.Activate(M5Action.SearchFilter);
            extensions.AppendDesktopText("__cancelled__"); extensions.CancelTextEntry();
            Assert.That(extensions.Feature, Is.EqualTo(feature));
            yield return null;
        }
    }
}
