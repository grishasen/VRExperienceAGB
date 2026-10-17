using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Presentation
{
    /// <summary>Explicit forest selection. Opening choices never enters a tree or changes its route.</summary>
    public sealed class TreeDescriptionCard : MonoBehaviour
    {
        private ForestGardenView garden;
        private TMP_Text title, structure, contribution, details, pageLabel;
        private GameObject exploreButton, detailsButton, closeButton, previousButton, nextButton;
        private int page = 1;
        public bool DetailsVisible { get; private set; }
        public TMP_Text DetailsText => details;
        private ModelDefinition model;
        public int Index { get; private set; } = -1;
        public long Revision { get; private set; }
        public TMP_Text ContributionText => contribution;
        public bool Visible => gameObject.activeInHierarchy;
        public bool CanInteract => Visible && garden.Visible && garden.Experience.Model == model &&
            garden.Experience.Director?.MenuVisible != true && !garden.M5.HelpOpen &&
            !garden.M5.Comparison.PanelOpen && !garden.M5.Extensions.PanelOpen &&
            (!garden.simplifiedNavigation || garden.Navigation.Page == NavigationPage.Forest);

        internal static TreeDescriptionCard Create(ForestGardenView garden, Transform parent)
        {
            var canvas = garden.CanvasAt("TreeDescriptionCard", Vector3.zero, new Vector2(720, 320), .0016f, parent);
            var background = canvas.gameObject.AddComponent<UnityEngine.UI.Image>();
            background.color = new Color(.035f, .065f, .095f, 1);
            background.raycastTarget = true;
            var card = canvas.gameObject.AddComponent<TreeDescriptionCard>();
            card.garden = garden;
            card.title = card.Text("Title", new Vector2(0, 106), new Vector2(648, 48), 36, true);
            card.closeButton = card.Button("×", new Vector2(300, 110), false, true);
            card.structure = card.Text("Structure", new Vector2(0, 57), new Vector2(648, 40), 25);
            card.structure.color = new Color(.70f, .77f, .83f);
            card.contribution = card.Text("Contribution", new Vector2(0, -3), new Vector2(648, 58), 27);
            card.exploreButton = card.Button("Explore tree", new Vector2(-166, -103), true);
            card.detailsButton = card.Button("Details", new Vector2(166, -103), false);
            card.details = card.Text("InlineDetails", new Vector2(0, 38), new Vector2(648, 310), 25);
            card.details.overflowMode = TextOverflowModes.Page;
            card.pageLabel = card.Text("DetailPage", new Vector2(0, -164), new Vector2(100, 40), 22);
            card.pageLabel.alignment = TextAlignmentOptions.Center;
            card.previousButton = card.Button("Previous", new Vector2(-218, -164), false, false, -1);
            card.nextButton = card.Button("Next", new Vector2(218, -164), false, false, 1);
            card.SetDetails(false);
            card.Hide();
            return card;
        }

        private TMP_Text Text(string name, Vector2 position, Vector2 size, int fontSize, bool heading = false)
        {
            var text = garden.Text(transform, name, "", position, size, fontSize);
            TreeCardTypography.Apply(text, heading);
            return text;
        }

        private GameObject Button(string label, Vector2 position, bool explore, bool close = false, int pageDelta = 0)
        {
            var go = new GameObject(pageDelta != 0 ? "DetailPage" + pageDelta : close ? "CloseTreeChoices" : explore ? "ExploreTree" : "TreeDetails", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            go.transform.SetParent(transform, false);
            var rect = (RectTransform)go.transform;
            rect.anchoredPosition = position;
            rect.sizeDelta = close ? new Vector2(48, 48) : pageDelta != 0 ? new Vector2(210, 52) : new Vector2(316, 66);
            go.GetComponent<UnityEngine.UI.Image>().color = explore ? new Color(.12f, .29f, .37f) : new Color(.10f, .16f, .22f);
            go.GetComponent<UnityEngine.UI.Button>().navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            var text = garden.Text(go.transform, "Label", label, Vector2.zero, rect.sizeDelta - new Vector2(12, 8), 26);
            TreeCardTypography.Apply(text);
            text.alignment = TextAlignmentOptions.Center;
            var target = go.AddComponent<TreeCardButton>();
            target.Card = this;
            target.Explore = explore;
            target.Close = close;
            target.PageDelta = pageDelta;
            return go;
        }

        public void Show(int index)
        {
            bool changed = !Visible || Index != index || model != garden.Experience.Model;
            Index = index;
            model = garden.Experience.Model;
            if (changed)
            {
                Revision++;
                SetDetails(false);
                var head = garden.Locomotion.Head;
                var forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
                if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
                transform.SetPositionAndRotation(head.position + forward * 2.0f + Vector3.Cross(Vector3.up, forward) * .62f - Vector3.up * .12f, Quaternion.LookRotation(forward));
            }
            gameObject.SetActive(true);
            RefreshContent();
        }

        internal void RefreshContent()
        {
            if (Index < 0 || model != garden.Experience.Model || Index >= garden.PlotCount) { Hide(); return; }
            var metric = garden.Metrics[Index];
            title.text = "Tree " + (Index + 1).ToString("00");
            structure.text = "Depth " + metric.MaximumDepth + "  ·  " + metric.LeafCount + " leaves";
            contribution.text = garden.TreeContributionCopy(Index);
        }

        private void SetDetails(bool expanded)
        {
            DetailsVisible = expanded; page = 1; Revision++;
            ((RectTransform)transform).sizeDelta = new Vector2(720, expanded ? 600 : 320);
            title.rectTransform.anchoredPosition = new Vector2(0, expanded ? 244 : 106);
            ((RectTransform)closeButton.transform).anchoredPosition = new Vector2(300, expanded ? 248 : 110);
            ((RectTransform)exploreButton.transform).anchoredPosition = new Vector2(-166, expanded ? -242 : -103);
            ((RectTransform)detailsButton.transform).anchoredPosition = new Vector2(166, expanded ? -242 : -103);
            detailsButton.GetComponentInChildren<TMP_Text>().text = expanded ? "Summary" : "Details";
            structure.gameObject.SetActive(!expanded); contribution.gameObject.SetActive(!expanded);
            details.gameObject.SetActive(expanded); pageLabel.gameObject.SetActive(expanded);
            previousButton.SetActive(expanded); nextButton.SetActive(expanded);
            if (expanded) {
                details.text = garden.Experience.Director.TreeDetailsCopy(Index);
                TurnPage(0);
            }
        }
        internal void TurnPage(int delta)
        {
            if (!DetailsVisible) return;
            details.ForceMeshUpdate();
            int count = Mathf.Max(1, details.textInfo.pageCount);
            page = Mathf.Clamp(page + delta, 1, count); details.pageToDisplay = page;
            pageLabel.text = page + " / " + count;
            previousButton.GetComponent<UnityEngine.UI.Button>().interactable = page > 1;
            nextButton.GetComponent<UnityEngine.UI.Button>().interactable = page < count;
            Revision++;
        }

        private void Update()
        {
            if (!CanInteract) Hide();
        }
        public void Hide() { Revision++; Index = -1; gameObject.SetActive(false); }
        private void OnDisable() { Revision++; }
        internal long SessionRevision => garden.Experience.Ensemble.Revision;
        internal long NavigationRevision => garden.Navigation?.Revision ?? 0;
        internal void Activate(bool explore)
        {
            if (!CanInteract) return;
            if (!explore) { SetDetails(!DetailsVisible); return; }
            int index = Index; Hide();
            garden.Activate(GardenCommand.SelectTree, index);
        }
    }

    public sealed class TreeCardButton : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
    {
        public TreeDescriptionCard Card;
        public bool Explore;
        public bool Close;
        public int PageDelta;
        private long revision = -1, sessionRevision, navigationRevision;
        private int pointer;
        public void OnPointerDown(PointerEventData data)
        {
            if (!Card.CanInteract || data.button != PointerEventData.InputButton.Left || !GetComponent<UnityEngine.UI.Button>().IsInteractable()) return;
            revision = Card.Revision; sessionRevision = Card.SessionRevision; navigationRevision = Card.NavigationRevision; pointer = data.pointerId;
        }
        public void OnPointerClick(PointerEventData data)
        {
            if (revision != Card.Revision || sessionRevision != Card.SessionRevision || navigationRevision != Card.NavigationRevision || pointer != data.pointerId || data.button != PointerEventData.InputButton.Left || !Card.CanInteract) return;
            revision = -1;
            ControllerSelectionFeedback.Pulse(data.pointerId);
            if(Close) Card.Hide(); else if(PageDelta != 0) Card.TurnPage(PageDelta); else Card.Activate(Explore);
        }
        private void OnDisable() { revision = -1; }
    }
}
