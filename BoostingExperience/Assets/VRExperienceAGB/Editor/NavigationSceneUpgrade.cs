using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRExperienceAGB.Presentation;

namespace VRExperienceAGB.Editor
{
    public static class NavigationSceneUpgrade
    {
        public static string Apply()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play mode first.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != OneTreeSceneBuilder.ScenePath) throw new System.InvalidOperationException("Open the teaching scene first.");
            var view = Object.FindAnyObjectByType<OneTreeExperience>();
            ((RectTransform)view.status.transform.parent).sizeDelta = new Vector2(1600, 2400);
            var template = view.controls.Single(c => c.action == TreeAction.Manual);
            var actions = new[] { TreeAction.DeepExample, TreeAction.TreeMap, TreeAction.PreviousTree, TreeAction.NextTree, TreeAction.EditProfile };
            var labels = new[] { "Deep tree: 8 levels", "Tree overview", "Previous tree", "Next tree", "Try a change" };
            var edits = new[] { TreeAction.NextFeature, TreeAction.DecreaseValue, TreeAction.IncreaseValue, TreeAction.RestoreProfile, TreeAction.CloseEdit };
            var editLabels = new[] { "Next feature", "Decrease / previous", "Increase / next", "Restore original", "Done editing" };
            for (int i = 0; i < actions.Length; i++)
            {
                Add(view, template, actions[i], labels[i], (i - 2) * 255 - 160, 1050);
                Add(view, template, edits[i], editLabels[i], (i - 2) * 255 - 160, 1050);
                view.controls.Single(c => c.action == edits[i]).gameObject.SetActive(false);
            }
            if (view.routeHistory == null)
            {
                view.routeHistory = Object.Instantiate(view.feedback, view.feedback.transform.parent);
                view.routeHistory.name = "RouteHistory";
            }
            var rt = view.routeHistory.rectTransform; rt.anchoredPosition = new Vector2(0, 950);
            rt.sizeDelta = new Vector2(1150, 80); view.routeHistory.fontSize = 22; view.routeHistory.raycastTarget = false;
            Add(view, template, TreeAction.Menu, "Menu", 470, -110);
            Add(view, template, TreeAction.InspectNodes, "Inspect nodes", 0, 0);
            Add(view, template, TreeAction.NextNode, "Next visible node", -200, 560);
            Add(view, template, TreeAction.CloseInspect, "Done inspecting", 200, 560);
            view.controls.Single(c => c.action == TreeAction.NextNode).gameObject.SetActive(false);
            view.controls.Single(c => c.action == TreeAction.CloseInspect).gameObject.SetActive(false);
            var secondary = view.controls.Where(c => OneTreeExperience.IsSecondary(c.action)).ToArray();
            for (int i = 0; i < secondary.Length; i++)
            {
                var rect = (RectTransform)secondary[i].transform;
                rect.anchoredPosition = new Vector2((i % 3 - 1) * 390, 790 - (i / 3) * 115);
                rect.sizeDelta = new Vector2(360, 80); secondary[i].label.fontSize = 25;
                secondary[i].gameObject.SetActive(false);
            }
            for (int i = 0; i < edits.Length; i++)
            {
                var rect = (RectTransform)view.controls.Single(c => c.action == edits[i]).transform;
                rect.anchoredPosition = new Vector2((i % 3 - 1) * 390, 790 - (i / 3) * 115);
                rect.sizeDelta = new Vector2(360, 80);
            }
            rt.anchoredPosition = new Vector2(0, 420);
            if (view.menuBackdrop == null)
            {
                view.menuBackdrop = new GameObject("MenuBackdrop", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                view.menuBackdrop.transform.SetParent(template.transform.parent, false);
            }
            var backdrop = (RectTransform)view.menuBackdrop.transform;
            backdrop.anchorMin = backdrop.anchorMax = new Vector2(.5f,.5f);
            backdrop.anchoredPosition = new Vector2(0, 620); backdrop.sizeDelta = new Vector2(1280, 580);
            backdrop.SetAsFirstSibling();
            view.menuBackdrop.GetComponent<UnityEngine.UI.Image>().color = new Color(.025f,.055f,.085f,.96f);
            view.menuBackdrop.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            view.menuBackdrop.SetActive(false); view.routeHistory.gameObject.SetActive(false);
            foreach (var control in view.controls)
            {
                if (control.action == TreeAction.TrueBranch || control.action == TreeAction.FalseBranch)
                {
                    var rect = (RectTransform)control.transform; rect.sizeDelta = new Vector2(210, 76);
                    control.GetComponent<UnityEngine.UI.Image>().color = new Color(.10f,.075f,.045f,.12f);
                    control.GetComponent<UnityEngine.UI.Outline>().effectColor = new Color(.72f,.54f,.30f,.15f);
                    control.label.color = new Color(1f,.86f,.59f); control.label.fontSize = 24;
                    control.label.enableAutoSizing = true; control.label.fontSizeMin = 16; control.label.fontSizeMax = 24;
                }
                else if (control.action == TreeAction.Manual || control.action == TreeAction.Profile)
                    control.label.color = new Color(.71f,.91f,1);
            }
            foreach (var name in new[] { "TrueChoiceStone", "FalseChoiceStone" })
            {
                var stone = view.presentationRoot.Find(name);
                if (stone != null) stone.localScale = new Vector3(.60f,.26f,.23f);
            }
            view.explanation.color = new Color(1,.90f,.69f);
            foreach (var item in view.presentationRoot.GetComponentsInChildren<Transform>(true)) item.gameObject.isStatic = false;
            EditorUtility.SetDirty(view); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            M4SceneUpgrade.Apply();
            return "Navigation and M4 controls saved.";
        }
        private static void Add(OneTreeExperience view, TreeActionButton template, TreeAction action, string text, float x, float y)
        {
            var control = view.controls.FirstOrDefault(c => c.action == action);
            if (control == null)
            {
                control = Object.Instantiate(template, template.transform.parent); control.name = action.ToString();
                control.action = action; control.experience = view;
                view.controls = view.controls.Concat(new[] { control }).ToArray();
            }
            control.label.text = text; control.label.fontSize = 22;
            var rt = (RectTransform)control.transform; rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(240, 70);
        }
    }
}
