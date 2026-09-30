using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using VRExperienceAGB.Application;

namespace VRExperienceAGB.Presentation
{
    /// <summary>Optional views, guidance and recovery around the same accepted teaching session.</summary>
    public sealed class M5ForestPresentation : MonoBehaviour
    {
        public ForestGardenView Garden { get; private set; }
        public OneTreeExperience View => Garden.Experience;
        public ForestDioramaView Diorama { get; private set; }
        public ForestAtmosphere Atmosphere { get; private set; }
        public bool HelpOpen { get; private set; }
        public int HelpPage { get; private set; }
        public long Revision { get; private set; }
        private Transform root;
        private Canvas help, overviewTools, treeTools, focusStatus;
        private TMP_Text helpText, focusText;
        private readonly List<M5PointerTarget> summaries = new List<M5PointerTarget>();
        private TreeSession helpSession;
        private bool pausedBeforeHelp;
        private static readonly string[] Guidance =
        {
            "1 / 4 · POINT AND SELECT\n\nPoint either controller at a button or a pine crown. Press its trigger to select. Pointing only shows information.\n\nIn the garden: left stick to walk, right stick to snap turn. You can also select a path marker to teleport.",
            "2 / 4 · EXPLORE OR FOLLOW\n\nExplore branches: choose TRUE or FALSE yourself. A route score describes your choices; it is not a customer probability.\n\nPrepared profiles follow their feature values deterministically. The verified teaching profiles are synthetic. The default export supports structure and manual exploration only.",
            "3 / 4 · READ THE FOREST\n\nPine height shows tree depth. Crown width shows leaf count.\nTurquoise + ring: positive reached-leaf contribution. Orange − ring: negative. Neutral: zero or unreached. These values are raw contributions.\n\nTabletop view shows every tree. Smaller, Larger and Rotate change the model view; your head remains tracked.",
            "4 / 4 · RETURN AND RECOVER\n\nPress A to open the tree menu. Back undoes a decision; Restart tree clears that tree's route. Return keeps your progress.\n\nOpen a hidden-subtree summary to inspect it, then choose Parent view or Your current decision to return.\n\nChoose seated or standing layout. Pause/Resume and Help remain in the tree menu. You can skip this guide and reopen it anytime."
        };
        public void Configure(ForestGardenView garden)
        {
            Garden = garden;
            root = new GameObject("M5ForestPresentation").transform; root.SetParent(View.transform, false);
            Diorama = new ForestDioramaView(this, root);
            Atmosphere = gameObject.AddComponent<ForestAtmosphere>(); Atmosphere.Configure(garden);
            help = Panel("VisitorGuide", new Vector2(1000, 650), root);
            helpText = garden.Text(help.transform, "Guidance", "", new Vector2(0, 65), new Vector2(900, 430), 29);
            Button(help, "Previous", new Vector2(-315, -232), new Vector2(250, 62), M5Action.HelpPrevious);
            Button(help, "Next", new Vector2(0, -232), new Vector2(250, 62), M5Action.HelpNext);
            Button(help, "Skip / Close", new Vector2(315, -232), new Vector2(250, 62), M5Action.CloseHelp);
            help.gameObject.SetActive(false);

            overviewTools = Panel("ForestViewTools", new Vector2(660, 205), root);
            Button(overviewTools, "Tabletop view", new Vector2(-158, 53), new Vector2(300, 58), M5Action.Diorama);
            Button(overviewTools, "Help / legend", new Vector2(158, 53), new Vector2(300, 58), M5Action.Help);
            Button(overviewTools, "Entrance", new Vector2(-158, -25), new Vector2(300, 58), M5Action.Entrance);
            Button(overviewTools, "Sound on / off", new Vector2(158, -25), new Vector2(300, 58), M5Action.ToggleAudio);
            overviewTools.gameObject.SetActive(false);

            treeTools = Panel("TreeViewTools", new Vector2(660, 540), View.presentationRoot);
            treeTools.transform.localPosition = new Vector3(-1.38f, 1.05f, 3.22f);
            garden.Text(treeTools.transform, "Title", "VIEW AND HELP", new Vector2(0, 220), new Vector2(620, 48), 27);
            Button(treeTools, "Pause / Resume", new Vector2(0, 145), new Vector2(590, 60), M5Action.Pause);
            Button(treeTools, "Your current decision", new Vector2(0, 65), new Vector2(590, 60), M5Action.FocusCurrent);
            Button(treeTools, "Parent view", new Vector2(0, -15), new Vector2(590, 60), M5Action.FocusParent);
            Button(treeTools, "Help / legend", new Vector2(0, -95), new Vector2(590, 60), M5Action.Help);
            Button(treeTools, "Sound on / off", new Vector2(0, -175), new Vector2(590, 60), M5Action.ToggleAudio);
            treeTools.gameObject.SetActive(false);

            focusStatus = Panel("FocusContext", new Vector2(680, 240), View.presentationRoot);
            focusStatus.transform.localPosition = new Vector3(1.40f, 1.08f, 3.22f);
            focusText = garden.Text(focusStatus.transform, "Context", "", new Vector2(0, 45), new Vector2(640, 128), 23);
            Button(focusStatus, "Your current decision", new Vector2(0, -75), new Vector2(590, 56), M5Action.FocusCurrent);
            focusStatus.gameObject.SetActive(false);
            foreach (var slot in View.nodeViews)
            {
                var button = Button(slot.title.GetComponentInParent<Canvas>(), "", new Vector2(0, -112), new Vector2(280, 46), M5Action.FocusNode, 0, 18);
                summaries.Add(button.GetComponent<M5PointerTarget>()); button.SetActive(false);
            }
        }
        internal Canvas Panel(string name, Vector2 size, Transform parent)
        {
            var canvas = Garden.CanvasAt(name, Vector3.zero, size, .001f, parent);
            var image = canvas.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(.025f, .065f, .085f, .97f); image.raycastTarget = true;
            return canvas;
        }
        internal GameObject Button(Canvas canvas, string text, Vector2 position, Vector2 size, M5Action action, int index = 0, int fontSize = 25)
        {
            var go = new GameObject(action + "-" + index, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button), typeof(M5PointerTarget));
            go.transform.SetParent(canvas.transform, false);
            var rect = (RectTransform)go.transform; rect.sizeDelta = size; rect.anchoredPosition = position;
            go.GetComponent<UnityEngine.UI.Image>().color = new Color(.045f, .17f, .21f, .98f);
            go.GetComponent<UnityEngine.UI.Button>().navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            var pointer = go.GetComponent<M5PointerTarget>(); pointer.presentation = this; pointer.action = action; pointer.index = index;
            Garden.Text(go.transform, "Label", text, Vector2.zero, size - new Vector2(14, 8), fontSize);
            return go;
        }
        public void Activate(M5Action action, int index = 0, string nodeId = null)
        {
            if (!View.Ready) return;
            if (HelpOpen && action != M5Action.HelpNext && action != M5Action.HelpPrevious && action != M5Action.CloseHelp) return;
            bool tree = Garden.Navigation?.Page == NavigationPage.Tree;
            bool table = Garden.Navigation?.Page == NavigationPage.Diorama;
            Revision++; Garden.Navigation?.InvalidatePointerPresses(); View.NameTooltip?.Hide();
            switch (action)
            {
                case M5Action.Help: OpenHelp(); break;
                case M5Action.HelpNext: HelpPage = Mathf.Min(Guidance.Length - 1, HelpPage + 1); break;
                case M5Action.HelpPrevious: HelpPage = Mathf.Max(0, HelpPage - 1); break;
                case M5Action.CloseHelp: CloseHelp(); break;
                case M5Action.Diorama: Diorama.Place(); Garden.Navigation.ShowDiorama(); break;
                case M5Action.Forest: Garden.Navigation.ShowForest(); break;
                case M5Action.SelectTree: if (table) View.SelectGardenTree(index); break;
                case M5Action.PreviousTree: if (table) View.SelectGardenTree(Mathf.Max(0, View.Ensemble.Index - 1)); break;
                case M5Action.NextTree: if (table) View.SelectGardenTree(Mathf.Min(Garden.PlotCount - 1, View.Ensemble.Index + 1)); break;
                case M5Action.PreviousTablePage: if (table) View.SelectGardenTree(Mathf.Max(0, (View.Ensemble.Index / ForestDioramaView.TreesPerPage - 1) * ForestDioramaView.TreesPerPage)); break;
                case M5Action.NextTablePage: if (table) View.SelectGardenTree(Mathf.Min(Garden.PlotCount - 1, (View.Ensemble.Index / ForestDioramaView.TreesPerPage + 1) * ForestDioramaView.TreesPerPage)); break;
                case M5Action.EnterTree: if (table) Garden.Navigation.OpenTree(View.Ensemble.Index); break;
                case M5Action.Smaller: if (table) Diorama.ChangeScale(-.1f); break;
                case M5Action.Larger: if (table) Diorama.ChangeScale(.1f); break;
                case M5Action.RotateLeft: if (table) Diorama.Rotate(-30); break;
                case M5Action.RotateRight: if (table) Diorama.Rotate(30); break;
                case M5Action.ResetView: if (table) Diorama.Reset(); break;
                case M5Action.FocusNode:
                    if (tree && View.Session.State.PendingDecision == null && View.FocusedView.Focus(nodeId)) View.Refresh();
                    break;
                case M5Action.FocusParent: if (tree) { View.FocusedView.ParentFocus(); View.Refresh(); } break;
                case M5Action.FocusCurrent: if (tree) { View.FocusedView.CurrentFocus(); View.Refresh(); } break;
                case M5Action.Pause:
                    if (tree) { Garden.Navigation.ToggleExplicitPause(); View.Refresh(); } break;
                case M5Action.Entrance: if (Garden.Navigation?.Page == NavigationPage.Forest) { Garden.Locomotion.ReturnToEntrance(); Garden.Navigation.Place(); PlaceOverviewTools(); } break;
                case M5Action.ToggleAudio: Atmosphere.SoundEnabled = !Atmosphere.SoundEnabled; break;
            }
            Refresh();
        }
        public void OpenHelp()
        {
            if (HelpOpen) return;
            Garden.Navigation?.InvalidatePointerPresses();
            helpSession = View.Session; pausedBeforeHelp = helpSession.State.Paused; helpSession.SetPaused(true);
            HelpOpen = true; HelpPage = 0; Revision++; Place(help, 1.15f, -.12f, 0); Refresh();
        }
        public void CloseHelp()
        {
            if (!HelpOpen) return;
            Garden.Navigation?.InvalidatePointerPresses();
            HelpOpen = false; Revision++;
            if (helpSession == View.Session) helpSession.SetPaused(pausedBeforeHelp);
            helpSession = null; View.Refresh();
        }
        private NavigationPage lastPage = (NavigationPage)(-1);
        public void Refresh()
        {
            if (Garden == null || !View.Ready || Diorama == null) return;
            bool simple = Garden.simplifiedNavigation && Garden.Navigation != null;
            var page = simple ? Garden.Navigation.Page : NavigationPage.Forest;
            if (simple && lastPage != page) { PlaceOverviewTools(); lastPage = page; }
            Diorama.SetVisible(simple && page == NavigationPage.Diorama && !HelpOpen);
            overviewTools.gameObject.SetActive(simple && (page == NavigationPage.Home || page == NavigationPage.Forest || page == NavigationPage.TreeList) && !HelpOpen);
            treeTools.gameObject.SetActive(simple && page == NavigationPage.Tree && Garden.Navigation.TreeMenuOpen && !HelpOpen);
            help.gameObject.SetActive(HelpOpen);
            helpText.text = Guidance[HelpPage];
            help.GetComponentsInChildren<M5PointerTarget>().First(t => t.action == M5Action.HelpNext).GetComponent<UnityEngine.UI.Button>().interactable = HelpPage < Guidance.Length - 1;
            help.GetComponentsInChildren<M5PointerTarget>().First(t => t.action == M5Action.HelpPrevious).GetComponent<UnityEngine.UI.Button>().interactable = HelpPage > 0;
            foreach (var target in overviewTools.GetComponentsInChildren<M5PointerTarget>(true))
            {
                target.gameObject.SetActive(page == NavigationPage.Forest || target.action == M5Action.Help);
                if(target.action == M5Action.Help) ((RectTransform)target.transform).anchoredPosition = page == NavigationPage.Forest ? new Vector2(158, 53) : Vector2.zero;
            }
            ((RectTransform)overviewTools.transform).sizeDelta = page == NavigationPage.Forest ? new Vector2(660, 205) : new Vector2(320, 80);
            foreach (var target in treeTools.GetComponentsInChildren<M5PointerTarget>(true))
            {
                if (target.action == M5Action.Pause) target.GetComponentInChildren<TMP_Text>().text = Garden.Navigation?.ExplicitlyPaused == true ? "Resume" : "Pause";
                if (target.action == M5Action.FocusParent) target.GetComponent<UnityEngine.UI.Button>().interactable = View.FocusedView.FocusDepth > 0;
            }
            bool browsing = View.FocusedView.FocusRoot != null;
            focusStatus.gameObject.SetActive(simple && page == NavigationPage.Tree && browsing && !HelpOpen);
            if (browsing) focusText.text = "SUBTREE VIEW · " + View.FocusedView.FocusDepth + "\nYour route stays at depth " + View.Session.State.Decisions.Count + ".\nInspecting adds no contribution.";
            for (int i = 0; i < summaries.Count; i++)
            {
                var slot = View.nodeViews[i]; var target = summaries[i];
                int count = slot.gameObject.activeSelf ? View.FocusedView.Hidden(slot.nodeId) : 0;
                bool visible = simple && page == NavigationPage.Tree && !HelpOpen && !Garden.Navigation.TreeMenuOpen && count > 0 && i > 0;
                target.gameObject.SetActive(visible); target.nodeId = slot.nodeId;
                target.GetComponentInChildren<TMP_Text>(true).text = "Open subtree · " + count + " hidden";
                target.GetComponent<UnityEngine.UI.Button>().interactable = View.Session.State.PendingDecision == null;
            }
            if (simple && page == NavigationPage.Tree && (browsing || HelpOpen))
            {
                foreach (var target in View.controls.Where(t => t.action == TreeAction.TrueBranch || t.action == TreeAction.FalseBranch)) target.gameObject.SetActive(false);
                foreach (string name in new[] { "TrueChoiceStone", "FalseChoiceStone" })
                    View.presentationRoot.Find(name)?.gameObject.SetActive(false);
            }
            Atmosphere.SetActive(simple ? page == NavigationPage.Forest || page == NavigationPage.Diorama : Garden.Visible);
        }
        private void PlaceOverviewTools()
        {
            var page = Garden.Navigation?.Page;
            Place(overviewTools, 1.5f, page == NavigationPage.Forest ? -.43f : -.32f, page == NavigationPage.Forest ? .42f : .76f);
        }
        private void Place(Canvas canvas, float distance, float vertical, float side)
        {
            var head = Garden.Locomotion.Head; var forward = head.forward; forward.y = 0;
            if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
            forward.Normalize(); canvas.transform.rotation = Quaternion.LookRotation(forward);
            canvas.transform.position = head.position + forward * distance + Vector3.up * vertical + canvas.transform.right * side;
        }
        private void OnDestroy() { if (root != null) Destroy(root.gameObject); }
    }
}
