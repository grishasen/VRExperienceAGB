using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VRExperienceAGB.Presentation
{
    /// <summary>Desktop controls issue the same navigation commands as the Quest controls.</summary>
    public sealed class DesktopExperienceInput : MonoBehaviour
    {
        private OneTreeExperience view;
        private DesktopTreePreview observer;
        private Keyboard subscribedKeyboard;
        private bool helpVisible = true;
        private Canvas help;
        private TMP_Text helpText;
        public bool TextEntryActive => view?.Garden?.M5?.Extensions?.TextEntryActive == true;

        private void Awake()
        {
            view = GetComponent<OneTreeExperience>();
            observer = GetComponent<DesktopTreePreview>();
        }
        private System.Collections.IEnumerator Start()
        {
            if (observer == null || !observer.Active) { enabled = false; yield break; }
            while (view != null && !view.Ready) yield return null;
            if (view == null) yield break;
            foreach (var label in view.GetComponentsInChildren<TMP_Text>(true))
                if (label.name == "ControllerShortcuts") label.text = "Esc: menu · R: center · F12: screenshot";
            var root = new GameObject("DesktopControls", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
            root.transform.SetParent(observer.previewCamera.transform, false);
            help = root.GetComponent<Canvas>(); help.renderMode = RenderMode.ScreenSpaceOverlay; help.sortingOrder = 200;
            var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1440, 900); scaler.matchWidthOrHeight = .5f;
            var panel = new GameObject("ControlHints", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            panel.transform.SetParent(root.transform, false);
            var rect = (RectTransform)panel.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(16, -16); rect.sizeDelta = new Vector2(760, 60);
            var background = panel.GetComponent<UnityEngine.UI.Image>(); background.color = new Color(.015f, .04f, .05f, .92f); background.raycastTarget = false;
            var labelObject = new GameObject("Hint", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(panel.transform, false);
            helpText = labelObject.GetComponent<TextMeshProUGUI>();
            helpText.rectTransform.anchorMin = Vector2.zero; helpText.rectTransform.anchorMax = Vector2.one;
            helpText.rectTransform.offsetMin = new Vector2(12, 6); helpText.rectTransform.offsetMax = new Vector2(-12, -6);
            helpText.fontSize = 18; helpText.color = Color.white; helpText.raycastTarget = false; TreeCardTypography.SetFont(helpText);
        }
        private void Update()
        {
            if (subscribedKeyboard != Keyboard.current)
            {
                Unsubscribe(); subscribedKeyboard = Keyboard.current;
                if (subscribedKeyboard != null) subscribedKeyboard.onTextInput += Type;
            }
            if (view == null || !view.Ready || view.Garden?.Navigation == null || view.Garden.Locomotion == null || view.Director == null ||
                view.ApplicationSuspended || !UnityEngine.Application.isFocused) return;
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.f1Key.wasPressedThisFrame) helpVisible = !helpVisible;
            if (TextEntryActive)
            {
                if (keyboard.backspaceKey.wasPressedThisFrame) view.Garden.M5.Extensions.Activate(M5Action.ReviewBackspace);
                if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
                    view.Garden.M5.Extensions.Activate(M5Action.ReviewTextDone);
                if (keyboard.escapeKey.wasPressedThisFrame) view.Garden.M5.Extensions.CancelTextEntry();
                if ((keyboard.leftMetaKey.isPressed || keyboard.rightMetaKey.isPressed || keyboard.ctrlKey.isPressed) && keyboard.vKey.wasPressedThisFrame)
                    view.Garden.M5.Extensions.AppendDesktopText(GUIUtility.systemCopyBuffer);
                return;
            }
            if (keyboard.escapeKey.wasPressedThisFrame) { view.Director.ToggleMenu(); return; }
            var axis = new Vector2((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
            HandleMovement(axis, Time.unscaledDeltaTime);
            if (!view.Garden.Locomotion.InputBlocked && view.Garden.Navigation.Page == NavigationPage.Diorama)
            {
                float scale = (keyboard.eKey.isPressed ? 1 : 0) - (keyboard.qKey.isPressed ? 1 : 0);
                if (scale != 0)
                {
                    view.Garden.Navigation.InvalidatePointerPresses();
                    view.Garden.M5.Diorama.ChangeScale(scale * Mathf.Min(Time.unscaledDeltaTime, .05f) * .35f);
                }
            }
        }
        public void HandleMovement(Vector2 axis, float seconds)
        {
            if (observer == null || !observer.Active || view == null || !view.Ready || view.Garden?.Navigation == null ||
                view.Garden.Locomotion == null || TextEntryActive) return;
            view.Garden.Locomotion.HandleLeftStick(Vector2.ClampMagnitude(axis, 1), seconds);
        }
        private void Type(char character)
        {
            if (!UnityEngine.Application.isFocused || view == null || view.ApplicationSuspended || !TextEntryActive) return;
            if (Keyboard.current.leftMetaKey.isPressed || Keyboard.current.rightMetaKey.isPressed || Keyboard.current.ctrlKey.isPressed) return;
            if (!char.IsControl(character)) view.Garden.M5.Extensions.AppendDesktopText(character.ToString());
        }
        private void Unsubscribe()
        {
            if (subscribedKeyboard != null) subscribedKeyboard.onTextInput -= Type;
            subscribedKeyboard = null;
        }
        private void OnDisable() => Unsubscribe();
        private void LateUpdate()
        {
            if (help == null) return;
            help.gameObject.SetActive(helpVisible);
            bool table = view?.Garden?.Navigation?.Page == NavigationPage.Diorama;
            string text = TextEntryActive ? "Type to edit · Enter: apply · Esc: cancel · Cmd+V: paste" :
                (table ? "A / D: rotate table · W / S: distance · Q / E: size" : "W A S D: walk in forest") +
                "\nRight-drag: look · Click: select · Esc: menu · R: center · F1: hide help";
            helpText.text = text;
        }
        private void OnDestroy() { if (help != null) Destroy(help.gameObject); }
    }
}
