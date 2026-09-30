using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRExperienceAGB.Presentation;

namespace VRExperienceAGB.Editor
{
    public static class M4SceneUpgrade
    {
        public static string Apply()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play mode first.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != OneTreeSceneBuilder.ScenePath) throw new System.InvalidOperationException("Open the teaching scene first.");
            var view = Object.FindAnyObjectByType<OneTreeExperience>();
            var template = view.controls.Single(c => c.action == TreeAction.Manual);
            Add(view, template, TreeAction.LargerEnsemble, "Larger ensemble: 24 trees");
            Add(view, template, TreeAction.CancelEdit, "Cancel edit");
            Add(view, template, TreeAction.SetMissing, "Set explicit Missing");
            Add(view, template, TreeAction.TourDetail, "Choose short tour");
            Add(view, template, TreeAction.GroupRemaining, "Explain remaining group");
            Add(view, template, TreeAction.Result, "Review contributions");
            Add(view, template, TreeAction.NextLedgerPage, "Next ledger page");
            Add(view, template, TreeAction.CloseResult, "Return to tour");
            Add(view, template, TreeAction.ReviseChoice, "Revise earlier choice");
            Add(view, template, TreeAction.ContinueFree, "Continue free exploration");
            var secondary = view.controls.Where(c => OneTreeExperience.IsSecondary(c.action)).ToArray();
            for (int i = 0; i < secondary.Length; i++) Place(secondary[i], (i % 3 - 1) * 390, 870 - (i / 3) * 105);
            var edits = new[] { TreeAction.NextFeature, TreeAction.DecreaseValue, TreeAction.IncreaseValue, TreeAction.SetMissing,
                TreeAction.RestoreProfile, TreeAction.CloseEdit, TreeAction.CancelEdit };
            for (int i = 0; i < edits.Length; i++) Place(view.controls.Single(c => c.action == edits[i]), (i % 3 - 1) * 390, 790 - (i / 3) * 115);
            view.controls.Single(c => c.action == TreeAction.CloseEdit).label.text = "Apply change";
            Place(view.controls.Single(c => c.action == TreeAction.NextLedgerPage), -200, 400);
            Place(view.controls.Single(c => c.action == TreeAction.CloseResult), 200, 400);
            if (view.ledgerText == null)
            {
                view.ledgerText = Object.Instantiate(view.feedback, view.feedback.transform.parent);
                view.ledgerText.name = "ContributionLedger";
            }
            view.ledgerText.rectTransform.anchoredPosition = new Vector2(0, 760);
            view.ledgerText.rectTransform.sizeDelta = new Vector2(1180, 580);
            view.ledgerText.fontSize = 30; view.ledgerText.enableAutoSizing = false;
            view.ledgerText.alignment = TextAlignmentOptions.TopLeft; view.ledgerText.raycastTarget = false;
            view.ledgerText.gameObject.SetActive(false);
            view.feedback.rectTransform.anchoredPosition = new Vector2(0, 300);
            view.feedback.rectTransform.sizeDelta = new Vector2(1180, 160);
            view.feedback.fontSize = 22;
            view.score.rectTransform.sizeDelta = new Vector2(1100, 155);
            view.score.fontSize = 22;
            var backdrop = (RectTransform)view.menuBackdrop.transform;
            backdrop.anchoredPosition = new Vector2(0, 675); backdrop.sizeDelta = new Vector2(1280, 800);
            view.routeHistory.rectTransform.anchoredPosition = new Vector2(0, 190);
            view.routeHistory.rectTransform.sizeDelta = new Vector2(1180, 60);
            foreach (var item in view.presentationRoot.GetComponentsInChildren<Transform>(true)) item.gameObject.isStatic = false;
            EditorUtility.SetDirty(view); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            return "M4 ledger, tours, edit and contradiction controls saved.";
        }
        private static void Add(OneTreeExperience view, TreeActionButton template, TreeAction action, string text)
        {
            var control = view.controls.SingleOrDefault(c => c.action == action);
            if (control == null)
            {
                control = Object.Instantiate(template, template.transform.parent);
                control.name = action.ToString(); control.action = action; control.experience = view;
                view.controls = view.controls.Concat(new[] { control }).ToArray();
            }
            control.label.text = text; control.gameObject.SetActive(false);
        }
        private static void Place(TreeActionButton control, float x, float y)
        {
            var rt = (RectTransform)control.transform;
            rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(360, 80);
            control.label.fontSize = 25;
        }
    }
}
